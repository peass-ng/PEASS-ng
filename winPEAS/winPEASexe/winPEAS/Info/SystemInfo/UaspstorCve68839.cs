using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using winPEAS.Helpers;

namespace winPEAS.Info.SystemInfo
{
    internal enum UaspstorPatchStatus
    {
        Unknown,
        Susceptible,
        Patched
    }

    internal sealed class UaspstorCve68839Report
    {
        internal string ProductName = "";
        internal string BuildVersion = "";
        internal string FixedVersion = "";
        internal string PatchEvidence = "";
        internal string DriverPath = "";
        internal string DriverVersion = "";
        internal string DriverSignatureStatus = "";
        internal bool DriverSignatureValid;
        internal string DriverSha256 = "";
        internal int Build;
        internal int? DriverRevision;
        internal bool BuildListed;
        internal bool DriverExists;
        internal bool DriverRegistered;
        internal int? DriverStart;
        internal UaspstorPatchStatus PatchStatus;
        internal readonly List<string> CollectionErrors = new List<string>();
    }

    internal static class UaspstorCve68839
    {
        // Microsoft CNA record for CVE-2026-68839 (September 2026):
        // https://github.com/CVEProject/cvelistV5/blob/main/cves/2026/68xxx/CVE-2026-68839.json
        // Windows Server 2025 and Windows 11 24H2 both use build 26100, but
        // have different fixed revisions. Do not collapse these branches.
        private static readonly Dictionary<int, int> FixedRevisions = new Dictionary<int, int>
        {
            { 9200, 26349 }, { 9600, 23398 }, { 14393, 9512 },
            { 17763, 9245 }, { 19044, 7725 }, { 19045, 7725 },
            { 20348, 5622 }, { 22631, 7582 }, { 26200, 9445 },
            { 28000, 2954 }
        };

        internal static UaspstorCve68839Report GetReport(Dictionary<string, string> basicInfo)
        {
            var report = new UaspstorCve68839Report
            {
                ProductName = GetValue(basicInfo, "ProductName")
            };
            if (string.IsNullOrEmpty(report.ProductName))
                report.ProductName = GetValue(basicInfo, "OS Name");

            int build;
            int revision;
            if (!int.TryParse(GetValue(basicInfo, "CurrentBuild"), out build))
            {
                report.PatchEvidence = "Windows build is unavailable.";
                return report;
            }

            bool revisionKnown = int.TryParse(GetValue(basicInfo, "UpdateBuildRevision"), out revision);
            report.Build = build;
            report.BuildVersion = revisionKnown ? build + "." + revision : build.ToString();

            if ((build == 9200 || build == 9600) &&
                report.ProductName.IndexOf("Server", StringComparison.OrdinalIgnoreCase) < 0)
            {
                report.PatchEvidence = "Microsoft lists this build for Server 2012/R2 only; product branch is not confirmed.";
                return report;
            }

            int fixedRevision;
            if (build == 26100)
            {
                if (report.ProductName.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0)
                    fixedRevision = 33438;
                else if (report.ProductName.IndexOf("Windows 11", StringComparison.OrdinalIgnoreCase) >= 0)
                    fixedRevision = 9445;
                else
                {
                    report.PatchEvidence = "Build 26100 is shared by Windows 11 and Server 2025; product branch is unknown.";
                    return report;
                }
            }
            else if (!FixedRevisions.TryGetValue(build, out fixedRevision))
            {
                report.PatchEvidence = "Microsoft does not publish a fixed-build threshold for this build line; patch status is unknown.";
                return report;
            }

            report.BuildListed = true;
            report.FixedVersion = build + "." + fixedRevision;
            CollectDriver(report);
            CollectRegistration(report);
            AssessPatchStatus(report, fixedRevision, revisionKnown, revision);
            return report;
        }

        internal static void AssessPatchStatus(UaspstorCve68839Report report, int fixedRevision,
            bool revisionKnown, int revision)
        {
            report.PatchStatus = UaspstorPatchStatus.Unknown;
            // UBR is not a reliable patch indicator on Server 2012/R2. Use
            // the signed driver version there rather than declaring it patched.
            bool osVersionUsable = report.Build != 9200 && report.Build != 9600;
            if (osVersionUsable && revisionKnown)
            {
                // An unverified file version must not override the running OS
                // version published in Microsoft's affected-product record.
                report.PatchStatus = revision >= fixedRevision ?
                    UaspstorPatchStatus.Patched : UaspstorPatchStatus.Susceptible;
                report.PatchEvidence = revision >= fixedRevision ?
                    "OS build is at or above Microsoft's fixed build." :
                    "Running OS build predates Microsoft's fixed build.";
            }
            else if (report.DriverRevision.HasValue && report.DriverRevision.Value >= fixedRevision &&
                report.DriverSignatureValid)
            {
                report.PatchStatus = UaspstorPatchStatus.Patched;
                report.PatchEvidence = "Signature-verified uaspstor.sys file version is at or above Microsoft's fixed build.";
            }
            else if (report.DriverRevision.HasValue && report.DriverRevision.Value < fixedRevision)
            {
                report.PatchStatus = UaspstorPatchStatus.Susceptible;
                report.PatchEvidence = "uaspstor.sys file version predates Microsoft's fixed build.";
            }
            else if (report.DriverRevision.HasValue && !report.DriverSignatureValid)
            {
                report.PatchEvidence = "uaspstor.sys version appears fixed, but its file signature could not be verified (catalog-only signatures may not be detected).";
            }
            else
            {
                report.PatchEvidence = "Insufficient OS or driver version evidence to determine patch state.";
            }
        }

        private static void CollectDriver(UaspstorCve68839Report report)
        {
            try
            {
                string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string systemDirectory = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? "Sysnative" : "System32";
                report.DriverPath = Path.Combine(windowsDirectory, systemDirectory, "drivers", "uaspstor.sys");
                report.DriverExists = File.Exists(report.DriverPath);
                if (!report.DriverExists)
                    return;

                FileVersionInfo version = FileVersionInfo.GetVersionInfo(report.DriverPath);
                report.DriverVersion = version.FileVersion ?? "";
                if (version.FileBuildPart == report.Build &&
                    version.FilePrivatePart >= 0)
                    report.DriverRevision = version.FilePrivatePart;
                report.DriverSignatureValid = AuthenticodeHelper.Verify(report.DriverPath, out string signatureStatus);
                report.DriverSignatureStatus = signatureStatus;
                using (var stream = File.OpenRead(report.DriverPath))
                using (var sha256 = SHA256.Create())
                    report.DriverSha256 = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "");
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("uaspstor.sys metadata: " + ex.Message);
            }
        }

        private static void CollectRegistration(UaspstorCve68839Report report)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\UASPStor"))
                {
                    if (key == null)
                        return;
                    report.DriverRegistered = true;
                    object start = key.GetValue("Start");
                    if (start != null)
                        report.DriverStart = Convert.ToInt32(start);
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("UASPStor service registration: " + ex.Message);
            }
        }

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            return values != null && values.TryGetValue(key, out string value) ? value ?? "" : "";
        }
    }
}
