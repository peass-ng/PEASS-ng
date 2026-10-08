import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SystemTimersTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.timer_script = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "4_procs_crons_timers_srvcs_sockets"
            / "9_System_timers.sh"
        )

    def _run_check(self, properties=None, timer_rows=None, disabled_rows="UNIT FILE STATE\n"):
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            bindir = tmp_path / "bin"
            bindir.mkdir()
            executable = tmp_path / "timer-command"
            executable.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
            executable.chmod(0o777)
            timer_file = tmp_path / "demo.timer"
            timer_file.write_text("[Timer]\nOnCalendar=daily\n", encoding="utf-8")
            calls = tmp_path / "calls"

            systemctl = bindir / "systemctl"
            systemctl.write_text(
                "#!/bin/sh\n"
                "printf '%s\\n' \"$*\" >> \"$FAKE_CALLS\"\n"
                "case \"$1\" in\n"
                "  list-timers) printf '%s\\n' \"$FAKE_TIMER_ROWS\" ;;\n"
                "  list-unit-files) printf '%s\\n' \"$FAKE_DISABLED_ROWS\" ;;\n"
                "  show)\n"
                "    case \"$2:$4\" in\n"
                "      *.timer:FragmentPath) printf 'FragmentPath=%s\\n' \"$FAKE_TIMER_PATH\" ;;\n"
                "      *.timer:Unit) printf 'Unit=demo.service\\n' ;;\n"
                "      *) printf '%s\\n' \"$FAKE_SERVICE_PROPERTIES\" ;;\n"
                "    esac ;;\n"
                "esac\n",
                encoding="utf-8",
            )
            systemctl.chmod(0o755)

            if timer_rows is None:
                timer_rows = (
                    "NEXT LEFT LAST PASSED UNIT ACTIVATES\n"
                    "Thu 2026-10-08 23:00:00 CEST 1h left Wed 2026-10-07 23:00:00 CEST "
                    "1d ago demo.timer demo.service\n"
                    "1 timers listed."
                )
            if properties is None:
                properties = (
                    "User=\nDynamicUser=no\n"
                    f"ExecStart={{ path={executable} ; argv[]={executable} ; ignore_errors=no ; }}"
                )
            env = os.environ.copy()
            env.update(
                {
                    "FAKE_CALLS": str(calls),
                    "FAKE_TIMER_PATH": str(timer_file),
                    "FAKE_TIMER_ROWS": timer_rows,
                    "FAKE_DISABLED_ROWS": disabled_rows,
                    "FAKE_SERVICE_PROPERTIES": properties,
                    "PATH": f"{bindir}:{env['PATH']}",
                }
            )
            body = "\n".join(
                [
                    "SEARCH_IN_FOLDER=",
                    "PSTORAGE_TIMER=",
                    "E=E",
                    "timersG='demo[.]timer'",
                    "SED_GREEN='&'",
                    "print_2title() { :; }",
                    "print_3title() { :; }",
                    "print_info() { :; }",
                    "echo_not_found() { :; }",
                    f". {shlex.quote(str(self.timer_script))}",
                ]
            )
            result = subprocess.run(
                ["sh", "-c", body],
                cwd=self.repo_root,
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )
            return result, calls.read_text(encoding="utf-8").splitlines(), str(executable)

    def test_active_row_uses_timer_unit_and_structured_executable(self):
        result, calls, executable = self._run_check()

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("NEXT LEFT LAST PASSED UNIT ACTIVATES", result.stdout)
        self.assertIn("demo.timer demo.service", result.stdout)
        self.assertIn("Potential privilege escalation in timer: demo.timer", result.stdout)
        self.assertIn("RUNS_AS_ROOT: Service runs as root", result.stdout)
        self.assertIn(f"WRITABLE_EXEC: Executable is writable: {executable}", result.stdout)
        self.assertIn("show demo.timer -p Unit", calls)
        self.assertEqual(sum(call.startswith("show demo.service ") for call in calls), 1)
        self.assertFalse(any(call.startswith("show Thu ") for call in calls))

    def test_non_root_and_dynamic_users_are_not_reported_as_root(self):
        for properties in (
            "User=daemon\nDynamicUser=no\nExecStart=/bin/true",
            "User=\nDynamicUser=yes\nExecStart=/bin/true",
            "ExecStart=/bin/true",
        ):
            with self.subTest(properties=properties):
                result, _, _ = self._run_check(properties=properties)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("RUNS_AS_ROOT", result.stdout)

    def test_plain_relative_exec_and_disabled_timer_are_checked(self):
        properties = "User=root\nDynamicUser=no\nExecStart=relative-command --flag"
        result, calls, _ = self._run_check(
            properties=properties,
            disabled_rows="UNIT FILE STATE\ndisabled.timer disabled\n1 unit files listed.",
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("RELATIVE_PATH: Uses relative path: relative-command", result.stdout)
        self.assertIn("show disabled.timer -p FragmentPath", calls)
        self.assertFalse(any(call.startswith("show 1 ") for call in calls))

    def test_large_timer_list_keeps_display_and_bounds_inspection(self):
        rows = "NEXT LEFT LAST PASSED UNIT ACTIVATES\n" + "\n".join(
            f"- - - - timer{i}.timer demo.service" for i in range(201)
        )
        result, calls, _ = self._run_check(timer_rows=rows)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("timer200.timer demo.service", result.stdout)
        self.assertEqual(sum(call.endswith("-p Unit") for call in calls), 200)
        self.assertFalse(any(call.startswith("show timer200.timer ") for call in calls))


if __name__ == "__main__":
    unittest.main()
