import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SystemdWritableExecPathsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkSystemdWritableExecPaths.sh"
        )

    def _run_check(self, service_user="", iamroot=""):
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            bindir = tmp_path / "bin"
            writable_dir = tmp_path / "deploy"
            bindir.mkdir()
            writable_dir.mkdir()
            writable_dir.chmod(0o777)

            marker = tmp_path / "executed"
            executable = writable_dir / "root-service"
            executable.write_text(
                f'#!/bin/sh\ntouch {shlex.quote(str(marker))}\n', encoding="utf-8"
            )
            executable.chmod(0o755)

            systemctl = bindir / "systemctl"
            systemctl.write_text(
                "#!/bin/sh\n"
                "case \"$1\" in\n"
                "  list-units) echo 'demo.service loaded active running Demo' ;;\n"
                "  show)\n"
                "    printf 'User=%s\\n' \"$FAKE_SERVICE_USER\"\n"
                "    echo 'DynamicUser=no'\n"
                "    echo 'RootImage='\n"
                "    echo 'RootDirectory='\n"
                "    printf 'ExecStart={ path=%s ; argv[]=%s ; }\\n' \"$FAKE_EXECUTABLE\" \"$FAKE_EXECUTABLE\"\n"
                "    ;;\n"
                "esac\n",
                encoding="utf-8",
            )
            systemctl.chmod(0o755)

            env = os.environ.copy()
            env.update(
                {
                    "FAKE_EXECUTABLE": str(executable),
                    "FAKE_SERVICE_USER": service_user,
                    "PATH": f"{bindir}:{env['PATH']}",
                }
            )
            body = "\n".join(
                [
                    f"IAMROOT={shlex.quote(iamroot)}",
                    "E=E",
                    "SED_RED_YELLOW='&'",
                    'print_3title() { echo "TITLE: $1"; }',
                    "print_info() { :; }",
                    f". {shlex.quote(str(self.function_file))}",
                    "checkSystemdWritableExecPaths",
                ]
            )
            result = subprocess.run(
                ["sh", "-c", body],
                cwd=str(self.repo_root),
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )
            return result, marker.exists(), str(executable), str(writable_dir)

    def test_writable_parent_of_root_service_executable_is_reported_passively(self):
        result, executed, executable, writable_dir = self._run_check()

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse(executed, "the configured service executable must never run")
        self.assertIn("TITLE: Root systemd service executables replaceable", result.stdout)
        self.assertIn(f"demo.service: {executable}", result.stdout)
        self.assertIn(f"writable parent: {writable_dir}", result.stdout)

    def test_non_root_service_is_suppressed(self):
        result, executed, _, _ = self._run_check(service_user="daemon")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse(executed)
        self.assertEqual("", result.stdout)

    def test_check_is_suppressed_when_linpeas_runs_as_root(self):
        result, executed, _, _ = self._run_check(iamroot="1")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse(executed)
        self.assertEqual("", result.stdout)


if __name__ == "__main__":
    unittest.main()
