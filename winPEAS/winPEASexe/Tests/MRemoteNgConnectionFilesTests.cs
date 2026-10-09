using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class MRemoteNgConnectionFilesTests
    {
        private const string Header = "<mrng:Connections xmlns:mrng=\"http://mremoteng.org\" ConfVersion=\"2.6\" EncryptionEngine=\"AES\" FullFileEncryption=\"false\">";
        private const string Node = "<Node Hostname=\"example.invalid\" Protocol=\"RDP\" Username=\"sample\" Password=\"synthetic-encrypted-value\" />";

        [TestMethod]
        public void ArbitraryNamedXmlIsFoundInCurrentDocuments()
        {
            string root = Path.Combine(Path.GetTempPath(), "mremote-fixture-" + Guid.NewGuid().ToString("N"));
            string documents = Path.Combine(root, "Documents");
            try
            {
                Directory.CreateDirectory(documents);
                File.WriteAllText(Path.Combine(documents, "config.xml"), Header + Node + "</mrng:Connections>");
                File.WriteAllText(Path.Combine(documents, "unrelated.xml"), "<Connections><Node Password=\"sample\" /></Connections>");
                MRemoteNgConnectionReport report = MRemoteNgConnectionFiles.Scan(documents, null);
                Assert.AreEqual(1, report.Findings.Count);
                Assert.AreEqual("config.xml", Path.GetFileName(report.Findings[0].Path));
                Assert.AreEqual(1, report.Findings[0].EncryptedNodeCount.Value);
                Assert.IsFalse(report.LimitReached);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [TestMethod]
        public void UnrelatedEmptyMalformedAndExternalEntityXmlAreIgnored()
        {
            Assert.IsFalse(Inspect("<Connections><Node Password=\"x\" /></Connections>"));
            Assert.IsFalse(Inspect(""));
            Assert.IsFalse(Inspect(Header + Node));
            Assert.IsFalse(Inspect("<!DOCTYPE Connections [<!ENTITY x SYSTEM 'file:///unreadable'>]>" + Header + Node + "</mrng:Connections>"));
            Assert.IsFalse(Inspect(Header + "</mrng:Connections>"));
        }

        [TestMethod]
        public void CustomMasterAndFullFileEncryptionRemainLeadsWithoutPlaintextClaim()
        {
            MRemoteNgConnectionFinding finding;
            // The synthetic marker represents a protected master-key configuration; no key is read.
            Assert.IsTrue(Inspect(Header.Replace("FullFileEncryption=\"false\"", "FullFileEncryption=\"false\" PasswordProtected=\"true\"") + Node + "</mrng:Connections>", out finding));
            Assert.AreEqual(1, finding.EncryptedNodeCount.Value);
            Assert.IsTrue(Inspect("<Connections ConfVersion=\"2.6\" EncryptionEngine=\"AES\" FullFileEncryption=\"true\" />", out finding));
            Assert.IsNull(finding.EncryptedNodeCount);
            Assert.AreEqual("fixture.xml", finding.Path);
        }

        [TestMethod]
        public void ProfileBackupIsIncludedButEmptyDefaultFileIsIgnored()
        {
            string root = Path.Combine(Path.GetTempPath(), "mremote-profile-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "confCons.xml"), "");
                File.WriteAllText(Path.Combine(root, "confCons.xml.backup"), Header + Node + "</mrng:Connections>");
                MRemoteNgConnectionReport report = MRemoteNgConnectionFiles.Scan(null, root);
                Assert.AreEqual(1, report.Findings.Count);
                Assert.AreEqual("confCons.xml.backup", Path.GetFileName(report.Findings[0].Path));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [TestMethod]
        public void FileSizeAndCandidateCapsBoundTheProbe()
        {
            using (var oversized = new MemoryStream(new byte[MRemoteNgConnectionFiles.MaxXmlBytes + 1]))
            {
                MRemoteNgConnectionFinding finding;
                Assert.IsFalse(MRemoteNgConnectionFiles.TryInspectXml(oversized, "fixture.xml", out finding));
            }

            string root = Path.Combine(Path.GetTempPath(), "mremote-cap-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                for (int i = 0; i <= MRemoteNgConnectionFiles.MaxCandidates; i++)
                    File.WriteAllText(Path.Combine(root, i.ToString("D3") + ".xml"), "<unrelated />");
                MRemoteNgConnectionReport report = MRemoteNgConnectionFiles.Scan(root, null);
                Assert.AreEqual(MRemoteNgConnectionFiles.MaxCandidates, report.CandidatesInspected);
                Assert.IsTrue(report.LimitReached);
                Assert.AreEqual(0, report.Findings.Count);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [TestMethod]
        public void DirectoryCapReportsPartialScan()
        {
            string root = Path.Combine(Path.GetTempPath(), "mremote-dirs-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                for (int i = 0; i <= MRemoteNgConnectionFiles.MaxDirectories; i++)
                    Directory.CreateDirectory(Path.Combine(root, i.ToString("D3")));
                MRemoteNgConnectionReport report = MRemoteNgConnectionFiles.Scan(root, null);
                Assert.IsTrue(report.LimitReached);
                Assert.AreEqual(0, report.Findings.Count);
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [TestMethod]
        public void NetworkProfileRootIsSkippedWithPartialVisibility()
        {
            MRemoteNgConnectionReport report = MRemoteNgConnectionFiles.Scan(@"\\server\share\Documents", null);
            Assert.AreEqual(0, report.Findings.Count);
            Assert.IsTrue(report.Partial);
        }

        private static bool Inspect(string xml)
        {
            MRemoteNgConnectionFinding finding;
            return Inspect(xml, out finding);
        }

        private static bool Inspect(string xml, out MRemoteNgConnectionFinding finding)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                return MRemoteNgConnectionFiles.TryInspectXml(stream, "fixture.xml", out finding);
        }
    }
}
