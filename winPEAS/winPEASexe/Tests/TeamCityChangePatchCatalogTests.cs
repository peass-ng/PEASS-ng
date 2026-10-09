using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class TeamCityChangePatchCatalogTests
    {
        [TestMethod]
        public void CatalogListsOnlyConventionalChangePatchPaths()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "TeamCity change patch candidates");
            var file = entry.value.files.Single();
            Assert.IsTrue(entry.value.config.auto_check);
            CollectionAssert.Contains(entry.value.disable, "linpeas");
            Assert.AreEqual("*.changes.diff", file.name);
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));
            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\ProgramData\JetBrains\TeamCity\system\changes\101.changes.diff", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"D:\Users\Service Account\.BuildServer\system\changes\202.changes.diff", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\Users\Public\Downloads\101.changes.diff", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\ProgramData\JetBrains\TeamCity\system\changes\older\101.changes.diff", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\ProgramData\JetBrains\TeamCity\system\changes\101.changes.diff.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(entry.name,
                @"C:\ProgramData\JetBrains\TeamCity\system\changes\101.changes.diff", null));
        }

        [TestMethod]
        public void ProbeRejectsMissingDirectoryAndReparseAttributes()
        {
            Assert.IsFalse(FileAnalysis.IsPlainDirectoryAttributes(
                FileAttributes.Directory | FileAttributes.ReparsePoint));
            Assert.IsFalse(FileAnalysis.IsPlainDirectoryAttributes(FileAttributes.Normal));
            Assert.IsTrue(FileAnalysis.IsPlainDirectoryAttributes(FileAttributes.Directory));

            bool partial;
            var absent = FileAnalysis.ProbeTeamCityChanges(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), out partial);
            Assert.AreEqual(0, absent.Count);
            Assert.IsFalse(partial);
        }

        [TestMethod]
        public void ProbeUsesOnlyTopLevelNamesAndCapsFileCount()
        {
            string common = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            string changes = Path.Combine(common, "JetBrains", "TeamCity", "system", "changes");
            Directory.CreateDirectory(changes);
            try
            {
                for (int index = 0; index < FileAnalysis.MaxTeamCityChangeFiles + 1; index++)
                    File.WriteAllText(Path.Combine(changes, index + ".changes.diff"),
                        "SENSITIVE_FIXTURE_VALUE_DO_NOT_PRINT");
                File.WriteAllText(Path.Combine(changes, "unrelated.txt"), "irrelevant");
                string nested = Path.Combine(changes, "older");
                Directory.CreateDirectory(nested);
                File.WriteAllText(Path.Combine(nested, "nested.changes.diff"), "irrelevant");

                bool partial;
                var files = FileAnalysis.ProbeTeamCityChanges(common, out partial);
                Assert.AreEqual(FileAnalysis.MaxTeamCityChangeFiles, files.Count);
                Assert.IsTrue(partial);
                Assert.IsTrue(files.All(file => file.FullPath.EndsWith(".changes.diff",
                    StringComparison.OrdinalIgnoreCase)));
                Assert.IsFalse(files.Any(file => file.FullPath.Contains("older")));
                Assert.IsFalse(files.Any(file => file.FullPath.Contains("SENSITIVE_FIXTURE_VALUE_DO_NOT_PRINT")));
            }
            finally { Directory.Delete(common, true); }
        }

        [TestMethod]
        public void ProbeCapsVisitedEntriesEvenWhenNamesDoNotMatch()
        {
            string common = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            string changes = Path.Combine(common, "JetBrains", "TeamCity", "system", "changes");
            Directory.CreateDirectory(changes);
            try
            {
                for (int index = 0; index < FileAnalysis.MaxTeamCityChangeEntries + 1; index++)
                    File.WriteAllText(Path.Combine(changes, index + ".txt"), "irrelevant");
                bool partial;
                var files = FileAnalysis.ProbeTeamCityChanges(common, out partial);
                Assert.AreEqual(0, files.Count);
                Assert.IsTrue(partial);
            }
            finally { Directory.Delete(common, true); }
        }
    }
}
