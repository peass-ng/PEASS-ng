using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;


namespace winPEAS.Info.SystemInfo
{
    class SystemInfo
    {
        // From Seatbelt
        public static bool IsVirtualMachine()
        {
            // returns true if the system is likely a virtual machine
            // Adapted from RobSiklos' code from https://stackoverflow.com/questions/498371/how-to-detect-if-my-application-is-running-in-a-virtual-machine/11145280#11145280
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    using (var items = searcher.Get())
                    {
                        foreach (var item in items)
                        {
                            string manufacturer = item["Manufacturer"].ToString().ToLower();
                            if ((manufacturer == "microsoft corporation" && item["Model"].ToString().ToUpperInvariant().Contains("VIRTUAL"))
                                || manufacturer.Contains("vmware")
                                || item["Model"].ToString() == "VirtualBox")
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return false;
        }
        
        //From Seatbelt
        public static Dictionary<string, string> GetBasicOSInfo()
        {
            Dictionary<string, string> results = new Dictionary<string, string>
            {
                { "Hotfixes", "unavailable" }
            };
            string output = ReadSystemInfoOutput();
            // Split the output by newline characters
            string[] lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            string osname = @".*?Microsoft[\(R\)]{0,3} Windows[\(R\)?]{0,3} ?(Serverr? )?(\d+\.?\d?( R2)?|XP|VistaT).*";
            string osversion = @".*?((\d+\.?){3}) ((Service Pack (\d)|N\/\w|.+) )?[ -\xa5]+ (\d+).*";
            // Iterate over each line and add key-value pairs to the dictionary
            foreach (string line in lines)
            {
                int index = line.IndexOf(':');
                if (index != -1)
                {
                    string key = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1).Trim();
                    if (Regex.IsMatch(value, osname, RegexOptions.IgnoreCase))
                    {
                        results["OS Name"] = value;
                    }

                    if (Regex.IsMatch(value, osversion, RegexOptions.IgnoreCase))
                    {
                        results["OS Version"] = value;
                    }

                    if (value.Contains("based PC")) 
                    {
                        results["System Type"] = value;
                    }
                    
                }
            }

            try
            {
                string ProductName = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "ProductName");
                string EditionID = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "EditionID");
                string ReleaseId = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "ReleaseId");
                string DisplayVersion = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "DisplayVersion");
                string BuildBranch = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "BuildBranch");
                string CurrentBuild = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuild");
                string UpdateBuildRevision = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "UBR");
                string CurrentMajorVersionNumber = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentMajorVersionNumber");
                string CurrentVersion = RegistryHelper.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentVersion");
                ProductName = NormalizeProductName(ProductName, CurrentBuild);

                if (!results.ContainsKey("OS Name") && !string.IsNullOrEmpty(ProductName))
                    results["OS Name"] = ProductName;
                if (!results.ContainsKey("OS Version") && !string.IsNullOrEmpty(CurrentBuild))
                    results["OS Version"] = CurrentVersion + " (Build " + CurrentBuild +
                        (string.IsNullOrEmpty(UpdateBuildRevision) ? "" : "." + UpdateBuildRevision) + ")";

                bool isHighIntegrity = MyUtils.IsHighIntegrity();

                CultureInfo ci = CultureInfo.InstalledUICulture;
                string systemLang = ci.Name;
                var timeZone = TimeZoneInfo.Local;
                InputLanguage myCurrentLanguage = InputLanguage.CurrentInputLanguage;

                string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432") ??
                    Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                if (!results.ContainsKey("System Type") && !string.IsNullOrEmpty(arch))
                    results["System Type"] = arch + "-based PC";
                string ProcessorCount = Environment.ProcessorCount.ToString();
                bool isVM;
                string vmError;
                bool vmKnown = CheckRunner.TryRunBounded(IsVirtualMachine, TimeSpan.FromSeconds(2),
                    out isVM, out vmError);

                DateTime now = DateTime.Now;

                String strHostName = Dns.GetHostName();
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                string dnsDomain = properties.DomainName;

                results["Hostname"] = strHostName;
                if (dnsDomain.Length > 1)
                    results["Domain Name"] = dnsDomain;
                results["ProductName"] = ProductName;
                results["EditionID"] = EditionID;
                results["ReleaseId"] = ReleaseId;
                results["DisplayVersion"] = DisplayVersion;
                results["BuildBranch"] = BuildBranch;
                results["CurrentBuild"] = CurrentBuild;
                results["UpdateBuildRevision"] = UpdateBuildRevision;
                results["CurrentMajorVersionNumber"] = CurrentMajorVersionNumber;
                results["CurrentVersion"] = CurrentVersion;
                results["Architecture"] = arch;
                results["ProcessorCount"] = ProcessorCount;
                results["SystemLang"] = systemLang;
                results["KeyboardLang"] = myCurrentLanguage.Culture.EnglishName;
                results["TimeZone"] = timeZone.DisplayName;
                results["IsVirtualMachine"] = vmKnown ? isVM.ToString() : "unknown";
                if (!vmKnown)
                    results["VM detection"] = "unavailable (" + vmError + ")";
                results["Current Time"] = now.ToString();
                results["HighIntegrity"] = isHighIntegrity.ToString();
                results["PartOfDomain"] = Checks.Checks.IsPartOfDomain.ToString();

                string hotfixes;
                string hotfixError;
                bool hotfixesKnown = CheckRunner.TryRunBounded(QueryHotfixes, TimeSpan.FromSeconds(3),
                    out hotfixes, out hotfixError);
                results["Hotfixes"] = hotfixesKnown ? hotfixes : "unavailable";
                if (!hotfixesKnown)
                    results["Hotfix collection"] = "unavailable (" + hotfixError + ")";
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        internal static string NormalizeProductName(string productName, string build)
        {
            if (int.TryParse(build, out int buildNumber) && buildNumber >= 22000 &&
                productName != null && productName.StartsWith("Windows 10 ", StringComparison.OrdinalIgnoreCase))
                return "Windows 11 " + productName.Substring("Windows 10 ".Length);
            return productName;
        }

        private static string QueryHotfixes()
        {
            const string query = "SELECT HotFixID,InstalledOn FROM Win32_QuickFixEngineering";
            using (var search = new ManagementObjectSearcher(query))
            {
                search.Options.Timeout = TimeSpan.FromSeconds(2);
                using (var collection = search.Get())
                {
                    var hotfixes = new List<string>();
                    foreach (ManagementObject quickFix in collection)
                    {
                        hotfixes.Add(quickFix["HotFixID"] + " (" + quickFix["InstalledOn"] + ")");
                    }
                    return string.Join(", ", hotfixes);
                }
            }
        }

        private static string ReadSystemInfoOutput()
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "systeminfo.exe";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    // Drain stdout while waiting so a full pipe cannot stall the child.
                    var output = process.StandardOutput.ReadToEndAsync();
                    if (!process.WaitForExit(8000))
                    {
                        try { process.Kill(); } catch (InvalidOperationException) { }
                        return "";
                    }
                    return output.Wait(1000) ? output.Result : "";
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException("systeminfo.exe: " + ex.Message);
                return "";
            }
        }

        public static List<Dictionary<string, string>> GetDrivesInfo()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>> { };
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            try
            {
                foreach (DriveInfo d in allDrives)
                {
                    Dictionary<string, string> res = new Dictionary<string, string>{
                    { "Name", "" },
                    { "Type", "" },
                    { "Volume label", "" },
                    { "Filesystem", "" },
                    { "Available space", ""}
                };

                    res["Name"] = d.Name;
                    res["Type"] = d.DriveType.ToString();
                    if (d.IsReady)
                    {
                        res["Volume label"] = d.VolumeLabel;
                        res["Filesystem"] = d.DriveFormat;
                        res["Available space"] = d.TotalFreeSpace.ToString();
                    }
                    results.Add(res);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        //From https://stackoverflow.com/questions/1331887/detect-antivirus-on-windows-using-c-sharp
        public static Dictionary<string, string> GetAVInfo()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            string whitelistpaths = "";

            try
            {
                var keys = RegistryHelper.GetRegValues("HKLM", @"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths");
                if (keys != null)
                    whitelistpaths = String.Join("\n    ", keys.Keys);

                using (ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
                {
                    using (var data = wmiData.Get())
                    {
                        foreach (ManagementObject virusChecker in data)
                        {
                            results["Name"] = (string)virusChecker["displayName"];
                            results["ProductEXE"] = (string)virusChecker["pathToSignedProductExe"];
                            results["pathToSignedReportingExe"] = (string)virusChecker["pathToSignedReportingExe"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            if (!string.IsNullOrEmpty(whitelistpaths))
            {
                results["whitelistpaths"] = "    " + whitelistpaths; //Add this info the last
            }

            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetUACSystemPolicies()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            try
            {
                string ConsentPromptBehaviorAdmin = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin");
                results["ConsentPromptBehaviorAdmin"] = ConsentPromptBehaviorAdmin switch
                {
                    "0" => $"{ConsentPromptBehaviorAdmin} - No prompting",
                    "1" => $"{ConsentPromptBehaviorAdmin} - PromptOnSecureDesktop",
                    "2" => $"{ConsentPromptBehaviorAdmin} - PromptPermitDenyOnSecureDesktop",
                    "3" => $"{ConsentPromptBehaviorAdmin} - PromptForCredsNotOnSecureDesktop",
                    "4" => $"{ConsentPromptBehaviorAdmin} - PromptForPermitDenyNotOnSecureDesktop",
                    "5" => $"{ConsentPromptBehaviorAdmin} - PromptForNonWindowsBinaries",
                    _ => $"{ConsentPromptBehaviorAdmin} - PromptForNonWindowsBinaries",
                };
                string EnableLUA = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "EnableLUA");
                results["EnableLUA"] = EnableLUA;

                string LocalAccountTokenFilterPolicy = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "LocalAccountTokenFilterPolicy");
                results["LocalAccountTokenFilterPolicy"] = LocalAccountTokenFilterPolicy;

                string FilterAdministratorToken = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "FilterAdministratorToken");
                results["FilterAdministratorToken"] = FilterAdministratorToken;
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetPowerShellSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            try
            {
                results["PowerShell v2 Version"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\PowerShell\\1\\PowerShellEngine", "PowerShellVersion");
                results["PowerShell v5 Version"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\PowerShell\\3\\PowerShellEngine", "PowerShellVersion");
                results["PowerShell Core Version"] = string.Join(", ", GetPowerShellCoreVersions());
                results["Transcription Settings"] = "";
                results["Module Logging Settings"] = "";
                results["Scriptblock Logging Settings"] = "";
                results["PS history file"] = "";
                results["PS history size"] = "";

                Dictionary<string, object> transcriptionSettingsCU = RegistryHelper.GetRegValues("HKCU",
                    "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\Transcription");
                if ((transcriptionSettingsCU == null) || (transcriptionSettingsCU.Count == 0))
                    transcriptionSettingsCU = RegistryHelper.GetRegValues("HKCU", @"HKLM\SOFTWARE\Wow6432Node\Policies\Microsoft\Windows\PowerShell\Transcription");

                if ((transcriptionSettingsCU != null) && (transcriptionSettingsCU.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in transcriptionSettingsCU)
                    {
                        results["Transcription Settings CU"] += string.Format("  {0,30} : {1}\r\n", kvp.Key, kvp.Value);
                    }
                }

                Dictionary<string, object> transcriptionSettingsLM = RegistryHelper.GetRegValues("HKLM",
                    "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\Transcription");

                if ((transcriptionSettingsLM == null) || (transcriptionSettingsLM.Count == 0))
                {
                    transcriptionSettingsLM = RegistryHelper.GetRegValues("HKLM", @"HKLM\SOFTWARE\Wow6432Node\Policies\Microsoft\Windows\PowerShell\Transcription");
                }

                if ((transcriptionSettingsLM != null) && (transcriptionSettingsLM.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in transcriptionSettingsLM)
                    {
                        results["Transcription Settings LM"] += $"  {kvp.Key,30} : {kvp.Value}\r\n";
                    }
                }

                Dictionary<string, object> moduleLoggingSettingsLM = RegistryHelper.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging");
                if ((moduleLoggingSettingsLM == null) || (moduleLoggingSettingsLM.Count == 0))
                    moduleLoggingSettingsLM = RegistryHelper.GetRegValues("HKLM", @"SOFTWARE\Wow6432Node\Policies\Microsoft\Windows\PowerShell\ModuleLogging");

                if ((moduleLoggingSettingsLM != null) && (moduleLoggingSettingsLM.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in moduleLoggingSettingsLM)
                    {
                        results["Module Logging Settings"] += $"  {kvp.Key,30} : {kvp.Value}\r\n";
                    }
                }

                Dictionary<string, object> moduleLoggingSettingsCU = RegistryHelper.GetRegValues("HKCU", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging");
                if ((moduleLoggingSettingsCU == null) || (moduleLoggingSettingsCU.Count == 0))
                    moduleLoggingSettingsCU = RegistryHelper.GetRegValues("HKCU", @"SOFTWARE\Wow6432Node\Policies\Microsoft\Windows\PowerShell\ModuleLogging");

                if ((moduleLoggingSettingsCU != null) && (moduleLoggingSettingsCU.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in moduleLoggingSettingsCU)
                    {
                        results["Module Logging Settings CU"] += $"  {kvp.Key,30} : {kvp.Value}\r\n";
                    }
                }

                Dictionary<string, object> scriptBlockSettingsLM = RegistryHelper.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging");
                if ((scriptBlockSettingsLM == null) || (scriptBlockSettingsLM.Count == 0))
                    scriptBlockSettingsLM = RegistryHelper.GetRegValues("HKLM", @"SOFTWARE\Wow6432Node\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging");

                if ((scriptBlockSettingsLM != null) && (scriptBlockSettingsLM.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in scriptBlockSettingsLM)
                    {
                        results["Scriptblock Logging Settings LM"] = $"  {kvp.Key,30} : {kvp.Value}\r\n";
                    }
                }

                Dictionary<string, object> scriptBlockSettingsCU = RegistryHelper.GetRegValues("HKCU", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging");
                if ((scriptBlockSettingsCU == null) || (scriptBlockSettingsCU.Count == 0))
                    scriptBlockSettingsCU = RegistryHelper.GetRegValues("HKCU", @"SOFTWARE\Wow6432Node\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging");

                if ((scriptBlockSettingsCU != null) && (scriptBlockSettingsCU.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in scriptBlockSettingsCU)
                    {
                        results["Scriptblock Logging Settings CU"] = $"  {kvp.Key,30} : {kvp.Value}\r\n";
                    }
                }

                string ps_history_path = Environment.ExpandEnvironmentVariables(@"%APPDATA%\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt");
                string ps_history_path2 =
                    $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\AppData\\Roaming\\Microsoft\\Windows\\PowerShell\\PSReadline\\ConsoleHost_history.txt";
                ps_history_path = File.Exists(ps_history_path) ? ps_history_path : ps_history_path2;
                if (File.Exists(ps_history_path))
                {
                    FileInfo fi = new FileInfo(ps_history_path);
                    long size = fi.Length;
                    results["PS history file"] = ps_history_path;
                    results["PS history size"] = size.ToString() + "B";
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        private static IEnumerable<string> GetPowerShellCoreVersions()
        {
            var keys = RegistryHelper.GetRegSubkeys("HKLM", @"SOFTWARE\Microsoft\PowerShellCore\InstalledVersions\") ?? new string[] { };

            return keys.Select(key =>
                RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\PowerShellCore\InstalledVersions\" + key, "SemanticVersion"))
                              .Where(version => version != null).ToList();
        }

        // From seatbelt
        public static Dictionary<string, string> GetAuditSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                Dictionary<string, object> settings = RegistryHelper.GetRegValues("HKLM", "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit");
                if ((settings != null) && (settings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in settings)
                    {
                        if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.String"))
                        {
                            string result = string.Join(",", (string[])kvp.Value);
                            results.Add(kvp.Key, result);
                        }
                        else
                        {
                            results.Add(kvp.Key, (string)kvp.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetWEFSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                Dictionary<string, object> settings = RegistryHelper.GetRegValues("HKLM", "Software\\Policies\\Microsoft\\Windows\\EventLog\\EventForwarding\\SubscriptionManager");
                if ((settings != null) && (settings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in settings)
                    {
                        if (kvp.Value.GetType().IsArray && (kvp.Value.GetType().GetElementType().ToString() == "System.String"))
                        {
                            string result = string.Join(",", (string[])kvp.Value);
                            results.Add(kvp.Key, result);
                        }
                        else
                        {
                            results.Add(kvp.Key, (string)kvp.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetLapsSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                string AdmPwdEnabled = RegistryHelper.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "AdmPwdEnabled");

                if (AdmPwdEnabled != "")
                {
                    results["LAPS Enabled"] = AdmPwdEnabled;
                    results["LAPS Admin Account Name"] = RegistryHelper.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "AdminAccountName");
                    results["LAPS Password Complexity"] = RegistryHelper.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordComplexity");
                    results["LAPS Password Length"] = RegistryHelper.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordLength");
                    results["LAPS Expiration Protection Enabled"] = RegistryHelper.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PwdExpirationProtectionEnabled");
                }
                else
                {
                    results["LAPS Enabled"] = "LAPS not installed";
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetUserEnvVariables()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
                    result[(string)env.Key] = (string)env.Value;
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return result;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetSystemEnvVariables()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                Dictionary<string, object> settings = RegistryHelper.GetRegValues("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment");
                if ((settings != null) && (settings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in settings)
                    {
                        result[kvp.Key] = (string)kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return result;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetInternetSettings(string root_reg)
        {
            // lists user/system internet settings, including default proxy info
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                Dictionary<string, object> proxySettings = RegistryHelper.GetRegValues(root_reg, "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");
                if ((proxySettings != null) && (proxySettings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in proxySettings)
                    {
                        results[kvp.Key] = kvp.Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
