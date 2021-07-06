using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace winPEAS.Helpers.Search
{
    static class SearchHelper
    {
        public static List<CustomFileInfo> RootDirUsers = new List<CustomFileInfo>();
        public static List<CustomFileInfo> RootDirCurrentUser = new List<CustomFileInfo>();
        public static List<CustomFileInfo> ProgramFiles = new List<CustomFileInfo>();
        public static List<CustomFileInfo> ProgramFilesX86 = new List<CustomFileInfo>();
        public static List<CustomFileInfo> DocumentsAndSettings = new List<CustomFileInfo>();
        public static List<CustomFileInfo> GroupPolicyHistory = new List<CustomFileInfo>();

        public static string SystemDrive = Environment.GetEnvironmentVariable("SystemDrive");
        private static string GlobalPattern = "*";

        public static List<CustomFileInfo> GetFilesFast(string folder, string pattern = "*", HashSet<string> excludedDirs = null, bool isFoldersIncluded = false)
        {
            ConcurrentBag<CustomFileInfo> files = new ConcurrentBag<CustomFileInfo>();
            IEnumerable<DirectoryInfo> startDirs = GetStartDirectories(folder, files, pattern, isFoldersIncluded);
            IList<DirectoryInfo> startDirsExcluded = new List<DirectoryInfo>();

            if (excludedDirs != null)
            {
                foreach (var startDir in startDirs)
                {
                    bool shouldAdd = true;
                    string startDirLower = startDir.FullName.ToLower();

                    shouldAdd = !excludedDirs.Contains(startDirLower);

                    if (shouldAdd)
                    {
                        startDirsExcluded.Add(startDir);
                    }
                }
            }
            else
            {
                startDirsExcluded = startDirs.ToList();
            }

            Parallel.ForEach(startDirsExcluded, (d) =>
            {
                Parallel.ForEach(GetStartDirectories(d.FullName, files, pattern, isFoldersIncluded), (dir) =>
                {
                    GetFiles(dir.FullName, pattern).ForEach(
                        (f) =>
                            files.Add(new CustomFileInfo(f.Name, f.Extension, f.FullName, false))
                        );
                });
            });

            return files.ToList();
        }
       
        private static List<FileInfo> GetFiles(string folder, string pattern = "*")
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
            catch (Exception)
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
            catch (Exception)
            {
            }

            return result;
        }

        private static IEnumerable<DirectoryInfo> GetStartDirectories(string folder, ConcurrentBag<CustomFileInfo> files, string pattern, bool isFoldersIncluded = false)
        {
            while (true)
            {
                DirectoryInfo[] directories = null;
                try
                {
                    var dirInfo = new DirectoryInfo(folder);
                    directories = dirInfo.GetDirectories();

                    if (isFoldersIncluded)
                    {
                        foreach (var directory in directories)
                        {
                            files.Add(new CustomFileInfo(directory.Name, null, directory.FullName, true));
                        }
                    }

                    foreach (var f in dirInfo.GetFiles(pattern))
                    {
                        files.Add(new CustomFileInfo(f.Name, f.Extension, f.FullName, false));
                    }

                    if (directories.Length > 1) return new List<DirectoryInfo>(directories);

                    if (directories.Length == 0) return new List<DirectoryInfo>();
                }
                catch (UnauthorizedAccessException)
                {
                    return new List<DirectoryInfo>();
                }
                catch (PathTooLongException)
                {
                    return new List<DirectoryInfo>();
                }
                catch (DirectoryNotFoundException)
                {
                    return new List<DirectoryInfo>();
                }
                catch (Exception)
                {
                    return new List<DirectoryInfo>();
                }

                folder = directories[0].FullName;
                isFoldersIncluded = false;
            }
        }

        internal static void CreateSearchDirectoriesList()
        {
            // c:\users
            string rootUsersSearchPath = $"{SystemDrive}\\Users\\";
            SearchHelper.RootDirUsers = SearchHelper.GetFilesFast(rootUsersSearchPath, GlobalPattern, isFoldersIncluded: true);

            // c:\users\current_user
            string rootCurrentUserSearchPath = Environment.GetEnvironmentVariable("USERPROFILE");
            SearchHelper.RootDirCurrentUser = SearchHelper.GetFilesFast(rootCurrentUserSearchPath, GlobalPattern);

            // c:\Program Files\
            string rootProgramFiles = $"{SystemDrive}\\Program Files\\";
            SearchHelper.ProgramFiles = SearchHelper.GetFilesFast(rootProgramFiles, GlobalPattern);

            // c:\Program Files (x86)\
            string rootProgramFilesX86 = $"{SystemDrive}\\Program Files (x86)\\";
            SearchHelper.ProgramFilesX86 = SearchHelper.GetFilesFast(rootProgramFilesX86, GlobalPattern);

            // c:\Documents and Settings\
            string documentsAndSettings = $"{SystemDrive}\\Documents and Settings\\";
            SearchHelper.DocumentsAndSettings = SearchHelper.GetFilesFast(documentsAndSettings, GlobalPattern);

            // c:\ProgramData\Microsoft\Group Policy\History
            string groupPolicyHistory = $"{SystemDrive}\\ProgramData\\Microsoft\\Group Policy\\History";
            SearchHelper.GroupPolicyHistory = SearchHelper.GetFilesFast(groupPolicyHistory, GlobalPattern);

            // c:\Documents and Settings\All Users\Application Data\\Microsoft\\Group Policy\\History
            string groupPolicyHistoryLegacy = $"{documentsAndSettings}\\All Users\\Application Data\\Microsoft\\Group Policy\\History";
            //SearchHelper.GroupPolicyHistoryLegacy = SearchHelper.GetFilesFast(groupPolicyHistoryLegacy, globalPattern);
            var groupPolicyHistoryLegacyFiles = SearchHelper.GetFilesFast(groupPolicyHistoryLegacy, GlobalPattern);

            SearchHelper.GroupPolicyHistory.AddRange(groupPolicyHistoryLegacyFiles);
        }

        internal static void CleanLists()
        {
            SearchHelper.RootDirUsers = null;
            SearchHelper.RootDirCurrentUser = null;
            SearchHelper.ProgramFiles = null;
            SearchHelper.ProgramFilesX86 = null;
            SearchHelper.DocumentsAndSettings = null;
            SearchHelper.GroupPolicyHistory = null;

            GC.Collect();
        }

        internal static IEnumerable<CustomFileInfo> SearchUserCredsFiles()
        {
            var patterns = new List<string>
            {
                ".*credential.*",
                ".*password.*"
            };

            foreach (var file in SearchHelper.RootDirUsers)
            {                
                //string extLower = file.Extension.ToLower();

                if (!file.IsDirectory)
                {
                    string nameLower = file.Filename.ToLower();
                    //  string nameExtLower = nameLower + "." + extLower;

                    foreach (var pattern in patterns)
                    {
                        if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                        {
                            yield return file;

                            break;
                        }
                    }

                }
            }
        }

        internal static List<string> SearchUsersInterestingFiles()
        {
            var result = new List<string>();

            foreach (var file in SearchHelper.RootDirCurrentUser)
            {
                if (!file.IsDirectory)
                {
                    string extLower = file.Extension.ToLower();
                    string nameLower = file.Filename.ToLower();

                    if (Patterns.WhitelistExtensions.Contains(extLower) ||
                        Patterns.WhiteListExactfilenamesWithExtensions.Contains(nameLower))
                    {
                        result.Add(file.FullPath);
                    }
                    else
                    {
                        foreach (var pattern in Patterns.WhiteListRegexp)
                        {
                            if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                            {
                                result.Add(file.FullPath);

                                break;
                            }
                        }
                    }

                }               
            }

            return result;
        }

        internal static List<string> FindCachedGPPPassword()
        {
            var result = new List<string>();

            var allowedExtensions = new HashSet<string>
            {
                ".xml"
            };

            foreach (var file in SearchHelper.GroupPolicyHistory)
            {
                if (!file.IsDirectory)
                {
                    string extLower = file.Extension.ToLower();

                    if (allowedExtensions.Contains(extLower))
                    {
                        result.Add(file.FullPath);
                    }
                }
            }

            return result;
        }

        internal static IEnumerable<string> SearchMcAfeeSitelistFiles()
        {
            var allowedFilenames = new HashSet<string>
            {
                "sitelist.xml"
            };

            string programDataPath = $"{SystemDrive}\\ProgramData\\";
            var programData = SearchHelper.GetFilesFast(programDataPath, GlobalPattern);

            var searchFiles = new List<CustomFileInfo>();
            searchFiles.AddRange(SearchHelper.ProgramFiles);
            searchFiles.AddRange(SearchHelper.ProgramFilesX86);
            searchFiles.AddRange(programData);
            searchFiles.AddRange(SearchHelper.DocumentsAndSettings);
            searchFiles.AddRange(SearchHelper.RootDirUsers);

            foreach (var file in searchFiles)
            {
                if (!file.IsDirectory)
                {
                    string filenameToLower = file.Filename.ToLower();

                    if (allowedFilenames.Contains(filenameToLower))
                    {
                        yield return file.FullPath;
                    }
                }
            }
        }

        internal static List<string> SearchCurrentUserDocs()
        {
            var result = new List<string>();

            var allowedRegexp = new List<string>
            {
                ".*diagram.*",
            };

            var allowedExtensions = new HashSet<string>()
            {
                ".doc",
                ".docx",
                ".vsd",
                ".xls",
                ".xlsx",
                ".pdf",
            };

            foreach (var file in SearchHelper.RootDirCurrentUser)
            {
                if (!file.IsDirectory)
                {
                    string extLower = file.Extension.ToLower();
                    string nameLower = file.Filename.ToLower();

                    if (allowedExtensions.Contains(extLower))
                    {
                        result.Add(file.FullPath);
                    }
                    else
                    {
                        foreach (var pattern in allowedRegexp)
                        {
                            if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                            {
                                result.Add(file.FullPath);

                                break;
                            }
                        }
                    }
                }               
            }

            return result;
        }

        internal static List<string> SearchUsersDocs()
        {
            var result = new List<string>();

            var allowedRegexp = new List<string>
            {
                ".*diagram.*",
            };

            var allowedExtensions = new HashSet<string>()
            {
                ".doc",
                ".docx",
                ".vsd",
                ".xls",
                ".xlsx",
                ".pdf",
            };

            foreach (var file in SearchHelper.RootDirUsers)
            {
                if (!file.IsDirectory)
                {
                    string extLower = file.Extension.ToLower();
                    string nameLower = file.Filename.ToLower();

                    if (allowedExtensions.Contains(extLower))
                    {
                        result.Add(file.FullPath);
                    }
                    else
                    {
                        foreach (var pattern in allowedRegexp)
                        {
                            if (Regex.IsMatch(nameLower, pattern, RegexOptions.IgnoreCase))
                            {
                                result.Add(file.FullPath);

                                break;
                            }
                        }
                    }
                }               
            }

            return result;
        }
    }
}
