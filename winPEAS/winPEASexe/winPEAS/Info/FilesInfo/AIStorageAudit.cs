using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace winPEAS.Info.FilesInfo
{
    internal static class AIStorageAudit
    {
        internal static Dictionary<string, string> GetCurrentUserRoots()
        {
            var roots = new Dictionary<string, string>();
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(profile))
            {
                roots.Add("Codex", Path.Combine(profile, ".codex"));
                roots.Add("Claude Code", Path.Combine(profile, ".claude"));
                roots.Add("Cursor CLI", Path.Combine(profile, ".cursor"));
                roots.Add("Antigravity CLI", Path.Combine(profile, ".gemini", "antigravity-cli"));
                roots.Add("Grok", Path.Combine(profile, ".grok"));
            }
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appData))
                roots.Add("Cursor desktop", Path.Combine(appData, "Cursor"));
            return roots;
        }

        internal static bool HasReparsePoint(string path, Func<string, FileAttributes> readAttributes = null)
        {
            readAttributes = readAttributes ?? File.GetAttributes;
            // Inspect ancestors first so a junction cannot redirect the root check.
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && HasReparsePoint(parent, readAttributes))
                return true;
            return (readAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

        internal static List<string> GetBroadAccessRules(DirectorySecurity security)
        {
            var findings = new List<string>();
            FileSystemRights relevant = FileSystemRights.ReadData | FileSystemRights.WriteData
                | FileSystemRights.AppendData | FileSystemRights.Delete
                | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.ChangePermissions
                | FileSystemRights.TakeOwnership;
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                if (rule.AccessControlType != AccessControlType.Allow
                    || (rule.PropagationFlags & PropagationFlags.InheritOnly) != 0
                    || (rule.FileSystemRights & relevant) == 0)
                    continue;
                var sid = (SecurityIdentifier)rule.IdentityReference;
                if (sid.IsWellKnown(WellKnownSidType.WorldSid)
                    || sid.IsWellKnown(WellKnownSidType.AuthenticatedUserSid)
                    || sid.IsWellKnown(WellKnownSidType.BuiltinUsersSid)
                    || sid.IsWellKnown(WellKnownSidType.BuiltinGuestsSid))
                    findings.Add(string.Format("{0}: {1}", sid.Value, rule.FileSystemRights));
            }
            // Allow ACEs are review candidates, not an effective-access calculation.
            return findings;
        }
    }
}
