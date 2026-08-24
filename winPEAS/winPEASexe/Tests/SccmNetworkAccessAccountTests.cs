using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using winPEAS.Info.WindowsCreds;

namespace winPEAS.Tests
{
    [TestClass]
    public class SccmNetworkAccessAccountTests
    {
        [TestMethod]
        public void ReportsConfiguredPoliciesWithoutReadingSecretValues()
        {
            SccmNetworkAccessAccountReport report = SccmNetworkAccessAccount.GetReport(() => 2);

            Assert.AreEqual(SccmNetworkAccessAccountStatus.Configured, report.Status);
            Assert.AreEqual(2, report.AccountCount);
            Assert.IsFalse(report.LimitReached);
            Assert.AreEqual(
                "SELECT SiteSettingsKey FROM CCM_NetworkAccessAccount",
                SccmNetworkAccessAccount.MetadataOnlyQuery);
        }

        [TestMethod]
        public void BoundsTheNumberOfReportedPolicies()
        {
            SccmNetworkAccessAccountReport report = SccmNetworkAccessAccount.GetReport(
                () => SccmNetworkAccessAccount.MaxAccounts + 1);

            Assert.AreEqual(SccmNetworkAccessAccount.MaxAccounts, report.AccountCount);
            Assert.IsTrue(report.LimitReached);
        }

        [TestMethod]
        public void DistinguishesNoPolicyFromAccessDenied()
        {
            SccmNetworkAccessAccountReport absent = SccmNetworkAccessAccount.GetReport(() => 0);
            SccmNetworkAccessAccountReport denied = SccmNetworkAccessAccount.GetReport(
                () => throw new UnauthorizedAccessException());

            Assert.AreEqual(SccmNetworkAccessAccountStatus.NotConfigured, absent.Status);
            Assert.AreEqual(SccmNetworkAccessAccountStatus.AccessDenied, denied.Status);
        }
    }
}
