import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SudoAdduserGroupTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (Path(__file__).resolve().parents[1] /
                      "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
        source = cls.module.read_text()
        start = source.index("sudo_adduser_group_review() (")
        end = source.index("\nsudo_adduser_group_review ", start)
        cls.function = source[start:end]

    def run_case(self, grant, policy="%admin ALL=(ALL) ALL\n",
                 groups="root:x:0:\n", extra_policy_files=None):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            sudoers = base / "sudoers"
            if policy is not None:
                sudoers.write_text(policy)
            included = base / "sudoers.d"
            included.mkdir()
            for name, content in (extra_policy_files or {}).items():
                (included / name).write_text(content)
            group_file = base / "group"
            group_file.write_text(groups)
            bin_dir = base / "bin"
            bin_dir.mkdir()
            call_log = base / "calls"
            for name in ("sudo", "adduser", "su"):
                command = bin_dir / name
                command.write_text(
                    '#!/bin/sh\nprintf "%s\\n" called >> "$CALL_LOG"\n'
                )
                command.chmod(0o755)
            env = dict(os.environ, GRANT=grant, CALL_LOG=str(call_log),
                       PATH=f"{bin_dir}:{os.environ['PATH']}")
            shell = (self.function + "\n" +
                     "sudo_adduser_group_review \"$GRANT\" '' '' " +
                     " ".join(shlex.quote(str(path)) for path in
                              (sudoers, included, group_file)))
            result = subprocess.run(["sh", "-c", shell], env=env,
                                    capture_output=True, text=True, timeout=8)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse(call_log.exists())
            return result.stdout

    def test_visible_group_rule_and_missing_group_are_review_candidate(self):
        grant = "User operator may run the following commands on host:\n" \
                "    (ALL : ALL) NOPASSWD: /usr/sbin/adduser ^[a-zA-Z0-9]+$"
        output = self.run_case(grant)
        self.assertEqual(output.count("Sudo adduser missing-group review candidate"), 1)
        self.assertIn("local admin group absent", output)
        self.assertIn("verify effective policy", output)
        self.assertNotIn("root access", output)

    def test_exact_name_and_included_policy(self):
        output = self.run_case("    (root) NOPASSWD: /usr/sbin/adduser admin",
                               policy="", extra_policy_files={"grant": "%admin ALL=(ALL:ALL) ALL\n"})
        self.assertIn("review candidate", output)
        self.assertNotIn("conditional lead", output)

    def test_unreadable_or_oversized_policy_is_unknown(self):
        grant = "    (root) NOPASSWD: /usr/sbin/adduser ^admin$"
        self.assertIn("conditional lead", self.run_case(grant, policy=None))
        self.assertIn("policy unreadable or partial", self.run_case(
            grant, policy="# filler\n" + "x" * 65537))

    def test_existing_group_or_visible_nonmatching_policy_is_not_reported(self):
        grant = "    (root) NOPASSWD: /usr/sbin/adduser ^admin$"
        self.assertEqual(self.run_case(grant, groups="root:x:0:\nadmin:x:1001:\n"), "")
        self.assertEqual(self.run_case(grant, policy="%sudo ALL=(ALL) ALL\n"), "")
        self.assertEqual(self.run_case(grant, policy="# %admin ALL=(ALL) ALL\n"), "")
        self.assertEqual(self.run_case(grant, policy="%admin host=(ALL) ALL\n"), "")

    def test_rejects_unrelated_and_ambiguous_grants(self):
        policies = (
            "    (operator) NOPASSWD: /usr/sbin/adduser ^admin$",
            "    (ALL, !root) NOPASSWD: /usr/sbin/adduser ^admin$",
            "    (root) NOPASSWD: /usr/sbin/useradd ^admin$",
            "    (root) NOPASSWD: /usr/sbin/adduser",
            "    (root) NOPASSWD: /usr/sbin/adduser ^admin$ --system",
            "    (root) NOPASSWD: /usr/sbin/adduser ^admin$|wheel",
            "    (root) NOPASSWD: !/usr/sbin/adduser ^admin$",
            "    (root) NOPASSWD: /usr/sbin/adduser ^admin$, !/usr/sbin/adduser ^admin$",
            "    (root) NOPASSWD: /usr/sbin/adduser ^admin$\n"
            "        !/usr/sbin/adduser ^admin$",
            "    (root) NOPASSWD: /usr/sbin/adduser ^admin$\n"
            "        --other-option",
        )
        for policy in policies:
            with self.subTest(policy=policy):
                self.assertEqual(self.run_case(policy), "")

    def test_source_line_bound_rejects_partial_capture(self):
        grant = ("    (root) NOPASSWD: /usr/sbin/adduser ^admin$\n" +
                 "irrelevant\n" * 3000)
        self.assertEqual(self.run_case(grant), "")

    def test_module_syntax_and_metadata(self):
        from linPEAS.builder.src.linpeasModule import LinpeasModule

        module = LinpeasModule(str(self.module))
        self.assertEqual(module.id, "UG_Sudo_l")
        result = subprocess.run(["sh", "-n", str(self.module)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
