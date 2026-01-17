using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
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
                PrintOemPrivilegedUtilities,
                PrintLegacySignedKernelDrivers,
                PrintKernelQuickIndicators,
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

        void PrintOemPrivilegedUtilities()
        {
            try
            {
                Beaprint.MainPrint("OEM privileged utilities & risky components");
                var findings = OemSoftwareHelper.GetPotentiallyVulnerableComponents(Checks.CurrentUserSiDs);

                if (findings.Count == 0)
                {
                    Beaprint.GoodPrint("    None of the supported OEM utilities were detected.");
                    return;
                }

                foreach (var finding in findings)
                {
                    bool hasCves = finding.Cves != null && finding.Cves.Length > 0;
                    string cveSuffix = hasCves ? $" ({string.Join(", ", finding.Cves)})" : string.Empty;
                    Beaprint.BadPrint($"  {finding.Name}{cveSuffix}");

                    if (!string.IsNullOrWhiteSpace(finding.Description))
                    {
                        Beaprint.GrayPrint($"      {finding.Description}");
                    }

                    foreach (var evidence in finding.Evidence)
                    {
                        string message = $"      - {evidence.Message}";
                        if (evidence.Highlight)
                        {
                            Beaprint.BadPrint(message);
                        }
                        else
                        {
                            Beaprint.GrayPrint(message);
                        }
                    }

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintLegacySignedKernelDrivers()
        {
            try
            {
                Beaprint.MainPrint("Kernel drivers with weak/legacy signatures");
                Beaprint.LinkPrint("https://research.checkpoint.com/2025/cracking-valleyrat-from-builder-secrets-to-kernel-rootkits/",
                    "Legacy cross-signed drivers (pre-July-2015) can still grant kernel execution on modern Windows");

                List<ServicesInfoHelper.KernelDriverInfo> drivers = ServicesInfoHelper.GetKernelDriverInfos();
                if (drivers.Count == 0)
                {
                    Beaprint.InfoPrint("  Unable to enumerate kernel services");
                    return;
                }

                var suspiciousDrivers = drivers.Where(d => d.Signature != null && (!d.Signature.IsSigned || d.Signature.IsLegacyExpired))
                                               .OrderBy(d => d.Name)
                                               .ToList();

                if (suspiciousDrivers.Count == 0)
                {
                    Beaprint.InfoPrint("  No unsigned or legacy-signed kernel drivers detected");
                    return;
                }

                foreach (var driver in suspiciousDrivers)
                {
                    var signature = driver.Signature ?? new ServicesInfoHelper.KernelDriverSignatureInfo();
                    List<string> reasons = new List<string>();

                    if (!signature.IsSigned)
                    {
                        reasons.Add("unsigned or signature missing");
                    }
                    else if (signature.IsLegacyExpired)
                    {
                        reasons.Add("signed with certificate that expired before 29-Jul-2015 (legacy exception)");
                    }

                    if (!string.IsNullOrEmpty(driver.StartMode) &&
                        (driver.StartMode.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                         driver.StartMode.Equals("Boot", StringComparison.OrdinalIgnoreCase)))
                    {
                        reasons.Add($"loads at early boot (Start={driver.StartMode})");
                    }

                    if (string.Equals(driver.Name, "kernelquick", StringComparison.OrdinalIgnoreCase))
                    {
                        reasons.Add("service name matches ValleyRAT rootkit loader");
                    }

                    string reason = reasons.Count > 0 ? string.Join("; ", reasons) : "Potentially risky driver";
                    string signatureLine = signature.IsSigned
                        ? $"Subject: {signature.Subject}; Issuer: {signature.Issuer}; Valid: {FormatDate(signature.NotBefore)} - {FormatDate(signature.NotAfter)}"
                        : $"Signature issue: {signature.Error ?? "Unsigned"}";

                    Beaprint.BadPrint($"  {driver.Name} ({driver.DisplayName})");
                    Beaprint.NoColorPrint($"      Path       : {driver.PathName}");
                    Beaprint.NoColorPrint($"      Start/State: {driver.StartMode}/{driver.State}");
                    Beaprint.NoColorPrint($"      Reason     : {reason}");
                    Beaprint.NoColorPrint($"      Signature  : {signatureLine}");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintKernelQuickIndicators()
        {
            try
            {
                Beaprint.MainPrint("KernelQuick / ValleyRAT rootkit indicators");

                bool found = false;

                Dictionary<string, object> serviceValues = RegistryHelper.GetRegValues("HKLM", @"SYSTEM\\CurrentControlSet\\Services\\kernelquick");
                if (serviceValues != null)
                {
                    found = true;
                    string imagePath = serviceValues.ContainsKey("ImagePath") ? serviceValues["ImagePath"].ToString() : "Unknown";
                    string start = serviceValues.ContainsKey("Start") ? serviceValues["Start"].ToString() : "Unknown";
                    Beaprint.BadPrint("  Service HKLM\\SYSTEM\\CurrentControlSet\\Services\\kernelquick present");
                    Beaprint.NoColorPrint($"      ImagePath : {imagePath}");
                    Beaprint.NoColorPrint($"      Start     : {start}");
                }

                foreach (var path in new[] { @"SOFTWARE\\KernelQuick", @"SOFTWARE\\WOW6432Node\\KernelQuick", @"SYSTEM\\CurrentControlSet\\Services\\kernelquick" })
                {
                    Dictionary<string, object> values = RegistryHelper.GetRegValues("HKLM", path);
                    if (values == null)
                        continue;

                    var kernelQuickValues = values.Where(k => k.Key.StartsWith("KernelQuick_", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (kernelQuickValues.Count == 0)
                        continue;

                    found = true;
                    Beaprint.BadPrint($"  Registry values under HKLM\\{path}");
                    foreach (var kv in kernelQuickValues)
                    {
                        string displayValue = kv.Value is byte[] bytes ? $"(binary) {bytes.Length} bytes" : string.Format("{0}", kv.Value);
                        Beaprint.NoColorPrint($"      {kv.Key} = {displayValue}");
                    }
                }

                Dictionary<string, object> ipdatesValues = RegistryHelper.GetRegValues("HKLM", @"SOFTWARE\\IpDates");
                if (ipdatesValues != null)
                {
                    found = true;
                    Beaprint.BadPrint("  Possible kernel shellcode staging key HKLM\\SOFTWARE\\IpDates");
                    foreach (var kv in ipdatesValues)
                    {
                        string displayValue = kv.Value is byte[] bytes ? $"(binary) {bytes.Length} bytes" : string.Format("{0}", kv.Value);
                        Beaprint.NoColorPrint($"      {kv.Key} = {displayValue}");
                    }
                }

                if (!found)
                {
                    Beaprint.InfoPrint("  No KernelQuick-specific registry indicators were found");
                }
                else
                {
                    Beaprint.LinkPrint("https://research.checkpoint.com/2025/cracking-valleyrat-from-builder-secrets-to-kernel-rootkits/",
                        "KernelQuick_* values and HKLM\\SOFTWARE\\IpDates are used by the ValleyRAT rootkit to hide files and stage APC payloads");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private string FormatDate(DateTime? dateTime)
        {
            return dateTime.HasValue ? dateTime.Value.ToString("yyyy-MM-dd HH:mm") : "n/a";
        }
    }
}
