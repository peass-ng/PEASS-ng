using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using winPEAS.TaskScheduler;
using ScheduledTask = winPEAS.TaskScheduler.Task;
using TaskAction = winPEAS.TaskScheduler.Action;

namespace winPEAS.Info.ApplicationInfo
{
    internal sealed class PrivilegedScheduledTaskFinding
    {
        public string TaskPath { get; set; }
        public string Principal { get; set; }
        public string Executable { get; set; }
        public string TargetPath { get; set; }
        public string AccessReason { get; set; }
    }

    internal sealed class PrivilegedScheduledTaskReport
    {
        public List<PrivilegedScheduledTaskFinding> Findings { get; } = new List<PrivilegedScheduledTaskFinding>();
        public int FoldersInspected { get; set; }
        public int TasksInspected { get; set; }
        public bool FolderLimitReached { get; set; }
        public bool TaskLimitReached { get; set; }
        public bool TargetLimitReached { get; set; }
        public bool TimeLimitReached { get; set; }
        public bool FindingLimitReached { get; set; }
        public int TargetsInspected { get; set; }
        internal Stopwatch InspectionTimer { get; } = Stopwatch.StartNew();
    }

    internal static class PrivilegedScheduledTasks
    {
        internal const int MaxTasks = 2048;
        internal const int MaxFindings = 64;
        internal const int MaxFolders = 1024;
        internal const int MaxTargets = 2048;
        internal const int MaxInspectionMilliseconds = 15000;
        private const int MaxFolderDepth = 32;
        private const int MaxTargetsPerAction = 16;
        private const int MaxArgumentLength = 8192;

        private const uint FileWriteData = 0x00000002;
        private const uint FileAppendData = 0x00000004;
        private const uint FileDeleteChild = 0x00000040;
        private const uint WriteDac = 0x00040000;
        private const uint WriteOwner = 0x00080000;
        private const uint GenericWrite = 0x40000000;
        private const uint GenericAll = 0x10000000;

        private static readonly HashSet<string> ReplaceableScriptExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".ps1", ".psm1", ".bat", ".cmd", ".vbs", ".vbe", ".js", ".jse",
            ".wsf", ".wsh", ".hta", ".py", ".pl", ".rb",
        };

