using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using winPEAS.Checks;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.Helpers.YamlConfig;

namespace winPEAS.Info.ApplicationInfo
{
    // https://www.ghacks.net/2016/06/04/windows-automatic-startup-locations/

    internal static class AutoRuns
    {
        public static List<Dictionary<string, string>> GetAutoRuns(Dictionary<string, string> NtAccountNames)
        {
            var result = new List<Dictionary<string, string>>();
            var regAutoRuns = GetRegistryAutoRuns(NtAccountNames);
            var folderAutoRuns = GetAutoRunsFolder();
            var fileAutoRuns = GetAutoRunsFiles();
            var wmicAutoRuns = GetAutoRunsWMIC();

            result.AddRange(regAutoRuns);
            result.AddRange(folderAutoRuns);
            result.AddRange(fileAutoRuns);
            result.AddRange(wmicAutoRuns);

            return result;
        }

        private static List<List<string>> autorunLocations = new List<List<string>>()
        {
            //Common Autoruns
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\Run"},
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\RunOnce"},
            new List<string> {"HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"},
            new List<string> {"HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce"},
            new List<string> {"HKLM", @"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Run"},
            new List<string> {"HKLM", @"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Runonce"},
            new List<string> {"HKLM", @"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\RunEx"},

            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Run"},
            new List<string> {"HKCU", @"Software\Microsoft\Windows NT\CurrentVersion\Windows\Run"},
            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\RunOnce"},
            new List<string> {"HKCU", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"},
            new List<string> {"HKCU", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce"},

            //Service Autoruns
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\RunService"},
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\RunOnceService"},
            new List<string> {"HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunService"},
            new List<string> {"HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnceService"},
            new List<string> {"HKLM", @"System\CurrentControlSet\Services"},

            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\RunService"},
            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\RunOnceService"},
            new List<string> {"HKCU", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunService"},
            new List<string> {"HKCU", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnceService"},
            
            //Special Autorun
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\RunOnceEx"},
            new List<string> {"HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnceEx"},

            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\RunOnceEx"},
            new List<string> {"HKCU", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnceEx"},

            //RunServices
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\RunServices"},

            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\RunServices"},            

            //RunServicesOnce 
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\RunServicesOnce"},

            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\RunServicesOnce"},            

            //Startup Path
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Startup"},
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Common Startup"},

            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Common Startup"},
            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Startup"},
            

            //Winlogon
            new List<string> {"HKLM", @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon", "Userinit"},    // key = Winlogo, Value = Userinit
            new List<string> {"HKLM", @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon", "Shell"},

            new List<string> {"HKCU", @"Software\Microsoft\Windows NT\CurrentVersion\Windows", "load"},

            //Policy Settings
            new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "Run"}, // key = Explorer, Value = Run

            new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "Run"},

            //AlternateShell in SafeBoot
            new List<string> {"HKLM", @"SYSTEM\CurrentControlSet\Control\SafeBoot", "AlternateShell"},

            //Font Drivers
            new List<string> {"HKLM", @"Software\Microsoft\Windows NT\CurrentVersion\Font Drivers"},
            new List<string> {"HKLM", @"Software\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Font Drivers"},
            new List<string> {"HKLM", @"Software\Microsoft\Windows NT\CurrentVersion\Drivers32"},
            new List<string> {"HKLM", @"Software\Wow6432Node\Microsoft\Windows NT\CurrentVersion\Drivers32"},

            //Open Command
            new List<string> {"HKLM", @"Software\Classes\htmlfile\shell\open\command", ""}, //Get (Default) value with empty string
            new List<string> {"HKLM", @"Software\Wow6432Node\Classes\htmlfile\shell\open\command", ""}, //Get (Default) value with empty string

            // undocumented
            new List<string> { "HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SharedTaskScheduler"},
            new List<string> { "HKLM", @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\SharedTaskScheduler"},

            // Misc Startup keys
            new List<string> { "HKLM", @"System\CurrentControlSet\Control\Session Manager\KnownDlls" },
            //new List<string> { "HKCU", @"Control Panel\Desktop\scrnsave.exe" }, ???
        };

        private static List<List<string>> autorunLocationsKeys = new List<List<string>>
        {
            //Installed Components
            new List<string> { "HKLM", @"Software\Microsoft\Active Setup\Installed Components", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Microsoft\Active Setup\Installed Components", "StubPath"},

            new List<string> { "HKCU", @"Software\Microsoft\Active Setup\Installed Components", "StubPath"},
            new List<string> { "HKCU", @"Software\Wow6432Node\Microsoft\Active Setup\Installed Components", "StubPath"},

            // Shell related autostart entries, e.g. items displayed when you right-click on files or folders.
            new List<string> { "HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ShellServiceObjects", "StubPath"},
            new List<string> { "HKLM", @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellServiceObjects", "StubPath"},
            new List<string> { "HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad", "StubPath"},
            new List<string> { "HKLM", @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad", "StubPath"},
            new List<string> { "HKCU", @"Software\Classes\*\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\*\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKCU", @"Software\Classes\Drive\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\Drive\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\*\ShellEx\PropertySheetHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\*\ShellEx\PropertySheetHandlers", "StubPath"},
            new List<string> { "HKCU", @"Software\Classes\Directory\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\Directory\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\Directory\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKCU", @"Software\Classes\Directory\Shellex\DragDropHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\Directory\Shellex\DragDropHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\Directory\Shellex\DragDropHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\Directory\Shellex\CopyHookHandlers", "StubPath"},
            new List<string> { "HKCU", @"Software\Classes\Directory\Background\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\Directory\Background\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\Directory\Background\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\Folder\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\Folder\ShellEx\ContextMenuHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\Folder\ShellEx\DragDropHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\Folder\ShellEx\DragDropHandlers", "StubPath"},
            new List<string> { "HKLM", @"Software\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers", "StubPath"},

            // Misc Startup keys
            new List<string> { "HKLM", @"Software\Classes\Filter", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\CLSID\{083863F1-70DE-11d0-BD40-00A0C911CE86}\Instance", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\CLSID\{083863F1-70DE-11d0-BD40-00A0C911CE86}\Instance", "StubPath"},
            new List<string> { "HKLM", @"Software\Classes\CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}\Instance", "StubPath"},
            new List<string> { "HKLM", @"Software\Wow6432Node\Classes\CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}\Instance", "StubPath"},
            new List<string> { "HKLM", @"System\CurrentControlSet\Services\WinSock2\Parameters\Protocol_Catalog9\Catalog_Entries", "StubPath"},
            new List<string> { "HKLM", @"System\CurrentControlSet\Services\WinSock2\Parameters\Protocol_Catalog9\Catalog_Entries64", "StubPath"},
        };


        //This registry expect subkeys with the CLSID name
        private static List<List<string>> autorunLocationsKeysCLSIDs = new List<List<string>>
        {
            //Browser Helper Objects
            new List<string> { "HKLM", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects" },
            new List<string> { "HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects" },

            //Internet Explorer Extensions
            new List<string> { "HKLM", @"Software\Microsoft\Internet Explorer\Extensions" },
            new List<string> { "HKLM", @"Software\Wow6432Node\Microsoft\Internet Explorer\Extensions" },
        };

        //////////////////////////////////////
        ///////  Get Autorun Registry ////////
        //////////////////////////////////////
        /// Find Autorun registry where you have write or equivalent access
        private static IEnumerable<Dictionary<string, string>> GetRegistryAutoRuns(Dictionary<string, string> NtAccountNames)
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                //Add the keyvalues inside autorunLocationsKeys to autorunLocations
                foreach (List<string> autorunLocationKey in autorunLocationsKeys)
                {
                    List<string> subkeys = RegistryHelper.GetRegSubkeys(autorunLocationKey[0], autorunLocationKey[1]).ToList();
                    foreach (string keyname in subkeys)
                    {
                        string clsid_name = keyname;
                        Match clsid = Regex.Match(keyname, @"^\W*(\{[\w\-]+\})\W*");
                        if (clsid.Groups.Count > 1) //Sometime the CLSID is bad writting and this kind of fix common mistakes
                        {
                            clsid_name = clsid.Groups[1].ToString();
                        }

                        autorunLocations.Add(autorunLocationKey.Count > 2
                            ? new List<string>
                            {
                                autorunLocationKey[0], autorunLocationKey[1] + "\\" + clsid_name, autorunLocationKey[2]
                            }
                            : new List<string> { autorunLocationKey[0], autorunLocationKey[1] + "\\" + clsid_name });
                    }
                }

                //Read registry and get values
                foreach (List<string> autorunLocation in autorunLocations)
                {
                    Dictionary<string, object> settings = RegistryHelper.GetRegValues(autorunLocation[0], autorunLocation[1]);
                    if ((settings != null) && (settings.Count != 0))
                    {
                        foreach (KeyValuePair<string, object> kvp in settings)
                        {
                            RegistryKey key = null;
                            if ("HKLM" == autorunLocation[0])
                            {
                                key = Registry.LocalMachine.OpenSubKey(autorunLocation[1]);
                            }
                            else
                            {
                                key = Registry.CurrentUser.OpenSubKey(autorunLocation[1]);
                            }

                            if (autorunLocation.Count > 2 && kvp.Key != autorunLocation[2])
                            {
                                continue; //If only interested on 1 key of the registry and it's that one, continue
                            }

                            string orig_filepath = Environment.ExpandEnvironmentVariables(string.Format("{0}", kvp.Value));
                            string filepath = orig_filepath;

                            if (MyUtils.GetExecutableFromPath(Environment.ExpandEnvironmentVariables(string.Format("{0}", kvp.Value))).Length > 0)
                            {
                                filepath = MyUtils.GetExecutableFromPath(filepath);
                            }

                            string filepath_cleaned = filepath.Replace("'", "").Replace("\"", "");
                            string folder = Path.GetDirectoryName(filepath_cleaned);

                            try
                            {
                                //If the path doesn't exist, pass
                                if (File.GetAttributes(filepath_cleaned).HasFlag(FileAttributes.Directory))
                                {
                                    //If the path is already a folder, change the values of the params
                                    orig_filepath = "";
                                    folder = filepath_cleaned;
                                }
                            }
                            catch
                            {
                            }

                            var injectablePaths = new List<string>();
                            var isUnquotedSpaced = MyUtils.CheckQuoteAndSpaceWithPermissions(filepath, out injectablePaths);

                            results.Add(new Dictionary<string, string>()
                            {
                                {"Reg", autorunLocation[0] + "\\" + autorunLocation[1]},
                                {"RegKey", kvp.Key},
                                {"Folder", folder},
                                {"File", orig_filepath},
                                {
                                    "RegPermissions",
                                    string.Join(", ", PermissionsHelper.GetMyPermissionsR(key, Checks.Checks.CurrentUserSiDs))
                                },
                                {
                                    "interestingFolderRights",
                                    string.Join(", ", PermissionsHelper.GetPermissionsFolder(folder, Checks.Checks.CurrentUserSiDs))
                                },
                                {
                                    "interestingFileRights",
                                    orig_filepath.Length > 1 ? string.Join(", ", PermissionsHelper.GetPermissionsFile(orig_filepath, Checks.Checks.CurrentUserSiDs)) : ""
                                },
                                {"isUnquotedSpaced", isUnquotedSpaced ? string.Join(",", injectablePaths) : "false" }
                            });
                        }
                    }
                }

                //Check the autoruns that depends on CLSIDs
                foreach (List<string> autorunLocation in autorunLocationsKeysCLSIDs)
                {
                    List<string> CLSIDs = RegistryHelper.GetRegSubkeys(autorunLocation[0], autorunLocation[1]).ToList();
                    foreach (string clsid in CLSIDs)
                    {
                        string reg = autorunLocation[1] + "\\" + clsid;
                        RegistryKey key = null;
                        if ("HKLM" == autorunLocation[0])
                            key = Registry.LocalMachine.OpenSubKey(reg);
                        else
                            key = Registry.CurrentUser.OpenSubKey(reg);

                        string orig_filepath = MyUtils.GetCLSIDBinPath(clsid);
                        if (string.IsNullOrEmpty(orig_filepath))
                            continue;
                        orig_filepath = Environment.ExpandEnvironmentVariables(orig_filepath).Replace("'", "").Replace("\"", "");
                        string folder = Path.GetDirectoryName(orig_filepath);

                        var injectablePaths = new List<string>();
                        var isUnquotedSpaced = MyUtils.CheckQuoteAndSpaceWithPermissions(orig_filepath, out injectablePaths);

                        results.Add(new Dictionary<string, string>()
                        {
                            {"Reg", autorunLocation[0] + "\\" + reg},
                            {"RegKey", ""},
                            {"Folder", folder},
                            {"File", orig_filepath},
                            {
                                "RegPermissions",
                                string.Join(", ", PermissionsHelper.GetMyPermissionsR(key , Checks.Checks.CurrentUserSiDs))
                            },
                            {
                                "interestingFolderRights",
                                string.Join(", ", PermissionsHelper.GetPermissionsFolder(folder, Checks.Checks.CurrentUserSiDs))
                            },
                            {
                                "interestingFileRights",
                                orig_filepath.Length > 1 ? string.Join(", ", PermissionsHelper.GetPermissionsFile(orig_filepath, Checks.Checks.CurrentUserSiDs)) : ""
                            },
                            {"isUnquotedSpaced", isUnquotedSpaced ? string.Join(",", injectablePaths) : "false" }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        private static IEnumerable<Dictionary<string, string>> GetAutoRunsFolder()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            var autorunLocations = new List<string>
            {
                Environment.ExpandEnvironmentVariables(@"%programdata%\Microsoft\Windows\Start Menu\Programs\Startup"),
            };

            string usersPath = Path.Combine(Environment.GetEnvironmentVariable(@"USERPROFILE"));
            usersPath = Directory.GetParent(usersPath).FullName;

            var config = YamlConfigHelper.GetWindowsSearchConfig();
            var pwdInsideHistory = config.variables.FirstOrDefault(v => v.name.Equals("pwd_inside_history", StringComparison.InvariantCultureIgnoreCase)).value;
            // add .* around each element to match the whole line
            var items = pwdInsideHistory.Split('|').Select(v => $".*{v}.*");
            pwdInsideHistory = string.Join("|", items);

            try
            {
                if (Directory.Exists(usersPath))
                {
                    var userDirs = Directory.EnumerateDirectories(usersPath);

                    foreach (var userDir in userDirs)
                    {
                        string startupPath = $@"{userDir}\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup";

                        if (Directory.Exists(startupPath))
                        {
                            autorunLocations.Add(startupPath);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            foreach (string path in autorunLocations)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        var files = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);

                        foreach (string filepath in files)
                        {
                            var fileContent = File.ReadAllText(filepath);
                            var sensitiveInfoList = FileAnalysis.SearchContent(fileContent, pwdInsideHistory, false);
                            // remove all non-printable and control characters
                            sensitiveInfoList = sensitiveInfoList.Select(s => s = Regex.Replace(s, @"\p{C}+", string.Empty)).ToList();

                            var injectablePaths = new List<string>();
                            var isUnquotedSpaced = MyUtils.CheckQuoteAndSpaceWithPermissions(filepath, out injectablePaths);

                            string folder = Path.GetDirectoryName(filepath);
                            results.Add(new Dictionary<string, string>() {
                                { "Reg", "" },
                                { "RegKey", "" },
                                { "RegPermissions", "" },
                                { "Folder", folder },
                                { "File", filepath },
                                { "isWritableReg", ""},
                                { "interestingFolderRights", string.Join(", ", PermissionsHelper.GetPermissionsFolder(folder, Checks.Checks.CurrentUserSiDs))},
                                { "interestingFileRights", string.Join(", ", PermissionsHelper.GetPermissionsFile(filepath, Checks.Checks.CurrentUserSiDs))},
                                {"isUnquotedSpaced", isUnquotedSpaced ? string.Join(",", injectablePaths) : "false" },
                                { "sensitiveInfoList", string.Join(", ", sensitiveInfoList) },
                            });
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            var taskAutorunLocations = new HashSet<string>()
            {
                $"{systemDrive}\\windows\\tasks",
                $"{systemDrive}\\windows\\system32\\tasks",
            };

            foreach (string folder in taskAutorunLocations)
            {
                try
                {
                    var injectablePaths = new List<string>();
                    var isUnquotedSpaced = MyUtils.CheckQuoteAndSpaceWithPermissions(folder, out injectablePaths);

                    results.Add(new Dictionary<string, string>() {
                        { "Reg", "" },
                        { "RegKey", "" },
                        { "RegPermissions", "" },
                        { "Folder", folder },
                        { "File", "" },
                        { "isWritableReg", ""},
                        { "interestingFolderRights", string.Join(", ", PermissionsHelper.GetPermissionsFolder(folder, Checks.Checks.CurrentUserSiDs))},
                        { "interestingFileRights", ""},
                        {"isUnquotedSpaced", isUnquotedSpaced ? string.Join(",", injectablePaths) : "false" }
                    });
                }
                catch (Exception)
                {
                }
            }

            return results;
        }

        private static IEnumerable<Dictionary<string, string>> GetAutoRunsWMIC()
        {
            var results = new List<Dictionary<string, string>>();
            try
            {
                SelectQuery query = new SelectQuery("Win32_StartupCommand");

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    using (ManagementObjectCollection win32_startup = searcher.Get())
                    {
                        foreach (ManagementObject startup in win32_startup)
                        {
                            string command = startup["command"].ToString();
                            command = Environment.ExpandEnvironmentVariables(string.Format("{0}", command));
                            string filepath = MyUtils.GetExecutableFromPath(command);

                            if (!string.IsNullOrEmpty(filepath))
                            {
                                string filepathCleaned = filepath.Replace("'", "").Replace("\"", "");

                                try
                                {
                                    string folder = Path.GetDirectoryName(filepathCleaned);
                                    var injectablePaths = new List<string>();
                                    var isUnquotedSpaced = MyUtils.CheckQuoteAndSpaceWithPermissions(command, out injectablePaths);

                                    results.Add(new Dictionary<string, string>()
                                {
                                    {"Reg", ""},
                                    {"RegKey", "From WMIC"},
                                    {"RegPermissions", ""},
                                    {"Folder", folder},
                                    {"File", command},
                                    {"isWritableReg", ""},
                                    {
                                        "interestingFolderRights",
                                        string.Join(", ", PermissionsHelper.GetPermissionsFolder(folder, Checks.Checks.CurrentUserSiDs))
                                    },
                                    {
                                        "interestingFileRights",
                                        string.Join(", ", PermissionsHelper.GetPermissionsFile(filepath, Checks.Checks.CurrentUserSiDs))
                                    },
                                    {"isUnquotedSpaced", isUnquotedSpaced ? string.Join(",", injectablePaths) : "false" }
                                });
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Beaprint.GrayPrint("Error getting autoruns from WMIC: " + e);
            }
            return results;
        }

        private static IEnumerable<Dictionary<string, string>> GetAutoRunsFiles()
        {
            var results = new List<Dictionary<string, string>>();
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            var autostartFiles = new HashSet<string>
            {
                $"{systemDrive}\\autoexec.bat",
                $"{systemDrive}\\config.sys",
                $"{systemDrive}\\windows\\winstart.bat",
                $"{systemDrive}\\windows\\wininit.ini",
                $"{systemDrive}\\windows\\dosstart.bat",
                $"{systemDrive}\\windows\\system.ini",
                $"{systemDrive}\\windows\\win.ini",
                $"{systemDrive}\\windows\\system\\autoexec.nt",
                $"{systemDrive}\\windows\\system\\config.nt"
            };

            foreach (string path in autostartFiles)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        string folder = Path.GetDirectoryName(path);
                        var injectablePaths = new List<string>();
                        var isUnquotedSpaced = MyUtils.CheckQuoteAndSpaceWithPermissions(path, out injectablePaths);

                        results.Add(new Dictionary<string, string>
                        {
                            { "Reg", "" },
                            { "RegKey", "" },
                            { "RegPermissions", "" },
                            { "Folder", folder },
                            { "File", path },
                            { "isWritableReg", ""},
                            { "interestingFolderRights", string.Join(", ", PermissionsHelper.GetPermissionsFolder(folder, Checks.Checks.CurrentUserSiDs))},
                            { "interestingFileRights", string.Join(", ", PermissionsHelper.GetPermissionsFile(path, Checks.Checks.CurrentUserSiDs))},
                            {"isUnquotedSpaced", isUnquotedSpaced ? string.Join(",", injectablePaths) : "false" }
                        });
                    }
                }
                catch (Exception)
                {
                }
            }

            return results;
        }
    }
}
