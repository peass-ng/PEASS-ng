using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace winPEAS
{
    class ServicesInfo
    {
        public static List<Dictionary<string, string>> GetNonstandardServices()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_service");
                ManagementObjectCollection data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    if (result["PathName"] != null)
                    {
                        Match path = Regex.Match(result["PathName"].ToString(), @"^\W*([a-z]:\\.+?(\.exe|\.dll|\.sys))\W*", RegexOptions.IgnoreCase);
                        String binaryPath = path.Groups[1].ToString();
                        string companyName = "";
                        string isDotNet = "";
                        try
                        {
                            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(binaryPath);
                            companyName = myFileVersionInfo.CompanyName;
                            isDotNet = MyUtils.CheckIfDotNet(binaryPath) ? "isDotNet" : "";
                        }
                        catch (Exception ex)
                        {
                            // Not enough privileges
                        }

                        if ((String.IsNullOrEmpty(companyName)) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                        {
                            Dictionary<string, string> toadd = new Dictionary<string, string>();
                            toadd["Name"] = String.Format("{0}", result["Name"]);
                            toadd["DisplayName"] = String.Format("{0}", result["DisplayName"]);
                            toadd["CompanyName"] = companyName;
                            toadd["State"] = String.Format("{0}", result["State"]);
                            toadd["StartMode"] = String.Format("{0}", result["StartMode"]);
                            toadd["PathName"] = String.Format("{0}", result["PathName"]);
                            toadd["FilteredPath"] = binaryPath;
                            toadd["isDotNet"] = isDotNet;
                            toadd["Description"] = String.Format("{0}", result["Description"]);
                            results.Add(toadd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [X] Exception: {0}", ex.Message);
            }
            return results;
        }

        public static List<string> GetWriteServiceRegs()
        {
            List<string> results = new List<string>();
            try
            {
                RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"system\currentcontrolset\services");
                foreach (string serviceRegName in regKey.GetSubKeyNames())
                {
                    if (MyUtils.CheckWriteAccessReg("HKLM", @"system\currentcontrolset\services\" + serviceRegName))
                        results.Add(@"HKLM\system\currentcontrolset\services\" + serviceRegName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetRegistryAutoRuns()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                string[] autorunLocations = new string[] {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunService",
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnceService",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunService",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnceService"
            };

                foreach (string autorunLocation in autorunLocations)
                {
                    Dictionary<string, object> settings = MyUtils.GetRegValues("HKLM", autorunLocation);
                    if ((settings != null) && (settings.Count != 0))
                    {
                        foreach (KeyValuePair<string, object> kvp in settings)
                        {
                            string filepath = Environment.ExpandEnvironmentVariables(String.Format("{0}", kvp.Value));
                            string folder = System.IO.Path.GetDirectoryName(filepath.Replace("'", "").Replace("\"", ""));
                            results.Add(new Dictionary<string, string>() {
                            { "Reg", "HKLM\\"+autorunLocation },
                            { "Folder", folder },
                            { "File", filepath },
                            { "isWritableReg", MyUtils.CheckWriteAccessReg("HKLM", autorunLocation).ToString()},
                            { "interestingFolderRights", String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.interestingUsersGroups))},
                            { "interestingFileRights", String.Join(", ", MyUtils.GetPermissionsFile(filepath, Program.interestingUsersGroups))},
                            { "isUnquotedSpaced", MyUtils.CheckQuoteAndSpace(filepath).ToString() }
                        });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return results;
        }
        public static Dictionary<string, string> GetPathDLLHijacking()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                var path_env = Environment.GetEnvironmentVariable("PATH");
                List<string> folders = path_env.Split(';').ToList();
                foreach (string folder in folders)
                {
                    results[folder] = String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.interestingUsersGroups));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [X] Exception: {0}", ex.Message);
            }
            return results;
        }
    }
}
