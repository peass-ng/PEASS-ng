using System;
using System.IO;
using winPEAS.Helpers;
using winPEAS.Info.UserInfo;

namespace winPEAS.KnownFileCreds.SuperPutty
{
    static class SuperPutty
    {
        public static void PrintInfo()
        {
            PrintConfigurationFiles();
        }

        private static void PrintConfigurationFiles()
        {
            Beaprint.MainPrint("SuperPutty configuration files");

            var dirs = User.GetUsersFolders();
            var filter = "sessions*.xml";

            foreach (var dir in dirs)
            {
                try
                {
                    var path = $"{dir}\\Documents\\SuperPuTTY\\";
                    if (Directory.Exists(path))
                    {
                        var files = Directory.EnumerateFiles(path, filter, SearchOption.TopDirectoryOnly);

                        foreach (var file in files)
                        {
                            Beaprint.BadPrint($"     {file}");
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
