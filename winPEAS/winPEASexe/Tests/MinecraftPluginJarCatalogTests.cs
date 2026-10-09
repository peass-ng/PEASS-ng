using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class MinecraftPluginJarCatalogTests
    {
        [TestMethod]
        public void PluginCandidateOnlyListsArchivesInServerPluginDirectories()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "Minecraft plugin JAR candidates");
            var file = entry.value.files.Single();
            Assert.IsTrue(entry.value.config.auto_check);
            CollectionAssert.Contains(entry.value.disable, "linpeas");
            Assert.AreEqual("*.jar", file.name);
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));
            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\serviceacct\server\plugins\custom.jar", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"D:\minecraft-server\plugins\custom.jar", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Games\paper\plugins\custom.JAR", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\serviceacct\Downloads\custom.jar", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\server\plugins\custom.jar.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\server\plugins\subdir\custom.jar", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\server\plugins\custom.jar", null));
        }
    }
}
