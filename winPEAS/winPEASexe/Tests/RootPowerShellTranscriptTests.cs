using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;

namespace winPEAS.Tests
{
    [TestClass]
    public class RootPowerShellTranscriptTests
    {
        [TestMethod]
        public void FindsOnlyImmediateChildTranscriptPathsWithoutReadingContents()
        {
            string root = Path.Combine(Path.GetTempPath(), "transcript-inventory-" + Guid.NewGuid().ToString("N"));
            try
            {
                string day = Path.Combine(root, "20200101");
                Directory.CreateDirectory(day);
                string transcript = Path.Combine(day, "PowerShell_transcript.example.txt");
                File.WriteAllText(transcript, "test credential that must never be printed");
                File.WriteAllText(Path.Combine(day, "ordinary.txt"), "not a transcript");
                string deeper = Path.Combine(day, "nested");
                Directory.CreateDirectory(deeper);
                File.WriteAllText(Path.Combine(deeper, "PowerShell_transcript.deep.txt"), "out of scope");

                bool partial;
                var found = SystemInfo.FindRootPowerShellTranscripts(root, out partial);
                Assert.IsFalse(partial);
                Assert.AreEqual(1, found.Count);
                Assert.AreEqual(transcript, found[0]);
                Assert.IsFalse(found[0].Contains("credential"));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void FileCapReportsPartialInventory()
        {
            string root = Path.Combine(Path.GetTempPath(), "transcript-inventory-" + Guid.NewGuid().ToString("N"));
            try
            {
                string day = Path.Combine(root, "20200101");
                Directory.CreateDirectory(day);
                for (int i = 0; i < 65; i++)
                    File.WriteAllText(Path.Combine(day, "PowerShell_transcript." + i + ".txt"), "x");

                bool partial;
                var found = SystemInfo.FindRootPowerShellTranscripts(root, out partial);
                Assert.IsTrue(partial);
                Assert.AreEqual(64, found.Count);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void MissingRootHasNoFindings()
        {
            string root = Path.Combine(Path.GetTempPath(), "transcript-inventory-" + Guid.NewGuid().ToString("N"));
            bool partial;
            var found = SystemInfo.FindRootPowerShellTranscripts(root, out partial);
            Assert.IsFalse(partial);
            Assert.AreEqual(0, found.Count);
        }

        [TestMethod]
        public void UncRootIsRejectedBeforeDirectoryAccess()
        {
            bool partial;
            var found = SystemInfo.FindRootPowerShellTranscripts(@"\\example.invalid\share\PSTranscripts", out partial);
            Assert.IsFalse(partial);
            Assert.AreEqual(0, found.Count);
        }
    }
}
