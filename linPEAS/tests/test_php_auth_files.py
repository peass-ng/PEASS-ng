import subprocess
import tempfile
import unittest
from pathlib import Path


class PhpAuthFilesTests(unittest.TestCase):
    def test_folder_search_does_not_scan_host_webroot(self):
        module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/9_interesting_files/22_Passwords_php_files.sh"
        ).read_text()
        with tempfile.TemporaryDirectory() as tmpdir:
            marker = Path(tmpdir) / "host-scan"
            shell = (
                "print_2title() { :; }; E=E; SED_RED=''; SEARCH_IN_FOLDER=/fixture;\n"
                f"find() {{ touch {marker}; }};\n" + module
            )
            result = subprocess.run(["bash", "-c", shell], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse(marker.exists(), "folder search scanned the host webroot")
            self.assertEqual(result.stdout, "")

    def test_bounded_webroot_scan_includes_login_and_auth_scripts(self):
        module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/9_interesting_files/22_Passwords_php_files.sh"
        ).read_text()
        with tempfile.TemporaryDirectory() as tmpdir:
            webroot = Path(tmpdir) / "www"
            html = webroot / "html"
            html.mkdir(parents=True)
            (html / "login.php").write_text("$logins = array('admin' => array('password' => 'login-secret'));\n")
            (html / "auth.php").write_text("$password = 'auth-secret';\n")
            (html / "profile.php").write_text("$password = 'profile-secret';\n")
            (html / "oversized").mkdir()
            (html / "oversized" / "auth.php").write_text(
                "$password = 'oversized-secret';\n" + "x" * 1048576
            )
            deep = webroot / "a/b/c/d/e"
            deep.mkdir(parents=True)
            (deep / "login.php").write_text("$password = 'deep-secret';\n")

            # Substitute only the scan root so the generated shell pipeline runs on fixtures.
            shell = "print_2title() { :; }; E=E; SED_RED='';\n" + module.replace(
                "find /var/www -maxdepth", f"find {webroot} -maxdepth"
            )
            result = subprocess.run(["bash", "-c", shell], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("login-secret", result.stdout)
            self.assertIn("auth-secret", result.stdout)
            self.assertNotIn("profile-secret", result.stdout)
            self.assertNotIn("oversized-secret", result.stdout)
            self.assertNotIn("deep-secret", result.stdout)


if __name__ == "__main__":
    unittest.main()
