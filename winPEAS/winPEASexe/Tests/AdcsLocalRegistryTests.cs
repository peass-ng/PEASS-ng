using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class AdcsLocalRegistryTests
    {
        [TestMethod]
        public void MemberServerCaChecksCaSettingsWithoutDcMappings()
        {
            var result = ActiveDirectoryInfo.AssessLocalAdcsRegistry(false, "Issuing CA", 0);

            Assert.IsFalse(result.CheckDcMappings);
            Assert.IsTrue(result.CheckCaSettings);
            Assert.AreEqual(ActiveDirectoryInfo.Esc11RegistryStatus.Candidate, result.Esc11Status);
        }

        [TestMethod]
        public void DomainControllerMappingsDoNotRequireLocalCa()
        {
            var result = ActiveDirectoryInfo.AssessLocalAdcsRegistry(true, null, null);

            Assert.IsTrue(result.CheckDcMappings);
            Assert.IsFalse(result.CheckCaSettings);
        }

        [TestMethod]
        public void MemberServerWithoutCaSkipsBothLocalChecks()
        {
            var result = ActiveDirectoryInfo.AssessLocalAdcsRegistry(false, "  ", null);

            Assert.IsFalse(result.CheckDcMappings);
            Assert.IsFalse(result.CheckCaSettings);
        }

        [TestMethod]
        public void MissingOrUnreadableInterfaceFlagsAreUnknown()
        {
            var result = ActiveDirectoryInfo.AssessLocalAdcsRegistry(false, "Issuing CA", null);

            Assert.IsTrue(result.CheckCaSettings);
            Assert.AreEqual(ActiveDirectoryInfo.Esc11RegistryStatus.Unknown, result.Esc11Status);
        }

        [TestMethod]
        public void ZeroInterfaceFlagsAreAnEsc11Candidate()
        {
            var result = ActiveDirectoryInfo.AssessLocalAdcsRegistry(true, "Issuing CA", 0);

            Assert.IsTrue(result.CheckDcMappings);
            Assert.IsTrue(result.CheckCaSettings);
            Assert.AreEqual(ActiveDirectoryInfo.Esc11RegistryStatus.Candidate, result.Esc11Status);
        }

        [TestMethod]
        public void EncryptionBitProtectsAgainstEsc11Relay()
        {
            var result = ActiveDirectoryInfo.AssessLocalAdcsRegistry(false, "Issuing CA", 0x200);

            Assert.AreEqual(ActiveDirectoryInfo.Esc11RegistryStatus.Protected, result.Esc11Status);
            Assert.AreEqual(
                ActiveDirectoryInfo.Esc11RegistryStatus.Protected,
                ActiveDirectoryInfo.AssessLocalAdcsRegistry(false, "Issuing CA", 0x201).Esc11Status);
        }
    }
}
