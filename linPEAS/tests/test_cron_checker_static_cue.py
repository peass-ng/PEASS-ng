"""Bounded static review of a conventional scheduled checker script."""

import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/7_Cron_jobs.sh"
)
SOURCE = MODULE.read_text(encoding="utf-8").split('\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1)[0]
VULNERABLE = '''#!/bin/sh
slapper (){
  SLAPPER_FILES="${ROOTDIR}tmp/.bugtraq ${ROOTDIR}tmp/update"
  file_port=
  for i in ${SLAPPER_FILES}; do
    if [ -f ${i} ]; then
      file_port=$file_port $i
      STATUS=1
    fi
  done
}
'''


class CronCheckerStaticCueTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.path = Path(temp.name) / "chkrootkit"

    def probe(self, content=VULNERABLE, path=None):
        self.path.write_text(content, encoding="utf-8")
        target = path or self.path
        result = subprocess.run(
            ["sh", "-c", SOURCE + '\ncron_chkrootkit_static_probe "$@"\n',
             "sh", str(target)],
            env=os.environ.copy(), capture_output=True, text=True, timeout=4,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_literal_loop_is_candidate_without_executing_checker(self):
        marker = self.path.parent / "ran"
        body = VULNERABLE.replace("  file_port=", f"  touch {marker}\n  file_port=")
        output = self.probe(body)
        self.assertEqual(output.count("review candidate"), 1)
        self.assertIn(str(self.path), output)
        self.assertIn("verify root scheduling", output)
        self.assertFalse(marker.exists())
        self.assertNotIn("SLAPPER_FILES", output)

    def test_patched_and_unrelated_forms_are_silent(self):
        cases = (
            VULNERABLE.replace("file_port=$file_port $i", 'file_port="$file_port $i"'),
            VULNERABLE.replace("file_port=$file_port $i", "# file_port=$file_port $i"),
            VULNERABLE.replace("slapper (){", "other (){"),
            VULNERABLE.replace("${SLAPPER_FILES}", "${OTHER_FILES}"),
            VULNERABLE.replace("[ -f ${i} ]", "[ -d ${i} ]"),
            VULNERABLE.replace("tmp/", "var/"),
            VULNERABLE.replace("file_port=$file_port $i", "file_port=$file_port $other"),
            VULNERABLE.replace("      file_port=$file_port $i\n", "")
                      .replace("  done\n", "  done\n  file_port=$file_port $i\n"),
            VULNERABLE.replace("      file_port=$file_port $i\n", "      :\n      :\n      :\n      file_port=$file_port $i\n"),
        )
        for content in cases:
            with self.subTest(content=content[-90:]):
                self.assertEqual("", self.probe(content))

    def test_symlink_and_oversize_are_silent(self):
        self.path.write_text(VULNERABLE, encoding="utf-8")
        alias = self.path.with_name("alias")
        alias.symlink_to(self.path)
        self.assertEqual("", self.probe(path=alias))
        self.assertEqual("", self.probe(VULNERABLE + "#" * 65537))

    def test_module_syntax_in_available_shells(self):
        for shell in ("sh", "bash", "dash", "ksh"):
            binary = shutil.which(shell)
            if binary:
                with self.subTest(shell=shell):
                    result = subprocess.run(
                        [binary, "-n"], input=SOURCE, text=True,
                        capture_output=True, timeout=3,
                    )
                    self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
