import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/16_Crontab_UI_misconfig.sh"
)
SIGNATURES = Path(__file__).resolve().parents[2] / "build_lists/sensitive_files.yaml"
UNIT_PATTERN = r"^[[:space:]]*ExecStart(Pre|Post)?[[:space:]]*=[^#]*crontab-ui"
SECRET = "FixtureSecret73"


class CrontabUiMisconfigTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.units = self.root / "units"
        self.units.mkdir()
        self.bin = self.root / "bin"
        self.bin.mkdir()
        self.db = self.root / "opt/crontabs/crontab.db"
        self.db.parent.mkdir(parents=True)
        self.ps_file = self.root / "ps.txt"
        self.ps_file.write_text("")
        self.call_log = self.root / "network-calls"
        module_copy = self.root / "crontab-ui-check.sh"
        source = MODULE.read_text()
        source = source.replace(
            "/etc/systemd/system /lib/systemd/system /usr/lib/systemd/system",
            str(self.units),
        ).replace("/opt/crontabs/crontab.db", str(self.db))
        module_copy.write_text(source)
        for name, body in {
            "systemctl": (
                '#!/bin/sh\n'
                'case "$1" in\n'
                '  list-units|list-unit-files) exit 0 ;;\n'
                '  is-active) echo inactive ;;\n'
                '  show) case "$4" in\n'
                '    LoadState) echo LoadState=loaded ;;\n'
                '    User) echo User= ;;\n'
                '    Environment) printf "Environment=%s\\n" "$FAKE_ENVIRONMENT" ;;\n'
                '  esac ;;\n'
                'esac\n'
            ),
            "ps": '#!/bin/sh\ncat "$FAKE_PS_FILE"\n',
            "ss": '#!/bin/sh\necho ss >> "$FAKE_NETWORK_LOG"\nexit 99\n',
            "netstat": '#!/bin/sh\necho netstat >> "$FAKE_NETWORK_LOG"\nexit 99\n',
            "curl": '#!/bin/sh\necho curl >> "$FAKE_NETWORK_LOG"\nexit 99\n',
            "wget": '#!/bin/sh\necho wget >> "$FAKE_NETWORK_LOG"\nexit 99\n',
        }.items():
            path = self.bin / name
            path.write_text(body)
            path.chmod(0o755)
        self.env = os.environ.copy()
        self.env.update({
            "PATH": f"{self.bin}:{self.env['PATH']}",
            "FAKE_PS_FILE": str(self.ps_file),
            "FAKE_NETWORK_LOG": str(self.call_log),
            "FAKE_ENVIRONMENT": "",
            "SEARCH_IN_FOLDER": "",
        })
        self.script = (
            'print_2title() { :; }; print_info() { :; }; '
            'print_list() { printf "%s\\n" "$1"; }; echo_not_found() { :; }; '
            '. "$CRONTAB_UI_MODULE"'
        )
        self.env["CRONTAB_UI_MODULE"] = str(module_copy)

    def run_module(self, shell="sh"):
        if shell == "busybox ash":
            argv = ["busybox", "ash", "-c", self.script]
        else:
            argv = [shell, "-c", self.script]
        result = subprocess.run(argv, env=self.env, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse(self.call_log.exists(), "network command was invoked")
        return result.stdout

    def add_unit(self, name="scheduler.service"):
        (self.units / name).write_text(
            "[Service]\nExecStart=/usr/bin/crontab-ui\n", encoding="utf-8"
        )

    def add_ndjson(self, path=None):
        target = path or self.db
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(
            '{"name":"backup","command":"zip -r -P '
            + SECRET
            + ' /tmp/backup.zip /srv"}\n'
            '{"name":"cleanup","command":"find /tmp -type f"}\n',
            encoding="utf-8",
        )
        return target

    def test_ere_unit_discovery_and_missing_environment(self):
        self.add_unit()
        self.add_ndjson()
        out = self.run_module()
        self.assertIn("Service evidence: scheduler.service (state: inactive", out)
        self.assertIn("root (systemd default; configuration evidence)", out)
        self.assertIn(str(self.db), out)
        self.assertIn("Jobs with command fields: 2", out)
        self.assertIn("inline zip -P credential candidates: 1", out)
        self.assertNotIn(SECRET, out)
        self.assertNotIn("backup.zip", out)

    def test_custom_path_and_environment_credentials_redacted(self):
        self.add_unit()
        custom = self.add_ndjson(self.root / "custom/crontab.db")
        self.env["FAKE_ENVIRONMENT"] = (
            f"CRON_DB_PATH={custom.parent} BASIC_AUTH_USER=root "
            f"BASIC_AUTH_PWD={SECRET}"
        )
        out = self.run_module()
        self.assertIn(str(custom), out)
        self.assertIn("BASIC_AUTH_PWD is set (value redacted)", out)
        self.assertNotIn(SECRET, out)
        self.assertNotIn(str(self.db), out)

    def test_unreadable_and_oversized_db_are_uncertain(self):
        self.add_unit()
        self.add_ndjson()
        self.db.chmod(0)
        try:
            out = self.run_module()
            self.assertIn("unreadable; contents unknown", out)
            self.assertNotIn("credential candidates:", out)
        finally:
            self.db.chmod(0o600)
        self.db.write_bytes(b"x" * (1048576 + 1))
        out = self.run_module()
        self.assertIn("oversized:", out)
        self.assertIn("contents not inspected", out)
        self.assertNotIn("credential candidates:", out)

    def test_false_positive_process_does_not_trigger_db_scan(self):
        self.add_ndjson()
        self.ps_file.write_text("alice python python /app/report-crontab-ui.py\n")
        out = self.run_module()
        self.assertNotIn("Process evidence", out)
        self.assertNotIn("DB candidate:", out)
        self.assertNotIn(SECRET, out)

    def test_real_process_is_redacted_and_correlates_db(self):
        self.add_ndjson()
        self.ps_file.write_text(
            f"root crontab-ui /usr/bin/crontab-ui --password {SECRET}\n"
        )
        out = self.run_module()
        self.assertIn("Process evidence (owner and executable only): root crontab-ui", out)
        self.assertIn("inline zip -P credential candidates: 1", out)
        self.assertNotIn(SECRET, out)

    def test_node_process_path_component_is_recognized(self):
        self.add_ndjson()
        self.ps_file.write_text("root node /usr/bin/node /opt/crontab-ui/bin/server.js\n")
        out = self.run_module()
        self.assertIn("Process evidence (owner and executable only): root crontab-ui", out)
        self.assertIn("DB candidate:", out)

    def test_shells_and_grep_implementations(self):
        self.add_unit()
        self.add_ndjson()
        for shell in ("sh", "bash", "dash", "busybox ash"):
            executable = shell.split()[0]
            if not shutil.which(executable):
                continue
            with self.subTest(shell=shell):
                self.assertIn("inline zip -P credential candidates: 1", self.run_module(shell))
        for grep in ("grep", "ggrep"):
            executable = shutil.which(grep)
            if not executable:
                continue
            with self.subTest(grep=grep):
                result = subprocess.run(
                    [executable, "-Eil", "-e", UNIT_PATTERN, str(self.units / "scheduler.service")],
                    capture_output=True,
                    text=True,
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("scheduler.service", result.stdout)
        if shutil.which("busybox"):
            result = subprocess.run(
                ["busybox", "grep", "-Eil", "-e", UNIT_PATTERN, str(self.units / "scheduler.service")],
                capture_output=True,
                text=True,
            )
            self.assertEqual(result.returncode, 0, result.stderr)

    def test_signature_lists_db_without_printing_matching_secret_line(self):
        text = SIGNATURES.read_text()
        block = text.split('          - name: "crontab.db"', 1)[1].split(
            '          - name: "crontab-ui.service"', 1
        )[0]
        self.assertIn("just_list_file: True", block)
        self.assertNotIn("bad_regex:", block)
        self.assertNotIn("only_bad_lines:", block)


if __name__ == "__main__":
    unittest.main()
