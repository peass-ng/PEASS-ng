using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.SystemInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class FixedBuildIndicatorTests
    {
        private static readonly FixedBuildIndicator Indicator = new FixedBuildIndicator
        {
            product = "Windows Server 2022",
            build = 20348,
            fixed_ubr = 2527,
            fixed_kb = "5039227"
        };

        private static Dictionary<string, string> Info(string build, string ubr)
        {
            return new Dictionary<string, string>
            {
                ["ProductName"] = "Windows Server 2022 Datacenter",
                ["CurrentBuild"] = build,
                ["UpdateBuildRevision"] = ubr
            };
        }

        [TestMethod]
        public void BelowCnaFixedBuildIsAnIndicatorOnly()
        {
            Assert.AreEqual(FixedBuildStatus.BelowFixedBuild,
                WindowsVersionVulns.AssessFixedBuild(Info("20348", "2113"), Indicator));
        }

        [TestMethod]
        public void ExactMicrosoftReleaseBuildIsFixed()
        {
            Assert.AreEqual(FixedBuildStatus.FixedBuild,
                WindowsVersionVulns.AssessFixedBuild(Info("20348", "2527"), Indicator));
            var differentlyCasedProduct = Info("20348", "2527");
            differentlyCasedProduct["ProductName"] = "WINDOWS SERVER 2022 Datacenter";
            Assert.AreEqual(FixedBuildStatus.FixedBuild,
                WindowsVersionVulns.AssessFixedBuild(differentlyCasedProduct, Indicator));
        }

        [TestMethod]
        public void NewerOrMissingRevisionAndOtherProductsRemainUnknown()
        {
            Assert.AreEqual(FixedBuildStatus.Unknown,
                WindowsVersionVulns.AssessFixedBuild(Info("20348", "3000"), Indicator));
            Assert.AreEqual(FixedBuildStatus.Unknown,
                WindowsVersionVulns.AssessFixedBuild(Info("20348", ""), Indicator));
            Assert.AreEqual(FixedBuildStatus.Unknown,
                WindowsVersionVulns.AssessFixedBuild(Info("26100", "2527"), Indicator));
        }
    }
}
