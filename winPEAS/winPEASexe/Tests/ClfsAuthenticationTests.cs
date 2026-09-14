using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.SystemInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class ClfsAuthenticationTests
    {
        [TestMethod]
        public void RecognizesEnforcedAndDisabledModes()
        {
            Assert.AreEqual(
                ClfsAuthenticationStatus.Enforced,
                ClfsAuthentication.Evaluate(0, 0).Status);
            Assert.AreEqual(
                ClfsAuthenticationStatus.DisabledByAdministrator,
                ClfsAuthentication.Evaluate(2, null).Status);
            Assert.AreEqual(
                ClfsAuthenticationStatus.DisabledBySystem,
                ClfsAuthentication.Evaluate(3L, null).Status);
        }

        [TestMethod]
        public void DistinguishesNormalAndIndefiniteLearningModes()
        {
            ClfsAuthenticationReport normal = ClfsAuthentication.Evaluate(1, 7776000);
            ClfsAuthenticationReport indefinite = ClfsAuthentication.Evaluate(1, 0);
            ClfsAuthenticationReport defaultPeriod = ClfsAuthentication.Evaluate(1, null);

            Assert.AreEqual(ClfsAuthenticationStatus.Learning, normal.Status);
            Assert.AreEqual((ulong)7776000, normal.EnforcementTransitionPeriod.Value);
            Assert.AreEqual(
                ClfsAuthenticationStatus.LearningWithoutAutoEnforcement,
                indefinite.Status);
            Assert.AreEqual(ClfsAuthenticationStatus.Learning, defaultPeriod.Status);
        }

        [TestMethod]
        public void TreatsMalformedOrUndocumentedModesAsUnknown()
        {
            Assert.AreEqual(
                ClfsAuthenticationStatus.Unknown,
                ClfsAuthentication.Evaluate("not-a-number", 10).Status);
            Assert.AreEqual(
                ClfsAuthenticationStatus.Unknown,
                ClfsAuthentication.Evaluate("2", 10).Status);
            Assert.AreEqual(
                ClfsAuthenticationStatus.Unknown,
                ClfsAuthentication.Evaluate(4, 10).Status);
            Assert.AreEqual(
                ClfsAuthenticationStatus.Unknown,
                ClfsAuthentication.Evaluate(-1, 10).Status);
        }
    }
}
