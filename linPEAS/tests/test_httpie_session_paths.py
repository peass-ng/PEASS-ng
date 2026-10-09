"""Bounded, metadata-only HTTPie session discovery in the home-file module."""

import os
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MODULE = ROOT / "linPEAS/builder/linpeas_parts/9_interesting_files/10_Others_homes.sh"


class HttpieSessionPathTests(unittest.TestCase):
    def test_exact_paths_caps_and_no_content_reads(self):
        source = MODULE.read_text()
        syntax = subprocess.run(["sh", "-n", str(MODULE)], capture_output=True, text=True)
        self.assertEqual(0, syntax.returncode, syntax.stderr)
        self.assertNotIn('cat "$httpie_file"', source)
        self.assertNotIn('find "$httpie_sessions"', source)

        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            current = base / "current"
            other = base / "other"
            linked_ancestor = base / "linked-ancestor"
            extra = base / "extra"
            for home in (current, other, linked_ancestor, extra):
                home.mkdir()
            (base / "empty").mkdir()
            good = other / ".config/httpie/sessions/localhost_5000/admin.json"
            good.parent.mkdir(parents=True)
            good.write_text("DO_NOT_PRINT_HTTP_SESSION_SECRET")
            (linked_ancestor / ".config").symlink_to(other / ".config", target_is_directory=True)
            linked_good = linked_ancestor / ".config/httpie/sessions/localhost_5000/admin.json"
            (good.parent / "admin.json.bak").write_text("NO_BACKUP_MATCH")
            deep = good.parent / "nested/deep.json"
            deep.parent.mkdir()
            deep.write_text("NO_DEEP_MATCH")
            outside = other / ".config/not-httpie/sessions/localhost_5000/admin.json"
            outside.parent.mkdir(parents=True)
            outside.write_text("NO_OTHER_APP_MATCH")
            link = good.parent / "linked.json"
            link.symlink_to(good)
            own = current / ".config/httpie/sessions/api.example/default.json"
            own.parent.mkdir(parents=True)
            own.write_text("DO_NOT_PRINT_OWN_SESSION_SECRET")
            for number in range(40):
                path = extra / f".config/httpie/sessions/host-{number:02d}/session.json"
                path.parent.mkdir(parents=True)
                path.write_text("DO_NOT_PRINT_BULK_SECRET")

            stub_bin = base / "bin"
            stub_bin.mkdir()
            stub_awk = stub_bin / "awk"
            stub_awk.write_text('#!/bin/sh\nprintf "%s\\n" "$FIXTURE_HOMES"\n')
            stub_awk.chmod(0o755)
            env = {
                **os.environ,
                "PATH": str(stub_bin) + os.pathsep + os.environ["PATH"],
                "FIXTURE_HOMES": "\n".join((str(other), str(linked_ancestor), str(extra))),
                "HOME": str(current),
                "HOMESEARCH": str(base / "empty"),
                "SEARCH_IN_FOLDER": "",
                "USER": "fixture",
            }
            cmd = "print_2title() { :; }; echo_not_found() { :; }; . '" + str(MODULE) + "'"
            result = subprocess.run(["sh", "-c", cmd], env=env, text=True,
                                    capture_output=True, timeout=5)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(good), result.stdout)
            self.assertIn(str(own), result.stdout)
            self.assertNotIn(str(linked_good), result.stdout)
            self.assertNotIn(str(deep), result.stdout)
            self.assertNotIn(str(outside), result.stdout)
            self.assertNotIn(str(link), result.stdout)
            self.assertNotIn("DO_NOT_PRINT", result.stdout)
            found = [line for line in result.stdout.splitlines() if line.endswith(".json")]
            self.assertEqual(24, len(found), result.stdout)


if __name__ == "__main__":
    unittest.main()
