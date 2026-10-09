import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SystemdWritableDropinsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkSystemdWritableDropins.sh"
        )

    def _run_check(self, properties=None, directory_count=1, iamroot="", timeout_available=True,
                   writable_unit_dir=False, runtime_marker=True, symlink_unit_dir=False,
                   write_search_only=False):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            system_dir = root / "system"
            actual_system_dir = root / "system_real" if symlink_unit_dir else system_dir
            runtime_dir = root / "runtime"
            bindir = root / "bin"
            actual_system_dir.mkdir()
            if symlink_unit_dir:
                system_dir.symlink_to(actual_system_dir)
            if runtime_marker:
                runtime_dir.mkdir()
            bindir.mkdir()
            for number in range(directory_count):
                (actual_system_dir / f"demo{number:02d}.service.d").mkdir(mode=0o700)
            actual_system_dir.chmod(0o300 if write_search_only else
                                    (0o700 if writable_unit_dir else 0o500))

            # These stubs keep the test independent of the host init system.
            for name, content in {
                "uname": "#!/bin/sh\necho Linux\n",
                "id": "#!/bin/sh\n[ \"$1\" = -u ] && echo 1000\n",
                "sed": "#!/bin/sh\nwhile IFS= read -r line; do printf '%s\\n' \"$line\"; done\n",
                "systemctl": (
                    "#!/bin/sh\n"
                    "[ \"$1\" = show ] || exit 99\n"
                    "printf '%s\\n' \"$*\" >> \"$FAKE_CALL_LOG\"\n"
                    "printf '%s\\n' \"$FAKE_PROPERTIES\"\n"
                ),
            }.items():
                path = bindir / name
                path.write_text(content, encoding="utf-8")
                path.chmod(0o755)

            log = root / "calls"
            env = os.environ.copy()
            env.update(
                {
                    "PATH": f"{bindir}:{env['PATH']}" if timeout_available else str(bindir),
                    "FAKE_CALL_LOG": str(log),
                    "FAKE_PROPERTIES": properties
                    if properties is not None
                    else "LoadState=loaded\nActiveState=active\nUser=\nDynamicUser=no\n"
                    "RootDirectory=\nRootImage=\nPrivateUsers=no",
                }
            )
            script = "\n".join(
                [
                    f"IAMROOT={shlex.quote(iamroot)}",
                    "E=E",
                    "SED_RED_YELLOW='&'",
                    'print_3title() { echo "TITLE: $1"; }',
                    "print_info() { :; }",
                    f". {shlex.quote(str(self.function_file))}",
                    f"checkSystemdWritableDropins {shlex.quote(str(system_dir))} "
                    f"{shlex.quote(str(runtime_dir))}",
                ]
            )
            try:
                result = subprocess.run(
                    ["/bin/sh", "-c", script],
                    cwd=str(self.repo_root),
                    env=env,
                    capture_output=True,
                    text=True,
                    check=False,
                )
            finally:
                actual_system_dir.chmod(0o700)
            return result, log.read_text(encoding="utf-8").splitlines() if log.exists() else [], str(system_dir)

    def test_empty_writable_dropin_for_active_root_service_is_reported(self):
        result, calls, system_dir = self._run_check()

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: Writable systemd drop-in directories", result.stdout)
        self.assertIn(f"demo00.service: {system_dir}/demo00.service.d", result.stdout)
        self.assertEqual(1, len(calls))
        self.assertIn("demo00.service", calls[0])

    def test_writable_parent_unit_directory_needs_no_unit_scan(self):
        result, calls, system_dir = self._run_check(
            directory_count=0, write_search_only=True, timeout_available=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: Writable systemd system unit directory", result.stdout)
        self.assertIn(system_dir + " is writable and searchable", result.stdout)
        self.assertIn("verify effective sudo start/restart policy", result.stdout)
        self.assertEqual([], calls)

    def test_missing_runtime_marker_preserves_dropin_checks(self):
        result, calls, system_dir = self._run_check(
            writable_unit_dir=True, runtime_marker=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("TITLE: Writable systemd system unit directory", result.stdout)
        self.assertIn(f"demo00.service: {system_dir}/demo00.service.d", result.stdout)
        self.assertEqual(1, len(calls))

    def test_symlinked_parent_still_checks_existing_dropins(self):
        result, calls, system_dir = self._run_check(
            writable_unit_dir=True, symlink_unit_dir=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("TITLE: Writable systemd system unit directory", result.stdout)
        self.assertIn(f"demo00.service: {system_dir}/demo00.service.d", result.stdout)
        self.assertEqual(1, len(calls))

    def test_missing_timeout_does_not_query_systemctl(self):
        result, calls, _ = self._run_check(timeout_available=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")
        self.assertEqual(calls, [])

    def test_non_root_service_is_suppressed(self):
        properties = "LoadState=loaded\nActiveState=active\nUser=daemon\nDynamicUser=no"
        result, calls, _ = self._run_check(properties=properties)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)
        self.assertEqual(1, len(calls))

    def test_no_candidate_makes_no_systemctl_call(self):
        result, calls, _ = self._run_check(directory_count=0)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)
        self.assertEqual([], calls)

    def test_systemctl_calls_are_capped(self):
        result, calls, _ = self._run_check(directory_count=25)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(20, len(calls))
        self.assertEqual(20, result.stdout.count("(a new .conf could run"))

    def test_root_invocation_skips_check(self):
        result, calls, _ = self._run_check(iamroot="1")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)
        self.assertEqual([], calls)


if __name__ == "__main__":
    unittest.main()
