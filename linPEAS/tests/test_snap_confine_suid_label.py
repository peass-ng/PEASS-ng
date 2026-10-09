"""The SUID catalog keeps an exact, version-neutral snap-confine review cue."""

import subprocess
import unittest
from pathlib import Path


RULES = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/variables/sidB.sh"


class SnapConfineSuidLabelTests(unittest.TestCase):
    def test_exact_helper_match_without_socket_cve_verdict(self):
        entries = [line.strip().rstrip("\\") for line in RULES.read_text().splitlines()
                   if line.strip().startswith("/snap-confine$")]
        self.assertEqual(1, len(entries))
        pattern, label = entries[0].split("%", 1)
        self.assertEqual("/snap-confine$", pattern)
        self.assertIn("review", label)
        self.assertNotIn("CVE-2019-7304", label)
        self.assertNotIn("2.37", label)

        positive = ("/usr/lib/snapd/snap-confine", "/snap/snapd/current/usr/lib/snapd/snap-confine")
        negative = ("/usr/lib/snapd/snap-confine-helper", "/usr/lib/snapd/snap-confine2",
                    "/usr/lib/snapd/not-snap-confine.so", "/usr/lib/snapd/snapd")
        for path in positive + negative:
            grep = subprocess.run(["grep", "-q", pattern], input=path + "\n",
                                  text=True, timeout=2)
            self.assertEqual(path in positive, grep.returncode == 0, path)
            sed = subprocess.run(["sed", "-E", "s," + pattern + ",[review],"],
                                 input=path + "\n", capture_output=True,
                                 text=True, timeout=2, check=True)
            self.assertEqual(path in positive, "[review]" in sed.stdout, path)


if __name__ == "__main__":
    unittest.main()
