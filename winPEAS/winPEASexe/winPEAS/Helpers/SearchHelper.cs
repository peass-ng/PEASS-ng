using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using winPEAS.FastSearch.FileSearcher;
using winPEAS.KnownFileCreds;

namespace winPEAS.Helpers
{
    internal static class SearchHelper
    {
        public static List<string> FindFiles_old_implementation(string path, string patterns)
        {
            // finds files matching one or more patterns under a given path, recursive
            // adapted from http://csharphelper.com/blog/2015/06/find-files-that-match-multiple-patterns-in-c/
            //      pattern: "*pass*;*.png;"

            var files = new List<string>();

            if (!Directory.Exists(path))
            {
                return files;
            }

            try
            {
                // search every pattern in this directory's files
                foreach (string pattern in patterns.Split(';'))
                {
                    files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                }

                // go recurse in all sub-directories
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(FindFiles_old_implementation(directory, patterns));
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }
            catch (DirectoryNotFoundException) { }

            return files;
        }

        public static List<string> FindFiles_fileSearcher(string path, string patterns)
        {
            var files = new List<string>();

            foreach (string pattern in patterns.Split(';'))
            {
                // var found = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
                List<FileInfo> res = FileSearcher.GetFilesFast(path, pattern);
                files.AddRange(res.Select(s => s.FullName));
            }

            return files;
        }


        private static void PrintSearchResults(IEnumerable<string> results, string description = null)
        {
            Beaprint.LinkPrint($"------------------------- results: {description ?? string.Empty} --------------------------------");
            if (results != null)
            {
                Beaprint.LinkPrint(string.Join("\n", results ?? Enumerable.Empty<string>()));
            }
            Beaprint.LinkPrint($"------------------------- results: {description ?? string.Empty} --------------------------------");
            Beaprint.LinkPrint("\n\n\n\n");
        }

        public static List<string> FindFiles(string path, string patterns)
        {
            List<string> result = new List<string>();

            MeasureHelper.MeasureMethod(() => result = FindFiles_old_implementation(path, patterns), "old implementation");
            PrintSearchResults(result, "old implementation");

            MeasureHelper.MeasureMethod(() => result = FindFiles_fileSearcher(path, patterns), "new implementation");
            PrintSearchResults(result, "new implementation");

            return result;
        }

        public static void FindFiles_old_implementation(string path, string patterns, Dictionary<string, string> color)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                // search every pattern in this directory's files
                foreach (string pattern in patterns.Split(';'))
                {
                    Beaprint.AnsiPrint("    " + String.Join("\n    ", Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly).Where(filepath => !filepath.Contains(".dll"))), color);
                }

                if (!Checks.Checks.IsSearchFast)
                {
                    Thread.Sleep(Checks.Checks.SearchTime);
                }

                // go recurse in all sub-directories
                foreach (string directory in Directory.GetDirectories(path))
                {
                    if (!directory.Contains("AppData"))
                    {
                        FindFiles_old_implementation(directory, patterns, color);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }
            catch (DirectoryNotFoundException) { }
        }

        public static void FindFiles_fileSearcher(string path, string patterns, Dictionary<string, string> color, HashSet<string> excludedDirs = null)
        {
            // search every pattern in this directory's files
            foreach (string pattern in patterns.Split(';'))
            {
                // var found = Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly).Where(filepath => !filepath.Contains(".dll"));
                List<FileInfo> res = FileSearcher.GetFilesFast(path, pattern, excludedDirs);
                var found = res.Where(filepath => filepath.Extension != null && !filepath.Extension.Equals("dll")).Select(s => s.FullName);
                Beaprint.AnsiPrint("    " + string.Join("\n    ", found), color);
            }
        }

        public static void FindFiles(string path, string patterns, Dictionary<string, string> color)
        {
            Beaprint.LinkPrint($"------------------------- results: old implementation --------------------------------");
            MeasureHelper.MeasureMethod(() => FindFiles_old_implementation(path, patterns, color), "old implementation");
            Beaprint.LinkPrint($"------------------------- results: old implementation --------------------------------");
            Beaprint.LinkPrint("\n\n\n\n");
            Beaprint.LinkPrint($"------------------------- results: new implementation --------------------------------");
            HashSet<string> excludedDirs = new HashSet<string>() { "AppData" };
            MeasureHelper.MeasureMethod(() => FindFiles_fileSearcher(path, patterns, color, excludedDirs), "new implementation");
            Beaprint.LinkPrint($"------------------------- results: new implementation --------------------------------");
        }
    }
}
