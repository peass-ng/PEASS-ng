"""Fast safeguards for the passive PowerShell gMSA reader inventory."""

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


class GmsaReaderBounds(unittest.TestCase):
    def test_ldap_query_is_passive_and_bounded(self):
        source = function_source("Get-GmsaReadersReport")
        for fragment in (
            "$searcher.PageSize = 0",
            "$searcher.SizeLimit = 121",
            "$searcher.ClientTimeout = [TimeSpan]::FromSeconds(5)",
            "$searcher.ServerTimeLimit = [TimeSpan]::FromSeconds(5)",
            "$searcher.ReferralChasing = [System.DirectoryServices.ReferralChasingOption]::None",
            "$report.Inspected -ge 120",
            "$watch.ElapsedMilliseconds -ge 8000",
            "$blobsSeen -ge 2",
            "$acesSeen -ge 128",
            "$principals.Count -ge 16",
            "$blob.Length -gt 16384",
        ):
            with self.subTest(fragment=fragment):
                self.assertIn(fragment, source)
        self.assertNotIn("msDS-ManagedPassword\"", source)
        self.assertNotRegex(source, r"(?i)\.CommitChanges\(|\.Save\(|\.Put\(")

    def test_partial_results_and_disposal_survive_error_and_cap(self):
        source = function_source("Get-GmsaReadersReport")
        for fragment in (
            "$report.State = if ($report.Truncated) { 'Partial' } else { 'Observed' }",
            "$report.State = if ($report.Inspected -gt 0) { 'Partial' } else { 'Unknown' }",
            "$report.Rows = @($sorted | Select-Object -First 20)",
            "$report.Omitted = $rows.Count - $report.Rows.Count",
            "@{Expression={ $_.WeakPrincipals -ne '' };Descending=$true}",
        ):
            self.assertIn(fragment, source)
        for resource in ("results", "searcher", "container", "domainEntry"):
            self.assertIn(f"${resource}.Dispose()", source)

    def test_broad_trustees_require_allow_ace_and_output_states_uncertainty(self):
        source = function_source("Get-GmsaReadersReport")
        caller = SCRIPT.split("$gmsaReport = Get-GmsaReadersReport", 1)[1].split(
            "$adcsInfo =", 1
        )[0]
        for sid in ("S-1-1-0", "S-1-5-11", "513$"):
            self.assertIn(sid, source)
        self.assertIn("$ace.AceQualifier -eq 'AccessAllowed'", source)
        self.assertIn("$gmsaReport.Rows", caller)
        self.assertIn("$gmsaReport.Omitted", caller)
        self.assertIn("$gmsaReport.State -ne 'Observed'", caller)
        self.assertIn("Remaining access is unknown", caller)

    @unittest.skipUnless(shutil.which("pwsh") or shutil.which("powershell"),
                         "PowerShell is not installed")
    def test_powershell_script_parses(self):
        exe = shutil.which("pwsh") or shutil.which("powershell")
        path = str(Path(__file__).with_name("winPEAS.ps1"))
        command = (
            "$tokens=$null; $errors=$null; "
            f"[System.Management.Automation.Language.Parser]::ParseFile('{path}',"
            "[ref]$tokens,[ref]$errors) | Out-Null; "
            "if ($errors.Count -ne 0) { $errors | Out-String | Write-Error; exit 1 }"
        )
        result = subprocess.run(
            [exe, "-NoProfile", "-NonInteractive", "-Command", command],
            text=True, capture_output=True, timeout=15,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
