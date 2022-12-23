using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using winPEAS.Helpers;
using winPEAS.Helpers.Extensions;
using winPEAS.Info.NetworkInfo;
using winPEAS.Info.NetworkInfo.Enums;
using winPEAS.Info.NetworkInfo.InternetSettings;

namespace winPEAS.Checks
{
    internal class NetworkInfo : ISystemCheck
    {
        static string commonShares = "[a-zA-Z]+[$]";
        static string badIps = "127.0.0.1";

        static Dictionary<string, string> colorsN = new Dictionary<string, string>()
        {
            { badIps, Beaprint.ansi_color_bad },
            { @"\[\:\:1\]", Beaprint.ansi_color_bad },
            { @"\[\:\:\]", Beaprint.ansi_color_bad },
        };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Network Information");

            new List<Action>
            {
                PrintNetShares,
                PrintMappedDrivesWMI,
                PrintHostsFile,
                PrintNetworkIfaces,
                PrintListeningPorts,
                PrintFirewallRules,
                PrintDNSCache,
                PrintInternetSettings,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private void PrintNetShares()
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

        private void PrintHostsFile()
        {
            try
            {
                Beaprint.MainPrint("Host File");
                string[] lines = File.ReadAllLines(@Path.GetPathRoot(Environment.SystemDirectory) + @"\windows\system32\drivers\etc\hosts");

                foreach (string line in lines)
                {
                    if (line.Length > 0 && line[0] != '#')
                    {
                        Console.WriteLine("    " + line.Replace("\t", "    "));
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintNetworkIfaces()
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

        private void PrintListeningPorts()
        {
            Process[] processes = Process.GetProcesses();
            Dictionary<int, Process> processesByPid = processes.ToDictionary(k => k.Id, v => v);

            PrintListeningPortsTcp(processesByPid);
            PrintListeningPortsUdp(processesByPid);
        }

        private void PrintListeningPortsTcp(Dictionary<int, Process> processesByPid)
        {
            Beaprint.MainPrint("Current TCP Listening Ports");
            Beaprint.LinkPrint("", "Check for services restricted from the outside");

            PrintListeningPortsTcpIPv4(processesByPid);
            Beaprint.ColorPrint("", Beaprint.NOCOLOR);
            PrintListeningPortsTcpIPv6(processesByPid);
        }

        private void PrintListeningPortsTcpIPv4(Dictionary<int, Process> processesByPid)
        {
            try
            {
                Beaprint.ColorPrint("  Enumerating IPv4 connections\n", Beaprint.LBLUE);

                string formatString = @"{0,-12} {1,-21} {2,-13} {3,-21} {4,-15} {5,-17} {6,-15} {7}";

                Beaprint.NoColorPrint(
                    string.Format($"{formatString}\n", "  Protocol", "Local Address", "Local Port", "Remote Address", "Remote Port", "State", "Process ID", "Process Name"));

                foreach (var tcpConnectionInfo in NetworkInfoHelper.GetTcpConnections(IPVersion.IPv4, processesByPid))
                {
                    Beaprint.AnsiPrint(
                        string.Format(formatString,
                                       "  TCP",
                                       tcpConnectionInfo.LocalAddress,
                                       tcpConnectionInfo.LocalPort,
                                       tcpConnectionInfo.RemoteAddress,
                                       tcpConnectionInfo.RemotePort,
                                       tcpConnectionInfo.State.GetDescription(),
                                       tcpConnectionInfo.ProcessId,
                                       tcpConnectionInfo.ProcessName
                                     ),
                                     colorsN);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintListeningPortsTcpIPv6(Dictionary<int, Process> processesByPid)
        {
            try
            {
                Beaprint.ColorPrint("  Enumerating IPv6 connections\n", Beaprint.LBLUE);

                string formatString = @"{0,-12} {1,-43} {2,-13} {3,-43} {4,-15} {5,-17} {6,-15} {7}";

                Beaprint.NoColorPrint(
                    string.Format($"{formatString}\n", "  Protocol", "Local Address", "Local Port", "Remote Address", "Remote Port", "State", "Process ID", "Process Name"));

                foreach (var tcpConnectionInfo in NetworkInfoHelper.GetTcpConnections(IPVersion.IPv6, processesByPid))
                {
                    Beaprint.AnsiPrint(
                        string.Format(formatString,
                                       "  TCP",
                                       $"[{tcpConnectionInfo.LocalAddress}]",
                                       tcpConnectionInfo.LocalPort,
                                       $"[{tcpConnectionInfo.RemoteAddress}]",
                                       tcpConnectionInfo.RemotePort,
                                       tcpConnectionInfo.State.GetDescription(),
                                       tcpConnectionInfo.ProcessId,
                                       tcpConnectionInfo.ProcessName
                                     ),
                                     colorsN);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintListeningPortsUdp(Dictionary<int, Process> processesByPid)
        {
            Beaprint.MainPrint("Current UDP Listening Ports");
            Beaprint.LinkPrint("", "Check for services restricted from the outside");

            PrintListeningPortsUdpIPv4(processesByPid);
            Beaprint.ColorPrint("", Beaprint.NOCOLOR);
            PrintListeningPortsUdpIPv6(processesByPid);
        }

        private void PrintListeningPortsUdpIPv4(Dictionary<int, Process> processesByPid)
        {
            try
            {
                Beaprint.ColorPrint("  Enumerating IPv4 connections\n", Beaprint.LBLUE);

                string formatString = @"{0,-12} {1,-21} {2,-13} {3,-30} {4,-17} {5}";

                Beaprint.NoColorPrint(
                    string.Format($"{formatString}\n", "  Protocol", "Local Address", "Local Port", "Remote Address:Remote Port", "Process ID", "Process Name"));

                foreach (var udpConnectionInfo in NetworkInfoHelper.GetUdpConnections(IPVersion.IPv4, processesByPid))
                {
                    if (udpConnectionInfo.ProcessName == "dns") // Hundreds of them sometimes
                    {
                        continue;
                    }

                    Beaprint.AnsiPrint(
                        string.Format(formatString,
                                       "  UDP",
                                       udpConnectionInfo.LocalAddress,
                                       udpConnectionInfo.LocalPort,
                                       "*:*",   // UDP does not have remote address/port
                                       udpConnectionInfo.ProcessId,
                                       udpConnectionInfo.ProcessName
                                     ),
                                     colorsN);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintListeningPortsUdpIPv6(Dictionary<int, Process> processesByPid)
        {
            try
            {
                Beaprint.ColorPrint("  Enumerating IPv6 connections\n", Beaprint.LBLUE);

                string formatString = @"{0,-12} {1,-43} {2,-13} {3,-30} {4,-17} {5}";

                Beaprint.NoColorPrint(
                    string.Format($"{formatString}\n", "  Protocol", "Local Address", "Local Port", "Remote Address:Remote Port", "Process ID", "Process Name"));

                foreach (var udpConnectionInfo in NetworkInfoHelper.GetUdpConnections(IPVersion.IPv6, processesByPid))
                {
                    if (udpConnectionInfo.ProcessName == "dns") // Hundreds of them sometimes
                    {
                        continue;
                    }

                    Beaprint.AnsiPrint(
                        string.Format(formatString,
                                       "  UDP",
                                       $"[{udpConnectionInfo.LocalAddress}]",
                                       udpConnectionInfo.LocalPort,
                                       "*:*",   // UDP does not have remote address/port
                                       udpConnectionInfo.ProcessId,
                                       udpConnectionInfo.ProcessName
                                     ),
                                     colorsN);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintFirewallRules()
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
                    string filePerms = string.Join(", ", PermissionsHelper.GetPermissionsFile(rule["AppName"], Checks.CurrentUserSiDs));
                    string folderPerms = string.Join(", ", PermissionsHelper.GetPermissionsFolder(rule["AppName"], Checks.CurrentUserSiDs));
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

        private void PrintDNSCache()
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

        private static void PrintMappedDrivesWMI()
        {
            try
            {
                Beaprint.MainPrint("Enumerate Network Mapped Drives (WMI)");

                using (var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_networkconnection"))
                {
                    using (var data = wmiData.Get())
                    {
                        foreach (ManagementObject result in data)
                        {
                            Beaprint.NoColorPrint($"   Local Name         :       {result["LocalName"]}\n" +
                                                        $"   Remote Name        :       {result["RemoteName"]}\n" +
                                                        $"   Remote Path        :       {result["RemotePath"]}\n" +
                                                        $"   Status             :       {result["Status"]}\n" +
                                                        $"   Connection State   :       {result["ConnectionState"]}\n" +
                                                        $"   Persistent         :       {result["Persistent"]}\n" +
                                                        $"   UserName           :       {result["UserName"]}\n" +
                                                        $"   Description        :       {result["Description"]}\n");

                            Beaprint.PrintLineSeparator();
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        private static void PrintInternetSettings()
        {
            try
            {
                Beaprint.MainPrint("Enumerating Internet settings, zone and proxy configuration");

                var info = InternetSettings.GetInternetSettingsInfo();

                Beaprint.ColorPrint("  General Settings", Beaprint.LBLUE);
                Beaprint.NoColorPrint($"  {"Hive",-10}  {"Key",-40}  {"Value"}");

                foreach (var i in info.GeneralSettings)
                {
                    Beaprint.NoColorPrint($"  {i.Hive,-10}  {i.ValueName,-40}  {i.Value}");
                }

                Beaprint.ColorPrint("\n  Zone Maps", Beaprint.LBLUE);

                if (info.ZoneMaps.Count == 0)
                {
                    Beaprint.NoColorPrint("  No URLs configured");
                }
                else
                {
                    Beaprint.NoColorPrint($"  {"Hive",-10}  {"Value Name",-40}  {"Interpretation"}");

                    foreach (var i in info.ZoneMaps)
                    {
                        Beaprint.NoColorPrint($"  {i.Hive,-10}  {i.ValueName,-40}  {i.Interpretation}");
                    }
                }

                Beaprint.ColorPrint("\n  Zone Auth Settings", Beaprint.LBLUE);
                if (info.ZoneAuthSettings.Count == 0)
                {
                    Beaprint.NoColorPrint("  No Zone Auth Settings");
                }
                else
                {
                    foreach (var i in info.ZoneAuthSettings)
                    {
                        Beaprint.NoColorPrint($"  {i.Interpretation}");
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
