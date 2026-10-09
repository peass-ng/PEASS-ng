using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;

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

        internal static bool ShouldRetainInventoryFile(string filename, string extension)
        {
            string ext = (extension ?? string.Empty).ToLowerInvariant();
            if (!StaticExtensions.Contains(ext)) return true;
            if (ext != ".7z" && ext != ".zip") return false;
            string name = filename ?? string.Empty;
            return name.Equals("MEMORY.7z", StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf("bitlocker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("recovery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("credential", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static List<CustomFileInfo> GetFilesFast(string folder, string pattern = "*", HashSet<string> excludedDirs = null, bool isFoldersIncluded = false)
        {
            ConcurrentBag<CustomFileInfo> files = new ConcurrentBag<CustomFileInfo>();
            IEnumerable<DirectoryInfo> startDirs = GetStartDirectories(folder, files, pattern, isFoldersIncluded);
            IList<DirectoryInfo> startDirsExcluded = new List<DirectoryInfo>();
            ConcurrentDictionary<string, byte> known_dirs = new ConcurrentDictionary<string, byte>();

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
                var foundFiles = GetFiles(d.FullName, pattern);
                foreach (var f in foundFiles)
                {
                    if (f != null && ShouldRetainInventoryFile(f.Name, f.Extension))
                    {
                        CustomFileInfo file_info = new CustomFileInfo(f.Name, f.Extension, f.FullName, f.Length, false);
                        files.Add(file_info);

                        CustomFileInfo file_dir = new CustomFileInfo(f.Directory.Name, "", f.Directory.FullName, 0, true);
                        if (known_dirs.TryAdd(file_dir.FullPath, 0))
                        {
                            files.Add(file_dir);
                        }
                    }
                }
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

            ConcurrentBag<FileInfo> result = new ConcurrentBag<FileInfo>();

            Parallel.ForEach(directories, (d) =>
            {
                foreach (var file in GetFiles(d.FullName, pattern))
                {
                    result.Add(file);
                }
            });

            try
            {
                foreach (var file in dirInfo.GetFiles(pattern))
                {
                    result.Add(file);
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
            catch (Exception)
            {
            }

            return result.ToList();
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
                        if (ShouldRetainInventoryFile(f.Name, f.Extension))
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
            RootDirUsers = GetFilesFast(rootUsersSearchPath, GlobalPattern, isFoldersIncluded: true);

            // c:\users\current_user
            string rootCurrentUserSearchPath = Environment.GetEnvironmentVariable("USERPROFILE");
            RootDirCurrentUser = GetFilesFast(rootCurrentUserSearchPath, GlobalPattern, isFoldersIncluded: true);

            // c:\Program Files\
            string rootProgramFiles = $"{SystemDrive}\\Program Files\\";
            ProgramFiles = GetFilesFast(rootProgramFiles, GlobalPattern, isFoldersIncluded: true);

            // c:\Program Files (x86)\
            string rootProgramFilesX86 = $"{SystemDrive}\\Program Files (x86)\\";
            ProgramFilesX86 = GetFilesFast(rootProgramFilesX86, GlobalPattern, isFoldersIncluded: true);

            // c:\Documents and Settings\
            string documentsAndSettings = $"{SystemDrive}\\Documents and Settings\\";
            DocumentsAndSettings = GetFilesFast(documentsAndSettings, GlobalPattern, isFoldersIncluded: true);

            // c:\ProgramData\Microsoft\Group Policy\History
            string groupPolicyHistory = $"{SystemDrive}\\ProgramData\\Microsoft\\Group Policy\\History";
            GroupPolicyHistory = GetFilesFast(groupPolicyHistory, GlobalPattern, isFoldersIncluded: true);

            // c:\Documents and Settings\All Users\Application Data\\Microsoft\\Group Policy\\History
            string groupPolicyHistoryLegacy = $"{documentsAndSettings}\\All Users\\Application Data\\Microsoft\\Group Policy\\History";
            //SearchHelper.GroupPolicyHistoryLegacy = SearchHelper.GetFilesFast(groupPolicyHistoryLegacy, globalPattern);
            var groupPolicyHistoryLegacyFiles = GetFilesFast(groupPolicyHistoryLegacy, GlobalPattern, isFoldersIncluded: true);
            GroupPolicyHistory.AddRange(groupPolicyHistoryLegacyFiles);
        }

        internal static void CleanLists()
        {
            RootDirUsers = null;
            RootDirCurrentUser = null;
            ProgramFiles = null;
            ProgramFilesX86 = null;
            DocumentsAndSettings = null;
            GroupPolicyHistory = null;

            GC.Collect();
        }

        internal static IEnumerable<CustomFileInfo> SearchUserCredsFiles()
        {
            var patterns = new List<string>
            {
                ".*credential.*",
                ".*password.*"
            };

            foreach (var file in RootDirUsers)
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

        internal const int MaxMessengerAndRecoveryArtifacts = 20;

        internal static string ClassifyMessengerOrRecoveryFile(CustomFileInfo file)
        {
            if (file == null || file.IsDirectory || string.IsNullOrWhiteSpace(file.FullPath)) return null;
            string path = file.FullPath.Replace('/', '\\');
            string name = file.Filename ?? string.Empty;
            string ext = (file.Extension ?? string.Empty).ToLowerInvariant();
            const string appRoot = "\\AppData\\Roaming\\Output Messenger\\";
            int rootIndex = path.IndexOf(appRoot, StringComparison.OrdinalIgnoreCase);
            if (rootIndex >= 0)
            {
                string appRelative = path.Substring(rootIndex + appRoot.Length);
                int userFolderEnd = appRelative.IndexOf('\\');
                if (userFolderEnd > 0 && userFolderEnd <= 32)
                {
                    string remaining = appRelative.Substring(userFolderEnd + 1);
                    if (remaining.IndexOf('\\') < 0 &&
                        (name.Equals("OM.db3", StringComparison.OrdinalIgnoreCase) ||
                         name.Equals("OT.db3", StringComparison.OrdinalIgnoreCase)))
                        return "messaging client database";
                    if (remaining.StartsWith("Received Files\\", StringComparison.OrdinalIgnoreCase) &&
                        (ext == ".pcap" || ext == ".pcapng" || ext == ".exe" ||
                         ext == ".7z" || ext == ".zip"))
                        return "messaging client received file";
                }
            }
            if ((ext == ".html" || ext == ".txt") &&
                name.IndexOf("bitlocker", StringComparison.OrdinalIgnoreCase) >= 0)
                return "BitLocker recovery export candidate";
            if (name.Equals("MEMORY.7z", StringComparison.OrdinalIgnoreCase))
                return "compressed system memory dump candidate";
            if ((ext == ".7z" || ext == ".zip") &&
                (name.IndexOf("bitlocker", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 name.IndexOf("recovery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 name.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 name.IndexOf("credential", StringComparison.OrdinalIgnoreCase) >= 0))
                return "backup/recovery archive candidate";
            return null;
        }

        internal static List<string> SearchMessengerAndRecoveryArtifacts()
        {
            return SearchMessengerAndRecoveryArtifacts(RootDirCurrentUser, RootDirUsers);
        }

        internal static List<string> SearchMessengerAndRecoveryArtifacts(
            IEnumerable<CustomFileInfo> current, IEnumerable<CustomFileInfo> users)
        {
            var findings = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            current = current ?? Enumerable.Empty<CustomFileInfo>();
            users = users ?? Enumerable.Empty<CustomFileInfo>();
            foreach (CustomFileInfo file in current.Concat(users))
            {
                string kind = ClassifyMessengerOrRecoveryFile(file);
                if (kind == null || !seen.Add(file.FullPath)) continue;
                if (findings.Count == MaxMessengerAndRecoveryArtifacts)
                {
                    findings.Add("[additional matching files omitted]");
                    break;
                }
                findings.Add(file.FullPath + " [" + kind + "; " + file.Size + " bytes; contents not inspected]");
            }
            return findings;
        }

        internal static List<string> SearchUsersInterestingFiles()
        {
            return SearchUsersInterestingFiles(RootDirCurrentUser);
        }

        internal static List<string> SearchUsersInterestingFiles(IEnumerable<CustomFileInfo> files)
        {
            var result = new List<string>();

            foreach (var file in files ?? Enumerable.Empty<CustomFileInfo>())
            {
                if (!file.IsDirectory)
                {
                    string extLower = file.Extension.ToLower();
                    string nameLower = file.Filename.ToLower();

                    if (extLower == ".wim")
                    {
                        // The current-user inventory is already available. Do not search another tree
                        // or open image contents: a WIM filename is only a backup review lead.
                        if (IsBackupWimPath(file.FullPath) && CanOpenLocalBackupWim(file.FullPath))
                            result.Add(file.FullPath + " [readable local WIM backup candidate; contents not inspected]");
                        continue;
                    }

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

        internal static bool IsBackupWimPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string normalized = path.Replace('/', '\\');
            // Reject UNC and relative paths; mapped network drives are rejected at open time.
            if (normalized.Length < 4 || !char.IsLetter(normalized[0]) ||
                normalized[1] != ':' || normalized[2] != '\\') return false;

            string[] parts = normalized.Split('\\');
            string name = parts[parts.Length - 1];
            if (!name.EndsWith(".wim", StringComparison.OrdinalIgnoreCase)) return false;
            string stem = name.Substring(0, name.Length - 4);
            if (stem.Length == 0) return false;
            foreach (string routine in new[] { "install", "boot", "winre", "winpe", "recovery" })
                if (string.Equals(stem, routine, StringComparison.OrdinalIgnoreCase)) return false;

            bool backupDirectory = false;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                string part = parts[i];
                if (string.Equals(part, "Windows", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "Program Files", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "Program Files (x86)", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "Recovery", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "sources", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "$Windows.~BT", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(part, "backup", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "backups", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "image", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "images", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "snapshot", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "snapshots", StringComparison.OrdinalIgnoreCase)) backupDirectory = true;
            }
            return backupDirectory || stem.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                stem.IndexOf("snapshot", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool CanOpenLocalBackupWim(string path)
        {
            try
            {
                if (new DriveInfo(path.Substring(0, 3)).DriveType != DriveType.Fixed) return false;
                using (new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete)) { }
                return true;
            }
            catch (Exception) { return false; }
        }

        internal static List<string> FindCachedGPPPassword()
        {
            var result = new List<string>();

            var allowedExtensions = new HashSet<string>
            {
                ".xml"
            };

            foreach (var file in GroupPolicyHistory)
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
            var programData = GetFilesFast(programDataPath, GlobalPattern);

            var searchFiles = new List<CustomFileInfo>();
            searchFiles.AddRange(ProgramFiles);
            searchFiles.AddRange(ProgramFilesX86);
            searchFiles.AddRange(programData);
            searchFiles.AddRange(DocumentsAndSettings);
            searchFiles.AddRange(RootDirUsers);

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

            foreach (var file in RootDirCurrentUser)
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

            foreach (var file in RootDirUsers)
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
