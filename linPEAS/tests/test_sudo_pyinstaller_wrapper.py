"""A root-capable sudo wrapper must actually pass a caller argument to PyInstaller."""

import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
SOURCE = MODULE.read_text()
PATH_HELPER = re.search(r"^sudo_python_import_plain_path\(\) \{\n.*?^\}",
                        SOURCE, re.MULTILINE | re.DOTALL).group()
REVIEW = re.search(r"^sudo_pyinstaller_wrapper_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo PyInstaller wrapper review candidate:"
WRAPPER = ('#!/bin/bash\n'
           'name=$2\n'
           'ext=$(printf "%s" "$2" | awk -F. "{print $NF}")\n'
           'if [[ "$ext" == "spec" ]]; then\n'
           '  /usr/bin/pyinstaller $name\n'
           'fi\n')


class SudoPyInstallerWrapperTests(unittest.TestCase):
    def scan(self, source=WRAPPER, runas="root", args="", tag="NOPASSWD: ",
             extra="", symlink=False, executable=True):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as temp:
            root = Path(temp)
            script = root / "build-app.sh"
            actual = root / "actual.sh" if symlink else script
            marker = root / "executed"
            actual.write_text(source + f'\ntouch "{marker}"\n')
            actual.chmod(0o755 if executable else 0o644)
            if symlink:
                script.symlink_to(actual)
            rule = (f"    ({runas}) {tag}{script}{args}\n" +
                    extra.replace("{script}", str(script)))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_pyinstaller_wrapper_review "$1"', "sh", rule],
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "The privileged wrapper must never run")
            return result.stdout

    def test_exact_root_unrestricted_rule_and_no_content_disclosure(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                output = self.scan(runas=runas)
                self.assertEqual(1, output.count(MARKER))
                self.assertNotIn("touch", output)
                self.assertIn("build identity", output)
        self.assertEqual(1, self.scan(args=" *").count(MARKER))
        self.assertEqual(1, self.scan(source="#!/bin/sh\n"
                                      'if [ "$1" = "build.spec" ]; then\n'
                                      '  pyinstaller "$2"\n').count(MARKER))

    def test_policy_denials_and_file_boundaries(self):
        for kwargs in (
            {"runas": "builder"},
            {"runas": "ALL, !root"},
            {"args": " fixed.spec"},
            {"tag": "NOEXEC: "},
            {"extra": "    (root) ! {script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"extra": "    (root) ! /private/tmp/*\n"},
            {"extra": "        continuation\n"},
            {"symlink": True},
            {"executable": False},
            {"source": WRAPPER + "#" * 65536},
            {"source": WRAPPER + "x\n" * 401},
        ):
            with self.subTest(kwargs=str(kwargs)[:80]):
                self.assertEqual("", self.scan(**kwargs))

    def test_requires_spec_and_caller_argument_reaching_build(self):
        for source in (
            WRAPPER.replace('"spec"', '"py"'),
            WRAPPER.replace("name=$2", "name=$3"),
            WRAPPER.replace("/usr/bin/pyinstaller $name",
                            "echo /usr/bin/pyinstaller $name"),
            "#!/bin/sh\n# spec\npyinstaller \"$2\"\n",
            "#!/bin/sh\n# pyinstaller \"$2\"\necho spec\n",
            "#!/bin/sh\necho spec\npyinstaller \"$2\"\n",
        ):
            with self.subTest(source=source[:70]):
                self.assertEqual("", self.scan(source=source))

    def test_deduplicates_rules(self):
        output = self.scan(extra="    (root) {script}\n")
        self.assertEqual(1, output.count(MARKER))


if __name__ == "__main__":
    unittest.main()
