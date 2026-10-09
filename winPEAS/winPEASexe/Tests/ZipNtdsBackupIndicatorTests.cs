using System;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class ZipNtdsBackupIndicatorTests
    {
        [TestMethod]
        public void NestedMixedCasePairMatchesExactBasenames()
        {
            byte[] zip = MakeZip("snapshot/NTDS.DiT", @"hives\system", "snapshot/ntds.dit.txt");
            string ntds, system;
            Assert.AreEqual(ZipNtdsBackupStatus.Match, Inspect(zip, out ntds, out system));
            Assert.AreEqual("snapshot/NTDS.DiT", ntds);
            Assert.AreEqual(@"hives\system", system);
        }

        [TestMethod]
        public void PartialAndMisleadingNamesDoNotMatch()
        {
            string ntds, system;
            Assert.AreEqual(ZipNtdsBackupStatus.NoMatch,
                Inspect(MakeZip("ntds.dit.txt", "SYSTEM"), out ntds, out system));
            Assert.AreEqual(ZipNtdsBackupStatus.NoMatch,
                Inspect(MakeZip("ntds.dit", "SYSTEM.old"), out ntds, out system));
            Assert.AreEqual(ZipNtdsBackupStatus.NoMatch,
                Inspect(MakeZip("ordinary.txt"), out ntds, out system));
        }

        [TestMethod]
        public void CorruptTruncatedAndEncryptedArchivesAreUnknown()
        {
            string ntds, system;
            byte[] zip = MakeZip("ntds.dit", "SYSTEM");
            Assert.AreEqual(ZipNtdsBackupStatus.Unknown,
                Inspect(new byte[] { 1, 2, 3 }, out ntds, out system));
            var truncated = new byte[zip.Length - 3];
            Array.Copy(zip, truncated, truncated.Length);
            Assert.AreEqual(ZipNtdsBackupStatus.Unknown,
                Inspect(truncated, out ntds, out system));
            int central = FindSignature(zip, 0x02014b50);
            Assert.IsTrue(central >= 0);
            zip[central + 8] |= 1; // General purpose flag: encrypted member.
            Assert.AreEqual(ZipNtdsBackupStatus.Unknown, Inspect(zip, out ntds, out system));
        }

        [TestMethod]
        public void CommentedArchiveIsUnknownWithoutSearchingThroughPayload()
        {
            byte[] zip = MakeZip("ntds.dit", "SYSTEM");
            zip[zip.Length - 2] = 1; // EOCD comment length.
            Array.Resize(ref zip, zip.Length + 1);
            zip[zip.Length - 1] = 65;
            string ntds, system;
            Assert.AreEqual(ZipNtdsBackupStatus.Unknown, Inspect(zip, out ntds, out system));
        }

        [TestMethod]
        public void ParserReadsNoLocalHeadersOrMemberPayload()
        {
            byte[] zip = MakeZip("ntds.dit", "SYSTEM");
            int centralOffset = BitConverter.ToInt32(zip, zip.Length - 22 + 16);
            using (var stream = new MetadataOnlyStream(zip, centralOffset))
            {
                string ntds, system;
                Assert.AreEqual(ZipNtdsBackupStatus.Match,
                    ZipNtdsBackupIndicator.InspectMetadata(stream, () => false, out ntds, out system));
            }
        }

        [TestMethod]
        public void ArchiveMemberAndElapsedLimitsAreUnknown()
        {
            byte[] zip = MakeZip("ntds.dit", "SYSTEM");
            string ntds, system;
            using (var stream = new MemoryStream(zip))
                Assert.AreEqual(ZipNtdsBackupStatus.Unknown,
                    ZipNtdsBackupIndicator.InspectMetadata(stream, () => false,
                        out ntds, out system, maxArchiveBytes: zip.Length - 1));
            using (var stream = new MemoryStream(zip))
                Assert.AreEqual(ZipNtdsBackupStatus.Unknown,
                    ZipNtdsBackupIndicator.InspectMetadata(stream, () => false,
                        out ntds, out system, maxMembers: 1));
            using (var stream = new MemoryStream(zip))
                Assert.AreEqual(ZipNtdsBackupStatus.Unknown,
                    ZipNtdsBackupIndicator.InspectMetadata(stream, () => true,
                        out ntds, out system));
        }

        private static ZipNtdsBackupStatus Inspect(byte[] bytes, out string ntds, out string system)
        {
            using (var stream = new MemoryStream(bytes))
                return ZipNtdsBackupIndicator.InspectMetadata(stream, () => false, out ntds, out system);
        }

        private static byte[] MakeZip(params string[] names)
        {
            using (var stream = new MemoryStream())
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
                    foreach (string name in names) zip.CreateEntry(name);
                return stream.ToArray();
            }
        }

        private static int FindSignature(byte[] bytes, uint signature)
        {
            for (int i = 0; i <= bytes.Length - 4; i++)
                if ((uint)(bytes[i] | bytes[i + 1] << 8 | bytes[i + 2] << 16 | bytes[i + 3] << 24) == signature)
                    return i;
            return -1;
        }

        private sealed class MetadataOnlyStream : MemoryStream
        {
            private readonly int centralOffset;

            internal MetadataOnlyStream(byte[] bytes, int centralOffset) : base(bytes)
            {
                this.centralOffset = centralOffset;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (Position < centralOffset) throw new InvalidOperationException("Local ZIP data was read");
                return base.Read(buffer, offset, count);
            }
        }
    }
}
