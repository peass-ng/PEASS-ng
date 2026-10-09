"""A root cron Ansible helper may consume playbooks from a writable directory."""

import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/7_Cron_jobs.sh")
MARKER = "Cron Ansible playbook review candidate"


@unittest.skipUnless(shutil.which("timeout") or shutil.which("gtimeout"), "timeout required")
@unittest.skipIf(os.geteuid() == 0, "root has no lower-to-higher privilege transition")
class CronAnsibleGlobTests(unittest.TestCase):
    def setUp(self):
        fixture_dir = "/tmp" if sys.platform.startswith("linux") else MODULE.parent
        self.temp = tempfile.TemporaryDirectory(dir=fixture_dir)
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name)
        self.playbooks = self.base / "tasks"
        self.playbooks.mkdir(mode=0o770)
        self.cron = self.base / "crontab"

    def run_probe(self, line):
        self.cron.write_text(line + "\n")
        source = MODULE.read_text().split('\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1)[0]
        result = subprocess.run(
            ["sh", "-c", source + '\ncron_ansible_glob_probe "$@"\n',
             "sh", str(self.cron)],
            capture_output=True, text=True, timeout=6,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def cron_line(self, glob=None, helper="/usr/local/bin/ansible-parallel", runas="root"):
        if glob is None:
            glob = self.playbooks / "*.yml"
        return f"*/2 * * * * {runas} {helper} {glob}"

    def test_literal_root_glob_and_writable_directory_reports_without_globbing(self):
        output = self.run_probe(self.cron_line())
        self.assertEqual(output.count(MARKER), 1, output)
        self.assertIn("verify helper behavior", output)
        self.assertFalse(list(self.playbooks.iterdir()))

    def test_other_identity_and_non_ansible_helper_do_not_report(self):
        self.assertNotIn(MARKER, self.run_probe(self.cron_line(runas="nobody")))
        self.assertNotIn(MARKER, self.run_probe(self.cron_line(helper="/bin/echo")))

    def test_unwritable_sticky_and_symlinked_directory_do_not_report(self):
        self.playbooks.chmod(0o500)
        self.assertNotIn(MARKER, self.run_probe(self.cron_line()))
        self.playbooks.chmod(0o1777)
        self.assertNotIn(MARKER, self.run_probe(self.cron_line()))
        self.playbooks.chmod(0o770)
        link = self.base / "linked"
        link.symlink_to(self.playbooks, target_is_directory=True)
        self.assertNotIn(MARKER, self.run_probe(self.cron_line(link / "*.yaml")))

    def test_rejects_extra_arguments_and_oversized_schedule(self):
        self.assertNotIn(MARKER, self.run_probe(self.cron_line() + " --extra-vars=x"))
        self.assertIn("8 KiB schedule limit", self.run_probe("#" + "x" * 8192))

    def test_missing_timeout_is_silent(self):
        self.cron.write_text(self.cron_line() + "\n")
        source = MODULE.read_text().split('\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1)[0]
        empty_path = self.base / "empty-path"
        empty_path.mkdir()
        result = subprocess.run(
            ["/bin/sh", "-c", source + '\ncron_ansible_glob_probe "$@"\n',
             "sh", str(self.cron)],
            capture_output=True, text=True, timeout=6,
            env={"PATH": str(empty_path)},
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
