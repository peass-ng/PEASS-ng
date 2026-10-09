import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")

WRAPPER = r'''#!/bin/bash
json_file="$1"
allowed_paths=("/var/" "/home/")
updated_json=$(/usr/bin/jq '.directories_to_archive |= map(gsub("\\.\\./"; ""))' "$json_file")
/usr/bin/echo "$updated_json" > "$json_file"
directories_to_archive=$(/usr/bin/echo "$updated_json" | /usr/bin/jq -r '.directories_to_archive[]')
is_allowed_path() {
  local path="$1"
  for allowed_path in "${allowed_paths[@]}"; do
    if [[ "$path" == $allowed_path* ]]; then return 0; fi
  done
  return 1
}
/usr/bin/backy "$json_file"
'''


class SudoBackupWrapperJsonTests(unittest.TestCase):
    def run_case(self, source=WRAPPER, rule="root", suffix="", links=False):
        with tempfile.TemporaryDirectory(dir=Path(tempfile.gettempdir()).resolve()) as temp:
            base = Path(temp)
            bin_dir = base / "bin"
            bin_dir.mkdir()
            script = base / "backup.sh"
            script.write_text(source + "\n# SECRET-NEVER-PRINT\n")
            script.chmod(0o755)
            if links:
                link = base / "backup-link.sh"
                link.symlink_to(script)
                script = link
            sudo = bin_dir / "sudo"
            sudo.write_text('#!/bin/sh\nprintf "%s\\n" "$FAKE_SUDO_RULE"\n')
            sudo.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{bin_dir}:{env['PATH']}"
            env["FAKE_SUDO_RULE"] = f"    ({rule}) NOPASSWD: {script}{suffix}"
            body = "\n".join([
                "E=E", "sudoB=__unused__", "sudoG=__unused__",
                "sudoVB1=__unused__", "sudoVB2=__unused__",
                "SED_RED='&'", "SED_RED_YELLOW='&'", "SED_GREEN='&'",
                "PASSWORD=", "TIMEOUT=", "ROOT_FOLDER=",
                "print_2title() { :; }", "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(MODULE))}",
            ])
            result = subprocess.run(["sh", "-c", body], env=env,
                                    capture_output=True, text=True, timeout=12)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertNotIn("SECRET-NEVER-PRINT", result.stdout)
            return result.stdout

    def test_caller_json_sanitized_then_reread_is_candidate(self):
        output = self.run_case()
        self.assertIn("Sudo backup-wrapper JSON review candidate", output)
        self.assertIn("indicator lines", output)

    def test_requires_effective_shape_and_multiple_indicators(self):
        for kwargs in ({"rule": "nobody"}, {"suffix": ' ""'},
                       {"suffix": " /fixed/task.json"},
                       {"source": WRAPPER.replace("gsub(", "sub(")},
                       {"source": WRAPPER.replace(" > \"$json_file\"", " > /tmp/fixed")},
                       {"source": WRAPPER.replace("/usr/bin/backy \"$json_file\"", "/usr/bin/backy /tmp/fixed")},
                       {"source": "# " + WRAPPER.replace("\n", "\n# ")}):
            with self.subTest(kwargs=kwargs):
                self.assertNotIn("JSON review candidate", self.run_case(**kwargs))

    def test_reports_symlink_and_scan_limits_as_unverified(self):
        self.assertIn("symlink component", self.run_case(links=True))
        self.assertIn("64 KiB script limit", self.run_case(source=WRAPPER + "# x\n" * 23000))
        self.assertIn("scan limit", self.run_case(source="\n" * 201 + WRAPPER))


if __name__ == "__main__":
    unittest.main()
