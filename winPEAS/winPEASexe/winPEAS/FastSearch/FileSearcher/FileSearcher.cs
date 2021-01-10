using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

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
           // ConcurrentBag<string> files = new ConcurrentBag<string>();

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
                   // FindFiles(dir.FullName, pattern, SearchOption.TopDirectoryOnly).ForEach((f) => files.Add(f));
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

        public static List<string> FindFiles(string directory, string filters, SearchOption searchOption)
        {
            if (!Directory.Exists(directory)) return new List<string>();

            var include = (from filter in filters.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) where !string.IsNullOrEmpty(filter.Trim()) select filter.Trim());
            var exclude = (from filter in include where filter.Contains(@"!") select filter);

            include = include.Except(exclude);

            if (include.Count() == 0) include = new string[] { "*" };

            var rxfilters = from filter in exclude select string.Format("^{0}$", filter.Replace("!", "").Replace(".", @"\.").Replace("*", ".*").Replace("?", "."));
            Regex regex = new Regex(string.Join("|", rxfilters.ToArray()));

            List<Thread> workers = new List<Thread>();
            List<string> files = new List<string>();

            foreach (string filter in include)
            {
                Thread worker = new Thread(
                    new ThreadStart(
                        delegate
                        {
                            try
                            {
                                //string[] allfiles = Directory.GetFiles(directory, filter, searchOption);
                                string[] allfiles = Directory.GetFiles(directory, filter, SearchOption.TopDirectoryOnly);
                                if (exclude.Count() > 0)
                                {
                                    lock (files)
                                    {
                                        files.AddRange(allfiles.Where(p => !regex.Match(p).Success));
                                    }
                                }
                                else
                                {
                                    lock (files)
                                    {
                                        files.AddRange(allfiles);
                                    }
                                }
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

                        }
                    ));

                workers.Add(worker);
                worker.Start();
            }

            foreach (Thread worker in workers)
            {
                worker.Join();
            }

            return files;
        }
    }
}
