using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.Info.ApplicationInfo;
using winPEAS.TaskScheduler;
using ScheduledTask = winPEAS.TaskScheduler.Task;

namespace winPEAS.Info.SystemInfo
{
    internal static class CrossDeviceComCve66804
    {
        internal const string CrossDeviceClsid = "{E9F83CF2-E0C0-4CA7-AF01-E90C70BEF496}";
        internal const string ShellCreateObjectClsid = "{135FD325-45B7-4C30-89F8-4386961669F0}";
        internal const string CreateObjectTaskPath = @"\Microsoft\Windows\Shell\CreateObjectTask";

        private const uint FileWriteData = 0x00000002;
        private const uint FileAppendData = 0x00000004;
        private const uint ExecuteFile = 0x00000020;
        private const uint WriteDac = 0x00040000;
        private const uint WriteOwner = 0x00080000;
        private const uint GenericAll = 0x10000000;
        private const uint GenericExecute = 0x20000000;

        // Microsoft Security Response Center, August 2026 security update.
        private static readonly Dictionary<int, FixedBuild> FixedBuilds = new Dictionary<int, FixedBuild>
        {
            { 19045, new FixedBuild(7663, "5120249") },
            { 26100, new FixedBuild(9168, "5121003") },
            { 26200, new FixedBuild(9168, "5121003") },
            { 28000, new FixedBuild(2704, "5121000") },
        };

        internal static CrossDeviceComReport GetReport(Dictionary<string, string> basicInfo)
        {
            var report = new CrossDeviceComReport
            {
                ProductName = GetValue(basicInfo, "ProductName"),
                Architecture = GetValue(basicInfo, "Architecture"),
                InstalledHotfixes = GetValue(basicInfo, "Hotfixes")
            };

            CollectPatchState(report, basicInfo);

            HashSet<string> unprivilegedSids = PermissionsHelper.GetUnprivilegedTokenSids();
            CollectCrossDeviceRegistrations(report, unprivilegedSids);
            CollectShellCreateObjectRegistration(report);
            CollectCreateObjectTask(report, unprivilegedSids);
            return report;
        }

        private static void CollectPatchState(CrossDeviceComReport report, Dictionary<string, string> basicInfo)
        {
            int build;
            int revision;
            if (!int.TryParse(GetValue(basicInfo, "CurrentBuild"), out build) ||
                !int.TryParse(GetValue(basicInfo, "UpdateBuildRevision"), out revision))
            {
                report.PatchStatus = CrossDevicePatchStatus.Unknown;
                report.PatchEvidence = "Unable to parse CurrentBuild/UBR.";
                return;
            }

            report.Build = build;
            report.Revision = revision;
            report.BuildVersion = build + "." + revision;

            FixedBuild fixedBuild;
            if (!FixedBuilds.TryGetValue(build, out fixedBuild))
            {
                report.PatchStatus = CrossDevicePatchStatus.NotAffected;
                report.PatchEvidence = "Microsoft does not list this Windows build line for CVE-2026-66804.";
                return;
            }

            report.BuildApplicable = true;
            report.FixedRevision = fixedBuild.Revision;
            report.FixedVersion = build + "." + fixedBuild.Revision;
            report.ApplicableKb = fixedBuild.Kb;

            if (ContainsHotfix(report.InstalledHotfixes, fixedBuild.Kb))
            {
                report.PatchStatus = CrossDevicePatchStatus.Patched;
                report.DirectFixInstalled = true;
                report.PatchEvidence = "The applicable August 2026 security update is present in the hotfix inventory.";
            }
            else if (revision >= fixedBuild.Revision)
            {
                report.PatchStatus = CrossDevicePatchStatus.Patched;
                report.PatchEvidence = "The OS build is at or above Microsoft's cumulative-update fixed build.";
            }
            else
            {
                report.PatchStatus = CrossDevicePatchStatus.Susceptible;
                report.PatchEvidence = "The OS build is below Microsoft's fixed build and the applicable fix KB was not found.";
            }
        }

