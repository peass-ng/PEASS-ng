"""Configuration-supplied version probes must not execute replaceable programs."""

import os
import subprocess
import tempfile
import unittest
from pathlib import Path


HELPER = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/functions/lp_trusted_version_path.sh"


class TrustedVersionPathTests(unittest.TestCase):
    def resolve(self, path, env=None):
        return subprocess.run(
            ["sh", "-c", '. "$1"; lp_trusted_version_path "$2"',
             "sh", str(HELPER), str(path)],
            env=env, capture_output=True, text=True, timeout=5,
        )

    def test_system_binary_resolves_to_checked_canonical_path(self):
        result = self.resolve("/bin/sh")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), str(Path("/bin/sh").resolve()))

    def test_relative_and_missing_paths_are_rejected(self):
        for path in ("sh", "/does-not-exist/version-probe"):
            with self.subTest(path=path):
                result = self.resolve(path)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(result.stdout, "")

    def test_user_executable_is_not_invoked(self):
        with tempfile.TemporaryDirectory() as temp:
            program = Path(temp) / "python3"
            marker = Path(temp) / "invoked"
            program.write_text(f"#!/bin/sh\ntouch '{marker}'\n")
            program.chmod(0o755)
            result = self.resolve(program)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(result.stdout, "")
            self.assertFalse(marker.exists())

    def test_user_alias_only_returns_protected_target_not_alias(self):
        with tempfile.TemporaryDirectory() as temp:
            alias = Path(temp) / "python3"
            alias.symlink_to("/bin/sh")
            result = self.resolve(alias)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.strip(), str(Path("/bin/sh").resolve()))

    def test_group_writable_root_owned_binary_or_ancestor_is_rejected(self):
        # Simulate metadata without requiring root or changing host permissions.
        with tempfile.TemporaryDirectory() as temp:
            tools = Path(temp)
            fake_ls = tools / "ls"
            fake_ls.write_text(
                '#!/bin/sh\n'
                'if [ "$2" = "$UNSAFE_PATH" ]; then mode=-rwxrwxr-x; '
                'else mode=drwxr-xr-x; fi\n'
                'printf "%s 1 0 0 0 Jan 1 00:00 fixture\\n" "$mode"\n'
            )
            fake_ls.chmod(0o755)
            binary = Path("/bin/sh").resolve()
            for unsafe in (binary, binary.parent):
                with self.subTest(unsafe=unsafe):
                    env = dict(os.environ, PATH=f"{tools}:{os.environ['PATH']}",
                               UNSAFE_PATH=str(unsafe))
                    result = self.resolve(binary, env)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertEqual(result.stdout, "")


if __name__ == "__main__":
    unittest.main()
