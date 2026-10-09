using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class WritableServiceRecoveryCommandTests
    {
        [TestMethod]
        public void EligibilityRequiresEnabledUnprotectedLocalSystemWin32Service()
        {
            Assert.IsTrue(ServicesInfoHelper.IsEligibleRecoveryService(0x10, 2, 0, "LocalSystem"));
            Assert.IsTrue(ServicesInfoHelper.IsEligibleRecoveryService(0x20, 3, null, string.Empty));

            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(0x1, 2, 0, "LocalSystem"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(0x10, 4, 0, "LocalSystem"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(0x10, 2, 1, "LocalSystem"));
            Assert.IsFalse(ServicesInfoHelper.IsEligibleRecoveryService(
                0x10,
                2,
                0,
                @"NT AUTHORITY\LocalService"));
        }

        [TestMethod]
        public void ParsesQuotedExpandedAndUnquotedRecoveryExecutables()
        {
            string executable;
            string arguments;

            Assert.IsTrue(ServicesInfoHelper.TryParseRecoveryCommand(
                @"""%SystemRoot%\System32\cmd.exe"" /c C:\ProgramData\Vendor\recover.cmd",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.AreEqual(@"C:\Windows\System32\cmd.exe", executable);
            Assert.AreEqual(@"/c C:\ProgramData\Vendor\recover.cmd", arguments);

            Assert.IsTrue(ServicesInfoHelper.TryParseRecoveryCommand(
                @"C:\Program Files\Vendor\recovery.exe --quiet",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.AreEqual(@"C:\Program Files\Vendor\recovery.exe", executable);
            Assert.AreEqual("--quiet", arguments);

            Assert.IsTrue(ServicesInfoHelper.TryParseRecoveryCommand(
                @"cmd /c C:\ProgramData\Vendor\helper.exe",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.AreEqual(@"C:\Windows\System32\cmd.exe", executable);
            Assert.AreEqual(@"/c C:\ProgramData\Vendor\helper.exe", arguments);
        }

        [TestMethod]
        public void ResolvesKnownInterpreterAndReferencedScriptTargets()
        {
            var targets = ServicesInfoHelper.GetRecoveryCommandTargets(
                @"cmd.exe /c C:\ProgramData\Vendor\recover.cmd",
                @"C:\Windows");

            CollectionAssert.Contains(targets, @"C:\Windows\System32\cmd.exe");
            CollectionAssert.Contains(targets, @"C:\ProgramData\Vendor\recover.cmd");
            Assert.AreEqual(2, targets.Distinct(System.StringComparer.OrdinalIgnoreCase).Count());
        }

        [TestMethod]
        public void RejectsRemoteUnexpandedAndMalformedCommands()
        {
            string executable;
            string arguments;

            Assert.IsFalse(ServicesInfoHelper.TryParseRecoveryCommand(
                @"\\server\share\recover.exe",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.IsFalse(ServicesInfoHelper.TryParseRecoveryCommand(
                @"%UNKNOWN_RECOVERY_ROOT%\recover.exe",
                @"C:\Windows",
                out executable,
                out arguments));
            Assert.IsFalse(ServicesInfoHelper.TryParseRecoveryCommand(
                @"""C:\Program Files\Vendor\recovery.exe --quiet",
                @"C:\Windows",
                out executable,
                out arguments));
        }
    }

    [TestClass]
    public class ServiceCommandLineTests
    {
        [TestMethod]
        public void PairedFlagsAreCandidatesAndArgumentValuesNeverAppearInDisplay()
        {
            var cases = new[]
            {
                @"C:\Windows\helper.exe -u name -p sampleSecret",
                @"""C:\Program Files\Vendor\helper.exe"" --user name --password sampleSecret",
                @"""C:\Program Files\Vendor\helper.exe"" --user=name --password=sampleSecret",
                @"C:\Vendor\helper.exe /user:name /password:sampleSecret"
            };
            foreach (string command in cases)
            {
                ServiceCommandLineAssessment result = ServicesInfoHelper.AssessServiceCommandLine(command);
                Assert.IsTrue(result.CredentialPairCandidate, command);
                Assert.IsFalse(result.UnpairedPasswordOption, command);
                StringAssert.Contains(result.DisplayPath, "[arguments redacted]");
                Assert.IsFalse(result.DisplayPath.Contains("sampleSecret"), result.DisplayPath);
                Assert.IsFalse(result.DisplayPath.Contains("name -p"), result.DisplayPath);
            }
            Assert.IsTrue(ServicesInfoHelper.AssessServiceCommandLine(cases[1]).DisplayPath.StartsWith(
                @"""C:\Program Files\Vendor\helper.exe""", StringComparison.Ordinal));
        }

        [TestMethod]
        public void UnpairedOrUnrelatedFlagsAreOnlyAmbiguous()
        {
            var lonePassword = ServicesInfoHelper.AssessServiceCommandLine(@"C:\Vendor\helper.exe -p sampleSecret");
            Assert.IsFalse(lonePassword.CredentialPairCandidate);
            Assert.IsTrue(lonePassword.UnpairedPasswordOption);
            Assert.IsFalse(lonePassword.DisplayPath.Contains("sampleSecret"));

            var unrelated = ServicesInfoHelper.AssessServiceCommandLine(@"C:\Vendor\helper.exe -p 8080 --port 1234");
            Assert.IsFalse(unrelated.CredentialPairCandidate);
            var missingValue = ServicesInfoHelper.AssessServiceCommandLine(@"C:\Vendor\helper.exe -u -p");
            Assert.IsFalse(missingValue.CredentialPairCandidate);
            var malformed = ServicesInfoHelper.AssessServiceCommandLine("unknown --user name --password sampleSecret");
            Assert.IsFalse(malformed.CredentialPairCandidate);
            Assert.IsFalse(malformed.DisplayPath.Contains("sampleSecret"));
        }

        [TestMethod]
        public void WmiFailureUsesRegistryOnlyInventoryWithoutDisplayName()
        {
            bool registryRead = false;
            ServiceRegistryInventory selected = ServicesInfoHelper.SelectNonstandardServices(
                () => { throw new InvalidOperationException("WMI unavailable"); },
                () =>
                {
                    registryRead = true;
                    ServiceRegistryInventory inventory = ServicesInfoHelper.ReadRegistryServiceEntries(
                        new[] { "VendorSvc" },
                        name => new Dictionary<string, object>
                        {
                            ["ImagePath"] = @"C:\Vendor\helper.exe -u name -p sampleSecret"
                        }, 2);
                    return inventory;
                });
            Assert.IsTrue(registryRead);
            Assert.AreEqual(1, selected.Entries.Count);
            Assert.IsTrue(selected.UsedRegistry);
            Assert.AreEqual("VendorSvc", ServicesInfoHelper.GetRegistryServiceDisplayName(selected.Entries[0]));
            ServiceCommandLineAssessment assessment = ServicesInfoHelper.AssessServiceCommandLine(
                Convert.ToString(selected.Entries[0].Values["ImagePath"]));
            Assert.IsTrue(assessment.CredentialPairCandidate);
            Assert.IsFalse(assessment.DisplayPath.Contains("sampleSecret"));
        }

        [TestMethod]
        public void UnreadableRegistryKeyDoesNotHideLaterEntriesAndCapIsVisible()
        {
            ServiceRegistryInventory inventory = ServicesInfoHelper.ReadRegistryServiceEntries(
                new[] { "Unreadable", "Visible", "BeyondCap" },
                name => name == "Unreadable" ? null : new Dictionary<string, object>
                {
                    ["ImagePath"] = @"C:\Vendor\helper.exe"
                }, 2);
            Assert.AreEqual(2, inventory.Inspected);
            Assert.AreEqual(1, inventory.Unreadable);
            Assert.AreEqual(1, inventory.Entries.Count);
            Assert.AreEqual("Visible", inventory.Entries[0].Name);
            Assert.IsTrue(inventory.LimitReached);
            Assert.AreEqual("Visible", ServicesInfoHelper.GetRegistryServiceDisplayName(inventory.Entries[0]));
        }
    }
}
