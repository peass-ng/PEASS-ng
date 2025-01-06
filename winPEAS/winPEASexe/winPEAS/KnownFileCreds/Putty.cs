using Microsoft.Win32;
using System;
using System.Collections.Generic;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.KnownFileCreds
{
    static class Putty
    {
        public static void PrintInfo()
        {
            PrintPuttySess();
            PrintPuttySSH();
            PrintSSHKeysReg();
        }

        private static void PrintPuttySess()
        {
            try
            {
                Beaprint.MainPrint("Putty Sessions");
                List<Dictionary<string, string>> putty_sess = GetPuttySessions();

                Dictionary<string, string> colorF = new Dictionary<string, string>()
                        {
                            { "ProxyPassword.*|PublicKeyFile.*|HostName.*|PortForwardings.*", Beaprint.ansi_color_bad },
                        };
                Beaprint.DictPrint(putty_sess, colorF, true, true);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("{0}", ex));
            }
        }

        private static void PrintPuttySSH()
        {
            try
            {
                Beaprint.MainPrint("Putty SSH Host keys");
                List<Dictionary<string, string>> putty_sess = ListPuttySSHHostKeys();
                Dictionary<string, string> colorF = new Dictionary<string, string>()
                        {
                            { ".*", Beaprint.ansi_color_bad },
                        };
                Beaprint.DictPrint(putty_sess, colorF, false, true);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("{0}", ex));
            }
        }

        private static void PrintSSHKeysReg()
        {
            try
            {
                Beaprint.MainPrint("SSH keys in registry");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#ssh-keys-in-registry", "If you find anything here, follow the link to learn how to decrypt the SSH keys");

                string[] ssh_reg = RegistryHelper.GetRegSubkeys("HKCU", @"OpenSSH\Agent\Keys");
                if (ssh_reg.Length == 0)
                    Beaprint.NotFoundPrint();
                else
                {
                    foreach (string ssh_key_entry in ssh_reg)
                        Beaprint.BadPrint(ssh_key_entry);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("{0}", ex));
            }
        }

        private static List<Dictionary<string, string>> GetPuttySessions()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // extracts saved putty sessions and basic configs (via the registry)
            if (MyUtils.IsHighIntegrity())
            {
                Console.WriteLine("\r\n\r\n=== Putty Saved Session Information (All Users) ===\r\n");

                string[] SIDs = Registry.Users.GetSubKeyNames();
                foreach (string SID in SIDs)
                {
                    if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                    {
                        string[] subKeys = RegistryHelper.GetRegSubkeys("HKU", string.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\", SID));

                        foreach (string sessionName in subKeys)
                        {
                            Dictionary<string, string> putty_sess = new Dictionary<string, string>()
                            {
                                { "User SID", SID },
                                { "SessionName", sessionName },
                                { "HostName", "" },
                                { "PortNumber", ""},
                                { "UserName", "" },
                                { "PublicKeyFile", "" },
                                { "PortForwardings", "" },
                                { "ConnectionSharing", "" },
                                { "ProxyPassword", "" },
                                { "ProxyUsername", "" },
                            };

                            string[] keys =
                            {
                                "HostName",
                                "PortNumber",
                                "UserName",
                                "PublicKeyFile",
                                "PortForwardings",
                                "ConnectionSharing",
                                "AgentFwd",
                                "ProxyPassword",
                                "ProxyUsername",
                            };

                            foreach (string key in keys)
                                putty_sess[key] = RegistryHelper.GetRegValue("HKU", string.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\{1}", SID, sessionName), key);

                            results.Add(putty_sess);
                        }
                    }
                }
            }
            else
            {
                string[] subKeys = RegistryHelper.GetRegSubkeys("HKCU", "Software\\SimonTatham\\PuTTY\\Sessions\\");
                RegistryKey selfKey = Registry.CurrentUser.OpenSubKey(@"Software\\SimonTatham\\PuTTY\\Sessions"); // extract own Sessions registry keys           

                if (selfKey != null)
                {
                    string[] subKeyNames = selfKey.GetValueNames();
                    foreach (string name in subKeyNames)
                    {
                        Dictionary<string, string> putty_sess_key = new Dictionary<string, string>()
                        {
                            { "RegKey Name", name },
                            { "RegKey Value", (string)selfKey.GetValue(name) },
                        };

                        results.Add(putty_sess_key);
                    }
                    selfKey.Close();
                }
                
                foreach (string sessionName in subKeys)
                {
                    Dictionary<string, string> putty_sess = new Dictionary<string, string>()
                    {
                        { "SessionName", sessionName },
                        { "HostName", "" },
                        { "PortNumber", "" },
                        { "UserName", "" },
                        { "PublicKeyFile", "" },
                        { "PortForwardings", "" },
                        { "ConnectionSharing", "" },
                        { "ProxyPassword", "" },
                        { "ProxyUsername", "" },
                    };

                    string[] keys =
                    {
                        "HostName",
                        "PortNumber",
                        "UserName",
                        "PublicKeyFile",
                        "PortForwardings",
                        "ConnectionSharing",
                        "AgentFwd",
                        "ProxyPassword",
                        "ProxyUsername",
                    };

                    foreach (string key in keys)
                        putty_sess[key] = RegistryHelper.GetRegValue("HKCU", string.Format("Software\\SimonTatham\\PuTTY\\Sessions\\{0}", sessionName), key);

                    results.Add(putty_sess);
                }
            }
            return results;
        }

        private static List<Dictionary<string, string>> ListPuttySSHHostKeys()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // extracts saved putty host keys (via the registry)
            if (MyUtils.IsHighIntegrity())
            {
                Console.WriteLine("\r\n\r\n=== Putty SSH Host Hosts (All Users) ===\r\n");

                string[] SIDs = Registry.Users.GetSubKeyNames();
                foreach (string SID in SIDs)
                {
                    if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                    {
                        Dictionary<string, object> hostKeys = RegistryHelper.GetRegValues("HKU", string.Format("{0}\\Software\\SimonTatham\\PuTTY\\SshHostKeys\\", SID));
                        if ((hostKeys != null) && (hostKeys.Count != 0))
                        {
                            Dictionary<string, string> putty_ssh = new Dictionary<string, string>
                            {
                                ["UserSID"] = SID
                            };
                            foreach (KeyValuePair<string, object> kvp in hostKeys)
                            {
                                putty_ssh[kvp.Key] = ""; //Looks like only matters the key name, not the value
                            }
                            results.Add(putty_ssh);
                        }
                    }
                }
            }
            else
            {
                Dictionary<string, object> hostKeys = RegistryHelper.GetRegValues("HKCU", "Software\\SimonTatham\\PuTTY\\SshHostKeys\\");
                if ((hostKeys != null) && (hostKeys.Count != 0))
                {
                    Dictionary<string, string> putty_ssh = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, object> kvp in hostKeys)
                    {
                        putty_ssh[kvp.Key] = ""; //Looks like only matters the key name, not the value
                    }
                    results.Add(putty_ssh);
                }
            }
            return results;
        }
    }
}
