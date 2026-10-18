"""Small builds retain declared checks without executing generated shell code."""

import copy
import sys
import tempfile
import time
import unittest
from pathlib import Path
from types import MappingProxyType
from unittest.mock import patch


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from builder.src import linpeasBaseBuilder as base_builder  # noqa: E402


class SmallModuleSelectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.selector = object.__new__(base_builder.LinpeasBaseBuilder)
        started = time.perf_counter()
        print("FIXTURE_PARSE_BEGIN", flush=True)
        modules = cls.selector.get_modules(True, False, False, False, [], [])
        print("FIXTURE_PARSE_COMPLETE modules=%d elapsed_seconds=%.3f" %
              (len(modules), time.perf_counter() - started), flush=True)
        # Parse and validate the real catalog once. Templates stay private;
        # every constructor replacement returns a fresh deep copy.
        cls._module_templates = MappingProxyType({
            module.path: copy.deepcopy(module) for module in modules
        })
        cls.checks = {module.id: module for module in modules if module.is_check}
        cls.small_ids = {key for key, module in cls.checks.items() if module.is_small}
        cls.non_fat_ids = {key for key, module in cls.checks.items() if not module.is_fat}

    def setUp(self):
        self.selector = object.__new__(base_builder.LinpeasBaseBuilder)
        self.checks = copy.deepcopy(type(self).checks)
        self.small_ids = type(self).small_ids.copy()
        self.non_fat_ids = type(self).non_fat_ids.copy()

    def select(self, **overrides):
        options = dict(
            all_modules=False,
            all_no_fat_modules=False,
            no_network_scanning=False,
            small=False,
            include_modules=[],
            exclude_modules=[],
        )
        options.update(overrides)
        # Keep real directory enumeration, matching, filtering, and selection.
        # Only repeated parsing of the same immutable fixture is substituted.
        def fresh_module(path):
            return copy.deepcopy(self._module_templates[path])

        with patch.object(base_builder, "LinpeasModule", side_effect=fresh_module):
            return self.selector.get_modules(**options)

    def check_ids(self, **options):
        return {module.id for module in self.select(**options) if module.is_check}

    def test_small_alone_selects_declared_small_checks(self):
        self.assertIn("SY_Path", self.small_ids)
        self.assertNotIn("SY_Date", self.small_ids)
        self.assertSetEqual(self.check_ids(small=True), self.small_ids)

    def test_other_build_modes_preserve_selection(self):
        cases = [
            ({"all_modules": True}, set(self.checks)),
            ({"all_no_fat_modules": True}, self.non_fat_ids),
            ({"include_modules": ["sy_path", "sy_date"]}, {"SY_Path", "SY_Date"}),
            ({"small": True, "include_modules": ["sy_path", "sy_date"]}, {"SY_Path"}),
            ({"small": True, "include_modules": ["sy_date"]}, set()),
            ({"small": True, "include_modules": ["no_such_review_module"]}, set()),
            ({"small": True, "all_modules": True}, self.small_ids),
            ({"small": True, "all_no_fat_modules": True}, self.small_ids & self.non_fat_ids),
        ]
        for options, expected in cases:
            with self.subTest(options=options):
                self.assertSetEqual(self.check_ids(**options), expected)

    def test_small_still_honors_module_and_section_exclusions(self):
        self.assertSetEqual(
            self.check_ids(small=True, exclude_modules=["sy_path"]),
            self.small_ids - {"SY_Path"},
        )
        cloud_ids = {
            key for key, module in self.checks.items()
            if module.section_info["name_check"] == "cloud"
        }
        self.assertSetEqual(
            self.check_ids(small=True, exclude_modules=["3_cloud"]),
            self.small_ids - cloud_ids,
        )

    def test_small_still_honors_no_network_scanning(self):
        ordinary = self.select(small=True)
        without_network_jobs = self.select(small=True, no_network_scanning=True)
        self.assertTrue(any("check_network_jobs" in module.path for module in ordinary))
        self.assertFalse(any("check_network_jobs" in module.path for module in without_network_jobs))
        self.assertSetEqual(
            {module.id for module in without_network_jobs if module.is_check},
            self.small_ids,
        )

    def test_small_base_output_contains_selected_check_code(self):
        # Only the base assembler is used: no external-tool downloads, catalog
        # expansion, subprocesses, or execution of generated shell code.
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "linpeas_base.sh"
            with patch.object(base_builder, "TEMPORARY_LINPEAS_BASE_PATH", str(output)), \
                 patch.object(base_builder, "LINPEAS_PARTS", copy.deepcopy(base_builder.LINPEAS_PARTS)):
                # This integration call remains uncached and reparses the real
                # catalog before resolving dependencies and assembling text.
                started = time.perf_counter()
                print("UNCACHED_ASSEMBLY_BEGIN", flush=True)
                builder = base_builder.LinpeasBaseBuilder(False, False, False, True, [], [])
                builder.build()
                print("UNCACHED_ASSEMBLY_COMPLETE checks=%d elapsed_seconds=%.3f" %
                      (len(builder.checks), time.perf_counter() - started), flush=True)
            content = output.read_text(encoding="utf-8")

        self.assertRegex(content, r'(?m)^CHECKS="[^"]+"')
        self.assertIn('print_title "System Information"', content)
        self.assertIn(self.checks["SY_Path"].sh_code, content)
        self.assertNotIn(self.checks["SY_Date"].sh_code, content)


if __name__ == "__main__":
    unittest.main()
