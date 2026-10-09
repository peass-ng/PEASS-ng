using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class RecycleBinArchiveIndicatorTests
    {
        [TestMethod]
        public void OldVersionTwoBackupArchiveIsIncludedFromMetadata()
        {
            DateTime deletion = DateTime.UtcNow.AddDays(-134);
            RecycleBinArchiveCandidate candidate;
            Assert.IsTrue(RecycleBinArchiveIndicator.TryParse(
                Metadata(2, @"C:\backups\wapt-backup-sunday.7z", deletion),
                @"C:\$Recycle.Bin\S-1-5-21\$R123456", out candidate));
            Assert.IsNotNull(candidate);
            Assert.AreEqual("wapt-backup-sunday.7z", candidate.OriginalName);
            Assert.AreEqual(deletion, candidate.DeletedUtc);
            Assert.AreEqual("unknown", candidate.Accessibility);
            Assert.AreEqual(31457280L, candidate.Size);
            Assert.IsTrue(candidate.LikelyBackup);
        }

        [TestMethod]
        public void VersionOneAndMixedCaseArchivesAreCandidates()
        {
            RecycleBinArchiveCandidate candidate;
            Assert.IsTrue(RecycleBinArchiveIndicator.TryParse(
                Metadata(1, @"C:\old\settings.ZIP", DateTime.UtcNow), "paired", out candidate));
            Assert.AreEqual("settings.ZIP", candidate.OriginalName);
            Assert.IsTrue(RecycleBinArchiveIndicator.TryParse(
                Metadata(2, @"C:\old\snapshot.RaR", DateTime.UtcNow), "paired", out candidate));
            Assert.AreEqual("snapshot.RaR", candidate.OriginalName);
            Assert.IsTrue(RecycleBinArchiveIndicator.TryParse(
                Metadata(2, @"C:\old\ordinary.txt", DateTime.UtcNow), "paired", out candidate));
            Assert.IsNull(candidate);
        }

        [TestMethod]
        public void TruncatedInvalidAndUnsafeMetadataAreRejected()
        {
            RecycleBinArchiveCandidate candidate;
            Assert.IsFalse(RecycleBinArchiveIndicator.TryParse(new byte[10], "paired", out candidate));
            byte[] data = Metadata(2, @"C:\old\backup.7z", DateTime.UtcNow);
            Array.Resize(ref data, data.Length - 2);
            Assert.IsFalse(RecycleBinArchiveIndicator.TryParse(data, "paired", out candidate));
            data = Metadata(2, @"C:\old\backup.7z", DateTime.UtcNow);
            Array.Copy(BitConverter.GetBytes(long.MinValue), 0, data, 16, 8);
            Assert.IsFalse(RecycleBinArchiveIndicator.TryParse(data, "paired", out candidate));
            data = Metadata(2, "C:\\old\\bad\nbackup.7z", DateTime.UtcNow);
            Assert.IsFalse(RecycleBinArchiveIndicator.TryParse(data, "paired", out candidate));
        }

        [TestMethod]
        public void MissingPairedFileIsReportedWithoutReadingArchiveBytes()
        {
            RecycleBinArchiveCandidate candidate;
            Assert.IsTrue(RecycleBinArchiveIndicator.TryParse(
                Metadata(2, @"C:\old\backup.7z", DateTime.UtcNow),
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")), out candidate));
            Assert.IsFalse(RecycleBinArchiveIndicator.CheckPairedFile(candidate));
            Assert.AreEqual("missing", candidate.Accessibility);
            Assert.AreEqual(31457280L, candidate.Size);
        }

        [TestMethod]
        public void CandidateCapReportsPartialVisibility()
        {
            var result = new RecycleBinArchiveResult();
            for (int i = 0; i < RecycleBinArchiveIndicator.MaxCandidates; i++)
                Assert.IsTrue(RecycleBinArchiveIndicator.TryAddCandidate(result, new RecycleBinArchiveCandidate()));
            Assert.IsFalse(result.Partial);
            Assert.IsFalse(RecycleBinArchiveIndicator.TryAddCandidate(result, new RecycleBinArchiveCandidate()));
            Assert.IsTrue(result.Partial);
            Assert.AreEqual(RecycleBinArchiveIndicator.MaxCandidates, result.Candidates.Count);
        }

        private static byte[] Metadata(long version, string path, DateTime deletedUtc)
        {
            byte[] name = Encoding.Unicode.GetBytes(path + "\0");
            byte[] data = new byte[version == 1 ? 544 : 28 + name.Length];
            Array.Copy(BitConverter.GetBytes(version), 0, data, 0, 8);
            Array.Copy(BitConverter.GetBytes(31457280L), 0, data, 8, 8);
            Array.Copy(BitConverter.GetBytes(deletedUtc.ToFileTimeUtc()), 0, data, 16, 8);
            if (version == 2) Array.Copy(BitConverter.GetBytes((uint)(name.Length / 2)), 0, data, 24, 4);
            Array.Copy(name, 0, data, version == 1 ? 24 : 28, name.Length);
            return data;
        }
    }
}
