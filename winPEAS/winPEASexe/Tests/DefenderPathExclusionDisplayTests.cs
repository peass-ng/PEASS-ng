using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class DefenderPathExclusionDisplayTests
    {
        [TestMethod]
        public void PolicyOnlyExclusionIsDisplayedWithoutLocalExclusion()
        {
            var lines = SystemInfo.FormatDefenderPathExclusions(
                new List<string>(), new List<string> { @"C:\PolicyOnly" }).ToArray();

            CollectionAssert.AreEqual(new[] {
                "\n  PolicyManagerPathExclusions:",
                @"    C:\PolicyOnly"
            }, lines);
        }

        [TestMethod]
        public void LocalAndPolicyExclusionsRemainDistinct()
        {
            var lines = SystemInfo.FormatDefenderPathExclusions(
                new List<string> { @"C:\LocalOnly" },
                new List<string> { @"D:\PolicyOnly" }).ToArray();

            CollectionAssert.AreEqual(new[] {
                "\n  Path Exclusions:",
                @"    C:\LocalOnly",
                "\n  PolicyManagerPathExclusions:",
                @"    D:\PolicyOnly"
            }, lines);
        }

        [TestMethod]
        public void EmptyExclusionsProduceNoSection()
        {
            Assert.AreEqual(0, SystemInfo.FormatDefenderPathExclusions(
                new List<string>(), new List<string>()).Count());
        }
    }
}
