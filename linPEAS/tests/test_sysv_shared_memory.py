"""Focused, read-only System V IPC output fixtures."""

import os
import pathlib
import subprocess
import tempfile
import unittest


MODULE = (
    pathlib.Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/20_Sysv_shared_memory.sh"
)


class SysvSharedMemoryTests(unittest.TestCase):
    def run_fixture(self, ipcs_output, search_in_folder=""):
        with tempfile.TemporaryDirectory() as tmp:
            directory = pathlib.Path(tmp)
            fixture = directory / "ipcs-output"
            fixture.write_text(ipcs_output)
            ipcs = directory / "ipcs"
            ipcs.write_text('#!/bin/sh\ncat "$IPC_FIXTURE"\n')
            ipcs.chmod(0o755)
            env = os.environ.copy()
            env.update(
                PATH=str(directory) + os.pathsep + env.get("PATH", ""),
                IPC_FIXTURE=str(fixture),
                SEARCH_IN_FOLDER=search_in_folder,
            )
            script = (
                'print_2title() { printf "TITLE:%s\\n" "$1"; }; '
                'print_info() { :; }; '
                '. "$IPC_MODULE"'
            )
            env["IPC_MODULE"] = str(MODULE)
            result = subprocess.run(
                ["/bin/sh", "-c", script],
                env=env,
                text=True,
                capture_output=True,
                timeout=5,
                check=True,
            )
            self.assertEqual(result.stderr, "")
            return result.stdout

    def test_linux_numeric_modes_select_other_writable_root_only(self):
        output = self.run_fixture(
            "------ Shared Memory Segments --------\n"
            "key shmid owner perms bytes nattch status\n"
            "0x00001234 17 root 666 1024 1\n"
            "0x00001235 18 0 0622 1024 1\n"
            "0x00001236 19 root 660 1024 1\n"
            "0x00001237 20 alice 666 1024 1\n"
        )
        self.assertIn("id=17 key=0x00001234 mode=666 size=1024", output)
        self.assertIn("id=18 key=0x00001235 mode=0622 size=1024", output)
        self.assertNotIn("id=19", output)
        self.assertNotIn("id=20", output)

    def test_bsd_symbolic_modes_select_other_writable_root_only(self):
        output = self.run_fixture(
            "IPC status from <running system>\n"
            "T ID KEY MODE OWNER GROUP\n"
            "Shared Memory:\n"
            "m 12 0x42 --rw-rw-rw- root wheel\n"
            "m 13 0x43 --rw-rw---- root wheel\n"
            "m 14 0x44 --rw-rw-rw- alice staff\n"
        )
        self.assertIn("id=12 key=0x42 mode=--rw-rw-rw- size=unknown", output)
        self.assertNotIn("id=13", output)
        self.assertNotIn("id=14", output)

    def test_empty_or_unrecognized_output_is_quiet(self):
        self.assertEqual(self.run_fixture("\n------ Shared Memory Segments --------\n"), "")
        self.assertEqual(self.run_fixture("arbitrary output\nroot 666\n"), "")

    def test_offline_filesystem_search_skips_ipc(self):
        self.assertEqual(
            self.run_fixture(
                "key shmid owner perms bytes nattch status\n"
                "0x1 7 root 666 1 0\n",
                search_in_folder="/mnt/image",
            ),
            "",
        )

    def test_candidate_limit_is_explicit(self):
        rows = ["key shmid owner perms bytes nattch status"]
        rows.extend(f"0x{i:x} {i} root 666 1 0" for i in range(25))
        output = self.run_fixture("\n".join(rows) + "\n")
        self.assertEqual(output.count("Root-owned System V shared memory"), 20)
        self.assertIn("20-candidate limit", output)
        self.assertNotIn("id=24", output)

    def test_input_line_limit_is_explicit(self):
        output = self.run_fixture("\n".join(["key shmid owner perms bytes nattch status"] + ["x"] * 512) + "\n")
        self.assertIn("512-line/20-candidate limit", output)


if __name__ == "__main__":
    unittest.main()
