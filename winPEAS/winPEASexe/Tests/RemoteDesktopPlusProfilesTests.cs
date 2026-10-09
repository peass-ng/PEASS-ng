using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class RemoteDesktopPlusProfilesTests
    {
        private const string Export = "<?xml version=\"1.0\"?><Data><Profile><ProfileName>operator</ProfileName>" +
            "<UserName>host</UserName><Password>synthetic-secret-marker</Password><Secure>False</Secure></Profile></Data>";

        [TestMethod]
        public void FindsUtf16ExportInShallowDriveDirectoryWithoutReturningSecret()
        {
            string root = TempRoot();
            try
            {
                string admin = Path.Combine(root, "_admin");
                Directory.CreateDirectory(admin);
                string path = Path.Combine(admin, "profiles.xml");
                File.WriteAllText(path, Export, Encoding.Unicode);
                RemoteDesktopPlusProfileReport report = RemoteDesktopPlusProfiles.Scan(root, null);
                Assert.AreEqual(1, report.Paths.Count);
                Assert.AreEqual(path, report.Paths[0]);
                Assert.IsFalse(report.Paths[0].Contains("synthetic-secret-marker"));
                Assert.IsFalse(report.LimitReached);
            }
            finally { Directory.Delete(root, true); }
        }

        [TestMethod]
        public void FindsCachedUserCandidateAndIgnoresUnrelatedXml()
        {
            string root = TempRoot();
            try
            {
                string profile = Path.Combine(root, "user");
                Directory.CreateDirectory(profile);
                string path = Path.Combine(profile, "profiles.xml");
                File.WriteAllText(path, Export, Encoding.UTF8);
                RemoteDesktopPlusProfileReport report = RemoteDesktopPlusProfiles.Scan(null, new[] { path, path });
                Assert.AreEqual(1, report.Paths.Count);
                Assert.AreEqual(1, report.CandidatesInspected);

                File.WriteAllText(path, "<Connections><Password>secret</Password></Connections>");
                report = RemoteDesktopPlusProfiles.Scan(null, new[] { path });
                Assert.AreEqual(0, report.Paths.Count);
            }
            finally { Directory.Delete(root, true); }
        }

        [TestMethod]
        public void RejectsEmptyMalformedDtdAndOversizedXml()
        {
            Assert.IsFalse(Inspect(Export.Replace("synthetic-secret-marker", "")));
            Assert.IsFalse(Inspect(Export.Replace("<Secure>False</Secure>", "")));
            Assert.IsFalse(Inspect(Export.Substring(0, Export.Length - 7)));
            Assert.IsFalse(Inspect(Export.Replace("<?xml version=\"1.0\"?>",
                "<!DOCTYPE Data [<!ENTITY x SYSTEM 'file:///tmp/key'>]>")));
            using (var stream = new MemoryStream(new byte[RemoteDesktopPlusProfiles.MaxXmlBytes + 1]))
                Assert.IsFalse(RemoteDesktopPlusProfiles.TryInspectXml(stream));
        }

        [TestMethod]
        public void DirectoryAndCandidateCapsReportPartialVisibility()
        {
            string root = TempRoot();
            try
            {
                for (int i = 0; i <= RemoteDesktopPlusProfiles.MaxDirectories; i++)
                    Directory.CreateDirectory(Path.Combine(root, i.ToString("D3")));
                RemoteDesktopPlusProfileReport report = RemoteDesktopPlusProfiles.Scan(root, null);
                Assert.IsTrue(report.LimitReached);

                string user = Path.Combine(root, "user");
                Directory.CreateDirectory(user);
                string[] paths = Enumerable.Range(0, RemoteDesktopPlusProfiles.MaxCandidates + 1)
                    .Select(i => Path.Combine(user, i.ToString("D3"), "profiles.xml")).ToArray();
                foreach (string path in paths)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, "<Data />");
                }
                report = RemoteDesktopPlusProfiles.Scan(null, paths);
                Assert.AreEqual(RemoteDesktopPlusProfiles.MaxCandidates, report.CandidatesInspected);
                Assert.IsTrue(report.LimitReached);
            }
            finally { Directory.Delete(root, true); }
        }

        [TestMethod]
        public void NetworkDriveRootIsSkipped()
        {
            RemoteDesktopPlusProfileReport report = RemoteDesktopPlusProfiles.Scan(@"\\server\share", null);
            Assert.AreEqual(0, report.Paths.Count);
            Assert.IsTrue(report.Partial);
        }

        private static bool Inspect(string xml)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                return RemoteDesktopPlusProfiles.TryInspectXml(stream);
        }

        private static string TempRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "remote-profile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
