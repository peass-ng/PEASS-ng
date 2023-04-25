using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Helpers;
using winPEAS.Helpers.Search;
using Path = Alphaleonis.Win32.Filesystem.Path;


namespace winPEAS.Tests
{
    [TestClass]
    public class SearchHelperTests
    {
        [TestMethod]
        public void TestGetFilesFastBypassesMAX_PATHLimit()
        {
            // Create a folder with files that have names longer than 260 characters
            string folder = "C:\\Temp\\TestFolder";
            var createdirectory = new DirectoryInfo(folder);
            createdirectory.Create();
            for (int i = 0; i < 10; i++)
            {
                string longName = new string('a', 300);
                string fileName = Path.Combine(folder, $"{longName}_{i}.txt");

                // Use the fsutil command to create a file with a long name
                ProcessStartInfo startInfo = new ProcessStartInfo("fsutil", $"file createnew {fileName} 0")
                {
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    ErrorDialog = true,
                    WindowStyle = ProcessWindowStyle.Normal

                };
                Process.Start(startInfo);

            }

            // Call the GetFilesFast method to get a list of all the files in the folder
            List<CustomFileInfo> files = SearchHelper.GetFilesFast(folder);

            // Get a list of all the files in the folder using System.IO.Directory.GetFiles
            string[] directoryFiles = System.IO.Directory.GetFiles(folder);
            List<FileInfo> expectedFiles = directoryFiles.Select(f => new FileInfo(f)).ToList();

            // Make sure the lists have the same number of elements
            Assert.AreEqual(expectedFiles.Count, files.Count);


        }
    }
}