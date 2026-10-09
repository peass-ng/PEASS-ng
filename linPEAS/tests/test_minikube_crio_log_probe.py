"""A local Minikube log may identify a nested CRI-O runtime without leaking logs."""

import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/2_container/3_Container_details.sh")
FUNCTION = re.search(r"^minikube_crio_log_probe\(\) \{\n.*?^\}",
                     MODULE.read_text(), re.MULTILINE | re.DOTALL).group()
VALID_LINE = "* Preparing Kubernetes v1.23.3 on CRI-O 1.22.1 ...\n"


class MinikubeCrioLogProbeTests(unittest.TestCase):
    def run_probe(self, *homes):
        command = ("print_list() { printf '%s\\n' \"$1\"; }\n" + FUNCTION +
                   "\nminikube_crio_log_probe \"$@\"")
        result = subprocess.run(["sh", "-c", command, "test", *map(str, homes)],
                                text=True, capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("", result.stderr)
        return result.stdout

    @staticmethod
    def write_log(home, content):
        log_dir = home / ".minikube/logs"
        log_dir.mkdir(parents=True, exist_ok=True)
        (log_dir / "lastStart.txt").write_text(content)
        return log_dir / "lastStart.txt"

    def test_reports_only_bounded_historical_version(self):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            home = Path(tmp) / "user"
            self.write_log(home, "token=SENSITIVE_VALUE\n" + VALID_LINE)
            output = self.run_probe(home)
            self.assertIn("CRI-O .... 1.22.1", output)
            self.assertIn("historical", output)
            self.assertNotIn("SENSITIVE_VALUE", output)
            self.assertNotIn(str(home), output)

    def test_rejects_links_and_nonregular_files(self):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            actual = root / "actual"
            log = self.write_log(actual, VALID_LINE)
            linked_home = root / "linked-home"
            linked_home.symlink_to(actual, target_is_directory=True)
            self.assertEqual("", self.run_probe(linked_home))
            linked_log = root / "linked-log"
            linked_log_dir = linked_log / ".minikube/logs"
            linked_log_dir.mkdir(parents=True)
            (linked_log_dir / "lastStart.txt").symlink_to(log)
            self.assertEqual("", self.run_probe(linked_log))
            linked_dir = root / "linked-dir"
            (linked_dir / ".minikube").mkdir(parents=True)
            (linked_dir / ".minikube/logs").symlink_to(log.parent,
                                                        target_is_directory=True)
            self.assertEqual("", self.run_probe(linked_dir))
            fifo_home = root / "fifo"
            fifo_dir = fifo_home / ".minikube/logs"
            fifo_dir.mkdir(parents=True)
            os.mkfifo(fifo_dir / "lastStart.txt")
            self.assertEqual("", self.run_probe(fifo_home))

    def test_caps_homes_bytes_and_lines(self):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            late = root / "late"
            self.write_log(late, "x" * 65536 + "\n" + VALID_LINE)
            self.assertEqual("", self.run_probe(late))
            self.write_log(late, "noise\n" * 200 + VALID_LINE)
            self.assertEqual("", self.run_probe(late))
            self.write_log(late, VALID_LINE)
            self.assertEqual("", self.run_probe(*(root / f"empty{i}" for i in range(8)), late))
            self.assertIn("1.22.1", self.run_probe(*(root / f"empty{i}" for i in range(7)), late))

    def test_rejects_unvalidated_versions(self):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            home = Path(tmp) / "user"
            for value in ("unknown", "1.22", "1.22.1.4", "9" * 21):
                with self.subTest(value=value):
                    self.write_log(home, VALID_LINE.replace("1.22.1", value))
                    self.assertEqual("", self.run_probe(home))

    def test_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
