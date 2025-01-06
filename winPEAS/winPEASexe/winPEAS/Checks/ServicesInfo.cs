using System;
using System.Collections.Generic;
using System.IO;
using winPEAS.Helpers;
using winPEAS.Info.ServicesInfo;

namespace winPEAS.Checks
{
    internal class ServicesInfo : ISystemCheck
    {
        Dictionary<string, string> modifiableServices = new Dictionary<string, string>();

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Services Information");

            /// Start finding Modifiable services so any function could use them

            try
            {
                CheckRunner.Run(() =>
                {
                    modifiableServices = ServicesInfoHelper.GetModifiableServices(Checks.CurrentUserSiDs);
                }, isDebug);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            new List<Action>
            {
                PrintInterestingServices,
                PrintModifiableServices,
                PrintWritableRegServices,
                PrintPathDllHijacking,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        void PrintInterestingServices()
        {
            try
            {
                Beaprint.MainPrint("Interesting Services -non Microsoft-");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#services", "Check if you can overwrite some service binary or perform a DLL hijacking, also check for unquoted paths");

                List<Dictionary<string, string>> services_info = ServicesInfoHelper.GetNonstandardServices();

                if (services_info.Count < 1)
                {
                    services_info = ServicesInfoHelper.GetNonstandardServicesFromReg();
                }

                foreach (Dictionary<string, string> serviceInfo in services_info)
                {
                    List<string> fileRights = PermissionsHelper.GetPermissionsFile(serviceInfo["FilteredPath"], Checks.CurrentUserSiDs);
                    List<string> dirRights = new List<string>();

                    if (serviceInfo["FilteredPath"] != null && serviceInfo["FilteredPath"] != "")
                    {
                        dirRights = PermissionsHelper.GetPermissionsFolder(Path.GetDirectoryName(serviceInfo["FilteredPath"]), Checks.CurrentUserSiDs);
                    }

                    bool noQuotesAndSpace = MyUtils.CheckQuoteAndSpace(serviceInfo["PathName"]);

                    string formString = "    {0}(";
                    if (serviceInfo["CompanyName"] != null && serviceInfo["CompanyName"].Length > 1)
                        formString += "{1} - ";
                    if (serviceInfo["DisplayName"].Length > 1)
                        formString += "{2}";
                    formString += ")";
                    if (serviceInfo["PathName"].Length > 1)
                        formString += "[{3}]";
                    if (serviceInfo["StartMode"].Length > 1)
                        formString += " - {4}";
                    if (serviceInfo["State"].Length > 1)
                        formString += " - {5}";
                    if (serviceInfo["isDotNet"].Length > 1)
                        formString += " - {6}";
                    if (noQuotesAndSpace)
                        formString += " - {7}";
                    if (modifiableServices.ContainsKey(serviceInfo["Name"]))
                    {
                        if (modifiableServices[serviceInfo["Name"]] == "Start")
                            formString += "\n    You can START this service";
                        else
                            formString += "\n    YOU CAN MODIFY THIS SERVICE: " + modifiableServices[serviceInfo["Name"]];
                    }
                    if (fileRights.Count > 0)
                        formString += "\n    File Permissions: {8}";
                    if (dirRights.Count > 0)
                        formString += "\n    Possible DLL Hijacking in binary folder: {9} ({10})";
                    if (serviceInfo["Description"].Length > 1)
                        formString += "\n    " + Beaprint.ansi_color_gray + "{11}";

                    {
                        Dictionary<string, string> colorsS = new Dictionary<string, string>()
                            {
                                { "File Permissions:.*", Beaprint.ansi_color_bad },
                                { "Possible DLL Hijacking.*", Beaprint.ansi_color_bad },
                                { "No quotes and Space detected", Beaprint.ansi_color_bad },
                                { "YOU CAN MODIFY THIS SERVICE:.*", Beaprint.ansi_color_bad },
                                { " START ", Beaprint.ansi_color_bad },
                                { serviceInfo["PathName"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (fileRights.Count > 0 || dirRights.Count > 0 || noQuotesAndSpace) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            };

                        Beaprint.AnsiPrint(string.Format(formString, serviceInfo["Name"], serviceInfo["CompanyName"], serviceInfo["DisplayName"], serviceInfo["PathName"], serviceInfo["StartMode"], serviceInfo["State"], serviceInfo["isDotNet"], "No quotes and Space detected", string.Join(", ", fileRights), dirRights.Count > 0 ? Path.GetDirectoryName(serviceInfo["FilteredPath"]) : "", string.Join(", ", dirRights), serviceInfo["Description"]), colorsS);
                    }

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintModifiableServices()
        {
            try
            {
                Beaprint.MainPrint("Modifiable Services");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#services", "Check if you can modify any service");
                if (modifiableServices.Count > 0)
                {
                    Beaprint.BadPrint("    LOOKS LIKE YOU CAN MODIFY OR START/STOP SOME SERVICE/s:");
                    Dictionary<string, string> colorsMS = new Dictionary<string, string>()
                        {
                            // modify
                            { "AllAccess", Beaprint.ansi_color_bad },
                            { "ChangeConfig", Beaprint.ansi_color_bad },
                            { "WriteDac", Beaprint.ansi_color_bad },
                            { "WriteOwner", Beaprint.ansi_color_bad },
                            { "AccessSystemSecurity", Beaprint.ansi_color_bad },
                            { "GenericAll", Beaprint.ansi_color_bad },
                            { "GenericWrite (ChangeConfig)", Beaprint.ansi_color_bad },

                            // start/stop
                            { "GenericExecute (Start/Stop)", Beaprint.ansi_color_yellow },
                            { "Start", Beaprint.ansi_color_yellow },
                            { "Stop", Beaprint.ansi_color_yellow },
                        };
                    Beaprint.DictPrint(modifiableServices, colorsMS, false, true);
                }
                else
                    Beaprint.GoodPrint("    You cannot modify any service");

            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintWritableRegServices()
        {
            try
            {
                Beaprint.MainPrint("Looking if you can modify any service registry");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#services-registry-modify-permissions", "Check if you can modify the registry of a service");
                List<Dictionary<string, string>> regPerms = ServicesInfoHelper.GetWriteServiceRegs(Checks.CurrentUserSiDs);

                Dictionary<string, string> colorsWR = new Dictionary<string, string>()
                            {
                                { @"\(.*\)", Beaprint.ansi_color_bad },
                            };

                if (regPerms.Count <= 0)
                    Beaprint.GoodPrint("    [-] Looks like you cannot change the registry of any service...");
                else
                {
                    foreach (Dictionary<string, string> writeServReg in regPerms)
                        Beaprint.AnsiPrint(string.Format("    {0} ({1})", writeServReg["Path"], writeServReg["Permissions"]), colorsWR);

                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintPathDllHijacking()
        {
            try
            {
                Beaprint.MainPrint("Checking write permissions in PATH folders (DLL Hijacking)");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#dll-hijacking", "Check for DLL Hijacking in PATH folders");
                Dictionary<string, string> path_dllhijacking = ServicesInfoHelper.GetPathDLLHijacking();
                foreach (KeyValuePair<string, string> entry in path_dllhijacking)
                {
                    if (string.IsNullOrEmpty(entry.Value))
                    {
                        Beaprint.GoodPrint("    " + entry.Key);
                    }
                    else
                    {
                        Beaprint.BadPrint("    (DLL Hijacking) " + entry.Key + ": " + entry.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

    }
}
