import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/9_interesting_files/27_Passwords_in_logs.sh"
)


class AccessLogGetCredentialTests(unittest.TestCase):
    def run_probe(self, *paths):
        source = MODULE.read_text(encoding="utf-8").split(
            '\nif ! [ "$SEARCH_IN_FOLDER" ]; then', 1
        )[0]
        result = subprocess.run(
            ["dash", "-c", source + '\nlp_http_log_credential_cues "$@"\n', "dash", *map(str, paths)],
            capture_output=True,
            text=True,
            timeout=8,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_long_get_request_reports_path_only(self):
        with tempfile.TemporaryDirectory() as tmp:
            log = Path(tmp) / "access.log"
            secret = "SENSITIVE_ACCESS_LOG_PASSWORD"
            request = (
                '127.0.0.1 - - [04/Feb/2025:02:20:11 +0000] '
                f'"GET /join.php?loginUsername=user&loginPassword={secret}&loginForm=Login HTTP/1.1" '
                '302 329 "http://example.invalid/join.php" '
                '"Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:134.0) Gecko/20100101 Firefox/134.0"\n'
            )
            self.assertGreater(len(request), 200)
            log.write_text(request, encoding="utf-8")
            output = self.run_probe(log)
            self.assertIn(f"{log}: credential-bearing GET request present", output)
            self.assertEqual(output.count("credential-bearing GET request present"), 1)
            self.assertNotIn(secret, output)
            self.assertNotIn("loginUsername", output)
            self.assertNotIn("/join.php", output)

    def test_only_nonempty_query_values_in_get_request_count(self):
        with tempfile.TemporaryDirectory() as tmp:
            log = Path(tmp) / "access.log"
            log.write_text(
                '127.0.0.1 "GET /login?password= HTTP/1.1" 200 1\n'
                '127.0.0.1 "POST /login?password=hidden HTTP/1.1" 200 1\n'
                '127.0.0.1 "GET /plain HTTP/1.1" 200 1 "https://site/?password=hidden" "ua"\n'
                '127.0.0.1 "GET /plain HTTP/1.1" 200 1 "ref" "password=hidden"\n'
                '127.0.0.1 "GET /login?password HTTP/1.1" 200 1\n',
                encoding="utf-8",
            )
            self.assertEqual(self.run_probe(log), "")
            log.write_text('127.0.0.1 "GET /login?loginPassword=present HTTP/1.1" 200 1\n')
            self.assertIn("credential-bearing GET request present", self.run_probe(log))

    def test_tail_byte_limit_and_file_guard(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            log = root / "access.log"
            log.write_text(
                '127.0.0.1 "GET /login?password=too-old HTTP/1.1" 200 1\n'
                + ("x" * 70000)
                + "\n",
                encoding="utf-8",
            )
            self.assertEqual(self.run_probe(log), "")
            alias = root / "alias.log"
            alias.symlink_to(log)
            self.assertEqual(self.run_probe(alias), "")
            self.assertEqual(self.run_probe(root / "missing.log"), "")

    def test_file_cap(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            paths = []
            for i in range(9):
                path = root / f"access-{i}.log"
                path.write_text(
                    '127.0.0.1 "GET /login?password=TOP_SECRET_VALUE HTTP/1.1" 200 1\n',
                    encoding="utf-8",
                )
                paths.append(path)
            output = self.run_probe(*paths)
            self.assertEqual(output.count("credential-bearing GET request present"), 8)
            self.assertIn("partial (file or time cap reached)", output)
            self.assertNotIn("TOP_SECRET_VALUE", output)

    def test_short_legacy_log_entry_is_still_selected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            log = root / "ordinary.log"
            log.write_text("short pwd=fixture-value\n", encoding="utf-8")
            module = MODULE.read_text(encoding="utf-8")
            legacy = module.split('  (find /var/log/', 1)[1].split('  lp_http_log_credential_cues', 1)[0]
            legacy = '  (find /var/log/' + legacy
            legacy = legacy.replace(
                "find /var/log/ /var/logs/ /private/var/log",
                f"find {root}",
            )
            result = subprocess.run(
                ["dash", "-c", "E=E; SED_RED=''; log_find_exec_end='+';\n" + legacy],
                capture_output=True,
                text=True,
                timeout=8,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("short =fixture-value", result.stdout)


if __name__ == "__main__":
    unittest.main()
