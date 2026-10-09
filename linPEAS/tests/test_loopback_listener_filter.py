import subprocess
import unittest
from pathlib import Path


class LoopbackListenerFilterTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/5_network_information/4_Open_ports.sh"
        ).read_text()
        cls.functions = source.rsplit("\nget_open_ports\n", 1)[0]

    def _filter(self, lines, column):
        command = self.functions + f"\nlp_loopback_listeners {column}\n"
        return subprocess.run(["sh", "-c", command], input="\n".join(lines) + "\n",
                              capture_output=True, text=True)

    def test_ss_ipv4_ipv6_and_mapped_loopback(self):
        addresses = ["127.0.0.1:8111", "127.2.3.4:8111", "[::1]:8111",
                     "[::ffff:127.0.0.1]:8111", "[::FFFF:127.0.0.1]:8111",
                     "0.0.0.0:8111", "[::]:8111", "[::ffff:192.0.2.10]:8111"]
        lines = [f"tcp LISTEN 0 128 {address} 0.0.0.0:*" for address in addresses]
        result = self._filter(lines, 5)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.splitlines(), lines[:5])

    def test_netstat_uses_local_bind_column(self):
        lines = [
            "tcp 0 0 127.0.0.1:22 0.0.0.0:* LISTEN 1/sshd",
            "tcp6 0 0 [::ffff:127.0.0.2]:443 [::]:* LISTEN -",
            "tcp 0 0 0.0.0.0:80 127.0.0.1:1234 LISTEN -",
        ]
        result = self._filter(lines, 4)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.splitlines(), lines[:2])


if __name__ == "__main__":
    unittest.main()
