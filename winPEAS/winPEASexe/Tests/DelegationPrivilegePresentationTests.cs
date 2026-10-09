using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Info.UserInfo.Token;

namespace winPEAS.Tests
{
    [TestClass]
    public class DelegationPrivilegePresentationTests
    {
        private const string Name = "SeEnableDelegationPrivilege";

        [TestMethod]
        public void OnlyAnEnabledCurrentTokenPrivilegeTriggersEmphasis()
        {
            var privileges = new Dictionary<string, string>
            {
                { Name, "SE_PRIVILEGE_ENABLED" },
                { "SeChangeNotifyPrivilege", "SE_PRIVILEGE_ENABLED" }
            };
            Assert.IsTrue(Token.IsPrivilegeEnabled(privileges, Name));
            Assert.IsFalse(Token.IsPrivilegeEnabled(privileges, "SeDebugPrivilege"));

            privileges[Name] = "SE_PRIVILEGE_ENABLED_BY_DEFAULT, SE_PRIVILEGE_ENABLED";
            Assert.IsTrue(Token.IsPrivilegeEnabled(privileges, Name));

            privileges[Name] = "DISABLED";
            Assert.IsFalse(Token.IsPrivilegeEnabled(privileges, Name));

            privileges[Name] = "SE_PRIVILEGE_ENABLED_BY_DEFAULT";
            Assert.IsFalse(Token.IsPrivilegeEnabled(privileges, Name));

            privileges[Name] = "unknown";
            Assert.IsFalse(Token.IsPrivilegeEnabled(privileges, Name));

            privileges.Remove(Name);
            Assert.IsFalse(Token.IsPrivilegeEnabled(privileges, Name));
            Assert.IsFalse(Token.IsPrivilegeEnabled(null, Name));
        }

        [TestMethod]
        public void NoteStatesScopeAndLimits()
        {
            StringAssert.Contains(UserInfo.DelegationPrivilegeNote, "this process token");
            StringAssert.Contains(UserInfo.DelegationPrivilegeNote, "domain machine-account quota");
            StringAssert.Contains(UserInfo.DelegationPrivilegeNote, "effective computer-object rights");
            StringAssert.Contains(UserInfo.DelegationPrivilegeNote, "does not prove unconstrained delegation or a coercion path");
        }
    }
}
