"""Current-process NoNewPrivs reports unknown on systems without procfs."""

import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/1_system_information/16_Protections.sh"


class NoNewPrivsStatusTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = MODULE.read_text()
        cls.function = source.split("read_no_new_privs_status() {\n", 1)[1].split("\n}\n", 1)[0]
        cls.function = "read_no_new_privs_status() {\n" + cls.function + "\n}\n"

    def read_status(self, path):
        result = subprocess.run(
            ["/bin/sh", "-c", self.function + '\nread_no_new_privs_status "$1"', "sh", str(path)],
            text=True, capture_output=True, timeout=2, check=True,
        )
        self.assertEqual("", result.stderr)
        return result.stdout.strip()

    def test_missing_procfs_is_unknown(self):
        with tempfile.TemporaryDirectory() as temp:
            self.assertEqual("unknown", self.read_status(Path(temp) / "missing-status"))

    def test_exact_kernel_values_and_malformed_field(self):
        with tempfile.TemporaryDirectory() as temp:
            status = Path(temp) / "status"
            for value, expected in (("0", "0"), ("1", "1"), ("2", "unknown"), ("", "unknown")):
                with self.subTest(value=value):
                    status.write_text("Name:\tsh\nNoNewPrivs:\t" + value + "\n")
                    self.assertEqual(expected, self.read_status(status))


if __name__ == "__main__":
    unittest.main()
