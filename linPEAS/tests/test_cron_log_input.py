import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/7_Cron_jobs.sh"
)


class CronLogInputTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(
            dir="/private/tmp" if Path("/private/tmp").is_dir() else None
        )
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.script = self.root / "analyse.sh"
        self.script.write_text("#!/bin/bash\n# fixture; never executed\n", encoding="utf-8")
        self.script.chmod(0o444)
        self.log_dir = self.root / "log"
        self.log_dir.mkdir(mode=0o777)
        self.log_dir.chmod(0o777)
        self.log = self.log_dir / "application.log"
        self.log.write_text("private fixture data\n", encoding="utf-8")
        self.log.chmod(0o444)
        self.cron = self.root / "system-cron"
        self.command = f"* * * * * root /bin/bash {self.script} {self.log}"

    def run_probe(self, lines=None, *paths):
        if lines is not None:
            self.cron.write_text(lines + "\n", encoding="utf-8")
        source = MODULE.read_text(encoding="utf-8").split(
            '\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1
        )[0]
        shell = source + '\ncron_log_input_probe "$@"\n'
        result = subprocess.run(
            ["dash", "-c", shell, "dash", *(str(p) for p in (paths or (self.cron,)))],
            capture_output=True,
            text=True,
            timeout=8,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_root_literal_log_and_replacement_route(self):
        output = self.run_probe(self.command)
        self.assertEqual(output.count("review candidate"), 1)
        self.assertIn(str(self.log), output)
        self.assertIn("replace directory entry", output)
        self.assertIn("Metadata only", output)
        self.assertNotIn("private fixture data", output)

    def test_nonroot_nonbash_unrelated_and_nonliteral_commands(self):
        variants = [
            self.command.replace(" root ", " service "),
            self.command.replace("/bin/bash", "/bin/sh"),
            self.command.replace(str(self.log), str(self.root / "other.log")),
            self.command.replace(str(self.log), "$LOG"),
            self.command + " ; echo extra",
            self.command.replace(str(self.log), str(self.log) + "$(id)"),
        ]
        for line in variants:
            with self.subTest(line=line):
                self.assertNotIn("review candidate", self.run_probe(line))

    def test_access_sticky_and_symlink_guards(self):
        self.log_dir.chmod(0o555)
        self.assertNotIn("review candidate", self.run_probe(self.command))
        self.log_dir.chmod(0o1777)
        self.assertNotIn("review candidate", self.run_probe(self.command))
        self.log_dir.chmod(0o777)
        alias = self.root / "alias.log"
        alias.symlink_to(self.log)
        self.assertNotIn(
            "review candidate", self.run_probe(self.command.replace(str(self.log), str(alias)))
        )
        link_dir = self.root / "linked"
        link_dir.symlink_to(self.log_dir, target_is_directory=True)
        self.assertNotIn(
            "review candidate",
            self.run_probe(self.command.replace(str(self.log_dir), str(link_dir))),
        )
        script_alias = self.root / "script-alias.sh"
        script_alias.symlink_to(self.script)
        self.assertNotIn(
            "review candidate",
            self.run_probe(self.command.replace(str(self.script), str(script_alias))),
        )

    def test_direct_file_write_without_parent_replacement(self):
        self.log_dir.chmod(0o555)
        self.log.chmod(0o666)
        output = self.run_probe(self.command)
        self.assertIn("review candidate", output)
        self.assertIn("can write input file", output)
        self.assertNotIn("replace directory entry", output)

    def test_spool_root_and_bounded_visibility(self):
        spool = self.root / "root"
        spool.write_text(self.command.replace(" root ", " ") + "\n", encoding="utf-8")
        self.assertIn("review candidate", self.run_probe(None, spool))
        self.assertEqual(
            self.run_probe("\n".join([self.command] * 18)).count("review candidate"),
            16,
        )
        self.assertIn("truncated at 16 lines", self.run_probe(None))
        self.assertIn("skipped oversized", self.run_probe("#" * 8200 + "\n" + self.command))
        self.assertIn("truncated at 8 cron files", self.run_probe(self.command, *([self.cron] * 9)))


if __name__ == "__main__":
    unittest.main()
