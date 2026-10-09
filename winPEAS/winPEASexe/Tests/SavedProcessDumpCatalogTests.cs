using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Tests
{
    [TestClass]
    public class SavedProcessDumpCatalogTests
    {
        [TestMethod]
        public void CatalogListsOnlyExactSavedDumpNamesWithoutReadingContent()
        {
            var entry = YamlConfigHelper.GetWindowsSearchConfig().search
                .Single(item => item.name == "Saved authentication process dump candidates");
            var files = entry.value.files.ToArray();
            Assert.IsTrue(entry.value.config.auto_check);
            CollectionAssert.Contains(entry.value.disable, "linpeas");
            CollectionAssert.AreEquivalent(new[] { "lsass.dmp", "lsass.zip" },
                files.Select(file => file.name).ToArray());

            foreach (var file in files)
            {
                Assert.AreEqual("f", file.value.type);
                Assert.IsTrue(file.value.just_list_file == true);
                Assert.IsTrue(string.IsNullOrEmpty(file.value.bad_regex));
                Assert.IsTrue(string.IsNullOrEmpty(file.value.check_extra_path));
                Assert.IsFalse(file.name.Contains("*"));
            }

            Assert.IsTrue(files.Any(file => string.Equals(file.name, "LSASS.DMP",
                StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(files.Any(file => string.Equals(file.name, "lsass.dmp.bak",
                StringComparison.OrdinalIgnoreCase)));
            Assert.IsFalse(files.Any(file => string.Equals(file.name, "unrelated.zip",
                StringComparison.OrdinalIgnoreCase)));
        }
    }
}
