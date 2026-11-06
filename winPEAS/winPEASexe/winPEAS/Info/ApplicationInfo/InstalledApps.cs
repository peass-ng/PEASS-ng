using System;
using System.Collections.Generic;
using System.IO;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.ApplicationInfo
{
    internal static class InstalledApps
    {
        private static readonly string[] UninstallRegistryPaths =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        internal static List<string> GetInstallLocations()
        {
            return GetInstallLocationsCore(
                path => RegistryHelper.GetRegSubkeys("HKLM", path),
                path => RegistryHelper.GetRegValue("HKLM", path, "InstallLocation"),
                Directory.Exists);
        }

        internal static List<string> GetInstallLocationsCore(
            Func<string, string[]> getSubkeys,
            Func<string, string> getInstallLocation,
            Func<string, bool> directoryExists)
        {
            var locations = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string registryPath in UninstallRegistryPaths)
            {
                string[] subkeys;
                try { subkeys = getSubkeys(registryPath); }
                catch { continue; }
                if (subkeys == null) continue;

                foreach (string app in subkeys)
                {
                    try
                    {
                        string location = getInstallLocation(registryPath + @"\" + app);
                        if (string.IsNullOrWhiteSpace(location)) continue;
                        location = Environment.ExpandEnvironmentVariables(location.Trim().Trim('"'));
                        if (location.Length < 3 || !char.IsLetter(location[0]) ||
                            location[1] != ':' || (location[2] != '\\' && location[2] != '/')) continue;
                        location = Path.GetFullPath(location).TrimEnd('\\');
                        if (location.Length <= 2 || !directoryExists(location)) continue;
                        if (seen.Add(location)) locations.Add(location);
                    }
                    catch { /* Ignore malformed or inaccessible installation paths. */ }
                }
            }
            return locations;
        }

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

            foreach (var registryPath in UninstallRegistryPaths)
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
