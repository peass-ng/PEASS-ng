using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.SystemInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class WindowsVersionVulnsTests
    {
        [TestMethod]
        public void CurrentWindows11BuildsMapToTheirOwnProductDefinitions()
        {
            AssertProductCandidate(26200, "x64-based", "25H2");
            AssertProductCandidate(26300, "x64-based", "26H2");
            AssertProductCandidate(28000, "ARM64-based", "26H1");
        }

        private static void AssertProductCandidate(int build, string architecture, string version)
        {
            var basicInfo = new Dictionary<string, string>
            {
                { "OS Name", "Microsoft Windows 11" },
                { "ProductName", "Windows 11" },
                { "OS Version", "10.0." + build + " Build " + build },
                { "System Type", architecture }
            };

            var report = WindowsVersionVulns.GetVulnerabilityReport(basicInfo);
            string product = "Windows 11 Version " + version + " for " + architecture + " Systems";
            CollectionAssert.Contains(report.CandidateProducts, product);
            CollectionAssert.Contains(report.MatchedProducts, product);
        }
    }
}
