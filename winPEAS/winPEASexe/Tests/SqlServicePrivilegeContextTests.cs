using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ServicesInfo;

namespace Tests
{
    [TestClass]
    public class SqlServicePrivilegeContextTests
    {
        [TestMethod]
        public void MatchesOnlyConfiguredSqlServiceNames()
        {
            Assert.IsTrue(ServicesInfoHelper.IsSqlServiceName("MSSQLSERVER"));
            Assert.IsTrue(ServicesInfoHelper.IsSqlServiceName("SQLSERVERAGENT"));
            Assert.IsTrue(ServicesInfoHelper.IsSqlServiceName("MSSQL$APP"));
            Assert.IsTrue(ServicesInfoHelper.IsSqlServiceName("SQLAgent$APP"));
            Assert.IsFalse(ServicesInfoHelper.IsSqlServiceName("MSSQL$"));
            Assert.IsFalse(ServicesInfoHelper.IsSqlServiceName("OtherMSSQLSERVER"));
            Assert.AreEqual("line next", ServicesInfoHelper.FormatSqlServiceValue("line\nnext"));
            Assert.IsTrue(ServicesInfoHelper.FormatSqlServiceValue(new string('x', 100)).Length <= 83);
        }

        [TestMethod]
        public void ComparesOnlyTheSameKnownAccountAndKnownToken()
        {
            string[] required = { "SeChangeNotifyPrivilege", "SeImpersonatePrivilege" };
            Assert.IsTrue(ServicesInfoHelper.IsCurrentServiceAccount(@".\svc", @"HOST\svc", "HOST"));
            Assert.IsFalse(ServicesInfoHelper.IsCurrentServiceAccount(@"OTHER\svc", @"HOST\svc", "HOST"));
            StringAssert.Contains(ServicesInfoHelper.ClassifySqlServicePrivilegeContext(
                @"OTHER\svc", @"HOST\svc", "HOST", required, false), "no token comparison");
            StringAssert.Contains(ServicesInfoHelper.ClassifySqlServicePrivilegeContext(
                @"HOST\svc", @"HOST\svc", "HOST", null, false), "unavailable");
            StringAssert.Contains(ServicesInfoHelper.ClassifySqlServicePrivilegeContext(
                @"HOST\svc", @"HOST\svc", "HOST", required, null), "unavailable");
            StringAssert.Contains(ServicesInfoHelper.ClassifySqlServicePrivilegeContext(
                @"HOST\svc", @"HOST\svc", "HOST", required, false), "unverified");
            StringAssert.Contains(ServicesInfoHelper.ClassifySqlServicePrivilegeContext(
                @"HOST\svc", @"HOST\svc", "HOST", required, true), "also present");
            StringAssert.Contains(ServicesInfoHelper.ClassifySqlServicePrivilegeContext(
                @"HOST\svc", @"HOST\svc", "HOST", new string[0], false), "not listed");
        }
    }
}
