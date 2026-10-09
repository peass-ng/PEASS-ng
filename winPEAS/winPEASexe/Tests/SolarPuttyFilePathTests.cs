using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class SolarPuttyFilePathTests
    {
        private static string PatternFor(string filename)
        {
            var config = YamlConfigHelper.GetWindowsSearchConfig();
            var record = config.search.Single(item => item.name == "Solar-PuTTY session stores");
            var file = record.value.files.Single(item => item.name == filename);
            Assert.IsTrue(record.value.config.auto_check);
            Assert.IsTrue(file.value.just_list_file == true);
            return file.value.check_extra_path;
        }

        [TestMethod]
        public void SessionExportRequiresAppFolderAndExactFilename()
        {
            string pattern = PatternFor("sessions-backup.dat");
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Solar-PuTTY session stores",
                @"C:\Users\A\Documents\Solar-PuTTY\sessions-backup.dat", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Solar-PuTTY session stores",
                @"C:\Users\A\Documents\sessions-backup.dat", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Solar-PuTTY session stores",
                @"C:\Users\A\Documents\Solar-PuTTY\sessions-backup.dat.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Solar-PuTTY session stores",
                @"C:\Users\A\Documents\Solar-PuTTY\sessions-backup.dat", null));
        }

        [TestMethod]
        public void NativeStoreRequiresAppDataPath()
        {
            string pattern = PatternFor("data.dat");
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Solar-PuTTY session stores",
                @"C:\Users\A\AppData\Roaming\SolarWinds\FreeTools\Solar-PuTTY\data.dat", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Solar-PuTTY session stores",
                @"C:\Users\A\Documents\data.dat", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Solar-PuTTY session stores",
                @"C:\Users\A\AppData\Roaming\SolarWinds\FreeTools\Other\data.dat", pattern));
        }

        [TestMethod]
        public void ExistingSelectorsRetainTheirBehavior()
        {
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Splunk",
                @"C:\Program Files\Splunk\etc\system\local\server.conf", "Splunk"));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Splunk",
                @"C:\Users\A\server.conf", "Splunk"));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Other",
                @"C:\Users\A\server.conf", "never-match"));
        }
    }
}
