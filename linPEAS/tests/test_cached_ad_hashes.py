import os
import subprocess
import tempfile
import unittest
from pathlib import Path


class CachedAdHashesTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/7_software_information/Cached_AD_hashes.sh"
        ).read_text()

    def _run(self, root):
        source = self.module
        source = source.replace("/var/lib/sss/db", str(root / "sss/db"))
        source = source.replace("/var/lib/samba", str(root / "samba"))
        source = source.replace("/var/opt/quest", str(root / "quest"))
        script = "print_2title() { printf 'TITLE:%s\\n' \"$1\"; }\n" + source
        env = os.environ.copy()
        env["DEBUG"] = ""
        return subprocess.run(["sh", "-c", script], text=True, capture_output=True, env=env)

    def test_existing_sssd_cache_glob_expands_and_does_not_read_contents(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            cache_dir = root / "sss/db"
            cache_dir.mkdir(parents=True)
            (cache_dir / "cache_example.ldb").write_text("SECRET_NEVER_PRINT")
            (cache_dir / "cache_example.tmp").write_text("NOT_A_CACHE")
            result = self._run(root)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("cache_example.ldb", result.stdout)
            self.assertIn("readable by current user", result.stdout)
            self.assertNotIn("cache_example.tmp", result.stdout)
            self.assertNotIn("SECRET_NEVER_PRINT", result.stdout)

    def test_missing_cache_does_not_claim_presence(self):
        with tempfile.TemporaryDirectory() as temporary:
            result = self._run(Path(temporary))
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout, "")

    def test_output_is_bounded(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            cache_dir = root / "sss/db"
            cache_dir.mkdir(parents=True)
            for number in range(35):
                (cache_dir / f"cache_{number:02}.ldb").touch()
            result = self._run(root)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.count("cache_") - 1, 32)
            self.assertIn("additional cache files omitted", result.stdout)


if __name__ == "__main__":
    unittest.main()
