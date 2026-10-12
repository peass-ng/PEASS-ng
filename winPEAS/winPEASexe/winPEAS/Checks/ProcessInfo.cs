using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using winPEAS.Helpers;
using winPEAS.Info.ProcessInfo;

namespace winPEAS.Checks
{
    internal class ProcessInfo : ISystemCheck
    {
        public string[] MitreAttackIds { get; } = new[] { "T1057", "T1134.001" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Processes Information", "T1057,T1134.001");

            new List<Action>
            {
                PrintInterestingProcesses,
                PrintProcessTokenExposure,
                PrintVulnLeakedHandlers,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        void PrintInterestingProcesses()
        {
            try
            {
                Beaprint.MainPrint("Interesting Processes -non Microsoft-", "T1057");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#running-processes", "Check if any interesting processes for memory dump or if you could overwrite some binary running");
                List<Dictionary<string, string>> processesInfo = ProcessesInfo.GetProcInfo();

                foreach (Dictionary<string, string> procInfo in processesInfo)
                {
                    Dictionary<string, string> colorsP = new Dictionary<string, string>()
                        {
                            { " " + Checks.CurrentUserName, Beaprint.ansi_current_user },
                            { "Permissions:.*", Beaprint.ansi_color_bad },
                            { "Possible DLL Hijacking.*", Beaprint.ansi_color_bad },
                        };

                    // we need to find first occurrence of the procinfo name
                    string processNameSanitized = procInfo["Name"].Trim().ToLower();

                    if (DefensiveProcesses.AVVendorsByProcess.ContainsKey(processNameSanitized))
                    {
                        if (DefensiveProcesses.AVVendorsByProcess[processNameSanitized].Count > 0)
                        {
                            procInfo["Product"] = string.Join(", ", DefensiveProcesses.AVVendorsByProcess[processNameSanitized]);
                        }
                        colorsP[procInfo["Product"]] = Beaprint.ansi_color_good;
                    }
                    else if (InterestingProcesses.Definitions.ContainsKey(procInfo["Name"]))
                    {
                        if (!string.IsNullOrEmpty(InterestingProcesses.Definitions[procInfo["Name"]]))
                        {
                            procInfo["Product"] = InterestingProcesses.Definitions[procInfo["Name"]];
                        }
                        colorsP[procInfo["Product"]] = Beaprint.ansi_color_bad;
                    }

                    List<string> fileRights = PermissionsHelper.GetPermissionsFile(procInfo["ExecutablePath"], Checks.CurrentUserSiDs);
                    List<string> dirRights = new List<string>();
                    if (procInfo["ExecutablePath"] != null && procInfo["ExecutablePath"] != "")
                        dirRights = PermissionsHelper.GetPermissionsFolder(Path.GetDirectoryName(procInfo["ExecutablePath"]), Checks.CurrentUserSiDs);

                    colorsP[procInfo["ExecutablePath"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+", "\\+") + "[^\"^']"] = (fileRights.Count > 0 || dirRights.Count > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good;

                    string formString = "    {0}({1})[{2}]";
                    if (procInfo["Product"] != null && procInfo["Product"].Length > 1)
                        formString += ": {3}";
                    if (procInfo["Owner"].Length > 1)
                        formString += " -- POwn: {4}";
                    if (procInfo["isDotNet"].Length > 1)
                        formString += " -- {5}";
                    if (fileRights.Count > 0)
                        formString += "\n    Permissions: {6}";
                    if (dirRights.Count > 0)
                        formString += "\n    Possible DLL Hijacking folder: {7} ({8})";
                    if (procInfo["CommandLine"].Length > 1)
                        formString += "\n    " + Beaprint.ansi_color_gray + "Command Line: {9}";


                    Beaprint.AnsiPrint(string.Format(formString, procInfo["Name"], procInfo["ProcessID"], procInfo["ExecutablePath"], procInfo["Product"], procInfo["Owner"], procInfo["isDotNet"], string.Join(", ", fileRights), dirRights.Count > 0 ? Path.GetDirectoryName(procInfo["ExecutablePath"]) : "", string.Join(", ", dirRights), procInfo["CommandLine"]), colorsP);
                    Beaprint.PrintLineSeparator();
                }

                var moduleDirectoryReview = ProcessModuleDirectoryReview.FindLeads(
                    processesInfo, Checks.CurrentUserName,
                    path => PermissionsHelper.GetPermissionsFolder(path, Checks.CurrentUserSiDs,
                        PermissionType.WRITEABLE_OR_EQUIVALENT));
                foreach (var lead in moduleDirectoryReview.Leads)
                    Beaprint.BadPrint("    Writable module-directory review lead: " + lead.ProcessName +
                        " (owner " + lead.Owner + ") -> " + lead.Directory + " (" + lead.Permissions +
                        "; confirm effective ACL, process privilege, and an actual DLL load from this path)");
                if (moduleDirectoryReview.Truncated)
                    Beaprint.GrayPrint("    Module-directory review truncated at its process, path, or finding limit; additional paths are unknown.");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(ex.Message);
            }
        }

        void PrintProcessTokenExposure()
        {
            try
            {
                Beaprint.MainPrint("Privileged and service process token exposure", "T1134.001");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#token-manipulation", "Review token impersonation and duplication prerequisites");
                Beaprint.InfoPrint("Read-only access probes only: granted token rights are conditional leads, not proof that impersonation or CreateProcessAsUser will succeed.");

                ProcessTokenExposureReport report = ProcessTokenExposure.Inspect();
                int displayed = 0;
                int omitted = 0;
                const int maxDisplayed = 128;
                foreach (ProcessTokenExposureInfo process in report.Processes)
                {
                    if (!process.ShouldDisplay) continue;
                    if (displayed >= maxDisplayed)
                    {
                        ++omitted;
                        continue;
                    }
                    ++displayed;

                    string services = process.Services.Count == 0 ? "none" : string.Join(", ",
                        process.Services.Take(8).Select(service => service.Name + " [" + service.Account + "]")) +
                        (process.Services.Count > 8 ? ", ..." : "");
                    string heading = string.Format("    {0}({1}) - {2}", process.ProcessName,
                        process.ProcessId, process.AccountKind);
                    if (process.IsConditionalTokenAbuseCandidate)
                        Beaprint.BadPrint(heading + " - conditional token-duplication candidate");
                    else
                        Beaprint.NoColorPrint(heading);
                    Beaprint.NoColorPrint("      Path: " + process.ExecutablePath);
                    Beaprint.NoColorPrint("      Services: " + services);
                    Beaprint.NoColorPrint("      Token: " + process.TokenUser + " [" + process.TokenSid +
                        "]; logon session: " + process.LogonSession + "; integrity: " + process.Integrity);
                    Beaprint.GrayPrint("      Open access: " + process.AccessSummary);
                    if (process.IsConditionalTokenAbuseCandidate)
                        Beaprint.GrayPrint("      A different higher-value token was openable with TOKEN_DUPLICATE. Token type, enabled privileges, process protection and the intended impersonation/process-creation path still require validation.");
                    Beaprint.PrintLineSeparator();
                }

                int candidates = report.Processes.Count(process => process.IsConditionalTokenAbuseCandidate);
                int queryable = report.Processes.Count(process => process.TokenQueryAccess);
                Beaprint.GrayPrint(string.Format(
                    "    Inspected {0} processes: {1} queryable tokens, {2} conditional duplication candidate(s), {3} process-query denials, {4} token-query denials.",
                    report.Enumerated, queryable, candidates, report.ProcessQueryDenied, report.TokenQueryDenied));
                if (displayed == 0)
                    Beaprint.GrayPrint("    No queryable privileged or service process tokens were observed.");
                if (omitted > 0)
                    Beaprint.GrayPrint("    " + omitted + " additional privileged/service token record(s) omitted from output.");
                if (report.ServiceInventoryUnavailable)
                    Beaprint.GrayPrint("    Running-service associations were unavailable; account classification and output may be incomplete.");
                if (report.ProcessLimitReached || report.TimeLimitReached)
                    Beaprint.GrayPrint("    Process-token inspection was partial (1024-process cap; 2.5-second inspection budget); remaining processes are unknown.");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintVulnLeakedHandlers()
        {
            try
            {
                Beaprint.MainPrint("Vulnerable Leaked Handlers", "T1134.001");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#leaked-handlers");

                List<Dictionary<string, string>> vulnHandlers = new List<Dictionary<string, string>>(); 

                Beaprint.InfoPrint("Getting Leaked Handlers, it might take some time...");
                using (var progress = new ProgressBar())
                {
                    vulnHandlers = ProcessesInfo.GetVulnHandlers(progress);
                }
                Dictionary<string, string> colors = new Dictionary<string, string>();
                colors[Checks.CurrentUserName] = Beaprint.ansi_color_bad;
                colors[HandlesHelper.elevatedProcess] = Beaprint.ansi_color_bad;

                foreach (Dictionary<string, string> handler in vulnHandlers)
                {
                    colors[handler["Reason"]] = Beaprint.ansi_color_bad;
                }
                Beaprint.DictPrint(vulnHandlers, colors, true);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }
    }
}
