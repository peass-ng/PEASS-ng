"""A sudo-approved model loader is reviewed without loading any checkpoint."""

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
REVIEW = re.search(r"^sudo_model_loader_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo model-loader review candidate:"


class SudoModelLoaderTests(unittest.TestCase):
    def run_case(self, runas="root", tag="NOPASSWD: ", suffix="*.pth",
                 wrapper=None, helper=None, writable=True, symlink_wrapper=False,
                 symlink_helper=False, extra_policy="", wrapper_prefix="",
                 repeat_rule=False):
        parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=parent) as tmp:
            base = Path(tmp)
            models = base / "models"
            models.mkdir(mode=0o700)
            if not writable:
                models.chmod(0o500)
            target = base / "evaluate_model"
            script = base / "wrapper" if symlink_wrapper else target
            python_target = base / "evaluate.py"
            python_helper = base / "helper.py" if symlink_helper else python_target
            wrapper = wrapper if wrapper is not None else (
                '#!/bin/sh\n' + wrapper_prefix + 'MODEL_FILE="$1"\n'
                f'PYTHON_SCRIPT="{python_helper}"\n'
                '/usr/bin/python3 "$PYTHON_SCRIPT" "$MODEL_FILE"\n'
            )
            helper = helper if helper is not None else (
                'import sys\nimport torch\n'
                'model_path = sys.argv[1]\n'
                'torch.load(model_path)\n'
            )
            target.write_text(wrapper)
            target.chmod(0o755)
            python_target.write_text(helper)
            if symlink_wrapper:
                script.symlink_to(target)
            if symlink_helper:
                python_helper.symlink_to(python_target)
            policy = (f"    ({runas}) {tag}{script} {models}/{suffix}\n"
                      + extra_policy)
            if repeat_rule:
                policy += f"    ({runas}) {tag}{script} {models}/{suffix}\n"
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_model_loader_review "$1"', "sh", policy],
                capture_output=True, text=True, timeout=5,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stderr, "")
            return result.stdout, base

    def test_root_sudo_and_forwarded_model_yield_one_cue(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                output, _ = self.run_case(runas=runas)
                self.assertEqual(output.count(MARKER), 1)
                self.assertIn("loader at line 4", output)
                self.assertIn("weights_only behavior", output)
        for ext in ("pt", "ckpt"):
            with self.subTest(ext=ext):
                output, _ = self.run_case(suffix=f"*.{ext}")
                self.assertIn(MARKER, output)

    def test_static_only_and_no_model_contents_read(self):
        with tempfile.TemporaryDirectory() as tmp:
            marker = Path(tmp) / "executed"
            helper = (f'open({str(marker)!r}, "w").write("executed")\n'
                      'import torch\nstate = torch.load("model.pth")\n')
            output, _ = self.run_case(
                helper=helper, wrapper_prefix=f'touch "{marker}"\n')
            self.assertIn(MARKER, output)
            self.assertFalse(marker.exists())

    def test_nonroot_denied_fixed_and_noexec_suppressed(self):
        for kwargs in (
            {"runas": "builder"}, {"runas": "ALL, !root"},
            {"tag": "NOEXEC: "}, {"tag": "! "},
            {"suffix": "fixed.pth"},
            {"extra_policy": "    (root) !ALL\n"},
            {"extra_policy": "        --another-argument\n"},
            {"writable": False},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertEqual(self.run_case(**kwargs)[0], "")

    def test_missing_evidence_and_symlinks_suppressed(self):
        default_helper = ('import torch\nstate = torch.load("model.pth")\n')
        for kwargs in (
            {"wrapper": '#!/bin/sh\n/usr/bin/python3 /tmp/a.py "$1"\n'},
            {"helper": 'print("torch.load(x)")\n'},
            {"helper": 'import torch\n# torch.load(model_path)\n'},
            {"helper": default_helper + ("# padding\n" * 201)},
            {"helper": default_helper + ("x" * 65536)},
            {"symlink_wrapper": True},
            {"symlink_helper": True},
            {"extra_policy": "x" * 2049 + "\n"},
            {"extra_policy": "x\n" * 3001},
        ):
            with self.subTest(kwargs=str(kwargs)[:70]):
                self.assertEqual(self.run_case(**kwargs)[0], "")

    def test_repeated_identical_grants_deduplicate(self):
        output, _ = self.run_case(repeat_rule=True)
        self.assertEqual(output.count(MARKER), 1)

    def test_module_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
