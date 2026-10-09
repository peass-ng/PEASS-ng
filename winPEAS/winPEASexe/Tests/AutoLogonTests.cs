using System.Collections.Generic;
using Microsoft.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.UserInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class AutoLogonTests
    {
        [TestMethod]
        public void NativeRegistryViewDependsOnOperatingSystemArchitecture()
        {
            // A 32-bit winPEAS process on x64 must still read native Winlogon.
            Assert.AreEqual(RegistryView.Registry64, UserInfoHelper.GetAutoLogonRegistryView(true));
            Assert.AreEqual(RegistryView.Registry32, UserInfoHelper.GetAutoLogonRegistryView(false));
        }

        [TestMethod]
        public void PlaintextPasswordsAreHighRiskEvenWhenAutoLogonIsDisabled()
        {
            var defaultPassword = new Dictionary<string, string>
            {
                ["AutoAdminLogon"] = "0",
                ["DefaultPassword"] = "secret"
            };
            var alternatePassword = new Dictionary<string, string>
            {
                ["AltDefaultPassword"] = "alternate"
            };

            Assert.AreEqual(AutoLogonFinding.PlaintextPassword, UserInfoHelper.ClassifyAutoLogon(defaultPassword));
            Assert.AreEqual(AutoLogonFinding.PlaintextPassword, UserInfoHelper.ClassifyAutoLogon(alternatePassword));
        }

        [TestMethod]
        public void EnabledWithoutPlaintextIsOnlyAnInformationalLead()
        {
            var values = new Dictionary<string, string>
            {
                ["AutoAdminLogon"] = "1",
                ["DefaultUserName"] = "user",
                ["DefaultPassword"] = "",
                ["AltDefaultPassword"] = ""
            };

            Assert.AreEqual(AutoLogonFinding.EnabledWithoutPlaintextPassword, UserInfoHelper.ClassifyAutoLogon(values));
        }

        [TestMethod]
        public void UsernameOnlyWithAutoLogonDisabledIsNotACredential()
        {
            var values = new Dictionary<string, string>
            {
                ["AutoAdminLogon"] = "0",
                ["DefaultDomainName"] = "domain",
                ["DefaultUserName"] = "user"
            };

            Assert.AreEqual(AutoLogonFinding.None, UserInfoHelper.ClassifyAutoLogon(values));
        }

        [TestMethod]
        public void AbsentValuesDoNotProduceAFinding()
        {
            Assert.AreEqual(AutoLogonFinding.None, UserInfoHelper.ClassifyAutoLogon(new Dictionary<string, string>()));
            Assert.AreEqual(AutoLogonFinding.None, UserInfoHelper.ClassifyAutoLogon(null));
        }
    }
}
