import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SystemdEnvironmentFilesTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.script = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "4_procs_crons_timers_srvcs_sockets"
            / "11_Systemd.sh"
        )

    def _run_helper(self, unit_file):
        body = "\n".join(
            [
                "SEARCH_IN_FOLDER=1",
                f". {shlex.quote(str(self.script))}",
                f"systemd_envfile_findings_for_unit {shlex.quote(str(unit_file))}",
            ]
        )
        return subprocess.run(["sh", "-c", body], capture_output=True, text=True)

    def test_oversized_unit_is_not_scanned(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            env_file = root / "secret.env"
            env_file.write_text("API_TOKEN=never-print-this-value\n")
            unit_file = root / "oversized.service"
            unit_file.write_text(f"[Service]\nEnvironmentFile={env_file}\n" + "#" * 65536)
            result = self._run_helper(unit_file)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")

    def test_quoted_optional_path_reports_keys_without_values(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            env_file = root / "service secrets.env"
            env_file.write_text(
                "# PASSWORD=comment-only\n"
                "APP_SECRET_KEY=never-print-this-value\n"
                "API_TOKEN=another-value\n"
                "EMPTY_PASSWORD=\n"
                "PLAIN_SETTING=ok\n",
                encoding="utf-8",
            )
            unit_file = root / "demo.service"
            unit_file.write_text(
                "[Unit]\nEnvironmentFile=/not/a/service/file\n"
                "[Service]\n"
                f'EnvironmentFile=-"{env_file}"\n',
                encoding="utf-8",
            )

            result = self._run_helper(unit_file)

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(
                f"{env_file}: APP_SECRET_KEY,API_TOKEN\n", result.stdout
            )
            self.assertNotIn("never-print-this-value", result.stdout)
            self.assertNotIn("another-value", result.stdout)

    def test_optional_prefix_inside_quotes_is_supported(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            env_file = root / "config.env"
            env_file.write_text("LOGIN_TOKEN=hidden\n", encoding="utf-8")
            unit_file = root / "demo.service"
            unit_file.write_text(
                f'[Service]\nEnvironmentFile="-{env_file}"\n', encoding="utf-8"
            )

            result = self._run_helper(unit_file)

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(f"{env_file}: LOGIN_TOKEN\n", result.stdout)

    def test_limit_of_four_files_per_unit(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            unit_file = root / "demo.service"
            files = []
            for index in range(5):
                env_file = root / f"config{index}.env"
                env_file.write_text(f"ITEM_SECRET={index}\n", encoding="utf-8")
                files.append(env_file)
            unit_file.write_text(
                "[Service]\n"
                + "".join(f"EnvironmentFile={path}\n" for path in files),
                encoding="utf-8",
            )

            result = self._run_helper(unit_file)

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(4, result.stdout.count("ITEM_SECRET"))
            self.assertNotIn(str(files[4]), result.stdout)

    def test_nonliteral_oversized_and_nonregular_files_are_skipped(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            too_large = root / "large.env"
            too_large.write_text("LONG_SECRET=" + "x" * 65537, encoding="utf-8")
            with_space = root / "has space.env"
            with_space.write_text("SPACE_TOKEN=value\n", encoding="utf-8")
            unit_file = root / "demo.service"
            unit_file.write_text(
                "[Service]\n"
                f"EnvironmentFile={root / '*.env'}\n"
                f"EnvironmentFile={with_space}\n"
                f"EnvironmentFile={root}\n"
                f"EnvironmentFile={too_large}\n",
                encoding="utf-8",
            )

            result = self._run_helper(unit_file)

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual("", result.stdout)
            self.assertNotIn(str(too_large), result.stdout)

    def test_empty_directive_resets_earlier_paths(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            stale = root / "stale.env"
            stale.write_text("STAGING_SECRET=stale\n", encoding="utf-8")
            current = root / "current.env"
            current.write_text("CURRENT_TOKEN=current\n", encoding="utf-8")
            unit_file = root / "demo.service"
            unit_file.write_text(
                "[Service]\n"
                f"EnvironmentFile={stale}\n"
                "EnvironmentFile=\n"
                f"EnvironmentFile='{current}'\n",
                encoding="utf-8",
            )

            result = self._run_helper(unit_file)

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(f"{current}: CURRENT_TOKEN\n", result.stdout)

    @unittest.skipIf(os.geteuid() == 0, "root can read mode 000 files")
    def test_unreadable_file_is_skipped(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            env_file = root / "private.env"
            env_file.write_text("PRIVATE_SECRET=value\n", encoding="utf-8")
            env_file.chmod(0o000)
            unit_file = root / "demo.service"
            unit_file.write_text(
                f"[Service]\nEnvironmentFile={env_file}\n", encoding="utf-8"
            )

            result = self._run_helper(unit_file)

            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual("", result.stdout)

    def test_active_services_are_listed_once_without_restricting_older_checks(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            bindir = root / "bin"
            bindir.mkdir()
            calls = root / "calls"
            systemctl = bindir / "systemctl"
            systemctl.write_text(
                "#!/bin/sh\n"
                "printf '%s\\n' \"$1\" >> \"$FAKE_CALLS\"\n"
                "case \"$1\" in\n"
                "  list-units) i=0; while [ \"$i\" -le 200 ]; do "
                "printf 'demo%s.service loaded active running Test\\n' \"$i\"; "
                "i=$((i+1)); done ;;\n"
                "  show) echo 'CapabilityBoundingSet=' ;;\n"
                "esac\n",
                encoding="utf-8",
            )
            systemctl.chmod(0o755)
            env = os.environ.copy()
            env.update({"PATH": f"{bindir}:{env['PATH']}", "FAKE_CALLS": str(calls)})
            body = "\n".join(
                [
                    "SEARCH_IN_FOLDER=",
                    "NC=",
                    "E=E",
                    "Wfolders='^$'",
                    "SED_RED='&'",
                    "SED_RED_YELLOW='&'",
                    'print_2title() { :; }',
                    'print_list() { :; }',
                    'print_info() { :; }',
                    'echo_not_found() { :; }',
                    f". {shlex.quote(str(self.script))}",
                ]
            )
            result = subprocess.run(
                ["sh", "-c", body], env=env, capture_output=True, text=True
            )

            self.assertEqual(result.returncode, 0, result.stderr)
            seen_calls = calls.read_text(encoding="utf-8").splitlines()
            self.assertEqual(1, seen_calls.count("list-units"))
            # The writable-environment check also reads the effective user
            # for each of the 200 bounded active services.
            self.assertEqual(401, seen_calls.count("show"))

    def test_writable_envfile_in_active_root_unit_and_dropin_is_reported(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            bindir = root / "bin"
            bindir.mkdir()
            env_file = root / "service env"
            env_file.write_text("LD_PRELOAD=do-not-print-this-value\n", encoding="utf-8")
            env_file.chmod(0o666)
            unit = root / "demo.service"
            unit.write_text("[Service]\nEnvironmentFile=/stale.env\n", encoding="utf-8")
            dropin = root / "override.conf"
            dropin.write_text(
                f'[Service]\nEnvironmentFile=\nEnvironmentFile="{env_file}"\n',
                encoding="utf-8",
            )
            systemctl = bindir / "systemctl"
            systemctl.write_text(
                "#!/bin/sh\n"
                "case \"$1\" in\n"
                "  show) printf 'User=%s\\nDynamicUser=%s\\nRootDirectory=\\nRootImage=\\n' "
                '"$FAKE_USER" "$FAKE_DYNAMIC" ;;\n'
                '  cat) cat "$FAKE_UNIT" "$FAKE_DROPIN" ;;\n'
                "esac\n",
                encoding="utf-8",
            )
            systemctl.chmod(0o755)
            env = os.environ.copy()
            env.update(
                {
                    "PATH": f"{bindir}:{env['PATH']}",
                    "FAKE_UNIT": str(unit),
                    "FAKE_DROPIN": str(dropin),
                    "FAKE_USER": "",
                    "FAKE_DYNAMIC": "no",
                }
            )
            body = "\n".join(
                [
                    "SEARCH_IN_FOLDER=1",
                    f". {shlex.quote(str(self.script))}",
                    "systemd_wenv_findings_for_unit demo.service",
                ]
            )
            result = subprocess.run(
                ["sh", "-c", body], env=env, capture_output=True, text=True
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(f"demo.service: {env_file}\n", result.stdout)
            self.assertNotIn("do-not-print-this-value", result.stdout)
            self.assertNotIn("/stale.env", result.stdout)

            env["FAKE_USER"] = "daemon"
            nonroot = subprocess.run(
                ["sh", "-c", body], env=env, capture_output=True, text=True
            )
            self.assertEqual("", nonroot.stdout)
            env["FAKE_USER"] = ""
            env["FAKE_DYNAMIC"] = "yes"
            dynamic = subprocess.run(
                ["sh", "-c", body], env=env, capture_output=True, text=True
            )
            self.assertEqual("", dynamic.stdout)

    def test_writable_envfile_uses_first_property_values(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            bindir = root / "bin"
            bindir.mkdir()
            env_file = root / "config.env"
            env_file.write_text("TOKEN=not-printed\n", encoding="utf-8")
            systemctl = bindir / "systemctl"
            systemctl.write_text(
                "#!/bin/sh\n"
                "case \"$1\" in\n"
                "  show) printf '%s\\n' \"$FAKE_PROPERTIES\" ;;\n"
                "  cat) printf '[Service]\\nEnvironmentFile=%s\\n' \"$FAKE_ENV_FILE\" ;;\n"
                "esac\n",
                encoding="utf-8",
            )
            systemctl.chmod(0o755)
            env = os.environ.copy()
            env.update(
                {"PATH": f"{bindir}:{env['PATH']}", "FAKE_ENV_FILE": str(env_file)}
            )
            body = "\n".join(
                [
                    "SEARCH_IN_FOLDER=1",
                    f". {shlex.quote(str(self.script))}",
                    "systemd_wenv_findings_for_unit demo.service",
                ]
            )
            cases = [
                ("User=daemon\nUser=root", ""),
                ("User=\nUser=daemon", f"demo.service: {env_file}\n"),
                ("User=root\nDynamicUser=yes\nDynamicUser=no", ""),
                ("User=root\nRootDirectory=/srv/jail\nRootDirectory=", ""),
                (
                    "User=root\nRootDirectory=\nRootDirectory=/srv/jail",
                    f"demo.service: {env_file}\n",
                ),
                ("User=root\nRootImage=/srv/root.img\nRootImage=", ""),
                (
                    "User=root\nRootImage=\nRootImage=/srv/root.img",
                    f"demo.service: {env_file}\n",
                ),
                (
                    "User=root\nDynamicUser=no\nRootDirectory=\nRootImage=",
                    f"demo.service: {env_file}\n",
                ),
            ]
            for properties, expected in cases:
                with self.subTest(properties=properties):
                    env["FAKE_PROPERTIES"] = properties
                    result = subprocess.run(
                        ["sh", "-c", body], env=env, capture_output=True, text=True
                    )
                    self.assertEqual(result.returncode, 0, result.stderr)
                    self.assertEqual(result.stdout, expected)

    def test_new_indicator_only_checks_first_200_units_including_bulleted_rows(self):
        body = "\n".join(
            [
                "SEARCH_IN_FOLDER=1",
                f". {shlex.quote(str(self.script))}",
                "systemd_envfile_active_units",
            ]
        )
        rows = (
            "UNIT LOAD ACTIVE SUB DESCRIPTION\n"
            "● marked.service loaded active running Marked\n"
            + "\n".join(
                f"demo{index}.service loaded active running Demo" for index in range(201)
            )
            + "\n"
        )
        result = subprocess.run(
            ["sh", "-c", body], input=rows, capture_output=True, text=True
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        units = result.stdout.splitlines()
        self.assertEqual(200, len(units))
        self.assertEqual("marked.service", units[0])
        self.assertEqual("demo198.service", units[-1])
        self.assertNotIn("demo199.service", units)


if __name__ == "__main__":
    unittest.main()
