"""Focused, passive MySQL/MariaDB service identity checks."""

import os
import pathlib
import subprocess
import tempfile
import unittest


MODULE = pathlib.Path(__file__).resolve().parents[1] / "builder/linpeas_parts/7_software_information/Mysql.sh"
MARKER = "### Review the MySQL/MariaDB service identity without assuming a version is exploitable. ###"


class MysqlServiceIdentityTests(unittest.TestCase):
    def setUp(self):
        # macOS /var is a symlink to /private/var; use a non-reparse home path.
        self.temp = tempfile.TemporaryDirectory(dir=pathlib.Path.home())
        self.addCleanup(self.temp.cleanup)
        self.root = pathlib.Path(self.temp.name)
        self.bin = self.root / "bin"
        self.bin.mkdir()
        (self.bin / "ps").write_text('#!/bin/sh\nprintf "%s\\n" "$PS_OUTPUT"\n')
        (self.bin / "mysqld").write_text('#!/bin/sh\necho "mysqld Ver 10.3.34-MariaDB"\n')
        for name in ("ps", "mysqld"):
            (self.bin / name).chmod(0o755)
        self.marker = self.root / "run/systemd/system"
        self.marker.mkdir(parents=True)
        self.units = self.root / "etc/systemd/system"
        self.units.mkdir(parents=True)
        self.script = MODULE.read_text().split(MARKER, 1)[1]
        for original, sentinel in (
            ("/run/systemd/system", "@RUN_UNIT_DIR@"),
            ("/etc/systemd/system", "@ETC_UNIT_DIR@"),
            ("/usr/lib/systemd/system", "@USR_UNIT_DIR@"),
            ("/lib/systemd/system", "@LIB_UNIT_DIR@"),
        ):
            self.script = self.script.replace(original, sentinel)
        for sentinel, replacement in (
            ("@RUN_UNIT_DIR@", self.marker),
            ("@ETC_UNIT_DIR@", self.units),
            ("@USR_UNIT_DIR@", self.root / "usr/lib/systemd/system"),
            ("@LIB_UNIT_DIR@", self.root / "lib/systemd/system"),
        ):
            self.script = self.script.replace(sentinel, str(replacement))

    def run_check(self, ps_output=""):
        env = os.environ.copy()
        env.update(PATH=f"{self.bin}:{env['PATH']}", PS_OUTPUT=ps_output, E="e", SED_RED="&", SED_GREEN="&")
        result = subprocess.run(["sh", "-c", self.script], env=env, capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def write_unit(self, contents, name="mysql-start.service"):
        path = self.units / name
        path.write_text(contents)
        return path

    def test_visible_root_mariadb_version_independent(self):
        output = self.run_check("root 123 0.0 0.0 0 0 ? S 00:00 0:00 /usr/sbin/mariadbd")
        self.assertIn("process runs as root", output)
        self.assertIn("10.3.34", output)

    def test_visible_nonroot_not_a_root_candidate(self):
        output = self.run_check("mysql 123 0.0 0.0 0 0 ? S 00:00 0:00 /usr/sbin/mysqld")
        self.assertIn("runs as user 'mysql'", output)
        self.assertNotIn("process runs as root", output)

    def test_hidden_process_uses_conditional_unit_context(self):
        self.write_unit("[Unit]\nDescription=DB\n[Service]\nUser=root\nExecStart=/usr/sbin/mysqld\n")
        output = self.run_check()
        self.assertIn("process not visible", output)
        self.assertIn("unit root-context review candidate", output)
        self.assertIn("process state, drop-ins", output)

    def test_nonroot_dynamic_and_unrelated_units_are_not_candidates(self):
        self.write_unit("[Service]\nUser=mysql\nExecStart=/usr/sbin/mysqld\n", "mysql-user.service")
        self.write_unit("[Service]\nDynamicUser=yes\nExecStart=/usr/sbin/mariadbd\n", "mariadb-dynamic.service")
        self.write_unit("[Service]\nUser=root\nExecStart=/bin/false\n", "mysql-unrelated.service")
        self.write_unit("[Service]\nUser=root\nExecStart=/usr/sbin/mysqld --user=mysql\n", "mysql-drops-user.service")
        self.write_unit("[Service]\nExecStart=/usr/sbin/mysqld\nExecStart=\nExecStart=/bin/true\n", "mysql-reset.service")
        self.assertNotIn("unit root-context review candidate", self.run_check())

    def test_symlink_unit_and_symlink_ancestor_are_skipped(self):
        real = self.root / "real.service"
        real.write_text("[Service]\nUser=root\nExecStart=/usr/sbin/mysqld\n")
        (self.units / "mysql-link.service").symlink_to(real)
        self.assertNotIn("unit root-context review candidate", self.run_check())
        self.write_unit(real.read_text())
        moved = self.root / "real-systemd"
        self.units.rename(moved)
        self.units.symlink_to(moved, target_is_directory=True)
        self.assertNotIn("unit root-context review candidate", self.run_check())

    def test_missing_systemd_marker_and_oversize_unit_are_skipped(self):
        self.write_unit("[Service]\nExecStart=/usr/sbin/mysqld\n" + "#" * 65536)
        self.assertNotIn("unit root-context review candidate", self.run_check())
        self.write_unit("[Service]\nExecStart=/usr/sbin/mysqld\n", "mariadb-small.service")
        self.marker.rmdir()
        self.assertNotIn("unit root-context review candidate", self.run_check())

    def test_unit_candidate_budget(self):
        for index in range(13):
            self.write_unit("[Service]\nUser=mysql\nExecStart=/usr/sbin/mysqld\n", f"mysql-{index:02d}.service")
        self.assertIn("unit review incomplete (12-file limit)", self.run_check())


if __name__ == "__main__":
    unittest.main()
