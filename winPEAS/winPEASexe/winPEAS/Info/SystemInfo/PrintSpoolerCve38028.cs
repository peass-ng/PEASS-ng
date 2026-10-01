using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Management;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo
{
    internal static class PrintSpoolerCve38028
    {
        private const string SpoolerRegistryPath = @"SYSTEM\CurrentControlSet\Services\Spooler";

        // Microsoft Security Response Center, October 2022 security updates.
        // Windows 8.1/Server 2012 use rollup/security-only KBs because their
        // CurrentBuild/UBR values are not reliable cumulative-update indicators.
        private static readonly Dictionary<int, FixedBuild> FixedBuilds = new Dictionary<int, FixedBuild>
        {
            { 9200,  new FixedBuild(0, "5018457", "5018478") },
            { 9600,  new FixedBuild(0, "5018474", "5018476") },
            { 10240, new FixedBuild(19507, "5018425") },
            { 14393, new FixedBuild(5427, "5018411") },
            { 17763, new FixedBuild(3532, "5018419") },
            { 19042, new FixedBuild(2130, "5018410") },
            { 19043, new FixedBuild(2130, "5018410") },
            { 19044, new FixedBuild(2130, "5018410") },
            { 20348, new FixedBuild(1129, "5018421") },
            { 22000, new FixedBuild(1098, "5018418") },
            { 22621, new FixedBuild(674, "5018427") },
        };

        internal static PrintSpoolerCve38028Report GetReport(Dictionary<string, string> basicInfo)
        {
            var report = new PrintSpoolerCve38028Report
            {
                ProductName = GetValue(basicInfo, "ProductName"),
                Architecture = GetValue(basicInfo, "Architecture"),
                DisplayVersion = GetValue(basicInfo, "DisplayVersion"),
            };

            CollectPatchState(report, basicInfo);
            CollectRegistryState(report);
            CollectWmiState(report);
            CollectRegistryAcl(report);

            report.ServiceAvailable = report.ServiceExists &&
                !string.Equals(report.StartMode, "Disabled", StringComparison.OrdinalIgnoreCase) &&
                report.RegistryStart != 4;
            return report;
        }

        private static void CollectPatchState(PrintSpoolerCve38028Report report, Dictionary<string, string> basicInfo)
        {
            if (!int.TryParse(GetValue(basicInfo, "CurrentBuild"), out int build))
            {
                report.PatchStatus = PrintSpoolerPatchStatus.Unknown;
                report.PatchEvidence = "Unable to parse CurrentBuild.";
                return;
            }

            report.Build = build;
            bool revisionKnown = int.TryParse(GetValue(basicInfo, "UpdateBuildRevision"), out int revision);
            if (revisionKnown)
            {
                report.Revision = revision;
                report.BuildVersion = build + "." + revision;
            }
            else
            {
                report.BuildVersion = build.ToString();
            }

            if (!FixedBuilds.TryGetValue(build, out FixedBuild fixedBuild))
            {
                report.PatchStatus = PrintSpoolerPatchStatus.NotAffected;
                report.PatchEvidence = "Microsoft does not list this Windows build as affected by CVE-2022-38028.";
                return;
            }

            report.BuildApplicable = true;
            report.FixedRevision = fixedBuild.Revision;
            report.FixedVersion = fixedBuild.Revision > 0 ? build + "." + fixedBuild.Revision : "KB-based";
            report.ApplicableKbs.AddRange(fixedBuild.Kbs);

            foreach (string kb in fixedBuild.Kbs)
            {
                if (WindowsVersionVulns.IsHotfixInstalledOrSuperseded(basicInfo, kb))
                {
                    report.PatchStatus = PrintSpoolerPatchStatus.Patched;
                    report.PatchEvidence = "The applicable fix KB or a superseding update is present in the hotfix inventory.";
                    report.MatchedFixKb = kb;
                    return;
                }
            }

            if (fixedBuild.Revision > 0 && revisionKnown)
            {
                if (revision >= fixedBuild.Revision)
                {
                    report.PatchStatus = PrintSpoolerPatchStatus.Patched;
                    report.PatchEvidence = "OS build is at or above Microsoft's October 2022 fixed build.";
                }
                else
                {
                    report.PatchStatus = PrintSpoolerPatchStatus.Susceptible;
                    report.PatchEvidence = "OS build is below Microsoft's fixed build and no applicable or superseding fix KB was found.";
                }
                return;
            }

            report.PatchStatus = PrintSpoolerPatchStatus.Susceptible;
            report.PatchEvidence = "No applicable or superseding October 2022 fix KB was found for this affected legacy build.";
        }

        private static void CollectRegistryState(PrintSpoolerCve38028Report report)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(SpoolerRegistryPath))
                {
                    if (key == null)
                    {
                        return;
                    }

                    report.ServiceExists = true;
                    report.RegistryImagePath = Convert.ToString(key.GetValue("ImagePath", ""));
                    report.RegistryObjectName = Convert.ToString(key.GetValue("ObjectName", ""));
                    report.RegistryStart = TryConvertInt(key.GetValue("Start"));
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Spooler registry state: " + ex.Message);
            }
        }

        private static void CollectWmiState(PrintSpoolerCve38028Report report)
        {
            try
            {
                const string query = "SELECT Name,DisplayName,PathName,StartMode,State,Started,StartName FROM Win32_Service WHERE Name='Spooler'";
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2", query))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject service in results)
                    {
                        report.ServiceExists = true;
                        report.DisplayName = Convert.ToString(service["DisplayName"]);
                        report.PathName = Convert.ToString(service["PathName"]);
                        report.StartMode = Convert.ToString(service["StartMode"]);
                        report.State = Convert.ToString(service["State"]);
                        report.StartName = Convert.ToString(service["StartName"]);
                        report.Started = Convert.ToBoolean(service["Started"]);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Spooler WMI state: " + ex.Message);
            }
        }

        private static void CollectRegistryAcl(PrintSpoolerCve38028Report report)
        {
            try
            {
                if (RegistryAclScanner.TryGetWritableKey("HKLM", SpoolerRegistryPath, out RegistryWritableKeyInfo writableKey))
                {
                    report.RegistryWritableByLowPrivilegePrincipal = true;
                    report.RegistryWritablePrincipals.AddRange(writableKey.Principals);
                    report.RegistryWritableRights.AddRange(writableKey.Rights);
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Spooler registry ACL: " + ex.Message);
            }
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

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            return values != null && values.TryGetValue(key, out string value) ? value ?? "" : "";
        }

        private sealed class FixedBuild
        {
            internal FixedBuild(int revision, params string[] kbs)
            {
                Revision = revision;
                Kbs = kbs ?? new string[0];
            }

            internal int Revision { get; }
            internal string[] Kbs { get; }
        }
    }

    internal enum PrintSpoolerPatchStatus
    {
        Unknown,
        NotAffected,
        Susceptible,
        Patched
    }

    internal sealed class PrintSpoolerCve38028Report
    {
        public string ProductName { get; set; } = "";
        public string Architecture { get; set; } = "";
        public string DisplayVersion { get; set; } = "";
        public int Build { get; set; }
        public int Revision { get; set; }
        public string BuildVersion { get; set; } = "";
        public bool BuildApplicable { get; set; }
        public int FixedRevision { get; set; }
        public string FixedVersion { get; set; } = "";
        public List<string> ApplicableKbs { get; } = new List<string>();
        public string MatchedFixKb { get; set; } = "";
        public PrintSpoolerPatchStatus PatchStatus { get; set; } = PrintSpoolerPatchStatus.Unknown;
        public string PatchEvidence { get; set; } = "";
        public bool ServiceExists { get; set; }
        public bool ServiceAvailable { get; set; }
        public bool Started { get; set; }
        public string DisplayName { get; set; } = "";
        public string PathName { get; set; } = "";
        public string StartMode { get; set; } = "";
        public string State { get; set; } = "";
        public string StartName { get; set; } = "";
        public string RegistryImagePath { get; set; } = "";
        public string RegistryObjectName { get; set; } = "";
        public int? RegistryStart { get; set; }
        public bool RegistryWritableByLowPrivilegePrincipal { get; set; }
        public List<string> RegistryWritablePrincipals { get; } = new List<string>();
        public List<string> RegistryWritableRights { get; } = new List<string>();
        public List<string> CollectionErrors { get; } = new List<string>();

        public bool HighPriorityFinding =>
            PatchStatus == PrintSpoolerPatchStatus.Susceptible && ServiceAvailable;
    }
}
