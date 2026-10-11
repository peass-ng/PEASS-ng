using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using winPEAS.Checks;
using winPEAS.Helpers.Search;

namespace winPEAS.Tests
{
    [TestClass]
    public class SearchHelperRuntimeTests
    {
        [TestMethod]
        public void FileInventoryKeepsRealFilesAndDoesNotEnterJunctions()
        {
            string parent = Path.Combine(Path.GetTempPath(), "winpeas-search-" + Guid.NewGuid().ToString("N"));
            string root = Path.Combine(parent, "root");
            string external = Path.Combine(parent, "external");
            string real = Path.Combine(root, "real");
            string link = Path.Combine(root, "link");

            try
            {
                Directory.CreateDirectory(real);
                Directory.CreateDirectory(external);
                File.WriteAllText(Path.Combine(real, "local-review.txt"), "local");
                File.WriteAllText(Path.Combine(external, "linked-review.txt"), "linked");

                using (var process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/d /c mklink /J \"" + link + "\" \"" + external + "\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    Assert.IsTrue(process.WaitForExit(5000), "Junction creation timed out.");
                    Assert.AreEqual(0, process.ExitCode, "Junction creation failed.");
                }

                var paths = SearchHelper.GetFilesFast(root, "*.txt").Select(file => file.FullPath).ToList();
                Assert.IsTrue(paths.Any(path => path.EndsWith("local-review.txt", StringComparison.OrdinalIgnoreCase)));
                Assert.IsFalse(paths.Any(path => path.EndsWith("linked-review.txt", StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                if (Directory.Exists(link)) Directory.Delete(link);
                if (Directory.Exists(parent)) Directory.Delete(parent, true);
            }
        }

        [TestMethod]
        public void ReusedRegexesPreserveSearchContentResults()
        {
            const string pattern = @"(?i)(password|token)\s*[:=]\s*[^\s,;]+";
            string[] lines =
            {
                "user=alice password: Secret123 token=abc.def",
                "no credentials here",
                "PASSWORD = another-secret; token: second-token",
                "token=x token=y token=z"
            };

            var matchRegex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(120));
            var captureRegex = new Regex(pattern, RegexOptions.IgnoreCase);

            foreach (string line in lines)
            {
                var singleUseResults = FileAnalysis.SearchContent(line, pattern, true);
                var reusedResults = FileAnalysis.SearchContent(line, pattern, matchRegex, captureRegex);
                CollectionAssert.AreEqual(singleUseResults, reusedResults);
            }

            CollectionAssert.AreEqual(
                new[] { "password: Secret123", "token=abc.def" },
                FileAnalysis.SearchContent(lines[0], pattern, matchRegex, captureRegex));
            Assert.AreEqual(0,
                FileAnalysis.SearchContent(lines[1], pattern, matchRegex, captureRegex).Count);
        }

        [TestMethod]
        public void ReusedRegexesPreserveMatchFilteringAndLimit()
        {
            const string pattern = @"\b\w+\b";
            const string line = "a bb ccc dddd eeeee ffffff ggggggg hhhhhhhh iiiiiiiii jjjjjjjjjj " +
                "kkkkkkkkkkk llllllllllll mmmmmmmmmmmmm nnnnnnnnnnnnnn";
            var matchRegex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(120));
            var captureRegex = new Regex(pattern);

            var reusedResults = FileAnalysis.SearchContent(line, pattern, matchRegex, captureRegex);
            CollectionAssert.AreEqual(
                FileAnalysis.SearchContent(line, pattern, false),
                reusedResults);
            Assert.AreEqual(9, reusedResults.Count);
        }
    }
}
