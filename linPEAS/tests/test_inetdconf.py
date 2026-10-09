import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class InetdTelnetCVE202624061Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.part = (
            cls.repo_root
            / "linPEAS/builder/linpeas_parts/5_network_information/9_Inetdconf.sh"
        )
        cls.part_without_entrypoint = cls.part.read_text(encoding="utf-8").split(
            "# Run the main function\n", 1
        )[0]

    def _run_hint(self, stanza, version_output="telnetd (GNU inetutils) 2.7", trusted_daemon=True):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            source = base / "part.sh"
            source.write_text(self.part_without_entrypoint, encoding="utf-8")
            conf = base / "inetd.conf"
            daemon = base / "in.telnetd"
            daemon.write_text(
                '#!/bin/sh\nprintf "%s\\n" "$*" >> "$VERSION_CALLS"\n'
                'printf "%s\\n" "$FAKE_VERSION_OUTPUT"\n',
                encoding="utf-8",
            )
            daemon.chmod(0o755)
            conf.write_text(stanza.format(daemon=daemon), encoding="utf-8")
            bindir = base / "bin"
            bindir.mkdir()
            timeout = bindir / "timeout"
            timeout.write_text(
                '#!/bin/sh\n'
                '[ "$1" = -k ] && [ "$2" = 1 ] && [ "$3" = 1 ] || exit 2\n'
                'shift 3\nexec "$@"\n',
                encoding="utf-8",
            )
            timeout.chmod(0o755)
            calls = base / "calls"
            env = os.environ.copy()
            env.update(
                {
                    "PATH": f"{bindir}:{env['PATH']}",
                    "VERSION_CALLS": str(calls),
                    "FAKE_VERSION_OUTPUT": version_output,
                }
            )
            result = subprocess.run(
                [
                    "sh",
                    "-c",
                    ('lp_trusted_version_path() { printf "%s\\n" "$1"; }; '
                     if trusted_daemon else
                     f". {shlex.quote(str(self.part.parent.parent / 'functions/lp_trusted_version_path.sh'))}; ")
                    + f". {shlex.quote(str(source))}; "
                    f"inetd_telnet_cve_hint {shlex.quote(str(conf))}",
                ],
                cwd=self.repo_root,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )
            arguments = calls.read_text(encoding="utf-8") if calls.exists() else ""
        return result, arguments

    def test_active_root_gnu_stanza_reports_upstream_range_and_checks_version_once(self):
        stanza = (
            "telnet stream tcp nowait root {daemon} in.telnetd\n"
            "telnet stream tcp6 nowait root:root {daemon} in.telnetd\n"
        )
        result, calls = self._run_hint(stanza)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.count("upstream affected range"), 2)
        self.assertIn("confirm package patches and active listener", result.stdout)
        self.assertEqual(calls, "--version\n")

    def test_commented_and_non_root_stanzas_do_not_trigger_hint_or_version(self):
        stanza = (
            "# telnet stream tcp nowait root {daemon} in.telnetd\n"
            "  # telnet stream tcp nowait root {daemon} in.telnetd\n"
            "telnet stream tcp nowait nobody {daemon} in.telnetd\n"
            "ftp stream tcp nowait root {daemon} in.telnetd\n"
        )
        result, calls = self._run_hint(stanza)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")
        self.assertEqual(calls, "")

    def test_non_gnu_implementation_does_not_get_affected_range_hint(self):
        result, _ = self._run_hint(
            "telnet stream tcp nowait root {daemon} in.telnetd\n",
            "BusyBox v1.36.0 telnetd",
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("appears non-GNU", result.stdout)
        self.assertNotIn("upstream affected range", result.stdout)

    def test_untrusted_configured_daemon_is_not_executed(self):
        result, calls = self._run_hint(
            "telnet stream tcp nowait root {daemon} in.telnetd\n",
            trusted_daemon=False,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(calls, "")
        self.assertIn("implementation/version unknown", result.stdout)

    def test_missing_version_is_informational(self):
        result, _ = self._run_hint(
            "telnet stream tcp nowait root {daemon} in.telnetd\n", ""
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("implementation/version unknown", result.stdout)
        self.assertNotIn("upstream affected range", result.stdout)
        self.assertNotIn("vulnerable", result.stdout.lower())

    def test_version_probe_output_is_bounded(self):
        result, calls = self._run_hint(
            "telnet stream tcp nowait root {daemon} in.telnetd\n",
            "x" * 4096 + "\ntelnetd (GNU inetutils) 2.7",
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(calls, "--version\n")
        self.assertIn("implementation/version unknown", result.stdout)
        self.assertNotIn("upstream affected range", result.stdout)

    def test_gnu_version_outside_range_is_distinguished(self):
        result, _ = self._run_hint(
            "telnet stream tcp nowait root {daemon} in.telnetd\n",
            "telnetd (GNU inetutils) 2.8",
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("outside upstream affected range", result.stdout)

    def test_upstream_range_boundaries(self):
        cases = {
            "1.9.2": "outside upstream affected range",
            "1.9.3": "in upstream affected range",
            "2.7": "in upstream affected range",
            "2.7.1": "outside upstream affected range",
        }
        for version, expected in cases.items():
            with self.subTest(version=version):
                result, _ = self._run_hint(
                    "telnet stream tcp nowait root {daemon} in.telnetd\n",
                    f"telnetd (GNU inetutils) {version}",
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn(expected, result.stdout)

    def test_inetutils_inetd_name_is_accepted_for_command_and_process(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            source = Path(tmpdir) / "part.sh"
            source.write_text(self.part_without_entrypoint, encoding="utf-8")
            body = "\n".join(
                [
                    f". {shlex.quote(str(source))}",
                    'check_command() { [ "$1" = inetutils-inetd ]; }',
                    'pgrep() { [ "$1" = -x ] && [ "$2" = inetutils-inetd ]; }',
                    'warn_exec() { printf "WARN_EXEC:%s\\n" "$1"; }',
                    'print_3title() { :; }',
                    'echo_not_found() { printf "NOT_FOUND:%s\\n" "$1"; }',
                    "E=E",
                    "SED_YELLOW='&'",
                    "analyze_inetd",
                ]
            )
            result = subprocess.run(
                ["sh", "-c", body], capture_output=True, text=True, check=False
            )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("WARN_EXEC:inetutils-inetd", result.stdout)
        self.assertNotIn("inetd is not running", result.stdout)
        self.assertNotIn("NOT_FOUND:inetd", result.stdout)


if __name__ == "__main__":
    unittest.main()
