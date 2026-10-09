"""Bounded, passive classification of privileged office UNO listeners."""

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


class PrivilegedOfficeUnoSocketsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = MODULE.read_text()
        match = re.search(
            r"^  check_privileged_office_uno_sockets\(\) \{\n.*?^  \}\n",
            source,
            re.MULTILINE | re.DOTALL,
        )
        assert match is not None
        cls.check = match.group(0)

    def classify(self, lines):
        env = dict(os.environ, pslist="\n".join(lines))
        result = subprocess.run(
            ["sh", "-c", self.check + "\ncheck_privileged_office_uno_sockets"],
            env=env,
            text=True,
            capture_output=True,
            check=True,
            timeout=2,
        )
        return result.stdout.splitlines()

    def test_root_soffice_socket_is_reported_once_without_command_text(self):
        lines = [
            "root 123 0.0 0.1 /usr/lib/libreoffice/program/soffice.bin --headless --accept=\"socket,host=localhost,port=2002;urp;\"",
            "root 124 0.0 0.1 /usr/lib/libreoffice/program/soffice.bin --headless --accept=\"socket,host=localhost,port=2002;urp;\"",
        ]
        self.assertEqual(["localhost:2002"], self.classify(lines))

    def test_non_root_or_non_uno_process_is_not_reported(self):
        lines = [
            "user 123 0.0 0.1 /usr/bin/soffice --accept=socket,host=localhost,port=2002;urp;",
            "root 124 0.0 0.1 /usr/bin/soffice --headless",
            "root 125 0.0 0.1 /usr/bin/other --accept=socket,host=localhost,port=2002;urp;",
            "root 126 0.0 0.1 /usr/bin/soffice --accept=socket,host=localhost,port=2002;tcp;",
            "root 127 0.0 0.1 /usr/bin/soffice --accept=pipe,name=api;urp;",
            "root 128 /usr/bin/soffice host=localhost port=2002 --accept=pipe,name=api;urp;",
            "root 129 /usr/bin/soffice --accept=socket,host=localhost;urp; port=2002",
        ]
        self.assertEqual([], self.classify(lines))

    def test_endpoint_must_have_valid_port_and_output_is_capped(self):
        lines = [
            "root 99 /usr/bin/soffice --accept=socket,host=localhost,port=65536;urp;",
            "root 98 /usr/bin/soffice --accept=socket,host=localhost,port=0;urp;",
        ] + [
            "root {} /usr/bin/soffice --accept=socket,host=127.0.0.1,port={};urp;".format(
                pid, 2000 + pid
            )
            for pid in range(10)
        ]
        self.assertEqual(
            ["127.0.0.1:{}".format(2000 + pid) for pid in range(5)],
            self.classify(lines),
        )

    @unittest.skipUnless(Path("/proc/self/cmdline").exists(), "procfs required")
    def test_proc_fallback_keeps_owner_and_argument_boundaries(self):
        fallback = MODULE.parents[1] / "functions/print_ps.sh"
        expected_user = subprocess.check_output(["id", "-un"], text=True).strip()
        process = subprocess.Popen(
            ["soffice.bin", "-c", "import time; time.sleep(4)",
             "--accept=socket,host=localhost,port=2002;urp;"],
            executable=sys.executable,
        )
        try:
            time.sleep(0.1)
            result = subprocess.run(
                ["sh", "-c", '. "$1"; print_ps', "sh", str(fallback)],
                text=True, capture_output=True, check=True, timeout=5,
            )
            lines = [line for line in result.stdout.splitlines()
                     if "soffice.bin" in line and "--accept=" in line]
            self.assertTrue(lines)
            self.assertTrue(any(
                line.split()[0] == expected_user and
                "soffice.bin -c" in line and
                " --accept=socket,host=localhost,port=2002;urp;" in line
                for line in lines
            ))
        finally:
            process.terminate()
            process.wait(timeout=5)


if __name__ == "__main__":
    unittest.main()
