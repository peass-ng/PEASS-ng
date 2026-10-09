import subprocess
import unittest
from pathlib import Path

import yaml


class ZabbixFrontendConfigTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = Path(__file__).resolve().parents[2]
        records = yaml.safe_load((root / "build_lists/sensitive_files.yaml").read_text())["search"]
        zabbix = next(record for record in records if record["name"] == "Zabbix")
        config = next(record for record in zabbix["value"]["files"]
                      if record["name"] == "zabbix.conf.php")
        cls.selector = config["value"]["line_grep"]

    def test_frontend_keys_are_reported_without_values(self):
        content = "\n".join([
            "$DB['USER'] = 'app_user';",
            "$DB['PASSWORD'] = 'secret-never-print';",
            '$DB["SERVER"] = "127.0.0.1";',
            "$DB['DATABASE'] = 'zabbix';",
            "$DB['PASSWORD'] = 'duplicate-secret-never-print';",
            "// $DB['PASSWORD'] = 'comment-secret-never-print';",
            "$OTHER['PASSWORD'] = 'unrelated-secret-never-print';",
        ])
        result = subprocess.run(["sh", "-c", f"grep -E {self.selector}"],
                                input=content, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(len(result.stdout.splitlines()), 4)
        for key in ("USER", "PASSWORD", "SERVER", "DATABASE"):
            self.assertIn(f"{key}: present (value redacted)", result.stdout)
        self.assertNotIn("secret-never-print", result.stdout)

    def test_vault_or_empty_password_is_only_a_presence_signal(self):
        result = subprocess.run(["sh", "-c", f"grep -E {self.selector}"],
                                input="$DB['PASSWORD'] = '';\n",
                                capture_output=True, text=True)
        self.assertEqual(result.stdout.strip(), "PASSWORD: present (value redacted)")


if __name__ == "__main__":
    unittest.main()
