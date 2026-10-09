"""Static, bounded sudo wrapper-to-Python import review."""

import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
SOURCE = MODULE.read_text()
PATH_HELPER = re.search(r"^sudo_python_import_plain_path\(\) \{\n.*?^\}",
                        SOURCE, re.MULTILINE | re.DOTALL).group()
REVIEW = re.search(r"^sudo_setenv_pythonpath_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo Python environment import review candidate:"


class SudoSetenvPythonpathTests(unittest.TestCase):
    def scan(self, *, wrapper=None, target=None, rule=None, runas="root", target_symlink=False,
             wrapper_symlink=False, target_size=None, wrapper_size=None):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            marker = root / "executed"
            script = root / "maintenance"
            pyfile = root / "backup.py"
            actual_script = root / "actual-maintenance" if wrapper_symlink else script
            actual_pyfile = root / "actual-backup.py" if target_symlink else pyfile
            default_wrapper = f"#!/bin/bash\n{pyfile} &\n"
            default_target = "#!/usr/bin/python3\nfrom shutil import make_archive\n"
            wrapper_source = (wrapper if wrapper is not None else default_wrapper)
            actual_script.write_text(wrapper_source.replace("{target}", str(pyfile))
                                     + f"\ntouch {marker}\n"
                                     + ("x" * wrapper_size if wrapper_size else ""))
            actual_pyfile.write_text((target if target is not None else default_target)
                                     + f"\nopen('{marker}', 'w')\n"
                                     + ("x" * target_size if target_size else ""))
            actual_script.chmod(0o755)
            actual_pyfile.chmod(0o755)
            if wrapper_symlink:
                script.symlink_to(actual_script)
            if target_symlink:
                pyfile.symlink_to(actual_pyfile)
            sudo = rule.format(script=script) if rule else (
                f"Matching Defaults entries for user on host:\n"
                f"    env_reset, secure_path=/usr/bin:/bin\n"
                f"    ({runas}) SETENV: NOPASSWD: {script}\n"
            )
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_setenv_pythonpath_review "$1"', "sh", sudo],
                cwd=root, text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "No wrapper or Python target may execute")
            return result.stdout

    def test_root_setenv_one_hop_import(self):
        output = self.scan()
        self.assertEqual(1, output.count(MARKER))
        self.assertIn("literal shutil import", output)
        self.assertIn("secure_path governs PATH, not PYTHONPATH", output)
        self.assertNotIn("make_archive", output)
        self.assertEqual(1, self.scan(wrapper="#!/bin/sh\n{target} &\n").count(MARKER))
        for runas in ("ALL", "#0"):
            with self.subTest(runas=runas):
                self.assertEqual(1, self.scan(runas=runas).count(MARKER))

    def test_policy_boundaries(self):
        for rule in (
            "    (service) SETENV: {script}\n",
            "    (ALL, !root) SETENV: {script}\n",
            "    (root) NOPASSWD: {script}\n",
            "    (root) SETENV: NOSETENV: {script}\n",
            "    (root) SETENV: NOEXEC: {script}\n",
            "    (root) SETENV: {script} --fixed\n",
            "    (root) SETENV: {script}\n    (root) !{script}\n",
            "    (root) SETENV: {script}\n    (root) !ALL\n",
        ):
            with self.subTest(rule=rule):
                self.assertEqual("", self.scan(rule=rule))

    def test_source_and_path_boundaries(self):
        for option in (
            {"wrapper": "#!/bin/bash\necho /tmp/backup.py\n"},
            {"wrapper": "#!/bin/bash\n# /tmp/backup.py\n"},
            {"wrapper": "#!/bin/bash\n/tmp/backup.py --fixed\n"},
            {"wrapper": "#!/bin/bash\n/usr/bin/python3 -I /tmp/backup.py\n"},
            {"wrapper": "#!/bin/bash\nunset PYTHONPATH\n{target} &\n"},
            {"wrapper": "#!/bin/bash\nPYTHONPATH=/opt/fixed\n{target} &\n"},
            {"wrapper": "#!/bin/bash\n{target} &\n" + "# filler\n" * 201},
            {"wrapper": "#!/bin/bash\n", "wrapper_size": 65536},
            {"target": "#!/usr/bin/python3 -I\nfrom shutil import make_archive\n"},
            {"target": "#!/usr/bin/python3 -E\nfrom shutil import make_archive\n"},
            {"target": "#!/usr/bin/python3\n# from shutil import make_archive\n"},
            {"target": "#!/usr/bin/python3\nimport sys\n"},
            {"target": "#!/usr/bin/python3\n", "target_size": 65536},
            {"target_symlink": True},
            {"wrapper_symlink": True},
        ):
            with self.subTest(option=str(option)[:80]):
                self.assertEqual("", self.scan(**option))

    def test_shell_syntax(self):
        for shell in ("sh", "dash", "bash", "busybox"):
            command = shutil.which(shell)
            if not command:
                continue
            args = [command, "sh", "-n", str(MODULE)] if shell == "busybox" else [command, "-n", str(MODULE)]
            result = subprocess.run(args, text=True, capture_output=True, timeout=3)
            self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
