using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using winPEAS.Helpers;
using winPEAS.Helpers.AppLocker;
using winPEAS.Helpers.Extensions;
using winPEAS.Helpers.Registry;
using winPEAS.Info.SystemInfo;
using winPEAS.Info.SystemInfo.AuditPolicies;
using winPEAS.Info.SystemInfo.DotNet;
using winPEAS.Info.SystemInfo.GroupPolicy;
using winPEAS.Info.SystemInfo.NamedPipes;
using winPEAS.Info.SystemInfo.Ntlm;
using winPEAS.Info.SystemInfo.PowerShell;
using winPEAS.Info.SystemInfo.Printers;
using winPEAS.Info.SystemInfo.SysMon;
using winPEAS.Info.SystemInfo.WindowsDefender;
using winPEAS.Native.Enums;

namespace winPEAS.Checks
{
    class SystemInfo : ISystemCheck
    {
        static string badUAC = "No prompting|PromptForNonWindowsBinaries";
        static string goodUAC = "PromptPermitDenyOnSecureDesktop";
        static string badLAPS = "LAPS not installed";
        static Dictionary<string, string> _basicSystemInfo;
        static PrintSpoolerCve38028Report _spoolerReport;

        internal enum AlwaysInstallElevatedStatus
        {
            BothEnabled,
            OneEnabled,
            NotConfirmed
        }

        internal static AlwaysInstallElevatedStatus AssessAlwaysInstallElevated(string machineValue, string userValue)
        {
            bool machineEnabled = machineValue == "1";
            bool userEnabled = userValue == "1";
            if (machineEnabled && userEnabled) return AlwaysInstallElevatedStatus.BothEnabled;
            if (machineEnabled || userEnabled) return AlwaysInstallElevatedStatus.OneEnabled;
            return AlwaysInstallElevatedStatus.NotConfirmed;
        }

        internal static string DescribeAlwaysInstallElevatedValue(string value)
        {
            if (value == "1" || value == "0") return value;
            return string.IsNullOrEmpty(value) ? "missing" : "unexpected value";
        }

        internal enum PointAndPrintPolicyStatus
        {
            ExplicitAdminOnly,
            ReviewCandidate,
            UnknownValue,
            NoExplicitWeakValues
        }

        internal static PointAndPrintPolicyStatus AssessPointAndPrintPolicy(uint? restrict, uint? noWarn, uint? updatePrompt)
        {
            if (restrict == 1) return PointAndPrintPolicyStatus.ExplicitAdminOnly;
            if (restrict == 0 || (noWarn.HasValue && noWarn.Value != 0) ||
                (updatePrompt.HasValue && updatePrompt.Value != 0)) return PointAndPrintPolicyStatus.ReviewCandidate;
            if (restrict.HasValue) return PointAndPrintPolicyStatus.UnknownValue;
            return PointAndPrintPolicyStatus.NoExplicitWeakValues;
        }


        private static readonly Dictionary<string, string> _asrGuids = new Dictionary<string, string>
        {
            { "01443614-cd74-433a-b99e-2ecdc07bfc25" , "Block executable files from running unless they meet a prevalence, age, or trusted list criteria"},
            { "c1db55ab-c21a-4637-bb3f-a12568109d35" , "Use advanced protection against ransomware"},
            { "9e6c4e1f-7d60-472f-ba1a-a39ef669e4b2" , "Block credential stealing from the Windows local security authority subsystem (lsass.exe)"},
            { "d1e49aac-8f56-4280-b9ba-993a6d77406c" , "Block process creations originating from PSExec and WMI commands"},
            { "b2b3f03d-6a65-4f7b-a9c7-1c7ef74a9ba4" , "Block untrusted and unsigned processes that run from USB"},
            { "26190899-1602-49e8-8b27-eb1d0a1ce869" , "Block Office communication applications from creating child processes"},
            { "7674ba52-37eb-4a4f-a9a1-f0f9a1619a2c" , "Block Adobe Reader from creating child processes"},
            { "e6db77e5-3df2-4cf1-b95a-636979351e5b" , "Block persistence through WMI event subscription"},
            { "d4f940ab-401b-4efc-aadc-ad5f3c50688a" , "Block all Office applications from creating child processes"},
            { "5beb7efe-fd9a-4556-801d-275e5ffc04cc" , "Block execution of potentially obfuscated scripts"},
            { "92e97fa1-2edf-4476-bdd6-9dd0b4dddc7b" , "Block Win32 API calls from Office macro	"},
            { "3b576869-a4ec-4529-8536-b80a7769e899" , "Block Office applications from creating executable content	"},
            { "75668c1f-73b5-4cf0-bb93-3ecf5cb7cc84" , "Block Office applications from injecting code into other processes"},
            { "d3e037e1-3eb8-44c8-a917-57927947596d" , "Block JavaScript or VBScript from launching downloaded executable content"},
            { "be9ba2d9-53ea-4cdc-84e5-9b1eeee46550" , "Block executable content from email client and webmail"},
        };

