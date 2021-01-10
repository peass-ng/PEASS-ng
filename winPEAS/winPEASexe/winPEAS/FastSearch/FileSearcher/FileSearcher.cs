using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using winPEAS.Helpers;

namespace winPEAS.FastSearch.FileSearcher
{

    /// <summary>
    /// Represents a class for fast file search.
    /// </summary>
    public class FileSearcher
    {
        public static List<FileInfo> GetFilesFast(string folder, string pattern = "*", HashSet<string> excludedDirs = null)
        {
            ConcurrentBag<FileInfo> files = new ConcurrentBag<FileInfo>();

            //Beaprint.InfoPrint($"[*] folder 1: '{folder}'");

            IEnumerable<DirectoryInfo> startDirs = GetStartDirectories(folder, files, pattern);

            IList<DirectoryInfo> startDirsExcluded = startDirs.ToList();

            if (excludedDirs != null)
            {
                startDirsExcluded =
                    (from startDir in startDirs
                        from excludedDir in excludedDirs
                        where !startDir.FullName.Contains(excludedDir)
                        select startDir).ToList();
            }     

            //Beaprint.InfoPrint($"[*] folder 2: '{folder}'  pattern: '{pattern}'");
            //Beaprint.InfoPrint($"[*] folder 2: '{folder}'  GetStartDirectories: '{string.Join("\n", startDirs.Select(d => d.FullName))}'");
            //Beaprint.InfoPrint($"[*] folder 2: '{folder}'  startDirsExcluded: '{string.Join("\n", startDirsExcluded.Select(d => d.FullName))}'");

            //Beaprint.InfoPrint($"[*]  folder 3: '{folder}' excludedDirs: '{string.Join("\n", excludedDirs ?? Enumerable.Empty<string>()) }'");
            startDirsExcluded.AsParallel().ForAll((d) =>
            {
                GetStartDirectories(d.FullName, files, pattern).AsParallel().ForAll((dir) =>
                {
                    GetFiles(dir.FullName, pattern).ForEach((f) => files.Add(f));
                });
            });

            // !!!! TODO
            // probably we need to exclude the excluded dirs here - not in parallel processing

            //Parallel.ForEach(startDirsExcluded, (d) =>
            //{
            //    Parallel.ForEach(GetStartDirectories(d.FullName, files, pattern), (dir) =>
            //    {
            //        GetFiles(dir.FullName, pattern).ForEach((f) => files.Add(f));
            //    });
            //});

            return files.ToList();
        }

        private static List<DirectoryInfo> GetStartDirectories(string folder, ConcurrentBag<FileInfo> files, string pattern)
        {
            DirectoryInfo dirInfo = null;
            DirectoryInfo[] directories = null;
            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                foreach (var f in dirInfo.GetFiles(pattern))
                {
                    files.Add(f);
                }

                if (directories.Length > 1)
                    return new List<DirectoryInfo>(directories);

                if (directories.Length == 0)
                    return new List<DirectoryInfo>();

            }
            catch (UnauthorizedAccessException ex)
            {
                return new List<DirectoryInfo>();
            }
            catch (PathTooLongException ex)
            {
                return new List<DirectoryInfo>();
            }
            catch (DirectoryNotFoundException ex)
            {
                return new List<DirectoryInfo>();
            }

            return GetStartDirectories(directories[0].FullName, files, pattern);
        }

        public static List<FileInfo> GetFiles(string folder, string pattern = "*")
        {
            DirectoryInfo dirInfo;
            DirectoryInfo[] directories;
            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                if (directories.Length == 0)
                {
                    return new List<FileInfo>(dirInfo.GetFiles(pattern));
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new List<FileInfo>();
            }
            catch (PathTooLongException)
            {
                return new List<FileInfo>();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<FileInfo>();
            }

            List<FileInfo> result = new List<FileInfo>();

            foreach (var d in directories)
            {
                result.AddRange(GetFiles(d.FullName, pattern));
            }

            try
            {
                result.AddRange(dirInfo.GetFiles(pattern));
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (PathTooLongException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }

            return result;
        }
    }
}
