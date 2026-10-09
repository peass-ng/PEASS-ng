"""Static safety checks for the PowerShell DNS ACL enumeration.

Run with: python3 -m unittest winPEAS/winPEASps1/test_dns_acl_safeguards.py
"""

from pathlib import Path
import re
import shutil
import subprocess
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
    def test_review_mask_does_not_include_composite_generic_rights(self):
        source = function_source("Get-DnsZoneAceReviewSignal")
        mask = source.split("$reviewRights =", 1)[1].split("if (", 1)[0]
        self.assertNotIn("::GenericAll", mask)
        self.assertNotIn("::GenericWrite", mask)
        for right in ("CreateChild", "WriteProperty", "WriteDacl", "WriteOwner"):
            self.assertIn(f"::{right}", mask)

    @unittest.skipUnless(shutil.which("pwsh") or shutil.which("powershell"),
                         "PowerShell is not installed")
    def test_read_only_aces_are_not_write_review_signals(self):
        helper = "function Get-DnsZoneAceReviewSignal {\n" + function_source("Get-DnsZoneAceReviewSignal")
        fixtures = r"""
$ErrorActionPreference = 'Stop'
function Test-Ace($right, $sid = 'S-1-5-11', $type = 'Allow', $objectType = [guid]::Empty) {
  $ace = [pscustomobject]@{
    AccessControlType = $type
    IdentityReference = [pscustomobject]@{ Value = $sid }
    ActiveDirectoryRights = [System.DirectoryServices.ActiveDirectoryRights]$right
    ObjectType = $objectType
  }
  Get-DnsZoneAceReviewSignal -Ace $ace
}
foreach ($right in @('GenericRead', 'ReadControl', 'ReadProperty', 'ListChildren', 'ListObject', 'Delete')) {
  if ($null -ne (Test-Ace $right)) { throw "Read/non-write right reported: $right" }
}
foreach ($sid in @('S-1-1-0', 'S-1-5-11', 'S-1-5-21-111-222-333-513')) {
  foreach ($right in @('GenericAll', 'GenericWrite', 'CreateChild', 'WriteProperty', 'WriteDacl', 'WriteOwner')) {
    if ($null -eq (Test-Ace $right $sid)) { throw "Missing write review signal: $right $sid" }
  }
}
if ($null -ne (Test-Ace 'GenericAll' 'S-1-5-11' 'Deny')) { throw 'Deny ACE reported as Allow' }
if ($null -ne (Test-Ace 'GenericAll' 'S-1-5-32-544')) { throw 'Unrelated trustee reported' }
$scoped = Test-Ace 'WriteProperty' 'S-1-5-11' 'Allow' ([guid]'11111111-2222-3333-4444-555555555555')
if ($scoped.Scope -notlike '*scope unverified*') { throw 'Object-specific uncertainty lost' }
"""
        result = subprocess.run(
            [shutil.which("pwsh") or shutil.which("powershell"), "-NoProfile", "-NonInteractive",
             "-Command", helper + fixtures],
            text=True, capture_output=True, timeout=30,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

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
