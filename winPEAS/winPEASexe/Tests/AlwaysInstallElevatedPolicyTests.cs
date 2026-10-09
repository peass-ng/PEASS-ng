using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class AlwaysInstallElevatedPolicyTests
    {
        [TestMethod]
        public void BothValuesMustBeOneForReviewCandidate()
        {
            Assert.AreEqual(SystemInfo.AlwaysInstallElevatedStatus.BothEnabled,
                SystemInfo.AssessAlwaysInstallElevated("1", "1"));
            Assert.AreEqual(SystemInfo.AlwaysInstallElevatedStatus.OneEnabled,
                SystemInfo.AssessAlwaysInstallElevated("1", "0"));
            Assert.AreEqual(SystemInfo.AlwaysInstallElevatedStatus.OneEnabled,
                SystemInfo.AssessAlwaysInstallElevated("", "1"));
            Assert.AreEqual(SystemInfo.AlwaysInstallElevatedStatus.NotConfirmed,
                SystemInfo.AssessAlwaysInstallElevated("0", "0"));
        }

        [TestMethod]
        public void MissingAndUnexpectedValuesStayVisibleWithoutRawData()
        {
            Assert.AreEqual(SystemInfo.AlwaysInstallElevatedStatus.OneEnabled,
                SystemInfo.AssessAlwaysInstallElevated("1", "bad"));
            Assert.AreEqual(SystemInfo.AlwaysInstallElevatedStatus.NotConfirmed,
                SystemInfo.AssessAlwaysInstallElevated(null, "2"));
            Assert.AreEqual("missing", SystemInfo.DescribeAlwaysInstallElevatedValue(null));
            Assert.AreEqual("missing", SystemInfo.DescribeAlwaysInstallElevatedValue(""));
            Assert.AreEqual("unexpected value", SystemInfo.DescribeAlwaysInstallElevatedValue("bad"));
            Assert.AreEqual("0", SystemInfo.DescribeAlwaysInstallElevatedValue("0"));
            Assert.AreEqual("1", SystemInfo.DescribeAlwaysInstallElevatedValue("1"));
        }
    }
}
