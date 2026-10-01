using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace winPEAS.Info.SystemInfo
{
    internal static class StorvspVsmbCves
    {
        private const string DriverServiceKey = @"SYSTEM\CurrentControlSet\Services\storvsp";
        private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        // Microsoft Security Response Center, December 2025 security update.
        // The regular cumulative-update build is used as the safe version threshold.
        // Hotpatch builds are accepted only when their corresponding KB is present.
        private static readonly Dictionary<int, FixedBuild> FixedBuilds = new Dictionary<int, FixedBuild>
        {
            { 17763, new FixedBuild(8146, "5071544") },
            { 19044, new FixedBuild(6691, "5071546") },
            { 19045, new FixedBuild(6691, "5071546") },
            { 20348, new FixedBuild(4529, "5071547", 4467, "5071413") },
            { 22631, new FixedBuild(6345, "5071417") },
            { 25398, new FixedBuild(2025, "5071542") },
            { 26100, new FixedBuild(7462, "5072033", 7392, "5072014") },
            { 26200, new FixedBuild(7462, "5072033", 7392, "5072014") },
        };

        internal static StorvspVsmbReport GetReport(Dictionary<string, string> basicInfo)
        {
            var report = new StorvspVsmbReport
            {
                ProductName = GetValue(basicInfo, "ProductName"),
                Architecture = GetValue(basicInfo, "Architecture"),
                InstalledHotfixes = GetValue(basicInfo, "Hotfixes")
            };

            CollectBuildAndPatchState(report, basicInfo);
            CollectOptionalFeatureState(report);
            CollectDriverFile(report);
            CollectDriverRegistration(report);
            CollectDeviceLink(report);

            report.AttackSurfaceEnabled =
                (report.VirtualMachinePlatformState == OptionalFeatureState.Enabled &&
                    (report.DriverExists || report.DriverRegistered)) ||
                report.DriverStarted ||
                report.DeviceLinkPresent;
            return report;
        }

        private static void CollectBuildAndPatchState(StorvspVsmbReport report, Dictionary<string, string> basicInfo)
        {
            if (!int.TryParse(GetValue(basicInfo, "CurrentBuild"), out int build) ||
                !int.TryParse(GetValue(basicInfo, "UpdateBuildRevision"), out int revision))
            {
                report.PatchStatus = StorvspPatchStatus.Unknown;
                report.PatchEvidence = "Unable to parse CurrentBuild/UBR.";
                return;
            }

            report.Build = build;
            report.Revision = revision;
            report.BuildVersion = build + "." + revision;

            if (!FixedBuilds.TryGetValue(build, out FixedBuild fixedBuild))
            {
                // Microsoft lists build 14393 for CVE-2025-59517, but not for
                // CVE-2025-64673. It is therefore outside this two-CVE chain.
                report.OnlyCve59517Applicable = build == 14393;
                report.PatchStatus = StorvspPatchStatus.NotAffected;
                report.PatchEvidence = report.OnlyCve59517Applicable
                    ? "Microsoft lists this build for CVE-2025-59517 only, not the complete two-CVE chain."
                    : "Microsoft does not list this build for both CVEs in the chain.";
                return;
            }

            report.BuildApplicable = true;
            report.FixedRevision = fixedBuild.Revision;
            report.FixedVersion = build + "." + fixedBuild.Revision;
            report.ApplicableKb = fixedBuild.Kb;
            report.HotpatchRevision = fixedBuild.HotpatchRevision;
            report.HotpatchKb = fixedBuild.HotpatchKb;

            if (ContainsHotfix(report.InstalledHotfixes, fixedBuild.Kb))
            {
                report.PatchStatus = StorvspPatchStatus.Patched;
                report.DirectFixInstalled = true;
                report.PatchEvidence = "Applicable December 2025 security update is present in the hotfix inventory.";
            }
            else if (!string.IsNullOrEmpty(fixedBuild.HotpatchKb) && ContainsHotfix(report.InstalledHotfixes, fixedBuild.HotpatchKb))
            {
                report.PatchStatus = StorvspPatchStatus.Patched;
                report.DirectFixInstalled = true;
                report.PatchEvidence = "Applicable December 2025 security hotpatch is present in the hotfix inventory.";
            }
            else if (revision >= fixedBuild.Revision)
            {
                report.PatchStatus = StorvspPatchStatus.Patched;
                report.PatchEvidence = "OS build is at or above Microsoft's regular cumulative-update fixed build.";
            }
            else
            {
                report.PatchStatus = StorvspPatchStatus.Susceptible;
                report.PatchEvidence = "OS build is below Microsoft's regular fixed build and the applicable fix KB was not found.";
            }
        }

        private static void CollectOptionalFeatureState(StorvspVsmbReport report)
        {
            try
            {
                const string query = "SELECT Name,InstallState FROM Win32_OptionalFeature WHERE Name='VirtualMachinePlatform'";
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2", query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject feature in results)
                    {
                        uint state = Convert.ToUInt32(feature["InstallState"]);
                        report.VirtualMachinePlatformState = state == 1
                            ? OptionalFeatureState.Enabled
                            : state == 2 || state == 3
                                ? OptionalFeatureState.Disabled
                                : OptionalFeatureState.Unknown;
                        return;
                    }
                }

                report.VirtualMachinePlatformState = OptionalFeatureState.NotFound;
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Virtual Machine Platform feature: " + ex.Message);
            }
        }

        private static void CollectDriverFile(StorvspVsmbReport report)
        {
            try
            {
                string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string path = Path.Combine(windowsDirectory, "System32", "drivers", "storvsp.sys");
                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                {
                    string nativePath = Path.Combine(windowsDirectory, "Sysnative", "drivers", "storvsp.sys");
                    if (File.Exists(nativePath))
                    {
                        path = nativePath;
                    }
                }

                report.DriverPath = path;
                report.DriverExists = File.Exists(path);
                if (!report.DriverExists)
                {
                    return;
                }

                FileVersionInfo version = FileVersionInfo.GetVersionInfo(path);
                report.DriverFileVersion = version.FileVersion ?? "";
                report.DriverProductVersion = version.ProductVersion ?? "";
                report.DriverCompany = version.CompanyName ?? "";
                report.DriverLastWriteUtc = File.GetLastWriteTimeUtc(path);

                report.DriverSignatureValid = VerifyAuthenticode(path, out string signatureStatus);
                report.DriverSignatureStatus = signatureStatus;
                try
                {
                    using (X509Certificate certificate = X509Certificate.CreateFromSignedFile(path))
                    using (var certificate2 = new X509Certificate2(certificate))
                    {
                        report.DriverSigner = certificate2.Subject;
                    }
                }
                catch (Exception ex)
                {
                    report.CollectionErrors.Add("storvsp.sys signer: " + ex.Message);
                }

                if (report.BuildApplicable && report.DriverSignatureValid &&
                    TryGetFileBuildRevision(version, out int driverBuild, out int driverRevision) &&
                    driverBuild == report.Build && driverRevision >= report.FixedRevision)
                {
                    report.DriverVersionAtOrAboveFix = true;
                    report.PatchStatus = StorvspPatchStatus.Patched;
                    report.PatchEvidence = "storvsp.sys is at or above Microsoft's regular fixed build.";
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("storvsp.sys metadata: " + ex.Message);
            }
        }

        private static void CollectDriverRegistration(StorvspVsmbReport report)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(DriverServiceKey))
                {
                    if (key != null)
                    {
                        report.DriverRegistered = true;
                        report.DriverImagePath = Convert.ToString(key.GetValue("ImagePath", ""));
                        report.DriverStart = TryConvertInt(key.GetValue("Start"));
                        report.DriverType = TryConvertInt(key.GetValue("Type"));
                    }
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("storvsp driver registry: " + ex.Message);
            }

            try
            {
                const string query = "SELECT Name,PathName,StartMode,State,Started FROM Win32_SystemDriver WHERE Name='storvsp'";
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2", query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject driver in results)
                    {
                        report.DriverRegistered = true;
                        report.DriverWmiPath = Convert.ToString(driver["PathName"]);
                        report.DriverStartMode = Convert.ToString(driver["StartMode"]);
                        report.DriverState = Convert.ToString(driver["State"]);
                        report.DriverStarted = Convert.ToBoolean(driver["Started"]);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("storvsp driver WMI state: " + ex.Message);
            }
        }

        private static void CollectDeviceLink(StorvspVsmbReport report)
        {
            try
            {
                var target = new StringBuilder(4096);
                uint length = QueryDosDevice("STORVSP", target, target.Capacity);
                report.DeviceLinkPresent = length != 0;
                if (report.DeviceLinkPresent)
                {
                    report.DeviceLinkTarget = target.ToString();
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("STORVSP device link: " + ex.Message);
            }
        }

        private static bool ContainsHotfix(string hotfixes, string kb)
        {
            return !string.IsNullOrEmpty(kb) && Regex.IsMatch(
                hotfixes ?? "",
                @"\bKB" + Regex.Escape(kb) + @"\b",
                RegexOptions.IgnoreCase);
        }

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            return values != null && values.TryGetValue(key, out string value) ? value ?? "" : "";
        }

        private static int? TryConvertInt(object value)
        {
            try
            {
                return value == null ? (int?)null : Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetFileBuildRevision(FileVersionInfo version, out int build, out int revision)
        {
            build = version.FileBuildPart;
            revision = version.FilePrivatePart;
            return build > 0 && revision >= 0;
        }

        private static bool VerifyAuthenticode(string path, out string status)
        {
            IntPtr fileInfoPointer = IntPtr.Zero;
            try
            {
                var fileInfo = new WinTrustFileInfo(path);
                fileInfoPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);

                var trustData = new WinTrustData(fileInfoPointer);
                int result = WinVerifyTrust(new IntPtr(-1), WinTrustActionGenericVerifyV2, trustData);
                status = result == 0 ? "Valid" : "Invalid (0x" + result.ToString("X8") + ")";
                return result == 0;
            }
            catch (Exception ex)
            {
                status = "Error: " + ex.Message;
                return false;
            }
            finally
            {
                if (fileInfoPointer != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(fileInfoPointer, typeof(WinTrustFileInfo));
                    Marshal.FreeCoTaskMem(fileInfoPointer);
                }
            }
        }

        private sealed class FixedBuild
        {
            internal FixedBuild(int revision, string kb, int hotpatchRevision = 0, string hotpatchKb = "")
            {
                Revision = revision;
                Kb = kb;
                HotpatchRevision = hotpatchRevision;
                HotpatchKb = hotpatchKb;
            }

            internal int Revision { get; }
            internal string Kb { get; }
            internal int HotpatchRevision { get; }
            internal string HotpatchKb { get; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WinTrustFileInfo
        {
            public uint StructSize;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string FilePath;
            public IntPtr FileHandle;
            public IntPtr KnownSubject;

            public WinTrustFileInfo(string filePath)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));
                FilePath = filePath;
                FileHandle = IntPtr.Zero;
                KnownSubject = IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustData
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData));
            public IntPtr PolicyCallbackData = IntPtr.Zero;
            public IntPtr SipClientData = IntPtr.Zero;
            public uint UiChoice = 2; // WTD_UI_NONE
            public uint RevocationChecks = 0; // WTD_REVOKE_NONE
            public uint UnionChoice = 1; // WTD_CHOICE_FILE
            public IntPtr FileInfoPointer;
            public uint StateAction = 0; // WTD_STATEACTION_IGNORE
            public IntPtr StateData = IntPtr.Zero;
            public IntPtr UrlReference = IntPtr.Zero;
            public uint ProviderFlags = 0x00001000; // WTD_CACHE_ONLY_URL_RETRIEVAL
            public uint UiContext = 0;
            public IntPtr SignatureSettings = IntPtr.Zero;

            public WinTrustData(IntPtr fileInfoPointer)
            {
                FileInfoPointer = fileInfoPointer;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint QueryDosDevice(string deviceName, StringBuilder targetPath, int maximumLength);

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
            [In] WinTrustData trustData);
    }

    internal enum StorvspPatchStatus
    {
        Unknown,
        NotAffected,
        Susceptible,
        Patched
    }

    internal enum OptionalFeatureState
    {
        Unknown,
        NotFound,
        Disabled,
        Enabled
    }

    internal sealed class StorvspVsmbReport
    {
        public string ProductName { get; set; } = "";
        public string Architecture { get; set; } = "";
        public string InstalledHotfixes { get; set; } = "";
        public int Build { get; set; }
        public int Revision { get; set; }
        public string BuildVersion { get; set; } = "";
        public bool BuildApplicable { get; set; }
        public bool OnlyCve59517Applicable { get; set; }
        public int FixedRevision { get; set; }
        public string FixedVersion { get; set; } = "";
        public string ApplicableKb { get; set; } = "";
        public int HotpatchRevision { get; set; }
        public string HotpatchKb { get; set; } = "";
        public bool DirectFixInstalled { get; set; }
        public bool DriverVersionAtOrAboveFix { get; set; }
        public StorvspPatchStatus PatchStatus { get; set; } = StorvspPatchStatus.Unknown;
        public string PatchEvidence { get; set; } = "";
        public OptionalFeatureState VirtualMachinePlatformState { get; set; } = OptionalFeatureState.Unknown;
        public string DriverPath { get; set; } = "";
        public bool DriverExists { get; set; }
        public string DriverFileVersion { get; set; } = "";
        public string DriverProductVersion { get; set; } = "";
        public string DriverCompany { get; set; } = "";
        public DateTime? DriverLastWriteUtc { get; set; }
        public bool DriverSignatureValid { get; set; }
        public string DriverSignatureStatus { get; set; } = "Not checked";
        public string DriverSigner { get; set; } = "";
        public bool DriverRegistered { get; set; }
        public string DriverImagePath { get; set; } = "";
        public int? DriverStart { get; set; }
        public int? DriverType { get; set; }
        public string DriverWmiPath { get; set; } = "";
        public string DriverStartMode { get; set; } = "";
        public string DriverState { get; set; } = "";
        public bool DriverStarted { get; set; }
        public bool DeviceLinkPresent { get; set; }
        public string DeviceLinkTarget { get; set; } = "";
        public bool AttackSurfaceEnabled { get; set; }
        public List<string> CollectionErrors { get; } = new List<string>();

        public bool HighPriorityFinding =>
            PatchStatus == StorvspPatchStatus.Susceptible && AttackSurfaceEnabled;
    }
}
