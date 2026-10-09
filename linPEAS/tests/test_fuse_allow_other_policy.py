"""The FUSE cue distinguishes an active policy from comments and unknown state."""

import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/1_system_information/7_Mounts.sh"


class FuseAllowOtherPolicyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = MODULE.read_text()
        body = source.split("fuse_allow_other_state() {\n", 1)[1].split("\n}\n", 1)[0]
        cls.function = "fuse_allow_other_state() {\n" + body + "\n}\n"

    def state(self, path):
        result = subprocess.run(
            ["sh", "-c", self.function + '\nfuse_allow_other_state "$1"', "sh", str(path)],
            text=True, capture_output=True, timeout=2, check=True,
        )
        self.assertEqual("", result.stderr)
        return result.stdout.strip()

    def test_active_standalone_option_only(self):
        with tempfile.TemporaryDirectory() as temp:
            config = Path(temp) / "fuse.conf"
            config.write_text("# user_allow_other\n  user_allow_other  \n")
            self.assertEqual("enabled", self.state(config))

    def test_comment_and_assignment_are_not_active(self):
        with tempfile.TemporaryDirectory() as temp:
            config = Path(temp) / "fuse.conf"
            config.write_text("#user_allow_other\nuser_allow_other = yes\n")
            self.assertEqual("disabled", self.state(config))

    def test_missing_and_unbounded_config_are_unknown(self):
        with tempfile.TemporaryDirectory() as temp:
            config = Path(temp) / "fuse.conf"
            self.assertEqual("unknown", self.state(config))
            config.write_text("# unused\n" * 257 + "user_allow_other\n")
            self.assertEqual("unknown", self.state(config))
            config.write_text("# unused\n" * 256 + "user_allow_other\n")
            self.assertEqual("unknown", self.state(config))

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True, capture_output=True)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
