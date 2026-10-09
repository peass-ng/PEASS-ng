using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Helpers;
using winPEAS.Helpers.Search;

namespace winPEAS.Tests
{
    [TestClass]
    public class PasswordSafeFileSelectorTests
    {
        [TestMethod]
        public void CurrentUserInventorySelectsPasswordSafeByExtensionOnly()
        {
            var files = new List<CustomFileInfo>
            {
                new CustomFileInfo("Vault.PSAFE3", ".PSAFE3", @"C:\Users\A\Vault.PSAFE3", 952, false),
                new CustomFileInfo("Vault.kdbx", ".kdbx", @"C:\Users\A\Vault.kdbx", 12, false),
                new CustomFileInfo("Vault.txt", ".txt", @"C:\Users\A\Vault.txt", 12, false),
                new CustomFileInfo("Nested", null, @"C:\Users\A\Nested", 0, true),
            };

            List<string> selected = SearchHelper.SearchUsersInterestingFiles(files);
            CollectionAssert.Contains(selected, @"C:\Users\A\Vault.PSAFE3");
            CollectionAssert.Contains(selected, @"C:\Users\A\Vault.kdbx");
            CollectionAssert.DoesNotContain(selected, @"C:\Users\A\Vault.txt");
            CollectionAssert.DoesNotContain(selected, @"C:\Users\A\Nested");
            Assert.AreEqual(2, selected.Count);
        }
    }
}
