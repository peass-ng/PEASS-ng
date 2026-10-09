"""A cron-consumed script can be replaced through a writable parent directory."""

import os
import pwd
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/7_Cron_jobs.sh")
MARKER = "Cron script replacement review candidate"


@unittest.skipUnless(shutil.which("timeout") or shutil.which("gtimeout"), "timeout required")
@unittest.skipIf(os.geteuid() == 0, "permission test requires an unprivileged user")
class CronReplaceableScriptTests(unittest.TestCase):
    def setUp(self):
        # Docker Desktop bind mounts can report mode-0444 files as writable
        # to the mapped container UID. Use the container's own filesystem on
        # Linux; macOS /tmp is a symlink and the probe rejects symlink paths.
        fixture_dir = "/tmp" if sys.platform.startswith("linux") else MODULE.parent
        self.temp = tempfile.TemporaryDirectory(dir=fixture_dir)
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.parent = self.base / "scripts"
        self.parent.mkdir(mode=0o777)
        self.parent.chmod(0o777)
        self.script = self.parent / "task.sh"
        self.script.write_text('#!/bin/sh\ntrue\n')
        self.script.chmod(0o444)
        self.cron = self.base / "crontab"

    def run_probe(self, runas="root"):
        self.cron.write_text(f"* * * * * {runas} /bin/sh {self.script}\n")
        source = MODULE.read_text().split('\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1)[0]
        result = subprocess.run(
            ["sh", "-c", source + '\ncron_replaceable_script_probe "$@"\n',
             "sh", str(self.cron)],
            capture_output=True, text=True, timeout=6,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_read_only_script_under_replaceable_parent_reports(self):
        output = self.run_probe()
        self.assertEqual(output.count(MARKER), 1, output)
        self.assertIn("current user can replace its directory entry", output)
        self.assertEqual(self.script.read_text(), '#!/bin/sh\ntrue\n')

    def test_sticky_parent_and_same_user_do_not_report(self):
        self.parent.chmod(0o1777)
        self.assertNotIn(MARKER, self.run_probe())
        self.parent.chmod(0o777)
        self.assertNotIn(MARKER, self.run_probe(pwd.getpwuid(os.geteuid()).pw_name))

    def test_writable_script_does_not_duplicate_parent_cue(self):
        self.script.chmod(0o666)
        self.assertNotIn(MARKER, self.run_probe())

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
