"""Exercise the fixed-path hosts-file review cue without changing /etc/hosts."""

from pathlib import Path
import re
import shutil
import subprocess
import unittest


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/5_network_information/2_Hostname_hosts_dns.sh"
)
SOURCE = MODULE.read_text()
FUNCTION = re.search(r"(?ms)^get_hosts_info\(\) \{\n.*?^\}", SOURCE).group()


class HostsFileWriteabilityTests(unittest.TestCase):
    def run_fixture(self, shell, writable):
        script = f"""
print_3title() {{ :; }}
test() {{
    if [ "$1" = -w ] && [ "$2" = /etc/hosts ]; then
        [ "$HOSTS_WRITABLE" = 1 ]
    else
        command test "$@"
    fi
}}
{FUNCTION}
HOSTS_WRITABLE={int(writable)}
get_hosts_info
"""
        result = subprocess.run(
            shell,
            input=script,
            text=True,
            capture_output=True,
            check=True,
            timeout=5,
        )
        return result.stdout

    def test_writable_hosts_emits_conditional_cue(self):
        shells = [["/bin/sh"]]
        if shutil.which("busybox"):
            shells.append(["busybox", "sh"])
        for shell in shells:
            with self.subTest(shell=shell):
                output = self.run_fixture(shell, True)
                self.assertIn("Writable /etc/hosts review candidate", output)
                self.assertIn("higher-privileged job", output)

    def test_unwritable_hosts_does_not_emit_cue(self):
        self.assertNotIn(
            "Writable /etc/hosts review candidate",
            self.run_fixture(["/bin/sh"], False),
        )


if __name__ == "__main__":
    unittest.main()
