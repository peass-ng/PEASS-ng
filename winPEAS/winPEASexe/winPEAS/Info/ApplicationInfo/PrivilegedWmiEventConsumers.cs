using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using winPEAS.Helpers;

namespace winPEAS.Info.ApplicationInfo
{
    internal sealed class PrivilegedWmiEventConsumerFinding
    {
        public string ConsumerType { get; set; }
        public string ConsumerName { get; set; }
        public string TargetPath { get; set; }
        public string AccessReason { get; set; }
    }

    internal sealed class PrivilegedWmiEventConsumerReport
    {
        public List<PrivilegedWmiEventConsumerFinding> Findings { get; } = new List<PrivilegedWmiEventConsumerFinding>();
        public int BindingsInspected { get; set; }
        public int BoundConsumersInspected { get; set; }
        public int TargetsInspected { get; set; }
        public bool BindingLimitReached { get; set; }
        public bool ConsumerLimitReached { get; set; }
        public bool TargetLimitReached { get; set; }
        public bool FindingLimitReached { get; set; }
        public bool TimeLimitReached { get; set; }
        public string Error { get; set; }
        internal Stopwatch InspectionTimer { get; } = Stopwatch.StartNew();
    }

    internal sealed class WmiCommandParts
    {
        public string Executable { get; set; }
        public string Arguments { get; set; }
    }

    internal static class PrivilegedWmiEventConsumers
    {
        internal const int MaxBindings = 512;
        internal const int MaxConsumers = 256;
        internal const int MaxTargets = 512;
        internal const int MaxFindings = 64;
        internal const int MaxInspectionMilliseconds = 8000;

        private static readonly TimeSpan WmiQueryTimeout = TimeSpan.FromSeconds(2);

        internal static PrivilegedWmiEventConsumerReport GetReport()
        {
            var report = new PrivilegedWmiEventConsumerReport();

            try
            {
                HashSet<string> boundConsumerPaths = GetBoundConsumerPaths(report);
                if (boundConsumerPaths == null || ShouldStop(report))
                {
                    return report;
                }

                HashSet<string> unprivilegedSids = PermissionsHelper.GetUnprivilegedTokenSids();
                if (unprivilegedSids == null || unprivilegedSids.Count == 0)
                {
                    report.Error = "Could not resolve low-privilege security identifiers.";
                    return report;
                }

                try
                {
                    InspectCommandLineConsumers(boundConsumerPaths, unprivilegedSids, report);
                }
                catch (Exception ex)
                {
                    AppendError(report, "Could not enumerate command-line consumers: " + ex.Message);
                }

                if (!ShouldStop(report))
                {
                    try
                    {
                        InspectActiveScriptConsumers(boundConsumerPaths, unprivilegedSids, report);
                    }
                    catch (Exception ex)
                    {
                        AppendError(report, "Could not enumerate script consumers: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                report.Error = ex.Message;
            }

            return report;
        }

        private static HashSet<string> GetBoundConsumerPaths(PrivilegedWmiEventConsumerReport report)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var searcher = CreateSearcher("SELECT Consumer FROM __FilterToConsumerBinding"))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject binding in results)
                    {
                        using (binding)
                        {
                            if (report.BindingsInspected >= MaxBindings)
                            {
                                report.BindingLimitReached = true;
                                break;
                            }

                            report.BindingsInspected++;
                            string consumerReference = GetString(binding, "Consumer");
                            string normalized = NormalizeConsumerReference(consumerReference);
                            if (!string.IsNullOrEmpty(normalized))
                            {
                                paths.Add(normalized);
                            }
                        }

                        if (ShouldStop(report))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                report.Error = "Could not enumerate WMI event-consumer bindings: " + ex.Message;
                return null;
            }

            return paths;
        }

