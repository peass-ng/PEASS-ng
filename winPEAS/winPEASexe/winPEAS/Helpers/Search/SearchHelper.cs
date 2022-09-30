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

        public static List<string> StaticExtensions = new List<string>() {
            // archives
            ".7z", ".tar", ".zip", ".gz",

            // audio/video
            ".avi", ".mp3", ".mp4", ".wav", ".wmf", ".wmv", ".ts", ".pak",

            // icons
            ".ico",

            // fonts
            ".eot", ".fnt", ".fon", ".otf", ".odttf", ".ttc", ".ttf", ".woff", "woff2", "woff3",

            // images
            ".bmp", ".emf", ".gif", ".pm",
            ".jif", ".jfi", ".jfif", ".jpe", ".jpeg", ".jpg",
            ".png", ".psd", ".raw", ".svg", ".svgz", ".tif", ".tiff", ".webp",
        };

        public static List<CustomFileInfo> GetFilesFast(string folder, string pattern = "*", HashSet<string> excludedDirs = null, bool isFoldersIncluded = false)
        {
            ConcurrentBag<CustomFileInfo> files = new ConcurrentBag<CustomFileInfo>();
            IEnumerable<DirectoryInfo> startDirs = GetStartDirectories(folder, files, pattern, isFoldersIncluded);
            IList<DirectoryInfo> startDirsExcluded = new List<DirectoryInfo>();
            IList<string> known_dirs = new List<string>();

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
                        {
                            if (!StaticExtensions.Contains(f.Extension.ToLower()))
                            {
                                // It should always be lesss than 260, but some times it isn't so this will bypass that file
                                if (Checks.Checks.IsLongPath || f.FullName.Length <= 260)
                                {
                                    CustomFileInfo file_info = new CustomFileInfo(f.Name, f.Extension, f.FullName, f.Length, false);
                                    files.Add(file_info);

                                    CustomFileInfo file_dir = new CustomFileInfo(f.Directory.Name, "", f.Directory.FullName, 0, true);
                                    if (!known_dirs.Contains(file_dir.FullPath))
                                    {
                                        known_dirs.Add(file_dir.FullPath);
                                        files.Add(file_dir);
                                    }
                                }
                                else if (f.FullName.Length > 260)
                                    Beaprint.LongPathWarning(f.FullName);
                            }
                        }
                        ) ;
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
                            if (Checks.Checks.IsLongPath || directory.FullName.Length <= 260)
                                files.Add(new CustomFileInfo(directory.Name, null, directory.FullName, 0, true));

                            else if (directory.FullName.Length > 260)
                                Beaprint.LongPathWarning(directory.FullName);
                        }
                    }

                    foreach (var f in dirInfo.GetFiles(pattern))
                    {
                        if (!StaticExtensions.Contains(f.Extension.ToLower()))
                        {
                            if (Checks.Checks.IsLongPath || f.FullName.Length <= 260)
                                files.Add(new CustomFileInfo(f.Name, f.Extension, f.FullName, f.Length, false));

                            else if (f.FullName.Length > 260)
                                Beaprint.LongPathWarning(f.FullName);
                        }
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
            SearchHelper.RootDirCurrentUser = SearchHelper.GetFilesFast(rootCurrentUserSearchPath, GlobalPattern, isFoldersIncluded: true);

            // c:\Program Files\
            string rootProgramFiles = $"{SystemDrive}\\Program Files\\";
            SearchHelper.ProgramFiles = SearchHelper.GetFilesFast(rootProgramFiles, GlobalPattern, isFoldersIncluded: true);

            // c:\Program Files (x86)\
            string rootProgramFilesX86 = $"{SystemDrive}\\Program Files (x86)\\";
            SearchHelper.ProgramFilesX86 = SearchHelper.GetFilesFast(rootProgramFilesX86, GlobalPattern, isFoldersIncluded: true);

            // c:\Documents and Settings\
            string documentsAndSettings = $"{SystemDrive}\\Documents and Settings\\";
            SearchHelper.DocumentsAndSettings = SearchHelper.GetFilesFast(documentsAndSettings, GlobalPattern, isFoldersIncluded: true);

            // c:\ProgramData\Microsoft\Group Policy\History
            string groupPolicyHistory = $"{SystemDrive}\\ProgramData\\Microsoft\\Group Policy\\History";
            SearchHelper.GroupPolicyHistory = SearchHelper.GetFilesFast(groupPolicyHistory, GlobalPattern, isFoldersIncluded: true);

            // c:\Documents and Settings\All Users\Application Data\\Microsoft\\Group Policy\\History
            string groupPolicyHistoryLegacy = $"{documentsAndSettings}\\All Users\\Application Data\\Microsoft\\Group Policy\\History";
            //SearchHelper.GroupPolicyHistoryLegacy = SearchHelper.GetFilesFast(groupPolicyHistoryLegacy, globalPattern);
            var groupPolicyHistoryLegacyFiles = SearchHelper.GetFilesFast(groupPolicyHistoryLegacy, GlobalPattern, isFoldersIncluded: true);
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
