using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.KnownFileCreds;

namespace winPEAS.Tests
{
    [TestClass]
    public class DpapiMasterKeyProfileTests
    {
        [TestMethod]
        public void EnumeratesEachProfileAndBothProtectLocationsOnce()
        {
            string root = Path.Combine(Path.GetTempPath(), "winpeas-dpapi-" + Guid.NewGuid().ToString("N"));
            try
            {
                string first = Path.Combine(root, "first");
                string second = Path.Combine(root, "second");
                string guid = "12345678-1234-1234-1234-123456789abc";
                string firstKey = MakeKey(first, "Roaming", "S-1-5-21-1", guid);
                string secondKey = MakeKey(second, "Local", "S-1-5-21-2", guid);
                MakeKey(second, "Roaming", "S-1-5-21-2", guid + ".bak");

                var entries = KnownFileCredsInfo.ListMasterKeysInProfiles(new[] { first, second });

                Assert.AreEqual(2, entries.Count);
                CollectionAssert.AreEquivalent(new[] { firstKey, secondKey },
                    entries.Select(entry => entry["MasterKey"]).ToArray());
                Assert.IsTrue(entries.All(entry => entry.Keys.OrderBy(key => key)
                    .SequenceEqual(new[] { "Accessed", "MasterKey", "Modified" })));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [TestMethod]
        public void MissingProfileAndUnrelatedFilenameProduceNoKeys()
        {
            string root = Path.Combine(Path.GetTempPath(), "winpeas-dpapi-" + Guid.NewGuid().ToString("N"));
            try
            {
                MakeKey(root, "Roaming", "S-1-5-21-1", "ordinary.txt");
                Assert.AreEqual(0, KnownFileCredsInfo.ListMasterKeysInProfiles(
                    new[] { null, Path.Combine(root, "missing"), root }).Count);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static string MakeKey(string profile, string location, string sid, string name)
        {
            string folder = Path.Combine(profile, "AppData", location, "Microsoft", "Protect", sid);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, name);
            File.WriteAllBytes(path, new byte[] { 1 });
            return path;
        }
    }
}
