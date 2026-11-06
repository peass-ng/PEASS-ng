import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


SOURCE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/functions/search_for_regex.sh"


class RegexSearchRuntimeTests(unittest.TestCase):
    def test_batches_grep_and_prunes_node_modules(self):
        with tempfile.TemporaryDirectory(prefix="linpeas-regex-") as tmp:
            root = Path(tmp) / "scan with spaces"
            root.mkdir()
            for number in range(80):
                (root / f"sample-{number:03}.txt").write_text("TOKEN_123\n")
            excluded = root / "node_modules" / "nested"
            excluded.mkdir(parents=True)
            (excluded / "secret.txt").write_text("TOKEN_999\n")

            wrappers = Path(tmp) / "bin"
            wrappers.mkdir()
            count_file = Path(tmp) / "grep-calls"
            grep = shutil.which("grep")
            self.assertIsNotNone(grep)
            (wrappers / "grep").write_text(
                '#!/bin/sh\nprintf "call\\n" >> "$GREP_CALLS"\nexec "' + grep + '" "$@"\n'
            )
            (wrappers / "grep").chmod(0o755)
            timeout = wrappers / "gtimeout"
            timeout.write_text('#!/bin/sh\nshift\nexec "$@"\n')
            timeout.chmod(0o755)

            script = (
                'print_3title_no_nl() { :; }; '
                '. "$REGEX_SOURCE"; '
                'SEARCH_IN_FOLDER=1; ROOT_FOLDER="$SCAN_ROOT"; '
                'TIMEOUT="$TIMEOUT_STUB"; '
                'search_for_regex "tokens" "TOKEN_[0-9]+" ""'
            )
            env = os.environ.copy()
            env.update({
                "PATH": str(wrappers) + os.pathsep + env.get("PATH", ""),
                "GREP_CALLS": str(count_file),
                "REGEX_SOURCE": str(SOURCE),
                "SCAN_ROOT": str(root),
                "TIMEOUT_STUB": str(timeout),
            })
            completed = subprocess.run(["dash", "-c", script], env=env,
                                       capture_output=True, text=True, timeout=10)
            self.assertEqual(0, completed.returncode, completed.stderr)
            self.assertEqual(50, completed.stdout.count("TOKEN_123"))
            self.assertNotIn("TOKEN_999", completed.stdout)
            self.assertLessEqual(len(count_file.read_text().splitlines()), 3)


if __name__ == "__main__":
    unittest.main()
