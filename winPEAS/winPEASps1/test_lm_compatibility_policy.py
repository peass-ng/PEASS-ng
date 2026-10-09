"""Focused boundary checks for the local LAN Manager policy summary."""

import pathlib
import shutil
import subprocess
import unittest


SCRIPT = pathlib.Path(__file__).with_name("winPEAS.ps1")
IMAGE = "mcr.microsoft.com/powershell:7.5-ubuntu-24.04"


def policy_function(source, name="Get-LmCompatibilityPolicyReview"):
    start = source.index(f"function {name} {{")
    opening = source.index("{", start)
    depth = 0
    for index in range(opening, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise AssertionError("Policy function is incomplete")


def powershell_command():
    runtime = shutil.which("pwsh")
    if runtime:
        return [runtime, "-NoProfile", "-Command", "-"]
    if shutil.which("docker"):
        return [
            "docker", "run", "--rm", "-i", IMAGE,
            "pwsh", "-NoProfile", "-Command", "-",
        ]
    return None


class LmCompatibilityPolicyTests(unittest.TestCase):
    def test_existing_registry_read_and_output_are_connected(self):
        source = SCRIPT.read_text(encoding="utf-8-sig")
        self.assertIn("$lsa.LmCompatibilityLevel", source)
        self.assertEqual(source.count("$ntlmStatus = Get-NtlmPolicySummary"), 1)
        self.assertLess(
            source.index("$ntlmStatus = Get-NtlmPolicySummary"),
            source.index("if (-not $domainContext) {\n  Write-Host"),
        )
        self.assertIn("switch ($lmReview)", source)
        self.assertIn("permits NTLMv1 client responses by policy", source)
        self.assertIn("effective client policy unknown", source)
        self.assertIn("$lmValue = if ($lmReview -eq 'Unknown') { 'unknown' }", source)

    def test_policy_boundaries_in_powershell(self):
        source = SCRIPT.read_text(encoding="utf-8-sig")
        command = powershell_command()
        if not command:
            self.skipTest("PowerShell runtime unavailable")

        fixture = policy_function(source) + "\n" + "\n".join(
            f"$actual = Get-LmCompatibilityPolicyReview -Level {value}; "
            f"if ($actual -ne '{expected}') {{ throw 'level {value}: ' + $actual }}"
            for value, expected in [
                (0, "LegacyClient"), (1, "LegacyClient"), (2, "LegacyClient"),
                (3, "Ntlmv2Client"), (4, "Ntlmv2Client"), (5, "Ntlmv2Client"),
                (6, "Unknown"), (-1, "Unknown"),
                ("$null", "Unknown"), ("'invalid'", "Unknown"),
            ]
        ) + "\n" + policy_function(source, "Get-NtlmRestrictionValue") + "\n" + "\n".join(
            f"$actual = Get-NtlmRestrictionValue -Value {value}; "
            f"if ($actual -ne {expected}) {{ throw 'restriction {value}: ' + $actual }}"
            for value, expected in [
                (0, 0), (1, 1), (2, 2), (3, -1), (-1, -1),
                ("$null", -1), ("'invalid'", -1),
            ]
        ) + "\n"
        result = subprocess.run(command, input=fixture, text=True, capture_output=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertNotIn("Exception", result.stdout + result.stderr)

    def test_registry_reads_remain_independent(self):
        source = SCRIPT.read_text(encoding="utf-8-sig")
        command = powershell_command()
        if not command:
            self.skipTest("PowerShell runtime unavailable")
        fixture = (
            policy_function(source, "Get-NtlmPolicySummary")
            + "\nfunction Get-ItemProperty { param($Path, $ErrorAction)\n"
            + "if ($Path.EndsWith('MSV1_0')) { if ($script:msvAvailable) { return [pscustomobject]@{ RestrictReceivingNTLMTraffic = 1 } }; throw 'MSV absent' }\n"
            + "if ($script:lsaAvailable) { return [pscustomobject]@{ LmCompatibilityLevel = 2 } }; throw 'LSA absent'\n}\n"
            + "$script:msvAvailable = $false; $script:lsaAvailable = $true; $result = Get-NtlmPolicySummary; "
            + "if ($result.LmCompatibility -ne 2 -or $null -ne $result.RestrictReceiving) { throw 'LSA-only read lost' }\n"
            + "$script:msvAvailable = $true; $script:lsaAvailable = $false; $result = Get-NtlmPolicySummary; "
            + "if ($result.RestrictReceiving -ne 1 -or $null -ne $result.LmCompatibility) { throw 'MSV-only read lost' }\n"
            + "$script:msvAvailable = $false; $script:lsaAvailable = $false; $result = Get-NtlmPolicySummary; "
            + "if ($null -ne $result) { throw 'absent keys should be unknown' }\n"
        )
        result = subprocess.run(command, input=fixture, text=True, capture_output=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertNotIn("Exception", result.stdout + result.stderr)

    def test_local_policy_output_with_mocked_registry_summary(self):
        source = SCRIPT.read_text(encoding="utf-8-sig")
        command = powershell_command()
        if not command:
            self.skipTest("PowerShell runtime unavailable")
        block_start = source.index("$ntlmStatus = Get-NtlmPolicySummary")
        block_end = source.index("if (-not $domainContext) {\n  Write-Host", block_start)
        fixture = (
            policy_function(source)
            + "\nfunction Get-NtlmPolicySummary { return [pscustomobject]@{ LmCompatibility = $script:policyValue } }"
            + "\nfunction Write-Host { param($Object, $ForegroundColor) $script:messages += [string]$Object }"
            + "\nfunction Invoke-LocalPolicyReview {\n"
            + source[block_start:block_end]
            + "\n}\n"
        )
        cases = [
            ("2", "permits NTLMv1 client responses"),
            ("3", "specifies NTLMv2 client responses"),
            ("$null", "effective client policy unknown"),
            ("'invalid'", "effective client policy unknown"),
            ("6", "effective client policy unknown"),
        ]
        for value, expected in cases:
            fixture += (
                f"$script:policyValue = {value}; $script:messages = @(); Invoke-LocalPolicyReview; "
                f"if (-not (($script:messages -join ' ').Contains('{expected}'))) "
                f"{{ throw 'wrong output for {value}: ' + ($script:messages -join ' ') }}\n"
            )
        result = subprocess.run(command, input=fixture, text=True, capture_output=True, timeout=30)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertNotIn("Exception", result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
