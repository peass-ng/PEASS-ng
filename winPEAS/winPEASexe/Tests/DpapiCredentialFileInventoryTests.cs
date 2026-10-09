using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.KnownFileCreds;

namespace winPEAS.Tests
{
    [TestClass]
    public class DpapiCredentialFileInventoryTests
    {
        private static readonly Guid Provider = new Guid("df9d8cd0-1501-11d1-8c7a-00c04fc297eb");
        private static readonly Guid MasterKey = new Guid("12345678-1234-1234-1234-123456789abc");

        [TestMethod]
        public void MalformedFilesRetainContextAndDoNotHideValidFile()
        {
            string root = Path.Combine(Path.GetTempPath(), "winpeas-credfiles-" + Guid.NewGuid().ToString("N"));
            try
            {
                string profile = Path.Combine(root, "first");
                string shortFile = WriteCredential(profile, "00-short", new byte[10]);
                string negativeFile = WriteCredential(profile, "01-negative", Blob(-1));
                string oversizedFile = WriteCredential(profile, "02-oversized", Blob(4098));
                string truncatedFile = WriteCredential(profile, "03-truncated", Blob(20));
                byte[] otherBlob = Blob(0);
                Array.Clear(otherBlob, 16, 16);
                string otherFile = WriteCredential(profile, "04-other", otherBlob);
                byte[] validBlob = Blob(Encoding.Unicode.GetByteCount("saved entry"));
                byte[] description = Encoding.Unicode.GetBytes("saved entry");
                byte[] payload = Encoding.ASCII.GetBytes("PRIVATE_PAYLOAD_SENTINEL");
                Array.Resize(ref validBlob, validBlob.Length + description.Length + payload.Length);
                Array.Copy(description, 0, validBlob, 60, description.Length);
                Array.Copy(payload, 0, validBlob, 60 + description.Length, payload.Length);
                string validFile = WriteCredential(profile, "99-valid", validBlob);

                var entries = KnownFileCredsInfo.GetCredFilesInProfiles(new[] { profile });

                CollectionAssert.AreEquivalent(new[] { shortFile, negativeFile, oversizedFile,
                    truncatedFile, otherFile, validFile }, entries.Select(entry => entry["CredFile"]).ToArray());
                Assert.IsTrue(entries.All(entry => entry["Profile"] == profile && entry["Owner"] == "first"));
                Assert.IsTrue(entries.All(entry => entry.ContainsKey("Size")));
                Assert.IsFalse(entries.Single(entry => entry["CredFile"] == shortFile).ContainsKey("MasterKey"));
                Assert.IsFalse(entries.Single(entry => entry["CredFile"] == otherFile).ContainsKey("MasterKey"));
                Assert.IsTrue(entries.Where(entry => entry["CredFile"] == negativeFile ||
                    entry["CredFile"] == oversizedFile || entry["CredFile"] == truncatedFile)
                    .All(entry => entry["MasterKey"] == MasterKey.ToString() && !entry.ContainsKey("Description")));
                Assert.AreEqual("saved entry", entries.Single(entry => entry["CredFile"] == validFile)["Description"]);
                Assert.IsFalse(entries.SelectMany(entry => entry.Values).Any(value =>
                    value.Contains("PRIVATE_PAYLOAD_SENTINEL")));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void SelectedProfilesKeepDistinctAttribution()
        {
            string root = Path.Combine(Path.GetTempPath(), "winpeas-credfiles-" + Guid.NewGuid().ToString("N"));
            try
            {
                string first = Path.Combine(root, "first");
                string second = Path.Combine(root, "second");
                string firstFile = WriteCredential(first, "first-cred", Blob(0));
                string secondFile = WriteCredential(second, "second-cred", Blob(0), "Roaming");

                var current = KnownFileCredsInfo.GetCredFilesInProfiles(new[] { first });
                var elevated = KnownFileCredsInfo.GetCredFilesInProfiles(new[] { first, second });

                Assert.AreEqual(1, current.Count);
                Assert.AreEqual(firstFile, current[0]["CredFile"]);
                Assert.AreEqual(2, elevated.Count);
                Assert.AreEqual(first, elevated.Single(entry => entry["CredFile"] == firstFile)["Profile"]);
                Assert.AreEqual(second, elevated.Single(entry => entry["CredFile"] == secondFile)["Profile"]);
                Assert.AreEqual("second", elevated.Single(entry => entry["CredFile"] == secondFile)["Owner"]);
                Assert.IsTrue(elevated.All(entry => entry["MasterKey"] == MasterKey.ToString()));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static byte[] Blob(int descriptionLength)
        {
            var bytes = new byte[60];
            Array.Copy(BitConverter.GetBytes(1), 0, bytes, 12, 4);
            Array.Copy(Provider.ToByteArray(), 0, bytes, 16, 16);
            Array.Copy(MasterKey.ToByteArray(), 0, bytes, 36, 16);
            Array.Copy(BitConverter.GetBytes(descriptionLength), 0, bytes, 56, 4);
            return bytes;
        }

        private static string WriteCredential(string profile, string name, byte[] bytes, string location = "Local")
        {
            string directory = Path.Combine(profile, "AppData", location, "Microsoft", "Credentials");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }
    }
}
