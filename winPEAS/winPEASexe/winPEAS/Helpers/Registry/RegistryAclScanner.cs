using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Helpers;

namespace winPEAS.Helpers.Registry
{
    internal class RegistryWritableKeyInfo
    {
        public string Hive { get; set; }
        public string RelativePath { get; set; }
        public string FullPath { get; set; }
        public List<string> Principals { get; set; } = new List<string>();
        public List<string> Rights { get; set; } = new List<string>();
    }

    internal static class RegistryAclScanner
    {
        private static readonly Dictionary<string, string> LowPrivSidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null).Value, "BUILTIN\\Users" },
            { new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null).Value, "Authenticated Users" },
            { new SecurityIdentifier(WellKnownSidType.WorldSid, null).Value, "Everyone" },
            { new SecurityIdentifier(WellKnownSidType.InteractiveSid, null).Value, "Interactive" },
            { new SecurityIdentifier(WellKnownSidType.BuiltinGuestsSid, null).Value, "BUILTIN\\Guests" },
        };

        public static bool TryGetWritableKey(string hive, string relativePath, out RegistryWritableKeyInfo info)
        {
            info = null;
            using (var key = OpenKey(hive, relativePath))
            {
                if (key == null)
                {
                    return false;
                }

                return TryCollectWritableInfo(hive, relativePath, key, out info);
            }
        }

        public static List<RegistryWritableKeyInfo> ScanWritableKeys(string hive, IEnumerable<string> basePaths, int maxDepth, int maxResults)
        {
            var results = new List<RegistryWritableKeyInfo>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var basePath in basePaths ?? Enumerable.Empty<string>())
            {
                if (results.Count >= maxResults)
                {
                    break;
                }

                using (var key = OpenKey(hive, basePath))
                {
                    if (key == null)
                    {
                        continue;
                    }

                    Traverse(hive, key, basePath, 0, maxDepth, maxResults, seenPaths, results);
                }
            }

            return results;
        }

        private static void Traverse(string hive, RegistryKey currentKey, string currentPath, int depth, int maxDepth, int maxResults, HashSet<string> seenPaths, List<RegistryWritableKeyInfo> results)
        {
            if (currentKey == null || results.Count >= maxResults)
            {
                return;
            }

            if (TryCollectWritableInfo(hive, currentPath, currentKey, out var info))
            {
                if (seenPaths.Add(info.FullPath))
                {
                    results.Add(info);
                }

                if (results.Count >= maxResults)
                {
                    return;
                }
            }

            if (depth >= maxDepth)
            {
                return;
            }

            string[] subKeys;
            try
            {
                subKeys = currentKey.GetSubKeyNames();
            }
            catch
            {
                return;
            }

            foreach (var subKeyName in subKeys)
            {
                if (results.Count >= maxResults)
                {
                    break;
                }

                try
                {
                    using (var childKey = currentKey.OpenSubKey(subKeyName))
                    {
                        if (childKey == null)
                        {
                            continue;
                        }

                        string childPath = string.IsNullOrEmpty(currentPath) ? subKeyName : $"{currentPath}\\{subKeyName}";
                        Traverse(hive, childKey, childPath, depth + 1, maxDepth, maxResults, seenPaths, results);
                    }
                }
                catch
                {
                    // Ignore keys we cannot open
                }
            }
        }

        private static bool TryCollectWritableInfo(string hive, string relativePath, RegistryKey key, out RegistryWritableKeyInfo info)
        {
            info = null;

            try
            {
                var acl = key.GetAccessControl(AccessControlSections.Access);

                var principals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var rights = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (RegistryAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    if (rule.AccessControlType != AccessControlType.Allow)
                    {
                        continue;
                    }

                    var sid = rule.IdentityReference as SecurityIdentifier ?? rule.IdentityReference.Translate(typeof(SecurityIdentifier)) as SecurityIdentifier;
                    if (sid == null)
                    {
                        continue;
                    }

                    if (!LowPrivSidMap.TryGetValue(sid.Value, out var label))
                    {
                        continue;
                    }

                    string interestingRight = PermissionsHelper.PermInt2Str((int)rule.RegistryRights, PermissionType.WRITEABLE_OR_EQUIVALENT_REG);
                    if (string.IsNullOrEmpty(interestingRight))
                    {
                        continue;
                    }

                    principals.Add($"{label} ({sid.Value})");
                    rights.Add(interestingRight);
                }

                if (principals.Count == 0)
                {
                    return false;
                }

                string normalizedRelativePath = relativePath ?? string.Empty;
                string fullPath = string.IsNullOrEmpty(normalizedRelativePath) ? key.Name : $"{hive}\\{normalizedRelativePath}";

                info = new RegistryWritableKeyInfo
                {
                    Hive = hive,
                    RelativePath = normalizedRelativePath,
                    FullPath = fullPath,
                    Principals = principals.ToList(),
                    Rights = rights.ToList(),
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static RegistryKey OpenKey(string hive, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                RegistryKey baseKey = hive switch
                {
                    "HKLM" => Microsoft.Win32.Registry.LocalMachine,
                    "HKCU" => Microsoft.Win32.Registry.CurrentUser,
                    "HKU" => Microsoft.Win32.Registry.Users,
                    _ => null,
                };

                return baseKey?.OpenSubKey(path);
            }
            catch
            {
                return null;
            }
        }
    }
}
