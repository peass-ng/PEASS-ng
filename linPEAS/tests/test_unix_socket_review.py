import os
import shlex
import socket
import subprocess
import tempfile
import unittest
from pathlib import Path


class UnixSocketReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets"
            / "13_Unix_sockets_listening.sh"
        )

    def _run_module(self, name, file_mode, ss_line="", bsd_stat=False):
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            socket_path = root / name
            unix_socket = socket.socket(socket.AF_UNIX)
            try:
                unix_socket.bind(str(socket_path))
            finally:
                unix_socket.close()
            socket_path.chmod(file_mode)

            # Keep the test independent of host listeners, filesystem scans, and ownership.
            bindir = root / "bin"
            bindir.mkdir()
            for command, body in {
                "ss": '#!/bin/sh\nprintf "%s\\n" "$FAKE_SS_LINE"\n',
                "find": '#!/bin/sh\nprintf "%s\\n" "$FAKE_SOCKET_PATH"\n',
                "stat": (
                    '#!/bin/sh\n'
                    'if [ "$FAKE_BSD_STAT" = yes ] && [ "$1" = -c ]; then exit 1; fi\n'
                    'case "$FAKE_SOCKET_MODE" in\n'
                    '  000) printf "660|root|operators|0\\n" ;;\n'
                    '  *) printf "770|root|operators|0\\n" ;;\n'
                    'esac\n'
                ),
                "netstat": "#!/bin/sh\nexit 0\n",
                "lsof": "#!/bin/sh\nexit 0\n",
            }.items():
                path = bindir / command
                path.write_text(body, encoding="utf-8")
                path.chmod(0o755)

            script = "\n".join(
                [
                    "IAMROOT=",
                    "SEARCH_IN_FOLDER=",
                    "EXTRA_CHECKS=",
                    "E=E",
                    "SED_GREEN=",
                    "print_2title() { :; }",
                    "print_info() { :; }",
                    f". {shlex.quote(str(self.module))}",
                ]
            )
            env = os.environ.copy()
            env.update(
                {
                    "PATH": f"{bindir}:{env['PATH']}",
                    "FAKE_SS_LINE": ss_line.replace("SOCKET_PATH", str(socket_path)),
                    "FAKE_SOCKET_PATH": str(socket_path),
                    "FAKE_SOCKET_MODE": f"{file_mode:03o}",
                    "FAKE_BSD_STAT": "yes" if bsd_stat else "no",
                }
            )
            result = subprocess.run(
                ["sh", "-c", script], env=env, capture_output=True, text=True
            )
            return result, str(socket_path)

    def test_accessible_root_socket_is_review_lead_with_ss_details(self):
        if os.geteuid() == 0:
            self.skipTest("permission fixture requires a non-root test user")
        result, path = self._run_module(
            "accessible.sock",
            0o770,
            'u_str LISTEN 0 5 SOCKET_PATH 123 * 0 users:(("daemon",pid=4242,fd=3)) uid:0',
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(f"Path: {path}", result.stdout)
        self.assertIn("Mode: 770; Owner/Group: root:operators", result.stdout)
        self.assertIn("Current user access: read=yes, write=yes", result.stdout)
        self.assertIn("ss listener PID: 4242", result.stdout)
        self.assertIn("ss socket UID: 0", result.stdout)
        self.assertIn("Review lead:", result.stdout)
        self.assertNotIn("High risk", result.stdout)

    def test_nonmember_socket_has_no_access_or_review_lead(self):
        if os.geteuid() == 0:
            self.skipTest("permission fixture requires a non-root test user")
        result, path = self._run_module(
            "nonmember.sock", 0o000, "u_str LISTEN 0 5 SOCKET_PATH 123 * 0"
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(f"Path: {path}", result.stdout)
        self.assertIn("Mode: 660; Owner/Group: root:operators", result.stdout)
        self.assertIn("Current user access: read=no, write=no", result.stdout)
        self.assertNotIn("Review lead:", result.stdout)

    def test_discovered_non_listening_socket_has_no_listener_claim(self):
        if os.geteuid() == 0:
            self.skipTest("permission fixture requires a non-root test user")
        result, path = self._run_module("not-listening.sock", 0o770)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(f"Path: {path}", result.stdout)
        self.assertNotIn("Review lead:", result.stdout)
        self.assertNotIn("ss listener", result.stdout)
        self.assertNotIn("High risk", result.stdout)

    def test_bsd_stat_fallback_preserves_root_owner_review(self):
        if os.geteuid() == 0:
            self.skipTest("permission fixture requires a non-root test user")
        result, _ = self._run_module(
            "bsd-stat.sock", 0o770, "u_str LISTEN 0 5 SOCKET_PATH 123 * 0", bsd_stat=True
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Mode: 770; Owner/Group: root:operators", result.stdout)
        self.assertIn("Review lead:", result.stdout)


if __name__ == "__main__":
    unittest.main()
