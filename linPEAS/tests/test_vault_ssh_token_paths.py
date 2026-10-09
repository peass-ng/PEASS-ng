"""Focused coverage for cached Vault token paths without CLI calls."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/7_software_information/Vault_ssh.sh"
)


class VaultSshTokenPathTests(unittest.TestCase):
    def run_module(self, token="", helper=""):
        with tempfile.TemporaryDirectory() as temp:
            directory = Path(temp)
            for name, output in (("vault", "mount-marker"), ("vault-ssh-helper", "verify-marker")):
                command = directory / name
                command.write_text("#!/bin/sh\nprintf '%s\\n' '" + output + "'\n")
                command.chmod(0o755)

            env = os.environ.copy()
            env.update(
                {
                    "PATH": temp + os.pathsep + env.get("PATH", ""),
                    "PSTORAGE_VAULT_SSH_TOKEN": token,
                    "PSTORAGE_VAULT_SSH_HELPER": helper,
                    "DEBUG": "",
                    "E": "E",
                    "SED_RED": "RED-&",
                }
            )
            result = subprocess.run(
                ["sh", "-c", 'print_2title() { printf "%s\\n" "$1"; }; . "$1"', "sh", str(MODULE)],
                env=env,
                text=True,
                capture_output=True,
                timeout=3,
                check=True,
            )
            return result.stdout

    def test_token_without_helper_is_path_only_and_does_not_call_vault(self):
        output = self.run_module(token="/home/user/.vault-token")
        self.assertIn("/home/user/.vault-token", output)
        self.assertIn("RED-/home/user/.vault-token", output)
        self.assertIn("Paths only", output)
        self.assertNotIn("mount-marker", output)
        self.assertNotIn("verify-marker", output)

    def test_helper_branch_still_runs_and_token_path_is_visible(self):
        with tempfile.TemporaryDirectory() as temp:
            helper = Path(temp) / "vault-ssh-helper.hcl"
            helper.write_text("helper-config-marker\n")
            output = self.run_module(token="/home/user/.vault-token", helper=str(helper))
        self.assertIn("/home/user/.vault-token", output)
        self.assertIn("helper-config-marker", output)
        self.assertIn("verify-marker", output)
        self.assertIn("mount-marker", output)

    def test_helper_without_token_keeps_original_helper_section(self):
        with tempfile.TemporaryDirectory() as temp:
            helper = Path(temp) / "vault-ssh-helper.hcl"
            helper.write_text("helper-config-marker\n")
            output = self.run_module(helper=str(helper))
        self.assertIn("Searching Vault-ssh files", output)
        self.assertIn("helper-config-marker", output)
        self.assertIn("verify-marker", output)
        self.assertIn("mount-marker", output)
        self.assertNotIn("Vault CLI token file paths", output)

    def test_absent_paths_do_not_call_vault(self):
        output = self.run_module()
        self.assertNotIn("Vault CLI token file paths", output)
        self.assertNotIn("mount-marker", output)
        self.assertNotIn("verify-marker", output)

    def test_token_path_output_is_capped(self):
        paths = "\n".join(f"/home/user{index}/.vault-token" for index in range(11))
        output = self.run_module(token=paths)
        self.assertIn("/home/user9/.vault-token", output)
        self.assertNotIn("/home/user10/.vault-token", output)
        self.assertIn("Additional token paths omitted (partial inventory)", output)
        self.assertNotIn("RED-Additional token paths omitted", output)


if __name__ == "__main__":
    unittest.main()
