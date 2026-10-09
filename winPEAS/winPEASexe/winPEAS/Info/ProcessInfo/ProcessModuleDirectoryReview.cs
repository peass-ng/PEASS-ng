using System;
using System.Collections.Generic;
using System.IO;

namespace winPEAS.Info.ProcessInfo
{
    internal sealed class ProcessModuleDirectoryLead
    {
        internal string ProcessName;
        internal string Owner;
        internal string Directory;
        internal string Permissions;
    }

    internal sealed class ProcessModuleDirectoryReview
    {
        internal readonly List<ProcessModuleDirectoryLead> Leads = new List<ProcessModuleDirectoryLead>();
        internal bool Truncated;

        private static readonly string[] ModuleDirectoryNames = { "Libraries", "Plugins", "Modules", "Extensions" };

        internal static bool IsEligibleModuleDirectoryAttributes(FileAttributes attributes) =>
            (attributes & FileAttributes.Directory) != 0 &&
            (attributes & FileAttributes.ReparsePoint) == 0;

        // Restrict metadata and ACL reads to a small set of known local directories.
        internal static bool IsFixedUnreparsedDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length > 512 || path.Length < 4 ||
                !char.IsLetter(path[0]) || path[1] != ':' || path[2] != '\\' ||
                path.IndexOf('/') >= 0 || path.IndexOf(':', 2) >= 0)
                return false;

            string[] parts = path.Substring(3).Split('\\');
            if (parts.Length > 32) return false;
            foreach (string part in parts)
                if (part.Length == 0 || part == "." || part == "..") return false;

            try
            {
                string current = path.Substring(0, 3);
                if (new DriveInfo(current).DriveType != DriveType.Fixed) return false;
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                foreach (string part in parts)
                {
                    current = Path.Combine(current, part);
                    FileAttributes attributes = File.GetAttributes(current);
                    if ((attributes & FileAttributes.Directory) == 0 ||
                        (attributes & FileAttributes.ReparsePoint) != 0) return false;
                }
                return true;
            }
            catch { return false; }
        }

        internal static ProcessModuleDirectoryReview FindLeads(
            IEnumerable<Dictionary<string, string>> processes, string currentUser,
            Func<string, IList<string>> getPermissions)
        {
            var result = new ProcessModuleDirectoryReview();
            if (processes == null || string.IsNullOrEmpty(currentUser) || getPermissions == null)
                return result;

            var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            int examined = 0;
            int verifiedRoots = 0;
            foreach (Dictionary<string, string> process in processes)
            {
                if (++examined > 512) { result.Truncated = true; break; }
                if (process == null || !process.TryGetValue("Owner", out string owner) ||
                    !process.TryGetValue("ExecutablePath", out string executable) ||
                    !process.TryGetValue("Name", out string processName) ||
                    string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(executable)) continue;

                string shortOwner = owner.Substring(owner.LastIndexOf('\\') + 1);
                if (string.Equals(shortOwner, currentUser, StringComparison.OrdinalIgnoreCase)) continue;
                string root;
                try { root = Path.GetDirectoryName(executable); }
                catch { continue; }
                if (string.IsNullOrEmpty(root) || root.Length > 480 ||
                    (!string.IsNullOrEmpty(windowsDirectory) &&
                     root.StartsWith(windowsDirectory + "\\", StringComparison.OrdinalIgnoreCase))) continue;
                if (!seenRoots.Add(root)) continue;
                if (seenRoots.Count > 48) { result.Truncated = true; break; }
                if (!IsFixedUnreparsedDirectory(root)) continue;
                if (++verifiedRoots > 24) { result.Truncated = true; break; }

                foreach (string name in ModuleDirectoryNames)
                {
                    string path = Path.Combine(root, name);
                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(path); }
                    catch { continue; }
                    if (!IsEligibleModuleDirectoryAttributes(attributes)) continue;
                    IList<string> permissions;
                    try { permissions = getPermissions(path); }
                    catch { continue; }
                    if (permissions == null) continue;
                    bool anyDeny = false;
                    foreach (string permission in permissions)
                        if (permission != null && permission.IndexOf("[Deny:", StringComparison.OrdinalIgnoreCase) >= 0)
                            anyDeny = true;
                    if (anyDeny) continue;
                    foreach (string permission in permissions)
                    {
                        if (!HasCreateFileAllowWithoutDeny(permission)) continue;
                        result.Leads.Add(new ProcessModuleDirectoryLead {
                            ProcessName = processName, Owner = owner,
                            Directory = path, Permissions = permission
                        });
                        break;
                    }
                    if (result.Leads.Count >= 12) { result.Truncated = true; return result; }
                }
            }
            return result;
        }

        internal static bool HasCreateFileAllowWithoutDeny(string permission)
        {
            if (string.IsNullOrEmpty(permission) || permission.IndexOf("[Deny:", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            int allow = permission.IndexOf("[Allow:", StringComparison.OrdinalIgnoreCase);
            if (allow < 0) return false;
            string rights = permission.Substring(allow + 7);
            int end = rights.IndexOf(']');
            if (end < 0) return false;
            foreach (string right in rights.Substring(0, end).Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                if (right == "AllAccess" || right == "GenericAll" || right == "FullControl" ||
                    right == "GenericWrite" || right == "WriteData/CreateFiles" ||
                    right == "Modify" || right == "Write") return true;
            return false;
        }
    }
}
