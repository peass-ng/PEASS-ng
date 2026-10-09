"""Shared file selectors preserve their source data and platform exclusions."""

import sys
import re
import unittest
from pathlib import Path
from unittest.mock import patch


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from builder.src.peasLoaded import PEASLoaded  # noqa: E402
from builder.src.yamlGlobals import YAML_LOADED  # noqa: E402
from builder.src.linpeasBuilder import LinpeasBuilder  # noqa: E402


class SelectorCatalogLoadingTests(unittest.TestCase):
    def test_two_loads_preserve_cached_common_and_all_folders(self):
        before = [
            (record["name"], tuple(tuple(item["value"].get("search_in", ()))
                                   for item in record["value"]["files"]))
            for record in YAML_LOADED["search"]
        ]
        first = PEASLoaded()
        second = PEASLoaded()
        after = [
            (record["name"], tuple(tuple(item["value"].get("search_in", ()))
                                   for item in record["value"]["files"]))
            for record in YAML_LOADED["search"]
        ]
        self.assertEqual(before, after)
        self.assertEqual([item.name for item in first.peasrecords],
                         [item.name for item in second.peasrecords])
        self.assertTrue(next(item for item in second.peasrecords
                             if item.name == "Keepass").filerecords[0].search_in)

    def test_both_disable_locations_exclude_linux_records(self):
        def record(name, value_disable=None, config_disable=None):
            return {"name": name, "value": {
                "disable": value_disable,
                "config": {"auto_check": True, "disable": config_disable},
                "files": [],
            }}

        sample = {"search": [
            record("shared"), record("value disabled", ["linpeas"]),
            record("config disabled", config_disable=["linpeas"]),
            record("Windows disabled", ["winpeas"]),
        ]}
        with patch("builder.src.peasLoaded.YAML_LOADED", sample):
            names = [item.name for item in PEASLoaded().peasrecords]
        self.assertEqual(["shared", "Windows disabled"], names)

    def test_nested_root_folders_generate_valid_shell_variable_names(self):
        builder = LinpeasBuilder.__new__(LinpeasBuilder)
        builder.ploaded = PEASLoaded()
        builder.hidden_files = set()
        builder.bash_find_f_vars = set()
        builder.bash_find_d_vars = set()
        builder.bash_storages = set()
        builder._LinpeasBuilder__get_files_to_search()
        finds, _ = builder._LinpeasBuilder__generate_finds()
        names = [line.split("=", 1)[0] for line in finds]
        self.assertTrue(all(re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", name)
                            for name in names))
        self.assertIn("FIND_DATASTORE_BACKUPS", names)


if __name__ == "__main__":
    unittest.main()
