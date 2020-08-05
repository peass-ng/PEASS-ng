using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;

namespace winPEAS
{
    class ApplicationInfo
    {
        // https://stackoverflow.com/questions/115868/how-do-i-get-the-title-of-the-current-active-window-using-c
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        /*public static List<string> GetAppsRegistry()
        {
            List<string> retList = new List<string>();
            try
            {
                RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
                foreach (string subKeyName in softwareKey.GetSubKeyNames())
                {
                    retList.Add(subKeyName);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: "+ex);
            }
            return retList;
        }*/

        public static SortedDictionary<string, Dictionary<string, string>> GetInstalledAppsPermsPath(string fpath)
        {
            SortedDictionary<string, Dictionary<string, string>> results = new SortedDictionary<string, Dictionary<string, string>>();
            try
            {
                foreach (string f in Directory.GetFiles(fpath))
                {
                    results[f] = new Dictionary<string, string>(){
                            { f, String.Join(", ", MyUtils.GetPermissionsFile(f, Program.currentUserSIDs)) }
                        };
                }
                foreach (string d in Directory.GetDirectories(fpath))
                {
                    results[d] = MyUtils.GetRecursivePrivs(d);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }

        public static SortedDictionary<string, Dictionary<string, string>> GetInstalledAppsPerms()
        {
            //Get from Program Files
            SortedDictionary<string, Dictionary<string, string>> results = GetInstalledAppsPermsPath(@Path.GetPathRoot(Environment.SystemDirectory) + "Program Files");
            SortedDictionary<string, Dictionary<string, string>> results2 = GetInstalledAppsPermsPath(@Path.GetPathRoot(Environment.SystemDirectory) + "Program Files (x86)");
            results.Concat(results2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            //Get from Uninstall
            string[] subkeys = MyUtils.GetRegSubkeys("HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (subkeys != null)
            {
                foreach (string app in subkeys)
                {
                    string installLocation = MyUtils.GetRegValue("HKLM", String.Format(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}", app), "InstallLocation");
                    if (String.IsNullOrEmpty(installLocation))
                        continue;

                    installLocation = installLocation.Replace("\"", "");

                    if (installLocation.EndsWith(@"\"))
                        installLocation = installLocation.Substring(0, installLocation.Length - 1);

                    if (!results.ContainsKey(installLocation) && Directory.Exists(installLocation))
                    {
                        bool already = false;
                        foreach (string path in results.Keys)
                        {
                            if (installLocation.IndexOf(path) != -1) //Check for subfoldres of already found folders
                            { 
                                already = true;
                                break;
                            }
                        }
                        if (!already)
                            results[installLocation] = MyUtils.GetRecursivePrivs(installLocation);
                    }
                }
            }

            subkeys = MyUtils.GetRegSubkeys("HKLM", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            if (subkeys != null)
            {
                foreach (string app in subkeys)
                {
                    string installLocation = MyUtils.GetRegValue("HKLM", String.Format(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{0}", app), "InstallLocation");
                    if (String.IsNullOrEmpty(installLocation))
                        continue;

                    installLocation = installLocation.Replace("\"", "");

                    if (installLocation.EndsWith(@"\"))
                        installLocation = installLocation.Substring(0, installLocation.Length - 1);

                    if (!results.ContainsKey(installLocation) && Directory.Exists(installLocation))
                    {
                        bool already = false;
                        foreach (string path in results.Keys)
                        {
                            if (installLocation.IndexOf(path) != -1) //Check for subfoldres of already found folders
                            {
                                already = true;
                                break;
                            }
                        }
                        if (!already)
                            results[installLocation] = MyUtils.GetRecursivePrivs(installLocation);
                    }
                }
            }

            return results;
        }


        //////////////////////////////////////
        ///////  Get Autorun Registry ////////
        //////////////////////////////////////
        /// Find Autorun registry where you have write or equivalent access
        public static List<Dictionary<string, string>> GetRegistryAutoRuns(Dictionary<string, string> NtAccountNames)
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                List<List<String>> autorunLocations = new List<List<string>>()
                {
                    //Common Autoruns
                    new List<String> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<String> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<String> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<String> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<String> {"HKCU","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<String> {"HKCU","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<String> {"HKCU","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run"},
                    new List<String> {"HKCU","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce"},
                    new List<String> {"HKLM",@"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Run"},
                    new List<String> {"HKLM",@"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\Runonce"},
                    new List<String> {"HKLM",@"Software\Microsoft\Windows NT\CurrentVersion\Terminal Server\Install\Software\Microsoft\Windows\CurrentVersion\RunEx"},

                    //Service Autoruns
                    new List<String> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<String> {"HKLM","SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    new List<String> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<String> {"HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    new List<String> {"HKCU", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<String> {"HKCU", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    new List<String> {"HKCU", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunService"},
                    new List<String> {"HKCU", "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"},
                    
                    //Special Autorun
                    new List<String> {"HKLM","Software\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},
                    new List<String> {"HKLM","Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},
                    new List<String> {"HKCU","Software\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},
                    new List<String> {"HKCU","Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceEx"},

                    //Startup Path
                    new List<String> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Common Startup"},
                    new List<String> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Startup"},
                    new List<String> {"HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Common Startup"},
                    new List<String> {"HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "Common Startup"},

                    //Winlogon
                    new List<String> {"HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "Userinit"},
                    new List<String> {"HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "Shell"},

                    //Policy Settings
                    new List<String> {"HKLM", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "Run"},
                    new List<String> {"HKCU", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "Run"},

                    //AlternateShell in SafeBoot
                    new List<String> {"HKLM","SYSTEM\\CurrentControlSet\\Control\\SafeBoot", "AlternateShell"},

                    //Font Drivers
                    new List<String> {"HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Font Drivers"},
                    new List<String> {"HKLM", @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Font Drivers"},

                    //Open Command
                    new List<String> {"HKLM", @"SOFTWARE\Classes\htmlfile\shell\open\command", ""}, //Get (Default) value with empty string
                    new List<String> {"HKLM", @"SOFTWARE\Wow6432Node\Classes\htmlfile\shell\open\command", ""}, //Get (Default) value with empty string

                };

                List<List<String>> autorunLocationsKeys = new List<List<String>>
                {
                    //Installed Components
                    new List<String> { "HKLM","SOFTWARE\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                    new List<String> { "HKLM","SOFTWARE\\Wow6432Node\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                    new List<String> { "HKCU","SOFTWARE\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                    new List<String> { "HKCU","SOFTWARE\\Wow6432Node\\Microsoft\\Active Setup\\Installed Components", "StubPath"},
                };


                //This registry expect subkeys with the CLSID name
                List<List<String>> autorunLocationsKeysCLSIDs = new List<List<String>>
                {
                    //Browser Helper Objects
                    new List<String> { "HKLM", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects" },
                    new List<String> { "HKLM", @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects" },

                    //Internet Explorer Extensions
                    new List<String> { "HKLM", @"Software\Microsoft\Internet Explorer\Extensions" },
                    new List<String> { "HKLM", @"Software\Wow6432Node\Microsoft\Internet Explorer\Extensions" },
                };

                //Add the keyvalues inside autorunLocationsKeys to autorunLocations
                foreach (List<String> autorunLocationKey in autorunLocationsKeys)
                {
                    List<String> subkeys = MyUtils.GetRegSubkeys(autorunLocationKey[0], autorunLocationKey[1]).ToList();
                    foreach (String keyname in subkeys)
                    {
                        string clsid_name = keyname;
                        Match clsid = Regex.Match(keyname, @"^\W*(\{[\w\-]+\})\W*");
                        if (clsid.Groups.Count > 1) //Sometime the CLSID is bad writting and this kind of fix common mistakes
                            clsid_name = clsid.Groups[1].ToString();

                        if (autorunLocationKey.Count > 2)
                            autorunLocations.Add(new List<string> { autorunLocationKey[0], autorunLocationKey[1] + "\\" + clsid_name, autorunLocationKey[2] });
                        else
                            autorunLocations.Add(new List<string> { autorunLocationKey[0], autorunLocationKey[1] + "\\" + clsid_name });
                    }
                }

                //Read registry and get values
                foreach (List<String> autorunLocation in autorunLocations)
                {
                    Dictionary<string, object> settings = MyUtils.GetRegValues(autorunLocation[0], autorunLocation[1]);
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

                            string orig_filepath = Environment.ExpandEnvironmentVariables(String.Format("{0}", kvp.Value));
                            string filepath = orig_filepath;
                            if (MyUtils.GetExecutableFromPath(Environment.ExpandEnvironmentVariables(String.Format("{0}", kvp.Value))).Length > 0)
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
                                    string.Join(", ", MyUtils.GetMyPermissionsR(key, Program.currentUserSIDs))
                                },
                                {
                                    "interestingFolderRights",
                                    String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.currentUserSIDs))
                                },
                                {
                                    "interestingFileRights",
                                    orig_filepath.Length > 1 ? String.Join(", ", MyUtils.GetPermissionsFile(orig_filepath, Program.currentUserSIDs)) : ""
                                },
                                {"isUnquotedSpaced", MyUtils.CheckQuoteAndSpace(filepath).ToString()}
                            });
                        }
                    }
                }

                //Check the autoruns that depends on CLSIDs
                foreach (List<String> autorunLocation in autorunLocationsKeysCLSIDs)
                {
                    List<String> CLSIDs = MyUtils.GetRegSubkeys(autorunLocation[0], autorunLocation[1]).ToList();
                    foreach (String clsid in CLSIDs)
                    {
                        string reg = autorunLocation[1] + "\\" + clsid;
                        RegistryKey key = null;
                        if ("HKLM" == autorunLocation[0])
                            key = Registry.LocalMachine.OpenSubKey(reg);
                        else
                            key = Registry.CurrentUser.OpenSubKey(reg);

                        string orig_filepath = MyUtils.GetCLSIDBinPath(clsid);
                        if (String.IsNullOrEmpty(orig_filepath))
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
                                string.Join(", ", MyUtils.GetMyPermissionsR(key , Program.currentUserSIDs))
                            },
                            {
                                "interestingFolderRights",
                                String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.currentUserSIDs))
                            },
                            {
                                "interestingFileRights",
                                orig_filepath.Length > 1 ? String.Join(", ", MyUtils.GetPermissionsFile(orig_filepath, Program.currentUserSIDs)) : ""
                            },
                            {"isUnquotedSpaced", MyUtils.CheckQuoteAndSpace(orig_filepath).ToString()}
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetAutoRunsFolder()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            List<string> autorunLocations = new List<string>();
            autorunLocations.Add(Environment.ExpandEnvironmentVariables(@"%appdata%\Microsoft\Windows\Start Menu\Programs\Startup"));
            autorunLocations.Add(Environment.ExpandEnvironmentVariables(@"%programdata%\Microsoft\Windows\Start Menu\Programs\Startup"));

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
                            { "interestingFolderRights", String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.currentUserSIDs))},
                            { "interestingFileRights", String.Join(", ", MyUtils.GetPermissionsFile(filepath, Program.currentUserSIDs))},
                            { "isUnquotedSpaced", "" }
                    });
                }
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetAutoRunsWMIC()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                SelectQuery query = new SelectQuery("Win32_StartupCommand");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection win32_startup = searcher.Get();
                foreach (ManagementObject startup in win32_startup)
                {
                    string command = startup["command"].ToString();
                    command = Environment.ExpandEnvironmentVariables(String.Format("{0}", command));
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
                            String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.currentUserSIDs))
                        },
                        {
                            "interestingFileRights",
                            String.Join(", ", MyUtils.GetPermissionsFile(filepath, Program.currentUserSIDs))
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

        public static List<Dictionary<string, string>> GetAutoRuns(Dictionary<string,string> NtAccountNames)
        {
            List<Dictionary<string, string>> reg_autorus = GetRegistryAutoRuns(NtAccountNames);
            List<Dictionary<string, string>> file_autorus = GetAutoRunsFolder();
            List<Dictionary<string, string>> wmic_autorus = GetAutoRunsWMIC();
            reg_autorus.AddRange(file_autorus);
            reg_autorus.AddRange(wmic_autorus);
            return reg_autorus;
        }

        public static List<Dictionary<string, string>> GetScheduledAppsNoMicrosoft()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                void EnumFolderTasks(TaskFolder fld)
                {
                    foreach (Microsoft.Win32.TaskScheduler.Task task in fld.Tasks)
                        ActOnTask(task);
                    //task.Name
                    //task.Enabled
                    //task.Definition.Actions
                    //task.Definition
                    foreach (TaskFolder sfld in fld.SubFolders)
                        EnumFolderTasks(sfld);
                }

                void ActOnTask(Microsoft.Win32.TaskScheduler.Task t)
                {
                    if (t.Enabled && (!String.IsNullOrEmpty(t.Definition.RegistrationInfo.Author) && !t.Definition.RegistrationInfo.Author.Contains("Microsoft")))
                    {
                        List<string> f_trigger = new List<string>();
                        foreach (Trigger trigger in t.Definition.Triggers)
                            f_trigger.Add(String.Format("{0}", trigger));

                        results.Add(new Dictionary<string, string>()
                    {
                        { "Name", t.Name },
                        { "Action", Environment.ExpandEnvironmentVariables(String.Format("{0}", t.Definition.Actions)) },
                        { "Trigger", String.Join("\n             ", f_trigger) },
                        { "Author", String.Join(", ", t.Definition.RegistrationInfo.Author) },
                        { "Description", String.Join(", ", t.Definition.RegistrationInfo.Description) },
                    });
                    }
                }
                EnumFolderTasks(TaskService.Instance.RootFolder);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }

    }
}
