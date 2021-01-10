using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace winPEAS.Helpers
{
    internal static class SearchHelper
    {
        public static List<string> FindFiles(string path, string patterns)
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
                    files.AddRange(FindFiles(directory, patterns));
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }
            catch (DirectoryNotFoundException) { }

            return files;
        }

        public static void FindFiles(string path, string patterns, Dictionary<string, string> color)
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
                        FindFiles(directory, patterns, color);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }
            catch (DirectoryNotFoundException) { }
        }        
    }
}
