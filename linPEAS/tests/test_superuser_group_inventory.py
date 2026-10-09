"""The existing privileged-group inventory should use NSS when available."""

import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/11_Superusers.sh")
SOURCE = MODULE.read_text()
GROUP_BLOCK = re.search(r"^if command -v getent.*?^fi$", SOURCE,
                        re.MULTILINE | re.DOTALL).group()


class SuperuserGroupInventoryTests(unittest.TestCase):
    def test_getent_available_reports_group_with_one_lookup_each(self):
        with tempfile.TemporaryDirectory() as tmp:
            count_file = Path(tmp) / "lookups"
            script = """
getent() {
    printf '%s\\n' "$2" >> "$COUNT_FILE"
    if [ "$1" = group ] && [ "$2" = docker ]; then
        printf 'docker:x:998:sample\\n'
        return 0
    fi
    return 2
}
""" + GROUP_BLOCK
            result = subprocess.run(
                ["/bin/sh", "-c", script], capture_output=True, text=True, timeout=2,
                env={**os.environ, "COUNT_FILE": str(count_file), "E": "E",
                     "USER": "sample", "sh_usrs": "never-match",
                     "nosh_usrs": "never-match", "knw_usrs": "never-match",
                     "SED_LIGHT_CYAN": "&", "SED_BLUE": "&", "SED_GREEN": "&",
                     "SED_RED": "&"},
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("Users in group 'docker':", result.stdout)
            self.assertIn("docker:x:998:sample", result.stdout)
            lookups = count_file.read_text().splitlines()
            self.assertEqual(10, len(lookups))
            self.assertEqual(1, lookups.count("docker"))

    def test_missing_getent_skips_group_inventory(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = subprocess.run(
                ["/bin/sh", "-c", GROUP_BLOCK], capture_output=True, text=True,
                timeout=2, env={**os.environ, "PATH": tmp},
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stdout)
            self.assertEqual("", result.stderr)

    def test_module_syntax(self):
        result = subprocess.run(["/bin/sh", "-n", str(MODULE)], capture_output=True,
                                text=True, timeout=2)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
