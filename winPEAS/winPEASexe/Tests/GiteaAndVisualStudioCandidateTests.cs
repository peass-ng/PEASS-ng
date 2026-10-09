using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Checks;
using winPEAS.Helpers.YamlConfig;
using winPEAS.Info.ServicesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class GiteaAndVisualStudioCandidateTests
    {
        private const string CollectorImage =
            @"C:\Program Files (x86)\Microsoft Visual Studio\Shared\Common\DiagnosticsHub.Collection.Service\StandardCollector.Service.exe";

        [TestMethod]
        public void GiteaDatabaseIsListedOnlyUnderItsApplicationDataFolder()
        {
            var record = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "Gitea database");
            var file = record.value.files.Single(item => item.name == "gitea.db");
            Assert.IsTrue(record.value.config.auto_check);
            Assert.IsTrue(record.value.disable.Contains("linpeas"));
            Assert.IsTrue(file.value.just_list_file == true);
            Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));

            string pattern = file.value.check_extra_path;
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Gitea database",
                @"C:\Program Files\Gitea\data\gitea.db", pattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Gitea database",
                @"D:\Services\Gitea\data\gitea.db", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Gitea database",
                @"C:\Users\A\Downloads\gitea.db", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Gitea database",
                @"C:\Program Files\Gitea\data\gitea.db.bak", pattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Gitea database",
                @"C:\Program Files\Gitea\data\gitea.db", null));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Other",
                @"C:\Users\A\gitea.db", "never-match"));
        }

        [TestMethod]
        public void BroadWebConfigNamesRequireTheirYamlPathPatterns()
        {
            const string prestaShopPattern = @"/app/config/parameters\.php$";
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath(
                "PrestaShop database settings candidates",
                @"C:\inetpub\shop\app\config\parameters.php", prestaShopPattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(
                "PrestaShop database settings candidates",
                @"C:\Users\A\Downloads\parameters.php", prestaShopPattern));

            const string limeSurveyPattern = @"/limesurvey/application/config/config\.php$";
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("LimeSurvey",
                @"C:\inetpub\limesurvey\application\config\config.php", limeSurveyPattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("LimeSurvey",
                @"C:\inetpub\other\application\config\config.php", limeSurveyPattern));

            foreach (string category in new[] {
                "Backdrop CMS settings candidates", "LimeSurvey", "PSWM vault candidates",
                "PrestaShop database settings candidates", "ChangeDetection backup candidates",
                "WonderCMS", "Pluck CMS", "Gitea", "Gitea database",
                "Solar-PuTTY session stores", "Duplicati server state" })
                Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath(category,
                    @"C:\inetpub\app\config.php", null), category);
        }

        [TestMethod]
        public void GrafanaIniAndLegacySplunkRetainTheirUnfilteredBehavior()
        {
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Grafana",
                @"C:\Program Files\Grafana\conf\grafana.ini", null));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Grafana",
                @"C:\Users\A\Downloads\grafana.db", null));
            const string databasePattern =
                @"(^|/)(var/lib/grafana|opt/grafana/data|opt/data|data/grafana)/grafana\.db$";
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Grafana",
                @"C:\opt\data\grafana.db", databasePattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Grafana",
                @"C:\Users\A\Downloads\grafana.db", databasePattern));
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Splunk",
                @"C:\Users\A\server.conf", null));

            const string duplicatiPattern =
                @"(^|/)[Dd]uplicati(/config)?/Duplicati-server\.sqlite$|^/config/Duplicati-server\.sqlite$";
            Assert.IsTrue(FileAnalysis.MatchesScopedSensitiveFilePath("Duplicati server state",
                @"C:\ProgramData\Duplicati\config\Duplicati-server.sqlite", duplicatiPattern));
            Assert.IsFalse(FileAnalysis.MatchesScopedSensitiveFilePath("Duplicati server state",
                @"C:\Users\A\Downloads\Duplicati-server.sqlite", duplicatiPattern));
        }

        [TestMethod]
        public void CollectorAssessmentRequiresLocalSystemAndTheExpectedImage()
        {
            Assert.IsTrue(VisualStudioCollectorIndicator.Assess("LocalSystem",
                "\"" + CollectorImage + "\"", true).IsReviewCandidate);
            Assert.IsTrue(VisualStudioCollectorIndicator.Assess(@"NT AUTHORITY\SYSTEM",
                CollectorImage, false).IsReviewCandidate);
            Assert.IsFalse(VisualStudioCollectorIndicator.Assess("NetworkService",
                CollectorImage, true).IsReviewCandidate);
            Assert.IsFalse(VisualStudioCollectorIndicator.Assess("LocalSystem",
                @"C:\Tools\StandardCollector.Service.exe", true).IsReviewCandidate);
            Assert.IsFalse(VisualStudioCollectorIndicator.Assess("LocalSystem",
                null, true).IsReviewCandidate);
            Assert.IsFalse(VisualStudioCollectorIndicator.Assess("LocalSystem",
                CollectorImage + "-old", true).IsReviewCandidate);
        }

        [TestMethod]
        public void CollectorProbeTouchesOnlyTheFixedCompilerPathWhenRelevant()
        {
            var values = new Dictionary<string, string> {
                { "ObjectName", "LocalSystem" }, { "ImagePath", CollectorImage }
            };
            var reads = new List<string>();
            var probes = new List<string>();
            VisualStudioCollectorAssessment assessment = VisualStudioCollectorIndicator.CollectCore(
                name => { reads.Add(name); return values[name]; },
                path => { probes.Add(path); return true; }, @"D:\ProgramData");
            Assert.IsTrue(assessment.IsReviewCandidate);
            Assert.IsTrue(assessment.SetupWmiCompilerPresent);
            CollectionAssert.AreEqual(new[] { "ObjectName", "ImagePath" }, reads);
            CollectionAssert.AreEqual(new[] { @"D:\ProgramData\Microsoft\VisualStudio\SetupWMI\MofCompiler.exe" },
                probes);

            values["ObjectName"] = "NetworkService";
            probes.Clear();
            assessment = VisualStudioCollectorIndicator.CollectCore(
                name => values[name], path => { probes.Add(path); return true; }, @"D:\ProgramData");
            Assert.IsFalse(assessment.IsReviewCandidate);
            Assert.IsFalse(assessment.SetupWmiCompilerPresent);
            Assert.AreEqual(0, probes.Count);
        }
    }
}
