using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Info.UserInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class SessionAndSchannelCuesTests
    {
        [TestMethod]
        public void WtsFailureDoesNotMeanNoSessions()
        {
            Assert.AreEqual(RdpSessionVisibility.Unknown,
                UserInfoHelper.AssessRdpSessionVisibility(false, 0));
        }

        [TestMethod]
        public void SuccessfulWtsEnumerationDistinguishesEmptyFromObserved()
        {
            Assert.AreEqual(RdpSessionVisibility.Empty,
                UserInfoHelper.AssessRdpSessionVisibility(true, 0));
            Assert.AreEqual(RdpSessionVisibility.Observed,
                UserInfoHelper.AssessRdpSessionVisibility(true, 1));
        }

        [TestMethod]
        public void SchannelUpnMappingRequiresBitFour()
        {
            Assert.AreEqual(ActiveDirectoryInfo.SchannelUpnMappingStatus.Enabled,
                ActiveDirectoryInfo.AssessSchannelUpnMapping(0x4));
            Assert.AreEqual(ActiveDirectoryInfo.SchannelUpnMappingStatus.Enabled,
                ActiveDirectoryInfo.AssessSchannelUpnMapping(0xC));
            Assert.AreEqual(ActiveDirectoryInfo.SchannelUpnMappingStatus.Disabled,
                ActiveDirectoryInfo.AssessSchannelUpnMapping(0x18));
            Assert.AreEqual(ActiveDirectoryInfo.SchannelUpnMappingStatus.Unknown,
                ActiveDirectoryInfo.AssessSchannelUpnMapping(null));
        }
    }
}
