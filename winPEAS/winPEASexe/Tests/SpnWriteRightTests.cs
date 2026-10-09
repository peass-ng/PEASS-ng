using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class SpnWriteRightTests
    {
        [TestMethod]
        public void AttributeWriteAndValidatedSelfRemainDistinct()
        {
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.WriteProperty,
                ActiveDirectoryInfo.ClassifySpnWrite("servicePrincipalName", false));
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.ValidatedSelf,
                ActiveDirectoryInfo.ClassifySpnWrite("Validated-SPN", true));
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.ValidatedSelf,
                ActiveDirectoryInfo.ClassifySpnWrite("Validated write to service principal name", true));
        }

        [TestMethod]
        public void MismatchedAceKindAndUnrelatedGuidNameDoNotClaimSpnControl()
        {
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.None,
                ActiveDirectoryInfo.ClassifySpnWrite("servicePrincipalName", true));
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.None,
                ActiveDirectoryInfo.ClassifySpnWrite("Validated-SPN", false));
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.None,
                ActiveDirectoryInfo.ClassifySpnWrite("member", false));
            Assert.AreEqual(ActiveDirectoryInfo.SpnWriteRight.None,
                ActiveDirectoryInfo.ClassifySpnWrite("", true));
        }
    }
}
