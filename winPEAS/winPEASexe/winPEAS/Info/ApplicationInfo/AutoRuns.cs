using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using winPEAS.Helpers;

namespace winPEAS.Info.ApplicationInfo
{
    internal static class AutoRuns
    {
        public static List<Dictionary<string, string>> GetAutoRuns(Dictionary<string, string> NtAccountNames)
        {
            var result = new List<Dictionary<string, string>>();
            var regAutoRuns = GetRegistryAutoRuns(NtAccountNames);
            var folderAutoRuns = GetAutoRunsFolder();
            //var fileAutoRuns = GetAutoRunsFiles();
            var wmicAutoRuns = GetAutoRunsWMIC();

            result.AddRange(regAutoRuns);
            result.AddRange(folderAutoRuns);
            //result.AddRange(fileAutoRuns);
            result.AddRange(wmicAutoRuns);

            return result;
        }

        //////////////////////////////////////
        ///////  Get Autorun Registry ////////
        //////////////////////////////////////
        /// Find Autorun registry where you have write or equivalent access
        private static IEnumerable<Dictionary<string, string>> GetRegistryAutoRuns(Dictionary<string, string> NtAccountNames)
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                List<List<string>> autorunLocations = new List<List<string>>()
                {
                    //Common Autoruns
                    new List<string> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<string> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<string> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<string> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<string> {"HKCU","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<string> {"HKCU","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<string> {"HKCU","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<string> {"HKCU","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<string> {"HKLM",@"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Run"},
                    new List<string> {"HKLM",@"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Runonce"},
                    new List<string> {"HKLM",@"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\RunEx"},

                    //Service Autoruns
                    new List<string> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<string> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    new List<string> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<string> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    new List<string> {"HKCU", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<string> {"HKCU", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    new List<string> {"HKCU", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<string> {"HKCU", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    
                    //Special Autorun
                    new List<string> {"HKLM","Software\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},
                    new List<string> {"HKLM","Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},
                    new List<string> {"HKCU","Software\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},
                    new List<string> {"HKCU","Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},

                    //RunServicesOnce 
                    new List<string> {"HKCU","Software\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"},
                    new List<string> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunServicesOnce"},

                    //Startup Path
                    new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Common Startup"},
                    new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Startup"},
                    new List<string> {"HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Startup"},
                    new List<string> {"HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Common Startup"},

                    //Winlogon
                    new List<string> {"HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "Userinit"},
                    new List<string> {"HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "Shell"},

                    new List<string> { "HKCU", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", "load"},

                    //Policy Settings
                    new List<string> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "Run"},
                    new List<string> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "Run"},

                    //AlternateShell in SafeBoot
                    new List<string> {"HKLM","SYSTEM\\CurrentControlSet\\Control\\SafeBoot", "AlternateShell"},

                    //Font Drivers
                    new List<string> {"HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Font Drivers"},
                    new List<string> {"HKLM", @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Font Drivers"},

                    //Open Command
                    new List<string> {"HKLM", @"SOFTWARE\Classes\htmlfile\shell\open\command", ""}, //Get (Default) value with empty string
                    new List<string> {"HKLM", @"SOFTWARE\Wow6432Node\Classes\htmlfile\shell\open\command", ""}, //Get (Default) value with empty string

                };

                List<List<string>> autorunLocationsKeys = new List<List<string>>
                {
                    //Installed Components
                    new List<string> { "HKLM","SOFTWARE\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                    new List<string> { "HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                    new List<string> { "HKCU","SOFTWARE\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                    new List<string> { "HKCU","SOFTWARE\\Wow6432Node\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                };


                //This registry expect subkeys with the CLSID name
                List<List<string>> autorunLocationsKeysCLSIDs = new List<List<string>>
                {
                    //Browser Helper Objects
                    new List<string> { "HKLM", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects" },
                    new List<string> { "HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects" },

                    //Internet Explorer Extensions
                    new List<string> { "HKLM", @"Software\Microsoft\Internet Explorer\Extensions" },
                    new List<string> { "HKLM", @"Software\Wow6432Node\Microsoft\Internet Explorer\Extensions" },
                };

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
                            : new List<string> {autorunLocationKey[0], autorunLocationKey[1] + "\\" + clsid_name});
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
                                key = Registry.LocalMachine.OpenSubKey(autorunLocation[1]);
                            else
                                key = Registry.CurrentUser.OpenSubKey(autorunLocation[1]);


                            if (autorunLocation.Count > 2 && kvp.Key != autorunLocation[2])
                                continue; //If only interested on 1 key of the registry and it's that one, continue

                            string orig_filepath = Environment.ExpandEnvironmentVariables(string.Format("{0}", kvp.Value));
                            string filepath = orig_filepath;
                            if (MyUtils.GetExecutableFromPath(Environment.ExpandEnvironmentVariables(string.Format("{0}", kvp.Value))).Length > 0)
                                filepath = MyUtils.GetExecutableFromPath(filepath);
                            string filepath_cleaned = filepath.Replace("'", "").Replace("\"", "");

                            string folder = System.IO.Path.GetDirectoryName(filepath_cleaned);
                            try
                            { //If the path doesn't exist, pass
                                if (File.GetAttributes(filepath_cleaned).HasFlag(FileAttributes.Directory))
                                { //If the path is already a folder, change the values of the params
                                    orig_filepath = "";
                                    folder = filepath_cleaned;
                                }
                            }
                            catch
                            {
                            }

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
                                {"isUnquotedSpaced", MyUtils.CheckQuoteAndSpace(filepath).ToString()}
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
                        string folder = System.IO.Path.GetDirectoryName(orig_filepath);

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
                            {"isUnquotedSpaced", MyUtils.CheckQuoteAndSpace(orig_filepath).ToString()}
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
            List<string> autorunLocations = new List<string>();
            // displays startup for current user
            //autorunLocations.Add(Environment.ExpandEnvironmentVariables(@"%appdata%\Microsoft\Windows\Start Menu\Programs\Startup"));
            autorunLocations.Add(Environment.ExpandEnvironmentVariables(@"%programdata%\Microsoft\Windows\Start Menu\Programs\Startup"));

            //string usersPath = Environment.GetEnvironmentVariable("USERPROFILE") + "\\..\\";
            string usersPath = Path.Combine(Environment.GetEnvironmentVariable(@"USERPROFILE"));
            usersPath = Directory.GetParent(usersPath).FullName;
            try
            {
                var userDirs = Directory.GetDirectories(usersPath);

                foreach (var userDir in userDirs)
                {
                    string startupPath = $@"{userDir}\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup";

                    if (Directory.Exists(startupPath))
                    {
                        autorunLocations.Add(startupPath);
                    }
                }
            }
            catch (Exception e)
            {
            }

            foreach (string path in autorunLocations)
            {
                foreach (string filepath in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
                {
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
                            { "isUnquotedSpaced", "" }
                    });
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
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection win32_startup = searcher.Get();
                foreach (ManagementObject startup in win32_startup)
                {
                    string command = startup["command"].ToString();
                    command = Environment.ExpandEnvironmentVariables(string.Format("{0}", command));
                    string filepath = MyUtils.GetExecutableFromPath(command);
                    string filepath_cleaned = filepath.Replace("'", "").Replace("\"", "");
                    string folder = System.IO.Path.GetDirectoryName(filepath_cleaned);
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
                        {"isUnquotedSpaced", MyUtils.CheckQuoteAndSpace(command).ToString()}
                    });
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
            throw new NotImplementedException();
        }
    }
}
