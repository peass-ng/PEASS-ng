import os
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/15_Rcommands_trust.sh"
)


class RcommandsTrustTests(unittest.TestCase):
    def run_helper(self, name, file, size=None, env=None):
        script = 'SEARCH_IN_FOLDER=1; . "$1"; shift; rcommands_' + name + ' "$@"'
        args = ["sh", "-c", script, "sh", str(MODULE), str(file)]
        if size is not None:
            args.append(str(size))
        return subprocess.run(args, text=True, capture_output=True, check=True, env=env).stdout

    def test_hostname_and_remote_user_cues(self):
        with tempfile.TemporaryDirectory() as directory:
            file = Path(directory) / ".rhosts"
            file.write_text("# comment\ntrusted.example +\n192.0.2.5 alice\n")
            output = self.run_helper("trust_entries", file, file.stat().st_size)
            self.assertIn("trusted.example +", output)
            self.assertIn("Hostname-based trust candidate", output)
            self.assertIn("Remote-user + may broaden trust", output)
            self.assertIn("Wildcard '+' trust found", output)
            self.assertNotIn("# comment", output)
            self.assertNotIn("truncated", output)

    def test_numeric_address_does_not_claim_hostname_trust(self):
        with tempfile.TemporaryDirectory() as directory:
            file = Path(directory) / "hosts.equiv"
            file.write_text("192.0.2.5 alice\n")
            output = self.run_helper("trust_entries", file, file.stat().st_size)
            self.assertNotIn("Hostname-based trust candidate", output)
            self.assertNotIn("Remote-user +", output)
            self.assertNotIn("Wildcard", output)

    def test_first_field_wildcard_preserves_existing_warning(self):
        with tempfile.TemporaryDirectory() as directory:
            file = Path(directory) / "hosts.equiv"
            file.write_text("+ alice\n")
            output = self.run_helper("trust_entries", file, file.stat().st_size)
            self.assertIn("Wildcard '+' trust found", output)
            self.assertNotIn("Remote-user +", output)

    def test_line_and_byte_caps(self):
        with tempfile.TemporaryDirectory() as directory:
            file = Path(directory) / ".rhosts"
            file.write_text("trusted.example alice\n" * 100)
            output = self.run_helper("trust_entries", file, file.stat().st_size)
            self.assertEqual(output.count("trusted.example alice"), 64)
            self.assertIn("Trust entries truncated after 64 lines", output)

            file.write_text("trusted.example +" + "x" * 70000 + "\n")
            output = self.run_helper("trust_entries", file, file.stat().st_size)
            self.assertIn("line shortened", output)
            self.assertIn("Trust-file input limited to first 64 KiB", output)
            self.assertLess(len(output), 1200)

    def test_bsd_stat_fallback(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            file = root / ".rhosts"
            file.write_text("trusted.example +\n")
            fake_stat = root / "stat"
            fake_stat.write_text(
                "#!/bin/sh\n"
                "case \"$1\" in\n"
                "  -c) exit 1 ;;\n"
                "  -f) printf '600 root 18\\n' ;;\n"
                "  *) exit 1 ;;\n"
                "esac\n"
            )
            fake_stat.chmod(0o755)
            env = dict(os.environ)
            env["PATH"] = str(root) + os.pathsep + env.get("PATH", "")
            self.assertEqual(self.run_helper("trust_metadata", file, env=env), "600 root 18\n")

    def test_group_other_write_mode_bits(self):
        script = 'SEARCH_IN_FOLDER=1; . "$1"; rcommands_trust_group_other_writable "$2"'
        for mode, expected in (("600", 1), ("620", 0), ("602", 0), ("1600", 1), ("1620", 0), ("invalid", 1)):
            with self.subTest(mode=mode):
                result = subprocess.run(
                    ["sh", "-c", script, "sh", str(MODULE), mode],
                    text=True,
                    capture_output=True,
                )
                self.assertEqual(result.returncode, expected, result.stderr)


if __name__ == "__main__":
    unittest.main()
