using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Info.ApplicationInfo;
using winPEAS.TaskScheduler;

namespace winPEAS.Tests
{
    [TestClass]
    public class ScheduledApplicationDisplayTests
    {
        [TestMethod]
        public void ClassifiesOnlyPasswordBasedLogonsAsPossibleLeads()
        {
            foreach (TaskLogonType logon in new[] { TaskLogonType.Password, TaskLogonType.InteractiveTokenOrPassword })
            {
                string lead = ApplicationInfoHelper.GetScheduledCredentialLead(logon);
                StringAssert.Contains(lead, "Possible LSA-secret lead");
                StringAssert.Contains(lead, "admin or SYSTEM");
            }

            foreach (TaskLogonType logon in new[] { TaskLogonType.S4U, TaskLogonType.InteractiveToken,
                TaskLogonType.ServiceAccount, TaskLogonType.Group, TaskLogonType.None })
            {
                Assert.AreEqual(string.Empty, ApplicationInfoHelper.GetScheduledCredentialLead(logon));
                var app = ApplicationInfoHelper.CreateScheduledApp("Job", "Vendor", null, "job.exe", null,
                    new[] { "job.exe" }, "user", null, logon, TaskRunLevel.LUA);
                Assert.IsFalse(ApplicationsInfo.FormatScheduledApp(app, null, null).Contains("LSA-secret"));
            }
        }

        [TestMethod]
        public void IncludesEmptyAuthorAndFiltersMicrosoftCaseInsensitively()
        {
            Assert.IsTrue(ApplicationInfoHelper.ShouldIncludeScheduledTask(@"\Vendor\Updater", null));
            Assert.IsTrue(ApplicationInfoHelper.ShouldIncludeScheduledTask(@"\Vendor\Updater", ""));
            Assert.IsFalse(ApplicationInfoHelper.ShouldIncludeScheduledTask(@"\Microsoft\Windows\Updater", "Vendor"));
            Assert.IsFalse(ApplicationInfoHelper.ShouldIncludeScheduledTask(@"\Vendor\Updater", "mIcRoSoFt"));
            Assert.IsFalse(ApplicationInfoHelper.ShouldIncludeScheduledTask(null, "Vendor"));
        }

        [TestMethod]
        public void FormatsPrincipalActionPathRightsAndExistingFields()
        {
            var app = ApplicationInfoHelper.CreateScheduledApp("Updater", null, "Vendor update",
                "\"C:\\Program Files\\Vendor Agent\\update.exe\" --run",
                new[] { "Daily" }, new[] { @"C:\Program Files\Vendor Agent\update.exe" },
                @"DOMAIN\operator", null, TaskLogonType.Password, TaskRunLevel.Highest);
            string output = ApplicationsInfo.FormatScheduledApp(app,
                new[] { "update.exe: Write" }, new[] { "Vendor Agent: Modify" });

            StringAssert.Contains(output, "(author unknown) Updater:");
            StringAssert.Contains(output, @"Principal: DOMAIN\operator; Logon type: Password; Run level: Highest");
            StringAssert.Contains(output, @"Action path: C:\Program Files\Vendor Agent\update.exe");
            StringAssert.Contains(output, "Permissions file (current user): update.exe: Write");
            StringAssert.Contains(output, "Permissions parent directory (current user): Vendor Agent: Modify");
            StringAssert.Contains(output, "Trigger: Daily");
            StringAssert.Contains(output, "Vendor update");
            StringAssert.Contains(output, "Possible LSA-secret lead");
        }

