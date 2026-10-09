import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SudoSecurePathTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = Path(__file__).resolve().parents[1] / (
            "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
        )

    def run_policy(self, policy, setup=None):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp).resolve()
            bindir = base / "bin"
            bindir.mkdir()
            sudo = bindir / "sudo"
            sudo.write_text("#!/bin/sh\n[ \"$1 $2\" = '-n -l' ] || exit 99\n"
                            "printf '%s\\n' \"$FAKE_SUDO_POLICY\"\n")
            sudo.chmod(0o755)
            paths = setup(base) if setup else {}
            env = os.environ.copy()
            env.update(PATH=f"{bindir}:{env['PATH']}",
                       FAKE_SUDO_POLICY=policy.format(**paths))
            shell = "\n".join((
                "E=E", "sudoB=__no_color__", "sudoG=__no_color__",
                "sudoVB1=__no_color__", "sudoVB2=__no_color__",
                "SED_RED='&'", "SED_RED_YELLOW='&'", "SED_GREEN='&'",
                "ROOT_FOLDER=/", "PASSWORD=", "TIMEOUT=",
                "print_2title() { :; }", "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(self.module))}",
            ))
            result = subprocess.run(["sh", "-c", shell], env=env,
                                    capture_output=True, text=True, timeout=15)
            self.assertEqual(result.returncode, 0, result.stderr)
            return result.stdout

    @staticmethod
    def writable(base):
        writable = base / "writable"
        writable.mkdir()
        return {"writable": writable}

    def test_escaped_colon_and_root_rule(self):
        policy = ("Matching Defaults entries for user on host:\n"
                  "    env_reset, secure_path={writable}\\:/usr/bin\\:/bin\n\n"
                  "User user may run the following commands on host:\n"
                  "    (root) NOPASSWD: /usr/local/bin/status.sh")
        output = self.run_policy(policy, self.writable)
        self.assertIn("Writable/traversable sudo secure_path candidate 1 entry 1:", output)
        self.assertIn("verify effective Defaults", output)

    def test_multiple_defaults_and_repeated_sudo_output(self):
        def setup(base):
            paths = self.writable(base)
            second = base / "second"
            second.mkdir()
            paths["second"] = second
            return paths

        policy = ("Matching Defaults entries for user on host:\n"
                  "    secure_path=/usr/bin\\:/bin\n"
                  "    secure_path={writable}\\:/usr/bin\n"
                  "    secure_path={writable}\\:/usr/bin\n\n"
                  "Defaults!/usr/local/bin/status.sh secure_path={second}\\:/bin\n"
                  "User user may run the following commands on host:\n"
                  "    (root) NOPASSWD: /usr/local/bin/status.sh")
        output = self.run_policy(policy, setup)
        self.assertIn("candidate 2 entry 1", output)
        self.assertIn("candidate 3 entry 1", output)
        self.assertEqual(output.count("Writable/traversable sudo secure_path"), 2)

    def test_safe_path_and_missing_output(self):
        self.assertNotIn("Writable/traversable sudo secure_path",
                         self.run_policy("Matching Defaults entries for user on host:\n"
                                         "    secure_path=/usr/bin\\:/bin"))
        self.assertNotIn("sudo secure_path candidate", self.run_policy(""))

    def test_symlink_component_is_ambiguous(self):
        def setup(base):
            paths = self.writable(base)
            link = base / "link"
            link.symlink_to(paths["writable"], target_is_directory=True)
            paths["link"] = link
            return paths

        output = self.run_policy("Matching Defaults entries for user on host:\n"
                                 "    secure_path={link}\\:/usr/bin", setup)
        self.assertIn("has symlink component", output)
        self.assertNotIn("Writable/traversable sudo secure_path", output)

    def test_candidate_and_entry_caps(self):
        def setup(base):
            paths = self.writable(base)
            return paths

        policy = ("Matching Defaults entries for user on host:\n"
                  + "\n".join(f"    secure_path={{writable}}\\:/usr/bin\\:/bin{i}"
                              for i in range(10)))
        output = self.run_policy(policy, setup)
        self.assertEqual(output.count("Writable/traversable sudo secure_path"), 8)
        self.assertNotIn("candidate 9", output)

        entries = "\\:".join(["/usr/bin"] * 16 + ["{writable}"])
        output = self.run_policy("Matching Defaults entries for user on host:\n"
                                 f"    secure_path={entries}", setup)
        self.assertNotIn("Writable/traversable sudo secure_path", output)


if __name__ == "__main__":
    unittest.main()
