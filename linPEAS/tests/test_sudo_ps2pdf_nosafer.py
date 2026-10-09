"""Only bounded exact root sudo scripts with unsafe PostScript conversion get a cue."""

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
REVIEW = re.search(r"^sudo_ps2pdf_nosafer_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo PostScript conversion review candidate:"
SCRIPT = ('#!/usr/bin/env python3\n'
          'secret = "DO_NOT_PRINT_THIS_SECRET"\n'
          'subprocess.check_output(["ps2pdf", "-dNOSAFER", source, output])\n')


class SudoPs2pdfNosaferTests(unittest.TestCase):
    def scan(self, script=SCRIPT, runas="root", args="", tag="NOPASSWD: ",
             extra="", symlink=False, parent_symlink=False, executable=True):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            actual_dir = root / "actual"
            actual_dir.mkdir()
            path_dir = root / "alias" if parent_symlink else actual_dir
            if parent_symlink:
                path_dir.symlink_to(actual_dir, target_is_directory=True)
            actual = actual_dir / "label"
            actual.write_text(script)
            actual.chmod(0o755 if executable else 0o644)
            target = path_dir / "label"
            if symlink:
                actual.rename(actual_dir / "real")
                target.symlink_to(actual_dir / "real")
            marker = root / "executed"
            actual_script = actual_dir / ("real" if symlink else "label")
            actual_script.write_text(actual_script.read_text() + f"\nopen('{marker}', 'w')\n")
            rule = f"    ({runas}) {tag}{target}{args}\n" + extra.replace("{script}", str(target))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_ps2pdf_nosafer_review "$1"', "sh", rule],
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "The allowed script must not execute")
            self.assertNotIn("DO_NOT_PRINT_THIS_SECRET", result.stdout)
            return result.stdout

    def test_exact_root_sudo_python_shebang_and_shell_scripts(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                output = self.scan(runas=runas)
                self.assertEqual(1, output.count(MARKER))
                self.assertIn("flag line 3", output)
        self.assertEqual(1, self.scan(args=" *").count(MARKER))
        self.assertEqual(1, self.scan(script=SCRIPT.replace("/env python3", "/python3")).count(MARKER))
        shell_script = '#!/bin/sh\nps2pdf input.ps output.pdf -dNOSAFER\n'
        self.assertEqual(1, self.scan(script=shell_script).count(MARKER))

    def test_missing_or_nonexecuting_literal_is_not_reported(self):
        for script in (
            SCRIPT.replace("-dNOSAFER", "-dSAFER"),
            SCRIPT.replace("ps2pdf", "ps2pdf_wrapper"),
            SCRIPT.replace("ps2pdf", "ps2pdf-wrapper"),
            SCRIPT.replace("-dNOSAFER", "-dNOSAFER_SOMETHING"),
            SCRIPT.replace("-dNOSAFER", "--dNOSAFER"),
            SCRIPT.replace('subprocess.check_output', '# subprocess.check_output'),
            SCRIPT.replace("#!/usr/bin/env python3", "#!/usr/bin/env ruby"),
            SCRIPT + "x" * 2049 + "\n",
            SCRIPT + "# padding\n" * 401,
            SCRIPT + "x" * 66000,
        ):
            with self.subTest(script=script[-50:]):
                self.assertEqual("", self.scan(script=script))

    def test_ambiguous_or_denied_policy_and_link_are_excluded(self):
        for kwargs in (
            {"runas": "operator"}, {"runas": "ALL, !root"},
            {"args": " --fixed-order"}, {"args": ' ""'},
            {"tag": "NOEXEC: "},
            {"tag": "NOEXEC: /bin/true, "},
            {"symlink": True},
            {"parent_symlink": True}, {"executable": False},
            {"extra": "    (root) ! {script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"extra": "        --ambiguous\n"},
            {"extra": "x" * 2049 + "\n"},
            {"extra": "\n" * 3001},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertEqual("", self.scan(**kwargs))

    def test_policy_script_cap_and_duplicate_rules(self):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            scripts = []
            for n in range(11):
                script = root / f"label{n}"
                script.write_text(SCRIPT)
                script.chmod(0o755)
                scripts.append(script)
            rule = "".join(f"    (root) NOPASSWD: {script}\n" for script in scripts)
            rule += f"    (root) NOPASSWD: {scripts[0]}\n"
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_ps2pdf_nosafer_review "$1"', "sh", rule],
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(10, result.stdout.count(MARKER))
            self.assertNotIn(str(scripts[10]), result.stdout)

    def test_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
