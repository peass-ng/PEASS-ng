import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class MacOSUpdaterTOCTOUTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.module_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "4_procs_crons_timers_srvcs_sockets"
            / "18_Macos_updater_toctou.sh"
        )

    @staticmethod
    def _write_command(directory, name, content):
        path = directory / name
        path.write_text(content, encoding="utf-8")
        path.chmod(0o755)

    def _run_module(
        self,
        base,
        *,
        plist_data="",
        plist_user="root",
        plist_mode=0o444,
        ps_output="",
        stat_mode="drwx------",
        macpeas=True,
        iamroot=False,
        search_in_folder=False,
    ):
        root = base / "root"
        launchdaemons = root / "Library" / "LaunchDaemons"
        launchdaemons.mkdir(parents=True)
        if plist_data:
            plist = launchdaemons / "com.example.updater.plist"
            plist.write_text("fixture", encoding="utf-8")
            plist.chmod(plist_mode)

        bindir = base / "bin"
        bindir.mkdir()
        self._write_command(
            bindir,
            "defaults",
            """#!/bin/sh
if [ "${3-}" = "UserName" ]; then
  printf '%s\\n' "$FAKE_PLIST_USER"
else
  printf '%s\\n' "$FAKE_PLIST_DATA"
fi
""",
        )
        self._write_command(
            bindir,
            "stat",
            """#!/bin/sh
# Fixture plists represent root-owned launchd configuration. Payload ownership
# stays root unless writability itself demonstrates current-user control.
if [ "${2-}" = "%Sp" ]; then
  printf '%s\\n' "$FAKE_STAT_MODE"
else
  printf '0\\n'
fi
""",
        )
        self._write_command(
            bindir,
            "ps",
            """#!/bin/sh
printf '%s\\n' "$FAKE_PS_OUTPUT"
""",
        )

        env = os.environ.copy()
        env.update(
            {
                "FAKE_PLIST_DATA": plist_data,
                "FAKE_PLIST_USER": plist_user,
                "FAKE_PS_OUTPUT": ps_output,
                "FAKE_STAT_MODE": stat_mode,
                "PATH": f"{bindir}:/usr/bin:/bin",
            }
        )
        body = "\n".join(
            [
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                f"MACPEAS={'1' if macpeas else ''}",
                f"IAMROOT={'1' if iamroot else ''}",
                f"SEARCH_IN_FOLDER={'1' if search_in_folder else ''}",
                "E=E",
                "SED_RED_YELLOW='&'",
                'print_3title() { printf "TITLE: %s\\n" "$1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.module_file))}",
            ]
        )
        return subprocess.run(
            ["sh", "-c", body],
            cwd=str(self.repo_root),
            env=env,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_root_launchdaemon_writable_update_path_is_reported_with_spaces(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            cache = base / "Electron Cache"
            cache.mkdir()
            package = cache / "pending update.pkg"
            plist_data = f'''{{
    "Label" => "com.example.updater"
    "ProgramArguments" => (
        "/Applications/Example Updater.app/Contents/MacOS/updater",
        "--cache",
        "{package}"
    )
}}'''
            result = self._run_module(base, plist_data=plist_data)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: Potential privileged updater TOCTOU paths", result.stdout)
        self.assertIn(str(package), result.stdout)
        self.assertIn("root LaunchDaemon metadata", result.stdout)

    def test_non_root_launchdaemon_is_ignored(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            cache = base / "update-cache"
            cache.mkdir()
            result = self._run_module(
                base,
                plist_data=f'"Program" => "/usr/bin/updater"\n"Cache" => "{cache}"',
                plist_user="nobody",
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("Potential privileged updater TOCTOU paths", result.stdout)

    def test_writable_launchdaemon_plist_is_left_to_stronger_existing_check(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            cache = base / "update-cache"
            cache.mkdir()
            result = self._run_module(
                base,
                plist_data=f'"Program" => "/usr/bin/updater"\n"Cache" => "{cache}"',
                plist_mode=0o600,
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("Potential privileged updater TOCTOU paths", result.stdout)

    def test_root_updater_process_writable_path_is_reported(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            cache = base / "update-cache"
            cache.mkdir()
            package = cache / "update.pkg"
            result = self._run_module(
                base,
                ps_output=f"root 4242 /usr/local/bin/electron-updater --package {package}",
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("PID 4242", result.stdout)
        self.assertIn(str(package), result.stdout)
        self.assertIn("root updater command line", result.stdout)

    def test_sticky_directory_alone_is_not_reported(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            cache = base / "update-cache"
            cache.mkdir()
            package = cache / "not-created.pkg"
            result = self._run_module(
                base,
                ps_output=f"root 4242 /usr/bin/updater --package {package}",
                stat_mode="drwxrwxrwt",
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("Potential privileged updater TOCTOU paths", result.stdout)

    def test_writable_payload_is_reported_even_when_parent_is_sticky(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            cache = base / "update-cache"
            cache.mkdir()
            package = cache / "existing-update.pkg"
            package.write_text("fixture", encoding="utf-8")
            result = self._run_module(
                base,
                ps_output=f"root 4242 /usr/bin/updater --package {package}",
                stat_mode="drwxrwxrwt",
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Potential privileged updater TOCTOU paths", result.stdout)
        self.assertIn(str(package), result.stdout)

    def test_unprivileged_updater_process_is_ignored(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            cache = base / "update-cache"
            cache.mkdir()
            result = self._run_module(
                base,
                ps_output=f"alice 4242 /usr/local/bin/electron-updater --cache {cache}",
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("Potential privileged updater TOCTOU paths", result.stdout)

    def test_non_macos_root_and_search_modes_do_not_run_detector(self):
        cases = (
            (False, False, False),
            (True, True, False),
            (True, False, True),
        )
        for macpeas, iamroot, search_in_folder in cases:
            with (
                self.subTest(
                    macpeas=macpeas,
                    iamroot=iamroot,
                    search_in_folder=search_in_folder,
                ),
                tempfile.TemporaryDirectory() as tmpdir,
            ):
                base = Path(tmpdir)
                cache = base / "update-cache"
                cache.mkdir()
                result = self._run_module(
                    base,
                    ps_output=f"root 4242 /usr/bin/updater --cache {cache}",
                    macpeas=macpeas,
                    iamroot=iamroot,
                    search_in_folder=search_in_folder,
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual("", result.stdout)


if __name__ == "__main__":
    unittest.main()
