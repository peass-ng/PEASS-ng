"""An unrestricted sudo dotnet path is a review cue, not proof of escalation."""

import shlex
import subprocess
import unittest
from pathlib import Path


VARIABLE = (Path(__file__).resolve().parents[1] /
            "builder/linpeas_parts/variables/sudoB.sh")


class SudoDotnetHighlightTests(unittest.TestCase):
    def colorize(self, value):
        body = (f". {shlex.quote(str(VARIABLE))}\n"
                'sed -E "s,$sudoB,<H>,g"')
        result = subprocess.run(["sh", "-c", body], input=value + "\n",
                                capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def test_unrestricted_exact_dotnet_path(self):
        for rule in ("(root) /usr/bin/dotnet",
                     "(root) NOPASSWD: /usr/local/bin/dotnet, /usr/bin/true"):
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_fixed_args_and_lookalikes_do_not_match(self):
        for rule in ("(root) /usr/bin/dotnet --info",
                     "(root) /usr/bin/dotnet /opt/trusted.dll",
                     "(root) /usr/bin/dotnet-helper",
                     "(root) /usr/bin/dotnet.dll",
                     "(root) !/usr/bin/dotnet"):
            with self.subTest(rule=rule):
                self.assertNotIn("<H>", self.colorize(rule))

    def test_variable_module_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(VARIABLE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
