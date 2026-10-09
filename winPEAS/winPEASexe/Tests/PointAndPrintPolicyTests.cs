using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class PointAndPrintPolicyTests
    {
        [TestMethod]
        public void AdminOnlyOverrideTakesPrecedenceOverOtherPolicyValues()
        {
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.ExplicitAdminOnly,
                SystemInfo.AssessPointAndPrintPolicy(1, 1, 2));
        }

        [TestMethod]
        public void EachExplicitWeakSettingIsIndependentlyReviewable()
        {
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.ReviewCandidate,
                SystemInfo.AssessPointAndPrintPolicy(0, null, null));
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.ReviewCandidate,
                SystemInfo.AssessPointAndPrintPolicy(null, 1, null));
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.ReviewCandidate,
                SystemInfo.AssessPointAndPrintPolicy(null, null, 2));
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.ReviewCandidate,
                SystemInfo.AssessPointAndPrintPolicy(null, null, 1));
        }

        [TestMethod]
        public void MissingValuesAndZeroPromptsDoNotProveProtection()
        {
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.NoExplicitWeakValues,
                SystemInfo.AssessPointAndPrintPolicy(null, null, null));
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.NoExplicitWeakValues,
                SystemInfo.AssessPointAndPrintPolicy(null, 0, 0));
        }

        [TestMethod]
        public void UnknownRestrictionValueIsNotClassifiedAsProtected()
        {
            Assert.AreEqual(SystemInfo.PointAndPrintPolicyStatus.UnknownValue,
                SystemInfo.AssessPointAndPrintPolicy(2, 0, 0));
        }
    }
}
