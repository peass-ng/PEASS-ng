using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace winPEAS.Helpers.AppLocker
{
    internal static class AppLockerHelper
    {
        private static readonly HashSet<string> _appLockerByPassDirectoriesSet = new HashSet<string>
        {
            @"C:\Windows\Temp",
            @"C:\Windows\System32\spool\drivers\color",
            @"C:\Windows\Tasks",
            @"C:\windows\tracing",
            @"C:\Windows\Registration\CRMLog",
            @"C:\Windows\System32\FxsTmp",
            @"C:\Windows\System32\com\dmp",
            @"C:\Windows\System32\Microsoft\Crypto\RSA\MachineKeys",
            @"C:\Windows\System32\spool\PRINTERS",
            @"C:\Windows\System32\spool\SERVERS",
            @"C:\Windows\System32\Tasks\Microsoft\Windows\SyncCenter",
            @"C:\Windows\System32\Tasks_Migrated",
            @"C:\Windows\SysWOW64\FxsTmp",
            @"C:\Windows\SysWOW64\com\dmp",
            @"C:\Windows\SysWOW64\Tasks\Microsoft\Windows\SyncCenter",
            @"C:\Windows\SysWOW64\Tasks\Microsoft\Windows\PLA\System",
        };

        // https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-defender-application-control/applocker/working-with-applocker-rules
        private static readonly Dictionary<string, HashSet<string>> _appLockerFileExtensionsByType = new Dictionary<string, HashSet<string>>()
        {
            { "appx", new HashSet<string> { ".appx" } },
            { "dll", new HashSet<string> { ".dll", ".ocx" } },
            { "exe", new HashSet<string> { ".exe", ".com" } },
            { "msi", new HashSet<string> { ".msi", ".msp", ".mst" } },
            { "script", new HashSet<string> { ".ps1", ".bat", ".cmd", ".vbs", ".js"} },
        };

        private static Dictionary<string, HashSet<string>> _appLockerByPassDirectoriesByPath = null;
        private const int FolderCheckMaxDepth = 3;

        public static void CreateLists()
        {
            if (_appLockerByPassDirectoriesByPath != null) return;

            _appLockerByPassDirectoriesByPath = new Dictionary<string, HashSet<string>>();

            foreach (var appLockerByPassDirectory in _appLockerByPassDirectoriesSet)
            {
                string directoryLower = appLockerByPassDirectory.ToLower();
                string currentDirectory = Directory.GetParent(directoryLower)?.FullName;

                while (!string.IsNullOrEmpty(currentDirectory))
                {
                    if (!_appLockerByPassDirectoriesByPath.ContainsKey(currentDirectory))
                    {
                        _appLockerByPassDirectoriesByPath[currentDirectory] = new HashSet<string>();
                    }

                    if (!_appLockerByPassDirectoriesByPath[currentDirectory].Contains(appLockerByPassDirectory))
                    {
                        _appLockerByPassDirectoriesByPath[currentDirectory].Add(appLockerByPassDirectory);
                    }

                    currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
                }
            }
        }

        public static void PrintAppLockerPolicy()
        {
            Beaprint.MainPrint("Checking AppLocker effective policy");

            try
            {
                string[] ruleTypes = { "All" };
                var appLockerSettings = SharpAppLocker.GetAppLockerPolicy(SharpAppLocker.PolicyType.Effective, ruleTypes, string.Empty, false, false);

                Beaprint.NoColorPrint($"   AppLockerPolicy version: {appLockerSettings.Version}\n   listing rules:\n\n");

                if (appLockerSettings.RuleCollection != null)
                {
                    foreach (var rule in appLockerSettings.RuleCollection)
                    {
                        PrintFileHashRules(rule);
                        PrintFilePathRules(rule);
                        PrintFilePublisherRules(rule);
                    }
                }
            }
            catch (COMException)
            {
                Beaprint.ColorPrint("     AppLocker unsupported on this Windows version.", Beaprint.ansi_color_yellow);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintFilePublisherRules(AppLockerPolicyRuleCollection rule)
        {
            if (rule?.FilePublisherRule == null) return;

            foreach (var filePublisherRule in rule.FilePublisherRule)
            {
                Beaprint.GoodPrint($"   File Publisher Rule\n");

                Beaprint.NoColorPrint($"   Rule Type:               {rule.Type}\n" +
                                      $"   Enforcement Mode:        {rule.EnforcementMode}\n" +
                                      $"   Name:                    {filePublisherRule.Name}\n" +
                                      $"   Description:             {filePublisherRule.Description}\n" +
                                      $"   Action:                  {filePublisherRule.Action}");

                var color = GetColorBySid(filePublisherRule.UserOrGroupSid);

                Beaprint.ColorPrint($"   User Or Group Sid:       {filePublisherRule.UserOrGroupSid}\n", color);

                Beaprint.GoodPrint($"   Conditions");

                foreach (var condition in filePublisherRule.Conditions)
                {
                    Beaprint.NoColorPrint(
                                      $"   Binary Name:             {condition.BinaryName}\n" +
                                      $"   Binary Version Range:    ({condition.BinaryVersionRange.LowSection} - {condition.BinaryVersionRange.HighSection})\n" +
                                      $"   Product Name:            {condition.ProductName}\n" +
                                      $"   Publisher Name:          {condition.PublisherName}\n");
                }

                Beaprint.PrintLineSeparator();
            }
        }

        private static void PrintFilePathRules(AppLockerPolicyRuleCollection rule)
        {
            if (rule?.FilePathRule == null) return;

            foreach (var filePathRule in rule.FilePathRule)
            {
                Beaprint.GoodPrint($"   File Path Rule\n");

                var normalizedName = NormalizePath(filePathRule.Name);


                Beaprint.NoColorPrint($"   Rule Type:               {rule.Type}\n" +
                                      $"   Enforcement Mode:        {rule.EnforcementMode}\n" +
                                      $"   Name:                    {filePathRule.Name}\n" +
                                      $"   Translated Name:         {normalizedName}\n" +
                                      $"   Description:             {filePathRule.Description}\n" +
                                      $"   Action:                  {filePathRule.Action}");

                var color = GetColorBySid(filePathRule.UserOrGroupSid);

                Beaprint.ColorPrint($"   User Or Group Sid:       {filePathRule.UserOrGroupSid}\n", color);

                Beaprint.GoodPrint($"   Conditions");

                foreach (var condition in filePathRule.Conditions)
                {
                    // print wildcards as red and continue
                    if (condition.Path == "*" || condition.Path == "*.*")
                    {
                        Beaprint.ColorPrint(
                                      $"   Path:                    {condition.Path}", Beaprint.ansi_color_bad);

                        continue;
                    }

                    Beaprint.NoColorPrint(
                                      $"   Path:                    {condition.Path}");


                    // TODO
                    // cache permissions in a dictionary

                    var normalizedPath = NormalizePath(condition.Path);

                    // it's a file rule
                    if (IsFilePath(normalizedPath))
                    {
                        // TODO
                        // load permissions from cache

                        // check file
                        CheckFileWriteAccess(normalizedPath);

                        // check directories
                        string directory = Path.GetDirectoryName(normalizedPath);

                        CheckDirectoryAndParentsWriteAccess(directory);
                    }

                    // it's a directory rule
                    else
                    {
                        // TODO
                        // load permissions from cache


                        // does the directory exists?
                        if (Directory.Exists(normalizedPath))
                        {
                            // can we write to the directory ?
                            var folderPermissions = PermissionsHelper.GetPermissionsFolder(normalizedPath, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);

                            // we can write 
                            if (folderPermissions.Count > 0)
                            {
                                Beaprint.BadPrint($"    Directory \"{normalizedPath}\" Permissions: " + string.Join(",", folderPermissions));
                            }
                            // we cannot write to the folder
                            else
                            {
                                // first check well known AppLocker bypass locations
                                if (_appLockerByPassDirectoriesByPath.ContainsKey(normalizedPath))
                                {
                                    // iterate over applocker bypass directories and check them
                                    foreach (var subfolders in _appLockerByPassDirectoriesByPath[normalizedPath])
                                    {
                                        var subfolderPermissions = PermissionsHelper.GetPermissionsFolder(subfolders, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);

                                        // we can write 
                                        if (subfolderPermissions.Count > 0)
                                        {
                                            Beaprint.BadPrint($"    Directory \"{subfolders}\" Permissions: " + string.Join(",", subfolderPermissions));
                                            break;
                                        }
                                    }
                                }
                                // the well-known bypass location does not contain the folder
                                // check file / subfolder write permissions
                                else
                                {
                                    // start with the current directory
                                    bool isFileOrSubfolderWriteAccess = CheckFilesAndSubfolders(normalizedPath, rule.Type, 0);

                                    if (!isFileOrSubfolderWriteAccess)
                                    {
                                        Beaprint.ColorPrint($"    No potential bypass found while recursively checking files/subfolders " +
                                                                    $"for write or equivalent permissions with depth: {FolderCheckMaxDepth}\n" +
                                                                    $"    Check permissions manually.", Beaprint.YELLOW);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // do we have write access recursively for the parent folder(s)?
                            CheckDirectoryAndParentsWriteAccess(normalizedPath);
                        }

                        // TODO
                        // save to cache for faster next search
                    }

                    Beaprint.GoodPrint("");
                }

                Beaprint.PrintLineSeparator();
            }
        }

        private static void PrintFileHashRules(AppLockerPolicyRuleCollection rule)
        {
            if (rule?.FileHashRule == null) return;

            foreach (var fileHashRule in rule.FileHashRule)
            {
                Beaprint.GoodPrint($"   File Hash Rule\n");

                Beaprint.NoColorPrint(
                                   $"   Rule Type:               {rule.Type}\n" +
                                   $"   Enforcement Mode:        {rule.EnforcementMode}\n" +
                                   $"   Name:                    {fileHashRule.Name}\n" +
                                   $"   Description:             {fileHashRule.Description}\n" +
                                   $"   Action:                  {fileHashRule.Action}");

                var color = GetColorBySid(fileHashRule.UserOrGroupSid);

                Beaprint.ColorPrint(
                                   $"   User Or Group Sid:       {fileHashRule.UserOrGroupSid}\n", color);

                Beaprint.GoodPrint($"   Conditions");

                foreach (var condition in fileHashRule.Conditions)
                {
                    Beaprint.NoColorPrint(
                                   $"   Source File Name:        {condition.FileHash.SourceFileName}\n" +
                                   $"   Data:                    {condition.FileHash.Data}\n" +
                                   $"   Source File Length:      {condition.FileHash.SourceFileLength}\n" +
                                   $"   Type:                    {condition.FileHash.Type}\n");
                }

                Beaprint.PrintLineSeparator();
            }
        }

        private static string GetColorBySid(string sid)
        {
            var color = Checks.Checks.CurrentUserSiDs.ContainsKey(sid)
                ? Beaprint.ansi_color_bad
                : Beaprint.ansi_color_good;

            return color;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            path = path.Replace("%OSDRIVE%", systemDrive);
            path = path.Replace("*", string.Empty);
            path = path.TrimEnd('\\');

            path = Environment.ExpandEnvironmentVariables(path);
            path = path.ToLower();

            return path;
        }

        private static bool CheckFilesAndSubfolders(string path, string ruleType, int depth)
        {
            if (string.IsNullOrWhiteSpace(ruleType)) throw new ArgumentNullException(nameof(ruleType));
            if (depth == FolderCheckMaxDepth) return false;

            try
            {
                if (Directory.Exists(path))
                {
                    var subfolders = Directory.EnumerateDirectories(path);
                    var files = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);

                    ruleType = ruleType.ToLower();

                    if (!_appLockerFileExtensionsByType.ContainsKey(ruleType))
                    {
                        throw new ArgumentException(nameof(ruleType));
                    }

                    var filteredFiles =
                        (from file in files
                         let extension = Path.GetExtension(file)?.ToLower() ?? string.Empty
                         where _appLockerFileExtensionsByType[ruleType].Contains(extension)
                         select file).ToList();

                    // first check write access for files
                    if (filteredFiles.Any(CheckFileWriteAccess))
                    {
                        return true;
                    }

                    // if we have not found any writable file, 
                    // check subfolders for write access
                    if (subfolders.Any(subfolder => CheckDirectoryWriteAccess(subfolder, out bool _, isGoodPrint: false)))
                    {
                        return true;
                    }

                    // check recursively all the subfolders for files/sub-subfolders                     
                    if (subfolders.Any(subfolder => CheckFilesAndSubfolders(subfolder, ruleType, depth + 1)))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        private static bool CheckFileWriteAccess(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            if (File.Exists(path))
            {
                var filePermissions = PermissionsHelper.GetPermissionsFile(path, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);

                if (filePermissions.Count > 0)
                {
                    Beaprint.BadPrint($"    File \"{path}\" Permissions: " + string.Join(",", filePermissions));

                    return true;
                }
            }
            else
            {
                Beaprint.BadPrint($"    File \"{path}\" does not exist.");
            }

            return false;
        }

        private static bool CheckDirectoryAndParentsWriteAccess(string directory)
        {
            while (!string.IsNullOrEmpty(directory))
            {
                // first check if we have write permission on the directory
                if (CheckDirectoryWriteAccess(directory, out var isDirectoryExisting))
                {
                    return true;
                }

                // the directory exists and we don't have write permissions
                // we can return false;
                if (isDirectoryExisting)
                {
                    return false;
                }

                // if the current folder does not exists, check it's parent directory recursively
                directory = Directory.GetParent(directory)?.FullName;
            }

            return false;
        }

        private static bool CheckDirectoryWriteAccess(string directory, out bool isDirectoryExisting, bool isGoodPrint = true)
        {
            isDirectoryExisting = true;

            if (!Directory.Exists(directory))
            {
                Beaprint.BadPrint($"    Directory \"{directory}\" does not exist.");
                isDirectoryExisting = false;
            }
            else
            {
                var folderPermissions = PermissionsHelper.GetPermissionsFolder(directory, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);

                if (folderPermissions.Count > 0)
                {
                    Beaprint.BadPrint($"    Directory \"{directory}\" Permissions: " + string.Join(",", folderPermissions));
                }
                else
                {
                    if (isGoodPrint)
                    {
                        Beaprint.GoodPrint($"    {directory}");
                    }
                }

                return folderPermissions.Count > 0;
            }

            return false;
        }

        private static bool IsFilePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   !string.IsNullOrWhiteSpace(Path.GetExtension(path));
        }
    }
}
