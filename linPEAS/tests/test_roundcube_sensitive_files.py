import subprocess
import sys
import unittest
from pathlib import Path

import yaml


class RoundcubeSensitiveFilesTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        data = yaml.safe_load((cls.repo_root / "build_lists/sensitive_files.yaml").read_text())
        roundcube = next(item for item in data["search"] if item["name"] == "Roundcube")
        directory = roundcube["value"]["files"][0]
        config = directory["value"]["files"][0]
        cls.selector = config["value"]["line_grep"]
        cls.record = roundcube

    def _select(self, content):
        return subprocess.run(
            ["sh", "-c", f"grep -E {self.selector}"],
            input=content,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_four_keys_are_bounded_and_values_redacted(self):
        content = "\n".join(
            [
                "$config['db_dsnw'] = 'mysql://user:secret-never-print@localhost/db';",
                '$config["des_key"] = "secret-never-print";',
                "$config['cipher_method'] = 'secret-never-print';",
                "$config['session_storage'] = 'secret-never-print';",
                "$config['db_dsnw'] = 'duplicate-secret-never-print';",
                "$config['skin'] = 'unrelated-secret-never-print';",
                "// $config['des_key'] = 'comment-secret-never-print';",
                "$unrelated['des_key'] = 'unrelated-secret-never-print';",
            ]
        )
        result = self._select(content)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(len(result.stdout.splitlines()), 4)
        for key in ("db_dsnw", "des_key", "cipher_method", "session_storage"):
            self.assertIn(f"{key}: present (value redacted)", result.stdout)
        self.assertNotIn("secret-never-print", result.stdout)
        self.assertNotIn("skin", result.stdout)

    def test_generic_php_and_absent_config_have_no_output(self):
        self.assertEqual(self._select("$unrelated['db_dsnw'] = 'secret';\n").stdout, "")
        self.assertEqual(self._select("").stdout, "")

    def test_builder_emits_selector_in_existing_roundcube_section(self):
        sys.path.insert(0, str(self.repo_root / "linPEAS"))
        from builder.src.linpeasBuilder import LinpeasBuilder  # pylint: disable=import-error
        from builder.src.peasLoaded import PEASLoaded  # pylint: disable=import-error

        builder = LinpeasBuilder.__new__(LinpeasBuilder)
        builder.ploaded = PEASLoaded()
        section = builder._LinpeasBuilder__generate_sections()["Roundcube"]
        self.assertIn("PSTORAGE_ROUNDCUBE", section)
        self.assertIn("config.inc.php", section)
        self.assertIn("db_dsnw is a database credential", section)
        self.assertIn("value redacted", section)
        self.assertIn("head -n 4", section)


if __name__ == "__main__":
    unittest.main()
