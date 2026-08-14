using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Native;

namespace winPEAS.Info.FilesInfo
{
    internal enum RegistryHiveExposureKind
    {
        UnsafeLiveAcl,
        ReadableBackup,
        ReadableShadowCopy,
    }

    internal sealed class RegistryHiveExposureFinding
    {
        public RegistryHiveExposureKind Kind { get; set; }
        public string HiveName { get; set; }
        public string Path { get; set; }
        public string Trustee { get; set; }
    }

    internal sealed class RegistryHiveExposureReport
    {
        public List<RegistryHiveExposureFinding> Findings { get; } = new List<RegistryHiveExposureFinding>();
        public int ShadowCopiesChecked { get; set; }
        public bool ShadowCopyEnumerationSucceeded { get; set; }
        public bool ShadowDeviceProbeUsed { get; set; }
        public bool ShadowCopyLimitReached { get; set; }
    }

    internal static class RegistryHiveExposure
    {
        internal const int MaxShadowCopies = 64;

        private const uint GenericRead = 0x80000000;
        private const uint GenericAll = 0x10000000;
        private const uint FileReadData = 0x00000001;
        private const uint OpenExisting = 3;
        private const uint ShareReadWriteDelete = 0x00000007;

        private static readonly string[] HiveNames = { "SAM", "SECURITY", "SYSTEM" };
        private const string ShadowDevicePrefix = @"\\?\GLOBALROOT\Device\HarddiskVolumeShadowCopy";

        public static RegistryHiveExposureReport GetReport(IEnumerable<string> backupPaths)
        {
            var report = new RegistryHiveExposureReport();
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            if (string.IsNullOrWhiteSpace(systemRoot))
            {
                return report;
            }

            bool isPrivilegedContext;
            HashSet<string> unprivilegedSids = GetUnprivilegedTokenSids(out isPrivilegedContext);
            CheckLiveHiveAcls(systemRoot, unprivilegedSids, report);
            CheckBackupHives(backupPaths, unprivilegedSids, isPrivilegedContext, report);
            CheckShadowCopies(systemRoot, unprivilegedSids, isPrivilegedContext, report);
            return report;
        }

        private static void CheckLiveHiveAcls(
            string systemRoot,
            HashSet<string> unprivilegedSids,
            RegistryHiveExposureReport report)
        {
            string displayConfigDirectory = Path.Combine(systemRoot, "System32", "config");
            string accessSystemDirectory = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                ? "Sysnative"
                : "System32";
            string accessConfigDirectory = Path.Combine(systemRoot, accessSystemDirectory, "config");

            foreach (string hiveName in HiveNames)
            {
                string hivePath = Path.Combine(displayConfigDirectory, hiveName);
                string accessPath = Path.Combine(accessConfigDirectory, hiveName);
                string trustee = GetReadTrustee(accessPath, unprivilegedSids);
                if (!string.IsNullOrEmpty(trustee))
                {
                    report.Findings.Add(new RegistryHiveExposureFinding
                    {
                        Kind = RegistryHiveExposureKind.UnsafeLiveAcl,
                        HiveName = hiveName,
                        Path = hivePath,
                        Trustee = trustee,
                    });
                }
            }
        }

        private static void CheckBackupHives(
            IEnumerable<string> backupPaths,
            HashSet<string> unprivilegedSids,
            bool isPrivilegedContext,
            RegistryHiveExposureReport report)
        {
            if (backupPaths == null)
            {
                return;
            }

            foreach (string backupPath in backupPaths)
            {
                if (string.IsNullOrWhiteSpace(backupPath))
                {
                    continue;
                }

                string trustee = GetReadTrustee(backupPath, unprivilegedSids);
                if (string.IsNullOrEmpty(trustee) && (isPrivilegedContext || !CanOpenForRead(backupPath)))
                {
                    continue;
                }

                report.Findings.Add(new RegistryHiveExposureFinding
                {
                    Kind = RegistryHiveExposureKind.ReadableBackup,
                    HiveName = Path.GetFileName(backupPath),
                    Path = backupPath,
                    Trustee = trustee,
                });
            }
        }

