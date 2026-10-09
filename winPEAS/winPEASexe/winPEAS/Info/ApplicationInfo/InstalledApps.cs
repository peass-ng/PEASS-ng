using System;
using System.Collections.Generic;
using System.IO;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.ApplicationInfo
{
    internal static class InstalledApps
    {
        public static SortedDictionary<string, Dictionary<string, string>> GetInstalledAppsPerms()
        {
            return GetInstalledAppsPermsCore(
                new[] { Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetEnvironmentVariable("ProgramFiles")
                        ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                        ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) },
                GetInstalledAppsPermsPath,
                path => RegistryHelper.GetRegSubkeys("HKLM", path),
                path => RegistryHelper.GetRegValue("HKLM", path, "InstallLocation"),
                Directory.Exists,
                path => PermissionsHelper.GetRecursivePrivs(path));
        }

        internal static SortedDictionary<string, Dictionary<string, string>> GetInstalledAppsPermsCore(
            IEnumerable<string> programFilesRoots,
            Func<string, SortedDictionary<string, Dictionary<string, string>>> getPathPermissions,
            Func<string, string[]> getSubkeys,
            Func<string, string> getInstallLocation,
            Func<string, bool> directoryExists,
            Func<string, Dictionary<string, string>> getRecursivePrivs)
        {
            var results = new SortedDictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in programFilesRoots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                foreach (var app in getPathPermissions(root))
                    results[app.Key] = app.Value;
            }

            string[] registryPaths = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var registryPath in registryPaths)
            {
                string[] subkeys = getSubkeys(registryPath);
                if (subkeys != null)
                {
                    foreach (string app in subkeys)
                    {
                        string installLocation = getInstallLocation(registryPath + @"\" + app);
                        if (string.IsNullOrEmpty(installLocation))
                        {
                            continue;
                        }

                        installLocation = installLocation.Replace("\"", "");

                        if (installLocation.EndsWith(@"\"))
                        {
                            installLocation = installLocation.Substring(0, installLocation.Length - 1);
                        }

                        if (!results.ContainsKey(installLocation) && directoryExists(installLocation))
                        {
                            bool already = false;
                            foreach (string path in results.Keys)
                            {
                                if (installLocation.StartsWith(path.TrimEnd('\\') + @"\", StringComparison.OrdinalIgnoreCase))
                                {
                                    already = true;
                                    break;
                                }
                            }

                            if (!already)
                            {
                                results[installLocation] = getRecursivePrivs(installLocation);
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
