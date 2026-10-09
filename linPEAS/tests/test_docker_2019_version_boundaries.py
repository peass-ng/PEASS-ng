"""Docker release cues use component ordering and vendor fix boundaries."""

import subprocess
import unittest
from pathlib import Path


FUNCTION = (Path(__file__).resolve().parents[1] /
            "builder/linpeas_parts/functions/checkDockerVersionExploits.sh")


class DockerVersionBoundaryTests(unittest.TestCase):
    def evaluate(self, version):
        shell = (
            'echo_no() { printf No; }; echo_not_found() { printf Unknown; }; '
            '. "$1"; dockerVersion=$2; checkDockerVersionExploits; '
            'printf "%s|%s|%s\\n" "$VULN_CVE_2019_13139" '
            '"$VULN_CVE_2019_5736" "$VULN_CVE_2021_41091"'
        )
        result = subprocess.run(
            ["sh", "-c", shell, "sh", str(FUNCTION), version],
            text=True, capture_output=True, timeout=3,
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("", result.stderr)
        return result.stdout.strip()

    def test_vendor_release_boundaries_and_package_suffixes(self):
        cases = {
            "17.03.1": "Yes|Yes|Yes",
            "18.09.1": "Yes|Yes|Yes",
            "18.09.2": "Yes|No|Yes",
            "18.09.3": "Yes|No|Yes",
            "18.09.4": "No|No|Yes",
            "18.09.4-ce": "No|No|Yes",
            "18.09.1+dfsg1": "Yes|Yes|Yes",
            "20.10.8": "No|No|Yes",
            "20.10.9": "No|No|No",
            "24.0.9": "No|No|No",
        }
        for version, expected in cases.items():
            with self.subTest(version=version):
                self.assertEqual(expected, self.evaluate(version))

    def test_unknown_versions_do_not_produce_definitive_negative(self):
        for version in ("not found", "unknown", "18.09.1-rc1",
                        "18.09.3.1", "999999999999.0.0"):
            with self.subTest(version=version):
                self.assertEqual("Unknown|Unknown|Unknown", self.evaluate(version))


if __name__ == "__main__":
    unittest.main()
