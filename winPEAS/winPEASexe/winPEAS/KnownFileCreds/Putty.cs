using System;
using System.Collections.Generic;
using Microsoft.Win32;
using winPEAS.Utils;

namespace winPEAS.KnownFileCreds
{
    static class Putty
    {
        public static List<Dictionary<string, string>> GetPuttySessions()
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
                        string[] subKeys = RegistryHelper.GetRegSubkeys("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\", SID));

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
                                "ProxyPassword",
                                "ProxyUsername",
                            };

                            foreach (string key in keys)
                                putty_sess[key] = RegistryHelper.GetRegValue("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\Sessions\\{1}", SID, sessionName), key);

                            results.Add(putty_sess);
                        }
                    }
                }
            }
            else
            {
                string[] subKeys = RegistryHelper.GetRegSubkeys("HKCU", "Software\\SimonTatham\\PuTTY\\Sessions\\");
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
                        "ProxyPassword",
                        "ProxyUsername",
                    };

                    foreach (string key in keys)
                        putty_sess[key] = RegistryHelper.GetRegValue("HKCU", String.Format("Software\\SimonTatham\\PuTTY\\Sessions\\{0}", sessionName), key);

                    results.Add(putty_sess);
                }
            }
            return results;
        }


        public static List<Dictionary<string, string>> ListPuttySSHHostKeys()
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
                        Dictionary<string, object> hostKeys = RegistryHelper.GetRegValues("HKU", String.Format("{0}\\Software\\SimonTatham\\PuTTY\\SshHostKeys\\", SID));
                        if ((hostKeys != null) && (hostKeys.Count != 0))
                        {
                            Dictionary<string, string> putty_ssh = new Dictionary<string, string>();
                            putty_ssh["UserSID"] = SID;
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
