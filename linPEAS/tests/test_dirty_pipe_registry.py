"""Check upstream Dirty Pipe branch boundaries through the registry evaluator."""

from pathlib import Path
import os
import shutil
import subprocess
import unittest


PARTS = Path(__file__).resolve().parents[1] / "builder/linpeas_parts"
DATA = PARTS / "variables/kernel_cve_registry_data.sh"
FUNCTIONS = PARTS / "functions/kernel_cve_registry_checks.sh"
ROWS = "\n".join(
    line for line in DATA.read_text().splitlines()
    if line.startswith("CVE-2022-0847\t")
)
SCRIPT = """
print_list() { printf '%b' "$1"; }
echo_not_found() { :; }
E=E
SED_RED_YELLOW='&'
. "$REGISTRY_FUNCTIONS"
KERNEL_CVE_DATA_23="$ROWS"
uname() {
    case "$1" in
        -s) echo Linux ;;
        -r) echo "$RELEASE" ;;
        -m) echo x86_64 ;;
    esac
}
kercve_run_registry
"""


class DirtyPipeRegistryTests(unittest.TestCase):
    def test_disjoint_upstream_fixed_boundaries(self):
        self.assertEqual(len(ROWS.splitlines()), 3)
        cases = {
            "5.7.99": False,
            "5.8.0": True,
            "5.10.101": True,
            "5.10.102": False,
            "5.11.0": True,
            "5.15.24": True,
            "5.15.25": False,
            "5.16.0": True,
            "5.16.10": True,
            "5.16.11": False,
            "5.17.0": False,
        }
        shells = [["/bin/sh"]]
        if shutil.which("busybox"):
            shells.append(["busybox", "sh"])
        for shell in shells:
            for release, candidate in cases.items():
                with self.subTest(shell=shell, release=release):
                    env = os.environ.copy()
                    env.update(REGISTRY_FUNCTIONS=str(FUNCTIONS), ROWS=ROWS, RELEASE=release)
                    result = subprocess.run(
                        shell, input=SCRIPT, text=True, capture_output=True,
                        check=True, timeout=5, env=env,
                    )
                    matches = result.stdout.count("Name: DirtyPipe")
                    self.assertEqual(matches, int(candidate), result.stdout)
                    if candidate:
                        self.assertIn("Details: Upstream stable", result.stdout)
                        self.assertIn("vendor backports", result.stdout)
                        self.assertIn("Rank: 1", result.stdout)


if __name__ == "__main__":
    unittest.main()
