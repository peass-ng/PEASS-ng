import re
import shutil
import subprocess
import sys
import unittest
from pathlib import Path


class LinpeasModulesMetadataTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.linpeas_dir = cls.repo_root / "linPEAS"
        cls.parts_dir = cls.linpeas_dir / "builder" / "linpeas_parts"

        # Ensure `import builder.*` works when tests are run from repo root.
        sys.path.insert(0, str(cls.linpeas_dir))

        from builder.src.linpeasModule import LinpeasModule  # pylint: disable=import-error

        cls.LinpeasModule = LinpeasModule
        cls.module_files = sorted(cls.parts_dir.rglob("*.sh"))
        cls.modules = [cls.LinpeasModule(str(path)) for path in cls.module_files]

    def _iter_module_files(self):
        return self.module_files

    def test_all_modules_parse(self):
        self.assertGreater(len(self.modules), 0, "No linPEAS module files were found.")

    def test_check_module_id_matches_filename(self):
        for module in self.modules:
            if not getattr(module, "is_check", False):
                continue

            # For checks, the filename (without numeric prefix) must match the module ID
            # (either full ID or stripping section prefix like `SI_`).
            path = Path(module.path)
            file_base = re.sub(r"^[0-9]+_", "", path.stem)
            module_id = getattr(module, "id", "")
            module_id_tail = module_id[3:] if len(module_id) >= 3 else ""
            self.assertIn(
                file_base,
                {module_id, module_id_tail},
                f"Module ID mismatch in {path}: id={module_id} expected suffix={file_base}",
            )

    def test_module_ids_are_unique(self):
        ids = []
        for module in self.modules:
            ids.append(getattr(module, "id", ""))

        duplicates = {x for x in ids if x and ids.count(x) > 1}
        self.assertEqual(set(), duplicates, f"Duplicate module IDs found: {sorted(duplicates)}")

    def test_module_shell_snippets_are_syntactically_valid(self):
        shell = shutil.which("sh")
        self.assertIsNotNone(shell, "sh is required to syntax-check linPEAS snippets.")

        failures = []
        for path in self._iter_module_files():
            result = subprocess.run([shell, "-n", str(path)], capture_output=True, text=True)
            if result.returncode != 0:
                failures.append(f"{path}: {result.stderr.strip()}")

        self.assertEqual([], failures)

    def test_declared_function_dependencies_exist(self):
        defined_functions = {
            function_name
            for module in self.modules
            for function_name in getattr(module, "defined_funcs", [])
        }

        missing = []
        for module in self.modules:
            for function_name in sorted(set(module.functions_used + module.initial_functions)):
                if function_name not in defined_functions:
                    missing.append(f"{module.path}: {function_name}")

        self.assertEqual([], missing)

    def test_declared_global_variable_dependencies_exist(self):
        # These are shell/runtime values intentionally supplied by the environment
        # or CLI parser instead of a linPEAS variable module.
        runtime_globals = {
            "IP",
            "RANDOM",
        }
        generated_globals = {
            variable_name
            for module in self.modules
            for variable_name in getattr(module, "generated_global_variables", [])
        }

        missing = []
        for module in self.modules:
            for variable_name in sorted(set(module.global_variables)):
                if variable_name not in generated_globals and variable_name not in runtime_globals:
                    missing.append(f"{module.path}: ${variable_name}")

        self.assertEqual([], missing)

    def test_sudo_l_check_is_bounded_for_non_interactive_runs(self):
        sudo_l_module = self.parts_dir / "6_users_information" / "7_Sudo_l.sh"
        content = sudo_l_module.read_text(encoding="utf-8")

        self.assertIn('"$TIMEOUT" 15 sudo -S -l', content)
        self.assertIn("sudo -n -l", content)
        self.assertNotRegex(content, r"secure_path_line=\$\(sudo\s+-l\b")


if __name__ == "__main__":
    unittest.main()