        private static void CheckShadowCopies(
            string systemRoot,
            HashSet<string> unprivilegedSids,
            bool isPrivilegedContext,
            RegistryHiveExposureReport report)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DeviceObject FROM Win32_ShadowCopy"))
                using (ManagementObjectCollection shadowCopies = searcher.Get())
                {
                    report.ShadowCopyEnumerationSucceeded = true;
                    foreach (ManagementObject shadowCopy in shadowCopies)
                    {
                        if (report.ShadowCopiesChecked >= MaxShadowCopies)
                        {
                            report.ShadowCopyLimitReached = true;
                            break;
                        }

                        string deviceObject = shadowCopy["DeviceObject"] as string;
                        if (!IsExpectedShadowDevicePath(deviceObject))
                        {
                            continue;
                        }

                        CheckShadowDevice(deviceObject, systemRoot, unprivilegedSids, isPrivilegedContext, report);
                    }
                }
            }
            catch
            {
                // WMI is often restricted to administrators. Bounded device-name probing still
                // verifies any snapshot that the current token can actually read.
                report.ShadowCopyEnumerationSucceeded = false;
                report.ShadowCopiesChecked = 0;
                report.Findings.RemoveAll(finding => finding.Kind == RegistryHiveExposureKind.ReadableShadowCopy);
                ProbeShadowDevices(systemRoot, unprivilegedSids, isPrivilegedContext, report);
            }
        }

        private static void ProbeShadowDevices(
            string systemRoot,
            HashSet<string> unprivilegedSids,
            bool isPrivilegedContext,
            RegistryHiveExposureReport report)
        {
            report.ShadowDeviceProbeUsed = true;
            for (int shadowNumber = 1; shadowNumber <= MaxShadowCopies; shadowNumber++)
            {
                string deviceObject = ShadowDevicePrefix + shadowNumber;
                CheckShadowDevice(deviceObject, systemRoot, unprivilegedSids, isPrivilegedContext, report);
            }

            report.ShadowCopyLimitReached = true;
        }

        private static void CheckShadowDevice(
            string deviceObject,
            string systemRoot,
            HashSet<string> unprivilegedSids,
            bool isPrivilegedContext,
            RegistryHiveExposureReport report)
        {
            report.ShadowCopiesChecked++;
            foreach (string hiveName in HiveNames)
            {
                string hivePath = BuildShadowHivePath(deviceObject, systemRoot, hiveName);
                if (string.IsNullOrEmpty(hivePath))
                {
                    continue;
                }

                string trustee = GetReadTrustee(hivePath, unprivilegedSids);
                if (string.IsNullOrEmpty(trustee) && (isPrivilegedContext || !CanOpenForRead(hivePath)))
                {
                    continue;
                }

                report.Findings.Add(new RegistryHiveExposureFinding
                {
                    Kind = RegistryHiveExposureKind.ReadableShadowCopy,
                    HiveName = hiveName,
                    Path = hivePath,
                    Trustee = trustee,
                });
            }
        }

        internal static string BuildShadowHivePath(string deviceObject, string systemRoot, string hiveName)
        {
            if (!IsExpectedShadowDevicePath(deviceObject) || string.IsNullOrWhiteSpace(systemRoot) ||
                string.IsNullOrWhiteSpace(hiveName) || systemRoot.Length < 4 || systemRoot[1] != ':' ||
                (systemRoot[2] != '\\' && systemRoot[2] != '/'))
            {
                return null;
            }

            string relativeWindowsDirectory = systemRoot.Substring(3).Trim('\\', '/');
            if (string.IsNullOrWhiteSpace(relativeWindowsDirectory) || relativeWindowsDirectory.Contains("..") ||
                hiveName.IndexOfAny(new[] { '\\', '/' }) >= 0)
            {
                return null;
            }

            return deviceObject.TrimEnd('\\') + "\\" + relativeWindowsDirectory.Replace('/', '\\') +
                "\\System32\\config\\" + hiveName;
        }

        private static bool IsExpectedShadowDevicePath(string deviceObject)
        {
            if (string.IsNullOrWhiteSpace(deviceObject) ||
                !deviceObject.StartsWith(ShadowDevicePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string suffix = deviceObject.Substring(ShadowDevicePrefix.Length).TrimEnd('\\');
            int shadowNumber;
            return suffix.Length > 0 && int.TryParse(suffix, out shadowNumber) && shadowNumber >= 0;
        }

        private static bool CanOpenForRead(string path)
        {
            try
            {
                using (var handle = Kernel32.CreateFile(
                    path,
                    FileReadData,
                    ShareReadWriteDelete,
                    IntPtr.Zero,
                    OpenExisting,
                    0,
                    IntPtr.Zero))
                {
                    // Opening the handle performs an effective access check; no hive bytes are read.
                    return !handle.IsInvalid;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetReadTrustee(string path, ISet<string> unprivilegedSids)
        {
            try
            {
                FileSecurity security = File.GetAccessControl(path, AccessControlSections.Access);
                var descriptor = new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0);
                return FindReadTrustee(descriptor, unprivilegedSids);
            }
            catch
            {
                return null;
            }
        }

        private static HashSet<string> GetUnprivilegedTokenSids(out bool isPrivilegedContext)
        {
            var unprivilegedSids = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "S-1-1-0",      // Everyone
                "S-1-5-4",      // Interactive
                "S-1-5-11",     // Authenticated Users
                "S-1-5-32-545", // BUILTIN\Users (the vulnerable CVE-2021-36934 ACE)
            };
            isPrivilegedContext = true;
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    var administratorsSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                    isPrivilegedContext = principal.IsInRole(administratorsSid) ||
                        (identity.User != null && IsPrivilegedServiceSid(identity.User.Value));
                    if (!isPrivilegedContext && identity.User != null)
                    {
                        unprivilegedSids.Add(identity.User.Value);
                    }

                    if (identity.Groups != null)
                    {
                        foreach (IdentityReference group in identity.Groups)
                        {
                            var sid = group as SecurityIdentifier;
                            if (sid != null && principal.IsInRole(sid) && !IsPrivilegedServiceSid(sid.Value) &&
                                !sid.Equals(administratorsSid))
                            {
                                unprivilegedSids.Add(sid.Value);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Keep the well-known unprivileged principals and avoid current-token open tests.
            }

            return unprivilegedSids;
        }

        private static bool IsPrivilegedServiceSid(string sid)
        {
            return string.Equals(sid, "S-1-5-18", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sid, "S-1-5-19", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sid, "S-1-5-20", StringComparison.OrdinalIgnoreCase);
        }

        internal static string FindReadTrustee(RawSecurityDescriptor descriptor, ISet<string> enabledSids)
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

            foreach (GenericAce ace in descriptor.DiscretionaryAcl)
            {
                if ((ace.AceFlags & AceFlags.InheritOnly) != 0)
                {
                    continue;
                }

                var qualifiedAce = ace as QualifiedAce;
                var knownAce = ace as KnownAce;
                if (qualifiedAce == null || knownAce == null || qualifiedAce.SecurityIdentifier == null ||
                    !enabledSids.Contains(qualifiedAce.SecurityIdentifier.Value) || !GrantsFileReadData(knownAce.AccessMask))
                {
                    continue;
                }

                if (qualifiedAce.AceQualifier == AceQualifier.AccessDenied)
                {
                    return null;
                }

                if (qualifiedAce.AceQualifier == AceQualifier.AccessAllowed)
                {
                    return qualifiedAce.SecurityIdentifier.Value;
                }
            }

            return null;
        }

        private static bool GrantsFileReadData(int accessMask)
        {
            uint mask = unchecked((uint)accessMask);
            return (mask & FileReadData) != 0 || (mask & GenericRead) != 0 || (mask & GenericAll) != 0;
        }
    }
}
