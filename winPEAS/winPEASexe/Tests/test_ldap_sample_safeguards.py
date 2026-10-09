"""Guard server-side caps on the PR's bounded, read-only LDAP samples."""

from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[2]
AD = (ROOT / "winPEASexe/winPEAS/Checks/ActiveDirectoryInfo.cs").read_text(
    encoding="utf-8"
)
PS = (ROOT / "winPEASps1/winPEAS.ps1").read_text(encoding="utf-8")


class LdapSampleSafeguards(unittest.TestCase):
    def test_csharp_size_capped_samples_do_not_enable_paging(self):
        for variable, limit in (
            ("search", "121"),
            ("searcher", "sampleLimit + 1"),
            ("ds", "OuSampleLimit + 1"),
            ("ds", "SampleObjectLimit + 1"),
            ("ds", "searchLimit"),
        ):
            with self.subTest(limit=limit):
                self.assertRegex(
                    AD,
                    rf"{variable}\.PageSize = 0;[^\n]*\n\s*"
                    rf"{variable}\.SizeLimit = {re.escape(limit)};",
                )

    def test_object_sample_has_timeouts_and_no_referrals(self):
        block = AD.split("ds.SizeLimit = SampleObjectLimit + 1;", 1)[1].split(
            "using (var results = ds.FindAll())", 1
        )[0]
        for required in (
            "ds.ReferralChasing = ReferralChasingOption.None;",
            "ds.ClientTimeout = OuSearchTimeout;",
            "ds.ServerTimeLimit = OuSearchTimeout;",
        ):
            self.assertIn(required, block)

    def test_powershell_spn_sample_is_nonpaged_and_disposes_owned_entries(self):
        block = PS.split("function Get-PrivilegedSpnTargets {", 1)[1].split(
            "function Get-NtlmPolicySummary", 1
        )[0]
        self.assertIn("$searcher.PageSize = 0", block)
        self.assertIn("$searcher.SizeLimit = 201", block)
        self.assertIn("ReferralChasingOption]::None", block)
        for resource in ("results", "searcher", "container", "domainEntry"):
            self.assertIn(f"${resource}.Dispose()", block)


if __name__ == "__main__":
    unittest.main()
