import os
import shlex
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


class ActualGroupsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.part = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/6_users_information/19_Actual_groups.sh"
        )

    def run_probe(self, groups, *, timeout_name="timeout", clock=None):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            fixture = root / "group"
            fixture.write_text(groups, encoding="utf-8")
            bindir = root / "bin"
            bindir.mkdir()
            for command in ("sed", "date"):
                (bindir / command).symlink_to(shutil.which(command))
            (bindir / "id").write_text(
                '#!/bin/sh\n'
                'case "$1" in\n'
                '  -G) echo "100 200" ;;\n'
                '  -g) echo "$FAKE_EFFECTIVE_GID" ;;\n'
                'esac\n',
                encoding="utf-8",
            )
            (bindir / "newgrp").write_text(
                '#!/bin/sh\n'
                'printf "%s\\n" "$1" >> "$FAKE_PROBE_LOG"\n'
                'read -r command\n'
                'case "$1" in\n'
                '  shadow_only) FAKE_EFFECTIVE_GID=300 ;;\n'
                '  *) FAKE_EFFECTIVE_GID=100 ;;\n'
                'esac\n'
                'export FAKE_EFFECTIVE_GID\n'
                'exec /bin/sh -c "$command"\n',
                encoding="utf-8",
            )
            (bindir / "id").chmod(0o755)
            (bindir / "newgrp").chmod(0o755)
            if timeout_name:
                (bindir / timeout_name).write_text(
                    '#!/bin/sh\n'
                    'printf "%s\\n" "$1 $2 $3" >> "$FAKE_TIMEOUT_LOG"\n'
                    'shift 3\n'
                    'exec "$@"\n',
                    encoding="utf-8",
                )
                (bindir / timeout_name).chmod(0o755)
            if clock is not None:
                (bindir / "date").unlink()
                (bindir / "date").write_text(
                    '#!/bin/sh\n'
                    'value=$(cat "$FAKE_CLOCK_FILE")\n'
                    'printf "%s\\n" "$((value + 1))" > "$FAKE_CLOCK_FILE"\n'
                    'echo "$value"\n',
                    encoding="utf-8",
                )
                (bindir / "date").chmod(0o755)
                (root / "clock").write_text(str(clock), encoding="utf-8")
                (bindir / "cat").symlink_to("/bin/cat")

            # Substitute only the group-file input; all commands remain fixtures.
            part = self.part.read_text(encoding="utf-8").replace(
                "done < /etc/group", f"done < {shlex.quote(str(fixture))}"
            )
            script = "\n".join(
                [
                    'print_2title() { echo "TITLE: $1"; }',
                    "IAMROOT=0 E=E groupsVB=NEVERMATCH groupsB=NEVERMATCH",
                    "SED_RED_YELLOW='' SED_RED=''",
                    part,
                    'printf "ACTUAL=%s\\n" "$ActualGroup"',
                ]
            )
            env = os.environ.copy()
            env.update(
                PATH=str(bindir),
                FAKE_PROBE_LOG=str(root / "probes"),
                FAKE_TIMEOUT_LOG=str(root / "timeouts"),
                FAKE_CLOCK_FILE=str(root / "clock"),
            )
            result = subprocess.run(
                ["/bin/sh", "-c", script],
                env=env,
                capture_output=True,
                text=True,
                timeout=5,
            )
            probes = (root / "probes").read_text().splitlines() if (root / "probes").exists() else []
            timeouts = (root / "timeouts").read_text().splitlines() if (root / "timeouts").exists() else []
            return result, probes, timeouts

    def test_missing_token_group_is_detected_even_without_group_file_member(self):
        groups = "in_token:x:100:\nshadow_only:x:300:\ndenied:x:400:\n"
        result, probes, timeouts = self.run_probe(groups)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(probes, ["shadow_only", "denied"])
        self.assertEqual(timeouts, ["-k 1 1", "-k 1 1"])
        self.assertIn("Accessible group not shown in id: shadow_only (gid=300)", result.stdout)
        self.assertNotIn("Accessible group not shown in id: denied", result.stdout)
        self.assertIn("ACTUAL=|shadow_only|", result.stdout)

    def test_no_timeout_skips_newgrp(self):
        result, probes, _ = self.run_probe("shadow_only:x:300:\n", timeout_name=None)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(probes, [])
        self.assertIn("ACTUAL=|", result.stdout)

    def test_global_deadline_stops_before_remaining_rows(self):
        groups = "".join(f"candidate{i}:x:{300 + i}:\n" for i in range(30))
        result, probes, _ = self.run_probe(groups, clock=100)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(len(probes), 14)

    def test_gtimeout_fallback(self):
        result, probes, timeouts = self.run_probe("shadow_only:x:300:\n", timeout_name="gtimeout")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(probes, ["shadow_only"])
        self.assertEqual(timeouts, ["-k 1 1"])

    def test_group_name_is_passed_literally(self):
        result, probes, _ = self.run_probe('literal$(touch marker):x:300:\n')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(probes, ['literal$(touch marker)'])


if __name__ == "__main__":
    unittest.main()
