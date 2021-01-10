using System;
using System.Collections.Generic;
using System.IO;
using winPEAS.Helpers;
using winPEAS.Info.NetworkInfo;

namespace winPEAS.Checks
{
    internal class NetworkInfo : ISystemCheck
    {
        static string commonShares = "[a-zA-Z]+[$]";
        static string badIps = "127.0.0.1";

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Network Information");
           
            new List<Action>
            {
                PrintNetShares,
                PrintHostsFile,
                PrintNetworkIfaces,
                PrintListeningPorts,
                PrintFirewallRules,
                PrintDNSCache,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        void PrintNetShares()
        {
            try
            {
                Beaprint.MainPrint("Network Shares");
                Dictionary<string, string> colorsN = new Dictionary<string, string>()
                {
                    { commonShares, Beaprint.ansi_color_good },
                    { "Permissions.*", Beaprint.ansi_color_bad }
                };

                List<Dictionary<string, string>> shares = NetworkInfoHelper.GetNetworkShares("127.0.0.1");

                foreach (Dictionary<string, string> share in shares)
                {
                    string line = string.Format("    {0} (" + Beaprint.ansi_color_gray + "Path: {1}" + Beaprint.NOCOLOR + ")", share["Name"], share["Path"]);
                    if (share["Permissions"].Length > 0)
                    {
                        line += " -- Permissions: " + share["Permissions"];
                    }
                    Beaprint.AnsiPrint(line, colorsN);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintHostsFile()
        {
            try
            {
                Beaprint.MainPrint("Host File");
                string[] lines = File.ReadAllLines(@Path.GetPathRoot(Environment.SystemDirectory) + @"\windows\system32\drivers\etc\hosts");

                foreach (string line in lines)
                {
                    if (line.Length > 0 && line[0] != '#')
                    {
                        System.Console.WriteLine("    " + line.Replace("\t", "    "));
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintNetworkIfaces()
        {
            try
            {
                Beaprint.MainPrint("Network Ifaces and known hosts");
                Beaprint.LinkPrint("", "The masks are only for the IPv4 addresses");
                foreach (Dictionary<string, string> card in NetworkInfoHelper.GetNetCardInfo())
                {
                    string formString = "    {0}[{1}]: {2} / {3}";
                    if (card["Gateways"].Length > 1)
                        formString += "\n        " + Beaprint.ansi_color_gray + "Gateways: " + Beaprint.NOCOLOR + "{4}";
                    if (card["DNSs"].Length > 1)
                        formString += "\n        " + Beaprint.ansi_color_gray + "DNSs: " + Beaprint.NOCOLOR + "{5}";
                    if (card["arp"].Length > 1)
                        formString += "\n        " + Beaprint.ansi_color_gray + "Known hosts:" + Beaprint.NOCOLOR + "\n{6}";

                    Console.WriteLine(string.Format(formString, card["Name"], card["PysicalAddr"], card["IPs"], card["Netmasks"].Replace(", 0.0.0.0", ""), card["Gateways"], card["DNSs"], card["arp"]));
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintListeningPorts()
        {
            try
            {
                Beaprint.MainPrint("Current Listening Ports");
                Beaprint.LinkPrint("", "Check for services restricted from the outside");
                List<List<string>> conns = NetworkInfoHelper.GetNetConnections();

                Dictionary<string, string> colorsN = new Dictionary<string, string>()
                {
                    { badIps, Beaprint.ansi_color_bad },
                };

                foreach (List<string> conn in conns)
                {
                    if (conn[0].Contains("UDP") && conn[1].Contains("0.0.0.0:") && (conn[1].Split(':')[1].Length > 4))
                        continue; //Delete useless UDP listening ports

                    if (conn[0].Contains("UDP") && conn[1].Contains("[::]:") && (conn[1].Split(']')[1].Length > 4))
                        continue; //Delete useless UDP listening ports

                    Beaprint.AnsiPrint($"    {conn[0],-10}{conn[1],-23}{conn[2],-23}{conn[3]}", colorsN);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintFirewallRules()
        {
            try
            {
                Beaprint.MainPrint("Firewall Rules");
                Beaprint.LinkPrint("", "Showing only DENY rules (too many ALLOW rules always)");
                Dictionary<string, string> colorsN = new Dictionary<string, string>()
                        {
                            { Globals.StrFalse, Beaprint.ansi_color_bad },
                            { Globals.StrTrue, Beaprint.ansi_color_good },
                        };

                Beaprint.AnsiPrint("    Current Profiles: " + Firewall.GetFirewallProfiles(), colorsN);
                foreach (KeyValuePair<string, string> entry in Firewall.GetFirewallBooleans())
                {
                    Beaprint.AnsiPrint(string.Format("    {0,-23}:    {1}", entry.Key, entry.Value), colorsN);
                }

                Beaprint.GrayPrint("    DENY rules:");
                foreach (Dictionary<string, string> rule in Firewall.GetFirewallRules())
                {
                    string filePerms = string.Join(", ", PermissionsHelper.GetPermissionsFile(rule["AppName"], winPEAS.Checks.Checks.CurrentUserSiDs));
                    string folderPerms = string.Join(", ", PermissionsHelper.GetPermissionsFolder(rule["AppName"], winPEAS.Checks.Checks.CurrentUserSiDs));
                    string formString = "    ({0}){1}[{2}]: {3} {4} {5} from {6} --> {7}";
                    if (filePerms.Length > 0)
                        formString += "\n    File Permissions: {8}";
                    if (folderPerms.Length > 0)
                        formString += "\n    Folder Permissions: {9}";
                    formString += "\n    {10}";

                    colorsN = new Dictionary<string, string>
                    {
                        { Globals.StrFalse, Beaprint.ansi_color_bad },
                        { Globals.StrTrue, Beaprint.ansi_color_good },
                        { "File Permissions.*|Folder Permissions.*", Beaprint.ansi_color_bad },
                        { rule["AppName"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (filePerms.Length > 0 || folderPerms.Length > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                    };

                    Beaprint.AnsiPrint(string.Format(formString, rule["Profiles"], rule["Name"], rule["AppName"], rule["Action"], rule["Protocol"], rule["Direction"], rule["Direction"] == "IN" ? rule["Local"] : rule["Remote"], rule["Direction"] == "IN" ? rule["Remote"] : rule["Local"], filePerms, folderPerms, rule["Description"]), colorsN);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintDNSCache()
        {
            try
            {
                Beaprint.MainPrint("DNS cached --limit 70--");
                Beaprint.GrayPrint(string.Format("    {0,-38}{1,-38}{2}", "Entry", "Name", "Data"));
                List<Dictionary<string, string>> DNScache = NetworkInfoHelper.GetDNSCache();
                foreach (Dictionary<string, string> entry in DNScache.GetRange(0,
                    DNScache.Count <= 70 ? DNScache.Count : 70))
                {
                    Console.WriteLine($"    {entry["Entry"],-38}{entry["Name"],-38}{entry["Data"]}");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }
    }
}