        private static void InspectCommandLineConsumers(
            ISet<string> boundConsumerPaths,
            ISet<string> unprivilegedSids,
            PrivilegedWmiEventConsumerReport report)
        {
            using (var searcher = CreateSearcher("SELECT * FROM CommandLineEventConsumer"))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach (ManagementObject consumer in results)
                {
                    using (consumer)
                    {
                        if (ShouldStopConsumers(report))
                        {
                            break;
                        }

                        if (!IsBound(consumer, boundConsumerPaths))
                        {
                            continue;
                        }

                        report.BoundConsumersInspected++;
                        string name = GetString(consumer, "Name") ?? "<unnamed>";
                        List<string> targets = GetCommandLineTargetPaths(
                            GetString(consumer, "ExecutablePath"),
                            GetString(consumer, "CommandLineTemplate"),
                            GetString(consumer, "WorkingDirectory"));

                        InspectTargets("CommandLineEventConsumer", name, targets, unprivilegedSids, report);
                    }
                }
            }
        }

        private static void InspectActiveScriptConsumers(
            ISet<string> boundConsumerPaths,
            ISet<string> unprivilegedSids,
            PrivilegedWmiEventConsumerReport report)
        {
            using (var searcher = CreateSearcher("SELECT * FROM ActiveScriptEventConsumer"))
            using (ManagementObjectCollection results = searcher.Get())
            {
                foreach (ManagementObject consumer in results)
                {
                    using (consumer)
                    {
                        if (ShouldStopConsumers(report))
                        {
                            break;
                        }

                        if (!IsBound(consumer, boundConsumerPaths))
                        {
                            continue;
                        }

                        report.BoundConsumersInspected++;
                        string name = GetString(consumer, "Name") ?? "<unnamed>";
                        string scriptPath = PrivilegedScheduledTasks.ResolveActionPath(
                            ExpandAndTrim(GetString(consumer, "ScriptFileName")),
                            null);

                        var targets = new List<string>();
                        if (IsStableTargetPath(scriptPath))
                        {
                            targets.Add(scriptPath);
                        }

                        InspectTargets("ActiveScriptEventConsumer", name, targets, unprivilegedSids, report);
                    }
                }
            }
        }