        private static void CollectCrossDeviceRegistrations(
            CrossDeviceComReport report,
            ISet<string> unprivilegedSids)
        {
            foreach (RegistryView view in GetRegistryViews())
            {
                var registration = new CrossDeviceComRegistration
                {
                    RegistryView = view == RegistryView.Registry64 ? "64-bit" : "32-bit"
                };
                report.CrossDeviceRegistrations.Add(registration);

                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey key = baseKey.OpenSubKey(
                        @"SOFTWARE\Classes\CLSID\" + CrossDeviceClsid + @"\InprocServer32"))
                    {
                        if (key == null)
                        {
                            continue;
                        }

                        registration.Present = true;
                        object value = key.GetValue(null, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        registration.RawServerPath = value == null ? "" : value.ToString();
                        registration.ResolvedServerPath = ResolveServerPath(registration.RawServerPath);
                        registration.MatchesExpectedPath = IsExpectedCrossDevicePath(registration.ResolvedServerPath);
                        registration.ServerExists = File.Exists(registration.ResolvedServerPath);
                        CollectPathControl(registration, unprivilegedSids);
                    }
                }
                catch (Exception ex)
                {
                    report.CollectionErrors.Add(registration.RegistryView + " CrossDevice registration: " + ex.Message);
                }
            }
        }

        private static void CollectPathControl(
            CrossDeviceComRegistration registration,
            ISet<string> unprivilegedSids)
        {
            if (string.IsNullOrWhiteSpace(registration.ResolvedServerPath))
            {
                return;
            }

            try
            {
                if (registration.ServerExists)
                {
                    registration.PathControlReason = PrivilegedScheduledTasks.GetWritableTargetReason(
                        registration.ResolvedServerPath,
                        unprivilegedSids);
                    registration.NearestExistingParent = Path.GetDirectoryName(registration.ResolvedServerPath);
                    registration.ParentAclSddl = File.GetAccessControl(
                        registration.ResolvedServerPath,
                        AccessControlSections.Access)
                        .GetSecurityDescriptorSddlForm(AccessControlSections.Access);
                    return;
                }

                string targetParent = Path.GetDirectoryName(registration.ResolvedServerPath);
                string existingParent = targetParent;
                while (!string.IsNullOrEmpty(existingParent) && !Directory.Exists(existingParent))
                {
                    DirectoryInfo parent = Directory.GetParent(existingParent);
                    existingParent = parent == null ? null : parent.FullName;
                }

                registration.NearestExistingParent = existingParent ?? "";
                if (string.IsNullOrEmpty(existingParent))
                {
                    return;
                }

                DirectorySecurity security = Directory.GetAccessControl(
                    existingParent,
                    AccessControlSections.Access);
                registration.ParentAclSddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.Access);
                var descriptor = new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0);

                uint createRight = string.Equals(existingParent, targetParent, StringComparison.OrdinalIgnoreCase)
                    ? FileWriteData
                    : FileAppendData;
                string trustee = PrivilegedScheduledTasks.FindWriteTrustee(
                    descriptor,
                    unprivilegedSids,
                    createRight | WriteDac | WriteOwner);
                if (!string.IsNullOrEmpty(trustee))
                {
                    registration.PathControlReason = string.Equals(existingParent, targetParent, StringComparison.OrdinalIgnoreCase)
                        ? "missing DLL can be created by " + trustee
                        : "missing directory chain and DLL can be created below " + existingParent + " by " + trustee;
                }
            }
            catch (Exception ex)
            {
                registration.CollectionError = ex.Message;
            }
        }

