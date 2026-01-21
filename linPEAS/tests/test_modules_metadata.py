import re
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

    def _iter_module_files(self):
        return sorted(self.parts_dir.rglob("*.sh"))

    def test_all_modules_parse(self):
        module_files = self._iter_module_files()
        self.assertGreater(len(module_files), 0, "No linPEAS module files were found.")

        # Parsing a module validates its metadata and dependencies.
        for path in module_files:
            _ = self.LinpeasModule(str(path))

    def test_check_module_id_matches_filename(self):
        for path in self._iter_module_files():
            module = self.LinpeasModule(str(path))
            if not getattr(module, "is_check", False):
                continue

            # For checks, the filename (without numeric prefix) must match the module ID
            # (either full ID or stripping section prefix like `SI_`).
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
        for path in self._iter_module_files():
            module = self.LinpeasModule(str(path))
            ids.append(getattr(module, "id", ""))

        duplicates = {x for x in ids if x and ids.count(x) > 1}
        self.assertEqual(set(), duplicates, f"Duplicate module IDs found: {sorted(duplicates)}")


if __name__ == "__main__":
    unittest.main()
