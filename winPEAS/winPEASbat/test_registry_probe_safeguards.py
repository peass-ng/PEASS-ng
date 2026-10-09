"""Guard the service-registry check against state-changing permission probes."""

from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
BAT = (Path(__file__).with_name("winPEAS.bat")).read_text(encoding="utf-8")
AD = (ROOT / "winPEASexe/winPEAS/Checks/ActiveDirectoryInfo.cs").read_text(
    encoding="utf-8"
)
PATTERNS = (ROOT / "winPEASexe/winPEAS/Helpers/Search/Patterns.cs").read_text(
    encoding="utf-8-sig"
)


class RegistryProbeSafeguards(unittest.TestCase):
    def test_batch_service_registry_probe_is_read_only(self):
        block = BAT.split(":CheckRegistryModificationAbilities\n", 1)[1].split(
            ":UnquotedServicePaths\n", 1
        )[0]
        self.assertIn("write access is unknown", block)
        self.assertNotRegex(block.lower(), r"\breg(?:\.exe)?\s+(?:save|restore|add|delete|import|load|unload|copy)\b")
        self.assertNotIn("reg.hiv", block.lower())

    def test_staged_computer_ldap_inventory_has_explicit_bounds_and_no_auth(self):
        block = AD.split("private void PrintStagedComputerCandidates()", 1)[1].split(
            "// Show matching ACL leads", 1
        )[0]
        for required in (
            "searcher.SizeLimit = sampleLimit + 1",
            "searcher.ClientTimeout = TimeSpan.FromSeconds(5)",
            "searcher.ServerTimeLimit = TimeSpan.FromSeconds(5)",
            "if (inspected == sampleLimit)",
            "if (displayed.Count >= displayLimit)",
            "ReferralChasingOption.None",
            "Candidate visibility unknown",
        ):
            self.assertIn(required, block)
        self.assertRegex(block, r"const int sampleLimit = 120;")
        self.assertRegex(block, r"const int displayLimit = 20;")
        self.assertNotRegex(
            block.lower(),
            r"\b(?:passwordguess|authenticate|logonuser|resetpassword|setpassword)\s*\(",
        )

    def test_access_database_metadata_uses_existing_user_file_selector(self):
        whitelist = PATTERNS.split("WhitelistExtensions", 1)[1].split("};", 1)[0]
        self.assertIn('".accdb"', whitelist)
        self.assertIn('".mdb"', whitelist)


if __name__ == "__main__":
    unittest.main()
