"""Excluded home descendants do not hide eligible writable paths elsewhere."""

import os
import pwd
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULES = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/8_interesting_perms_files"


class WritableScanPruningTests(unittest.TestCase):
    def test_owner_and_group_scans_preserve_visible_paths(self):
        with tempfile.TemporaryDirectory(prefix="peas-writable-") as temporary:
            root = Path(temporary) / "root"
            home = root / "home"
            outside = root / "opt"
            home.mkdir(parents=True)
            outside.mkdir()
            home.chmod(0o770)
            hidden = home / "excluded.txt"
            hidden.write_text("not a result")
            hidden.chmod(0o666)
            visible = outside / "visible.txt"
            visible.write_text("result")
            visible.chmod(0o666)
            for index in range(1000):
                (home / f"excluded-{index:04d}").touch()

            environment = os.environ.copy()
            environment.update(
                ROOT_FOLDER=str(root), HOME=str(home),
                USER=pwd.getpwuid(os.getuid()).pw_name,
                IAMROOT="", DEBUG="", notExtensions="^$",
                writeVB="___", writeB="___", TEST_GROUP=str(os.getgid()),
                GREEN="", NC="", E="E", SED_RED_YELLOW="&", SED_RED="&",
            )
            for filename, result_name in (
                ("14_Writable_files_owner_all.sh", "obmowbe"),
                ("15_Writable_files_group.sh", "iwfbg"),
            ):
                with self.subTest(module=filename):
                    module = MODULES / filename
                    command = (
                        'print_2title() { :; }; print_info() { :; }; '
                        'groups() { printf "%s\\n" "$TEST_GROUP"; }; '
                        '. "$1" >/dev/null 2>/dev/null; '
                        'if [ "$2" = owner ]; then printf "%s\\n" "$obmowbe"; '
                        'else printf "%s\\n" "$iwfbg"; fi'
                    )
                    kind = "owner" if result_name == "obmowbe" else "group"
                    result = subprocess.run(
                        ["sh", "-c", command, "sh", str(module), kind],
                        env=environment, text=True, capture_output=True, timeout=15,
                    )
                    self.assertEqual(result.returncode, 0, result.stderr)
                    self.assertIn(str(visible), result.stdout)
                    self.assertIn(str(home), result.stdout)
                    self.assertNotIn(str(hidden), result.stdout)


if __name__ == "__main__":
    unittest.main()