        private static readonly HashSet<string> HtaExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".hta",
        };

        private static readonly HashSet<string> PowerShellScriptExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".ps1", ".psm1",
        };

        private static readonly HashSet<string> CmdTargetExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".com", ".bat", ".cmd",
        };

        private static readonly HashSet<string> WindowsScriptHostExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh",
        };

        private static readonly HashSet<string> PythonScriptExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".py" };
        private static readonly HashSet<string> PerlScriptExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pl" };
        private static readonly HashSet<string> RubyScriptExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".rb" };
        private static readonly HashSet<string> JarExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jar" };
        private static readonly HashSet<string> DllExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".dll" };
        private static readonly HashSet<string> PythonInlineSwitches = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "-c", "-m", "-" };
        private static readonly HashSet<string> PerlRubyInlineSwitches = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "-e", "-E", "-" };

        public static PrivilegedScheduledTaskReport GetReport()
        {
            var report = new PrivilegedScheduledTaskReport();
            HashSet<string> unprivilegedSids = GetUnprivilegedTokenSids();

            try
            {
                using (TaskFolder root = TaskService.Instance.GetFolder(@"\"))
                {
                    ProcessFolder(root, 0, unprivilegedSids, report);
                }
            }
            catch
            {
                // Task Scheduler may be unavailable or access-restricted. Other checks should continue quietly.
            }

            return report;
        }

        private static void ProcessFolder(
            TaskFolder folder,
            int depth,
            ISet<string> unprivilegedSids,
            PrivilegedScheduledTaskReport report)
        {
            if (folder == null || depth > MaxFolderDepth || ShouldStop(report))
            {
                return;
            }

            if (report.FoldersInspected >= MaxFolders)
            {
                report.FolderLimitReached = true;
                return;
            }

            report.FoldersInspected++;

            try
            {
                using (TaskCollection tasks = folder.GetTasks())
                {
                    foreach (ScheduledTask task in tasks)
                    {
                        using (task)
                        {
                            if (ShouldStop(report))
                            {
                                return;
                            }

                            if (report.TasksInspected >= MaxTasks)
                            {
                                report.TaskLimitReached = true;
                                return;
                            }

                            report.TasksInspected++;
                            ProcessTask(task, unprivilegedSids, report);
                            if (ShouldStop(report))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // An inaccessible task must not prevent inspection of accessible folders.
            }

            try
            {
                using (TaskFolderCollection subFolders = folder.SubFolders)
                {
                    foreach (TaskFolder subFolder in subFolders)
                    {
                        using (subFolder)
                        {
                            ProcessFolder(subFolder, depth + 1, unprivilegedSids, report);
                        }

                        if (ShouldStop(report))
                        {
                            return;
                        }
                    }
                }
            }
            catch
            {
                // Some protected folders cannot be enumerated by a standard user.
            }
        }

        private static void ProcessTask(
            ScheduledTask task,
            ISet<string> unprivilegedSids,
            PrivilegedScheduledTaskReport report)
        {
            try
            {
                if (task == null || !task.Enabled || ShouldStop(report))
                {
                    return;
                }

                using (TaskDefinition definition = task.Definition)
                using (TaskPrincipal principal = definition.Principal)
                {
                    string principalName = principal.UserId;
                    if (string.IsNullOrWhiteSpace(principalName))
                    {
                        principalName = principal.Account;
                    }
                    if (!IsLocalSystemPrincipal(principalName))
                    {
                        return;
                    }

                    using (ActionCollection actions = definition.Actions)
                    {
                        foreach (TaskAction action in actions)
                        {
                            if (ShouldStop(report))
                            {
                                return;
                            }

                            using (action)
                            {
                                var execAction = action as TaskAction.ExecAction;
                                if (execAction != null)
                                {
                                    ProcessExecAction(task.Path, principalName, execAction, unprivilegedSids, report);
                                }
                            }

                            if (ShouldStop(report))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Definitions and individual actions can be access-restricted or malformed.
            }
        }

        private static void ProcessExecAction(
            string taskPath,
            string principal,
            TaskAction.ExecAction action,
            ISet<string> unprivilegedSids,
            PrivilegedScheduledTaskReport report)
        {
            string executable = ExpandAndTrim(action.Path);
            string arguments = ExpandAndTrim(action.Arguments);
            string workingDirectory = ExpandAndTrim(action.WorkingDirectory);
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string resolvedExecutable = ResolveActionPath(executable, workingDirectory);
            if (!string.IsNullOrEmpty(resolvedExecutable))
            {
                targets.Add(resolvedExecutable);
            }

            foreach (string referencedPath in ExtractReferencedFilePaths(executable, arguments, workingDirectory))
            {
                if (targets.Count >= MaxTargetsPerAction)
                {
                    break;
                }

                targets.Add(referencedPath);
            }

            foreach (string target in targets)
            {
                if (ShouldStop(report))
                {
                    return;
                }

                report.TargetsInspected++;
                string accessReason = GetWritableTargetReason(target, unprivilegedSids);
                if (string.IsNullOrEmpty(accessReason))
                {
                    continue;
                }

                report.Findings.Add(new PrivilegedScheduledTaskFinding
                {
                    TaskPath = taskPath,
                    Principal = principal,
                    Executable = executable,
                    TargetPath = target,
                    AccessReason = accessReason,
                });

                if (report.Findings.Count >= MaxFindings)
                {
                    report.FindingLimitReached = true;
                    return;
                }
            }
        }

        internal static bool IsLocalSystemPrincipal(string principal)
        {
            if (string.IsNullOrWhiteSpace(principal))
            {
                return false;
            }

            string normalized = principal.Trim().Replace('/', '\\');
            return normalized.Equals("S-1-5-18", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals(@"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase);
        }

        internal static List<string> ExtractReferencedFilePaths(
            string executable,
            string arguments,
            string workingDirectory)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string expandedArguments = ExpandAndTrim(arguments);
            string expandedWorkingDirectory = ExpandAndTrim(workingDirectory);
            if (string.IsNullOrEmpty(expandedArguments))
            {
                return new List<string>();
            }

            if (expandedArguments.Length > MaxArgumentLength)
            {
                expandedArguments = expandedArguments.Substring(0, MaxArgumentLength);
            }

            List<string> tokens = new List<string>(TokenizeArguments(expandedArguments));
            string executableName = GetFileName(executable).ToLowerInvariant();

            if (executableName == "powershell.exe" || executableName == "powershell" ||
                executableName == "pwsh.exe" || executableName == "pwsh")
            {
                AddSwitchValue(tokens, new[] { "-file", "-f" }, PowerShellScriptExtensions, expandedWorkingDirectory, paths);
            }
            else if (executableName == "cmd.exe" || executableName == "cmd")
            {
                AddCommandShellTarget(tokens, expandedWorkingDirectory, paths);
            }
            else if (executableName == "wscript.exe" || executableName == "wscript" ||
                executableName == "cscript.exe" || executableName == "cscript")
            {
                AddFirstPositional(tokens, WindowsScriptHostExtensions, expandedWorkingDirectory, paths, null);
            }
            else if (executableName == "mshta.exe" || executableName == "mshta")
            {
                AddFirstPositional(tokens, HtaExtensions, expandedWorkingDirectory, paths, null);
            }
            else if (executableName == "python.exe" || executableName == "python" ||
                executableName == "pythonw.exe" || executableName == "pythonw" ||
                Regex.IsMatch(executableName, @"^python\d+(?:\.\d+)?(?:w)?(?:\.exe)?$", RegexOptions.CultureInvariant))
            {
                AddFirstPositional(tokens, PythonScriptExtensions, expandedWorkingDirectory, paths, PythonInlineSwitches);
            }
            else if (executableName == "perl.exe" || executableName == "perl")
            {
                AddFirstPositional(tokens, PerlScriptExtensions, expandedWorkingDirectory, paths, PerlRubyInlineSwitches);
            }
            else if (executableName == "ruby.exe" || executableName == "ruby")
            {
                AddFirstPositional(tokens, RubyScriptExtensions, expandedWorkingDirectory, paths, PerlRubyInlineSwitches);
            }
            else if (executableName == "java.exe" || executableName == "java" ||
                executableName == "javaw.exe" || executableName == "javaw")
            {
                AddSwitchValue(tokens, new[] { "-jar" }, JarExtensions, expandedWorkingDirectory, paths);
            }
            else if (executableName == "rundll32.exe" || executableName == "rundll32" ||
                executableName == "regsvr32.exe" || executableName == "regsvr32")
            {
                AddFirstPositional(tokens, DllExtensions, expandedWorkingDirectory, paths, null);
            }

            return new List<string>(paths);
        }

        private static void AddSwitchValue(
            IList<string> tokens,
            IEnumerable<string> switchNames,
            ISet<string> allowedExtensions,
            string workingDirectory,
            ISet<string> paths)
        {
            var switches = new HashSet<string>(switchNames, StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < tokens.Count; index++)
            {
                string token = tokens[index];
                if (switches.Contains(token) && index + 1 < tokens.Count)
                {
                    AddExecutionCandidate(tokens[index + 1], allowedExtensions, workingDirectory, paths);
                    return;
                }

                foreach (string switchName in switches)
                {
                    string prefix = switchName + ":";
                    if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        AddExecutionCandidate(token.Substring(prefix.Length), allowedExtensions, workingDirectory, paths);
                        return;
                    }
                }
            }
        }

        private static void AddCommandShellTarget(
            IList<string> tokens,
            string workingDirectory,
            ISet<string> paths)
        {
            for (int index = 0; index + 1 < tokens.Count; index++)
            {
                if (!tokens[index].Equals("/c", StringComparison.OrdinalIgnoreCase) &&
                    !tokens[index].Equals("/k", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int candidateIndex = index + 1;
                if (tokens[candidateIndex].Equals("call", StringComparison.OrdinalIgnoreCase) &&
                    candidateIndex + 1 < tokens.Count)
                {
                    candidateIndex++;
                }

                AddExecutionCandidate(tokens[candidateIndex].TrimStart('&'), CmdTargetExtensions, workingDirectory, paths);
                return;
            }
        }

        private static void AddFirstPositional(
            IEnumerable<string> tokens,
            ISet<string> allowedExtensions,
            string workingDirectory,
            ISet<string> paths,
            ISet<string> terminatingSwitches)
        {
            foreach (string token in tokens)
            {
                if (terminatingSwitches != null && terminatingSwitches.Contains(token))
                {
                    return;
                }

                if (token.StartsWith("-", StringComparison.Ordinal) || token.StartsWith("/", StringComparison.Ordinal))
                {
                    continue;
                }

                AddExecutionCandidate(token.Split(',')[0], allowedExtensions, workingDirectory, paths);
                return;
            }
        }

        private static void AddExecutionCandidate(
            string value,
            ISet<string> allowedExtensions,
            string workingDirectory,
            ISet<string> paths)
        {
            string candidate = CleanCandidatePath(value);
            if (string.IsNullOrEmpty(candidate) || !allowedExtensions.Contains(Path.GetExtension(candidate)))
            {
                return;
            }

            if (IsLocalAbsolutePath(candidate))
            {
                paths.Add(candidate);
                return;
            }

            if (!IsLocalAbsolutePath(workingDirectory))
            {
                return;
            }

            try
            {
                paths.Add(Path.GetFullPath(Path.Combine(workingDirectory, candidate)));
            }
            catch
            {
                // Ignore malformed relative paths.
            }
        }

        private static IEnumerable<string> TokenizeArguments(string arguments)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            char quote = '\0';

            foreach (char character in arguments)
            {
                if ((character == '"' || character == '\'') && (quote == '\0' || quote == character))
                {
                    quote = quote == '\0' ? character : '\0';
                    continue;
                }

                if (char.IsWhiteSpace(character) && quote == '\0')
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        if (tokens.Count >= 128)
                        {
                            return tokens;
                        }
                    }
                }
                else
                {
                    current.Append(character);
                }
            }

            if (current.Length > 0 && tokens.Count < 128)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }

        private static string ResolveActionPath(string executable, string workingDirectory)
        {
            if (string.IsNullOrEmpty(executable))
            {
                return null;
            }

            string candidate = CleanCandidatePath(executable);
            if (IsLocalAbsolutePath(candidate))
            {
                return candidate;
            }

            if (!IsLocalAbsolutePath(workingDirectory))
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(Path.Combine(workingDirectory, candidate));
            }
            catch
            {
                return null;
            }
        }

        private static string GetWritableTargetReason(string displayPath, ISet<string> unprivilegedSids)
        {
            if (!IsPathOnLocalDrive(displayPath) || unprivilegedSids == null || unprivilegedSids.Count == 0)
            {
                return null;
            }

            try
            {
                string accessPath = GetAccessPath(displayPath);
                if (File.Exists(accessPath))
                {
                    var descriptor = GetFileSecurityDescriptor(accessPath);
                    string trustee = FindWriteTrustee(descriptor, unprivilegedSids, FileWriteData | WriteDac | WriteOwner);
                    if (string.IsNullOrEmpty(trustee) && ReplaceableScriptExtensions.Contains(Path.GetExtension(accessPath)))
                    {
                        trustee = FindWriteTrustee(descriptor, unprivilegedSids, FileAppendData);
                    }

                    if (!string.IsNullOrEmpty(trustee))
                    {
                        return "file is writable by " + trustee;
                    }

                    string parent = Path.GetDirectoryName(accessPath);
                    string replacementTrustee = GetDirectoryReplacementTrustee(parent, unprivilegedSids);
                    return string.IsNullOrEmpty(replacementTrustee)
                        ? null
                        : "parent permits file replacement by " + replacementTrustee;
                }

                string missingParent = Path.GetDirectoryName(accessPath);
                if (string.IsNullOrEmpty(missingParent) || !Directory.Exists(missingParent))
                {
                    return null;
                }

                string createTrustee = FindWriteTrustee(
                    GetDirectorySecurityDescriptor(missingParent),
                    unprivilegedSids,
                    FileWriteData | WriteDac | WriteOwner);
                return string.IsNullOrEmpty(createTrustee)
                    ? null
                    : "missing target can be created by " + createTrustee;
            }
            catch
            {
                return null;
            }
        }

        private static string GetDirectoryReplacementTrustee(string directoryPath, ISet<string> unprivilegedSids)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                return null;
            }

            RawSecurityDescriptor descriptor = GetDirectorySecurityDescriptor(directoryPath);
            return FindDirectoryReplacementTrustee(descriptor, unprivilegedSids);
        }

        internal static string FindDirectoryReplacementTrustee(
            RawSecurityDescriptor descriptor,
            ISet<string> unprivilegedSids)
        {
            string controlTrustee = FindWriteTrustee(descriptor, unprivilegedSids, WriteDac | WriteOwner);
            if (!string.IsNullOrEmpty(controlTrustee))
            {
                return controlTrustee;
            }

            foreach (string sid in unprivilegedSids)
            {
                var singleSid = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sid };
                if (!string.IsNullOrEmpty(FindWriteTrustee(descriptor, singleSid, FileWriteData)) &&
                    !string.IsNullOrEmpty(FindWriteTrustee(descriptor, singleSid, FileDeleteChild)))
                {
                    return sid;
                }
            }

            return null;
        }

        internal static string FindWriteTrustee(
            RawSecurityDescriptor descriptor,
            ISet<string> enabledSids,
            uint desiredRights)
        {
            if (descriptor == null || enabledSids == null || enabledSids.Count == 0 || desiredRights == 0)
            {
                return null;
            }

            if ((descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) == 0 ||
                descriptor.DiscretionaryAcl == null)
            {
                return enabledSids.Contains("S-1-1-0") ? "S-1-1-0" : null;
            }

            foreach (uint desiredRight in IndividualRights(desiredRights))
            {
                foreach (GenericAce ace in descriptor.DiscretionaryAcl)
                {
                    if ((ace.AceFlags & AceFlags.InheritOnly) != 0)
                    {
                        continue;
                    }

                    var qualifiedAce = ace as QualifiedAce;
                    var knownAce = ace as KnownAce;
                    if (qualifiedAce == null || knownAce == null || qualifiedAce.SecurityIdentifier == null ||
                        !enabledSids.Contains(qualifiedAce.SecurityIdentifier.Value) ||
                        !GrantsRight(unchecked((uint)knownAce.AccessMask), desiredRight))
                    {
                        continue;
                    }

                    if (qualifiedAce.AceQualifier == AceQualifier.AccessDenied)
                    {
                        break;
                    }

                    if (qualifiedAce.AceQualifier == AceQualifier.AccessAllowed)
                    {
                        return qualifiedAce.SecurityIdentifier.Value;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<uint> IndividualRights(uint rights)
        {
            uint[] candidates = { FileWriteData, FileAppendData, FileDeleteChild, WriteDac, WriteOwner };
            foreach (uint candidate in candidates)
            {
                if ((rights & candidate) != 0)
                {
                    yield return candidate;
                }
            }
        }

        private static bool GrantsRight(uint mask, uint desiredRight)
        {
            if ((mask & GenericAll) != 0 || (mask & desiredRight) != 0)
            {
                return true;
            }

            return (mask & GenericWrite) != 0 &&
                (desiredRight == FileWriteData || desiredRight == FileAppendData);
        }

        private static RawSecurityDescriptor GetFileSecurityDescriptor(string path)
        {
            FileSecurity security = File.GetAccessControl(path, AccessControlSections.Access);
            return FromBinarySecurityDescriptor(security.GetSecurityDescriptorBinaryForm());
        }

        private static RawSecurityDescriptor GetDirectorySecurityDescriptor(string path)
        {
            DirectorySecurity security = Directory.GetAccessControl(path, AccessControlSections.Access);
            return FromBinarySecurityDescriptor(security.GetSecurityDescriptorBinaryForm());
        }

        private static RawSecurityDescriptor FromBinarySecurityDescriptor(byte[] binaryDescriptor)
        {
            return binaryDescriptor == null || binaryDescriptor.Length == 0
                ? null
                : new RawSecurityDescriptor(binaryDescriptor, 0);
        }

        private static HashSet<string> GetUnprivilegedTokenSids()
        {
            var sids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "S-1-1-0",      // Everyone
                "S-1-5-4",      // Interactive
                "S-1-5-11",     // Authenticated Users
                "S-1-5-32-545", // BUILTIN\Users
            };

            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    var administratorsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                    bool privileged = principal.IsInRole(administratorsSid) ||
                        (identity.User != null && IsPrivilegedServiceSid(identity.User.Value));
                    if (!privileged && identity.User != null)
                    {
                        sids.Add(identity.User.Value);
                    }

                    if (identity.Groups != null)
                    {
                        foreach (IdentityReference group in identity.Groups)
                        {
                            var sid = group as SecurityIdentifier;
                            if (sid != null && principal.IsInRole(sid) && !sid.Equals(administratorsSid) &&
                                !IsPrivilegedServiceSid(sid.Value))
                            {
                                sids.Add(sid.Value);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Well-known low-privilege principals still provide useful configuration auditing.
            }

            return sids;
        }

        private static bool IsPrivilegedServiceSid(string sid)
        {
            return string.Equals(sid, "S-1-5-18", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sid, "S-1-5-19", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sid, "S-1-5-20", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetAccessPath(string displayPath)
        {
            if (!Environment.Is64BitOperatingSystem || Environment.Is64BitProcess)
            {
                return displayPath;
            }

            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            if (string.IsNullOrEmpty(systemRoot))
            {
                return displayPath;
            }

            string system32 = Path.Combine(systemRoot.TrimEnd('\\'), "System32") + "\\";
            if (!displayPath.StartsWith(system32, StringComparison.OrdinalIgnoreCase))
            {
                return displayPath;
            }

            return Path.Combine(systemRoot, "Sysnative") + "\\" + displayPath.Substring(system32.Length);
        }

        private static string ExpandAndTrim(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Environment.ExpandEnvironmentVariables(value).Trim();
        }

        private static string CleanCandidatePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim().Trim('"', '\'').Replace('/', '\\');
        }

        private static string GetFileName(string path)
        {
            string candidate = CleanCandidatePath(path) ?? string.Empty;
            int separator = Math.Max(candidate.LastIndexOf('\\'), candidate.LastIndexOf('/'));
            return separator >= 0 ? candidate.Substring(separator + 1) : candidate;
        }

        private static bool IsWindowsAbsolutePath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                ((path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\') ||
                 path.StartsWith(@"\\", StringComparison.Ordinal));
        }

        private static bool IsLocalAbsolutePath(string path)
        {
            return IsWindowsAbsolutePath(path) && !path.StartsWith(@"\\", StringComparison.Ordinal);
        }

        private static bool IsPathOnLocalDrive(string path)
        {
            if (!IsLocalAbsolutePath(path))
            {
                return false;
            }

            try
            {
                string root = Path.GetPathRoot(path);
                return !string.IsNullOrEmpty(root) && IsAllowedDriveType(new DriveInfo(root).DriveType);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsAllowedDriveType(DriveType driveType)
        {
            return driveType != DriveType.Network &&
                driveType != DriveType.NoRootDirectory &&
                driveType != DriveType.Unknown;
        }

        private static bool ShouldStop(PrivilegedScheduledTaskReport report)
        {
            if (report == null)
            {
                return true;
            }

            return ApplySafetyLimits(report, report.InspectionTimer.ElapsedMilliseconds);
        }

        internal static bool ApplySafetyLimits(PrivilegedScheduledTaskReport report, long elapsedMilliseconds)
        {
            if (report == null)
            {
                return true;
            }

            if (elapsedMilliseconds >= MaxInspectionMilliseconds)
            {
                report.TimeLimitReached = true;
            }

            if (report.TargetsInspected >= MaxTargets)
            {
                report.TargetLimitReached = true;
            }

            return report.FolderLimitReached || report.TaskLimitReached ||
                report.TargetLimitReached || report.TimeLimitReached || report.FindingLimitReached;
        }

    }
}
