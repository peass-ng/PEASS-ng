using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using winPEAS.Helpers;
using winPEAS.Helpers.CredentialManager;
using winPEAS.KnownFileCreds;
using winPEAS.KnownFileCreds.Kerberos;
using winPEAS.KnownFileCreds.Vault;
using winPEAS.Wifi.NativeWifiApi;

namespace winPEAS.Checks
{
    internal class WindowsCreds : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Windows Credentials");
            
            new List<Action>
            {
                PrintvaultCreds,
                PrintCredManag,
                PrintSavedRDPInfo,
                PrintRecentRunCommands,
                PrintDPAPIMasterKeys,
                PrintDpapiCredFiles,
                PrintRCManFiles,
                PrintKerberosTickets,
                //PrintKerberosTGTTickets, #Not working
                PrintWifi,
                PrintAppCmd,
                PrintSCClient,
                PrintSCCM
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        static void PrintvaultCreds()
        {
            try
            {
                Beaprint.MainPrint("Checking Windows Vault");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-manager-windows-vault");
                List<Dictionary<string, string>> vault_creds = VaultCli.DumpVault();

                Dictionary<string, string> colorsC = new Dictionary<string, string>()
                {
                    { "Identity.*|Credential.*|Resource.*", Beaprint.ansi_color_bad },
                };
                Beaprint.DictPrint(vault_creds, colorsC, true, true);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintCredManag()
        {
            try
            {
                Beaprint.MainPrint("Checking Credential manager");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-manager-windows-vault");
                if (winPEAS.Checks.Checks.ExecCmd)
                {
                    Dictionary<string, string> colorsC = new Dictionary<string, string>()
                    {
                        { "User:.*", Beaprint.ansi_color_bad },
                    };
                    Beaprint.AnsiPrint(MyUtils.ExecCMD("/list", "cmdkey.exe"), colorsC);
                    Beaprint.InfoPrint("If any cred was found, you can use it with 'runas /savecred'");
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintSavedRDPInfo()
        {
            try
            {
                Beaprint.MainPrint("Saved RDP connections");

                List<Dictionary<string, string>> rdps_info = RemoteDesktop.GetSavedRDPConnections();
                if (rdps_info.Count > 0)
                    System.Console.WriteLine(string.Format("    {0,-20}{1,-55}{2}", "Host", "Username Hint", "User SID"));
                else
                {
                    Beaprint.NotFoundPrint();
                }

                foreach (Dictionary<string, string> rdp_info in rdps_info)
                {
                    System.Console.WriteLine(string.Format("    {0,-20}{1,-55}{2}", rdp_info["Host"], rdp_info["Username Hint"], rdp_info["SID"]));
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintRecentRunCommands()
        {
            try
            {
                Beaprint.MainPrint("Recently run commands");
                Dictionary<string, object> recentCommands = KnownFileCredsInfo.GetRecentRunCommands();
                Beaprint.DictPrint(recentCommands, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintDPAPIMasterKeys()
        {
            try
            {
                Beaprint.MainPrint("Checking for DPAPI Master Keys");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#dpapi");
                List<Dictionary<string, string>> master_keys = KnownFileCredsInfo.ListMasterKeys();
                if (master_keys.Count != 0)
                {
                    Beaprint.DictPrint(master_keys, true);

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

        static void PrintDpapiCredFiles()
        {
            try
            {
                Beaprint.MainPrint("Checking for DPAPI Credential Files");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#dpapi");
                List<Dictionary<string, string>> cred_files = KnownFileCredsInfo.GetCredFiles();
                Beaprint.DictPrint(cred_files, false);
                if (cred_files.Count != 0)
                {
                    Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the creds file");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintRCManFiles()
        {
            try
            {
                Beaprint.MainPrint("Checking for RDCMan Settings Files");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#remote-desktop-credential-manager", "Dump credentials from Remote Desktop Connection Manager");
                List<Dictionary<string, string>> rdc_files = RemoteDesktop.GetRDCManFiles();
                Beaprint.DictPrint(rdc_files, false);
                if (rdc_files.Count != 0)
                {
                    Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the .rdg file");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintKerberosTickets()
        {
            try
            {
                Beaprint.MainPrint("Looking for kerberos tickets");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/pentesting/pentesting-kerberos-88");
                List<Dictionary<string, string>> kerberos_tckts = Kerberos.ListKerberosTickets();
                Beaprint.DictPrint(kerberos_tckts, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintKerberosTGTTickets()
        {
            try
            {
                Beaprint.MainPrint("Looking for kerberos TGT tickets");
                List<Dictionary<string, string>> kerberos_tgts = Kerberos.GetKerberosTGTData();
                Beaprint.DictPrint(kerberos_tgts, false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintWifi()
        {
            try
            {
                Beaprint.MainPrint("Looking for saved Wifi credentials");
                if (winPEAS.Checks.Checks.ExecCmd)
                {
                    Dictionary<string, string> networkConnections = Wifi.Wifi.Retrieve();
                    Dictionary<string, string> ansi_colors_regexp = new Dictionary<string, string>();

                    //Make sure the passwords are all flagged as ansi_color_bad.
                    foreach (var connection in networkConnections)
                    {
                        ansi_colors_regexp.Add(connection.Value, Beaprint.ansi_color_bad);
                    }
                    Beaprint.DictPrint(networkConnections, ansi_colors_regexp, false);
                }
                else
                {
                    foreach (var iface in new WlanClient().Interfaces)
                    {
                        foreach (var profile in iface.GetProfiles())
                        {
                            var xml = iface.GetProfileXml(profile.profileName);

                            XmlDocument xDoc = new XmlDocument();
                            xDoc.LoadXml(xml);

                            var keyMaterial = xDoc.GetElementsByTagName("keyMaterial");
                            if (keyMaterial.Count > 0)
                            {
                                string password = keyMaterial[0].InnerText;

                                Beaprint.BadPrint($"  found Wifi password for SSID: '{profile.profileName}', password: '{password}'  ");
                            }
                        }
                    }
                    //Beaprint.GrayPrint("    This function is not yet implemented.");
                    //Beaprint.InfoPrint("If you want to list saved Wifis connections you can list the using 'netsh wlan show profile'");
                    //Beaprint.InfoPrint("If you want to get the clear-text password use 'netsh wlan show profile <SSID> key=clear'");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        static void PrintAppCmd()
        {
            try
            {
                Beaprint.MainPrint("Looking AppCmd.exe");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#appcmd-exe");
                if (File.Exists(Environment.ExpandEnvironmentVariables(@"%systemroot%\system32\inetsrv\appcmd.exe")))
                {
                    Beaprint.BadPrint("    AppCmd.exe was found in " + Environment.ExpandEnvironmentVariables(@"%systemroot%\system32\inetsrv\appcmd.exe You should try to search for credentials"));
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

        static void PrintSCClient()
        {
            try
            {
                Beaprint.MainPrint("Looking SSClient.exe");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#scclient-sccm");
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
                Beaprint.MainPrint("Enumerating SSCM - System Center Configuration Manager settings");

                var server = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\CCMSetup", "LastValidMP");
                var siteCode = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\SMS\Mobile Client", "AssignedSiteCode");
                var productVersion = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\SMS\Mobile Client", "ProductVersion");
                var lastSuccessfulInstallParams = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\SMS\Mobile Client", "LastSuccessfulInstallParams");

                if (!string.IsNullOrEmpty(server) || !string.IsNullOrEmpty(siteCode) || !string.IsNullOrEmpty(productVersion) || !string.IsNullOrEmpty(lastSuccessfulInstallParams))
                {
                    Beaprint.NoColorPrint($"     Server:                            {server}\n" +
                                          $"     Site code:                         {siteCode}" +
                                          $"     Product version:                   {productVersion}" +
                                          $"     Last Successful Install Params:    {lastSuccessfulInstallParams}");
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