        [TestMethod]
        public void IncludesLiteralTaskScriptTargetsWithoutTreatingInlineCodeAsFiles()
        {
            var cmdPaths = ApplicationInfoHelper.GetScheduledActionReferencedPaths(
                @"C:\Windows\System32\cmd.exe", @"/c call ""C:\Jobs\daily clean.bat""",
                null);
            CollectionAssert.AreEqual(new[] { @"C:\Jobs\daily clean.bat" }, cmdPaths);

            var psPaths = ApplicationInfoHelper.GetScheduledActionReferencedPaths(
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                @"-NoProfile -File ""C:\Jobs\daily clean.ps1""", null);
            CollectionAssert.AreEqual(new[] { @"C:\Jobs\daily clean.ps1" }, psPaths);

            var relativePsPaths = ApplicationInfoHelper.GetScheduledActionReferencedPaths(
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                @"-NoProfile -File ""daily clean.ps1""", @"C:\Jobs");
            CollectionAssert.AreEqual(new[] { @"C:\Jobs\daily clean.ps1" }, relativePsPaths);

            Assert.AreEqual(0, ApplicationInfoHelper.GetScheduledActionReferencedPaths(
                @"C:\Windows\System32\cmd.exe", @"/c echo C:\Jobs\not-run.bat", null).Count);
            Assert.AreEqual(0, ApplicationInfoHelper.GetScheduledActionReferencedPaths(
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                @"-Command ""C:\Jobs\not-run.ps1""", null).Count);
            Assert.AreEqual(0, ApplicationInfoHelper.GetScheduledActionReferencedPaths(
                @"C:\Tools\writer.exe", @"--output C:\Jobs\not-run.bat", null).Count);
        }

        [TestMethod]
        public void FindsOnlyLiteralPowerShellFileTargetsInsideSmallScheduledBatchFiles()
        {
            string content = "@echo off\r\n" +
                "rem powershell.exe -File skipped.ps1\r\n" +
                "@powershell.exe -NoProfile -File \"rotate task.ps1\"\r\n" +
                "call \"C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe\" -File C:\\Jobs\\second.ps1\r\n" +
                "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe -windowstyle hidden -exec bypass -nop -file C:\\Jobs\\third.ps1 C:\\Logs\r\n" +
                "echo powershell.exe -File skipped.ps1\r\n" +
                "powershell.exe -Command Write-Output -File skipped.ps1\r\n" +
                "powershell.exe -File %TARGET%\r\n" +
                "powershell.exe -File C:\\Jobs\\third.ps1 & echo done\r\n";
            CollectionAssert.AreEqual(new[] { @"C:\Jobs\rotate task.ps1", @"C:\Jobs\second.ps1", @"C:\Jobs\third.ps1" },
                ApplicationInfoHelper.ParseScheduledBatchPowerShellPaths(content, @"C:\Jobs"));
            Assert.AreEqual(0, ApplicationInfoHelper.ParseScheduledBatchPowerShellPaths(
                "powershell.exe -File task.ps1", null).Count);
            Assert.AreEqual(0, ApplicationInfoHelper.ParseScheduledBatchPowerShellPaths(
                new string('x', 16385), @"C:\Jobs").Count);
        }

        [TestMethod]
        public void SelectsLiteralSnortConfigFromScheduledActionOnly()
        {
            Assert.AreEqual(@"C:\Snort\etc\snort.conf", ApplicationInfoHelper.GetSnortConfigPath(
                @"C:\Snort\bin\snort.exe", @"-i 1 -c C:\Snort\etc\snort.conf -l C:\Snort\log"));
            Assert.AreEqual(@"C:\Program Files\Snort\etc\snort.conf", ApplicationInfoHelper.GetSnortConfigPath(
                @"C:\Program Files\Snort\bin\snort.exe", @"-c ""C:\Program Files\Snort\etc\snort.conf"""));
            Assert.IsNull(ApplicationInfoHelper.GetSnortConfigPath(@"C:\Snort\bin\other.exe", @"-c C:\Snort\etc\snort.conf"));
            Assert.IsNull(ApplicationInfoHelper.GetSnortConfigPath("snort.exe", @"-c C:\Snort\etc\snort.conf"));
            Assert.IsNull(ApplicationInfoHelper.GetSnortConfigPath(@"C:\Snort\bin\snort.exe", @"-c \\server\snort.conf"));
            Assert.IsNull(ApplicationInfoHelper.GetSnortConfigPath(@"C:\Snort\bin\snort.exe", "-c "));
        }

