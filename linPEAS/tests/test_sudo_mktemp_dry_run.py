"""Only exact root sudo scripts with a bounded temporary-name race get a cue."""

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
REVIEW = re.search(r"^sudo_mktemp_dry_run_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo temporary-file race review candidate:"
SCRIPT = ("#!/bin/bash\n"
          "tmpName=$(mktemp -u /tmp/ssh-XXXXXXXX)\n"
          "(umask 110; touch $tmpName)\n"
          "/bin/echo DO_NOT_PRINT_THIS_SECRET >>$tmpName\n"
          "/bin/cat $tmpName >>/root/.ssh/authorized_keys\n")


class SudoMktempDryRunTests(unittest.TestCase):
    def scan(self, script=SCRIPT, runas="root", args="", tag="NOPASSWD: ",
             extra="", file_symlink=False, parent_symlink=False, executable=True):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            actual_dir = root / "actual"
            actual_dir.mkdir()
            shown_dir = root / "alias" if parent_symlink else actual_dir
            if parent_symlink:
                shown_dir.symlink_to(actual_dir, target_is_directory=True)
            actual = actual_dir / "helper"
            actual.write_text(script)
            actual.chmod(0o755 if executable else 0o644)
            target = shown_dir / "helper"
            if file_symlink:
                actual.rename(actual_dir / "real")
                target.symlink_to(actual_dir / "real")
            marker = root / "executed"
            actual_script = actual_dir / ("real" if file_symlink else "helper")
            actual_script.write_text(actual_script.read_text() +
                                     f"\nprintf x > {marker}\n")
            policy = f"    ({runas}) {tag}{target}{args}\n"
            policy += extra.replace("{script}", str(target))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_mktemp_dry_run_review "$1"', "sh", policy],
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "Privileged script must not run")
            self.assertNotIn("DO_NOT_PRINT_THIS_SECRET", result.stdout)
            return result.stdout

    def test_exact_root_script_reports_path_and_sink_line(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                output = self.scan(runas=runas)
                self.assertEqual(1, output.count(MARKER))
                self.assertIn("sink line 5", output)

    def test_safe_or_unrelated_script_does_not_match(self):
        variants = (
            SCRIPT.replace("mktemp -u", "mktemp"),
            SCRIPT.replace("mktemp -u", "mktemp --directory"),
            SCRIPT.replace("touch $tmpName", "touch $different"),
            SCRIPT.replace("/bin/cat $tmpName", "/bin/cat $different"),
            SCRIPT.replace("/root/.ssh/authorized_keys", "/home/user/out"),
            SCRIPT.replace("/tmp/ssh-XXXXXXXX", "/root/ssh-XXXXXXXX"),
            SCRIPT.replace("tmpName=$(mktemp -u /tmp/ssh-XXXXXXXX)",
                           "# tmpName=$(mktemp -u /tmp/ssh-XXXXXXXX)"),
            SCRIPT.replace("/bin/cat $tmpName >>/root/.ssh/authorized_keys\n",
                           "tmpName=$(mktemp /tmp/safe-XXXXXXXX)\n"
                           "/bin/cat $tmpName >>/root/.ssh/authorized_keys\n"),
            SCRIPT.replace("/bin/cat $tmpName >>/root/.ssh/authorized_keys\n", "") +
                "x" * 2049 + "\n",
            SCRIPT + "# padding\n" * 201,
            SCRIPT + "x" * 33000,
        )
        for script in variants:
            with self.subTest(script=script[-70:]):
                self.assertEqual("", self.scan(script=script))

    def test_policy_and_path_boundaries(self):
        variants = (
            {"runas": "operator"}, {"runas": "ALL, !root"},
            {"args": " --fixed"}, {"args": " *"},
            {"tag": "NOEXEC: "}, {"tag": "NOEXEC: /bin/true, "},
            {"extra": "    (root) !{script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"extra": "        --continued\n"},
            {"extra": "x" * 2049 + "\n"},
            {"extra": "x\n" * 3001},
            {"file_symlink": True}, {"parent_symlink": True},
            {"executable": False},
        )
        for kwargs in variants:
            with self.subTest(kwargs=kwargs):
                self.assertEqual("", self.scan(**kwargs))

    def test_module_shell_syntax(self):
        for shell in ("sh", "bash"):
            if shutil.which(shell) is None:
                continue
            with self.subTest(shell=shell):
                result = subprocess.run([shell, "-n", str(MODULE)],
                                        text=True, capture_output=True, timeout=3)
                self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
