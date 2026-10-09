"""The GitPython sudo cue is static, bounded, and tied to an exact root grant."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
SOURCE = MODULE.read_text()
PATH_HELPER = re.search(r"^sudo_python_import_plain_path\(\) \{\n.*?^\}",
                        SOURCE, re.MULTILINE | re.DOTALL).group()
REVIEW = re.search(r"^sudo_gitpython_clone_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "GitPython privileged clone review candidate:"
PARSER = next((p for p in ("/usr/bin/python3", "/usr/local/bin/python3")
               if os.path.isfile(p) and os.access(p, os.X_OK)), None)
DEFAULT_SOURCE = (
    "import sys\n"
    "from git import Repo\n"
    "url_to_clone = sys.argv[1]\n"
    "r = Repo.init('', bare=True)\n"
    'r.clone_from(url_to_clone, "new_changes", '
    'multi_options=["-c protocol.ext.allow=always"])\n'
)


@unittest.skipUnless(PARSER, "system Python 3 parser unavailable")
class SudoGitPythonCloneTests(unittest.TestCase):
    def run_case(self, source=DEFAULT_SOURCE, runas="root", suffix=" *",
                 tag="NOPASSWD: ", extra_policy="", symlink=False,
                 oversized=False, python_name="python3"):
        temp_parent = "/private/tmp" if os.path.isdir("/private/tmp") else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            base = Path(tmp)
            script = base / "clone.py"
            actual = base / "actual.py" if symlink else script
            actual.write_text(source + ("# padding\n" * 15000 if oversized else ""))
            if symlink:
                script.symlink_to(actual)
            binary = Path(PARSER).with_name(python_name)
            if not binary.is_file():
                binary = Path(PARSER)
            policy = (f"    ({runas}) {tag}{binary} {script}{suffix}\n"
                      + extra_policy)
            code = (PATH_HELPER + "\n" + REVIEW +
                    '\nsudo_gitpython_clone_review "$1"')
            result = subprocess.run(["sh", "-c", code, "sh", policy],
                                    capture_output=True, text=True, timeout=6)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stderr, "")
            self.assertNotIn("protocol.ext.allow=always", result.stdout)
            return result.stdout

    def test_exact_root_grant_and_literal_dataflow(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                out = self.run_case(runas=runas)
                self.assertEqual(out.count(MARKER), 1)
                self.assertIn("version, patch status", out)
        direct = DEFAULT_SOURCE.replace("url_to_clone = sys.argv[1]\n", "")
        direct = direct.replace("url_to_clone,", "sys.argv[1],")
        self.assertIn(MARKER, self.run_case(source=direct))

    def test_never_executes_python_source_or_prints_its_secrets(self):
        with tempfile.TemporaryDirectory() as tmp:
            marker = Path(tmp) / "executed"
            source = (f"open({str(marker)!r}, 'w').write('secret-value')\n"
                      + DEFAULT_SOURCE)
            self.assertIn(MARKER, self.run_case(source=source))
            self.assertFalse(marker.exists())

    def test_rejects_nonroot_denied_and_constrained_policy(self):
        for kwargs in (
            {"runas": "builder"},
            {"runas": "ALL, !root"},
            {"runas": "root, !root"},
            {"suffix": ""},
            {"suffix": " /opt/fixed.git"},
            {"tag": "NOEXEC: "},
            {"tag": "! "},
            {"extra_policy": "    (root) !ALL\n"},
            {"extra_policy": "    (root) NOEXEC: /usr/bin/true\n"},
            {"extra_policy": "x" * 2049 + "\n"},
            {"extra_policy": "x\n" * 3001},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertEqual("", self.run_case(**kwargs))

    def test_rejects_missing_or_ambiguous_source_evidence(self):
        variants = (
            DEFAULT_SOURCE.replace("from git import Repo", "import json"),
            DEFAULT_SOURCE.replace("sys.argv[1]", "'fixed.git'"),
            DEFAULT_SOURCE.replace("-c protocol.ext.allow=always", "-c protocol.ext.allow=never"),
            DEFAULT_SOURCE.replace("r.clone_from(", "other.clone_from("),
            DEFAULT_SOURCE.replace("r.clone_from(", "# r.clone_from("),
            DEFAULT_SOURCE.replace("r.clone_from(", "text = 'r.clone_from(' # "),
            DEFAULT_SOURCE.replace("url_to_clone = sys.argv[1]", "url_to_clone = sys.argv[1]\nurl_to_clone = 'fixed.git'"),
            DEFAULT_SOURCE.replace("r = Repo.init('', bare=True)", "r = object()"),
            DEFAULT_SOURCE.replace("multi_options=[\"-c protocol.ext.allow=always\"]", "multi_options=options"),
        )
        for source in variants:
            with self.subTest(source=source[-70:]):
                self.assertEqual("", self.run_case(source=source))
        self.assertEqual("", self.run_case(symlink=True))
        self.assertEqual("", self.run_case(oversized=True))

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
