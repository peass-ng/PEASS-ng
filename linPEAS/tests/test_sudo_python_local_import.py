import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SudoPythonLocalImportTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
        )

    def run_case(
        self, *, source="from helpers import status\n", helper_mode=0o666,
        package_mode=0o555, init_mode=0o444, create_helper=True,
        symlink_package=False,
        rule_template=None, script_mode=0o755, script_size=0,
    ):
        with tempfile.TemporaryDirectory(dir=Path.home()) as temp:
            base = Path(temp)
            bindir = base / "bin"
            bindir.mkdir()
            marker = base / "executed"
            script = base / "entry.py"
            script.write_text(
                "#!/usr/bin/env python3\n"
                "open(" + repr(str(marker)) + ", 'w').close()\n"
                + source + "SECRET = 'private-value-must-stay-hidden'\n"
                + ("x = 1\n" * script_size)
            )
            script.chmod(script_mode)
            package = base / "helpers"
            actual_package = base / "other"
            if symlink_package:
                actual_package.mkdir()
                package.symlink_to(actual_package, target_is_directory=True)
            else:
                package.mkdir()
                actual_package = package
            (actual_package / "__init__.py").write_text(
                "status = 1\n" if not create_helper else "pass\n"
            )
            (actual_package / "__init__.py").chmod(init_mode)
            helper = actual_package / "status.py"
            if create_helper:
                helper.write_text("SECRET = 'private-value-must-stay-hidden'\n")
                helper.chmod(helper_mode)
            unrelated = base / "unrelated.py"
            unrelated.write_text("x = 1\n")
            unrelated.chmod(0o666)
            sudo = bindir / "sudo"
            sudo.write_text("#!/bin/sh\nprintf '%s\\n' \"$FAKE_SUDO_RULE\"\n")
            sudo.chmod(0o755)
            env = os.environ.copy()
            env.update({
                "PATH": f"{bindir}:{env['PATH']}",
                "FAKE_SUDO_RULE": (
                    rule_template.format(script=script, directory=base)
                    if rule_template else f"    (operator) NOPASSWD: {script}"
                ),
            })
            body = "\n".join([
                "E=E",
                "sudoB=__unlikely_sudoB__",
                "sudoG=__unlikely_sudoG__",
                "sudoVB1=__unlikely_sudoVB1__",
                "sudoVB2=__unlikely_sudoVB2__",
                "SED_RED='&'",
                "SED_RED_YELLOW='&'",
                "SED_GREEN='&'",
                "PASSWORD=",
                "TIMEOUT=",
                "print_2title() { :; }",
                "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(self.module))}",
            ])
            actual_package.chmod(package_mode)
            base.chmod(0o555)
            try:
                result = subprocess.run(
                    ["sh", "-c", body], env=env, capture_output=True,
                    text=True, timeout=10,
                )
            finally:
                base.chmod(0o700)
                actual_package.chmod(0o700)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse(marker.exists(), "sudo entry point was executed")
            self.assertNotIn("private-value-must-stay-hidden", result.stdout)
            return result.stdout, str(helper)

    def test_non_root_runas_and_writable_submodule(self):
        output, helper = self.run_case()
        self.assertIn("Sudo Python import review:", output)
        self.assertIn("as operator", output)
        self.assertIn("Literal import at line 3: from helpers import status", output)
        self.assertIn(helper, output)
        self.assertEqual(output.count("Caller-writable local import candidate:"), 1)

    def test_parent_replacement_and_direct_import(self):
        output, helper = self.run_case(
            source="import helpers.status\n", helper_mode=0o444,
            package_mode=0o777,
        )
        self.assertIn(helper, output)
        self.assertIn("parent permits replacement", output)
        self.assertIn("Literal import at line 3: import helpers.status", output)

    def test_top_level_module_and_package_initializer(self):
        module_output, _ = self.run_case(source="import unrelated\n")
        self.assertIn("unrelated.py", module_output)
        package_output, _ = self.run_case(
            create_helper=False, init_mode=0o666,
        )
        self.assertIn("helpers/__init__.py", package_output)

    def test_rejects_unrelated_or_unresolved_imports(self):
        for case in (
            {"source": "# from helpers import status\n"},
            {"source": "from helpers import status\n", "create_helper": False},
            {"source": "def f():\n    from helpers import status\n"},
            {"source": "from helpers import status\n", "symlink_package": True},
            {"source": "from helpers import status\n", "helper_mode": 0o444,
             "package_mode": 0o1777},
            {"source": "from helpers import status\n", "script_size": 17000},
            {"source": "x = 1\n" * 400 + "from helpers import status\n"},
            {"source": "import absent\n" * 20 + "from helpers import status\n"},
        ):
            with self.subTest(case=case):
                output, _ = self.run_case(**case)
                self.assertNotIn("Sudo Python import review:", output)

    def test_rejects_ambiguous_sudo_rules(self):
        for rule_template in (
            "    (operator) NOPASSWD: {script} *",
            "    (operator) NOPASSWD: !{script}",
            "    (operator) NOPASSWD: {directory}/*.py",
            "    (operator) NOPASSWD: /usr/bin/python3 {script}",
            "    (operator) NOPASSWD: {script}, !{script}",
        ):
            with self.subTest(rule_template=rule_template):
                output, _ = self.run_case(rule_template=rule_template)
                self.assertNotIn("Sudo Python import review:", output)


if __name__ == "__main__":
    unittest.main()