        public string[] MitreAttackIds { get; } = new[] { "T1082", "T1068", "T1546.015", "T1548.002", "T1003.001", "T1003.004", "T1003.005", "T1059.001", "T1552.001", "T1552.002", "T1562.001", "T1562.002", "T1518.001", "T1557.001", "T1558", "T1559", "T1134.001", "T1547.005", "T1484.001", "T1613", "T1654", "T1072", "T1187", "T1200" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("System Information", "T1082,T1068,T1546.015,T1548.002,T1003.001,T1003.004,T1003.005,T1059.001,T1552.001,T1552.002,T1562.001,T1562.002,T1518.001,T1557.001,T1558,T1559,T1134.001,T1547.005,T1484.001,T1613,T1654,T1072,T1187,T1200");

            new List<Action>
            {
                PrintBasicSystemInfo,
                PrintWindowsVersionVulnerabilities,
                PrintSpoolerCve38028,
                PrintStorvspVsmbCves,
                PrintWindowsInstallerCve27727,
                PrintCrossDeviceComCve66804,
                PrintMicrosoftUpdatesCOM,
                PrintSystemLastShutdownTime,
                PrintUserEV,
                PrintSystemEV,
                PrintAuditInfo,
                PrintAuditPoliciesInfo,
                PrintWEFInfo,
                PrintLAPSInfo,
                PrintWdigest,
                PrintLSAProtection,
                PrintCredentialGuard,
                DmaProtection.PrintInfo,
                PrintCachedCreds,
                PrintRegistryCreds,
                PrintAVInfo,
                PrintWindowsDefenderInfo,
                PrintUACInfo,
                PrintPSInfo,
                PrintPowerShellSessionSettings,
                PrintTranscriptPS,
                PrintInetInfo,
                PrintDrivesInfo,
                PrintWSUS,
                PrintKrbRelayUp,
                PrintInsideContainer,
                PrintAlwaysInstallElevated,
                PrintObjectManagerRaceAmplification,
                PrintClfsAuthentication,
                PrintLSAInfo,
                PrintNtlmSettings,
                PrintLocalGroupPolicy,
                PrintPotentialGPOAbuse,
                AppLockerHelper.PrintAppLockerPolicy,
                PrintPrintNightmarePointAndPrint,
                PrintPrintersWMIInfo,
                PrintPrinterDriverAclReview,
                PrintNamedPipes,
                PrintNamedPipeAbuseCandidates,
                PrintAMSIProviders,
                PrintSysmon,
                PrintDotNetVersions
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private static void PrintBasicSystemInfo()
        {
            try
            {
                Beaprint.MainPrint("Basic System Information", "T1082");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#version-exploits", "Check if the Windows versions is vulnerable to some known exploit");
                Dictionary<string, string> basicDictSystem = Info.SystemInfo.SystemInfo.GetBasicOSInfo();
                _basicSystemInfo = new Dictionary<string, string>(basicDictSystem);
                basicDictSystem["Hotfixes"] = Beaprint.ansi_color_good + basicDictSystem["Hotfixes"] + Beaprint.NOCOLOR;
                Dictionary<string, string> colorsSI = new Dictionary<string, string>
                {
                    { Globals.StrTrue, Beaprint.ansi_color_bad },
                };
                Beaprint.DictPrint(basicDictSystem, colorsSI, false);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintWindowsVersionVulnerabilities()
        {
            try
            {
                Beaprint.MainPrint("Windows Version Vulnerabilities", "T1082,T1068");

                var basicInfo = _basicSystemInfo ?? Info.SystemInfo.SystemInfo.GetBasicOSInfo();
                var report = WindowsVersionVulns.GetVulnerabilityReport(basicInfo);

                if (report.CandidateProducts.Count == 0)
                {
                    Beaprint.InfoPrint("Unable to map this OS to product definitions.");
                    return;
                }

                Beaprint.InfoPrint("Product candidates: " + string.Join(" | ", report.CandidateProducts));
                if (!string.IsNullOrEmpty(report.DefinitionsDate))
                {
                    Beaprint.InfoPrint("Definitions date: " + report.DefinitionsDate);
                }
                Beaprint.InfoPrint("Installed hotfixes detected: " + report.InstalledHotfixesCount);
                if (basicInfo != null && basicInfo.TryGetValue("Hotfix collection", out var hotfixCollection))
                {
                    Beaprint.InfoPrint("Patch inventory is unavailable (" + hotfixCollection + "); version matches below cannot be filtered by installed updates.");
                }
                if (report.CandidateProducts.Any(p => p.StartsWith("Windows Server 2022", StringComparison.OrdinalIgnoreCase)) &&
                    (basicInfo == null || !basicInfo.TryGetValue("CurrentBuild", out var serverBuild) || serverBuild == "20348"))
                {
                    var indicator = WindowsVersionVulns.GetCve202430088Indicator();
                    if (indicator != null)
                    {
                        Beaprint.LinkPrint(indicator.cna_url, "Microsoft CNA affected range");
                        Beaprint.LinkPrint(indicator.release_url, "Microsoft Server 2022 fixed release");
                        var status = WindowsVersionVulns.AssessFixedBuild(basicInfo, indicator);
                        string build = basicInfo != null && basicInfo.TryGetValue("CurrentBuild", out var currentBuild) ? currentBuild : "unknown";
                        string ubr = basicInfo != null && basicInfo.TryGetValue("UpdateBuildRevision", out var revision) ? revision : "unknown";
                        if (status == FixedBuildStatus.BelowFixedBuild)
                            Beaprint.InfoPrint($"CVE-2024-30088: {build}.{ubr} is below the Microsoft CNA fixed build {indicator.build}.{indicator.fixed_ubr}; verify installed hotpatches before treating this as affected.");
                        else if (status == FixedBuildStatus.FixedBuild)
                            Beaprint.GoodPrint($"CVE-2024-30088: running build {build}.{ubr} matches the Microsoft fixed release (KB{indicator.fixed_kb}).");
                        else
                            Beaprint.InfoPrint($"CVE-2024-30088: patch status unknown for build {build}.{ubr}; this fixed-build reference does not verify later cumulative or hotpatch revisions.");
                    }
                }
                if (report.TotalMatchedBeforeFiltering > 0)
                {
                    Beaprint.InfoPrint($"Pre-filter matches: {report.TotalMatchedBeforeFiltering}, filtered by installed/superseded KBs: {report.FilteredByPatches}");
                }

                if (report.Vulnerabilities.Count == 0)
                {
                    Beaprint.InfoPrint("No other known exploited vulnerabilities matched the product and reported QFE data.");
                    return;
                }

                Beaprint.InfoPrint($"Matched {report.Vulnerabilities.Count} known exploited vulnerability candidates for this Windows version; missing QFE entries do not establish exploitability.");
                if (report.MatchedProducts.Count > 0)
                {
                    Beaprint.InfoPrint("Matched products: " + string.Join(" | ", report.MatchedProducts));
                }

                int maxToPrint = 20;
                foreach (var vuln in report.Vulnerabilities.Take(maxToPrint))
                {
                    string vulnId = string.IsNullOrWhiteSpace(vuln.cve) ? $"KB{vuln.kb}" : vuln.cve;
                    string kbInfo = string.IsNullOrWhiteSpace(vuln.kb) ? "" : $" KB{vuln.kb}";
                    string severityInfo = string.IsNullOrWhiteSpace(vuln.severity) ? "" : $" [{vuln.severity}]";
                    string impactInfo = string.IsNullOrWhiteSpace(vuln.impact) ? "" : $" {vuln.impact}";
                    Beaprint.InfoPrint($"    {vulnId}{kbInfo}{severityInfo}{impactInfo}");
                }

                if (report.Vulnerabilities.Count > maxToPrint)
                {
                    Beaprint.InfoPrint($"Showing {maxToPrint}/{report.Vulnerabilities.Count} results.");
                }

                Beaprint.InfoPrint("This check applies version matching with installed/superseded KB filtering.");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintSpoolerCve38028()
        {
            try
            {
                Beaprint.MainPrint("Print Spooler LPE (CVE-2022-38028 / GooseEgg)", "T1068");
                Beaprint.LinkPrint("https://msrc.microsoft.com/update-guide/vulnerability/CVE-2022-38028", "Microsoft advisory and security updates");
                Beaprint.LinkPrint("https://www.microsoft.com/en-us/security/blog/2024/04/22/analyzing-forest-blizzards-custom-post-compromise-tool-for-exploiting-cve-2022-38028-to-obtain-credentials/", "Microsoft GooseEgg analysis");
                Beaprint.InfoPrint("Passive/read-only exposure check; it does not interact with the spooler, submit a print job, or search for actor-specific files.");

                var basicInfo = _basicSystemInfo ?? Info.SystemInfo.SystemInfo.GetBasicOSInfo();
                PrintSpoolerCve38028Report report = winPEAS.Info.SystemInfo.PrintSpoolerCve38028.GetReport(basicInfo);
                _spoolerReport = report;

                Beaprint.NoColorPrint("    Product/version/architecture: " + report.ProductName + " / " + report.DisplayVersion + " / " + report.Architecture);
                Beaprint.NoColorPrint("    OS build: " + (string.IsNullOrEmpty(report.BuildVersion) ? "unknown" : report.BuildVersion));
                if (report.BuildApplicable)
                {
                    Beaprint.NoColorPrint("    First fixed level: " + report.FixedVersion + " (KB" + string.Join(" or KB", report.ApplicableKbs) + ")");
                    Beaprint.NoColorPrint("    Applicable/superseded fix found: " + !string.IsNullOrEmpty(report.MatchedFixKb) +
                        (string.IsNullOrEmpty(report.MatchedFixKb) ? "" : " (KB" + report.MatchedFixKb + ")"));
                }

                string registryStart = report.RegistryStart.HasValue ? report.RegistryStart.Value.ToString() : "unknown";
                Beaprint.NoColorPrint("    Spooler present/available/running: " + report.ServiceExists + " / " + report.ServiceAvailable + " / " + report.Started);
                Beaprint.NoColorPrint("    WMI path/account/start/state: " + report.PathName + " / " + report.StartName + " / " + report.StartMode + " / " + report.State);
                Beaprint.NoColorPrint("    Registry image/account/start: " + report.RegistryImagePath + " / " + report.RegistryObjectName + " / " + registryStart);
                Beaprint.NoColorPrint("    Spooler registry writable by broad low-privilege principal: " + report.RegistryWritableByLowPrivilegePrincipal);
                if (report.RegistryWritableByLowPrivilegePrincipal)
                {
                    Beaprint.BadPrint("    Registry ACL principals/rights: " + string.Join(", ", report.RegistryWritablePrincipals) + " / " + string.Join(", ", report.RegistryWritableRights));
                }

                if (report.HighPriorityFinding)
                {
                    Beaprint.BadPrint("HIGH: The Windows build is missing the CVE-2022-38028 fix and the Print Spooler is available. " + report.PatchEvidence);
                    Beaprint.BadPrint("Install the applicable October 2022 update or a later cumulative update. Disable the Print Spooler where it is not required.");
                }
                else if (report.PatchStatus == PrintSpoolerPatchStatus.Susceptible)
                {
                    Beaprint.InfoPrint("The Windows build appears unpatched, but the Print Spooler is absent or disabled, so the CVE is not reported as currently exposed. " + report.PatchEvidence);
                }
                else if (report.PatchStatus == PrintSpoolerPatchStatus.Patched)
                {
                    Beaprint.GoodPrint("The running system is at or above Microsoft's fixed level for CVE-2022-38028. " + report.PatchEvidence);
                }
                else if (report.PatchStatus == PrintSpoolerPatchStatus.NotAffected)
                {
                    Beaprint.GoodPrint("Microsoft does not list this Windows build as affected by CVE-2022-38028.");
                }
                else
                {
                    Beaprint.InfoPrint("Unable to determine the CVE-2022-38028 patch state. " + report.PatchEvidence);
                }

                foreach (string error in report.CollectionErrors)
                {
                    Beaprint.GrayPrint("    Collection note: " + error);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintStorvspVsmbCves()
        {
            try
            {
                Beaprint.MainPrint("Storage VSP vSMB LPE (CVE-2025-59517 / CVE-2025-64673)", "T1068");
                Beaprint.LinkPrint("https://msrc.microsoft.com/update-guide/vulnerability/CVE-2025-59517", "Microsoft advisory for CVE-2025-59517");
                Beaprint.LinkPrint("https://msrc.microsoft.com/update-guide/vulnerability/CVE-2025-64673", "Microsoft advisory for CVE-2025-64673");
                Beaprint.LinkPrint("https://blog.exodusintel.com/2026/07/27/from-virtual-share-to-physical-shell-leveraging-windows-inconsistent-access-control-for-lpe/", "Technical details");
                Beaprint.InfoPrint("Passive/read-only check; it does not open a vSMB share, send IOCTL 0x240330, replace files, or activate COM objects.");

                var basicInfo = _basicSystemInfo ?? Info.SystemInfo.SystemInfo.GetBasicOSInfo();
                StorvspVsmbReport report = StorvspVsmbCves.GetReport(basicInfo);

                Beaprint.NoColorPrint("    Product/architecture: " + report.ProductName + " / " + report.Architecture);
                Beaprint.NoColorPrint("    OS build: " + (string.IsNullOrEmpty(report.BuildVersion) ? "unknown" : report.BuildVersion));
                if (report.BuildApplicable)
                {
                    Beaprint.NoColorPrint("    First regular fixed build: " + report.FixedVersion + " (KB" + report.ApplicableKb + ")");
                    if (!string.IsNullOrEmpty(report.HotpatchKb))
                    {
                        Beaprint.NoColorPrint("    December 2025 hotpatch: " + report.Build + "." + report.HotpatchRevision + " (KB" + report.HotpatchKb + ")");
                    }
                    Beaprint.NoColorPrint("    Applicable fix KB directly present: " + report.DirectFixInstalled +
                        " (later cumulative updates are also recognized by build/driver version)");
                }

                if (report.SurfaceCollectionSkipped)
                {
                    Beaprint.NoColorPrint("    Attack-surface collection: skipped because this two-CVE chain is patched or not applicable to the OS build.");
                }
                else
                {
                    Beaprint.NoColorPrint("    Virtual Machine Platform: " + report.VirtualMachinePlatformState);
                    Beaprint.NoColorPrint("    storvsp driver registered/started: " + report.DriverRegistered + " / " +
                        (report.DriverRuntimeKnown ? report.DriverStarted.ToString() : "unknown"));
                    if (report.DriverRegistered)
                    {
                        string registryState = report.DriverStart.HasValue ? report.DriverStart.Value.ToString() : "unknown";
                        string driverType = report.DriverType.HasValue ? report.DriverType.Value.ToString() : "unknown";
                        Beaprint.NoColorPrint("    storvsp registry image/start/type: " + report.DriverImagePath + " / " + registryState + " / " + driverType);
                        if (report.DriverRuntimeKnown)
                        {
                            Beaprint.NoColorPrint("    storvsp WMI path/start mode/state: " + report.DriverWmiPath + " / " + report.DriverStartMode + " / " + report.DriverState);
                        }
                    }

                    Beaprint.NoColorPrint("    storvsp.sys: " + report.DriverPath + " (exists: " + report.DriverExists + ")");
                    if (report.DriverExists)
                    {
                        Beaprint.NoColorPrint("    storvsp.sys file/product version: " + report.DriverFileVersion + " / " + report.DriverProductVersion);
                        Beaprint.NoColorPrint("    storvsp.sys company/signer: " + report.DriverCompany + " / " + report.DriverSigner);
                        Beaprint.NoColorPrint("    storvsp.sys Authenticode: " + report.DriverSignatureStatus);
                        if (report.DriverLastWriteUtc.HasValue)
                        {
                            Beaprint.NoColorPrint("    storvsp.sys last write UTC: " + report.DriverLastWriteUtc.Value.ToString("u"));
                        }
                    }

                    Beaprint.NoColorPrint("    \\.\\STORVSP DOS device link: " + report.DeviceLinkPresent +
                        (string.IsNullOrEmpty(report.DeviceLinkTarget) ? "" : " -> " + report.DeviceLinkTarget));
                    Beaprint.NoColorPrint("    Relevant vSMB attack surface enabled: " + report.AttackSurfaceEnabled);
                }

                if (report.HighPriorityFinding)
                {
                    Beaprint.BadPrint("HIGH: The unpatched build and enabled STORVSP/vSMB attack surface match the low-privileged-to-SYSTEM chain (CVE-2025-59517 + CVE-2025-64673). " + report.PatchEvidence);
                    Beaprint.BadPrint("Install KB" + report.ApplicableKb + " or a later cumulative update. If Virtual Machine Platform is unnecessary, disable it to remove this attack surface.");
                }
                else if (report.PatchStatus == StorvspPatchStatus.Susceptible)
                {
                    Beaprint.InfoPrint("The OS build is susceptible to both CVEs, but an enabled STORVSP/vSMB attack surface was not confirmed by the available evidence. " + report.PatchEvidence);
                }
                else if (report.PatchStatus == StorvspPatchStatus.Patched)
                {
                    Beaprint.GoodPrint("The running system is at or above Microsoft's fixed level for both Storage VSP vulnerabilities. " + report.PatchEvidence);
                }
                else if (report.OnlyCve59517Applicable)
                {
                    Beaprint.InfoPrint("CVE-2025-59517 applies to this build, but Microsoft does not list CVE-2025-64673; this two-CVE chain is not reported as applicable.");
                }
                else if (report.PatchStatus == StorvspPatchStatus.NotAffected)
                {
                    Beaprint.GoodPrint("Microsoft does not list this OS build as affected by both CVEs in the vSMB chain.");
                }
                else
                {
                    Beaprint.InfoPrint("Unable to determine the Storage VSP patch state. " + report.PatchEvidence);
                }

                foreach (string error in report.CollectionErrors)
                {
                    Beaprint.GrayPrint("    Collection note: " + error);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintWindowsInstallerCve27727()
        {
            try
            {
                Beaprint.MainPrint("Windows Installer CVE-2025-27727", "T1068");
                Beaprint.LinkPrint("https://msrc.microsoft.com/update-guide/vulnerability/CVE-2025-27727", "Microsoft advisory and security updates");
                Beaprint.LinkPrint("https://blog.exodusintel.com/2026/07/06/microsoft-windows-installer-folder-delete-privilege-escalation/", "Technical details");
                Beaprint.InfoPrint("Passive/read-only check; it does not invoke the MSI COM interface or alter installer state.");

                var basicInfo = _basicSystemInfo ?? Info.SystemInfo.SystemInfo.GetBasicOSInfo();
                WindowsInstallerReport report = WindowsInstallerCve27727.GetReport(basicInfo);

                Beaprint.NoColorPrint("    Product: " + report.ProductName);
                Beaprint.NoColorPrint("    Architecture: " + report.Architecture);
                Beaprint.NoColorPrint("    OS build: " + (string.IsNullOrEmpty(report.BuildVersion) ? "unknown" : report.BuildVersion));
                if (!string.IsNullOrEmpty(report.FixedVersion))
                {
                    Beaprint.NoColorPrint("    First fixed build: " + report.FixedVersion + " (April 2025 KB" + report.ApplicableKb + ")");
                    string[] applicableKbs = report.ApplicableKb.Split('/');
                    bool aprilKbFound = applicableKbs.Any(kb => report.InstalledHotfixes.IndexOf("KB" + kb, StringComparison.OrdinalIgnoreCase) >= 0);
                    Beaprint.NoColorPrint("    Applicable April 2025 KB directly present in hotfix inventory: " + aprilKbFound +
                        " (a later cumulative update may supersede it)");
                }

                switch (report.VersionStatus)
                {
                    case WindowsInstallerVersionStatus.Susceptible:
                        Beaprint.BadPrint("The running OS build is below Microsoft's fixed build for CVE-2025-27727.");
                        break;
                    case WindowsInstallerVersionStatus.Patched:
                        Beaprint.GoodPrint("The running OS build is at or above Microsoft's fixed build for CVE-2025-27727.");
                        break;
                    case WindowsInstallerVersionStatus.NotListed:
                        Beaprint.InfoPrint("This OS build line is not in the Microsoft/NVD affected-build list for CVE-2025-27727.");
                        break;
                    default:
                        Beaprint.InfoPrint("Unable to determine CVE-2025-27727 patch status from the OS build.");
                        break;
                }

                Beaprint.NoColorPrint("    msi.dll: " + (string.IsNullOrEmpty(report.MsiDllPath) ? "not found" : report.MsiDllPath));
                if (!string.IsNullOrEmpty(report.MsiDllFileVersion))
                {
                    Beaprint.NoColorPrint("    msi.dll file/product version: " + report.MsiDllFileVersion + " / " + report.MsiDllProductVersion);
                    Beaprint.NoColorPrint("    msi.dll company/size/last write UTC: " + report.MsiDllCompany + " / " + report.MsiDllSize + " bytes / " + report.MsiDllLastWriteUtc.ToString("u"));
                }

                if (report.TempPackagesTotal == 0)
                {
                    Beaprint.GoodPrint("No values found in HKLM\\" + WindowsInstallerCve27727.TempPackagesKey + ".");
                }
                else
                {
                    Beaprint.InfoPrint("TempPackages values (showing " + report.TempPackages.Count + "/" + report.TempPackagesTotal + "):");
                    foreach (TempPackageEntry entry in report.TempPackages)
                    {
                        string details = entry.Kind + "=" + entry.Data + (entry.IsFolder ? ", folder" : "");
                        if (entry.IsConfigMsi)
                        {
                            Beaprint.BadPrint("    " + entry.Path + " (" + details + ", Config.Msi cleanup entry)");
                        }
                        else if (entry.IsOutsideCommonInstallerLocations)
                        {
                            Beaprint.InfoPrint("    " + entry.Path + " (" + details + ", outside common installer locations)");
                        }
                        else
                        {
                            Beaprint.NoColorPrint("    " + entry.Path + " (" + details + ")");
                        }
                    }
                }

                Beaprint.NoColorPrint("    Installer\\Folders contains Config.Msi: " + report.InstallerFoldersHasConfigMsi);
                Beaprint.NoColorPrint("    " + report.ConfigMsiPath + " exists: " + report.ConfigMsiExists);
                if (report.ConfigMsiExists)
                {
                    Beaprint.NoColorPrint("    Config.Msi owner: " + report.ConfigMsiOwner);
                    Beaprint.NoColorPrint("    Config.Msi created/last write UTC: " + report.ConfigMsiCreationUtc.ToString("u") + " / " + report.ConfigMsiLastWriteUtc.ToString("u"));
                    if (report.ConfigMsiNullDacl)
                    {
                        Beaprint.BadPrint("Config.Msi has a NULL DACL (all users receive full access).");
                    }
                    if (report.ConfigMsiUnusualOwner)
                    {
                        Beaprint.BadPrint("Config.Msi has an unexpected non-system owner.");
                    }
                    foreach (string acl in report.ConfigMsiDangerousAcls)
                    {
                        Beaprint.BadPrint("    Potentially dangerous Config.Msi ACL: " + acl);
                    }
                    if (report.ConfigMsiRecentlyChanged)
                    {
                        Beaprint.InfoPrint("Config.Msi changed within the last seven days; correlate with legitimate installations.");
                    }
                    foreach (string rollbackFile in report.RollbackFiles)
                    {
                        Beaprint.BadPrint("    Rollback artifact: " + rollbackFile);
                    }
                }

                if (report.InstallerFoldersHasConfigMsi && !report.ConfigMsiExists)
                {
                    Beaprint.InfoPrint("Config.Msi is absent while Installer\\Folders metadata remains (stale metadata alone can be normal).");
                }

                foreach (string error in report.CollectionErrors)
                {
                    Beaprint.InfoPrint("Collection note: " + error);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintCrossDeviceComCve66804()
        {
            try
            {
                Beaprint.MainPrint("CrossDevice dangling COM LPE (CVE-2026-66804)", "T1068,T1546.015");
                Beaprint.LinkPrint("https://msrc.microsoft.com/update-guide/vulnerability/CVE-2026-66804", "Microsoft advisory and security updates");
                Beaprint.LinkPrint("https://projectzero.google/2026/09/windows-dangling-com.html", "Project Zero technical analysis");
                Beaprint.InfoPrint("Passive/read-only check; it does not create files/events, load registered DLLs, start the task, activate COM, or inspect combase internals.");

                var basicInfo = _basicSystemInfo ?? Info.SystemInfo.SystemInfo.GetBasicOSInfo();
                CrossDeviceComReport report = CrossDeviceComCve66804.GetReport(basicInfo);

                Beaprint.NoColorPrint("    Product/architecture: " + report.ProductName + " / " + report.Architecture);
                Beaprint.NoColorPrint("    OS build: " + (string.IsNullOrEmpty(report.BuildVersion) ? "unknown" : report.BuildVersion));
                if (report.BuildApplicable)
                {
                    Beaprint.NoColorPrint("    First fixed build: " + report.FixedVersion + " (KB" + report.ApplicableKb + ")");
                    Beaprint.NoColorPrint("    Applicable fix KB directly present: " + report.DirectFixInstalled +
                        " (later cumulative updates are recognized by build revision)");
                }

                foreach (CrossDeviceComRegistration registration in report.CrossDeviceRegistrations)
                {
                    if (!registration.Present)
                    {
                        Beaprint.NoColorPrint("    " + registration.RegistryView + " CrossDevice InprocServer32: not present");
                        continue;
                    }

                    Beaprint.NoColorPrint("    " + registration.RegistryView + " CrossDevice InprocServer32: " + registration.RawServerPath);
                    Beaprint.NoColorPrint("        Resolved path: " + registration.ResolvedServerPath);
                    Beaprint.NoColorPrint("        Expected path / DLL exists: " + registration.MatchesExpectedPath + " / " + registration.ServerExists);
                    if (!string.IsNullOrEmpty(registration.NearestExistingParent))
                    {
                        Beaprint.NoColorPrint("        Nearest existing parent: " + registration.NearestExistingParent);
                    }
                    if (!string.IsNullOrEmpty(registration.PathControlReason))
                    {
                        Beaprint.BadPrint("        Path control: " + registration.PathControlReason);
                    }
                    if (!string.IsNullOrEmpty(registration.ParentAclSddl))
                    {
                        Beaprint.NoColorPrint("        Relevant file/parent DACL: " + registration.ParentAclSddl);
                    }
                    if (!string.IsNullOrEmpty(registration.CollectionError))
                    {
                        Beaprint.GrayPrint("        Collection note: " + registration.CollectionError);
                    }
                }

                Beaprint.NoColorPrint("    Shell Create Object Handler present/view: " +
                    report.ShellClassPresent + " / " + report.ShellClassRegistryView);
                if (report.ShellClassPresent)
                {
                    Beaprint.NoColorPrint("        Name/AppID/RunAs: " + report.ShellClassName + " / " +
                        report.ShellAppId + " / " + report.ShellRunAs);
                    Beaprint.NoColorPrint("        Configured to run as SYSTEM: " + report.ShellRunsAsSystem);
                }

                Beaprint.NoColorPrint("    " + CrossDeviceComCve66804.CreateObjectTaskPath +
                    " present/enabled/state: " + report.TaskPresent + " / " + report.TaskEnabled + " / " + report.TaskState);
                if (report.TaskPresent)
                {
                    Beaprint.NoColorPrint("        Principal / runs as SYSTEM: " + report.TaskPrincipal + " / " + report.TaskRunsAsSystem);
                    Beaprint.NoColorPrint("        Execute DACL readable / runnable by low privilege: " +
                        report.TaskExecuteAclKnown + " / " + report.TaskRunnableByLowPrivilege);
                    if (report.TaskRunnableByLowPrivilege)
                    {
                        Beaprint.BadPrint("        Task execute access granted by: " + report.TaskExecuteTrustee);
                    }
                    if (!string.IsNullOrEmpty(report.TaskAclSddl))
                    {
                        Beaprint.NoColorPrint("        Task DACL: " + report.TaskAclSddl);
                    }
                }

                if (report.CompleteStaticChain)
                {
                    Beaprint.BadPrint("HIGH: The unpatched build, writable dangling CrossDevice COM path, SYSTEM COM registration, and user-executable CreateObjectTask match the static CVE-2026-66804 chain.");
                    Beaprint.BadPrint("Install KB" + report.ApplicableKb + " or a later cumulative update. Treat any unexpected DLL at the registered ProgramData path as suspicious.");
                }
                else if (report.PatchStatus == CrossDevicePatchStatus.Susceptible && report.HasControllableRegistration)
                {
                    Beaprint.BadPrint("The build is susceptible and the dangling COM server path is controllable, but every prerequisite for the documented trigger chain was not confirmed. " + report.PatchEvidence);
                }
                else if (report.PatchStatus == CrossDevicePatchStatus.Patched)
                {
                    Beaprint.GoodPrint("The running system is at or above Microsoft's fixed level for CVE-2026-66804. " + report.PatchEvidence);
                }
                else if (report.PatchStatus == CrossDevicePatchStatus.NotAffected)
                {
                    Beaprint.GoodPrint(report.PatchEvidence);
                }
                else
                {
                    Beaprint.InfoPrint("No complete static CVE-2026-66804 chain was confirmed. " + report.PatchEvidence);
                }

                Beaprint.InfoPrint("Custom-marshaling policy is not inferred: EOAC_NO_CUSTOM_MARSHAL or COMGLB_UNMARSHALING_POLICY_STRONG can block exploitation.");
                foreach (string error in report.CollectionErrors)
                {
                    Beaprint.GrayPrint("    Collection note: " + error);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintMicrosoftUpdatesCOM()
        {
            try
            {
                Beaprint.MainPrint("Showing All Microsoft Updates", "T1082");

                var searcher = Type.GetTypeFromProgID("Microsoft.Update.Searcher");
                var searcherObj = Activator.CreateInstance(searcher);

                // get the total number of updates
                var count = (int)searcherObj.GetType().InvokeMember("GetTotalHistoryCount", BindingFlags.InvokeMethod, null, searcherObj, new object[] { });

                // get the pointer to the update collection
                var results = searcherObj.GetType().InvokeMember("QueryHistory", BindingFlags.InvokeMethod, null, searcherObj, new object[] { 0, count });

                for (int i = 0; i < count; ++i)
                {
                    // get the actual update item
                    var item = searcherObj.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, results, new object[] { i });

                    // get our properties
                    //  ref - https://docs.microsoft.com/en-us/windows/win32/api/wuapi/nn-wuapi-iupdatehistoryentry
                    var title = searcherObj.GetType().InvokeMember("Title", BindingFlags.GetProperty, null, item, new object[] { })?.ToString() ?? string.Empty;
                    var date = searcherObj.GetType().InvokeMember("Date", BindingFlags.GetProperty, null, item, new object[] { });
                    var description = searcherObj.GetType().InvokeMember("Description", BindingFlags.GetProperty, null, item, new object[] { });
                    var clientApplicationID = searcherObj.GetType().InvokeMember("ClientApplicationID", BindingFlags.GetProperty, null, item, new object[] { });

                    string hotfixId = "";
                    Regex reg = new Regex(@"KB\d+");
                    var matches = reg.Matches(title);
                    if (matches.Count > 0)
                    {
                        hotfixId = matches[0].ToString();
                    }

                    Beaprint.NoColorPrint($"   HotFix ID                :   {hotfixId}\n" +
                                                $"   Installed At (UTC)       :   {Convert.ToDateTime(date.ToString()).ToUniversalTime()}\n" +
                                                $"   Title                    :   {title}\n" +
                                                $"   Client Application ID    :   {clientApplicationID}\n" +
                                                $"   Description              :   {description}\n");

                    Beaprint.PrintLineSeparator();

                    Marshal.ReleaseComObject(item);
                }

                Marshal.ReleaseComObject(results);
                Marshal.ReleaseComObject(searcherObj);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintPSInfo()
        {
            try
            {
                Dictionary<string, string> colorsPSI = new Dictionary<string, string>()
                {
                    { "PS history file: .+", Beaprint.ansi_color_bad },
                    { "PS history size: .+", Beaprint.ansi_color_bad }
                };
                Beaprint.MainPrint("PowerShell Settings", "T1059.001");
                Dictionary<string, string> PSs = Info.SystemInfo.SystemInfo.GetPowerShellSettings();
                Beaprint.DictPrint(PSs, colorsPSI, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintTranscriptPS()
        {
            try
            {
                Beaprint.MainPrint("PowerShell transcript paths", "T1552.001");
                Beaprint.InfoPrint("Transcripts can contain commands and secrets; review readable files separately.");
                string drive = Path.GetPathRoot(Environment.SystemDirectory);
                string transcriptsPath = drive + @"transcripts\";
                string usersPath = $"{drive}users";

                string powershellTranscriptFilter = "powershell_transcript*";

                var colors = new Dictionary<string, string>()
                {
                    { "^.*", Beaprint.ansi_color_bad },
                };

                var results = new List<string>();

                // A configured transcript directory may sit at the system drive root.
                // Inspect only its immediate children; never follow directory junctions.
                bool rootTranscriptPartial;
                string rootTranscriptPath = Path.Combine(drive, "PSTranscripts");
                var rootTranscriptFiles = FindRootPowerShellTranscripts(rootTranscriptPath, out rootTranscriptPartial);
                if (rootTranscriptFiles.Count > 0)
                    Beaprint.ListPrint(rootTranscriptFiles.Select(file => "[path only] - " + file).ToList(), colors);
                if (rootTranscriptPartial)
                    Beaprint.GrayPrint("    Root transcript inventory is partial (entry limit or inaccessible path).");

                var users = Directory.EnumerateDirectories(usersPath, "*", SearchOption.TopDirectoryOnly);
                var dict = new Dictionary<string, string>()
                {
                    // check \\transcripts\ folder
                    {transcriptsPath, "*"},
                };

                foreach (var user in users)
                {
                    // check the users directories
                    dict.Add($"{user}\\Documents", powershellTranscriptFilter);
                }

                foreach (var kvp in dict)
                {
                    var path = kvp.Key;
                    var filter = kvp.Value;

                    if (Directory.Exists(path))
                    {
                        try
                        {
                            var files = Directory.EnumerateFiles(path, filter, SearchOption.TopDirectoryOnly).ToList();

                            foreach (var file in files)
                            {
                                var fileInfo = new FileInfo(file);
                                var humanReadableSize = MyUtils.ConvertBytesToHumanReadable(fileInfo.Length);
                                var item = $"[{humanReadableSize}] - {file}";

                                results.Add(item);
                            }
                        }
                        catch (UnauthorizedAccessException) { }
                        catch (PathTooLongException) { }
                        catch (DirectoryNotFoundException) { }
                    }
                }

                if (results.Count > 0)
                {
                    Beaprint.ListPrint(results, colors);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        internal static List<string> FindRootPowerShellTranscripts(string rootPath, out bool partial,
            Func<string, FileAttributes> getAttributes = null)
        {
            const int maxRootEntries = 16;
            const int maxChildEntries = 64;
            var files = new List<string>();
            partial = false;
            getAttributes = getAttributes ?? File.GetAttributes;

            try
            {
                if (string.IsNullOrEmpty(rootPath) || rootPath.StartsWith(@"\\", StringComparison.Ordinal) ||
                    !Path.IsPathRooted(rootPath) ||
                    new DriveInfo(Path.GetPathRoot(rootPath)).DriveType != DriveType.Fixed)
                    return files;

                var rootAttributes = getAttributes(rootPath);
                if ((rootAttributes & FileAttributes.Directory) == 0 ||
                    (rootAttributes & FileAttributes.ReparsePoint) != 0)
                    return files;

                int rootEntries = 0;
                int childEntries = 0;
                foreach (var child in Directory.EnumerateFileSystemEntries(rootPath, "*", SearchOption.TopDirectoryOnly))
                {
                    if (++rootEntries > maxRootEntries)
                    {
                        partial = true;
                        break;
                    }

                    try
                    {
                        var childAttributes = getAttributes(child);
                        if ((childAttributes & FileAttributes.Directory) == 0 ||
                            (childAttributes & FileAttributes.ReparsePoint) != 0)
                            continue;

                        foreach (var file in Directory.EnumerateFileSystemEntries(child, "*", SearchOption.TopDirectoryOnly))
                        {
                            // Count every entry, including nonmatches and skipped reparse points.
                            if (childEntries++ >= maxChildEntries)
                            {
                                partial = true;
                                return files;
                            }

                            if (!Path.GetFileName(file).StartsWith("PowerShell_transcript", StringComparison.OrdinalIgnoreCase))
                                continue;
                            var attributes = getAttributes(file);
                            if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0)
                                files.Add(file);
                        }
                    }
                    catch (UnauthorizedAccessException) { partial = true; }
                    catch (PathTooLongException) { partial = true; }
                    catch (IOException) { partial = true; }
                    catch (System.Security.SecurityException) { partial = true; }
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (UnauthorizedAccessException) { partial = true; }
            catch (PathTooLongException) { partial = true; }
            catch (IOException) { partial = true; }
            catch (ArgumentException) { partial = true; }
            catch (NotSupportedException) { partial = true; }
            catch (System.Security.SecurityException) { partial = true; }
            return files;
        }

        private static void PrintAuditInfo()
        {
            try
            {
                Beaprint.MainPrint("Audit Settings", "T1562.002");
                Beaprint.LinkPrint("", "Check what is being logged");
                Dictionary<string, string> auditDict = Info.SystemInfo.SystemInfo.GetAuditSettings();
                Beaprint.DictPrint(auditDict, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintAuditPoliciesInfo()
        {
            try
            {
                Beaprint.MainPrint("Audit Policy Settings - Classic & Advanced", "T1562.002");

                var policies = AuditPolicies.GetAuditPoliciesInfos();

                foreach (var policy in policies)
                {
                    Beaprint.NoColorPrint($"    Domain        :     {policy.Domain}\n" +
                                                $"    GPO           :     {policy.GPO}\n" +
                                                $"    Type          :     {policy.Type}\n");

                    foreach (var entry in policy.Settings)
                    {
                        Beaprint.NoColorPrint($"        {entry.Subcategory,50}   :   {entry.AuditType}");
                    }

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintWEFInfo()
        {
            try
            {
                Beaprint.MainPrint("WEF Settings", "T1562.002");
                Beaprint.LinkPrint("", "Windows Event Forwarding, is interesting to know were are sent the logs");
                Dictionary<string, string> weftDict = Info.SystemInfo.SystemInfo.GetWEFSettings();
                Beaprint.DictPrint(weftDict, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintLAPSInfo()
        {
            try
            {
                Beaprint.MainPrint("LAPS Settings", "T1003.004");
                Beaprint.LinkPrint("", "If installed, local administrator password is changed frequently and is restricted by ACL");
                Dictionary<string, string> lapsDict = Info.SystemInfo.SystemInfo.GetLapsSettings();
                Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { badLAPS, Beaprint.ansi_color_bad }
                        };
                Beaprint.DictPrint(lapsDict, colorsSI, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintWdigest()
        {
            Beaprint.MainPrint("Wdigest", "T1003.001");
            Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#wdigest", "If enabled, plain-text crds could be stored in LSASS");
            string useLogonCredential = RegistryHelper.GetRegValue("HKLM", @"SYSTEM\CurrentControlSet\Control\SecurityProviders\WDigest", "UseLogonCredential");
            if (useLogonCredential == "1")
                Beaprint.BadPrint("    Wdigest is active");
            else
                Beaprint.GoodPrint("    Wdigest is not enabled");
        }

        static void PrintLSAProtection()
        {
            Beaprint.MainPrint("LSA Protection", "T1003.001");
            Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#lsa-protection", "If enabled, a driver is needed to read LSASS memory (If Secure Boot or UEFI, RunAsPPL cannot be disabled by deleting the registry key)");
            string useLogonCredential = RegistryHelper.GetRegValue("HKLM", @"SYSTEM\CurrentControlSet\Control\LSA", "RunAsPPL");
            if (useLogonCredential == "1")
                Beaprint.GoodPrint("    LSA Protection is active");
            else
                Beaprint.BadPrint("    LSA Protection is not enabled");
        }

        static void PrintCredentialGuard()
        {
            Beaprint.MainPrint("Credentials Guard", "T1003.001");
            Beaprint.LinkPrint("https://book.hacktricks.wiki/windows-hardening/stealing-credentials/credentials-protections#credentials-guard", "If enabled, a driver is needed to read LSASS memory");
            string lsaCfgFlags = RegistryHelper.GetRegValue("HKLM", @"System\CurrentControlSet\Control\LSA", "LsaCfgFlags");

            if (lsaCfgFlags == "1")
            {
                Console.WriteLine("    Please, note that this only checks the LsaCfgFlags key value. This is not enough to enable Credentials Guard (but it's a strong indicator).");
                Beaprint.GoodPrint("    CredentialGuard is active with UEFI lock");
            }
            else if (lsaCfgFlags == "2")
            {
                Console.WriteLine("    Please, note that this only checks the LsaCfgFlags key value. This is not enough to enable Credentials Guard (but it's a strong indicator).");
                Beaprint.GoodPrint("    CredentialGuard is active without UEFI lock");
            }
            else
            {
                Beaprint.BadPrint("    CredentialGuard is not enabled");
            }

            CredentialGuard.PrintInfo();
        }

        static void PrintCachedCreds()
        {
            try{
                Beaprint.MainPrint("Cached Creds", "T1003.005");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#cached-credentials", "If > 0, credentials will be cached in the registry and accessible by SYSTEM user");
                string cachedlogonscount = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "CACHEDLOGONSCOUNT");
                if (!string.IsNullOrEmpty(cachedlogonscount))
                {
                    int clc = Int16.Parse(cachedlogonscount);
                    if (clc > 0)
                    {
                        Beaprint.BadPrint("    cachedlogonscount is " + cachedlogonscount);
                    }
                    else
                    {
                        Beaprint.BadPrint("    cachedlogonscount is " + cachedlogonscount);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintUserEV()
        {
            try
            {
                Beaprint.MainPrint("User Environment Variables", "T1082");
                Beaprint.LinkPrint("", "Check for some passwords or keys in the env variables");
                Dictionary<string, string> userEnvDict = Info.SystemInfo.SystemInfo.GetUserEnvVariables();
                Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                {
                    { Globals.PrintCredStringsLimited, Beaprint.ansi_color_bad }
                };
                Beaprint.DictPrint(userEnvDict, colorsSI, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintSystemEV()
        {
            try
            {
                Beaprint.MainPrint("System Environment Variables", "T1082");
                Beaprint.LinkPrint("", "Check for some passwords or keys in the env variables");
                Dictionary<string, string> sysEnvDict = Info.SystemInfo.SystemInfo.GetSystemEnvVariables();
                Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                {
                    { Globals.PrintCredStringsLimited, Beaprint.ansi_color_bad }
                };
                Beaprint.DictPrint(sysEnvDict, colorsSI, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintInetInfo()
        {
            try
            {
                Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                {
                    { "ProxyServer.*", Beaprint.ansi_color_bad }
                };

                Beaprint.MainPrint("HKCU Internet Settings", "T1082");
                Dictionary<string, string> HKCUDict = Info.SystemInfo.SystemInfo.GetInternetSettings("HKCU");
                Beaprint.DictPrint(HKCUDict, colorsSI, true);

                Beaprint.MainPrint("HKLM Internet Settings", "T1082");
                Dictionary<string, string> HKMLDict = Info.SystemInfo.SystemInfo.GetInternetSettings("HKLM");
                Beaprint.DictPrint(HKMLDict, colorsSI, true);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintDrivesInfo()
        {
            try
            {
                Beaprint.MainPrint("Drives Information", "T1082");
                Beaprint.LinkPrint("", "Remember that you should search more info inside the other drives");
                Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                {
                    { "Permissions.*", Beaprint.ansi_color_bad}
                };

                foreach (Dictionary<string, string> drive in Info.SystemInfo.SystemInfo.GetDrivesInfo())
                {
                    string drive_permissions = string.Join(", ", PermissionsHelper.GetPermissionsFolder(drive["Name"], Checks.CurrentUserSiDs));
                    string dToPrint = string.Format("    {0} (Type: {1})", drive["Name"], drive["Type"]);
                    if (!string.IsNullOrEmpty(drive["Volume label"]))
                        dToPrint += "(Volume label: " + drive["Volume label"] + ")";

                    if (!string.IsNullOrEmpty(drive["Filesystem"]))
                        dToPrint += "(Filesystem: " + drive["Filesystem"] + ")";

                    if (!string.IsNullOrEmpty(drive["Available space"]))
                        dToPrint += "(Available space: " + (((Int64.Parse(drive["Available space"]) / 1024) / 1024) / 1024).ToString() + " GB)";

                    if (drive_permissions.Length > 0)
                        dToPrint += "(Permissions: " + drive_permissions + ")";

                    Beaprint.AnsiPrint(dToPrint, colorsSI);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintAVInfo()
        {
            try
            {
                Beaprint.MainPrint("AV Information", "T1518.001");
                Dictionary<string, string> AVInfo = Info.SystemInfo.SystemInfo.GetAVInfo();
                if (AVInfo.ContainsKey("Name") && AVInfo["Name"].Length > 0)
                    Beaprint.GoodPrint("    Some AV was detected, search for bypasses");
                else
                    Beaprint.BadPrint("    No AV was detected!!");

                Beaprint.DictPrint(AVInfo, true);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintUACInfo()
        {
            try
            {
                Beaprint.MainPrint("UAC Status", "T1548.002");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#from-administrator-medium-to-high-integrity-level--uac-bypasss", "If you are in the Administrators group check how to bypass the UAC");
                Dictionary<string, string> uacDict = Info.SystemInfo.SystemInfo.GetUACSystemPolicies();

                Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                {
                    { badUAC, Beaprint.ansi_color_bad },
                    { goodUAC, Beaprint.ansi_color_good }
                };
                Beaprint.DictPrint(uacDict, colorsSI, false);

                if ((uacDict["EnableLUA"] == "") || (uacDict["EnableLUA"] == "0"))
                    Beaprint.BadPrint("      [*] EnableLUA != 1, UAC policies disabled.\r\n      [+] Any local account can be used for lateral movement.");

                if ((uacDict["EnableLUA"] == "1") && (uacDict["LocalAccountTokenFilterPolicy"] == "1"))
                    Beaprint.BadPrint("      [*] LocalAccountTokenFilterPolicy set to 1.\r\n      [+] Any local account can be used for lateral movement.");

                if ((uacDict["EnableLUA"] == "1") && (uacDict["LocalAccountTokenFilterPolicy"] != "1") && (uacDict["FilterAdministratorToken"] != "1"))
                    Beaprint.GoodPrint("      [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken != 1.\r\n      [-] Only the RID-500 local admin account can be used for lateral movement.");

                if ((uacDict["EnableLUA"] == "1") && (uacDict["LocalAccountTokenFilterPolicy"] != "1") && (uacDict["FilterAdministratorToken"] == "1"))
                    Beaprint.GoodPrint("      [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken == 1.\r\n      [-] No local accounts can be used for lateral movement.");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintWSUS()
        {
            try
            {
                Beaprint.MainPrint("Checking WSUS", "T1072,T1068");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#wsus");
                string policyPath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
                string policyAUPath = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
                string wsusPolicyValue = RegistryHelper.GetRegValue("HKLM", policyPath, "WUServer");
                string wuStatusServerValue = RegistryHelper.GetRegValue("HKLM", policyPath, "WUStatusServer");
                string alternateUpdateServerValue = RegistryHelper.GetRegValue("HKLM", policyPath, "UpdateServiceUrlAlternate");
                string acceptTrustedPublisherCertsValue = RegistryHelper.GetRegValue("HKLM", policyPath, "AcceptTrustedPublisherCerts");
                string useWUServerValue = RegistryHelper.GetRegValue("HKLM", policyAUPath, "UseWUServer");

                if (!string.IsNullOrEmpty(wsusPolicyValue) && wsusPolicyValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    Beaprint.BadPrint("    WSUS is using http: " + wsusPolicyValue);
                    Beaprint.InfoPrint("You can test https://github.com/pimps/wsuxploit to escalate privileges");
                    if (useWUServerValue == "1")
                        Beaprint.BadPrint("    And UseWUServer is equals to 1, so it is vulnerable!");
                    else if (useWUServerValue == "0")
                        Beaprint.GoodPrint("    But UseWUServer is equals to 0, so it is not vulnerable!");
                    else
                        Console.WriteLine("    But UseWUServer is equals to " + useWUServerValue + ", so it may work or not");
                }
                else
                {
                    if (string.IsNullOrEmpty(wsusPolicyValue))
                        Beaprint.NotFoundPrint();
                    else
                        Beaprint.InfoPrint("    WSUS value: " + wsusPolicyValue);
                }

                if (!string.IsNullOrEmpty(wuStatusServerValue))
                    Beaprint.InfoPrint("    WUStatusServer: " + wuStatusServerValue);
                if (!string.IsNullOrEmpty(alternateUpdateServerValue))
                    Beaprint.InfoPrint("    UpdateServiceUrlAlternate: " + alternateUpdateServerValue);
                if (acceptTrustedPublisherCertsValue == "1")
                    Beaprint.InfoPrint("    AcceptTrustedPublisherCerts=1: updates from an intranet update service may be signed by a certificate in the local computer's Trusted Publishers store.");
                if (!string.IsNullOrEmpty(wsusPolicyValue) && wsusPolicyValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && useWUServerValue == "1")
                    Beaprint.InfoPrint("    HTTPS WSUS still depends on DNS and TLS certificate trust for this endpoint; review control of the configured name and issuing CA.");

                if (!string.IsNullOrEmpty(wsusPolicyValue))
                {
                    bool clientsForced = useWUServerValue == "1";
                    if (clientsForced)
                    {
                        Beaprint.BadPrint("    CVE-2025-59287: Clients talk to WSUS at " + wsusPolicyValue + " (UseWUServer=1). Unpatched WSUS allows unauthenticated deserialization to SYSTEM.");
                    }
                    else
                    {
                        Beaprint.InfoPrint("    CVE-2025-59287: WSUS endpoint discovered at " + wsusPolicyValue + ". Confirm patch level before attempting exploitation.");
                        if (!string.IsNullOrEmpty(useWUServerValue))
                            Beaprint.InfoPrint("    UseWUServer is set to " + useWUServerValue + ", clients may still reach Microsoft Update.");
                    }
                }

                string wsusSetupPath = @"SOFTWARE\Microsoft\Update Services\Server\Setup";
                string wsusVersion = RegistryHelper.GetRegValue("HKLM", wsusSetupPath, "VersionString");
                string wsusInstallPath = RegistryHelper.GetRegValue("HKLM", wsusSetupPath, "InstallPath");
                bool wsusRoleDetected = !string.IsNullOrEmpty(wsusVersion) || !string.IsNullOrEmpty(wsusInstallPath);

                if (TryGetServiceStateAndAccount("WSUSService", out string wsusServiceState, out string wsusServiceAccount))
                {
                    wsusRoleDetected = true;
                    string serviceMsg = "    WSUSService status: " + wsusServiceState;
                    if (!string.IsNullOrEmpty(wsusServiceAccount))
                        serviceMsg += " (runs as " + wsusServiceAccount + ")";
                    Beaprint.BadPrint(serviceMsg);
                }

                if (wsusRoleDetected)
                {
                    if (!string.IsNullOrEmpty(wsusVersion))
                        Beaprint.BadPrint("    WSUS Server version: " + wsusVersion + " (verify patch level for CVE-2025-59287).");
                    if (!string.IsNullOrEmpty(wsusInstallPath))
                        Beaprint.InfoPrint("    WSUS install path: " + wsusInstallPath);
                    Beaprint.BadPrint("    CVE-2025-59287: Local WSUS server exposes an unauthenticated deserialization surface reachable over HTTP(S). Patch or restrict access.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static bool TryGetServiceStateAndAccount(string serviceName, out string state, out string account)
        {
            state = string.Empty;
            account = string.Empty;

            try
            {
                string query = $"SELECT Name, State, StartName FROM Win32_Service WHERE Name='{serviceName.Replace("'", "''")}'";
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2", query))
                {
                    foreach (ManagementObject service in searcher.Get())
                    {
                        state = service["State"]?.ToString() ?? string.Empty;
                        account = service["StartName"]?.ToString() ?? string.Empty;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return false;
        }

        static void PrintKrbRelayUp()
        {
            try
            {
                Beaprint.MainPrint("Checking KrbRelayUp", "T1187,T1558");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#krbrelayup");

                var policy = Ntlm.GetLocalDcLdapPolicy();
                if (policy.Role == LocalDomainRole.DomainController)
                {
                    if (policy.HasObservedRelayCondition)
                    {
                        Beaprint.BadPrint("  Local DC LDAP policy shows a relay precondition; this does not establish a usable coercion or account path.");
                        Beaprint.InfoPrint("  LDAP signing: " + policy.SigningStatus + "; LDAPS channel binding: " + policy.ChannelBindingStatus + ".");
                    }
                    else
                        Beaprint.InfoPrint("  No permissive local DC LDAP policy was established; see the server policy values below.");
                }
                else if (policy.Role == LocalDomainRole.Member && !string.IsNullOrEmpty(Checks.CurrentAdDomainName))
                    Beaprint.InfoPrint("  Domain member: DC server-side LDAP policy is unknown from this host.");
                else if (policy.Role == LocalDomainRole.Unknown)
                    Beaprint.InfoPrint("  Local domain role and DC server-side LDAP policy are unknown.");
                else
                {
                    Beaprint.InfoPrint("  No current AD domain was identified; local relay prerequisites were not assessed.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintInsideContainer()
        {
            try
            {
                Beaprint.MainPrint("Checking If Inside Container", "T1613");
                Beaprint.LinkPrint("", "If the binary cexecsvc.exe or associated service exists, you are inside Docker");
                Dictionary<string, object> regVal = RegistryHelper.GetRegValues("HKLM", @"System\CurrentControlSet\Services\cexecsvc");
                bool cexecsvcExist = File.Exists(Environment.SystemDirectory + @"\cexecsvc.exe");
                if (regVal != null || cexecsvcExist)
                {
                    Beaprint.BadPrint("You are inside a container");
                }
                else
                {
                    Beaprint.GoodPrint("You are NOT inside a container");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintAlwaysInstallElevated()
        {
            try
            {
                Beaprint.MainPrint("Checking AlwaysInstallElevated", "T1548.002");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#alwaysinstallelevated");
                string path = "Software\\Policies\\Microsoft\\Windows\\Installer";
                string HKLM_AIE = RegistryHelper.GetRegValue("HKLM", path, "AlwaysInstallElevated");
                string HKCU_AIE = RegistryHelper.GetRegValue("HKCU", path, "AlwaysInstallElevated");
                Beaprint.GrayPrint("    HKLM policy value: " + DescribeAlwaysInstallElevatedValue(HKLM_AIE));
                Beaprint.GrayPrint("    HKCU policy value: " + DescribeAlwaysInstallElevatedValue(HKCU_AIE));

                switch (AssessAlwaysInstallElevated(HKLM_AIE, HKCU_AIE))
                {
                    case AlwaysInstallElevatedStatus.BothEnabled:
                        Beaprint.BadPrint("    Both AlwaysInstallElevated policy values are 1: review elevated MSI installation for this user (value types and effective policy are unverified).");
                        break;
                    case AlwaysInstallElevatedStatus.OneEnabled:
                        Beaprint.GrayPrint("    Only one AlwaysInstallElevated policy value is 1; the two-policy condition for unmanaged elevated MSI installation is not confirmed.");
                        break;
                    default:
                        Beaprint.GrayPrint("    The two-policy AlwaysInstallElevated condition is not confirmed.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintObjectManagerRaceAmplification()
        {
            try
            {
                Beaprint.MainPrint("Object Manager race-window amplification primitives", "T1068");
                Beaprint.LinkPrint("https://projectzero.google/2025/12/windows-exploitation-techniques.html", "Project Zero write-up:");

                if (ObjectManagerHelper.TryCreateSessionEvent(out var objectName, out var error))
                {
                    Beaprint.BadPrint($"    Created a test named event ({objectName}) under \\BaseNamedObjects.");
                    Beaprint.InfoPrint("    -> Low-privileged users can slow NtOpen*/NtCreate* lookups using ~32k-character names or ~16k-level directory chains.");
                    Beaprint.InfoPrint("    -> Point attacker-controlled symbolic links to the slow path to stretch kernel race windows.");
                    Beaprint.InfoPrint("    -> Use this whenever a bug follows check -> NtOpenX -> privileged action patterns.");
                }
                else
                {
                    Beaprint.InfoPrint($"    Could not create a test event under \\BaseNamedObjects ({error}). The namespace might be locked down.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintClfsAuthentication()
        {
            try
            {
                ClfsAuthenticationReport report = ClfsAuthentication.GetReport();
                if (report.Status == ClfsAuthenticationStatus.NotAvailable)
                {
                    return;
                }

                Beaprint.MainPrint("CLFS logfile authentication mitigation", "T1068");
                Beaprint.LinkPrint(
                    "https://support.microsoft.com/en-us/servicing/os/windows/2025/03/common-log-file-system-clfs-authentication-mitigation",
                    "CLFS authentication blocks maliciously modified logfiles before the kernel driver parses them.");

                switch (report.Status)
                {
                    case ClfsAuthenticationStatus.Enforced:
                        Beaprint.GoodPrint("    Enforced (Mode 0): CLFS rejects logfiles with missing or invalid authentication codes.");
                        break;
                    case ClfsAuthenticationStatus.Learning:
                        Beaprint.InfoPrint("    Learning mode (Mode 1): unauthenticated logfiles are still accepted during the adoption period.");
                        if (report.EnforcementTransitionPeriod.HasValue)
                        {
                            Beaprint.InfoPrint($"    Automatic enforcement transition: {report.EnforcementTransitionPeriod.Value} seconds.");
                        }
                        break;
                    case ClfsAuthenticationStatus.LearningWithoutAutoEnforcement:
                        Beaprint.BadPrint("    Learning mode has no automatic enforcement transition (Mode 1, EnforcementTransitionPeriod 0).");
                        break;
                    case ClfsAuthenticationStatus.DisabledByAdministrator:
                        Beaprint.BadPrint("    Disabled by an administrator (Mode 2): maliciously modified CLFS logfiles are not rejected by this mitigation.");
                        break;
                    case ClfsAuthenticationStatus.DisabledBySystem:
                        Beaprint.BadPrint("    Disabled automatically by the system (Mode 3): maliciously modified CLFS logfiles are not rejected by this mitigation.");
                        break;
                    case ClfsAuthenticationStatus.AccessDenied:
                        Beaprint.InfoPrint("    The CLFS authentication registry configuration could not be read (access denied).");
                        break;
                    default:
                        string mode = report.Mode.HasValue ? report.Mode.Value.ToString() : "missing or invalid";
                        Beaprint.InfoPrint($"    Unknown CLFS authentication mode: {mode}.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintNtlmSettings()
        {
            Beaprint.MainPrint($"Enumerating NTLM Settings", "T1557.001");

            try
            {
                var info = Ntlm.GetNtlmSettingsInfo();

                string lmCompatibilityLevelColor = info.LanmanCompatibilityLevel >= 3 ? Beaprint.ansi_color_good : Beaprint.ansi_color_bad;
                Beaprint.ColorPrint($"  LanmanCompatibilityLevel    : {info.LanmanCompatibilityLevel} ({info.LanmanCompatibilityLevelString})\n", lmCompatibilityLevelColor);

                var ntlmSettingsColors = new Dictionary<string, string>
                {
                    { "True", Beaprint.ansi_color_good },
                    { "False", Beaprint.ansi_color_bad },
                    { "No signing", Beaprint.ansi_color_bad},
                    { "null", Beaprint.ansi_color_bad},
                    { "Require Signing", Beaprint.ansi_color_good},
                    { "Negotiate signing", Beaprint.ansi_color_yellow},
                    { "Unknown", Beaprint.ansi_color_bad},
                };

                Beaprint.ColorPrint("\n  NTLM Signing Settings", Beaprint.LBLUE);
                Beaprint.AnsiPrint($"      ClientRequireSigning    : {info.ClientRequireSigning}\n" +
                                   $"      ClientNegotiateSigning  : {info.ClientNegotiateSigning}\n" +
                                   $"      ServerRequireSigning    : {info.ServerRequireSigning}\n" +
                                   $"      ServerNegotiateSigning  : {info.ServerNegotiateSigning}\n" +
                                   $"      LDAP client signing     : {(info.LdapSigning != null ? info.LdapSigning.ToString() : "missing")} ({info.LdapSigningString})",
                                   ntlmSettingsColors);

                var dcPolicy = info.DcLdapPolicy;
                Beaprint.ColorPrint("\n  Local DC LDAP server policy", Beaprint.LBLUE);
                if (dcPolicy.Role != LocalDomainRole.DomainController)
                    Beaprint.InfoPrint("      DC server policy         : unknown (" + dcPolicy.Role + "); client signing cannot substitute for it.");
                else
                {
                    Beaprint.NoColorPrint("      LDAPServerIntegrity     : " + FormatDcPolicyValue(dcPolicy.SigningReadState, dcPolicy.SigningValue) + " (" + dcPolicy.SigningStatus + ")");
                    Beaprint.NoColorPrint("      LdapEnforceChannelBinding: " + FormatDcPolicyValue(dcPolicy.ChannelBindingReadState, dcPolicy.ChannelBindingValue) + " (" + dcPolicy.ChannelBindingStatus + "; LDAPS only)");
                    if (dcPolicy.SigningReadState == LdapPolicyReadState.Missing)
                        Beaprint.InfoPrint(dcPolicy.Generation == DomainControllerGeneration.Before2025
                            ? "      Absent signing policy: older DC default does not require signing."
                            : "      Absent signing policy: effective default unknown; new Server 2025 deployments have different enforcement defaults.");
                    if (dcPolicy.ChannelBindingStatus == LdapsChannelBindingStatus.WhenSupported)
                        Beaprint.InfoPrint("      When Supported is partial LDAPS protection: capable clients need a valid channel binding token.");
                }

                Beaprint.ColorPrint("\n  Session Security", Beaprint.LBLUE);

                if (info.NTLMMinClientSec != null)
                {
                    var clientSessionSecurity = (SessionSecurity)info.NTLMMinClientSec;
                    var clientSessionSecurityDescription = clientSessionSecurity.GetDescription();
                    var color = !clientSessionSecurity.HasFlag(SessionSecurity.NTLMv2) && !clientSessionSecurity.HasFlag(SessionSecurity.Require128BitKey) ?
                                                              Beaprint.ansi_color_bad :
                                                              Beaprint.ansi_color_good;
                    Beaprint.ColorPrint($"      NTLMMinClientSec        : {info.NTLMMinClientSec} ({clientSessionSecurityDescription})", color);

                    if (info.LanmanCompatibilityLevel < 3 && !clientSessionSecurity.HasFlag(SessionSecurity.NTLMv2))
                    {
                        Beaprint.BadPrint("        [!] NTLM clients support NTLMv1!");
                    }
                }

                if (info.NTLMMinServerSec != null)
                {
                    var serverSessionSecurity = (SessionSecurity)info.NTLMMinServerSec;
                    var serverSessionSecurityDescription = serverSessionSecurity.GetDescription();
                    var color = !serverSessionSecurity.HasFlag(SessionSecurity.NTLMv2) && !serverSessionSecurity.HasFlag(SessionSecurity.Require128BitKey) ?
                                                             Beaprint.ansi_color_bad :
                                                             Beaprint.ansi_color_good;
                    Beaprint.ColorPrint($"      NTLMMinServerSec        : {info.NTLMMinServerSec} ({serverSessionSecurityDescription})\n", color);

                    if (info.LanmanCompatibilityLevel < 3 && !serverSessionSecurity.HasFlag(SessionSecurity.NTLMv2))
                    {
                        Beaprint.BadPrint("        [!] NTLM services on this machine support NTLMv1!");
                    }
                }

                var ntlmOutboundRestrictionsColor = info.OutboundRestrictions == 2 ? Beaprint.ansi_color_good : Beaprint.ansi_color_bad;

                Beaprint.ColorPrint("\n  NTLM Auditing and Restrictions", Beaprint.LBLUE);
                Beaprint.NoColorPrint($"      InboundRestrictions     : {info.InboundRestrictions} ({info.InboundRestrictionsString})");
                Beaprint.ColorPrint($"      OutboundRestrictions    : {info.OutboundRestrictions} ({info.OutboundRestrictionsString})", ntlmOutboundRestrictionsColor);
                Beaprint.NoColorPrint($"      InboundAuditing         : {info.InboundAuditing} ({info.InboundRestrictionsString})");
                Beaprint.NoColorPrint($"      OutboundExceptions      : {info.OutboundExceptions}");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static string FormatDcPolicyValue(LdapPolicyReadState state, uint? value)
        {
            if (state == LdapPolicyReadState.Missing) return "missing";
            if (state == LdapPolicyReadState.Error) return "unavailable (read error)";
            return value.HasValue ? value.Value.ToString() : "unavailable (invalid DWORD)";
        }

        private static void PrintPrintNightmarePointAndPrint()
        {
            Beaprint.MainPrint("PrintNightmare PointAndPrint Policies", "T1068");
            Beaprint.LinkPrint("https://support.microsoft.com/en-us/servicing/os/windows/2021/07/kb5005010-restricting-installation-of-new-printer-drivers-after-applying-the-july-6-2021-updates", "Microsoft Point and Print policy guidance");

            try
            {
                string key = @"Software\Policies\Microsoft\Windows NT\Printers\PointAndPrint";
                var restrict = RegistryHelper.GetDwordValue("HKLM", key, "RestrictDriverInstallationToAdministrators");
                var noWarn = RegistryHelper.GetDwordValue("HKLM", key, "NoWarningNoElevationOnInstall");
                var updatePrompt = RegistryHelper.GetDwordValue("HKLM", key, "UpdatePromptSettings");

                Beaprint.NoColorPrint($"      RestrictDriverInstallationToAdministrators: {(restrict.HasValue ? restrict.Value.ToString() : "not set or unavailable")}\n" +
                                      $"      NoWarningNoElevationOnInstall: {(noWarn.HasValue ? noWarn.Value.ToString() : "not set or unavailable")}\n" +
                                      $"      UpdatePromptSettings: {(updatePrompt.HasValue ? updatePrompt.Value.ToString() : "not set or unavailable")}");

                string spoolerState = _spoolerReport == null ? "unknown" :
                    !string.IsNullOrEmpty(_spoolerReport.State) ? _spoolerReport.State :
                    _spoolerReport.ServiceExists && _spoolerReport.RegistryStart == 4 ? "disabled (registry)" : "unknown";
                Beaprint.NoColorPrint("      Spooler state (earlier service check): " + spoolerState);

                switch (AssessPointAndPrintPolicy(restrict, noWarn, updatePrompt))
                {
                    case PointAndPrintPolicyStatus.ExplicitAdminOnly:
                        Beaprint.InfoPrint("      Administrator-only driver installation is explicitly configured; verify installed security updates and effective policy.");
                        break;
                    case PointAndPrintPolicyStatus.ReviewCandidate:
                        Beaprint.BadPrint("      [!] Point and Print policy review candidate: an explicit value permits or weakens non-administrator driver installation.");
                        break;
                    case PointAndPrintPolicyStatus.UnknownValue:
                        Beaprint.InfoPrint("      Unrecognized administrator restriction value; review effective policy.");
                        break;
                    default:
                        Beaprint.InfoPrint("      No explicit weak value found; an absent restriction has update-dependent defaults.");
                        break;
                }
                Beaprint.NoColorPrint("      Registry values alone do not establish exploitability; verify security updates, effective policy, Spooler state and access.");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintPrintersWMIInfo()
        {
            Beaprint.MainPrint("Enumerating Printers (WMI)", "T1082");

            try
            {
                foreach (var printer in Printers.GetPrinterWMIInfos())
                {
                    Beaprint.NoColorPrint($"      Name:                    {printer.Name}\n" +
                                                 $"      Status:                  {printer.Status}\n" +
                                                 $"      Sddl:                    {printer.Sddl}\n" +
                                                 $"      Is default:              {printer.IsDefault}\n" +
                                                 $"      Is network printer:      {printer.IsNetworkPrinter}\n");
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                //Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintPrinterDriverAclReview()
        {
            Beaprint.MainPrint("Printer driver support-file ACL candidates", "T1574");
            try
            {
                var report = Printers.GetDriverAclReview();
                if (!report.RootPresent)
                {
                    Beaprint.NotFoundPrint();
                    return;
                }

                Beaprint.NoColorPrint("      Fixed-depth ProgramData driver-support metadata review; no DLL is opened or loaded.");
                foreach (var finding in report.Findings)
                {
                    Beaprint.BadPrint($"      ACL allow candidate: {finding.Path} ({finding.Rights})");
                }
                Beaprint.NoColorPrint($"      Reviewed {report.DriversInspected} driver folders and {report.DllsInspected} DLL paths." +
                    (report.Partial ? " Result is partial (limit or access error)." : ""));
                Beaprint.NoColorPrint("      Verify effective ACLs and denies, installed driver/load path, service identity, and vendor patch state before treating a candidate as exploitable.");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintNamedPipes()
        {
            Beaprint.MainPrint("Enumerating Named Pipes", "T1559");

            try
            {
                string formatString = "  {0,-100} {1,-70} {2}\n";

                Beaprint.NoColorPrint(string.Format($"{formatString}", "Name", "CurrentUserPerms", "Sddl"));

                foreach (var namedPipe in NamedPipes.GetNamedPipeInfos())
                {
                    var colors = new Dictionary<string, string>
                    {
                        {namedPipe.CurrentUserPerms.Replace("[","\\[").Replace("]","\\]"), Beaprint.ansi_color_bad },
                    };

                    Beaprint.AnsiPrint(string.Format(formatString, namedPipe.Name, namedPipe.CurrentUserPerms, namedPipe.Sddl), colors);
                }
            }
            catch (Exception ex)
            {
                //Beaprint.PrintException(ex.Message);
            }
        }


        private static void PrintNamedPipeAbuseCandidates()
        {
            Beaprint.MainPrint("Named Pipes with Low-Priv Write Access to Privileged Servers", "T1134.001,T1559");

            try
            {
                var candidates = NamedPipeSecurityAnalyzer.GetNamedPipeAbuseCandidates().ToList();

                if (!candidates.Any())
                {
                    Beaprint.NoColorPrint("      No risky named pipe ACLs were found.\n");
                    return;
                }

                foreach (var candidate in candidates)
                {
                    var aclSummary = candidate.LowPrivilegeAces.Any()
                        ? string.Join("; ", candidate.LowPrivilegeAces.Select(ace =>
                            $"{ace.Principal} [{ace.RightsDescription}]").Where(s => !string.IsNullOrEmpty(s)))
                        : "Unknown";

                    var serverSummary = candidate.Processes.Any()
                        ? string.Join("; ", candidate.Processes.Select(proc =>
                            $"{proc.ProcessName} (PID {proc.Pid}, {proc.UserName ?? proc.UserSid})"))
                        : "No privileged handles observed (service idle or access denied)";

                    var color = candidate.HasPrivilegedServer ? Beaprint.ansi_color_bad : Beaprint.ansi_color_yellow;

                    Beaprint.ColorPrint($"    \\\\.\\pipe\\{candidate.Name}", color);
                    Beaprint.NoColorPrint($"      Low-priv ACLs  : {aclSummary}");
                    Beaprint.NoColorPrint($"      Observed owners: {serverSummary}");
                    Beaprint.NoColorPrint($"      SDDL           : {candidate.Sddl}");
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintAMSIProviders()
        {
            Beaprint.MainPrint("Enumerating AMSI registered providers", "T1562.001");

            try
            {
                var providers = RegistryHelper.GetRegSubkeys("HKLM", @"SOFTWARE\Microsoft\AMSI\Providers") ?? new string[] { };

                foreach (var provider in providers)
                {
                    var providerPath = RegistryHelper.GetRegValue("HKLM", $"SOFTWARE\\Classes\\CLSID\\{provider}\\InprocServer32", "");

                    Beaprint.NoColorPrint($"    Provider:       {provider}\n" +
                                          $"    Path:           {providerPath}\n");

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception)
            {
            }
        }

        private void PrintSysmon()
        {
            PrintSysmonConfiguration();
            PrintSysmonEventLogs();
        }

        private void PrintSysmonConfiguration()
        {
            Beaprint.MainPrint("Enumerating Sysmon configuration", "T1518.001");

            Dictionary<string, string> colors = new Dictionary<string, string>
            {
                { SysMon.NotDefined, Beaprint.ansi_color_bad },
                { "False", Beaprint.ansi_color_bad },
            };

            try
            {
                if (!MyUtils.IsHighIntegrity())
                {
                    Beaprint.NoColorPrint("      You must be an administrator to run this check");
                    return;
                }

                foreach (var item in SysMon.GetSysMonInfos())
                {
                    Beaprint.AnsiPrint($"      Installed:                {item.Installed}\n" +
                                       $"      Hashing Algorithm:        {item.HashingAlgorithm.GetDescription()}\n" +
                                       $"      Options:                  {item.Options.GetDescription()}\n" +
                                       $"      Rules:                    {item.Rules}\n",
                                          colors);
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception)
            {
            }
        }

        private void PrintSysmonEventLogs()
        {
            Beaprint.MainPrint("Enumerating Sysmon process creation logs (1)", "T1654");

            try
            {
                if (!MyUtils.IsHighIntegrity())
                {
                    Beaprint.NoColorPrint("      You must be an administrator to run this check");
                    return;
                }

                foreach (var item in SysMon.GetSysMonEventInfos())
                {
                    Beaprint.BadPrint($"      EventID:                  {item.EventID}\n" +
                                      $"      User Name:                {item.UserName}\n" +
                                      $"      Time Created:             {item.TimeCreated}\n");
                    Beaprint.PrintLineSeparator();
                }

            }
            catch (Exception)
            {
            }
        }

        internal static IEnumerable<string> FormatDefenderPathExclusions(
            IList<string> pathExclusions, IList<string> policyManagerPathExclusions)
        {
            if (pathExclusions.Count != 0)
            {
                yield return "\n  Path Exclusions:";
                foreach (var path in pathExclusions)
                    yield return $"    {path}";
            }

            if (policyManagerPathExclusions.Count != 0)
            {
                yield return "\n  PolicyManagerPathExclusions:";
                foreach (var path in policyManagerPathExclusions)
                    yield return $"    {path}";
            }
        }

        internal static string ExtractHistoricalDefenderExclusion(string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
                return null;

            foreach (var kind in new[] { "Paths", "Processes", "Extensions" })
            {
                var marker = "\\Exclusions\\" + kind + "\\";
                var start = newValue.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (start < 0)
                    continue;

                var name = newValue.Substring(start + marker.Length);
                var equals = name.IndexOf(" = ", StringComparison.Ordinal);
                if (equals >= 0)
                    name = name.Substring(0, equals);
                name = name.Replace('\r', ' ').Replace('\n', ' ').Trim();
                if (name.Length == 0)
                    return null;
                return kind + ": " + (name.Length > 160 ? name.Substring(0, 160) + "..." : name);
            }
            return null;
        }

        internal static string ExtractDefenderEventNewValue(string eventXml)
        {
            if (string.IsNullOrEmpty(eventXml) || eventXml.Length > 32768)
                return null;
            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                using (var xmlReader = XmlReader.Create(new StringReader(eventXml), settings))
                {
                    var document = XDocument.Load(xmlReader);
                    return document.Descendants().Where(element => element.Name.LocalName == "Data")
                        .Where(element => string.Equals((string)element.Attribute("Name"), "New Value",
                            StringComparison.OrdinalIgnoreCase))
                        .Select(element => element.Value).FirstOrDefault();
                }
            }
            catch (XmlException)
            {
                return null;
            }
        }

        private static IList<string> GetRecentDefenderExclusionChanges()
        {
            var changes = new List<string>();
            try
            {
                // Query only recent configuration events, newest first; never scan an entire log.
                const string query = "*[System[(EventID=5007) and TimeCreated[timediff(@SystemTime) <= 604800000]]]";
                using (var reader = MyUtils.GetEventLogReader("Microsoft-Windows-Windows Defender/Operational", query))
                {
                    var started = System.Diagnostics.Stopwatch.StartNew();
                    for (var inspected = 0; inspected < 48 && changes.Count < 5 &&
                        started.ElapsedMilliseconds < 1500; inspected++)
                    {
                        using (var record = reader.ReadEvent(TimeSpan.FromMilliseconds(200)))
                        {
                            if (record == null)
                                break;
                            var exclusion = ExtractHistoricalDefenderExclusion(
                                ExtractDefenderEventNewValue(record.ToXml()));
                            if (exclusion != null)
                                changes.Add("    " + record.TimeCreated?.ToString("u") + " " + exclusion);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Defender or its operational log may be absent or inaccessible.
            }
            return changes;
        }

        private static void PrintWindowsDefenderInfo()
        {
            Beaprint.MainPrint("Windows Defender configuration", "T1518.001");

            Beaprint.ColorPrint("  Defender engine CVE-2026-41091", Beaprint.LBLUE);
            Beaprint.LinkPrint(
                "https://msrc.microsoft.com/update-guide/vulnerability/CVE-2026-41091",
                "Actively exploited link-following LPE; Microsoft fixed the engine in 1.1.26040.8");

            var engineInfo = WindowsDefender.GetDefenderEngineInfo();
            var engineStatus = WindowsDefender.AssessCve41091Engine(engineInfo?.EngineVersion);
            if (engineStatus == DefenderCve41091Status.Candidate && engineInfo.ServiceEnabled == true)
            {
                Beaprint.BadPrint("    Microsoft Defender engine " + engineInfo.EngineVersion +
                    " predates the CVE-2026-41091 fix and the antimalware service is enabled. This is a local SYSTEM escalation candidate; update the Defender engine.");
            }
            else if (engineStatus == DefenderCve41091Status.Candidate && engineInfo.ServiceEnabled == false)
            {
                Beaprint.InfoPrint("    Microsoft Defender engine " + engineInfo.EngineVersion +
                    " predates the CVE-2026-41091 fix, but WMI reports the antimalware service disabled. Exposure is not confirmed; update before enabling Defender.");
            }
            else if (engineStatus == DefenderCve41091Status.Candidate)
            {
                Beaprint.InfoPrint("    Microsoft Defender engine " + engineInfo.EngineVersion +
                    " predates the CVE-2026-41091 fix, but service state is unavailable. Verify that Defender is active and update the engine.");
            }
            else if (engineStatus == DefenderCve41091Status.Fixed)
            {
                Beaprint.GoodPrint("    Microsoft Defender engine " + engineInfo.EngineVersion +
                    " is at or above the CVE-2026-41091 fixed version (1.1.26040.8).");
            }
            else
            {
                Beaprint.GrayPrint("    Defender engine version unavailable or unparsable; CVE-2026-41091 status is unknown.");
            }

            void DisplayDefenderSettings(WindowsDefenderSettings settings)
            {
                var pathExclusions = settings.PathExclusions;
                var processExclusions = settings.ProcessExclusions;
                var extensionExclusions = settings.ExtensionExclusions;
                var asrSettings = settings.AsrSettings;

                foreach (var line in FormatDefenderPathExclusions(pathExclusions, settings.PolicyManagerPathExclusions))
                    Beaprint.NoColorPrint(line);

                if (processExclusions.Count != 0)
                {
                    Beaprint.NoColorPrint("\n  Process Exclusions");
                    foreach (var process in processExclusions)
                    {
                        Beaprint.NoColorPrint($"    {process}");
                    }
                }

                if (extensionExclusions.Count != 0)
                {
                    Beaprint.NoColorPrint("\n  Extension Exclusions");
                    foreach (var ext in extensionExclusions)
                    {
                        Beaprint.NoColorPrint($"    {ext}");
                    }
                }

                if (asrSettings.Enabled)
                {
                    Beaprint.NoColorPrint("\n  Attack Surface Reduction Rules:\n");

                    Beaprint.NoColorPrint($"    {"State",-10} Rule\n");
                    foreach (var rule in asrSettings.Rules)
                    {
                        string state;
                        if (rule.State == 0)
                            state = "Disabled";
                        else if (rule.State == 1)
                            state = "Blocked";
                        else if (rule.State == 2)
                            state = "Audited";
                        else
                            state = $"{rule.State} - Unknown";

                        var asrRule = _asrGuids.ContainsKey(rule.Rule.ToString())
                            ? _asrGuids[rule.Rule.ToString()]
                            : $"{rule.Rule} - Please report this";

                        Beaprint.NoColorPrint($"    {state,-10} {asrRule}");
                    }

                    if (asrSettings.Exclusions.Count > 0)
                    {
                        Beaprint.NoColorPrint("\n  ASR Exclusions:");
                        foreach (var exclusion in asrSettings.Exclusions)
                        {
                            Beaprint.NoColorPrint($"    {exclusion}");
                        }
                    }
                }
            }

            try
            {
                var info = WindowsDefender.GetDefenderSettingsInfo();

                Beaprint.ColorPrint("  Local Settings", Beaprint.LBLUE);
                DisplayDefenderSettings(info.LocalSettings);

                Beaprint.ColorPrint("  Group Policy Settings", Beaprint.LBLUE);
                DisplayDefenderSettings(info.GroupPolicySettings);

                if (info.LocalSettings.PathExclusions.Count == 0 &&
                    info.LocalSettings.PolicyManagerPathExclusions.Count == 0 &&
                    info.LocalSettings.ProcessExclusions.Count == 0 &&
                    info.LocalSettings.ExtensionExclusions.Count == 0 &&
                    info.GroupPolicySettings.PathExclusions.Count == 0 &&
                    info.GroupPolicySettings.PolicyManagerPathExclusions.Count == 0 &&
                    info.GroupPolicySettings.ProcessExclusions.Count == 0 &&
                    info.GroupPolicySettings.ExtensionExclusions.Count == 0)
                {
                    var changes = GetRecentDefenderExclusionChanges();
                    if (changes.Count != 0)
                    {
                        Beaprint.NoColorPrint("\n  Recent exclusion changes in Defender event log (historical; verify current policy):");
                        foreach (var change in changes)
                            Beaprint.NoColorPrint(change);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        private static void PrintDotNetVersions()
        {
            try
            {
                Beaprint.MainPrint("Installed .NET versions\n", "T1082");

                var info = DotNet.GetDotNetInfo();

                Beaprint.ColorPrint("  CLR Versions", Beaprint.LBLUE);
                foreach (var version in info.ClrVersions)
                {
                    Beaprint.NoColorPrint($"   {version}");
                }

                Beaprint.ColorPrint("\n  .NET Versions", Beaprint.LBLUE);
                foreach (var version in info.DotNetVersions)
                {
                    Beaprint.NoColorPrint($"   {version}");
                }

                var colors = new Dictionary<string, string>
                {
                    { "True", Beaprint.ansi_color_good },
                    { "False", Beaprint.ansi_color_bad },
                };

                Beaprint.ColorPrint("\n  .NET & AMSI (Anti-Malware Scan Interface) support", Beaprint.LBLUE);
                Beaprint.AnsiPrint($"      .NET version supports AMSI     : {info.IsAmsiSupportedByDotNet}\n" +
                                   $"      OS supports AMSI               : {info.IsAmsiSupportedByOs}",
                                    colors);

                var highestVersion = info.HighestVersion;
                var lowestVersion = info.LowestVersion;

                if ((highestVersion.Major == DotNetInfo.AmsiSupportedByDotNetMinMajorVersion) && (highestVersion.Minor >= DotNetInfo.AmsiSupportedByDotNetMinMinorVersion))
                {
                    Beaprint.NoColorPrint($"        [!] The highest .NET version is enrolled in AMSI!");
                }

                if (
                    info.IsAmsiSupportedByOs &&
                    info.IsAmsiSupportedByDotNet &&
                    ((
                         (lowestVersion.Major == DotNetInfo.AmsiSupportedByDotNetMinMajorVersion - 1)
                     ) ||
                     ((lowestVersion.Major == DotNetInfo.AmsiSupportedByDotNetMinMajorVersion) && (lowestVersion.Minor < DotNetInfo.AmsiSupportedByDotNetMinMinorVersion)))
                )
                {
                    Beaprint.NoColorPrint($"        [-] You can invoke .NET version {lowestVersion.Major}.{lowestVersion.Minor} to bypass AMSI.");
                }
            }
            catch (Exception e)
            {
            }
        }

        private static void PrintSystemLastShutdownTime()
        {
            try
            {
                Beaprint.MainPrint("System Last Shutdown Date/time (from Registry)\n", "T1082");

                var shutdownBytes = RegistryHelper.GetRegValueBytes("HKLM", "SYSTEM\\ControlSet001\\Control\\Windows", "ShutdownTime");
                if (shutdownBytes != null)
                {
                    var shutdownInt = BitConverter.ToInt64(shutdownBytes, 0);
                    var shutdownTime = DateTime.FromFileTime(shutdownInt);

                    Beaprint.NoColorPrint($"    Last Shutdown Date/time        :    {shutdownTime}");
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void PrintLSAInfo()
        {
            try
            {
                Beaprint.MainPrint("Enumerate LSA settings - auth packages included\n", "T1547.005");

                var settings = RegistryHelper.GetRegValues("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Lsa");

                if ((settings != null) && (settings.Count != 0))
                {
                    foreach (var kvp in settings)
                    {
                        var val = string.Empty;

                        if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.String"))
                        {
                            val = string.Join(",", (string[])kvp.Value);
                        }
                        else if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.Byte"))
                        {
                            val = BitConverter.ToString((byte[])kvp.Value);
                        }
                        else
                        {
                            val = kvp.Value.ToString();
                        }

                        var key = kvp.Key;

                        Beaprint.NoColorPrint($"    {key,-30}       :       {val}");

                        if (Regex.IsMatch(key, "Security Packages") && Regex.IsMatch(val, @".*wdigest.*"))
                        {
                            Beaprint.BadPrint("    [!]      WDigest is enabled - plaintext password extraction is possible!");
                        }

                        if (key.Equals("RunAsPPL", StringComparison.InvariantCultureIgnoreCase) && val == "1")
                        {
                            Beaprint.BadPrint("    [!]      LSASS Protected Mode is enabled! You will not be able to access lsass.exe's memory easily.");
                        }

                        if (key.Equals("DisableRestrictedAdmin", StringComparison.InvariantCultureIgnoreCase) && val == "0")
                        {
                            Beaprint.BadPrint("    [!]      RDP Restricted Admin Mode is enabled! You can use pass-the-hash to access RDP on this system.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void PrintLocalGroupPolicy()
        {
            try
            {
                Beaprint.MainPrint("Display Local Group Policy settings - local users/machine", "T1082");

                var infos = GroupPolicy.GetLocalGroupPolicyInfos();

                foreach (var info in infos)
                {
                    Beaprint.NoColorPrint($"   Type             :     {info.GPOType}\n" +
                                                $"   Display Name     :     {info.DisplayName}\n" +
                                                $"   Name             :     {info.GPOName}\n" +
                                                $"   Extensions       :     {info.Extensions}\n" +
                                                $"   File Sys Path    :     {info.FileSysPath}\n" +
                                                $"   Link             :     {info.Link}\n" +
                                                $"   GPO Link         :     {info.GPOLink.GetDescription()}\n" +
                                                $"   Options          :     {info.Options.GetDescription()}\n");

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void PrintPotentialGPOAbuse()
        {
            try
            {
                Beaprint.MainPrint("Potential GPO abuse vectors (applied domain GPOs writable by current user)", "T1484.001");

                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.NoColorPrint("    Host is not joined to a domain or domain info is unavailable.");
                    return;
                }

                // Build a friendly group list for the current user to quickly spot interesting memberships
                var currentGroups = winPEAS.Info.UserInfo.User.GetUserGroups(Checks.CurrentUserName, Checks.CurrentUserDomainName) ?? new System.Collections.Generic.List<string>();
                var hasGPCO = currentGroups.Any(g => string.Equals(g, "Group Policy Creator Owners", System.StringComparison.InvariantCultureIgnoreCase));

                if (hasGPCO)
                {
                    Beaprint.BadPrint("    [!] Current user is member of 'Group Policy Creator Owners' — can create/own new GPOs. If you can link a GPO to an OU that applies here, you can execute code as SYSTEM via scheduled task/startup script.");
                }

                var infos = GroupPolicy.GetLocalGroupPolicyInfos();

                bool anyFinding = false;
                foreach (var info in infos)
                {
                    var fileSysPath = info.FileSysPath?.ToString();
                    if (string.IsNullOrEmpty(fileSysPath))
                    {
                        continue;
                    }

                    // Only look at domain GPOs stored in SYSVOL
                    var isSysvolPath = fileSysPath.StartsWith(@"\", System.StringComparison.InvariantCultureIgnoreCase) &&
                                       fileSysPath.IndexOf(@"\SysVol\", System.StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                                       fileSysPath.IndexOf(@"\Policies\", System.StringComparison.InvariantCultureIgnoreCase) >= 0;

                    if (!isSysvolPath)
                    {
                        continue;
                    }

                    // Check write/equivalent permissions on common abuse locations inside the GPO
                    var pathsToCheck = new System.Collections.Generic.List<string>
                    {
                        fileSysPath,
                        System.IO.Path.Combine(fileSysPath, @"Machine\Scripts\Startup"),
                        System.IO.Path.Combine(fileSysPath, @"User\Scripts\Logon"),
                        System.IO.Path.Combine(fileSysPath, @"Machine\Preferences\ScheduledTasks"),
                        System.IO.Path.Combine(fileSysPath, @"Machine\Microsoft\Windows NT\SecEdit")
                    };

                    foreach (var p in pathsToCheck)
                    {
                        var perms = PermissionsHelper.GetPermissionsFolder(p, Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);
                        if (perms != null && perms.Count > 0)
                        {
                            if (!anyFinding)
                            {
                                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/index.html", "Why it matters");
                            }
                            anyFinding = true;
                            Beaprint.BadPrint("    [!] Applied GPO path write candidate");
                            Beaprint.NoColorPrint($"        GPO Display Name : {info.DisplayName}");
                            Beaprint.NoColorPrint($"        GPO Name         : {info.GPOName}");
                            Beaprint.NoColorPrint($"        GPO Link         : {info.Link}");
                            Beaprint.NoColorPrint($"        Path             : {p}");
                            foreach (var entry in perms)
                            {
                                Beaprint.NoColorPrint($"          -> {entry}");
                            }
                            Beaprint.GrayPrint("        Review effective share/GPT and GPC rights, scope, and filtering. An applicable machine-side immediate task can run at policy refresh; startup scripts wait for startup, and user logon scripts run at logon under that user.");
                        }
                    }
                }

                if (!anyFinding && !hasGPCO)
                {
                    Beaprint.NoColorPrint("    No obvious GPO abuse via writable SYSVOL paths or GPCO membership detected.");
                }
            }
            catch (Exception ex)
            {
                // Avoid noisy stack traces in normal runs
                Beaprint.GrayPrint($"    [!] Error while checking potential GPO abuse: {ex.Message}");
            }
        }


        private static void PrintPowerShellSessionSettings()
        {
            try
            {
                Beaprint.MainPrint("Enumerating PowerShell Session Settings using the registry", "T1059.001");

                if (!MyUtils.IsHighIntegrity())
                {
                    Beaprint.NoColorPrint("      You must be an administrator to run this check");
                    return;
                }

                var infos = PowerShell.GetPowerShellSessionSettingsInfos();

                foreach (var info in infos)
                {
                    Beaprint.NoColorPrint($"    {"Name",-38} {info.Plugin}");

                    foreach (var access in info.Permissions)
                    {
                        Beaprint.NoColorPrint($"      {access.Principal,-35}  {access.Permission,-22}");
                    }

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void PrintRegistryCreds()
        {
            try
            {
                Beaprint.MainPrint("Enumerating saved credentials in Registry (CurrentPass)", "T1552.002");
                string currentPass = "CurrentPass";
                var hive = "HKLM";
                var path = "System";
                var controlSet = "ControlSet";

                var colors = new Dictionary<string, string>
                {
                    { currentPass, Beaprint.ansi_color_bad }
                };

                var subkeys = RegistryHelper.GetRegSubkeys(hive, path);

                foreach (var subkey in subkeys.Where(i => i.Contains(controlSet)))
                {
                    try
                    {
                        var subPath = @$"{path}\{subkey}\Control";
                        var key = $@"{hive}\{subPath}\{currentPass}";
                        var value = RegistryHelper.GetRegValue(hive, subPath, currentPass);

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            Beaprint.AnsiPrint($@"    {key,-60}   :   {value}", colors);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
