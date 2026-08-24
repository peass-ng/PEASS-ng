using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using winPEAS.Helpers;
using winPEAS.Helpers.CredentialManager;
using winPEAS.Helpers.Registry;
using winPEAS.Info.WindowsCreds;
using winPEAS.Info.WindowsCreds.AppCmd;
using winPEAS.KnownFileCreds;
using winPEAS.KnownFileCreds.Kerberos;
using winPEAS.KnownFileCreds.SecurityPackages;
using winPEAS.KnownFileCreds.Vault;
using winPEAS.Wifi.NativeWifiApi;

namespace winPEAS.Checks
{
    internal class WindowsCreds : ISystemCheck
    {
        private const string CredentialLockerMitreId = "T1555.004";
        private const string CredentialLockerLink = "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#credentials-manager--windows-vault";
        private const string DpapiLink = "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#dpapi";
        private const string RdcManLink = "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#remote-desktop-credential-manager";

        public string[] MitreAttackIds { get; } = new[] { "T1552.001", "T1552.002", "T1555.003", "T1555.004", "T1558", "T1547.005", "T1563.002" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Windows Credentials", "T1552.001,T1552.002,T1555.003,T1555.004,T1558,T1547.005,T1563.002");

            new List<Action>
            {
                PrintVaultCreds,
                PrintCredentialManager,
                PrintUWPPasswordVault,
                PrintSavedRDPInfo,
                PrintRDPSettings,
                PrintRecentRunCommands,
                PrintDPAPIMasterKeys,
                PrintDpapiCredFiles,
                PrintRCManFiles,
                PrintKerberosTickets,
                //PrintKerberosTGTTickets, #Not working
                PrintWifi,
                PrintAppCmd,
                PrintSCClient,
                PrintSCCM,
                PrintSecurityPackagesCredentials,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private static void PrintVaultCreds()
        {
            try
            {
                Beaprint.MainPrint("Checking Windows Vault", CredentialLockerMitreId);
                Beaprint.LinkPrint(CredentialLockerLink);
                var vaultCreds = VaultCli.DumpVault();

                var colorsC = new Dictionary<string, string>()
                {
                    { "Identity.*|Credential.*|Resource.*", Beaprint.ansi_color_bad },
                };
                Beaprint.DictPrint(vaultCreds, colorsC, true, true);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintCredentialManager()
        {
            try
            {
                Beaprint.MainPrint("Checking Credential manager", CredentialLockerMitreId);
                Beaprint.LinkPrint(CredentialLockerLink);

                var colorsC = new Dictionary<string, string>()
                {
                    { "Warning:", Beaprint.YELLOW },
                };
                Beaprint.AnsiPrint("    [!] Warning: if password contains non-printable characters, it will be printed as unicode base64 encoded string\n\n", colorsC);

                var keywords = new HashSet<string>
                {
                    nameof(Credential.Password),
                    nameof(Credential.Username),
                    nameof(Credential.Target),
                    nameof(Credential.PersistenceType),
                    nameof(Credential.LastWriteTime),
                };

                colorsC = new Dictionary<string, string>()
                {
                    { CredentialManager.UnicodeInfoText, Beaprint.LBLUE }
                };

                foreach (var keyword in keywords)
                {
                    colorsC.Add($"{keyword}:", Beaprint.ansi_color_bad);
                }

                var credentials = CredentialManager.GetCredentials();

                foreach (var credential in credentials)
                {
                    Beaprint.AnsiPrint(credential, colorsC);
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintUWPPasswordVault()
        {
            try
            {
                Beaprint.MainPrint("Checking UWP PasswordVault / Credential Locker", CredentialLockerMitreId);
                Beaprint.LinkPrint("https://hacktricks.wiki");

                var colorsC = new Dictionary<string, string>()
        {
            { "Resource.*|UserName.*|Password.*", Beaprint.ansi_color_bad },
        };

                Type vaultType = Type.GetType("Windows.Security.Credentials.PasswordVault, Windows, ContentType=WindowsRuntime");
                if (vaultType == null)
                {
                    Beaprint.PrintException("Could not load WindowsRuntime PasswordVault type. Ensure you are running on Windows.");
                    return;
                }

                object vaultInstance = Activator.CreateInstance(vaultType);
                var retrieveAllMethod = vaultType.GetMethod("RetrieveAll");
                var credentialsList = retrieveAllMethod.Invoke(vaultInstance, null) as System.Collections.IEnumerable;

                if (credentialsList == null)
                {
                    Beaprint.AnsiPrint("    [-] No UWP credentials found in the locker.\n", colorsC);
                    return;
                }

                int count = 0;
                foreach (object credential in credentialsList)
                {
                    count++;
                    try
                    {
                        Type credType = credential.GetType();
                        var retrievePasswordMethod = credType.GetMethod("RetrievePassword");
                        retrievePasswordMethod.Invoke(credential, null);

                        string resource = credType.GetProperty("Resource")?.GetValue(credential)?.ToString() ?? "";
                        string userName = credType.GetProperty("UserName")?.GetValue(credential)?.ToString() ?? "";
                        string password = credType.GetProperty("Password")?.GetValue(credential)?.ToString() ?? "";

                        var credData = new Dictionary<string, string>
                {
                    { "Resource", resource },
                    { "UserName", userName },
                    { "Password", password }
                };

                        Beaprint.DictPrint(credData, colorsC, true, true);
                        Beaprint.PrintLineSeparator();
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                if (count == 0)
                {
                    Beaprint.AnsiPrint("    [-] No UWP credentials found in the locker.\n", colorsC);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static List<Dictionary<string, string>> PrintLinkedCredentialFiles(
            string title,
            string mitreId,
            string link,
            Func<List<Dictionary<string, string>>> getFiles,
            bool deleteNulls = false,
            string linkComment = "")
        {
            Beaprint.MainPrint(title, mitreId);
            Beaprint.LinkPrint(link, linkComment);

            var files = getFiles();
            Beaprint.DictPrint(files, deleteNulls);

            return files;
        }

        static void PrintSavedRDPInfo()
        {
            try
            {
                Beaprint.MainPrint("Saved RDP connections", "T1552.002");

                List<Dictionary<string, string>> rdps_info = RemoteDesktop.GetSavedRDPConnections();
                if (rdps_info.Count > 0)
                    Beaprint.NoColorPrint(string.Format("    {0,-20}{1,-55}{2}", "Host", "Username Hint", "User SID"));
                else
                {
                    Beaprint.NotFoundPrint();
                }

                foreach (Dictionary<string, string> rdp_info in rdps_info)
                {
                    Beaprint.NoColorPrint(string.Format("    {0,-20}{1,-55}{2}", rdp_info["Host"], rdp_info["Username Hint"], rdp_info["SID"]));
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintRecentRunCommands()
        {
            try
            {
                Beaprint.MainPrint("Recently run commands", "T1552.002");
                Dictionary<string, object> recentCommands = KnownFileCredsInfo.GetRecentRunCommands();
                Beaprint.DictPrint(recentCommands, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintDPAPIMasterKeys()
        {
            try
            {
                var masterKeys = PrintLinkedCredentialFiles(
                    "Checking for DPAPI Master Keys",
                    "T1555.003",
                    DpapiLink,
                    KnownFileCredsInfo.ListMasterKeys,
                    true);

                if (masterKeys.Count > 0)
                {
                    if (MyUtils.IsHighIntegrity())
                    {
                        Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the masterkey.");
                    }
                }
                else
                {
                    Beaprint.NotFoundPrint();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintDpapiCredFiles()
        {
            try
            {
                var credFiles = PrintLinkedCredentialFiles(
                    "Checking for DPAPI Credential Files",
                    "T1555.003",
                    DpapiLink,
                    KnownFileCredsInfo.GetCredFiles);

                if (credFiles.Count > 0)
                {
                    Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the creds file");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintRCManFiles()
        {
            try
            {
                var rdcFiles = PrintLinkedCredentialFiles(
                    "Checking for RDCMan Settings Files",
                    "T1552.001",
                    RdcManLink,
                    RemoteDesktop.GetRDCManFiles,
                    linkComment: "Dump credentials from Remote Desktop Connection Manager");

                if (rdcFiles.Count > 0)
                {
                    Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the .rdg file");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintKerberosTickets()
        {
            try
            {
                Beaprint.MainPrint("Looking for Kerberos tickets", "T1558");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/network-services-pentesting/pentesting-kerberos-88/index.html");
                var kerberosTickets = Kerberos.ListKerberosTickets();

                Beaprint.DictPrint(kerberosTickets, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintKerberosTGTTickets()
        {
            try
            {
                Beaprint.MainPrint("Looking for Kerberos TGT tickets", "T1558");
                var kerberosTgts = Kerberos.GetKerberosTGTData();
                Beaprint.DictPrint(kerberosTgts, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintWifi()
        {
            try
            {
                Beaprint.MainPrint("Looking for saved Wifi credentials", "T1552.001");

                WlanClient wlanClient = new WlanClient();

                foreach (var @interface in new WlanClient().Interfaces)
                {
                    foreach (var profile in @interface.GetProfiles())
                    {
                        var xml = @interface.GetProfileXml(profile.profileName);

                        XmlDocument xDoc = new XmlDocument();
                        xDoc.LoadXml(xml);

                        var keyMaterial = xDoc.GetElementsByTagName("keyMaterial");

                        if (keyMaterial.Count > 0)
                        {
                            string password = keyMaterial[0].InnerText;

                            Beaprint.BadPrint($"   SSID         :       '{profile.profileName}\n'" +
                                              $"   password     :       '{password}'  \n\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);

                // revert to old way
                Beaprint.NoColorPrint("Enumerating WLAN using wlanapi.dll failed, trying to enumerate using 'netsh'");

                Dictionary<string, string> networkConnections = Wifi.Wifi.Retrieve();
                Dictionary<string, string> ansi_colors_regexp = new Dictionary<string, string>();

                if (networkConnections.Count > 0)
                {
                    //Make sure the passwords are all flagged as ansi_color_bad.
                    foreach (var connection in networkConnections)
                    {
                        ansi_colors_regexp.Add(connection.Value, Beaprint.ansi_color_bad);
                    }
                    Beaprint.DictPrint(networkConnections, ansi_colors_regexp, false);
                }
                else
                {
                    Beaprint.NoColorPrint("No saved Wifi credentials found");
                }
            }
        }

        private static void PrintAppCmd()
        {
            try
            {
                Beaprint.MainPrint("Looking AppCmd.exe", "T1552.001");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#appcmdexe");

                var appCmdPath = Environment.ExpandEnvironmentVariables(@"%systemroot%\system32\inetsrv\appcmd.exe");

                if (File.Exists(appCmdPath))
                {
                    Beaprint.BadPrint($"    AppCmd.exe was found in {appCmdPath}");
                }
                else
                {
                    Beaprint.NotFoundPrint();
                }

                if (!MyUtils.IsHighIntegrity())
                {
                    Beaprint.NoColorPrint("      You must be an administrator to run this check");
                    return;
                }

                var script = AppCmd.GetExtractAppCmdCredsPowerShellScript();

                string args = @$" {script}";

                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = "powershell.exe",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            Beaprint.BadPrint($"    {process.StandardOutput.ReadLine()}");
                        }

                        while (!process.StandardError.EndOfStream)
                        {
                            Console.WriteLine(process.StandardError.ReadLine());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintSCClient()
        {
            try
            {
                Beaprint.MainPrint("Looking SSClient.exe", "T1552.001");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#scclient--sccm");

                if (File.Exists(Environment.ExpandEnvironmentVariables(@"%systemroot%\Windows\CCM\SCClient.exe")))
                {
                    Beaprint.BadPrint("    SCClient.exe was found in " + Environment.ExpandEnvironmentVariables(@"%systemroot%\Windows\CCM\SCClient.exe DLL Side loading?"));
                }
                else
                {
                    Beaprint.NotFoundPrint();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintSCCM()
        {
            try
            {
                Beaprint.MainPrint("Enumerating SSCM - System Center Configuration Manager settings", "T1552.001");
                Beaprint.LinkPrint(
                    "https://specterops.io/blog/2022/06/28/the-phantom-credentials-of-sccm-why-the-naa-wont-die/",
                    "Network Access Account policies place recoverable domain credential material on SCCM clients; prefer Enhanced HTTP and least privilege.");

                var server = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\CCMSetup", "LastValidMP");
                var siteCode = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\SMS\Mobile Client", "AssignedSiteCode");
                var productVersion = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\SMS\Mobile Client", "ProductVersion");
                var lastSuccessfulInstallParams = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\SMS\Mobile Client", "LastSuccessfulInstallParams");

                if (!string.IsNullOrEmpty(server) || !string.IsNullOrEmpty(siteCode) || !string.IsNullOrEmpty(productVersion) || !string.IsNullOrEmpty(lastSuccessfulInstallParams))
                {
                    Beaprint.NoColorPrint($"     Server:                            {server}\n" +
                                                 $"     Site code:                         {siteCode}\n" +
                                                 $"     Product version:                   {productVersion}\n" +
                                                 $"     Last Successful Install Params:    {lastSuccessfulInstallParams}\n");
                }

                SccmNetworkAccessAccountReport naaReport = SccmNetworkAccessAccount.GetReport();
                switch (naaReport.Status)
                {
                    case SccmNetworkAccessAccountStatus.Configured:
                        string count = naaReport.LimitReached
                            ? $"{naaReport.AccountCount} or more"
                            : naaReport.AccountCount.ToString();
                        Beaprint.BadPrint($"     SCCM Network Access Account credential policies cached in WMI: {count}");
                        Beaprint.GrayPrint("       Only non-secret instance metadata was queried; credential blobs were not read or displayed.");
                        Beaprint.BadPrint("       A local administrator or SYSTEM can recover these domain credentials. Remove or disable unused accounts and review their rights.");
                        break;
                    case SccmNetworkAccessAccountStatus.NotConfigured:
                        Beaprint.GoodPrint("     No active SCCM Network Access Account credential policy is exposed through WMI.");
                        break;
                    case SccmNetworkAccessAccountStatus.AccessDenied:
                        Beaprint.GrayPrint("     SCCM policy WMI access was denied; an elevated context is required to determine whether Network Access Account credentials are cached.");
                        break;
                    default:
                        Beaprint.GrayPrint("     SCCM Network Access Account policy could not be queried in the current environment.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintSecurityPackagesCredentials()
        {
            Beaprint.MainPrint("Enumerating Security Packages Credentials", "T1547.005");

            try
            {
                var credentials = (SecurityPackages.GetNtlmCredentials() ?? Enumerable.Empty<NtlmHashInfo>()).ToList();

                if (credentials.Any())
                {
                    foreach (var credential in credentials)
                    {
                        if (credential != null)
                        {
                            Beaprint.BadPrint($"  Version: {credential.Version}\n" +
                                              $"  Hash:    {credential.Hash}\n");
                            Beaprint.PrintLineSeparator();
                        }
                    }
                }
                else
                {
                    Beaprint.GoodPrint("  The NTLM security package does not contain any credentials.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintRDPSettings()
        {
            try
            {
                Beaprint.MainPrint("Remote Desktop Server/Client Settings", "T1563.002");

                var info = Info.WindowsCreds.RemoteDesktop.GetRDPSettingsInfo();

                var server = info.ServerSettings;
                Beaprint.ColorPrint("  RDP Server Settings", Beaprint.LBLUE);
                Beaprint.NoColorPrint($"    Network Level Authentication            :       {server.NetworkLevelAuthentication}\n" +
                                             $"    Block Clipboard Redirection             :       {server.BlockClipboardRedirection}\n" +
                                             $"    Block COM Port Redirection              :       {server.BlockComPortRedirection}\n" +
                                             $"    Block Drive Redirection                 :       {server.BlockDriveRedirection}\n" +
                                             $"    Block LPT Port Redirection              :       {server.BlockLptPortRedirection}\n" +
                                             $"    Block PnP Device Redirection            :       {server.BlockPnPDeviceRedirection}\n" +
                                             $"    Block Printer Redirection               :       {server.BlockPrinterRedirection}\n" +
                                             $"    Allow Smart Card Redirection            :       {server.AllowSmartCardRedirection}");

                Beaprint.ColorPrint("\n  RDP Client Settings", Beaprint.LBLUE);
                Beaprint.NoColorPrint($"    Disable Password Saving                 :       {info.ClientSettings.DisablePasswordSaving}\n" +
                                             $"    Restricted Remote Administration        :       {info.ClientSettings.RestrictedRemoteAdministration}");

                var type = info.ClientSettings.RestrictedRemoteAdministrationType;
                if (type != null)
                {
                    var str = GetDescriptionByType(type);

                    Beaprint.NoColorPrint($"  Restricted Remote Administration Type: {str}");
                }

                var level = info.ClientSettings.ServerAuthLevel;
                if (level != null)
                {
                    var str = GetDescriptionByType(level);

                    Beaprint.NoColorPrint($"  Server Authentication Level: {level} - {str}");
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static string GetDescriptionByType(uint? type)
        {
            var types = new Dictionary<uint, string>
                {
                    { 1, "Require Restricted Admin Mode" },
                    { 2, "Require Remote Credential Guard" },
                    { 3, "Require Restricted Admin or Remote Credential Guard" },
                };

            var str = $"{type} - Unknown";

            if (types.ContainsKey(type.Value))
            {
                str = types[type.Value];
            }

            return str;
        }
    }
}
