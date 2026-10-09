"""Focused metadata-only Passbolt browser-storage inventory fixtures."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest

MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/7_software_information/Browser_profiles.sh"
EXTENSION_ID = "didegimhafipceonhjepacocaffmoppf"


class PassboltBrowserProfileTests(unittest.TestCase):
    def run_module(self, home):
        shell = """
            print_2title() { :; }
            print_3title() { :; }
            print_info() { :; }
            HOMESEARCH=$TEST_HOME
            E=E
            SED_RED=profile
            . "$MODULE_PATH"
        """
        env = dict(os.environ, TEST_HOME=str(home), MODULE_PATH=str(MODULE))
        result = subprocess.run(["sh", "-c", shell], env=env, text=True,
                                capture_output=True, timeout=5, check=True)
        return result.stdout

    def test_exact_readable_extension_storage_path(self):
        with tempfile.TemporaryDirectory() as temporary:
            home = Path(temporary)
            profile = home / ".config/google-chrome/Default"
            store = profile / "Local Extension Settings" / EXTENSION_ID
            store.mkdir(parents=True)
            (store / "000003.log").write_text("secret-looking fixture must stay hidden")
            output = self.run_module(home)
            self.assertIn(str(store), output)
            self.assertIn("key availability unknown", output)
            self.assertNotIn("secret-looking fixture", output)

    def test_lookalike_and_symlink_parent_are_not_reported(self):
        with tempfile.TemporaryDirectory() as temporary:
            home = Path(temporary)
            base = home / ".config/google-chrome"
            wrong = base / "Default/Local Extension Settings" / (EXTENSION_ID + "x")
            wrong.mkdir(parents=True)
            linked_profile = base / "Profile 1"
            linked_profile.mkdir(parents=True)
            external = home / "external" / EXTENSION_ID
            external.mkdir(parents=True)
            (linked_profile / "Local Extension Settings").symlink_to(external.parent,
                                                                        target_is_directory=True)
            output = self.run_module(home)
            self.assertNotIn("Passbolt extension local storage", output)

    def test_inventory_is_capped_to_eight_profiles_per_browser(self):
        with tempfile.TemporaryDirectory() as temporary:
            home = Path(temporary)
            base = home / ".config/google-chrome"
            for i in range(12):
                (base / f"Profile {i}" / "Local Extension Settings" / EXTENSION_ID).mkdir(parents=True)
            output = self.run_module(home)
            self.assertEqual(output.count("Passbolt extension local storage"), 8)


if __name__ == "__main__":
    unittest.main()