        private static void CollectShellCreateObjectRegistration(CrossDeviceComReport report)
        {
            foreach (RegistryView view in GetRegistryViews())
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey classKey = baseKey.OpenSubKey(
                        @"SOFTWARE\Classes\CLSID\" + ShellCreateObjectClsid))
                    {
                        if (classKey == null)
                        {
                            continue;
                        }

                        report.ShellClassPresent = true;
                        report.ShellClassRegistryView = view == RegistryView.Registry64 ? "64-bit" : "32-bit";
                        report.ShellClassName = Convert.ToString(classKey.GetValue(null, ""));
                        report.ShellAppId = Convert.ToString(classKey.GetValue("AppID", ""));
                        if (string.IsNullOrWhiteSpace(report.ShellAppId))
                        {
                            report.ShellAppId = ShellCreateObjectClsid;
                        }

                        using (RegistryKey appIdKey = baseKey.OpenSubKey(
                            @"SOFTWARE\Classes\AppID\" + report.ShellAppId))
                        {
                            if (appIdKey != null)
                            {
                                report.ShellRunAs = Convert.ToString(appIdKey.GetValue("RunAs", ""));
                                report.ShellRunsAsSystem = IsLocalSystem(report.ShellRunAs);
                            }
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    report.CollectionErrors.Add(
                        (view == RegistryView.Registry64 ? "64-bit" : "32-bit") +
                        " Shell Create Object registration: " + ex.Message);
                }
            }
        }

        private static void CollectCreateObjectTask(
            CrossDeviceComReport report,
            ISet<string> unprivilegedSids)
        {
            try
            {
                using (ScheduledTask task = TaskService.Instance.GetTask(CreateObjectTaskPath))
                {
                    if (task == null)
                    {
                        return;
                    }

                    report.TaskPresent = true;
                    report.TaskEnabled = task.Enabled;
                    report.TaskState = task.State.ToString();

                    using (TaskDefinition definition = task.Definition)
                    using (TaskPrincipal principal = definition.Principal)
                    {
                        report.TaskPrincipal = string.IsNullOrWhiteSpace(principal.UserId)
                            ? principal.Account
                            : principal.UserId;
                        report.TaskRunsAsSystem = PrivilegedScheduledTasks.IsLocalSystemPrincipal(report.TaskPrincipal);
                    }

                    string sddl = task.GetSecurityDescriptorSddlForm(
                        SecurityInfos.Owner | SecurityInfos.DiscretionaryAcl);
                    if (!string.IsNullOrWhiteSpace(sddl))
                    {
                        report.TaskExecuteAclKnown = true;
                        report.TaskAclSddl = sddl;
                        report.TaskExecuteTrustee = FindTaskExecuteTrustee(
                            new RawSecurityDescriptor(sddl),
                            unprivilegedSids);
                        report.TaskRunnableByLowPrivilege = !string.IsNullOrEmpty(report.TaskExecuteTrustee);
                    }
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("CreateObjectTask: " + ex.Message);
            }
        }

        internal static string FindTaskExecuteTrustee(
            RawSecurityDescriptor descriptor,
            ISet<string> enabledSids)
        {
            if (descriptor == null || enabledSids == null || enabledSids.Count == 0)
            {
                return null;
            }

            if ((descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) == 0 ||
                descriptor.DiscretionaryAcl == null)
            {
                return enabledSids.Contains("S-1-1-0") ? "S-1-1-0" : null;
            }

            string allowingTrustee = null;
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
                    !GrantsTaskExecute(unchecked((uint)knownAce.AccessMask)))
                {
                    continue;
                }

                if (qualifiedAce.AceQualifier == AceQualifier.AccessDenied)
                {
                    return null;
                }

                if (qualifiedAce.AceQualifier == AceQualifier.AccessAllowed && allowingTrustee == null)
                {
                    allowingTrustee = qualifiedAce.SecurityIdentifier.Value;
                }
            }

            return allowingTrustee;
        }

        private static bool GrantsTaskExecute(uint mask)
        {
            return (mask & (GenericAll | GenericExecute | ExecuteFile)) != 0;
        }

        private static string ResolveServerPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return "";
            }

            string expanded = Environment.ExpandEnvironmentVariables(rawPath).Trim().Trim('"');
            try
            {
                return Path.GetFullPath(expanded);
            }
            catch
            {
                return expanded;
            }
        }

        private static bool IsExpectedCrossDevicePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (string.IsNullOrWhiteSpace(programData))
                {
                    programData = Environment.GetEnvironmentVariable("ProgramData");
                }
                if (string.IsNullOrWhiteSpace(programData))
                {
                    return false;
                }

