using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace winPEAS
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return false;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetBasicOSInfo()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                string ProductName = MyUtils.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "ProductName");
                string EditionID = MyUtils.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "EditionID");
                string ReleaseId = MyUtils.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "ReleaseId");
                string BuildBranch = MyUtils.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "BuildBranch");
                string CurrentMajorVersionNumber = MyUtils.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentMajorVersionNumber");
                string CurrentVersion = MyUtils.GetRegValue("HKLM", "Software\\Microsoft\\Windows NT\\CurrentVersion", "CurrentVersion");

                bool isHighIntegrity = MyUtils.IsHighIntegrity();

                CultureInfo ci = CultureInfo.InstalledUICulture;
                string systemLang = ci.Name;
                var timeZone = TimeZoneInfo.Local;
                InputLanguage myCurrentLanguage = InputLanguage.CurrentInputLanguage;

                string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                string userName = Environment.GetEnvironmentVariable("USERNAME");
                string ProcessorCount = Environment.ProcessorCount.ToString();
                bool isVM = IsVirtualMachine();

                DateTime now = DateTime.Now;

                String strHostName = Dns.GetHostName();
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                string dnsDomain = properties.DomainName;

                const string query = "SELECT HotFixID FROM Win32_QuickFixEngineering";
                var search = new ManagementObjectSearcher(query);
                var collection = search.Get();
                string hotfixes = "";
                foreach (ManagementObject quickFix in collection)
                    hotfixes += quickFix["HotFixID"].ToString() + ", ";

                results.Add("Hostname", strHostName);
                if (dnsDomain.Length > 1) results.Add("Domain Name", dnsDomain);
                results.Add("ProductName", ProductName);
                results.Add("EditionID", EditionID);
                results.Add("ReleaseId", ReleaseId);
                results.Add("BuildBranch", BuildBranch);
                results.Add("CurrentMajorVersionNumber", CurrentMajorVersionNumber);
                results.Add("CurrentVersion", CurrentVersion);
                results.Add("Architecture", arch);
                results.Add("ProcessorCount", ProcessorCount);
                results.Add("SystemLang", systemLang);
                results.Add("KeyboardLang", myCurrentLanguage.Culture.EnglishName);
                results.Add("TimeZone", timeZone.DisplayName);
                results.Add("IsVirtualMachine", isVM.ToString());
                results.Add("Current Time", now.ToString());
                results.Add("HighIntegrity", isHighIntegrity.ToString());
                results.Add("PartOfDomain", Program.partofdomain.ToString());
                results.Add("Hotfixes", hotfixes);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        //From https://stackoverflow.com/questions/1331887/detect-antivirus-on-windows-using-c-sharp
        public static Dictionary<string, string> GetAVInfo()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                ManagementObjectCollection data = wmiData.Get();

                foreach (ManagementObject virusChecker in data)
                {
                    results["Name"] = (string)virusChecker["displayName"];
                    results["ProductEXE"] = (string)virusChecker["pathToSignedProductExe"];
                    results["pathToSignedReportingExe"] = (string)virusChecker["pathToSignedReportingExe"];
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetUACSystemPolicies()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            try
            {
                string ConsentPromptBehaviorAdmin = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin");
                switch (ConsentPromptBehaviorAdmin)
                {
                    case "0":
                        results["ConsentPromptBehaviorAdmin"] = String.Format("{0} - No prompting", ConsentPromptBehaviorAdmin);
                        break;
                    case "1":
                        results["ConsentPromptBehaviorAdmin"] = String.Format("{0} - PromptOnSecureDesktop", ConsentPromptBehaviorAdmin);
                        break;
                    case "2":
                        results["ConsentPromptBehaviorAdmin"] = String.Format("{0} - PromptPermitDenyOnSecureDesktop", ConsentPromptBehaviorAdmin);
                        break;
                    case "3":
                        results["ConsentPromptBehaviorAdmin"] = String.Format("{0} - PromptForCredsNotOnSecureDesktop", ConsentPromptBehaviorAdmin);
                        break;
                    case "4":
                        results["ConsentPromptBehaviorAdmin"] = String.Format("{0} - PromptForPermitDenyNotOnSecureDesktop", ConsentPromptBehaviorAdmin);
                        break;
                    case "5":
                        results["ConsentPromptBehaviorAdmin"] = String.Format("{0} - PromptForNonWindowsBinaries", ConsentPromptBehaviorAdmin);
                        break;
                    default:
                        results["ConsentPromptBehaviorAdmin"] = String.Format("{0} - PromptForNonWindowsBinaries", ConsentPromptBehaviorAdmin);
                        break;
                }

                string EnableLUA = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "EnableLUA");
                results["EnableLUA"] = EnableLUA;

                string LocalAccountTokenFilterPolicy = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "LocalAccountTokenFilterPolicy");
                results["LocalAccountTokenFilterPolicy"] = LocalAccountTokenFilterPolicy;

                string FilterAdministratorToken = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "FilterAdministratorToken");
                results["FilterAdministratorToken"] = FilterAdministratorToken;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetPowerShellSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            try
            {
                results["PowerShell v2 Version"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\PowerShell\\1\\PowerShellEngine", "PowerShellVersion");
                results["PowerShell v5 Version"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\PowerShell\\3\\PowerShellEngine", "PowerShellVersion");
                results["Transcription Settings"] = "";
                results["Module Logging Settings"] = "";
                results["Scriptblock Logging Settings"] = "";

                Dictionary<string, object> transcriptionSettings = MyUtils.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\Transcription");
                if ((transcriptionSettings != null) && (transcriptionSettings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in transcriptionSettings)
                    {
                        results["Transcription Settings"] += String.Format("  {0,30} : {1}\r\n", kvp.Key, kvp.Value);
                    }
                }

                Dictionary<string, object> moduleLoggingSettings = MyUtils.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ModuleLogging");
                if ((moduleLoggingSettings != null) && (moduleLoggingSettings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in moduleLoggingSettings)
                    {
                        results["Module Logging Settings"] += String.Format("  {0,30} : {1}\r\n", kvp.Key, kvp.Value);
                    }
                }

                Dictionary<string, object> scriptBlockSettings = MyUtils.GetRegValues("HKLM", "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell\\ScriptBlockLogging");
                if ((scriptBlockSettings != null) && (scriptBlockSettings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in scriptBlockSettings)
                    {
                        results["Scriptblock Logging Settings"] = String.Format("  {0,30} : {1}\r\n", kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        // From seatbelt
        public static Dictionary<string, string> GetAuditSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                Dictionary<string, object> settings = MyUtils.GetRegValues("HKLM", "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\Audit");
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetWEFSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                Dictionary<string, object> settings = MyUtils.GetRegValues("HKLM", "Software\\Policies\\Microsoft\\Windows\\EventLog\\EventForwarding\\SubscriptionManager");
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetLapsSettings()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                string AdmPwdEnabled = MyUtils.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "AdmPwdEnabled");

                if (AdmPwdEnabled != "")
                {
                    results["LAPS Enabled"] = AdmPwdEnabled;
                    results["LAPS Admin Account Name"] = MyUtils.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "AdminAccountName");
                    results["LAPS Password Complexity"] = MyUtils.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordComplexity");
                    results["LAPS Password Length"] = MyUtils.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PasswordLength");
                    results["LAPS Expiration Protection Enabled"] = MyUtils.GetRegValue("HKLM", "Software\\Policies\\Microsoft Services\\AdmPwd", "PwdExpirationProtectionEnabled");
                }
                else
                {
                    results["LAPS Enabled"] = "LAPS not installed";
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return result;
        }

        //From Seatbelt
        public static Dictionary<string, string> GetSystemEnvVariables()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            try
            {
                Dictionary<string, object> settings = MyUtils.GetRegValues("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment");
                if ((settings != null) && (settings.Count != 0))
                {
                    foreach (KeyValuePair<string, object> kvp in settings)
                        result[kvp.Key] = (string)kvp.Value;
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
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
                Dictionary<string, object> proxySettings = MyUtils.GetRegValues(root_reg, "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }
    }
}
