import os
import shutil
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

    def _run_check(
        self,
        properties=None,
        timer_rows=None,
        disabled_rows="UNIT FILE STATE\n",
        restart_properties="",
        timeout_mode="run",
        script_mode=0o666,
        script_symlink=False,
        multi_restart=False,
    ):
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            bindir = tmp_path / "bin"
            bindir.mkdir()
            executable = tmp_path / "timer-command"
            executable.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
            executable.chmod(0o777)
            target_script = tmp_path / "backup.sh"
            target_script.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
            target_script.chmod(script_mode)
            if script_symlink:
                link = tmp_path / "backup-link.sh"
                link.symlink_to(target_script)
                target_script = link
            timer_file = tmp_path / "demo.timer"
            timer_file.write_text("[Timer]\nOnCalendar=daily\n", encoding="utf-8")
            calls = tmp_path / "calls"

            if timeout_mode != "missing":
                timeout = bindir / "timeout"
                timeout.write_text(
                    "#!/bin/sh\n"
                    "printf 'timeout %s\\n' \"$*\" >> \"$FAKE_CALLS\"\n"
                    "[ \"$FAKE_TIMEOUT_MODE\" = fail ] && exit 124\n"
                    "shift\n"
                    '"$@"\n',
                    encoding="utf-8",
                )
                timeout.chmod(0o755)
            else:
                for name in ("awk", "grep", "sed", "cut", "stat", "head"):
                    found = shutil.which(name)
                    self.assertIsNotNone(found)
                    (bindir / name).symlink_to(found)

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
                "      *.timer:Unit)\n"
                "        if [ \"$FAKE_MULTI_RESTART\" = yes ]; then\n"
                "          n=${2#timer}; n=${n%.timer}; printf 'Unit=demo%s.service\\n' \"$n\"\n"
                "        else printf 'Unit=demo.service\\n'; fi ;;\n"
                "      web_backup.service:User) printf '%s\\n' \"$FAKE_RESTART_PROPERTIES\" ;;\n"
                "      demo[0-9]*.service:User)\n"
                "        n=${2#demo}; n=${n%.service}\n"
                "        printf 'User=root\\nDynamicUser=no\\nExecStart={ path=/usr/bin/systemctl ; argv[]=/usr/bin/systemctl restart target%s.service ; }\\n' \"$n\" ;;\n"
                "      target[0-9]*.service:User) printf '%s\\n' \"$FAKE_RESTART_PROPERTIES\" ;;\n"
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
            restart_properties = restart_properties.replace("{script}", str(target_script))
            env = os.environ.copy()
            env.update(
                {
                    "FAKE_CALLS": str(calls),
                    "FAKE_TIMER_PATH": str(timer_file),
                    "FAKE_TIMER_ROWS": timer_rows,
                    "FAKE_DISABLED_ROWS": disabled_rows,
                    "FAKE_SERVICE_PROPERTIES": properties,
                    "FAKE_RESTART_PROPERTIES": restart_properties,
                    "FAKE_TIMEOUT_MODE": timeout_mode,
                    "FAKE_MULTI_RESTART": "yes" if multi_restart else "no",
                    "PATH": str(bindir) if timeout_mode == "missing" else f"{bindir}:{env['PATH']}",
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
                ["/bin/sh", "-c", body],
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

    def test_root_restart_chain_reports_only_fixed_writable_script(self):
        direct = (
            "User=\nDynamicUser=no\n"
            "ExecStart={ path=/usr/bin/systemctl ; argv[]=/usr/bin/systemctl "
            "restart web_backup.service ; ignore_errors=no ; }"
        )
        target = (
            "User=root\nDynamicUser=no\nRootDirectory=\nRootImage=\n"
            "ExecStart={ path=/bin/bash ; argv[]=/bin/bash {script} ; "
            "ignore_errors=no ; }"
        )
        result, calls, _ = self._run_check(
            properties=direct, restart_properties=target
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("WRITABLE_TIMER_SCRIPT: web_backup.service invokes", result.stdout)
        self.assertIn("(root review candidate)", result.stdout)
        self.assertEqual(
            sum(call.startswith("show web_backup.service ") for call in calls), 1
        )

    def test_restart_chain_rejects_nonroot_and_isolated_target(self):
        direct = (
            "User=root\nDynamicUser=no\n"
            "ExecStart={ path=/usr/bin/systemctl ; argv[]=/usr/bin/systemctl "
            "restart web_backup.service ; ignore_errors=no ; }"
        )
        target = (
            "User={user}\nDynamicUser={dynamic}\nRootDirectory={root_dir}\n"
            "RootImage={root_image}\n"
            "ExecStart={ path=/bin/bash ; argv[]=/bin/bash {script} ; }"
        )
        for user, dynamic, root_dir, root_image in (
            ("daemon", "no", "", ""),
            ("", "yes", "", ""),
            ("root", "no", "/isolated", ""),
            ("root", "no", "", "/image.raw"),
        ):
            with self.subTest(user=user, dynamic=dynamic, root_dir=root_dir):
                props = target.replace("{user}", user).replace("{dynamic}", dynamic)
                props = props.replace("{root_dir}", root_dir).replace(
                    "{root_image}", root_image
                )
                result, _, _ = self._run_check(
                    properties=direct, restart_properties=props
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("WRITABLE_TIMER_SCRIPT", result.stdout)

    def test_restart_chain_needs_complete_isolation_properties(self):
        direct = (
            "User=root\nDynamicUser=no\n"
            "ExecStart={ path=/usr/bin/systemctl ; argv[]=/usr/bin/systemctl "
            "restart web_backup.service ; }"
        )
        for missing in ("RootDirectory=", "RootImage="):
            with self.subTest(missing=missing):
                target = (
                    "User=root\nDynamicUser=no\nRootDirectory=\nRootImage=\n"
                    "ExecStart={ path=/bin/bash ; argv[]=/bin/bash {script} ; }"
                ).replace(missing + "\n", "")
                result, _, _ = self._run_check(
                    properties=direct, restart_properties=target
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("WRITABLE_TIMER_SCRIPT", result.stdout)

    def test_restart_chain_rejects_args_symlink_and_timeout(self):
        target = (
            "User=root\nDynamicUser=no\nRootDirectory=\nRootImage=\n"
            "ExecStart={ path=/bin/bash ; argv[]=/bin/bash {script} ; }"
        )
        for command in (
            "/usr/bin/systemctl --no-block restart web_backup.service",
            "/usr/bin/systemctl reload web_backup.service",
            "/usr/bin/systemctl restart web_backup.service extra",
            "/usr/bin/systemctl restart web_backup.service;id",
        ):
            with self.subTest(command=command):
                direct = (
                    "User=root\nDynamicUser=no\n"
                    f"ExecStart={{ path=/usr/bin/systemctl ; argv[]={command} ; }}"
                )
                result, calls, _ = self._run_check(
                    properties=direct, restart_properties=target
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("WRITABLE_TIMER_SCRIPT", result.stdout)
                self.assertFalse(
                    any(call.startswith("show web_backup.service ") for call in calls)
                )

        direct = (
            "User=root\nDynamicUser=no\n"
            "ExecStart={ path=/usr/bin/systemctl ; argv[]=/usr/bin/systemctl "
            "restart web_backup.service ; }"
        )
        for kwargs in ({"script_symlink": True}, {"timeout_mode": "fail"}):
            with self.subTest(kwargs=kwargs):
                result, _, _ = self._run_check(
                    properties=direct, restart_properties=target, **kwargs
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("WRITABLE_TIMER_SCRIPT", result.stdout)

    def test_restart_chain_requires_bounded_query_and_caps_distinct_targets(self):
        direct = (
            "User=root\nDynamicUser=no\n"
            "ExecStart={ path=/usr/bin/systemctl ; argv[]=/usr/bin/systemctl "
            "restart web_backup.service ; }"
        )
        target = (
            "User=root\nDynamicUser=no\nRootDirectory=\nRootImage=\n"
            "ExecStart={ path=/bin/bash ; argv[]=/bin/bash {script} ; }"
        )
        missing, missing_calls, _ = self._run_check(
            properties=direct, restart_properties=target, timeout_mode="missing"
        )
        self.assertEqual(missing.returncode, 0, missing.stderr)
        self.assertNotIn("WRITABLE_TIMER_SCRIPT", missing.stdout)
        self.assertFalse(any(call.startswith("show web_backup.service ") for call in missing_calls))

        rows = "NEXT LEFT LAST PASSED UNIT ACTIVATES\n" + "\n".join(
            f"- - - - timer{i}.timer demo{i}.service" for i in range(1, 6)
        )
        result, calls, _ = self._run_check(
            timer_rows=rows, restart_properties=target, multi_restart=True
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        targets = [call for call in calls if call.startswith("show target")]
        self.assertEqual(len(targets), 4)
        self.assertFalse(any(call.startswith("show target5.service ") for call in calls))

    @unittest.skipIf(os.geteuid() == 0, "root bypasses ordinary file-write mode")
    def test_restart_chain_rejects_unwritable_script(self):
        direct = (
            "User=root\nDynamicUser=no\n"
            "ExecStart={ path=/usr/bin/systemctl ; argv[]=/usr/bin/systemctl "
            "restart web_backup.service ; }"
        )
        target = (
            "User=root\nDynamicUser=no\nRootDirectory=\nRootImage=\n"
            "ExecStart={ path=/bin/bash ; argv[]=/bin/bash {script} ; }"
        )
        result, _, _ = self._run_check(
            properties=direct, restart_properties=target, script_mode=0o444
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("WRITABLE_TIMER_SCRIPT", result.stdout)


if __name__ == "__main__":
    unittest.main()
