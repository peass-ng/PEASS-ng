using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using winPEAS.Helpers;
using winPEAS.Helpers.AppLocker;
using winPEAS._3rdParty.Watson;
using winPEAS.Info.SystemInfo.Printers;
using winPEAS.Info.SystemInfo.NamedPipes;
using winPEAS.Info.SystemInfo;
using winPEAS.Info.SystemInfo.SysMon;
using winPEAS.Helpers.Extensions;
using winPEAS.Info.SystemInfo.WindowsDefender;

namespace winPEAS.Checks
{
    class SystemInfo : ISystemCheck
    {
        static string badUAC = "No prompting|PromptForNonWindowsBinaries";
        static string goodUAC = "PromptPermitDenyOnSecureDesktop";
        static string badLAPS = "LAPS not installed";


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

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("System Information");
            
            new List<Action>
            {
                PrintBasicSystemInfo,
                PrintUserEV,
                PrintSystemEV,
                PrintAuditInfo,
                PrintWEFInfo,
                PrintLAPSInfo,
                PrintWdigest,
                PrintLSAProtection,
                PrintCredentialGuard,
                PrintCachedCreds,
                PrintAVInfo,
                PrintWindowsDefenderInfo,
                PrintUACInfo,
                PrintPSInfo,
                PrintTranscriptPS,
                PrintInetInfo,
                PrintDrivesInfo,
                PrintWSUS,
                PrintAlwaysInstallElevated,
                PrintLsaCompatiblityLevel,
                AppLockerHelper.PrintAppLockerPolicy,
                PrintPrintersWMIInfo,
                PrintNamedPipes,
                PrintAMSIProviders,
                PrintSysmon
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        static void PrintBasicSystemInfo()
        {
            try
            {
                Beaprint.MainPrint("Basic System Information");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#kernel-exploits", "Check if the Windows versions is vulnerable to some known exploit");
                Dictionary<string, string> basicDictSystem = Info.SystemInfo.SystemInfo.GetBasicOSInfo();
                basicDictSystem["Hotfixes"] = Beaprint.ansi_color_good + basicDictSystem["Hotfixes"] + Beaprint.NOCOLOR;
                Dictionary<string, string> colorsSI = new Dictionary<string, string>
                {
                    { Globals.StrTrue, Beaprint.ansi_color_bad },
                };
                Beaprint.DictPrint(basicDictSystem, colorsSI, false);
                System.Console.WriteLine();
                Watson.FindVulns();

                //To update Watson, update the CVEs and add the new ones and update the main function so it uses new CVEs (becausfull with the Beaprints inside the FindVulns function)
                //Usually you won't need to do anything with the classes Wmi, Vulnerability and VulnerabilityCollection
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
                Beaprint.MainPrint("PowerShell Settings");
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
                Beaprint.MainPrint("PS default transcripts history");
                Beaprint.InfoPrint("Read the PS history inside these files (if any)");
                string drive = Path.GetPathRoot(Environment.SystemDirectory);
				string transcriptsPath = drive + @"transcripts\";
                string usersPath = $"{drive}users";

                var users = Directory.EnumerateDirectories(usersPath, "*", SearchOption.TopDirectoryOnly);
                string powershellTranscriptFilter = "powershell_transcript*";

                var colors = new Dictionary<string, string>()
                {
                    { "^.*", Beaprint.ansi_color_bad },
                };
                
                var results = new List<string>();

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

        static void PrintAuditInfo()
        {
            try
            {
                Beaprint.MainPrint("Audit Settings");
                Beaprint.LinkPrint("", "Check what is being logged");
                Dictionary<string, string> auditDict = Info.SystemInfo.SystemInfo.GetAuditSettings();
                Beaprint.DictPrint(auditDict, false);
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
                Beaprint.MainPrint("WEF Settings");
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
                Beaprint.MainPrint("LAPS Settings");
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
            Beaprint.MainPrint("Wdigest");
            Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/stealing-credentials/credentials-protections#wdigest", "If enabled, plain-text crds could be stored in LSASS");
            string useLogonCredential = RegistryHelper.GetRegValue("HKLM", @"SYSTEM\CurrentControlSet\Control\SecurityProviders\WDigest", "UseLogonCredential");
            if (useLogonCredential == "1")
                Beaprint.BadPrint("    Wdigest is active");
            else
                Beaprint.GoodPrint("    Wdigest is not enabled");
        }

        static void PrintLSAProtection()
        {
            Beaprint.MainPrint("LSA Protection");
            Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/stealing-credentials/credentials-protections#lsa-protection", "If enabled, a driver is needed to read LSASS memory (If Secure Boot or UEFI, RunAsPPL cannot be disabled by deleting the registry key)");
            string useLogonCredential = RegistryHelper.GetRegValue("HKLM", @"SYSTEM\CurrentControlSet\Control\LSA", "RunAsPPL");
            if (useLogonCredential == "1")
                Beaprint.GoodPrint("    LSA Protection is active");
            else
                Beaprint.BadPrint("    LSA Protection is not enabled");
        }

        static void PrintCredentialGuard()
        {
            Beaprint.MainPrint("Credentials Guard");
            Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/stealing-credentials/credentials-protections#credential-guard", "If enabled, a driver is needed to read LSASS memory");
            string lsaCfgFlags = RegistryHelper.GetRegValue("HKLM", @"System\CurrentControlSet\Control\LSA", "LsaCfgFlags");
            
            if (lsaCfgFlags == "1")
            {
                System.Console.WriteLine("    Please, note that this only checks the LsaCfgFlags key value. This is not enough to enable Credentials Guard (but it's a strong indicator).");
                Beaprint.GoodPrint("    CredentialGuard is active with UEFI lock");
            }
            else if (lsaCfgFlags == "2")
            {
                System.Console.WriteLine("    Please, note that this only checks the LsaCfgFlags key value. This is not enough to enable Credentials Guard (but it's a strong indicator).");
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
            Beaprint.MainPrint("Cached Creds");
            Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/stealing-credentials/credentials-protections#cached-credentials", "If > 0, credentials will be cached in the registry and accessible by SYSTEM user");
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

        static void PrintUserEV()
        {
            try
            {
                Beaprint.MainPrint("User Environment Variables");
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
                Beaprint.MainPrint("System Environment Variables");
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

                Beaprint.MainPrint("HKCU Internet Settings");
                Dictionary<string, string> HKCUDict = Info.SystemInfo.SystemInfo.GetInternetSettings("HKCU");
                Beaprint.DictPrint(HKCUDict, colorsSI, true);

                Beaprint.MainPrint("HKLM Internet Settings");
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
                Beaprint.MainPrint("Drives Information");
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
                Beaprint.MainPrint("AV Information");
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
                Beaprint.MainPrint("UAC Status");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#basic-uac-bypass-full-file-system-access", "If you are in the Administrators group check how to bypass the UAC");
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
                Beaprint.MainPrint("Checking WSUS");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#wsus");
                string path = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
                string path2 = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
                string HKLM_WSUS = RegistryHelper.GetRegValue("HKLM", path, "WUServer");
                string using_HKLM_WSUS = RegistryHelper.GetRegValue("HKLM", path, "UseWUServer");
                if (HKLM_WSUS.Contains("http://"))
                {
                    Beaprint.BadPrint("    WSUS is using http: " + HKLM_WSUS);
                    Beaprint.InfoPrint("You can test https://github.com/pimps/wsuxploit to escalate privileges");
                    if (using_HKLM_WSUS == "1")
                        Beaprint.BadPrint("    And UseWUServer is equals to 1, so it is vulnerable!");
                    else if (using_HKLM_WSUS == "0")
                        Beaprint.GoodPrint("    But UseWUServer is equals to 0, so it is not vulnerable!");
                    else
                        System.Console.WriteLine("    But UseWUServer is equals to " + using_HKLM_WSUS + ", so it may work or not");
                }
                else
                {
                    if (string.IsNullOrEmpty(HKLM_WSUS))
                        Beaprint.NotFoundPrint();
                    else
                        Beaprint.GoodPrint("    WSUS value: " + HKLM_WSUS);
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
                Beaprint.MainPrint("Checking AlwaysInstallElevated");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#alwaysinstallelevated");
                string path = "Software\\Policies\\Microsoft\\Windows\\Installer";
                string HKLM_AIE = RegistryHelper.GetRegValue("HKLM", path, "AlwaysInstallElevated");
                string HKCU_AIE = RegistryHelper.GetRegValue("HKCU", path, "AlwaysInstallElevated");
                
                if (HKLM_AIE == "1")
                { 
                    Beaprint.BadPrint("    AlwaysInstallElevated set to 1 in HKLM!");
                }

                if (HKCU_AIE == "1")
                {
                    Beaprint.BadPrint("    AlwaysInstallElevated set to 1 in HKCU!");
                }

                if (HKLM_AIE != "1" && HKCU_AIE != "1")
                {
                    Beaprint.GoodPrint("    AlwaysInstallElevated isn't available");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintLsaCompatiblityLevel()
        {
            string hive = "HKLM";
            string path = "SYSTEM\\CurrentControlSet\\Control\\Lsa\\";
            string key = "LmCompatibilityLevel";

            Beaprint.MainPrint($"Checking {hive}\\{path}{key}");

            try
            {
                string lmCompatibilityLevelValue = RegistryHelper.GetRegValue(hive, path, key);
                Dictionary<int, string> dict = new Dictionary<int, string>()
                {
                    { 0, "Send LM & NTLM responses" },
                    { 1, "Send LM & NTLM responses, use NTLMv2 session security if negotiated" },
                    { 2, "Send NTLM response only" },
                    { 3, "Send NTLMv2 response only" },
                    { 4, "Send NTLMv2 response only, refuse LM" },
                    { 5, "Send NTLMv2 response only, refuse LM & NTLM" },
                };

                if (!string.IsNullOrEmpty(lmCompatibilityLevelValue))
                {
                    if (int.TryParse(lmCompatibilityLevelValue, out int lmCompatibilityLevel))
                    {
                        string color = lmCompatibilityLevel == 5 ? Beaprint.ansi_color_good : Beaprint.ansi_color_bad;

                        if (dict.TryGetValue(lmCompatibilityLevel, out string description))
                        {
                            Beaprint.ColorPrint($"     value: {lmCompatibilityLevel}, description: {description}", color);
                        }
                        else
                        {
                            throw new Exception($"Unable to get value description for value '{lmCompatibilityLevel}'");
                        }
                    }
                    else
                    {
                        throw new Exception($"Unable to parse {key} value '{lmCompatibilityLevelValue}'");
                    }
                }
                else
                {
                    Beaprint.ColorPrint("     The registry key does not exist", Beaprint.ansi_color_yellow);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintPrintersWMIInfo()
        {
            Beaprint.MainPrint("Enumerating Printers (WMI)");

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

        private void PrintNamedPipes()
        {
            Beaprint.MainPrint("Enumerating Named Pipes");

            try
            {
                string formatString = "  {0,-100} {1}\n";

                Beaprint.NoColorPrint(string.Format($"{formatString}", "Name", "Sddl"));

                foreach (var namedPipe in NamedPipes.GetNamedPipeInfos())
                {
                    Beaprint.BadPrint(string.Format(formatString, namedPipe.Name, namedPipe.Sddl));
                }
            }
            catch (Exception ex) 
            {
                //Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintAMSIProviders()
        {
            Beaprint.MainPrint("Enumerating AMSI registered providers");

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
            Beaprint.MainPrint("Enumerating Sysmon configuration");

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
            Beaprint.MainPrint("Enumerating Sysmon process creation logs (1)");

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

        private static void PrintWindowsDefenderInfo()
        {
            Beaprint.MainPrint("Windows Defender configuration");



            void DisplayDefenderSettings(WindowsDefenderSettings settings)
            {
                var pathExclusions = settings.PathExclusions;
                var processExclusions = settings.ProcessExclusions;
                var extensionExclusions = settings.ExtensionExclusions;
                var asrSettings = settings.AsrSettings;

                if (pathExclusions.Count != 0)
                {
                    Beaprint.NoColorPrint("\n  Path Exclusions:");
                    foreach (var path in pathExclusions)
                    {
                        Beaprint.NoColorPrint($"    {path}");
                    }
                }

                if (pathExclusions.Count != 0)
                {
                    Beaprint.NoColorPrint("\n  PolicyManagerPathExclusions:");
                    foreach (var path in pathExclusions)
                    {
                        Beaprint.NoColorPrint($"    {path}");
                    }
                }

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
            }
            catch (Exception e)
            {
            }
        }
    }
}
