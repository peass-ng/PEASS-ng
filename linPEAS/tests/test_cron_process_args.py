import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/7_Cron_jobs.sh"
)


@unittest.skipUnless(shutil.which("timeout") or shutil.which("gtimeout"), "timeout required")
class CronProcessArgsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.helper = self.root / "helper.sh"
        self.cron = self.root / "system-cron"
        self.command = f"* * * * * root /bin/sh {self.helper}"
        self.write_helper(
            "pgrep -lfa apache2 | while read -r pid _cmd; do\n"
            "  cmd=$(printf '%s' \"$_cmd\" | sed 's/apache2/apache2ctl/')\n"
            "  $cmd -t\n"
            "done"
        )

    def write_helper(self, body):
        self.helper.write_text("#!/bin/sh\n" + body + "\n", encoding="utf-8")

    def run_probe(self, schedule=None, paths=None):
        self.cron.write_text((schedule or self.command) + "\n", encoding="utf-8")
        source = MODULE.read_text(encoding="utf-8").split(
            '\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1
        )[0]
        result = subprocess.run(
            ["sh", "-c", source + '\ncron_process_args_probe "$@"\n', "sh", *(str(p) for p in (paths or [self.cron]))],
            env=os.environ.copy(),
            capture_output=True,
            text=True,
            timeout=8,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_joined_root_helper_and_apache_option_flow(self):
        output = self.run_probe()
        self.assertEqual(output.count("review candidate"), 1)
        self.assertIn(f"Schedule owner: root; helper: {self.helper}", output)
        self.assertIn("Full-args pgrep/read: helper line 2", output)
        self.assertIn("Apache config-test option flow observed", output)
        self.assertNotIn("apache2ctl/')", output)

    def test_root_spool_and_generic_process_command(self):
        spool = self.root / "spool/cron/root"
        spool.parent.mkdir(parents=True)
        self.write_helper("pgrep -af worker | while read -r pid args; do\n"
                          "  command=$(printf '%s' \"$args\" | sed 's/worker/check/')\n"
                          "  $command\n"
                          "done")
        output = self.run_probe(self.command.replace(" root ", " "), [spool])
        self.assertNotIn("review candidate", output)  # no schedule at the spool path yet
        spool.write_text(self.command.replace(" root ", " ") + "\n", encoding="utf-8")
        output = self.run_probe(paths=[spool])
        self.assertIn("review candidate", output)
        self.assertNotIn("Apache config-test", output)

    def test_config_test_option_appended_before_execution(self):
        self.write_helper(
            "pgrep -lfa apache2 | while read -r pid _cmd; do\n"
            "  cmd=$(printf '%s' \"$_cmd\" | sed 's/apache2/apache2ctl/')\n"
            "  cmd=\"$cmd -t\"\n"
            "  $cmd\n"
            "done"
        )
        self.assertIn("Apache config-test option flow observed", self.run_probe())

    def test_helper_is_read_without_running_it(self):
        marker = self.root / "ran"
        self.write_helper(
            f"touch {marker}\n"
            "pgrep -lfa apache2 | while read -r pid _cmd; do\n"
            "  cmd=$(echo \"$_cmd\" | sed 's/apache2/apache2ctl/')\n"
            "  $cmd -t\n"
            "done"
        )
        self.assertIn("review candidate", self.run_probe())
        self.assertFalse(marker.exists())

    def test_unjoined_and_unprivileged_are_excluded(self):
        cases = (
            "printf '%s\\n' 'pgrep -lfa apache2 | while read pid _cmd'\ncmd=apache2ctl\n$cmd -t",
            "pgrep -lfa apache2 | while read -r pid _cmd; do\n  printf '%s\\n' \"$_cmd\"\ndone",
            "pgrep -lfa apache2 | while read -r pid _cmd; do\n  cmd=apache2ctl\n  $cmd -t\ndone",
            "pgrep -lfa apache2 | while read -r pid _cmd; do\n  cmd=$(printf '%s' \"$_cmd\" | sed 's/apache2/apache2ctl/')\n  readlink /proc/$pid/exe >/dev/null\n  apache2ctl -t\ndone",
            "pgrep -lfa apache2 | while read -r pid _cmd; do\n  cmd=$(echo \"$_cmd\" | sed 's/apache2/apache2ctl/')\n  $cmd2 -t\ndone",
        )
        for body in cases:
            with self.subTest(body=body):
                self.write_helper(body)
                self.assertNotIn("review candidate", self.run_probe())
        self.write_helper("pgrep -lfa apache2 | while read -r pid _cmd; do\n"
                          "cmd=$(echo \"$_cmd\" | sed 's/apache2/apache2ctl/')\n$cmd -t\ndone")
        self.assertNotIn("review candidate", self.run_probe(self.command.replace(" root ", " user ")))

    def test_caps_and_unreadable_helper(self):
        self.write_helper("#" * 4100)
        self.assertIn("unknown (oversized helper", self.run_probe())
        self.helper.unlink()
        self.helper.symlink_to(self.root / "elsewhere")
        self.assertIn("unknown (unreadable helper", self.run_probe())
        self.helper.unlink()
        self.assertIn("unknown (unreadable helper", self.run_probe())
        self.assertIn("unknown (oversized schedule", self.run_probe("#" * 8200))
        self.assertIn("unknown beyond 6 schedule files", self.run_probe(paths=[self.cron] * 7))

    @unittest.skipUnless(sys.platform.startswith("linux"), "AppImage is Linux-specific")
    @unittest.skipIf(os.geteuid() == 0, "writability must be tested as an unprivileged user")
    def test_imagemagick_appimage_writable_cwd_is_passive_candidate(self):
        cwd = self.root / "images"
        cwd.mkdir()
        magick = self.root / "magick"
        magick.write_bytes(b"\x7fELF" + b"\x00" * 4 + b"AI\x02" + b"\x00" * 20)
        magick.chmod(0o755)
        self.write_helper(
            f"cd {cwd}\n"
            "truncate -s 0 metadata.log\n"
            f"find {cwd} -type f -name '*.jpg' | xargs {magick} identify >> metadata.log"
        )
        output = self.run_probe()
        self.assertIn("Cron ImageMagick working-directory review candidate", output)
        self.assertIn("AppImage type-2 marker found", output)
        self.assertIn("cd line 2; identify line 4", output)
        self.assertFalse((cwd / "metadata.log").exists())

        self.assertNotIn("ImageMagick working-directory review candidate",
                         self.run_probe(self.command.replace(" root ", " user ")))
        cwd.chmod(0o500)
        self.assertNotIn("ImageMagick working-directory review candidate", self.run_probe())

    @unittest.skipUnless(sys.platform.startswith("linux"), "AppImage is Linux-specific")
    @unittest.skipIf(os.geteuid() == 0, "writability must be tested as an unprivileged user")
    def test_imagemagick_native_and_ambiguous_helpers_are_not_confirmed(self):
        cwd = self.root / "images"
        cwd.mkdir()
        magick = self.root / "magick"
        marker = self.root / "ran"
        magick.write_text(f"#!/bin/sh\ntouch {marker}\n", encoding="utf-8")
        magick.chmod(0o755)
        self.write_helper(f"cd {cwd}\n{magick} identify image.jpg")
        output = self.run_probe()
        self.assertIn("ImageMagick working-directory review candidate", output)
        self.assertIn("AppImage format not confirmed", output)
        self.assertFalse(marker.exists())

        for body in (
            f"{magick} identify image.jpg",  # no working-directory transition
            f"cd {cwd}\n{magick} version",  # no identify operation
            f"cd {cwd}\necho {magick} identify image.jpg",  # printed command
            f"cd {cwd}; echo unsafe\n{magick} identify image.jpg",  # compound cd
            f"cd {cwd}\n# {magick} identify image.jpg",  # comment
            f"cd {cwd}\n" + ("# padding\n" * 34) + f"{magick} identify image.jpg",
        ):
            with self.subTest(body=body):
                self.write_helper(body)
                self.assertNotIn("ImageMagick working-directory review candidate", self.run_probe())


if __name__ == "__main__":
    unittest.main()
