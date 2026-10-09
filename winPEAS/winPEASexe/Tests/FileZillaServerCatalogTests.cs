using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class FileZillaServerCatalogTests
    {
        [TestMethod]
        public void LegacyServerConfigurationIsListedOnlyAtConventionalInstallPaths()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "FileZilla Server legacy configuration candidates");
            var file = entry.value.files.Single();
            Assert.IsTrue(entry.value.config.auto_check);
            CollectionAssert.Contains(entry.value.disable, "linpeas");
            Assert.AreEqual("FileZilla Server.xml", file.name);
            Assert.AreEqual("f", file.value.type);
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));

            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\FileZilla Server\FileZilla Server.xml", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"D:\Program Files (x86)\FileZilla Server\FileZilla Server.xml", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\Public\FileZilla Server.xml", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\FileZilla Server\backup\FileZilla Server.xml", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\FileZilla Server\FileZilla Server.xml.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\FileZilla Client\filezilla.xml", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"\\server\share\Program Files\FileZilla Server\FileZilla Server.xml", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\FileZilla Server\FileZilla Server.xml", null));
        }
    }
}
