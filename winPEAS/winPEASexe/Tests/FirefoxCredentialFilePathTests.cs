using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.KnownFileCreds.Browsers.Firefox;

namespace winPEAS.Tests
{
    [TestClass]
    public class FirefoxCredentialFilePathTests
    {
        [TestMethod]
        public void ModernProfileReportsExactKeyAndLoginPathsWithoutReadingContents()
        {
            string profile = Path.Combine(Path.GetTempPath(), "firefox-profile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(profile);
            try
            {
                string key = Path.Combine(profile, "key4.db");
                string logins = Path.Combine(profile, "logins.json");
                File.WriteAllText(key, "SECRET_FIXTURE_DO_NOT_PRINT");
                File.WriteAllText(logins, "SECRET_FIXTURE_DO_NOT_PRINT");
                File.WriteAllText(Path.Combine(profile, "unrelated.json"), "ignored");

                var paths = Firefox.GetFirefoxCredentialFilePaths(profile);

                CollectionAssert.AreEqual(new[] { key, logins }, paths);
                Assert.IsFalse(string.Join(" ", paths).Contains("SECRET_FIXTURE_DO_NOT_PRINT"));
            }
            finally { Directory.Delete(profile, true); }
        }

        [TestMethod]
        public void MissingFilesAndDirectoriesDoNotBecomeCredentialCandidates()
        {
            string profile = Path.Combine(Path.GetTempPath(), "firefox-profile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(profile);
            try
            {
                Directory.CreateDirectory(Path.Combine(profile, "key4.db"));
                File.WriteAllText(Path.Combine(profile, "logins-backup.json"), "ignored");
                Assert.AreEqual(0, Firefox.GetFirefoxCredentialFilePaths(profile).Count);
                string legacyKey = Path.Combine(profile, "key3.db");
                File.WriteAllText(legacyKey, "fixture");
                CollectionAssert.AreEqual(new[] { legacyKey }, Firefox.GetFirefoxCredentialFilePaths(profile));
            }
            finally { Directory.Delete(profile, true); }
        }
    }
}
