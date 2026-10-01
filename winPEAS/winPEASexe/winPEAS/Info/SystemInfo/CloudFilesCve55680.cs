using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace winPEAS.Info.SystemInfo
{
    internal static class CloudFilesCve55680
    {
        private const string DriverServiceKey = @"SYSTEM\CurrentControlSet\Services\CldFlt";
        private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        // Microsoft Security Response Center, October 2025 security update.
        // These are the first cumulative-update builds listed for CVE-2025-55680.
        private static readonly Dictionary<int, FixedBuild> FixedBuilds = new Dictionary<int, FixedBuild>
        {
            { 17763, new FixedBuild(7919, "5066586") },
            { 19044, new FixedBuild(6456, "5066791") },
            { 19045, new FixedBuild(6456, "5066791") },
            { 20348, new FixedBuild(4294, "5066782") },
            { 22621, new FixedBuild(6060, "5066793") },
            { 22631, new FixedBuild(6060, "5066793") },
            { 25398, new FixedBuild(1913, "5066780") },
            { 26100, new FixedBuild(6899, "5066835") },
            { 26200, new FixedBuild(6899, "5066835") },
        };

        internal static CloudFilesReport GetReport(Dictionary<string, string> basicInfo)
        {
            var report = new CloudFilesReport
            {
                ProductName = GetValue(basicInfo, "ProductName"),
                Architecture = GetValue(basicInfo, "Architecture"),
                InstalledHotfixes = GetValue(basicInfo, "Hotfixes")
            };

            CollectBuildAndPatchState(report, basicInfo);
            CollectDriverFile(report);
            CollectDriverRegistration(report);
            report.AttackSurfacePresent = report.DriverExists || report.DriverRegistered;
            return report;
        }

        private static void CollectBuildAndPatchState(CloudFilesReport report, Dictionary<string, string> basicInfo)
        {
            int build;
            int revision;
            if (!int.TryParse(GetValue(basicInfo, "CurrentBuild"), out build) ||
                !int.TryParse(GetValue(basicInfo, "UpdateBuildRevision"), out revision))
            {
                report.PatchStatus = CloudFilesPatchStatus.Unknown;
                report.PatchEvidence = "Unable to parse CurrentBuild/UBR.";
                return;
            }

            report.Build = build;
            report.Revision = revision;
            report.BuildVersion = build + "." + revision;

            FixedBuild fixedBuild;
            if (!FixedBuilds.TryGetValue(build, out fixedBuild))
            {
                report.PatchStatus = CloudFilesPatchStatus.NotAffected;
                report.PatchEvidence = "Microsoft does not list this OS build line as affected by CVE-2025-55680.";
                return;
            }

            report.BuildApplicable = true;
            report.FixedRevision = fixedBuild.Revision;
            report.FixedVersion = build + "." + fixedBuild.Revision;
            report.ApplicableKb = fixedBuild.Kb;

            if (ContainsHotfix(report.InstalledHotfixes, fixedBuild.Kb))
            {
                report.DirectFixInstalled = true;
                report.PatchStatus = CloudFilesPatchStatus.Patched;
                report.PatchEvidence = "Applicable October 2025 security update is present in the hotfix inventory.";
            }
            else if (revision >= fixedBuild.Revision)
            {
                report.PatchStatus = CloudFilesPatchStatus.Patched;
                report.PatchEvidence = "OS build is at or above Microsoft's cumulative-update fixed build.";
            }
            else
            {
                report.PatchStatus = CloudFilesPatchStatus.Susceptible;
                report.PatchEvidence = "OS build is below Microsoft's fixed build and the applicable fix KB was not found.";
            }
        }

        private static void CollectDriverFile(CloudFilesReport report)
        {
            try
            {
                string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string path = Path.Combine(windowsDirectory, "System32", "drivers", "cldflt.sys");
                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                {
                    string nativePath = Path.Combine(windowsDirectory, "Sysnative", "drivers", "cldflt.sys");
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
                    report.CollectionErrors.Add("cldflt.sys signer: " + ex.Message);
                }

                int driverBuild;
                int driverRevision;
                if (report.BuildApplicable &&
                    TryGetFileBuildRevision(version, out driverBuild, out driverRevision) &&
                    driverBuild == report.Build && driverRevision >= report.FixedRevision)
                {
                    // MSRC does not publish an affected-binary threshold for cldflt.sys.
                    // Keep this as supporting evidence; OS build/KB remains authoritative.
                    report.DriverVersionAtOrAboveOsFix = true;
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("cldflt.sys metadata: " + ex.Message);
            }
        }

        private static void CollectDriverRegistration(CloudFilesReport report)
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
                report.CollectionErrors.Add("CldFlt driver registry: " + ex.Message);
            }

            try
            {
                const string query = "SELECT Name,PathName,StartMode,State,Started FROM Win32_SystemDriver WHERE Name='CldFlt'";
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2", query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject driver in results)
                    {
                        report.DriverRegistered = true;
                        report.DriverWmiPath = Convert.ToString(driver["PathName"]);
                        report.DriverStartMode = Convert.ToString(driver["StartMode"]);
                        report.DriverState = Convert.ToString(driver["State"]);
                        report.DriverStarted = driver["Started"] != null && Convert.ToBoolean(driver["Started"]);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("CldFlt driver WMI state: " + ex.Message);
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
            internal FixedBuild(int revision, string kb)
            {
                Revision = revision;
                Kb = kb;
            }

            internal int Revision { get; }
            internal string Kb { get; }
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

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
            [In] WinTrustData trustData);
    }

    internal enum CloudFilesPatchStatus
    {
        Unknown,
        NotAffected,
        Susceptible,
        Patched
    }

    internal sealed class CloudFilesReport
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
        public CloudFilesPatchStatus PatchStatus { get; set; }
        public string PatchEvidence { get; set; } = "";

        public string DriverPath { get; set; } = "";
        public bool DriverExists { get; set; }
        public string DriverFileVersion { get; set; } = "";
        public string DriverProductVersion { get; set; } = "";
        public string DriverCompany { get; set; } = "";
        public string DriverSigner { get; set; } = "";
        public bool DriverSignatureValid { get; set; }
        public string DriverSignatureStatus { get; set; } = "Not checked";
        public DateTime? DriverLastWriteUtc { get; set; }
        public bool DriverVersionAtOrAboveOsFix { get; set; }

        public bool DriverRegistered { get; set; }
        public bool DriverStarted { get; set; }
        public string DriverImagePath { get; set; } = "";
        public int? DriverStart { get; set; }
        public int? DriverType { get; set; }
        public string DriverWmiPath { get; set; } = "";
        public string DriverStartMode { get; set; } = "";
        public string DriverState { get; set; } = "";
        public bool AttackSurfacePresent { get; set; }
        public List<string> CollectionErrors { get; } = new List<string>();

        public bool HighPriorityFinding
        {
            get { return PatchStatus == CloudFilesPatchStatus.Susceptible && DriverStarted; }
        }

        public bool PotentiallyVulnerable
        {
            get { return PatchStatus == CloudFilesPatchStatus.Susceptible && AttackSurfacePresent; }
        }
    }
}
