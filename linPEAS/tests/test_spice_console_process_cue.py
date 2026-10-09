"""The cached process cue reports only exact unauthenticated loopback SPICE endpoints."""

import os
import re
import subprocess
import sys
import time
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/1_List_processes.sh"
)


def process(executable, args):
    return (
        "libvirt 1678 0.4 15.9 2122628 642348 ? Sl Feb24 24:57 "
        + executable + " " + args
    )


def proc_process(executable, args):
    return "  libvirt        1678      " + executable + " " + args


class SpiceConsoleProcessCueTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = MODULE.read_text()
        match = re.search(
            r"^  check_unauthenticated_spice_console\(\) \{\n.*?^  \}\n",
            source,
            re.MULTILINE | re.DOTALL,
        )
        assert match is not None
        cls.check = match.group(0)

    def classify(self, lines, nouseps=False):
        env = dict(os.environ, pslist="\n".join(lines), NOUSEPS="1" if nouseps else "")
        result = subprocess.run(
            ["sh", "-c", self.check + "\ncheck_unauthenticated_spice_console"],
            env=env,
            text=True,
            capture_output=True,
            check=True,
            timeout=2,
        )
        return result.stdout.splitlines()

    def test_exact_loopback_option_and_quoted_option(self):
        lines = [
            process("/usr/bin/qemu-system-x86_64", "-name guest -spice port=5900,addr=127.0.0.1,disable-ticketing -device virtio-net"),
            process("qemu-system-i386", "-spice addr=127.0.0.1,port=5900,disable-ticketing"),
        ]
        self.assertEqual(["127.0.0.1:5900"], self.classify(lines))
        proc_line = proc_process("qemu-system-i386", '-spice "addr=::1,disable-ticketing=on,port=5901" -display none')
        self.assertEqual(["[::1]:5901"], self.classify([proc_line], nouseps=True))

    def test_unrelated_option_text_or_other_executable_is_ignored(self):
        lines = [
            process("qemu-system-x86_64", "-name disable-ticketing -spice port=5900,addr=127.0.0.1"),
            process("qemu-system-x86_64", "-name disable-ticketing port=5900,addr=127.0.0.1"),
            process("/usr/bin/echo", "qemu-system-x86_64 -spice port=5900,addr=127.0.0.1,disable-ticketing"),
            process("qemu-system-x86_64-wrapper", "-spice port=5900,addr=127.0.0.1,disable-ticketing"),
        ]
        self.assertEqual([], self.classify(lines))
        disguised = proc_process("/usr/bin/echo", "a b c d e f g qemu-system-x86_64 -spice port=5900,addr=127.0.0.1,disable-ticketing")
        self.assertEqual([], self.classify([disguised], nouseps=True))

    def test_auth_enabled_remote_and_invalid_ports_are_ignored(self):
        lines = [
            process("qemu-system-x86_64", "-spice port=5900,addr=127.0.0.1,disable-ticketing=off"),
            process("qemu-system-x86_64", "-spice port=5900,addr=127.0.0.1,disable-ticketing,sasl=on"),
            process("qemu-system-x86_64", "-spice port=5900,addr=127.0.0.1,disable-ticketing,password-secret=auth"),
            process("qemu-system-x86_64", "-spice port=5900,addr=0.0.0.0,disable-ticketing"),
            process("qemu-system-x86_64", "-spice port=0,addr=127.0.0.1,disable-ticketing"),
            process("qemu-system-x86_64", "-spice port=65536,addr=127.0.0.1,disable-ticketing"),
        ]
        self.assertEqual([], self.classify(lines))

    def test_result_and_work_bounds(self):
        lines = [process("qemu-system-x86_64", "-spice port={},addr=127.0.0.1,disable-ticketing".format(5900 + n)) for n in range(10)]
        self.assertEqual(["127.0.0.1:{}".format(5900 + n) for n in range(5)], self.classify(lines))
        oversized = process("qemu-system-x86_64", "-name " + "x" * 8200 + " -spice port=6000,addr=127.0.0.1,disable-ticketing")
        self.assertEqual([], self.classify([oversized]))

    @unittest.skipUnless(Path("/proc/self/cmdline").exists(), "procfs required")
    def test_real_print_ps_snapshot_shape(self):
        printer = MODULE.parents[1] / "functions/print_ps.sh"
        child = subprocess.Popen(
            ["qemu-system-x86_64", "-c", "import time; time.sleep(4)",
             "-spice", "port=5999,addr=127.0.0.1,disable-ticketing"],
            executable=sys.executable,
        )
        try:
            time.sleep(0.1)
            snapshot = subprocess.run(
                ["sh", "-c", '. "$1"; print_ps', "sh", str(printer)],
                text=True, capture_output=True, check=True, timeout=5,
            ).stdout.splitlines()
            rows = [line for line in snapshot if len(line.split()) > 2 and
                    line.split()[2] == "qemu-system-x86_64"]
            self.assertTrue(rows)
            self.assertEqual(["127.0.0.1:5999"], self.classify(rows, nouseps=True))
        finally:
            child.terminate()
            child.wait(timeout=5)


if __name__ == "__main__":
    unittest.main()
