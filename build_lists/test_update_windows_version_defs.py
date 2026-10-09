import unittest

from build_lists.update_windows_version_defs import (
    RawEntry,
    add_server_2022_fixed_build_indicator,
    build_definitions,
)
from build_lists.validate_windows_version_defs import validate_entry


class WindowsVersionDefinitionsTests(unittest.TestCase):
    def test_server_2022_fixed_build_comes_from_cna_range(self):
        data = {}
        record = {
            "cveMetadata": {"cveId": "CVE-2024-30088"},
            "containers": {"cna": {"affected": [{
                "vendor": "Microsoft",
                "product": "Windows Server 2022",
                "platforms": ["x64-based Systems"],
                "versions": [{"version": "10.0.20348.0", "lessThan": "10.0.20348.2527", "status": "affected"}],
            }]}},
        }
        add_server_2022_fixed_build_indicator(data, record)
        indicator = data["fixed_build_indicators"]["CVE-2024-30088"]
        self.assertEqual((indicator["build"], indicator["fixed_ubr"], indicator["fixed_kb"]),
                         (20348, 2527, "5039227"))
        record["containers"]["cna"]["affected"][0]["versions"][0]["lessThan"] = "10.0.20348.2528"
        with self.assertRaises(ValueError):
            add_server_2022_fixed_build_indicator({}, record)

    def test_non_cve_advisories_do_not_break_generated_definitions(self):
        def entry(identifier, kb):
            return RawEntry(
                cve=identifier,
                kb=kb,
                product="Adobe Flash Player on Windows 10 Version 1511 for 32-bit Systems",
                severity="Critical",
                impact="Remote Code Execution",
                supersedes=("1234567",),
            )

        data = build_definitions(
            [entry("ADV160008", "2345678"), entry("CVE-2016-1234", "3456789")],
            {"ADV160008", "CVE-2016-1234"},
            "20261001",
        )
        vulnerabilities = data["products"]["Adobe Flash Player on Windows 10 Version 1511 for 32-bit Systems"]
        self.assertEqual([v["cve"] for v in vulnerabilities], ["CVE-2016-1234"])
        validate_entry("Windows", 0, vulnerabilities[0])
        self.assertEqual(data["kb_supersedes"]["2345678"], ["1234567"])


if __name__ == "__main__":
    unittest.main()
