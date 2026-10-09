import os
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/5_network_information/4_Open_ports.sh"
)


class FreeBSDOpenPortsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.functions = MODULE.read_text().rsplit("\nget_open_ports\n", 1)[0]

    def _run(self, os_name, fixture):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "uname").write_text("#!/bin/sh\nprintf '%s\\n' \"$MOCK_OS_NAME\"\n")
            (root / "netstat").write_text(
                "#!/bin/sh\nprintf '%s\\n' \"$*\" >> \"$MOCK_NETSTAT_CALLS\"\n"
                "case \"$*\" in\n"
                "  '-an -p tcp'|'-punta') cat \"$MOCK_NETSTAT_DATA\" ;;\n"
                "  *) exit 2 ;;\n"
                "esac\n"
            )
            for name in ("uname", "netstat"):
                (root / name).chmod(0o755)
            (root / "data").write_text(fixture)
            env = os.environ.copy()
            env.update({
                "PATH": f"{root}:/usr/bin:/bin",
                "MOCK_OS_NAME": os_name,
                "MOCK_NETSTAT_CALLS": str(root / "calls"),
                "MOCK_NETSTAT_DATA": str(root / "data"),
            })
            prelude = (
                "E=E; SED_RED=RED; SED_RED_YELLOW=YELLOW; "
                "print_2title() { :; }; print_3title() { printf '<%s>\\n' \"$1\"; }; "
                "print_info() { :; }; "
            )
            result = subprocess.run(
                ["sh", "-c", prelude + self.functions + "\nget_open_ports\n"],
                env=env, capture_output=True, text=True, timeout=5,
            )
            calls = (root / "calls").read_text().splitlines() if (root / "calls").exists() else []
            return result, calls

    def test_freebsd_tcp_listener_view_preserves_columns_and_loopback(self):
        data = (
            "Active Internet connections (including servers)\n"
            "Proto Recv-Q Send-Q Local Address Foreign Address (state)\n"
            "tcp4 0 0 127.0.0.1.5901 *.* LISTEN\n"
            "tcp6 0 0 ::1.5801 *.* LISTEN\n"
            "tcp4 0 0 *.80 *.* LISTEN\n"
            "tcp4 0 0 127.0.0.1.22 192.0.2.1.40000 ESTABLISHED\n"
        )
        result, calls = self._run("FreeBSD", data)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(calls, ["-an -p tcp"])
        self.assertIn("<Active Ports (FreeBSD netstat, TCP)>", result.stdout)
        self.assertIn("tcp4 0 0 RED.5901 *.* LISTEN", result.stdout)
        self.assertNotIn("192.0.2.1.40000 ESTABLISHED", result.stdout)
        loopback = result.stdout.split("<Local-only listeners (loopback)>\n", 1)[1].split(
            "<Unique listener bind addresses>", 1
        )[0]
        self.assertIn("5901", loopback)
        self.assertIn("5801", loopback)
        self.assertNotIn("*.80", loopback)
        self.assertIn("RED\n", result.stdout)

    def test_freebsd_listener_cap_is_explicit(self):
        data = "".join(f"tcp4 0 0 127.0.0.1.{port} *.* LISTEN\n" for port in range(1000, 1300))
        result, calls = self._run("FreeBSD", data)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(calls, ["-an -p tcp"])
        self.assertEqual(result.stdout.count("FreeBSD TCP listener inventory incomplete (256-entry cap)."), 1)
        self.assertNotIn("1256", result.stdout)

        complete_data = "".join(f"tcp4 0 0 127.0.0.1.{port} *.* LISTEN\n" for port in range(1000, 1256))
        complete, complete_calls = self._run("FreeBSD", complete_data)
        self.assertEqual(complete.returncode, 0, complete.stderr)
        self.assertEqual(complete_calls, ["-an -p tcp"])
        active = complete.stdout.split("<Active Ports (FreeBSD netstat, TCP)>\n", 1)[1].split(
            "<Local-only listeners (loopback)>", 1
        )[0]
        self.assertNotIn("incomplete", active)

    def test_linux_keeps_original_netstat_command(self):
        data = "tcp 0 0 127.0.0.1:5901 0.0.0.0:* LISTEN -\n"
        result, calls = self._run("Linux", data)
        self.assertEqual(result.returncode, 0, result.stderr)
        # The later focused views use ss when available on the test host;
        # netstat is still the Linux active-port source in either case.
        self.assertTrue(calls)
        self.assertTrue(all(call == "-punta" for call in calls), calls)
        self.assertIn("<Active Ports (netstat)>", result.stdout)
        active = result.stdout.split("<Active Ports (netstat)>\n", 1)[1].split(
            "<Local-only listeners (loopback)>", 1
        )[0]
        self.assertNotIn("FreeBSD TCP listener inventory", active)


if __name__ == "__main__":
    unittest.main()
