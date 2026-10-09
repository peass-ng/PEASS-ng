import os
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/9_interesting_files/22_Passwords_php_files.sh"
)


class PhpMysqliCandidateTests(unittest.TestCase):
    def run_module(
        self, root: Path, *, search_in_folder: str = "", no_timeout: bool = False
    ) -> str:
        module = MODULE.read_text().replace("/var/www", str(root))
        shell = "print_2title() { :; }; E=E; SED_RED='';\n"
        if no_timeout:
            shell += "command() { return 1; };\n"
        shell += module
        result = subprocess.run(
            ["bash", "-c", shell],
            env={**os.environ, "SEARCH_IN_FOLDER": search_in_folder},
            capture_output=True,
            text=True,
            timeout=15,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_literal_and_variable_arguments_are_redacted(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            page = root / "stats.php"
            page.write_text(
                '<?php $db = new mysqli("local,host", "user", "secret,part", "db");\n'
                '$db = new mysqli($host, $user, $secret, $name);\n'
                '$db = new mysqli("host", "user", getenv("DB_SECRET"), "db");\n'
            )
            output = self.run_module(root)
            self.assertIn(f"{page}:1: mysqli positional credentials candidate (third argument: literal)", output)
            self.assertIn(f"{page}:2: mysqli positional credentials candidate (third argument: variable)", output)
            self.assertIn(f"{page}:3: mysqli positional credentials candidate (third argument: expression)", output)
            for secret in ("local,host", "secret,part", "$secret", "DB_SECRET"):
                self.assertNotIn(secret, output)

    def test_negative_depth_and_size(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            (root / "not_php.txt").write_text('new mysqli("h", "u", "secret");\n')
            (root / "comment.php").write_text('// new mysqli("h", "u", "secret");\n')
            (root / "quoted.php").write_text('$example = "new mysqli(h, u, secret)";\n')
            (root / "inline_comment.php").write_text('$x = 1; // new mysqli("h", "u", "secret");\n')
            (root / "short.php").write_text('new mysqli("h", "u");\n')
            (root / "empty.php").write_text(
                'new mysqli("h", "u", "");\n'
                "new mysqli('h', 'u', '');\n"
                'new mysqli("h", "u", null);\n'
            )
            (root / "oversize.php").write_text('new mysqli("h", "u", "secret");\n' + "x" * 1048576)
            deep = root / "a/b/c/d/e"
            deep.mkdir(parents=True)
            (deep / "deep.php").write_text('new mysqli("h", "u", "secret");\n')
            output = self.run_module(root)
            self.assertNotIn("mysqli positional credentials candidate", output)

    def test_folder_mode_skips_webroot(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            (root / "stats.php").write_text('new mysqli("h", "u", "secret");\n')
            self.assertEqual(self.run_module(root, search_in_folder="/fixture"), "")

    def test_timeout_unavailable_is_explicitly_unknown(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = Path(tmpdir)
            (root / "stats.php").write_text('new mysqli("h", "u", "secret");\n')
            self.assertIn(
                "PHP mysqli positional credentials: unknown (timeout unavailable).",
                self.run_module(root, no_timeout=True),
            )


if __name__ == "__main__":
    unittest.main()
