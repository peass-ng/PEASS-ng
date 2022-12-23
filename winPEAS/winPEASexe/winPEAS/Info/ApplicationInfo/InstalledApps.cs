using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.ApplicationInfo
{
    internal static class InstalledApps
    {
        public static SortedDictionary<string, Dictionary<string, string>> GetInstalledAppsPerms()
        {
            //Get from Program Files
            SortedDictionary<string, Dictionary<string, string>> results = GetInstalledAppsPermsPath(Path.GetPathRoot(Environment.SystemDirectory) + "Program Files");
            SortedDictionary<string, Dictionary<string, string>> results2 = GetInstalledAppsPermsPath(Path.GetPathRoot(Environment.SystemDirectory) + "Program Files (x86)");
            results.Concat(results2).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            string[] registryPaths = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var registryPath in registryPaths)
            {
                string[] subkeys = RegistryHelper.GetRegSubkeys("HKLM", registryPath);
                if (subkeys != null)
                {
                    foreach (string app in subkeys)
                    {
                        string installLocation = RegistryHelper.GetRegValue("HKLM", string.Format(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{0}", app), "InstallLocation");
                        if (string.IsNullOrEmpty(installLocation))
                        {
                            continue;
                        }

                        installLocation = installLocation.Replace("\"", "");

                        if (installLocation.EndsWith(@"\"))
                        {
                            installLocation = installLocation.Substring(0, installLocation.Length - 1);
                        }

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
                            {
                                results[installLocation] = PermissionsHelper.GetRecursivePrivs(installLocation);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private static SortedDictionary<string, Dictionary<string, string>> GetInstalledAppsPermsPath(string fpath)
        {
            var results = new SortedDictionary<string, Dictionary<string, string>>();
            try
            {
                if (Directory.Exists(fpath))
                {
                    foreach (string f in Directory.EnumerateFiles(fpath))
                    {
                        results[f] = new Dictionary<string, string>
                    {
                        { f, string.Join(", ", PermissionsHelper.GetPermissionsFile(f, Checks.Checks.CurrentUserSiDs)) }
                    };
                    }
                    foreach (string d in Directory.EnumerateDirectories(fpath))
                    {
                        results[d] = PermissionsHelper.GetRecursivePrivs(d);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error: " + ex);
            }
            return results;
        }

    }
}
