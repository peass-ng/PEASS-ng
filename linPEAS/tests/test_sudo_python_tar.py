import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SudoPythonTarTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
        )

    def run_case(self, version="3.12.3", rule="root", wildcard=True,
                 source_kind="valid", archive_dir=True, python_name="python3",
                 fixed_argument=False):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            bindir = base / "bin"
            bindir.mkdir()
            python = bindir / python_name
            script = base / "restore.py"
            backups = base / "backups"
            if archive_dir:
                backups.mkdir()
            python.write_text(
                "#!/bin/sh\n"
                "[ \"$1\" = -I ] && [ \"$2\" = -S ] && [ \"$3\" = --version ] || exit 7\n"
                "printf 'Python %s\\n' \"$FAKE_PYTHON_VERSION\"\n"
            )
            python.chmod(0o755)
            source = (
                f'BACKUP_BASE_DIR = "{backups}"\n'
                'backup_path = os.path.join(BACKUP_BASE_DIR, args.backup)\n'
                'with tarfile.open(backup_path, "r") as tar:\n'
                '    tar.extractall(path=staging_dir, filter="data")\n'
                'SECRET = "do-not-print-me"\n'
            )
            if source_kind == "fixed_archive":
                source = source.replace("os.path.join(BACKUP_BASE_DIR, args.backup)",
                                        '"/opt/fixed.tar"')
            elif source_kind == "renamed":
                source = (f'INPUT_ROOT = "{backups}"\n'
                          'chosen_file = os.path.join(INPUT_ROOT, args.restore_file)\n'
                          'with tarfile.open(chosen_file, "r") as archive:\n'
                          "    archive.extractall(path=staging_dir, filter='data')\n")
            elif source_kind == "safe_filter":
                source = source.replace('filter="data"', 'filter="fully_trusted"')
            elif source_kind == "unrelated_extract":
                source = source.replace(
                    '    tar.extractall(path=staging_dir, filter="data")',
                    '    pass\nother_tar.extractall(path=staging_dir, filter="data")',
                )
            elif source_kind == "different_archive":
                source = source.replace('tarfile.open(backup_path, "r")',
                                        'tarfile.open(fixed_path, "r")')
            elif source_kind == "outside_with":
                source = source.replace(
                    '    tar.extractall(path=staging_dir, filter="data")',
                    '    pass\ntar.extractall(path=staging_dir, filter="data")',
                )
            elif source_kind == "overwritten_archive":
                source = source.replace(
                    'with tarfile.open(backup_path, "r") as tar:',
                    'backup_path = "/opt/fixed.tar"\n'
                    'with tarfile.open(backup_path, "r") as tar:',
                )
            elif source_kind == "overwritten_handle":
                source = source.replace(
                    '    tar.extractall(path=staging_dir, filter="data")',
                    '    tar = other_tar\n'
                    '    tar.extractall(path=staging_dir, filter="data")',
                )
            script.write_text(source)
            sudo = bindir / "sudo"
            sudo.write_text("#!/bin/sh\nprintf '%s\\n' \"$FAKE_SUDO_RULE\"\n")
            sudo.chmod(0o755)
            # The module requires a timeout for bounded interpreter inspection.
            timeout = bindir / "timeout"
            timeout.write_text(
                '#!/bin/sh\n'
                '[ "$1" = -k ] && [ "$2" = 1 ] && [ "$3" = 2 ] || exit 2\n'
                'shift 3\nexec "$@"\n'
            )
            timeout.chmod(0o755)
            env = os.environ.copy()
            env.update({
                "PATH": f"{bindir}:{env['PATH']}",
                "FAKE_PYTHON_VERSION": version,
                "FAKE_SUDO_RULE": (
                    f"    ({rule}) NOPASSWD: {python} {script}"
                    + (" *" if wildcard else (" /opt/fixed.tar" if fixed_argument else ""))
                ),
            })
            body = "\n".join([
                "E=E",
                "sudoB=__unlikely_sudoB__",
                "sudoG=__unlikely_sudoG__",
                "sudoVB1=__unlikely_sudoVB1__",
                "sudoVB2=__unlikely_sudoVB2__",
                "SED_RED='&'",
                "SED_RED_YELLOW='&'",
                "SED_GREEN='&'",
                "PASSWORD=",
                "TIMEOUT=",
                "print_2title() { :; }",
                "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(self.module))}",
            ])
            result = subprocess.run(["sh", "-c", body], env=env,
                                    capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertNotIn("do-not-print-me", result.stdout)
            return result.stdout

    def test_correlates_writable_user_archive_and_upstream_affected_python(self):
        for version in ("3.9.22", "3.10.17", "3.11.12", "3.12.10", "3.13.3"):
            with self.subTest(version=version):
                output = self.run_case(version=version)
                self.assertIn("Privileged sudo Python archive extraction review", output)
                self.assertIn("CVE-2025-4517", output)

    def test_correlates_other_names_and_versioned_python_binary(self):
        output = self.run_case(version="3.10.17", python_name="python3.10",
                               source_kind="renamed")
        self.assertIn("Privileged sudo Python archive extraction review", output)

    def test_skips_fixed_or_unsupported_python(self):
        for version in ("3.9.23", "3.10.18", "3.11.13", "3.12.11",
                        "3.13.4", "3.14.0"):
            with self.subTest(version=version):
                self.assertNotIn("archive extraction review", self.run_case(version=version))

    def test_version_probe_output_is_bounded(self):
        output = self.run_case(version="x" * 4096 + "\nPython 3.12.3")
        self.assertNotIn("archive extraction review", output)

    def test_skips_when_control_or_source_flow_is_missing(self):
        variants = (
            {"rule": "nobody"},
            {"wildcard": False},
            {"wildcard": False, "fixed_argument": True},
            {"archive_dir": False},
            {"source_kind": "fixed_archive"},
            {"source_kind": "safe_filter"},
            {"source_kind": "unrelated_extract"},
            {"source_kind": "different_archive"},
            {"source_kind": "outside_with"},
            {"source_kind": "overwritten_archive"},
            {"source_kind": "overwritten_handle"},
        )
        for variant in variants:
            with self.subTest(variant=variant):
                self.assertNotIn("archive extraction review", self.run_case(**variant))


if __name__ == "__main__":
    unittest.main()