        [TestMethod]
        public void SelectsOnlyBoundedLiteralModuleDirectories()
        {
            string config = "# dynamicpreprocessor directory C:\\ignored\\lib\n" +
                "dynamicpreprocessor directory C:\\Snort\\lib\\snort_dynamicpreprocessor\n" +
                "dynamicpreprocessor directory C:\\Snort\\lib\\snort_dynamicpreprocessor\n" +
                "dynamicpreprocessor file C:\\Snort\\lib\\other.dll\n" +
                "dynamicpreprocessor directory \\\\server\\share\\modules\n";
            var paths = ApplicationInfoHelper.GetSnortDynamicPreprocessorDirectories(config);
            Assert.AreEqual(1, paths.Count);
            Assert.AreEqual(@"C:\Snort\lib\snort_dynamicpreprocessor", paths[0]);
            Assert.AreEqual(0, ApplicationInfoHelper.GetSnortDynamicPreprocessorDirectories(
                new string('x', 65537)).Count);
        }

        [TestMethod]
        public void CapsConfigReadEvenWhenStreamContainsMoreThanInitialLimit()
        {
            using (var small = new MemoryStream(Encoding.UTF8.GetBytes(
                "dynamicpreprocessor directory C:\\Snort\\lib\\modules")))
                StringAssert.Contains(ApplicationInfoHelper.ReadSnortConfigCapped(small), "dynamicpreprocessor");
            using (var grown = new MemoryStream(new byte[65537]))
                Assert.IsNull(ApplicationInfoHelper.ReadSnortConfigCapped(grown));
        }

        [TestMethod]
        public void ComparesTaskPrincipalsLocallyAndLeavesAmbiguousNamesUnknown()
        {
            const string sid = "S-1-5-21-100-200-300-1100";
            Assert.IsTrue(ApplicationInfoHelper.IsDifferentTaskPrincipalWithoutLookup(
                @"DOMAIN\other", @"DOMAIN\current", sid));
            Assert.IsFalse(ApplicationInfoHelper.IsDifferentTaskPrincipalWithoutLookup(
                @"DOMAIN\current", @"DOMAIN\current", sid));
            Assert.IsTrue(ApplicationInfoHelper.IsDifferentTaskPrincipalWithoutLookup(
                "S-1-5-18", @"DOMAIN\current", sid));
            Assert.IsFalse(ApplicationInfoHelper.IsDifferentTaskPrincipalWithoutLookup(
                sid, @"DOMAIN\current", sid));
            Assert.IsFalse(ApplicationInfoHelper.IsDifferentTaskPrincipalWithoutLookup(
                "current@domain.test", @"DOMAIN\current", sid));
            Assert.IsFalse(ApplicationInfoHelper.IsDifferentTaskPrincipalWithoutLookup(
                "S-1-5-invalid", @"DOMAIN\current", sid));
        }

        [TestMethod]
        public void BoundsLiteralLocalPathsBeforeFilesystemInspection()
        {
            Assert.IsTrue(ApplicationInfoHelper.HasBoundedLocalPathComponents(
                @"C:\Snort\lib\snort_dynamicpreprocessor"));
            Assert.IsFalse(ApplicationInfoHelper.HasBoundedLocalPathComponents(
                @"C:\Snort\..\private\config.conf"));
            Assert.IsFalse(ApplicationInfoHelper.HasBoundedLocalPathComponents(
                @"\\server\share\config.conf"));
            Assert.IsFalse(ApplicationInfoHelper.HasBoundedLocalPathComponents(
                "C:\\" + string.Join("\\", Enumerable.Repeat("part", 33))));
            Assert.IsFalse(ApplicationInfoHelper.HasBoundedLocalPathComponents(
                @"C:\Snort\etc\" + new string('x', 500)));
        }

        [TestMethod]
        public void HandlesMissingFieldsAndNonPasswordPrincipalWithoutWarning()
        {
            var app = ApplicationInfoHelper.CreateScheduledApp(null, "Vendor", null, null, null, null,
                null, "Operators", TaskLogonType.Group, TaskRunLevel.LUA);
            string output = ApplicationsInfo.FormatScheduledApp(app, null, null);

            StringAssert.Contains(output, "Principal: Operators; Logon type: Group; Run level: LUA");
            StringAssert.Contains(output, "Action path: unknown");
            Assert.IsFalse(output.Contains("LSA-secret"));
            Assert.IsFalse(output.Contains("Permissions file"));
            Assert.IsFalse(output.Contains("Trigger:"));
        }
    }
}
