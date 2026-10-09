import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class PgBasebackupBoundaryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.parts = Path(__file__).resolve().parents[1] / "builder" / "linpeas_parts"
        cls.boundary = cls.parts / "functions" / "check_pg_basebackup_boundary.sh"
        cls.mount = cls.parts / "functions" / "pgbb_mount_options.sh"
        cls.privileged = cls.parts / "functions" / "check_privileged_file_location.sh"

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(dir="/private/tmp" if Path("/private/tmp").is_dir() else None)
        self.addCleanup(self.tmp.cleanup)
        self.base = Path(self.tmp.name)
        self.source = self.base / "cluster"
        self.source.mkdir()
        self.output = self.base / "output"
        self.output.mkdir()
        self.output.chmod(0o555)
        self.cron_dir = self.base / "cron.d"
        self.cron_dir.mkdir()
        self.cron = self.cron_dir / "backup"
        self.wrapper = self.base / "backup-wrapper"
        self.wrapper.write_text("#!/bin/sh\n/usr/bin/pg_basebackup -h /run/postgresql -D " + str(self.output) + "\n", encoding="utf-8")
        self.wrapper.chmod(0o555)
        self.cron.write_text("* * * * * root /bin/sh " + str(self.wrapper) + "\n", encoding="utf-8")
        self.cron.chmod(0o444)

    def run_check(self, job=None, mount="rw", source=None, action=None):
        if job is not None:
            self.cron.chmod(0o644)
            self.cron.write_text(job + "\n", encoding="utf-8")
            self.cron.chmod(0o444)
        source = source or self.source
        body = "\n".join(
            [
                ". " + shlex.quote(str(self.mount)),
                ". " + shlex.quote(str(self.boundary)),
                ". " + shlex.quote(str(self.privileged)),
                "PG_BASEBACKUP_DESTS=",
                "E=E; SED_RED_YELLOW='&'; SED_RED='&'",
                "pgbb_metadata() { case \"$1\" in \"$PG_TEST_ROOT\"/cron.d/*|\"$PG_TEST_ROOT\"/backup-wrapper|\"$PG_TEST_ROOT\"/output|\"$PG_TEST_ROOT\"/rootparent) echo '0 755' ;; *) stat -c '%u %a' \"$1\" 2>/dev/null || stat -f '%u %Lp' \"$1\" 2>/dev/null ;; esac; }",
                "pgbb_mount_options() { echo \"$PG_TEST_MOUNT\"; }",
                "pgbb_scan_file \"$PG_TEST_CRON\" \"$PG_TEST_SOURCE\"",
                action or "printf 'linked=%s\\n' \"$PG_BASEBACKUP_DESTS\"",
            ]
        )
        env = os.environ.copy()
        env.update(PG_TEST_ROOT=str(self.base), PG_TEST_CRON=str(self.cron), PG_TEST_SOURCE=str(source), PG_TEST_MOUNT=mount)
        result = subprocess.run(["sh", "-c", body], env=env, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_root_wrapper_links_writable_source_and_plain_output(self):
        output = self.run_check()
        self.assertEqual(output.count("review candidate"), 1)
        self.assertIn(str(self.source), output)
        self.assertIn(str(self.output), output)
        self.assertIn("mount rw", output)
        self.assertIn("linked=" + str(self.output), output)

    def test_direct_root_job_and_non_root_job(self):
        direct = f"* * * * * root /usr/bin/pg_basebackup -h /run/postgresql --pgdata={self.output}"
        self.assertIn("review candidate", self.run_check(direct))
        self.assertNotIn("review candidate", self.run_check(direct.replace(" root ", " postgres ")))
        self.assertIn("review lead", self.run_check(direct.replace("-h /run/postgresql ", "")))
        self.assertIn("review lead", self.run_check(direct.replace("/run/postgresql", "remote")))

    def test_non_writable_source_and_nosuid_output(self):
        self.source.chmod(0o555)
        self.assertNotIn("review candidate", self.run_check())
        self.source.chmod(0o755)
        self.assertNotIn("review candidate", self.run_check(mount="rw,nosuid"))
        self.assertNotIn("review candidate", self.run_check(mount="rw,noexec"))

    def test_missing_destination_is_only_a_lead(self):
        parent = self.base / "rootparent"
        parent.mkdir()
        parent.chmod(0o555)
        direct = f"* * * * * root /usr/bin/pg_basebackup -h /run/postgresql -D {parent / 'new'}"
        output = self.run_check(direct)
        self.assertIn("review lead", output)
        self.assertNotIn("review candidate", output)

    def test_tar_and_variable_output_stay_inconclusive(self):
        self.wrapper.chmod(0o755)
        self.wrapper.write_text(f"#!/bin/sh\n/usr/bin/pg_basebackup -h /run/postgresql -Ft -D {self.output}\n", encoding="utf-8")
        self.wrapper.chmod(0o555)
        self.assertIn("review lead", self.run_check())
        self.assertNotIn("review candidate", self.run_check())
        self.wrapper.chmod(0o755)
        self.wrapper.write_text("#!/bin/sh\n/usr/bin/pg_basebackup -h /run/postgresql -D $BACKUP_DIR\n", encoding="utf-8")
        self.wrapper.chmod(0o555)
        self.assertIn("review lead", self.run_check())
        self.assertNotIn("review candidate", self.run_check())

    def test_symlink_destination_and_unreadable_wrapper(self):
        link = self.base / "linked"
        link.symlink_to(self.output, target_is_directory=True)
        direct = f"* * * * * root /usr/bin/pg_basebackup -h /run/postgresql -D {link}"
        output = self.run_check(direct)
        self.assertIn("review lead", output)
        self.assertNotIn("review candidate", output)
        self.assertNotIn("review candidate", self.run_check(direct + "/"))
        self.wrapper.chmod(0o000)
        self.assertNotIn("review candidate", self.run_check())

    def test_entry_and_wrapper_prefix_limits(self):
        direct = f"* * * * * root /usr/bin/pg_basebackup -h /run/postgresql -D {self.output}"
        self.assertEqual(self.run_check("\n".join([direct] * 25)).count("review candidate"), 20)
        self.cron.chmod(0o644)
        self.cron.write_text(f"* * * * * root /bin/sh {self.wrapper}\n", encoding="utf-8")
        self.cron.chmod(0o444)
        self.wrapper.chmod(0o755)
        self.wrapper.write_text("#!/bin/sh\n" + ("# padding\n" * 90) + f"/usr/bin/pg_basebackup -h /run/postgresql -D {self.output}\n", encoding="utf-8")
        self.wrapper.chmod(0o555)
        self.assertNotIn("review candidate", self.run_check())

    def test_cron_bytes_line_length_and_path_depth_limits(self):
        direct = f"* * * * * root /usr/bin/pg_basebackup -h /run/postgresql -D {self.output}"
        self.assertNotIn("review candidate", self.run_check(direct + (" " * 1025)))
        self.assertNotIn("review candidate", self.run_check(direct + "\n" + ("#" * 65536)))
        long_path = "/" + "a" * 513
        self.assertNotIn("review candidate", self.run_check(direct.replace(str(self.output), long_path)))
        deep_path = "/" + "/".join(["a"] * 33)
        self.assertNotIn("review candidate", self.run_check(direct.replace(str(self.output), deep_path)))

    def test_cron_file_inventory_is_capped(self):
        paths = " ".join("file" + str(i) for i in range(70))
        script = "\n".join(
            [
                ". " + shlex.quote(str(self.boundary)),
                "pgbb_scan_file() { printf '%s\\n' \"$1\"; }",
                "pgbb_scan_files /source " + paths,
            ]
        )
        result = subprocess.run(["sh", "-c", script], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.splitlines(), ["file" + str(i) for i in range(64)])
        self.wrapper.chmod(0o755)
        self.wrapper.write_text("#!/bin/sh\n" + ("#" * 8192) + "\n", encoding="utf-8")
        self.wrapper.chmod(0o555)
        self.assertNotIn("review candidate", self.run_check())

    def test_post_copy_signal_requires_confirmed_destination(self):
        self.output.chmod(0o755)
        copied = self.output / "setid"
        copied.write_text("fixture", encoding="utf-8")
        copied.chmod(0o4555)
        other = self.base / "ordinary"
        other.write_text("fixture", encoding="utf-8")
        other.chmod(0o4555)
        self.output.chmod(0o555)
        action = f"check_privileged_file_location SUID {shlex.quote(str(copied))} root; check_privileged_file_location SUID {shlex.quote(str(other))} root"
        output = self.run_check(action=action)
        self.assertIn("under a reviewed PostgreSQL backup destination", output)
        self.assertEqual(output.count("under a reviewed PostgreSQL backup destination"), 1)
        self.assertNotIn("under a reviewed PostgreSQL backup destination", self.run_check(mount="rw,nosuid", action=action))

    def test_process_probe_requires_one_literal_source_within_first_256_rows(self):
        source = str(self.source)
        script = ". " + shlex.quote(str(self.boundary)) + "\n" + (
            "ps() { printf '%s\\n' 'ARGS' '/usr/lib/postgresql/bin/postgres -D " + source + "' "
            "'postgres: worker'; }\n"
            "pgbb_source_from_process\n"
        )
        result = subprocess.run(["sh", "-c", script], capture_output=True, text=True)
        self.assertEqual(result.stdout.strip(), source)
        script = script.replace("'postgres: worker'", "'/usr/lib/postgresql/bin/postgres -D /another/cluster'")
        result = subprocess.run(["sh", "-c", script], capture_output=True, text=True)
        self.assertEqual(result.stdout.strip(), "")
        script = ". " + shlex.quote(str(self.boundary)) + "\n" + (
            "ps() { awk 'BEGIN { for (i=1; i<=256; i++) print \"ordinary\"; "
            f"print \"postgres -D {source}\" }}'; }}\n"
            "pgbb_source_from_process\n"
        )
        result = subprocess.run(["sh", "-c", script], capture_output=True, text=True)
        self.assertEqual(result.stdout.strip(), "")

    def test_linux_mount_fixture_uses_longest_prefix(self):
        mounts = self.base / "mounts"
        mounts.write_text(
            "rootfs / ext4 rw,relatime 0 0\n"
            f"tmpfs {self.base} tmpfs rw,nosuid 0 0\n"
            f"tmpfs {self.output} tmpfs rw,noexec 0 0\n",
            encoding="utf-8",
        )
        script = self.mount.read_text(encoding="utf-8").replace("/proc/mounts", str(mounts))
        script += "\npgbb_mount_options " + shlex.quote(str(self.output / "child")) + "\n"
        result = subprocess.run(["sh"], input=script, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "rw,noexec")


if __name__ == "__main__":
    unittest.main()