                string expected = Path.GetFullPath(Path.Combine(
                    programData,
                    "CrossDevice",
                    "CrossDevice.Streaming.Source.dll"));
                return string.Equals(Path.GetFullPath(path), expected, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsHotfix(string hotfixes, string kb)
        {
            return !string.IsNullOrEmpty(hotfixes) && !string.IsNullOrEmpty(kb) &&
                hotfixes.IndexOf("KB" + kb, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLocalSystem(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim().Replace('/', '\\');
            return normalized.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals(@"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("S-1-5-18", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<RegistryView> GetRegistryViews()
        {
            yield return RegistryView.Registry64;
            yield return RegistryView.Registry32;
        }

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            string value;
            return values != null && values.TryGetValue(key, out value) ? value ?? "" : "";
        }

        private sealed class FixedBuild
        {
            internal FixedBuild(int revision, string kb)
            {
                Revision = revision;
                Kb = kb;
            }

            internal int Revision { get; private set; }
            internal string Kb { get; private set; }
        }
    }

    internal enum CrossDevicePatchStatus
    {
        Unknown,
        NotAffected,
        Susceptible,
        Patched,
    }

    internal sealed class CrossDeviceComRegistration
    {
        public string RegistryView { get; set; } = "";
        public bool Present { get; set; }
        public string RawServerPath { get; set; } = "";
        public string ResolvedServerPath { get; set; } = "";
        public bool MatchesExpectedPath { get; set; }
        public bool ServerExists { get; set; }
        public string PathControlReason { get; set; } = "";
        public string NearestExistingParent { get; set; } = "";
        public string ParentAclSddl { get; set; } = "";
        public string CollectionError { get; set; } = "";

        public bool IsControllableRegistration =>
            Present && MatchesExpectedPath && !string.IsNullOrEmpty(PathControlReason);
    }

    internal sealed class CrossDeviceComReport
    {
        public string ProductName { get; set; } = "";
        public string Architecture { get; set; } = "";
        public string InstalledHotfixes { get; set; } = "";
        public int Build { get; set; }
        public int Revision { get; set; }
        public string BuildVersion { get; set; } = "";
        public bool BuildApplicable { get; set; }
        public int FixedRevision { get; set; }
        public string FixedVersion { get; set; } = "";
        public string ApplicableKb { get; set; } = "";
        public bool DirectFixInstalled { get; set; }
        public CrossDevicePatchStatus PatchStatus { get; set; }
        public string PatchEvidence { get; set; } = "";
        public List<CrossDeviceComRegistration> CrossDeviceRegistrations { get; } =
            new List<CrossDeviceComRegistration>();
        public bool ShellClassPresent { get; set; }
        public string ShellClassRegistryView { get; set; } = "";
        public string ShellClassName { get; set; } = "";
        public string ShellAppId { get; set; } = "";
        public string ShellRunAs { get; set; } = "";
        public bool ShellRunsAsSystem { get; set; }
        public bool TaskPresent { get; set; }
        public bool TaskEnabled { get; set; }
        public string TaskState { get; set; } = "";
        public string TaskPrincipal { get; set; } = "";
        public bool TaskRunsAsSystem { get; set; }
        public bool TaskExecuteAclKnown { get; set; }
        public bool TaskRunnableByLowPrivilege { get; set; }
        public string TaskExecuteTrustee { get; set; } = "";
        public string TaskAclSddl { get; set; } = "";
        public List<string> CollectionErrors { get; } = new List<string>();

        public bool HasControllableRegistration =>
            CrossDeviceRegistrations.Exists(registration => registration.IsControllableRegistration);

        public bool CompleteStaticChain =>
            PatchStatus == CrossDevicePatchStatus.Susceptible &&
            HasControllableRegistration &&
            ShellClassPresent && ShellRunsAsSystem &&
            TaskPresent && TaskEnabled && TaskRunsAsSystem && TaskRunnableByLowPrivilege;
    }
}
