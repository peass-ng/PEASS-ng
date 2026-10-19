using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class AsRepRoastCandidateTests
    {
        [TestMethod]
        public void RequiresEnabledNonMachineAccountWithPreauthenticationDisabled()
        {
            Assert.IsTrue(ActiveDirectoryInfo.IsAsRepRoastCandidate(0x400200));
            Assert.IsTrue(ActiveDirectoryInfo.IsAsRepRoastCandidate(0x410200));
            Assert.IsFalse(ActiveDirectoryInfo.IsAsRepRoastCandidate(0x200));
            Assert.IsFalse(ActiveDirectoryInfo.IsAsRepRoastCandidate(0x400202));
            Assert.IsFalse(ActiveDirectoryInfo.IsAsRepRoastCandidate(0x401000));
            Assert.IsFalse(ActiveDirectoryInfo.IsAsRepRoastCandidate(0x402000));
            Assert.IsFalse(ActiveDirectoryInfo.IsAsRepRoastCandidate(null));
        }
    }
}
