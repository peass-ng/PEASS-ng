using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Win32;
using winPEAS.Helpers;
using winPEAS.TaskScheduler;
using ScheduledTask = winPEAS.TaskScheduler.Task;

namespace winPEAS.Info.ApplicationInfo
{
    internal enum RecallPolicyConfigurationStatus
    {
        NotPresent,
        Disabled,
        UnexpectedConfiguration,
        PotentiallyVulnerable,
        Patched,
        NotAffected,
        UnknownBuild,
        InspectionFailed,
    }

    internal sealed class RecallPolicyConfigurationReport
    {
        public RecallPolicyConfigurationStatus Status { get; set; }
        public bool TaskPresent { get; set; }
        public bool? TaskEnabled { get; set; }
        public bool ConfigurationRead { get; set; }
        public bool HasSystemPrincipal { get; set; }
        public bool HasExpectedComHandler { get; set; }
        public bool HasRecallWnfTrigger { get; set; }
        public int ExpectedWnfStateCount { get; set; }
        public bool HasSessionUnlockTrigger { get; set; }
        public bool? AllowStartOnDemand { get; set; }
        public List<string> WnfStateNames { get; } = new List<string>();
        public string ProductName { get; set; }
        public int? Build { get; set; }
        public int? Revision { get; set; }
        public string RequiredUpdate { get; set; }
        public string RequiredBuild { get; set; }
        public string Error { get; set; }

        public string BuildString
        {
            get
            {
                if (!Build.HasValue || !Revision.HasValue)
                {
                    return "unknown";
                }

                return string.Format("10.0.{0}.{1}", Build.Value, Revision.Value);
            }
        }

        public bool HasExpectedTaskMarkers
        {
            get { return HasSystemPrincipal && HasExpectedComHandler && HasRecallWnfTrigger; }
        }
    }

    internal static class RecallPolicyConfiguration
    {
        internal const string TaskPath = @"\Microsoft\Windows\WindowsAI\Recall\PolicyConfiguration";
        internal const string ComHandlerClassId = "{0BE6820D-B667-4CB6-931B-C153A77DA895}";
        internal const string RecallPolicyWnfState = "7508BCA32C079E41";
        private const string CurrentVersionRegistryPath = @"Software\Microsoft\Windows NT\CurrentVersion";
        private const string TaskCacheTreeRegistryPath = @"Software\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\Microsoft\Windows\WindowsAI\Recall\PolicyConfiguration";
        private const int Windows11FinalFixRevision = 7623;
        private const int Server2025FinalFixRevision = 32230;

        internal static readonly HashSet<string> ExpectedWnfStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            RecallPolicyWnfState,
            "7508BCA32C0F8241",
            "7528BCA32C079E41",
            "7510BCA338038113",
        };

        public static RecallPolicyConfigurationReport GetReport()
        {
            var report = new RecallPolicyConfigurationReport
            {
                Status = RecallPolicyConfigurationStatus.NotPresent,
            };
            bool taskCacheEntryPresent = false;

            try
            {
                report.ProductName = GetCurrentVersionValue("ProductName");
                report.Build = ParseRegistryInteger("CurrentBuildNumber");
                report.Revision = ParseRegistryInteger("UBR");
                taskCacheEntryPresent = IsTaskRegisteredInCache();

                using (ScheduledTask task = TaskService.Instance.GetTask(TaskPath))
                {
                    if (task == null)
                    {
                        if (taskCacheEntryPresent)
                        {
                            report.TaskPresent = true;
                            report.Status = RecallPolicyConfigurationStatus.InspectionFailed;
                            report.Error = "The task is registered but its definition is not readable through the Task Scheduler API.";
                        }
                        return report;
                    }

                    report.TaskPresent = true;
                    try
                    {
                        report.TaskEnabled = task.Enabled;
                    }
                    catch
                    {
                        report.TaskEnabled = null;
                    }

                    AnalyzeTaskXml(task.Xml, report);
                }

                if (!report.ConfigurationRead)
                {
                    report.Status = RecallPolicyConfigurationStatus.InspectionFailed;
                }
                else if (!report.HasExpectedTaskMarkers)
                {
                    report.Status = RecallPolicyConfigurationStatus.UnexpectedConfiguration;
                }
                else if (report.TaskEnabled == false)
                {
                    report.Status = RecallPolicyConfigurationStatus.Disabled;
                }
                else if (!report.TaskEnabled.HasValue)
                {
                    report.Status = RecallPolicyConfigurationStatus.InspectionFailed;
                    report.Error = "Unable to determine whether the task is enabled.";
                }
                else
                {
                    report.Status = AssessBuild(
                        report.ProductName,
                        report.Build,
                        report.Revision,
                        out string requiredUpdate,
                        out string requiredBuild);
                    report.RequiredUpdate = requiredUpdate;
                    report.RequiredBuild = requiredBuild;
                }
            }
            catch (Exception ex)
            {
                // The scheduler API can fail for an access-restricted task even when its
                // TaskCache entry proves that the task is registered.
                report.TaskPresent = report.TaskPresent || taskCacheEntryPresent;
                report.Status = RecallPolicyConfigurationStatus.InspectionFailed;
                report.Error = ex.Message;
            }

            return report;
        }

