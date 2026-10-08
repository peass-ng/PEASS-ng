using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class CheckmkAgentVersionTests
    {
        [TestMethod]
        public void ComparesPatchBoundariesForAffectedBranches()
        {
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Candidate,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.1.0p39"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Fixed,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.1.0p40"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Candidate,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.2.0p22"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Fixed,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.2.0p23"));
        }

        [TestMethod]
        public void HandlesEolAndNewBetaBranchesConservatively()
        {
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Candidate,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.0.0p39"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Candidate,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.0.0p40"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Fixed,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.3.0b1"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Fixed,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.3.0b2"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Fixed,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.4.0b1"));
            Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Fixed,
                ApplicationsInfo.ClassifyCheckmkAgentVersion("2.3.0p1"));
        }

        [TestMethod]
        public void TreatsBranchOnlyAndMalformedVersionsAsUnknown()
        {
            foreach (string version in new[] { null, "", "2.0", "2.1", "2.1.0", "2.2.0", "2.3.0", "2.3.0b0", "2.1.0p", "2.1.0pX", "2.1.1p39", "2.1.0.39", "junk" })
            {
                Assert.AreEqual(ApplicationsInfo.CheckmkVersionStatus.Unknown,
                    ApplicationsInfo.ClassifyCheckmkAgentVersion(version), version ?? "null");
            }
        }

        [TestMethod]
        public void MatchesOnlyTheAgentProduct()
        {
            Assert.IsTrue(ApplicationsInfo.IsCheckmkAgentProduct("Check MK Agent 2.1"));
            Assert.IsTrue(ApplicationsInfo.IsCheckmkAgentProduct("Check_MK Agent 2.2.0p22"));
            Assert.IsTrue(ApplicationsInfo.IsCheckmkAgentProduct("Checkmk Agent"));
            Assert.IsFalse(ApplicationsInfo.IsCheckmkAgentProduct("Checkmk Agent Updater"));
            Assert.IsFalse(ApplicationsInfo.IsCheckmkAgentProduct("Checkmk Server 2.1"));
            Assert.IsFalse(ApplicationsInfo.IsCheckmkAgentProduct("Other Check MK Agent 2.1"));
        }
    }
}
