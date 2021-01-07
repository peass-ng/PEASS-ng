using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FastSearchLibrary
{

    /// <summary>
    /// Represents a class for fast file search.
    /// </summary>
    public class FileSearcher
    {
        static public List<FileInfo> GetFilesFast(string folder, string pattern = "*", HashSet<string> excludedDirs = null)
        {
            ConcurrentBag<FileInfo> files = new ConcurrentBag<FileInfo>();

            List<DirectoryInfo> startDirs = GetStartDirectories(folder, files, pattern, excludedDirs);

            //startDirs.AsParallel().ForAll((d) =>
            //{
            //    GetStartDirectories(d.FullName, files, pattern).AsParallel().ForAll((dir) =>
            //    {
            //        GetFiles(dir.FullName, pattern).ForEach((f) => files.Add(f));
            //    });
            //});

            Parallel.ForEach(startDirs, (d) =>
            {
                Parallel.ForEach(GetStartDirectories(d.FullName, files, pattern, excludedDirs), (dir) =>
                {
                    GetFiles(dir.FullName, pattern).ForEach((f) => files.Add(f));
                });
            });
            
            return files.ToList();
        }

        static private List<DirectoryInfo> GetStartDirectories(
            string folder,
            ConcurrentBag<FileInfo> files,
            string pattern,
            HashSet<string> excludedDirs = null)
        {
            DirectoryInfo[] directories;

            if (excludedDirs != null)
            {
                foreach (var excludedDir in excludedDirs)
                {
                    if (folder.Contains(excludedDir))
                    {
                        return new List<DirectoryInfo>();
                    }
                }
            }

            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(folder);
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

        static public List<FileInfo> GetFiles(string folder, string pattern = "*")
        {
            DirectoryInfo dirInfo;
            DirectoryInfo[] directories;
            try
            {
                dirInfo = new DirectoryInfo(folder);
                directories = dirInfo.GetDirectories();

                if (directories.Length == 0)
                    return new List<FileInfo>(dirInfo.GetFiles(pattern));
            }
            catch (UnauthorizedAccessException ex)
            {
                return new List<FileInfo>();
            }
            catch (PathTooLongException ex)
            {
                return new List<FileInfo>();
            }
            catch (DirectoryNotFoundException ex)
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
            catch (UnauthorizedAccessException ex)
            {
            }
            catch (PathTooLongException ex)
            {
            }
            catch (DirectoryNotFoundException ex)
            {
            }

            return result;
        }
    }
}
