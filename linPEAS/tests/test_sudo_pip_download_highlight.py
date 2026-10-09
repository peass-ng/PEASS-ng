"""The existing sudo highlight marks exact pip download commands for review."""

import shlex
import subprocess
import unittest
from pathlib import Path


VARIABLE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/variables/sudoB.sh"


class SudoPipDownloadHighlightTests(unittest.TestCase):
    def colorize(self, rule):
        shell = f'. {shlex.quote(str(VARIABLE))}\nsed -E "s,$sudoB,<H>,g"'
        result = subprocess.run(
            ["sh", "-c", shell],
            input=rule + "\n",
            capture_output=True,
            text=True,
            timeout=5,
            check=False,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_exact_download_forms(self):
        for rule in (
            "(root) /usr/bin/pip3 download http://127.0.0.1:3000/pkg.tar.gz",
            "(root) /usr/local/bin/pip download /tmp/pkg.tar.gz",
            "(root) /usr/bin/pip3.11 ^download https://example.invalid/pkg.tar.gz",
        ):
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_nonmatching_commands(self):
        for rule in (
            "(root) !/usr/bin/pip3 download /tmp/pkg.tar.gz",
            "(root) /usr/bin/pip3 install /tmp/pkg.tar.gz",
            "(root) /usr/bin/pip3 download-extra /tmp/pkg.tar.gz",
            "(root) /usr/bin/pip3-helper download /tmp/pkg.tar.gz",
            "(root) /usr/bin/pip3 config download",
        ):
            with self.subTest(rule=rule):
                self.assertNotIn("<H>", self.colorize(rule))


if __name__ == "__main__":
    unittest.main()
