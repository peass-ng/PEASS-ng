import os
import pwd
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/7_Cron_jobs.sh"
)


@unittest.skipUnless(shutil.which("timeout") or shutil.which("gtimeout"), "timeout required")
@unittest.skipIf(os.geteuid() == 0, "root has no higher privilege cron owner")
class CronTarWildcardTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(
            dir="/private/tmp" if Path("/private/tmp").is_dir() else None
        )
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.bin = self.root / "bin"
        self.bin.mkdir()
        self.tar_log = self.root / "tar-invocations"
        self.install_tar("tar (GNU tar) 1.35")
        self.input = self.root / "writable"
        self.input.mkdir(mode=0o700)
        self.helper = self.root / "backup.sh"
        self.cron = self.root / "system-cron"
        self.command = f"* * * * * root /bin/sh {self.helper}"
        self.write_helper(f"cd {self.input}\ntar -czf /tmp/archive.tgz *")

    def install_tar(self, version):
        stub = self.bin / "tar"
        stub.write_text(
            "#!/bin/sh\n"
            'printf "%s\\n" "$*" >> "$TAR_CALL_LOG"\n'
            f'printf "%s\\n" "{version}"\n',
            encoding="utf-8",
        )
        stub.chmod(0o755)

    def write_helper(self, body):
        if self.helper.exists():
            self.helper.chmod(0o600)
        self.helper.write_text("#!/bin/sh\n" + body + "\n", encoding="utf-8")
        self.helper.chmod(0o444)

    def run_probe(self, schedule=None, cron_file=None, extra_paths=()):
        cron_file = cron_file or self.cron
        cron_file.write_text((schedule or self.command) + "\n", encoding="utf-8")
        source = MODULE.read_text(encoding="utf-8").split(
            '\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1
        )[0]
        env = os.environ.copy()
        env.update(PATH=f"{self.bin}:{env['PATH']}", TAR_CALL_LOG=str(self.tar_log))
        result = subprocess.run(
            ["sh", "-c", source + '\ncron_tar_wildcard_probe "$@"\n', "sh", str(cron_file), *(str(path) for path in extra_paths)],
            env=env,
            capture_output=True,
            text=True,
            timeout=8,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        if self.tar_log.exists():
            self.assertEqual(self.tar_log.read_text(), "--version\n")
            self.tar_log.unlink()
        return result.stdout

    def test_joined_schedule_helper_directory_and_gnu_tar(self):
        output = self.run_probe()
        self.assertEqual(output.count("review candidate"), 1)
        self.assertIn(f"Schedule owner: root", output)
        self.assertIn(str(self.helper), output)
        self.assertIn(str(self.input), output)
        self.assertIn("Candidate only", output)

    def test_readable_user_spool_and_line_cap(self):
        spool = self.root / "spool/cron/root"
        spool.parent.mkdir(parents=True)
        output = self.run_probe(
            self.command.replace(" root ", " "), cron_file=spool
        )
        self.assertEqual(output.count("review candidate"), 1)
        self.assertIn(str(spool), output)
        self.assertIn("unknown beyond schedule line/length cap", self.run_probe("# fixture\n" * 33))
        self.assertIn("unknown beyond 12 schedule files", self.run_probe("# fixture", extra_paths=(self.cron,) * 12))

    def test_quoted_end_of_options_and_other_archiver_are_excluded(self):
        for command in (
            "tar -czf /tmp/archive.tgz '*'",
            "tar -czf /tmp/archive.tgz \"*\"",
            "tar -czf /tmp/archive.tgz -- *",
            "bsdtar -czf /tmp/archive.tgz *",
            "tar -czf /tmp/archive.tgz $FILES",
        ):
            with self.subTest(command=command):
                self.write_helper(f"cd {self.input}\n{command}")
                self.assertNotIn("review candidate", self.run_probe())

    def test_same_user_nonwritable_and_missing_owner_are_excluded(self):
        self.assertNotIn(
            "review candidate",
            self.run_probe(self.command.replace(" root ", f" {pwd.getpwuid(os.geteuid()).pw_name} ")),
        )
        self.assertNotIn(
            "review candidate",
            self.run_probe(self.command.replace(" root ", " nonexistent_owner_53 ")),
        )
        self.input.chmod(0o500)
        self.assertNotIn("review candidate", self.run_probe())

    def test_bsd_tar_and_size_caps(self):
        self.install_tar("bsdtar 3.7")
        self.assertNotIn("review candidate", self.run_probe())
        self.assertIn("unknown (oversized helper", self.run_probe_with_helper("#" * 4100))
        self.assertIn("unknown (oversized schedule", self.run_probe("#" * 8200))

    def run_probe_with_helper(self, body):
        self.write_helper(body)
        return self.run_probe()


if __name__ == "__main__":
    unittest.main()
