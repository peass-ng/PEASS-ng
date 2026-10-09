"""Exact executable boundary for the existing sudo color cue."""

import shlex
import subprocess
import unittest
from pathlib import Path


VARIABLE = (Path(__file__).resolve().parents[1] /
            "builder/linpeas_parts/variables/sudoB.sh")


class SudoNginxHighlightTests(unittest.TestCase):
    def colorize(self, value):
        body = (f". {shlex.quote(str(VARIABLE))}\n"
                'sed -E "s,$sudoB,<H>,g"')
        result = subprocess.run(["sh", "-c", body], input=value + "\n",
                                capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def test_exact_nginx_paths_are_highlighted(self):
        for path in ("/usr/sbin/nginx", "/usr/local/nginx/sbin/nginx", "/nginx"):
            with self.subTest(path=path):
                output = self.colorize("(daemon) " + path)
                self.assertIn("<H>", output)
                self.assertNotIn(path, output)

    def test_lookalike_or_negated_paths_are_not_highlighted(self):
        for path in ("/usr/sbin/nginx-helper", "/usr/sbin/nginxx",
                     "/usr/sbin/mynginx", "/usr/sbin/nginx.conf",
                     "/usr/sbin/nginx/child", "!/usr/sbin/nginx"):
            with self.subTest(path=path):
                output = self.colorize("(daemon) " + path)
                self.assertNotIn("<H>", output)
                self.assertIn(path, output)

    def test_variable_module_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(VARIABLE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
