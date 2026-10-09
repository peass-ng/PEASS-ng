using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class IisWebrootBackupSelectorTests
    {
        [TestMethod]
        public void ListsOnlyImmediateDefaultWebrootBackupArchives()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "IIS default webroot backup archive candidates");
            var file = entry.value.files.Single();
            Assert.IsTrue(entry.value.config.auto_check);
            CollectionAssert.Contains(entry.value.disable, "linpeas");
            Assert.AreEqual("*backup*.zip", file.name);
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));
            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\inetpub\wwwroot\site-backup-old.zip", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"D:\inetpub\wwwroot\BACKUP.ZIP", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\Public\site-backup-old.zip", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\inetpub\wwwroot\old\site-backup-old.zip", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\inetpub\wwwroot\site-backup-old.zip.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\inetpub\wwwroot\site.zip", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\inetpub\wwwroot\site-backup-old.zip", null));
        }
    }
}
