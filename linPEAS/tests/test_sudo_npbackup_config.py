import json
import os
import shlex
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


class SudoBackupConfigTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = Path(__file__).resolve().parents[2]
        cls.module = cls.root / "linPEAS/builder/linpeas_parts/6_users_information/7_Sudo_l.sh"

    def run_case(self, policy, *, allow=True, password="", root_folder="/",
                 timeout=True, temp_name="config", date_step=None):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            bindir = base / "bin"
            bindir.mkdir()
            log = base / "calls.jsonl"
            sudo = bindir / "sudo"
            sudo.write_text(
                f"#!{sys.executable}\n"
                "import json, os, sys\n"
                "from pathlib import Path\n"
                "args = sys.argv[1:]\n"
                "with open(os.environ['FAKE_SUDO_LOG'], 'a') as out:\n"
                "    out.write(json.dumps(args) + '\\n')\n"
                "if args in (['-n', '-l'], ['-S', '-l']):\n"
                "    print(os.environ['FAKE_SUDO_POLICY'])\n"
                "    sys.exit(0 if os.environ['FAKE_SUDO_POLICY'] else 1)\n"
                "if (len(args) == 9 and args[0] in ('-n', '-S') and\n"
                "    args[1:4] == ['-l', '-u', 'root'] and args[4] == '--' and\n"
                "    args[5].endswith('/npbackup-cli') and args[6] == '-c' and\n"
                "    args[8] == '-b' and not Path(args[7]).exists()):\n"
                "    sys.exit(0 if os.environ['FAKE_QUERY_ALLOW'] == '1' else 1)\n"
                "sys.exit(99)  # Executing the client is never accepted.\n"
            )
            sudo.chmod(0o755)
            timeout_bin = bindir / "timeout"
            timeout_bin.write_text("#!/bin/sh\nshift\nexec \"$@\"\n")
            timeout_bin.chmod(0o755)
            if date_step is not None:
                date_bin = bindir / "date"
                date_bin.write_text(
                    "#!/bin/sh\n"
                    "n=$(cat \"$FAKE_DATE_STATE\" 2>/dev/null || echo 0)\n"
                    "echo \"$n\"\n"
                    f"echo $((n + {int(date_step)})) > \"$FAKE_DATE_STATE\"\n"
                )
                date_bin.chmod(0o755)
            config_dir = base / temp_name
            config_dir.mkdir()
            env = os.environ.copy()
            env.update({
                "PATH": f"{bindir}:{env['PATH']}",
                "FAKE_SUDO_LOG": str(log),
                "FAKE_SUDO_POLICY": policy,
                "FAKE_QUERY_ALLOW": "1" if allow else "0",
                "TMPDIR": str(config_dir),
                "FAKE_DATE_STATE": str(base / "date-state"),
            })
            body = "\n".join([
                "E=E", "sudoB=__no_color__", "sudoG=__no_color__",
                "sudoVB1=__no_color__", "sudoVB2=__no_color__",
                "SED_RED='&'", "SED_RED_YELLOW='&'", "SED_GREEN='&'",
                f"ROOT_FOLDER={shlex.quote(root_folder)}",
                f"PASSWORD={shlex.quote(password)}",
                f"TIMEOUT={shlex.quote(str(timeout_bin) if timeout else '/nonexistent/timeout')}",
                "print_2title() { :; }", "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(self.module))}",
            ])
            result = subprocess.run(["sh", "-c", body], env=env,
                                    capture_output=True, text=True, timeout=15)
            self.assertEqual(result.returncode, 0, result.stderr)
            calls = [json.loads(line) for line in log.read_text().splitlines()]
            self.assertTrue(all("-l" in call for call in calls), calls)
            queries = [call for call in calls if "-u" in call]
            self.assertLessEqual(len(queries), 10)
            self.assertFalse(list(config_dir.iterdir()), "probe must not create a config")
            return result.stdout, queries

    def test_root_and_all_rules_confirm_exact_query(self):
        for runas in ("root", "ALL : ALL"):
            with self.subTest(runas=runas):
                output, queries = self.run_case(
                    f"    ({runas}) NOPASSWD: /usr/local/bin/npbackup-cli")
                self.assertIn("Root-capable backup client accepts caller-selected config", output)
                self.assertEqual(len(queries), 1)
                self.assertEqual(queries[0][0:7],
                                 ["-n", "-l", "-u", "root", "--",
                                  "/usr/local/bin/npbackup-cli", "-c"])
                self.assertEqual(queries[0][8], "-b")

    def test_fixed_negated_nonroot_and_ambiguous_rules_are_skipped(self):
        for rule in (
            '    (root) NOPASSWD: /usr/local/bin/npbackup-cli ""',
            '    (root) NOPASSWD: /usr/local/bin/npbackup-cli -c /root/locked.conf',
            '    (daemon) NOPASSWD: /usr/local/bin/npbackup-cli',
            '    (ALL, !root) NOPASSWD: /usr/local/bin/npbackup-cli',
            '    (root) NOPASSWD: !/usr/local/bin/npbackup-cli',
            '    (root) NOPASSWD: /usr/local/bin/npbackup-cli-helper',
            '    (root) NOPASSWD: /usr/local/bin/npbackup-cli\\ test',
            '    (root) NOPASSWD: /usr/local/my\\ tools/npbackup-cli',
        ):
            with self.subTest(rule=rule):
                output, queries = self.run_case(rule)
                self.assertNotIn("Root-capable backup client accepts", output)
                self.assertEqual(queries, [])

    def test_comma_rules_and_duplicate_output_yield_one_probe(self):
        output, queries = self.run_case(
            "    (root) NOPASSWD: /bin/true, /usr/local/bin/npbackup-cli")
        self.assertIn("Root-capable backup client accepts", output)
        self.assertEqual(len(queries), 1)

    def test_denial_auth_and_timeout_remain_unverified(self):
        policy = "    (root) /usr/local/bin/npbackup-cli"
        for kwargs in ({"allow": False}, {"timeout": False}):
            with self.subTest(kwargs=kwargs):
                output, queries = self.run_case(policy, **kwargs)
                self.assertIn("authorization unverified", output)
                self.assertNotIn("Root-capable backup client accepts", output)
                self.assertEqual(len(queries), 1 if kwargs.get("allow") is False else 0)

    def test_password_and_offline_root(self):
        policy = "    (root) /usr/local/bin/npbackup-cli"
        output, queries = self.run_case(policy, password="fixture-password")
        self.assertIn("Root-capable backup client accepts", output)
        self.assertEqual(queries[0][0], "-S")
        output, queries = self.run_case(policy, root_folder="/offline")
        self.assertNotIn("Root-capable backup client accepts", output)
        self.assertEqual(queries, [])

    def test_rule_and_probe_cap(self):
        policy = "\n".join(
            f"    (root) NOPASSWD: /opt/client{i}/npbackup-cli"
            for i in range(15)
        )
        output, queries = self.run_case(policy)
        self.assertEqual(len(queries), 10)
        self.assertEqual(output.count("Root-capable backup client accepts"), 10)

    def test_total_probe_time_budget_stops_later_rules(self):
        policy = "\n".join(
            f"    (root) NOPASSWD: /opt/client{i}/npbackup-cli"
            for i in range(10)
        )
        output, queries = self.run_case(policy, date_step=2)
        self.assertEqual(len(queries), 2)
        self.assertEqual(output.count("Root-capable backup client accepts"), 2)

    def test_backup_metadata_lists_config_without_content(self):
        data = yaml.safe_load((self.root / "build_lists/sensitive_files.yaml").read_text())
        backups = next(item for item in data["search"] if item["name"] == "Backups")
        matches = [item["value"] for item in backups["value"]["files"]
                   if item["name"] == "npbackup.conf"]
        self.assertEqual(matches, [{"just_list_file": True, "type": "f",
                                    "search_in": ["common"]}])
        self.assertFalse(backups["value"]["config"]["auto_check"])
        db_section = next(item for item in data["search"] if item["name"] == "Database")
        self.assertIn("*.db", [item["name"] for item in db_section["value"]["files"]])

        sys.path.insert(0, str(self.root / "linPEAS"))
        from builder.src.fileRecord import FileRecord
        from builder.src.linpeasBuilder import LinpeasBuilder
        from builder.src.peassRecord import PEASRecord

        record = FileRecord(regex="npbackup.conf", **matches[0].copy())
        section = PEASRecord("Backups", False, [], [record])
        line = LinpeasBuilder.__new__(LinpeasBuilder)._LinpeasBuilder__construct_file_line(
            section, record
        )
        self.assertIn('ls -ld "$f"', line)
        self.assertNotIn('cat "$f"', line)
        self.assertIn('npbackup\\.conf$', line)


if __name__ == "__main__":
    unittest.main()
