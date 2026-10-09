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

        [TestMethod]
        public void HistoricalEventParserKeepsOnlyBoundedExclusionNames()
        {
            var eventXml = @"<Event xmlns=""http://schemas.microsoft.com/win/2004/08/events/event""><EventData>" +
                @"<Data Name=""Product Name"">Defender</Data><Data Name=""Product Version"">1</Data>" +
                @"<Data Name=""Old Value"">Default</Data>" +
                @"<Data Name=""New Value"">HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths\C:\Build = 0x0</Data>" +
                @"</EventData></Event>";
            Assert.AreEqual("Paths: C:\\Build", SystemInfo.ExtractHistoricalDefenderExclusion(
                SystemInfo.ExtractDefenderEventNewValue(eventXml)));
            Assert.AreEqual("Paths: C:\\Build", SystemInfo.ExtractHistoricalDefenderExclusion(
                @"HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths\C:\Build = 0x0"));
            Assert.AreEqual("Processes: tool.exe", SystemInfo.ExtractHistoricalDefenderExclusion(
                @"HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions\Processes\tool.exe = 0x0"));
            Assert.IsNull(SystemInfo.ExtractHistoricalDefenderExclusion(
                @"HKLM\SOFTWARE\Microsoft\Windows Defender\ServiceStartStates = 0x1"));
            Assert.IsNull(SystemInfo.ExtractHistoricalDefenderExclusion(
                @"HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths\ = 0x0"));
            Assert.IsTrue(SystemInfo.ExtractHistoricalDefenderExclusion(
                @"HKLM\SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths\" + new string('A', 300) + " = 0x0")
                .Length < 180);
        }
    }
}
