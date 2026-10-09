using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using winPEAS.Helpers;
using winPEAS.Native;
using winPEAS.TaskScheduler;

namespace winPEAS.Info.ApplicationInfo
{
    internal sealed class ScheduledAppsResult
    {
        public List<Dictionary<string, string>> Apps { get; } = new List<Dictionary<string, string>>();
        public bool LimitReached { get; set; }
        public bool WithoutAuthorLimitReached { get; set; }
    }

    internal class ApplicationInfoHelper
    {
        internal const int MaxScheduledTasksInspected = 2000;
        internal const int MaxScheduledFoldersInspected = 200;
        internal const int MaxScheduledAppsDisplayed = 150;
        internal const int MaxScheduledAppsWithoutAuthor = 30;

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            IntPtr handle = User32.GetForegroundWindow();

            if (User32.GetWindowText(handle, buff, nChars) > 0)
            {
                return buff.ToString();
            }

            return null;
        }

        internal static ScheduledAppsResult GetScheduledAppsNoMicrosoft()
        {
            var results = new ScheduledAppsResult();
            int tasksInspected = 0;
            int foldersInspected = 0;
            int withoutAuthor = 0;

            void ProcessTaskFolder(TaskFolder taskFolder)
            {
                if (results.LimitReached)
                    return;
                if (++foldersInspected > MaxScheduledFoldersInspected)
                {
                    results.LimitReached = true;
                    return;
                }

                try
                {
                    foreach (var runTask in taskFolder.GetTasks())
                    {
                        if (++tasksInspected > MaxScheduledTasksInspected || results.Apps.Count >= MaxScheduledAppsDisplayed)
                        {
                            results.LimitReached = true;
                            return;
                        }
                        ActOnTask(runTask);
                    }

                    foreach (var taskFolderSub in taskFolder.SubFolders)
                    {
                        if (results.LimitReached)
                            return;
                        ProcessTaskFolder(taskFolderSub);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException($"failed to browse scheduled task folder: '{taskFolder.Path}': {ex.Message}");
                }
            }

            void ActOnTask(Task t)
            {
                try
                {
                    if (t.Enabled && ShouldIncludeScheduledTask(t.Path, t.Definition.RegistrationInfo.Author))
                    {
                        string author = t.Definition.RegistrationInfo.Author;
                        if (string.IsNullOrWhiteSpace(author) && withoutAuthor >= MaxScheduledAppsWithoutAuthor)
                        {
                            results.WithoutAuthorLimitReached = true;
                            return;
                        }
                        List<string> f_trigger = new List<string>();
                        foreach (Trigger trigger in t.Definition.Triggers)
                        {
                            f_trigger.Add($"{trigger}");
                        }

                        List<string> actionPaths = new List<string>();
                        string snortExecutable = null;
                        string snortArguments = null;
                        foreach (winPEAS.TaskScheduler.Action action in t.Definition.Actions)
                        {
                            if (action is winPEAS.TaskScheduler.Action.ExecAction executable && !string.IsNullOrWhiteSpace(executable.Path))
                            {
                                string actionPath = Environment.ExpandEnvironmentVariables(executable.Path);
                                actionPaths.Add(actionPath);
                                if (snortExecutable == null && IsSnortExecutable(actionPath))
                                {
                                    snortExecutable = actionPath;
                                    snortArguments = executable.Arguments;
                                }
                            }
                        }

                        var principal = t.Definition.Principal;
                        var app = CreateScheduledApp(t.Name, author, t.Definition.RegistrationInfo.Description,
                            Environment.ExpandEnvironmentVariables($"{t.Definition.Actions}"), f_trigger,
                            actionPaths, principal.UserId, principal.GroupId, principal.LogonType, principal.RunLevel);
                        if (snortExecutable != null)
                        {
                            app["SnortArguments"] = snortArguments ?? string.Empty;
                            app["SnortExecutable"] = snortExecutable;
                        }
                        results.Apps.Add(app);
                        if (string.IsNullOrWhiteSpace(author))
                            withoutAuthor++;
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException($"failed to process scheduled task: '{t.Name}': {ex.Message}");
                }
            }

            TaskFolder folder = TaskService.Instance.GetFolder("\\");

            ProcessTaskFolder(folder);

            return results;
        }

        internal static bool IsSnortExecutable(string executable)
        {
            if (string.IsNullOrWhiteSpace(executable)) return false;
            string path = executable.Trim().Trim('"');
            int separator = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
            return IsLocalWindowsPath(path) &&
                path.Substring(separator + 1).Equals("snort.exe", StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetSnortConfigPath(string executable, string arguments)
        {
            if (!IsSnortExecutable(executable) || string.IsNullOrWhiteSpace(arguments) || arguments.Length > 4096)
                return null;

            Match match = Regex.Match(arguments, @"(?:^|\s)-c\s+(?:""(?<quoted>[^""\r\n]+)""|(?<plain>\S+))(?=\s|$)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            string path = match.Success ? match.Groups["quoted"].Success
                ? match.Groups["quoted"].Value : match.Groups["plain"].Value : null;
            path = string.IsNullOrEmpty(path) ? null : Environment.ExpandEnvironmentVariables(path);
            return IsLocalWindowsPath(path) && path.EndsWith(".conf", StringComparison.OrdinalIgnoreCase) ? path : null;
        }

        internal static List<string> GetSnortDynamicPreprocessorDirectories(string config)
        {
            var directories = new List<string>();
            if (string.IsNullOrEmpty(config) || config.Length > 65536) return directories;
            string[] lines = config.Split('\n');
            for (int index = 0; index < lines.Length && index < 1024 && directories.Count < 3; index++)
            {
                string line = lines[index].Trim();
                if (line.Length > 2048 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                Match match = Regex.Match(line, @"^dynamicpreprocessor\s+directory\s+(?<path>.+?)\s*$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success) continue;
                string path = match.Groups["path"].Value.Split('#')[0].Trim().Trim('"');
                path = Environment.ExpandEnvironmentVariables(path);
                if (IsLocalWindowsPath(path) && !directories.Contains(path, StringComparer.OrdinalIgnoreCase))
                    directories.Add(path);
            }
            return directories;
        }

        private static bool IsLocalWindowsPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.Length >= 3 && char.IsLetter(path[0]) &&
                path[1] == ':' && path[2] == '\\' && path.IndexOfAny(new[] { '\r', '\n', '"' }) < 0;
        }

        internal static bool HasBoundedLocalPathComponents(string path)
        {
            if (!IsLocalWindowsPath(path) || path.Length > 512 || path.IndexOf('/') >= 0) return false;
            string[] parts = path.Substring(3).Split('\\');
            if (parts.Length == 0 || parts.Length > 32) return false;
            foreach (string part in parts)
            {
                if (part.Length == 0 || part == "." || part == ".." || part.IndexOf(':') >= 0)
                    return false;
            }
            return true;
        }

        private static bool IsFixedUnreparsedPath(string path)
        {
            if (!HasBoundedLocalPathComponents(path)) return false;
            string root = Path.GetPathRoot(path);
            if (new DriveInfo(root).DriveType != DriveType.Fixed) return false;
            string current = root;
            foreach (string part in path.Substring(3).Split('\\'))
            {
                current = Path.Combine(current, part);
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    return false;
            }
            return true;
        }

        internal static string ReadSnortConfigCapped(Stream stream)
        {
            if (stream == null || !stream.CanRead) return null;
            byte[] bytes = new byte[65537];
            int read = 0;
            while (read < bytes.Length)
            {
                int count = stream.Read(bytes, read, bytes.Length - read);
                if (count == 0) break;
                read += count;
            }
            if (read > 65536) return null;
            using (var memory = new MemoryStream(bytes, 0, read, false))
            using (var reader = new StreamReader(memory, Encoding.UTF8, true))
                return reader.ReadToEnd();
        }

        internal static string GetSnortModuleDirectoryLead(string executable, string arguments, string principal,
            ISet<string> tokenSids)
        {
            string configPath = GetSnortConfigPath(executable, arguments);
            if (configPath == null || tokenSids == null || tokenSids.Count == 0 ||
                !HasDifferentTaskPrincipal(principal)) return null;
            try
            {
                string executablePath = executable.Trim().Trim('"');
                if (!IsFixedUnreparsedPath(executablePath) || !IsFixedUnreparsedPath(configPath))
                    return null;
                var executableFile = new FileInfo(executablePath);
                if (!executableFile.Exists) return null;
                var configFile = new FileInfo(configPath);
                if (!configFile.Exists || configFile.Length > 65536) return null;
                string config;
                using (var stream = new FileStream(configPath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    config = ReadSnortConfigCapped(stream);
                }
                if (config == null) return null;
                foreach (string path in GetSnortDynamicPreprocessorDirectories(config))
                {
                    if (!IsFixedUnreparsedPath(path) || !Directory.Exists(path)) continue;
                    byte[] security = Directory.GetAccessControl(path, AccessControlSections.Access)
                        .GetSecurityDescriptorBinaryForm();
                    string trustee = PrivilegedScheduledTasks.FindWriteTrustee(
                        new RawSecurityDescriptor(security, 0), tokenSids, 0x00000002);
                    if (!string.IsNullOrEmpty(trustee))
                        return path + " (create-files ACL for " + trustee + ")";
                }
            }
            catch
            {
                // Inaccessible config, identity, or ACL is unknown rather than a finding.
            }
            return null;
        }

        private static bool HasDifferentTaskPrincipal(string principal)
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    string currentName = winPEAS.Checks.Checks.CurrentUserDomainName + "\\" +
                        winPEAS.Checks.Checks.CurrentUserName;
                    return identity.User != null &&
                        IsDifferentTaskPrincipalWithoutLookup(principal, currentName, identity.User.Value);
                }
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsDifferentTaskPrincipalWithoutLookup(string principal, string currentName,
            string currentSid)
        {
            if (string.IsNullOrWhiteSpace(principal) || principal.Length > 256 ||
                string.IsNullOrWhiteSpace(currentSid)) return false;
            string configured = principal.Trim();
            if (configured.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                configured.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase))
                return !currentSid.Equals("S-1-5-18", StringComparison.OrdinalIgnoreCase);
            if (configured.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return !new SecurityIdentifier(configured).Value.Equals(currentSid,
                        StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
            // Task Scheduler commonly stores DOMAIN\user or MACHINE\user. Other
            // forms may alias this same account and are left unresolved locally.
            return !string.IsNullOrWhiteSpace(currentName) && configured.IndexOf('\\') >= 0 &&
                currentName.IndexOf('\\') >= 0 &&
                !configured.Equals(currentName, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ShouldIncludeScheduledTask(string path, string author)
        {
            return !string.IsNullOrEmpty(path) &&
                path.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) < 0 &&
                (author == null || author.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) < 0);
        }

        internal static string GetScheduledCredentialLead(TaskLogonType logonType)
        {
            return logonType == TaskLogonType.Password || logonType == TaskLogonType.InteractiveTokenOrPassword
                ? "Possible LSA-secret lead: password-based task logon may store a credential; recovery requires admin or SYSTEM access."
                : string.Empty;
        }

        internal static Dictionary<string, string> CreateScheduledApp(string name, string author, string description,
            string action, IEnumerable<string> triggers, IEnumerable<string> actionPaths,
            string userId, string groupId, TaskLogonType logonType, TaskRunLevel runLevel)
        {
            return new Dictionary<string, string>
            {
                { "Name", name ?? string.Empty },
                { "Author", string.IsNullOrWhiteSpace(author) ? "author unknown" : author },
                { "Description", description ?? string.Empty },
                { "Action", action ?? string.Empty },
                { "Trigger", string.Join("\n             ", triggers ?? Enumerable.Empty<string>()) },
                { "ActionPath", string.Join("\n", (actionPaths ?? Enumerable.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p))) },
                { "Principal", !string.IsNullOrWhiteSpace(groupId) ? groupId : (string.IsNullOrWhiteSpace(userId) ? "unknown" : userId) },
                { "LogonType", logonType.ToString() },
                { "RunLevel", runLevel.ToString() },
                { "CredentialLead", GetScheduledCredentialLead(logonType) },
            };
        }
    }
}