        private static void InspectTargets(
            string consumerType,
            string consumerName,
            IEnumerable<string> targets,
            ISet<string> unprivilegedSids,
            PrivilegedWmiEventConsumerReport report)
        {
            foreach (string target in targets)
            {
                if (ShouldStop(report))
                {
                    return;
                }

                if (report.TargetsInspected >= MaxTargets)
                {
                    report.TargetLimitReached = true;
                    return;
                }

                report.TargetsInspected++;
                string accessReason = PrivilegedScheduledTasks.GetWritableTargetReason(target, unprivilegedSids);
                if (string.IsNullOrEmpty(accessReason))
                {
                    continue;
                }

                report.Findings.Add(new PrivilegedWmiEventConsumerFinding
                {
                    ConsumerType = consumerType,
                    ConsumerName = consumerName,
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

        internal static List<string> GetCommandLineTargetPaths(
            string executablePath,
            string commandLineTemplate,
            string workingDirectory)
        {
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string expandedExecutable = ExpandAndTrim(executablePath);
            string expandedWorkingDirectory = ExpandAndTrim(workingDirectory);
            WmiCommandParts command = SplitCommandLineTemplate(commandLineTemplate);
            string effectiveExecutable = !string.IsNullOrEmpty(expandedExecutable)
                ? expandedExecutable
                : command.Executable;

            string resolvedExecutable = PrivilegedScheduledTasks.ResolveActionPath(
                effectiveExecutable,
                expandedWorkingDirectory);
            if (IsStableTargetPath(resolvedExecutable))
            {
                targets.Add(resolvedExecutable);
            }

            foreach (string referencedPath in PrivilegedScheduledTasks.ExtractReferencedFilePaths(
                effectiveExecutable,
                command.Arguments,
                expandedWorkingDirectory))
            {
                if (IsStableTargetPath(referencedPath))
                {
                    targets.Add(referencedPath);
                }
            }

            return new List<string>(targets);
        }

        internal static WmiCommandParts SplitCommandLineTemplate(string commandLineTemplate)
        {
            string commandLine = ExpandAndTrim(commandLineTemplate);
            var result = new WmiCommandParts
            {
                Executable = string.Empty,
                Arguments = string.Empty,
            };

            if (string.IsNullOrEmpty(commandLine))
            {
                return result;
            }

            if (commandLine[0] == '"')
            {
                int closingQuote = commandLine.IndexOf('"', 1);
                if (closingQuote > 1)
                {
                    result.Executable = commandLine.Substring(1, closingQuote - 1);
                    result.Arguments = commandLine.Substring(closingQuote + 1).TrimStart();
                    return result;
                }
            }

            Match executableMatch = Regex.Match(
                commandLine,
                @"^.+?\.(?:exe|com|bat|cmd)(?=\s|$)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (executableMatch.Success)
            {
                result.Executable = executableMatch.Value.Trim();
                result.Arguments = commandLine.Substring(executableMatch.Length).TrimStart();
                return result;
            }

            int firstSpace = commandLine.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
            if (firstSpace < 0)
            {
                result.Executable = commandLine;
            }
            else
            {
                result.Executable = commandLine.Substring(0, firstSpace);
                result.Arguments = commandLine.Substring(firstSpace + 1).TrimStart();
            }

            return result;
        }

        internal static string NormalizeConsumerReference(string consumerReference)
        {
            if (string.IsNullOrWhiteSpace(consumerReference))
            {
                return null;
            }

            try
            {
                return new ManagementPath(consumerReference).RelativePath;
            }
            catch
            {
                return consumerReference.Trim();
            }
        }

        private static bool IsBound(ManagementObject consumer, ISet<string> boundConsumerPaths)
        {
            try
            {
                if (consumer == null || consumer.Path == null || boundConsumerPaths == null)
                {
                    return false;
                }

                string relativePath = NormalizeConsumerReference(consumer.Path.Path);
                return !string.IsNullOrEmpty(relativePath) && boundConsumerPaths.Contains(relativePath);
            }
            catch
            {
                return false;
            }
        }

        private static ManagementObjectSearcher CreateSearcher(string query)
        {
            var scope = new ManagementScope(@"\\.\root\subscription");
            var options = new EnumerationOptions
            {
                ReturnImmediately = true,
                Rewindable = false,
                Timeout = WmiQueryTimeout,
            };
            return new ManagementObjectSearcher(scope, new ObjectQuery(query), options);
        }

        private static string GetString(ManagementBaseObject instance, string propertyName)
        {
            try
            {
                object value = instance[propertyName];
                return value == null ? null : value.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string ExpandAndTrim(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : Environment.ExpandEnvironmentVariables(value).Trim();
        }

        private static bool IsStableTargetPath(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && path.IndexOf('%') < 0;
        }

        private static void AppendError(PrivilegedWmiEventConsumerReport report, string error)
        {
            report.Error = string.IsNullOrEmpty(report.Error)
                ? error
                : report.Error + " " + error;
        }

        private static bool ShouldStopConsumers(PrivilegedWmiEventConsumerReport report)
        {
            if (report.BoundConsumersInspected >= MaxConsumers)
            {
                report.ConsumerLimitReached = true;
                return true;
            }

            return ShouldStop(report);
        }

        private static bool ShouldStop(PrivilegedWmiEventConsumerReport report)
        {
            if (report == null)
            {
                return true;
            }

            if (report.InspectionTimer.ElapsedMilliseconds >= MaxInspectionMilliseconds)
            {
                report.TimeLimitReached = true;
            }

            if (report.TargetsInspected >= MaxTargets)
            {
                report.TargetLimitReached = true;
            }

            if (report.Findings.Count >= MaxFindings)
            {
                report.FindingLimitReached = true;
            }

            return report.TimeLimitReached ||
                report.TargetLimitReached ||
                report.FindingLimitReached;
        }
    }
}