        internal static void AnalyzeTaskXml(string taskXml, RecallPolicyConfigurationReport report)
        {
            if (report == null || string.IsNullOrWhiteSpace(taskXml))
            {
                return;
            }

            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(taskXml);

            report.HasSystemPrincipal = HasNodeValue(document, "UserId", "S-1-5-18");
            report.HasExpectedComHandler = HasNodeValue(document, "ClassId", ComHandlerClassId);
            report.HasSessionUnlockTrigger = HasNodeValue(document, "StateChange", "SessionUnlock");

            XmlNode allowStartOnDemand = SelectFirstNode(document, "AllowStartOnDemand");
            if (allowStartOnDemand != null && bool.TryParse(allowStartOnDemand.InnerText, out bool allowDemandStart))
            {
                report.AllowStartOnDemand = allowDemandStart;
            }

            XmlNodeList wnfTriggers = document.SelectNodes("//*[local-name()='WnfStateChangeTrigger']");
            if (wnfTriggers != null)
            {
                var seenStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XmlNode trigger in wnfTriggers)
                {
                    XmlNode stateNode = trigger.SelectSingleNode("./*[local-name()='StateName']");
                    string stateName = stateNode == null ? "" : stateNode.InnerText.Trim();
                    if (stateName.Length == 0 || !seenStates.Add(stateName))
                    {
                        continue;
                    }

                    report.WnfStateNames.Add(stateName);
                    if (ExpectedWnfStates.Contains(stateName))
                    {
                        report.ExpectedWnfStateCount++;
                    }
                    if (stateName.Equals(RecallPolicyWnfState, StringComparison.OrdinalIgnoreCase))
                    {
                        report.HasRecallWnfTrigger = true;
                    }
                }
            }

            report.ConfigurationRead = true;
        }

        internal static RecallPolicyConfigurationStatus AssessBuild(
            string productName,
            int? build,
            int? revision,
            out string requiredUpdate,
            out string requiredBuild)
        {
            requiredUpdate = "";
            requiredBuild = "";

            if (!build.HasValue || !revision.HasValue)
            {
                return RecallPolicyConfigurationStatus.UnknownBuild;
            }

            bool isServer2025 = (productName ?? "").IndexOf("Server 2025", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isServer2025)
            {
                requiredUpdate = "KB5073379";
                requiredBuild = "10.0.26100.32230";
                if (build.Value < 26100)
                {
                    return RecallPolicyConfigurationStatus.NotAffected;
                }
                if (build.Value > 26100)
                {
                    return RecallPolicyConfigurationStatus.Patched;
                }

                return revision.Value < Server2025FinalFixRevision
                    ? RecallPolicyConfigurationStatus.PotentiallyVulnerable
                    : RecallPolicyConfigurationStatus.Patched;
            }

            requiredUpdate = "KB5074109";
            if (build.Value == 26100)
            {
                requiredBuild = "10.0.26100.7623";
                return revision.Value < Windows11FinalFixRevision
                    ? RecallPolicyConfigurationStatus.PotentiallyVulnerable
                    : RecallPolicyConfigurationStatus.Patched;
            }
            if (build.Value == 26200)
            {
                requiredBuild = "10.0.26200.7623";
                return revision.Value < Windows11FinalFixRevision
                    ? RecallPolicyConfigurationStatus.PotentiallyVulnerable
                    : RecallPolicyConfigurationStatus.Patched;
            }
            if (build.Value < 26100)
            {
                return RecallPolicyConfigurationStatus.NotAffected;
            }
            if (build.Value > 26200)
            {
                return RecallPolicyConfigurationStatus.Patched;
            }

            return RecallPolicyConfigurationStatus.UnknownBuild;
        }

        private static int? ParseRegistryInteger(string valueName)
        {
            string value = GetCurrentVersionValue(valueName);
            return int.TryParse(value, out int parsed) ? (int?)parsed : null;
        }

        private static string GetCurrentVersionValue(string valueName)
        {
            try
            {
                using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey versionKey = localMachine.OpenSubKey(CurrentVersionRegistryPath))
                {
                    return versionKey == null ? "" : string.Format("{0}", versionKey.GetValue(valueName));
                }
            }
            catch
            {
                return RegistryHelper.GetRegValue("HKLM", CurrentVersionRegistryPath, valueName);
            }
        }

        private static bool IsTaskRegisteredInCache()
        {
            try
            {
                using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey taskKey = localMachine.OpenSubKey(TaskCacheTreeRegistryPath))
                {
                    return taskKey != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool HasNodeValue(XmlDocument document, string localName, string expectedValue)
        {
            XmlNodeList nodes = document.SelectNodes("//*[local-name()='" + localName + "']");
            if (nodes == null)
            {
                return false;
            }

            foreach (XmlNode node in nodes)
            {
                if (string.Equals(node.InnerText.Trim(), expectedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static XmlNode SelectFirstNode(XmlDocument document, string localName)
        {
            return document.SelectSingleNode("//*[local-name()='" + localName + "']");
        }
    }
}
