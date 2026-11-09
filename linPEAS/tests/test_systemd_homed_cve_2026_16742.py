import os
import shlex
import socket
import subprocess
import tempfile
import unittest
from pathlib import Path


class SystemdHomedCVE202616742Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.check = (
            Path(__file__).resolve().parents[2]
            / "linPEAS/builder/linpeas_parts/functions/checkSystemdHomedCVE202616742.sh"
        )

    def run_fixture(self, version="261", nss=None, identity=True, userdb=True, iamroot=""):
        if nss is None:
            nss = "passwd: files systemd\ngroup: files systemd\n"
        with tempfile.TemporaryDirectory() as dirname:
            root = Path(dirname)
            (root / "etc").mkdir()
            (root / "etc/nsswitch.conf").write_text(nss, encoding="utf-8")
            home = root / "home"
            home.mkdir()
            if identity:
                (home / ".identity").write_text("not read by check\n", encoding="utf-8")
            sock = None
            if userdb:
                userdb_dir = root / "run/systemd/userdb"
                userdb_dir.mkdir(parents=True)
                sock = socket.socket(socket.AF_UNIX)
                sock.bind(str(userdb_dir / "io.systemd.Home"))
            try:
                body = "\n".join(
                    (
                        f"IAMROOT={shlex.quote(iamroot)}",
                        'print_3title() { printf "TITLE: %s\\n" "$1"; }',
                        "print_info() { :; }",
                        f". {shlex.quote(str(self.check))}",
                        "checkSystemdHomedCVE202616742 "
                        + " ".join(shlex.quote(x) for x in (version, str(root), str(home))),
                    )
                )
                return subprocess.run(
                    ["sh", "-c", body],
                    env=os.environ.copy(),
                    text=True,
                    capture_output=True,
                    check=False,
                )
            finally:
                if sock is not None:
                    sock.close()

    def test_current_homed_user_in_affected_range_is_reported_without_identity_contents(self):
        for version in ("245", "261"):
            with self.subTest(version=version):
                result = self.run_fixture(version=version)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("CVE-2026-16742", result.stdout)
                self.assertNotIn("not read by check", result.stdout)
                self.assertIn("vendor backport", result.stdout)

    def test_non_candidates_are_quiet(self):
        cases = (
            {"version": "244"},
            {"version": "262"},
            {"version": "bad"},
            {"identity": False},
            {"userdb": False},
            {"nss": "passwd: files systemd\ngroup: files\n"},
            {"nss": "passwd: files systemd\ngroup: files systemd\ninitgroups: files\n"},
            {"nss": "passwd: files systemd\ngroup: files systemd\n" + "#" * 65536},
            {"iamroot": "1"},
        )
        for case in cases:
            with self.subTest(case=case):
                result = self.run_fixture(**case)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(result.stdout, "")

    def test_explicit_systemd_initgroups_is_reported(self):
        result = self.run_fixture(
            nss="passwd: files systemd\ngroup: files systemd\ninitgroups: files systemd\n"
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("CVE-2026-16742", result.stdout)


if __name__ == "__main__":
    unittest.main()
