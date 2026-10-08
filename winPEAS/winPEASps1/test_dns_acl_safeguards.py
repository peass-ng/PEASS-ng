"""Static safety checks for the PowerShell DNS ACL enumeration.

Run with: python3 -m unittest winPEAS/winPEASps1/test_dns_acl_safeguards.py
"""

from pathlib import Path
import re
import unittest


SCRIPT = Path(__file__).with_name("winPEAS.ps1").read_text(encoding="utf-8")


def function_source(name):
    match = re.search(
        rf"(?ms)^function {re.escape(name)} \{{\n(.*?)(?=^function |\Z)", SCRIPT
    )
    if not match:
        raise AssertionError(f"Missing PowerShell function: {name}")
    return match.group(1)


class DnsAclSafeguards(unittest.TestCase):
    def test_only_three_known_direct_child_zone_containers(self):
        source = function_source("Get-WeakDnsUpdateFindings")
        self.assertEqual(source.count("LDAP://CN=MicrosoftDNS,"), 3)
        self.assertIn("DC=DomainDnsZones,$domainDN", source)
        self.assertIn("DC=ForestDnsZones,$forestDN", source)
        self.assertIn('"LDAP://CN=MicrosoftDNS,$domainDN"', source)
        self.assertIn("$searcher.SearchScope = [System.DirectoryServices.SearchScope]::OneLevel", source)
        self.assertIn("$searcher.ReferralChasing = [System.DirectoryServices.ReferralChasingOption]::None", source)
        self.assertIn("$searcher.Filter = '(objectClass=dnsZone)'", source)

    def test_search_and_processing_have_explicit_limits(self):
        source = function_source("Get-WeakDnsUpdateFindings")
        for required in (
            "$searcher.PageSize = 0",
            "$searcher.SizeLimit = 51",
            "$searcher.ClientTimeout = [TimeSpan]::FromMilliseconds([math]::Min(2000, $remaining))",
            "$searcher.ServerTimeLimit = $searcher.ClientTimeout",
            "$remaining = 8000 - $clock.ElapsedMilliseconds",
            "$seen -ge 50",
            "$acesSeen -ge 256",
            "$findings.Count -ge 40",
        ):
            self.assertIn(required, source)
        self.assertGreaterEqual(source.count("$clock.ElapsedMilliseconds -ge 8000"), 2)

    def test_dacl_is_requested_and_resources_are_disposed(self):
        source = function_source("Get-WeakDnsUpdateFindings")
        self.assertIn("$searcher.SecurityMasks = [System.DirectoryServices.SecurityMasks]::Dacl", source)
        self.assertIn("@('name', 'nTSecurityDescriptor')", source)
        self.assertNotIn("GetDirectoryEntry()", source)
        for resource in ("results", "searcher", "container"):
            self.assertIn(f"${resource}.Dispose()", source)

    def test_uncertainty_and_review_language_reach_output(self):
        source = function_source("Get-WeakDnsUpdateFindings")
        helper = function_source("Get-DnsZoneAceReviewSignal")
        caller = SCRIPT.split("$dnsReport = Get-WeakDnsUpdateFindings", 1)[1].split(
            "$spnReport =", 1
        )[0]
        self.assertIn("Unavailable = @(); Truncated = @()", source)
        self.assertIn("$report.Unavailable +=", source)
        self.assertIn("$report.Truncated +=", source)
        self.assertIn("'^S-1-1-0$'", helper)
        self.assertIn("'^S-1-5-11$'", helper)
        self.assertIn("'^S-1-5-21-([0-9]+-){3}513$'", helper)
        self.assertIn("AccessControlType -ne 'Allow'", helper)
        self.assertIn("dnsNode or attribute scope unverified", helper)
        self.assertIn("$dnsReport.Findings", caller)
        self.assertIn("$dnsReport.Unavailable", caller)
        self.assertIn("$dnsReport.Truncated", caller)
        self.assertIn("do not establish DNS write access or relay viability", caller)


if __name__ == "__main__":
    unittest.main()
