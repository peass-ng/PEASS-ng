using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class LansweeperConfigMetadataTests
    {
        private sealed class Fixture
        {
            internal readonly string Install = Path.Combine(Path.GetTempPath(), "Program Files", "Lansweeper");
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
            internal readonly List<string> Opened = new List<string>();
            internal string Config => Path.Combine(Install, "Website", "web.config");
            internal string Key => Path.Combine(Install, "Key", "Encryption.txt");
            internal string DeniedPath;

            internal Fixture()
            {
                Files[Config] = Encoding.UTF8.GetBytes("<configuration><connectionStrings configProtectionProvider=\"DataProtectionConfigurationProvider\"><EncryptedData>private-ciphertext</EncryptedData></connectionStrings></configuration>");
                Files[Key] = Encoding.UTF8.GetBytes("private-key");
            }

            internal FileAttributes Attributes(string path)
            {
                if (path == Install || path == Path.GetDirectoryName(Config) || path == Path.GetDirectoryName(Key))
                    return FileAttributes.Directory;
                if (Files.ContainsKey(path)) return FileAttributes.Normal;
                throw new FileNotFoundException();
            }

            internal Stream Open(string path)
            {
                Opened.Add(path);
                if (path == DeniedPath) throw new UnauthorizedAccessException();
                return new MemoryStream(Files[path], false);
            }

            internal LansweeperConfigResult Probe() =>
                LansweeperConfigMetadata.ProbeInstallCore(Install, Attributes, Open);
        }

        [TestMethod]
        public void AbsentInstallationDoesNotOpenFiles()
        {
            var fixture = new Fixture();
            Assert.IsNull(LansweeperConfigMetadata.ProbeInstallCore(
                Path.Combine(Path.GetTempPath(), "Program Files", "Other"), fixture.Attributes, fixture.Open));
            Assert.AreEqual(0, fixture.Opened.Count);
        }

        [TestMethod]
        public void ReadableProtectedPairIsOnlyAConditionalCandidate()
        {
            var fixture = new Fixture();
            LansweeperConfigResult result = fixture.Probe();
            Assert.AreEqual(ConfigFileState.Readable, result.ConfigState);
            Assert.AreEqual(ConfigFileState.Readable, result.KeyState);
            Assert.IsTrue(result.ProtectedConnectionStrings);
            Assert.IsTrue(result.Candidate);
            CollectionAssert.AreEquivalent(new[] { fixture.Config, fixture.Key }, fixture.Opened);
            Assert.IsFalse(string.Join("|", typeof(LansweeperConfigResult).GetFields(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Select(field => Convert.ToString(field.GetValue(result)))).Contains("private-"));
        }

        [TestMethod]
        public void UnprotectedAndMissingKeyAreNotCandidates()
        {
            var fixture = new Fixture();
            fixture.Files[fixture.Config] = Encoding.UTF8.GetBytes("<configuration><connectionStrings><add name=\"db\" connectionString=\"private-password\" /></connectionStrings></configuration>");
            fixture.Files.Remove(fixture.Key);
            LansweeperConfigResult result = fixture.Probe();
            Assert.AreEqual(ConfigFileState.Readable, result.ConfigState);
            Assert.AreEqual(ConfigFileState.Absent, result.KeyState);
            Assert.IsFalse(result.ProtectedConnectionStrings);
            Assert.IsFalse(result.Candidate);
            CollectionAssert.AreEqual(new[] { fixture.Config }, fixture.Opened);
        }

        [TestMethod]
        public void DeniedAndMalformedFilesRemainMetadataOnly()
        {
            var denied = new Fixture();
            denied.DeniedPath = denied.Config;
            LansweeperConfigResult deniedResult = denied.Probe();
            Assert.AreEqual(ConfigFileState.Denied, deniedResult.ConfigState);
            Assert.AreEqual(ConfigFileState.Readable, deniedResult.KeyState);
            Assert.IsFalse(deniedResult.Candidate);

            var deniedKey = new Fixture();
            deniedKey.DeniedPath = deniedKey.Key;
            LansweeperConfigResult deniedKeyResult = deniedKey.Probe();
            Assert.AreEqual(ConfigFileState.Readable, deniedKeyResult.ConfigState);
            Assert.AreEqual(ConfigFileState.Denied, deniedKeyResult.KeyState);
            Assert.IsFalse(deniedKeyResult.Candidate);

            var malformed = new Fixture();
            malformed.Files[malformed.Config] = Encoding.UTF8.GetBytes("<configuration><connectionStrings configProtectionProvider=\"provider\">private-ciphertext");
            LansweeperConfigResult malformedResult = malformed.Probe();
            Assert.AreEqual(ConfigFileState.Malformed, malformedResult.ConfigState);
            Assert.IsFalse(malformedResult.ProtectedConnectionStrings);
            Assert.IsFalse(malformedResult.Candidate);
        }

        [TestMethod]
        public void OversizeAndDtdConfigurationAreNotParsed()
        {
            var oversized = new Fixture();
            oversized.Files[oversized.Config] = new byte[LansweeperConfigMetadata.MaxConfigBytes + 1];
            LansweeperConfigResult largeResult = oversized.Probe();
            Assert.AreEqual(ConfigFileState.TooLarge, largeResult.ConfigState);
            Assert.IsFalse(largeResult.Candidate);

            var dtd = new Fixture();
            dtd.Files[dtd.Config] = Encoding.UTF8.GetBytes("<!DOCTYPE configuration [<!ENTITY secret 'private'>]><configuration><connectionStrings>&secret;</connectionStrings></configuration>");
            LansweeperConfigResult dtdResult = dtd.Probe();
            Assert.AreEqual(ConfigFileState.Malformed, dtdResult.ConfigState);
            Assert.IsFalse(dtdResult.Candidate);
        }

        [TestMethod]
        public void NetworkRootsAreRejectedBeforeAnyProbe()
        {
            Assert.AreEqual(0, LansweeperConfigMetadata.CandidateInstallPaths(
                @"\\server\share", "//server/share").Count());
        }
    }
}
