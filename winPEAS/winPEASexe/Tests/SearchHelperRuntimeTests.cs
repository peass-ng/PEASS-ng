using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    }
}
