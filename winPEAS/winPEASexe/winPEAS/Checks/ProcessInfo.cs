using System;
using System.Collections.Generic;
using System.IO;
using winPEAS.Helpers;
using winPEAS.Info.ProcessInfo;

namespace winPEAS.Checks
{
    internal class ProcessInfo : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Processes Information");           

            new List<Action>
            {
                PrintInterestingProcesses,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        void PrintInterestingProcesses()
        {
            try
            {
                Beaprint.MainPrint("Interesting Processes -non Microsoft-");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#running-processes", "Check if any interesting processes for memory dump or if you could overwrite some binary running");
                List<Dictionary<string, string>> processesInfo = ProcessesInfo.GetProcInfo();

                foreach (Dictionary<string, string> procInfo in processesInfo)
                {
                    Dictionary<string, string> colorsP = new Dictionary<string, string>()
                        {
                            { " " + Checks.CurrentUserName, Beaprint.ansi_current_user },
                            { "Permissions:.*", Beaprint.ansi_color_bad },
                            { "Possible DLL Hijacking.*", Beaprint.ansi_color_bad },
                        };

                    if (DefensiveProcesses.Definitions.ContainsKey(procInfo["Name"]))
                    {
                        if (!string.IsNullOrEmpty(DefensiveProcesses.Definitions[procInfo["Name"]]))
                        {
                            procInfo["Product"] = DefensiveProcesses.Definitions[procInfo["Name"]];
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
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(ex.Message);
            }
        }
    }
}
