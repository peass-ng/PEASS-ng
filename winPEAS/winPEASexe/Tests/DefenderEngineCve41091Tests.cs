using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.SystemInfo.WindowsDefender;

namespace winPEAS.Tests
{
    [TestClass]
    public class DefenderEngineCve41091Tests
    {
        [TestMethod]
        public void VersionsBeforeTheMicrosoftFixedFloorAreCandidates()
        {
            Assert.AreEqual(DefenderCve41091Status.Candidate,
                WindowsDefender.AssessCve41091Engine("1.1.26030.3008"));
            Assert.AreEqual(DefenderCve41091Status.Candidate,
                WindowsDefender.AssessCve41091Engine("1.1.26040.7"));
        }

        [TestMethod]
        public void FixedFloorAndNewerEnginesAreFixed()
        {
            Assert.AreEqual(DefenderCve41091Status.Fixed,
                WindowsDefender.AssessCve41091Engine("1.1.26040.8"));
            Assert.AreEqual(DefenderCve41091Status.Fixed,
                WindowsDefender.AssessCve41091Engine("1.1.26050.11"));
        }

        [TestMethod]
        public void MissingUnloadedAndMalformedVersionsRemainUnknown()
        {
            foreach (string version in new[]
            {
                null, "", "0.0.0.0", "1.1.26040", "1.1.26040.8.1", "not-a-version"
            })
            {
                Assert.AreEqual(DefenderCve41091Status.Unknown,
                    WindowsDefender.AssessCve41091Engine(version), version ?? "null");
            }
        }
    }
}
