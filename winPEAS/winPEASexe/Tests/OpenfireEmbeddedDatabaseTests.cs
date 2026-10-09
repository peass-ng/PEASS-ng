using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class OpenfireEmbeddedDatabaseTests
    {
        [TestMethod]
        public void EmbeddedDatabaseCandidateRequiresOpenfireDataDirectory()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "Openfire local configuration and database");
            var file = entry.value.files.Single(item => item.name == "openfire.script");
            Assert.IsTrue(entry.value.config.auto_check);
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));
            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\Openfire\embedded-db\openfire.script", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"D:\Apps\Openfire\embedded-db\openfire.script", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\A\Downloads\openfire.script", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\Openfire\embedded-db\openfire.script.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\Openfire\embedded-db\openfire.script", null));
        }

        [TestMethod]
        public void AdminConfigurationCandidateRequiresOpenfireConfDirectory()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "Openfire local configuration and database");
            var file = entry.value.files.Single(item => item.name == "openfire.xml");
            Assert.IsTrue(entry.value.config.auto_check);
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));
            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\Openfire\conf\openfire.xml", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"D:\Apps\Openfire\conf\openfire.xml", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\A\Downloads\openfire.xml", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\Openfire\conf\openfire.xml.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Program Files\Openfire\conf\openfire.xml", null));
        }
    }
}
