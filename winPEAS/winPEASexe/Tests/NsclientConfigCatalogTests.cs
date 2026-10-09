using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class NsclientConfigCatalogTests
    {
        [TestMethod]
        public void CatalogListsOnlyConventionalConfigPaths()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "NSClient configuration candidates");
            var file = entry.value.files.Single();
            Assert.IsTrue(entry.value.config.auto_check);
            CollectionAssert.Contains(entry.value.disable, "linpeas");
            Assert.AreEqual("nsclient.ini", file.name);
            Assert.AreEqual("f", file.value.type);
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));

            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\NSClient++\nsclient.ini", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files (x86)\NSClient++\NSCLIENT.INI", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\Public\nsclient.ini", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\OtherAgent\nsclient.ini", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\NSClient++\backup\nsclient.ini", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\NSClient++\nsclient.ini.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"\\server\share\Program Files\NSClient++\nsclient.ini", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\NSClient++\nsclient.ini", null));
        }
    }
}
