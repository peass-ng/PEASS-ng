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
