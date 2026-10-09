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
PHP_MARKER = "Cron PHP helper review candidate"
PHP_SCRIPT_MARKER = "Cron PHP script review candidate"


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

    def run_php_probe(self, script=None, runas="root"):
        script = script or self.script
        self.cron.write_text(f"* * * * * {runas} {script}\n")
        source = MODULE.read_text().split('\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1)[0]
        result = subprocess.run(
            ["sh", "-c", source + '\ncron_replaceable_script_probe "$@"\n',
             "sh", str(self.cron)],
            capture_output=True, text=True, timeout=6,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def run_php_cli_probe(self, command, runas="root"):
        self.cron.write_text(f"* * * * * {runas} {command}\n")
        source = MODULE.read_text().split('\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1)[0]
        result = subprocess.run(
            ["sh", "-c", source + '\ncron_replaceable_script_probe "$@"\n',
             "sh", str(self.cron)],
            capture_output=True, text=True, timeout=6,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def write_php_wrapper(self, source):
        self.script.chmod(0o644)
        self.script.write_text(source)
        self.script.chmod(0o444)

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

    def test_php_wrapper_calls_absent_helper_in_writable_directory(self):
        helper = self.parent / "helper.sh"
        wrapper = self.parent / "maintenance"
        wrapper.write_text(f"#!/usr/bin/php\n<?php\nexec('{helper}');\n")
        wrapper.chmod(0o555)
        output = self.run_php_probe(wrapper)
        self.assertEqual(output.count(PHP_MARKER), 1, output)
        self.assertIn(str(helper), output)
        self.assertNotIn("<?php", output)

    def test_php_helper_requires_root_and_missing_path(self):
        helper = self.parent / "helper.sh"
        self.write_php_wrapper(f"#!/usr/bin/php\n<?php\nexec('{helper}');\n")
        current_user = pwd.getpwuid(os.geteuid()).pw_name
        self.assertNotIn(PHP_MARKER, self.run_php_probe(runas=current_user))
        helper.write_text("#!/bin/sh\ntrue\n")
        self.assertNotIn(PHP_MARKER, self.run_php_probe())
        helper.unlink()
        helper.symlink_to(self.script)
        self.assertNotIn(PHP_MARKER, self.run_php_probe())

    def test_php_helper_requires_writable_nonsticky_parent(self):
        helper = self.parent / "helper.sh"
        self.write_php_wrapper(f"#!/usr/bin/php\n<?php\nexec('{helper}');\n")
        self.parent.chmod(0o555)
        self.assertNotIn(PHP_MARKER, self.run_php_probe())
        self.parent.chmod(0o1777)
        self.assertNotIn(PHP_MARKER, self.run_php_probe())

    def test_php_wrapper_rejects_nonliteral_or_oversized_source(self):
        helper = self.parent / "helper.sh"
        for source in (f"#!/bin/sh\nexec('{helper}');\n",
                       f"#!/usr/bin/php\n<?php\nexec($helper);\n",
                       f"#!/usr/bin/php\n<?php\n// exec('{helper}');\n",
                       f"#!/usr/bin/php\n<?php\nexec('{helper}');\n" + "x" * 4096):
            self.write_php_wrapper(source)
            self.assertNotIn(PHP_MARKER, self.run_php_probe())

    def test_php_wrapper_rejects_symlinked_ancestor(self):
        helper = self.parent / "helper.sh"
        self.write_php_wrapper(f"#!/usr/bin/php\n<?php\nexec('{helper}');\n")
        alias = self.base / "alias"
        alias.symlink_to(self.parent, target_is_directory=True)
        self.assertNotIn(PHP_MARKER, self.run_php_probe(alias / self.script.name))
        self.write_php_wrapper(f"#!/usr/bin/php\n<?php\nexec('{alias / 'helper.sh'}');\n")
        self.assertNotIn(PHP_MARKER, self.run_php_probe())

    def test_root_php_cli_with_literal_argument_and_redirect_reports_writable_script(self):
        php_script = self.parent / "task.php"
        php_script.write_text("<?php\n")
        php_script.chmod(0o666)
        for command in (f"php {php_script}",
                        f"php {php_script} schedule:run >> /dev/null 2>&1"):
            output = self.run_php_cli_probe(command)
            self.assertEqual(output.count(PHP_SCRIPT_MARKER), 1, output)
            self.assertIn(str(php_script), output)
            self.assertNotIn("<?php", output)

    def test_php_cli_rejects_nonroot_unwritable_quoted_wildcard_and_symlink_paths(self):
        php_script = self.parent / "task.php"
        php_script.write_text("<?php\n")
        php_script.chmod(0o666)
        command = f"php {php_script} schedule:run"
        current_user = pwd.getpwuid(os.geteuid()).pw_name
        self.assertNotIn(PHP_SCRIPT_MARKER,
                         self.run_php_cli_probe(command, runas=current_user))

        php_script.chmod(0o444)
        self.assertNotIn(PHP_SCRIPT_MARKER, self.run_php_cli_probe(command))
        php_script.chmod(0o666)
        for unsafe in (f"php '{php_script}' schedule:run",
                       f"php {self.parent}/*.php schedule:run",
                       f"php {php_script} 'schedule:run'"):
            self.assertNotIn(PHP_SCRIPT_MARKER, self.run_php_cli_probe(unsafe))

        alias = self.base / "alias"
        alias.symlink_to(self.parent, target_is_directory=True)
        self.assertNotIn(PHP_SCRIPT_MARKER,
                         self.run_php_cli_probe(f"php {alias / php_script.name} schedule:run"))

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
