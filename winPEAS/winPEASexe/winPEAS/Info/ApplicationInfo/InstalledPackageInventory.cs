using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace winPEAS.Info.ApplicationInfo
{
    public static class InstalledPackageInventory
    {
        public const int MaxPackages = 300;

        public static List<Dictionary<string, string>> GetInstalledPackages(int maxPackages = MaxPackages)
        {
            var packages = new List<Dictionary<string, string>>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddRegistryPackages(packages, seen, RegistryHive.LocalMachine, RegistryView.Registry64, maxPackages);
            AddRegistryPackages(packages, seen, RegistryHive.LocalMachine, RegistryView.Registry32, maxPackages);
            AddRegistryPackages(packages, seen, RegistryHive.CurrentUser, RegistryView.Default, maxPackages);

            return packages;
        }

        private static void AddRegistryPackages(
            List<Dictionary<string, string>> packages,
            HashSet<string> seen,
            RegistryHive hive,
            RegistryView view,
            int maxPackages)
        {
            if (packages.Count >= maxPackages)
            {
                return;
            }

            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                using (var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (uninstallKey == null)
                    {
                        return;
                    }

                    foreach (var subkeyName in uninstallKey.GetSubKeyNames())
                    {
                        if (packages.Count >= maxPackages)
                        {
                            return;
                        }

                        using (var appKey = uninstallKey.OpenSubKey(subkeyName))
                        {
                            if (appKey == null)
                            {
                                continue;
                            }

                            var package = CreatePackage(appKey);
                            if (package == null)
                            {
                                continue;
                            }

                            var key = $"{package["name"]}\0{package["version"]}";
                            if (!seen.Add(key))
                            {
                                continue;
                            }

                            packages.Add(package);
                        }
                    }
                }
            }
            catch
            {
                // Registry inventory is best-effort; unreadable views should not break winPEAS.
            }
        }

        private static Dictionary<string, string> CreatePackage(RegistryKey appKey)
        {
            var name = CleanValue(appKey.GetValue("DisplayName"));
            var version = CleanValue(appKey.GetValue("DisplayVersion"));
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            var package = new Dictionary<string, string>
            {
                { "name", name },
                { "version", version },
                { "manager", "windows-registry" }
            };

            var publisher = CleanValue(appKey.GetValue("Publisher"));
            if (!string.IsNullOrWhiteSpace(publisher))
            {
                package["publisher"] = publisher;
            }

            return package;
        }

        private static string CleanValue(object value)
        {
            return Convert.ToString(value)?.Trim().Replace("\r", " ").Replace("\n", " ");
        }
    }
}
