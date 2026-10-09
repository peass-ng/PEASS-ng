using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Helpers;
using winPEAS.Helpers.Search;
using winPEAS.Info.SystemInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class BitLockerDataAndRecoveryArtifactTests
    {
        [TestMethod]
        public void DataVolumeStatusDoesNotTreatUnknownAsUnprotected()
        {
            var locked = DmaProtection.AssessDataVolume("E:", 2, 1);
            Assert.AreEqual("unknown", locked.Protection);
            Assert.AreEqual("locked", locked.Lock);

            var open = DmaProtection.AssessDataVolume("D:", 1, 0);
            Assert.AreEqual("on", open.Protection);
            Assert.AreEqual("unlocked", open.Lock);

            var unavailable = DmaProtection.AssessDataVolume(null, null, null);
            Assert.AreEqual("(unmounted)", unavailable.DriveLetter);
            Assert.AreEqual("unknown", unavailable.Protection);
            Assert.AreEqual("unknown", unavailable.Lock);
        }

        [TestMethod]
        public void OnlyNamedRecoveryArchivesBypassStaticArchiveSkip()
        {
            Assert.IsTrue(SearchHelper.ShouldRetainInventoryFile("BitLocker-backup.7z", ".7z"));
            Assert.IsTrue(SearchHelper.ShouldRetainInventoryFile("Backup_Credentials.7z", ".7z"));
            Assert.IsTrue(SearchHelper.ShouldRetainInventoryFile("recovery.zip", ".zip"));
            Assert.IsFalse(SearchHelper.ShouldRetainInventoryFile("photos.7z", ".7z"));
            Assert.IsFalse(SearchHelper.ShouldRetainInventoryFile("random.zip", ".zip"));
            Assert.IsFalse(SearchHelper.ShouldRetainInventoryFile("backup.png", ".png"));
        }

        [TestMethod]
        public void AppArtifactsAndRecoveryExportsArePathScoped()
        {
            Assert.AreEqual("messaging client database", SearchHelper.ClassifyMessengerOrRecoveryFile(
                File(@"C:\Users\A\AppData\Roaming\Output Messenger\JAAA\OM.db3")));
            Assert.AreEqual("messaging client received file", SearchHelper.ClassifyMessengerOrRecoveryFile(
                File(@"C:\Users\A\AppData\Roaming\Output Messenger\JAAA\Received Files\203301\capture.pcapng")));
            Assert.AreEqual("BitLocker recovery export candidate", SearchHelper.ClassifyMessengerOrRecoveryFile(
                File(@"C:\Users\A\Documents\Microsoft account - BitLocker.html")));
            Assert.AreEqual("backup/recovery archive candidate", SearchHelper.ClassifyMessengerOrRecoveryFile(
                File(@"C:\Users\A\Documents\Backup_Credentials.7z")));
            Assert.IsNull(SearchHelper.ClassifyMessengerOrRecoveryFile(File(@"C:\Users\A\Documents\OM.db3")));
            Assert.IsNull(SearchHelper.ClassifyMessengerOrRecoveryFile(File(@"C:\Users\A\Documents\capture.pcapng")));
            Assert.IsNull(SearchHelper.ClassifyMessengerOrRecoveryFile(File(@"C:\Users\A\Documents\photos.7z")));
        }

        [TestMethod]
        public void CachedArtifactOutputIsDeduplicatedAndCapped()
        {
            var files = new List<CustomFileInfo>();
            for (int index = 0; index < 25; index++)
                files.Add(File(@"C:\Users\A\AppData\Roaming\Output Messenger\JAAA\Received Files\"
                    + index + ".pcapng"));

            List<string> findings = SearchHelper.SearchMessengerAndRecoveryArtifacts(files,
                new[] { files[0] });
            Assert.AreEqual(SearchHelper.MaxMessengerAndRecoveryArtifacts + 1, findings.Count);
            StringAssert.Contains(findings[0], "contents not inspected");
            Assert.AreEqual("[additional matching files omitted]", findings[findings.Count - 1]);
        }

        private static CustomFileInfo File(string path)
        {
            int lastSlash = path.LastIndexOf('\\');
            string name = path.Substring(lastSlash + 1);
            int dot = name.LastIndexOf('.');
            string extension = dot >= 0 ? name.Substring(dot) : string.Empty;
            return new CustomFileInfo(name, extension, path, 12, false);
        }
    }
}
