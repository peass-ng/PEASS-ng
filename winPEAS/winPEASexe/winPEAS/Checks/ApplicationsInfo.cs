using System;
using System.Collections.Generic;
using winPEAS.Helpers;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Checks
{
    internal class ApplicationsInfo : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Applications Information");

            new List<Action>
            {
                PrintActiveWindow,
                PrintInstalledApps,
                PrintAutoRuns,
                PrintScheduled,
                PrintDeviceDrivers,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        void PrintActiveWindow()
        {
            try
            {
                Beaprint.MainPrint("Current Active Window Application");
                string title = ApplicationInfoHelper.GetActiveWindowTitle();
                List<string> permsFile = PermissionsHelper.GetPermissionsFile(title, Checks.CurrentUserSiDs);
                List<string> permsFolder = PermissionsHelper.GetPermissionsFolder(title, Checks.CurrentUserSiDs);
                if (permsFile.Count > 0)
                {
                    Beaprint.BadPrint("    " + title);
                    Beaprint.BadPrint("    File Permissions: " + string.Join(",", permsFile));
                }
                else
                {
                    Beaprint.GoodPrint("    " + title);
                }

                if (permsFolder.Count > 0)
                {
                    Beaprint.BadPrint("    Possible DLL Hijacking, folder is writable: " + PermissionsHelper.GetFolderFromString(title));
                    Beaprint.BadPrint("    Folder Permissions: " + string.Join(",", permsFile));
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintInstalledApps()
        {
            try
            {
                Beaprint.MainPrint("Installed Applications --Via Program Files/Uninstall registry--");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#applications", "Check if you can modify installed software");
                SortedDictionary<string, Dictionary<string, string>> installedAppsPerms = InstalledApps.GetInstalledAppsPerms();
                string format = "    ==>  {0} ({1})";

                foreach (KeyValuePair<string, Dictionary<string, string>> app in installedAppsPerms)
                {
                    if (string.IsNullOrEmpty(app.Value.ToString())) //If empty, nothing found, is good
                    {
                        Beaprint.GoodPrint(app.Key);
                    }
                    else //Then, we need to look deeper
                    {
                        //Checkeamos si la carpeta (que va a existir como subvalor dentro de si misma) debe ser good
                        if (string.IsNullOrEmpty(app.Value[app.Key]))
                        {
                            Beaprint.GoodPrint("    " + app.Key);
                        }
                        else
                        {
                            Beaprint.BadPrint(string.Format("    {0}({1})", app.Key, app.Value[app.Key]));
                            app.Value[app.Key] = ""; //So no reprinted later
                        }

                        //Check the rest of the values to see if we have something to print in red (permissions)
                        foreach (KeyValuePair<string, string> subfolder in app.Value)
                        {
                            if (!string.IsNullOrEmpty(subfolder.Value))
                            {
                                Beaprint.BadPrint(string.Format(format, subfolder.Key, subfolder.Value));
                            }
                        }
                    }
                }
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Beaprint.PrintException(e.Message);
            }
        }

        private static void PrintAutoRuns()
        {
            try
            {
                Beaprint.MainPrint("Autorun Applications");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/privilege-escalation-with-autorun-binaries.html", "Check if you can modify other users AutoRuns binaries (Note that is normal that you can modify HKCU registry and binaries indicated there)");
                List<Dictionary<string, string>> apps = AutoRuns.GetAutoRuns(Checks.CurrentUserSiDs);

                foreach (Dictionary<string, string> app in apps)
                {
                    var colorsA = new Dictionary<string, string>
                        {
                            { "FolderPerms:.*", Beaprint.ansi_color_bad },
                            { "FilePerms:.*", Beaprint.ansi_color_bad },
                            { "(Unquoted and Space detected)", Beaprint.ansi_color_bad },
                            { "(PATH Injection)", Beaprint.ansi_color_bad },
                            { "RegPerms: .*", Beaprint.ansi_color_bad },
                            { (app["Folder"].Length > 0) ? app["Folder"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "ouigyevb2uivydi2u3id2ddf3", !string.IsNullOrEmpty(app["interestingFolderRights"]) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { (app["File"].Length > 0) ? app["File"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "adu8v298hfubibuidiy2422r", !string.IsNullOrEmpty(app["interestingFileRights"]) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { (app["Reg"].Length > 0) ? app["Reg"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "o8a7eduia37ibduaunbf7a4g7ukdhk4ua", (app["RegPermissions"].Length > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { "Potentially sensitive file content:", Beaprint.ansi_color_bad },
                        };
                    string line = "";

                    if (!string.IsNullOrEmpty(app["Reg"]))
                    {
                        line += "\n    RegPath: " + app["Reg"];
                    }

                    if (app["RegPermissions"].Length > 0)
                    {
                        line += "\n    RegPerms: " + app["RegPermissions"];
                    }

                    if (!string.IsNullOrEmpty(app["RegKey"]))
                    {
                        line += "\n    Key: " + app["RegKey"];
                    }

                    if (!string.IsNullOrEmpty(app["Folder"]))
                    {
                        line += "\n    Folder: " + app["Folder"];
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(app["Reg"]))
                        {
                            line += "\n    Folder: None (PATH Injection)";
                        }
                    }

                    if (!string.IsNullOrEmpty(app["interestingFolderRights"]))
                    {
                        line += "\n    FolderPerms: " + app["interestingFolderRights"];
                    }

                    string filepath_mod = app["File"].Replace("\"", "").Replace("'", "");
                    if (!string.IsNullOrEmpty(app["File"]))
                    {
                        line += "\n    File: " + filepath_mod;
                    }

                    if (app["isUnquotedSpaced"].ToLower() != "false")
                    {
                        line += $" (Unquoted and Space detected) - {app["isUnquotedSpaced"]}";
                    }

                    if (!string.IsNullOrEmpty(app["interestingFileRights"]))
                    {
                        line += "\n    FilePerms: " + app["interestingFileRights"];
                    }

                    if (app.ContainsKey("sensitiveInfoList") && !string.IsNullOrEmpty(app["sensitiveInfoList"]))
                    {
                        line += "\n    Potentially sensitive file content: " + app["sensitiveInfoList"];
                    }

                    Beaprint.AnsiPrint(line, colorsA);
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintScheduled()
        {
            try
            {
                Beaprint.MainPrint("Scheduled Applications --Non Microsoft--");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/privilege-escalation-with-autorun-binaries.html", "Check if you can modify other users scheduled binaries");
                List<Dictionary<string, string>> scheduled_apps = ApplicationInfoHelper.GetScheduledAppsNoMicrosoft();

                foreach (Dictionary<string, string> sapp in scheduled_apps)
                {
                    List<string> fileRights = PermissionsHelper.GetPermissionsFile(sapp["Action"], Checks.CurrentUserSiDs);
                    List<string> dirRights = PermissionsHelper.GetPermissionsFolder(sapp["Action"], Checks.CurrentUserSiDs);
                    string formString = "    ({0}) {1}: {2}";

                    if (fileRights.Count > 0)
                    {
                        formString += "\n    Permissions file: {3}";
                    }

                    if (dirRights.Count > 0)
                    {
                        formString += "\n    Permissions folder(DLL Hijacking): {4}";
                    }

                    if (!string.IsNullOrEmpty(sapp["Trigger"]))
                    {
                        formString += "\n    Trigger: {5}";
                    }

                    if (string.IsNullOrEmpty(sapp["Description"]))
                    {
                        formString += "\n    {6}";
                    }

                    Dictionary<string, string> colorsS = new Dictionary<string, string>()
                    {
                        { "Permissions.*", Beaprint.ansi_color_bad },
                        { sapp["Action"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (fileRights.Count > 0 || dirRights.Count > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                    };
                    Beaprint.AnsiPrint(string.Format(formString, sapp["Author"], sapp["Name"], sapp["Action"], string.Join(", ", fileRights), string.Join(", ", dirRights), sapp["Trigger"], sapp["Description"]), colorsS);
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintDeviceDrivers()
        {
            try
            {
                Beaprint.MainPrint("Device Drivers --Non Microsoft--");
                // this link is not very specific, but its the best on hacktricks
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#drivers", "Check 3rd party drivers for known vulnerabilities/rootkits.");

                foreach (var driver in DeviceDrivers.GetDeviceDriversNoMicrosoft())
                {
                    string pathDriver = driver.Key;
                    List<string> fileRights = PermissionsHelper.GetPermissionsFile(pathDriver, Checks.CurrentUserSiDs);
                    List<string> dirRights = PermissionsHelper.GetPermissionsFolder(pathDriver, Checks.CurrentUserSiDs);

                    Dictionary<string, string> colorsD = new Dictionary<string, string>()
                        {
                            { "Permissions.*", Beaprint.ansi_color_bad },
                            { "Capcom.sys", Beaprint.ansi_color_bad },
                            { pathDriver.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (fileRights.Count > 0 || dirRights.Count > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                        };


                    string formString = "    {0} - {1} [{2}]: {3}";
                    if (fileRights.Count > 0)
                    {
                        formString += "\n    Permissions file: {4}";
                    }

                    if (dirRights.Count > 0)
                    {
                        formString += "\n    Permissions folder(DLL Hijacking): {5}";
                    }

                    Beaprint.AnsiPrint(string.Format(formString, driver.Value.ProductName, driver.Value.ProductVersion, driver.Value.CompanyName, pathDriver, string.Join(", ", fileRights), string.Join(", ", dirRights)), colorsD);

                    //If vuln, end with separator
                    if ((fileRights.Count > 0) || (dirRights.Count > 0))
                    {
                        Beaprint.PrintLineSeparator();
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }
    }
}
