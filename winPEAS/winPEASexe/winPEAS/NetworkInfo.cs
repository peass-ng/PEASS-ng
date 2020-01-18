using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Net;
using System.Linq;

namespace winPEAS
{
    class NetworkInfo
    {
        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int GetIpNetTable(IntPtr pIpNetTable, [MarshalAs(UnmanagedType.U4)]ref int pdwSize, bool bOrder);

        [DllImport("IpHlpApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int FreeMibTable(IntPtr plpNetTable);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MIB_IPNETROW
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwIndex;
            [MarshalAs(UnmanagedType.U4)]
            public int dwPhysAddrLen;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac0;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac1;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac2;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac3;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac4;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac5;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac6;
            [MarshalAs(UnmanagedType.U1)]
            public byte mac7;
            [MarshalAs(UnmanagedType.U4)]
            public int dwAddr;
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
        }

        public enum ArpEntryType
        {
            Other = 1,
            Invalid = 2,
            Dynamic = 3,
            Static = 4,
        }
        public const int ERROR_INSUFFICIENT_BUFFER = 122;


        public static List<Dictionary<string, string>> GetNetCardInfo()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            Dictionary<int, Dictionary<string, string>> adapters = new Dictionary<int, Dictionary<string, string>>();

            try
            {
                foreach (NetworkInterface netElement in NetworkInterface.GetAllNetworkInterfaces())
                {
                    Dictionary<string, string> card = new Dictionary<string, string>() {
                        { "Index", netElement.GetIPProperties().GetIPv4Properties().Index.ToString() },
                        { "Name", netElement.Name },
                        { "PysicalAddr", "" },
                        { "DNSs", String.Join(", ", netElement.GetIPProperties().DnsAddresses) },
                        { "Gateways", "" },
                        { "IPs", "" },
                        { "Netmasks", "" },
                        { "arp", "" }
                    };
                    card["PysicalAddrIni"] = netElement.GetPhysicalAddress().ToString();
                    for (int i = 0; i < card["PysicalAddrIni"].Length; i += 2)
                        card["PysicalAddr"] += card["PysicalAddrIni"].Substring(i, 2) + ":";

                    foreach (GatewayIPAddressInformation address in netElement.GetIPProperties().GatewayAddresses.Reverse()) //Reverse so first IPv4
                        card["Gateways"] += address.Address + ", ";

                    foreach (UnicastIPAddressInformation ip in netElement.GetIPProperties().UnicastAddresses.Reverse())
                    { //Reverse so first IPv4
                        card["IPs"] += ip.Address.ToString() + ", ";
                        card["Netmasks"] += ip.IPv4Mask.ToString() + ", ";
                    }

                    //Delete last separator
                    if (card["PysicalAddr"].Length > 0)
                        card["PysicalAddr"] = card["PysicalAddr"].Remove(card["PysicalAddr"].Length - 1);

                    if (card["Gateways"].Length > 0)
                        card["Gateways"] = card["Gateways"].Remove(card["Gateways"].Length - 2);

                    if (card["IPs"].Length > 0)
                        card["IPs"] = card["IPs"].Remove(card["IPs"].Length - 2);

                    if (card["Netmasks"].Length > 0)
                        card["Netmasks"] = card["Netmasks"].Remove(card["Netmasks"].Length - 2);

                    adapters[netElement.GetIPProperties().GetIPv4Properties().Index] = card;
                }
                //return results;

                // GET ARP values

                int bytesNeeded = 0;

                int result = GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);

                // call the function, expecting an insufficient buffer.
                if (result != ERROR_INSUFFICIENT_BUFFER)
                {
                    Console.WriteLine("  [X] Exception: {0}", result);
                }

                IntPtr buffer = IntPtr.Zero;

                // allocate sufficient memory for the result structure
                buffer = Marshal.AllocCoTaskMem(bytesNeeded);

                result = GetIpNetTable(buffer, ref bytesNeeded, false);

                if (result != 0)
                {
                    Console.WriteLine("  [X] Exception allocating buffer: {0}", result);
                }

                // now we have the buffer, we have to marshal it. We can read the first 4 bytes to get the length of the buffer
                int entries = Marshal.ReadInt32(buffer);

                // increment the memory pointer by the size of the int
                IntPtr currentBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(typeof(int)));

                // allocate a list of entries
                List<MIB_IPNETROW> arpEntries = new List<MIB_IPNETROW>();

                // cycle through the entries
                for (int index = 0; index < entries; index++)
                {
                    arpEntries.Add((MIB_IPNETROW)Marshal.PtrToStructure(new IntPtr(currentBuffer.ToInt64() + (index * Marshal.SizeOf(typeof(MIB_IPNETROW)))), typeof(MIB_IPNETROW)));
                }

                // sort the list by interface index
                List<MIB_IPNETROW> sortedARPEntries = arpEntries.OrderBy(o => o.dwIndex).ToList();
                int currentIndexAdaper = -1;

                foreach (MIB_IPNETROW arpEntry in sortedARPEntries)
                {
                    int indexAdapter = arpEntry.dwIndex;
                    if (!adapters.ContainsKey(indexAdapter))
                    {
                        Console.WriteLine("Error: No interface found with Index " + arpEntry.dwIndex.ToString());
                        continue;
                    }
                    currentIndexAdaper = indexAdapter;

                    IPAddress ipAddr = new IPAddress(BitConverter.GetBytes(arpEntry.dwAddr));
                    byte[] macBytes = new byte[] { arpEntry.mac0, arpEntry.mac1, arpEntry.mac2, arpEntry.mac3, arpEntry.mac4, arpEntry.mac5 };
                    string physAddr = BitConverter.ToString(macBytes);
                    ArpEntryType entryType = (ArpEntryType)arpEntry.dwType;
                    adapters[arpEntry.dwIndex]["arp"] += String.Format("          {0,-22}{1,-22}{2}\n", ipAddr, physAddr, entryType);
                }

                FreeMibTable(buffer);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            results = adapters.Values.ToList();
            return results;
        }

        public static List<List<string>> GetNetConnections()
        {
            List<List<string>> results = new List<List<string>>();
            try
            {
                var props = IPGlobalProperties.GetIPGlobalProperties();
                results.Add(new List<string>() { "Proto", "Local Address", "Foreing Address", "State" });

                //foreach (var conn in props.GetActiveTcpConnections())
                //    results.Add(new List<string>() { "TCP", conn.LocalEndPoint.ToString(), conn.RemoteEndPoint.ToString(), conn.State.ToString() });

                foreach (var listener in props.GetActiveTcpListeners())
                {
                    bool repeated = false;
                    foreach(List<string> inside_entry in results)
                    {
                        if (inside_entry.SequenceEqual(new List<string>() { "TCP", listener.ToString(), "", "Listening" }))
                            repeated = true;
                    }
                    if (! repeated)
                        results.Add(new List<string>() { "TCP", listener.ToString(), "", "Listening" });
                }

                foreach (var listener in props.GetActiveUdpListeners())
                {
                    bool repeated = false;
                    foreach (List<string> inside_entry in results)
                    {
                        if (inside_entry.SequenceEqual(new List<string>() { "UDP", listener.ToString(), "", "Listening" }))
                            repeated = true;
                    }
                    if (!repeated)
                        results.Add(new List<string>() { "UDP", listener.ToString(), "", "Listening" });
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }

            return results;
        }

        // From Seatbelt
        [Flags]
        public enum FirewallProfiles : int
        {
            DOMAIN = 1,
            PRIVATE = 2,
            PUBLIC = 4,
            ALL = 2147483647
        }
        public static string GetFirewallProfiles()
        {
            string result = "";
            try
            {
                Type firewall = Type.GetTypeFromCLSID(new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD"));
                Object firewallObj = Activator.CreateInstance(firewall);
                Object types = firewallObj.GetType().InvokeMember("CurrentProfileTypes", BindingFlags.GetProperty, null, firewallObj, null);
                result = String.Format("{0}", (FirewallProfiles)Int32.Parse(types.ToString()));
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return result;
        }
        public static Dictionary<string, string> GetFirewallBooleans()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                Type firewall = Type.GetTypeFromCLSID(new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD"));
                Object firewallObj = Activator.CreateInstance(firewall);
                Object enabledDomain = firewallObj.GetType().InvokeMember("FirewallEnabled", BindingFlags.GetProperty, null, firewallObj, new object[] { 1 });
                Object enabledPrivate = firewallObj.GetType().InvokeMember("FirewallEnabled", BindingFlags.GetProperty, null, firewallObj, new object[] { 2 });
                Object enabledPublic = firewallObj.GetType().InvokeMember("FirewallEnabled", BindingFlags.GetProperty, null, firewallObj, new object[] { 4 });
                results = new Dictionary<string, string>() {
                    { "FirewallEnabled (Domain)", String.Format("{0}", enabledDomain) },
                    { "FirewallEnabled (Private)", String.Format("{0}", enabledPrivate) },
                    { "FirewallEnabled (Public)", String.Format("{0}", enabledPublic) },
                };
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }
        public static List<Dictionary<string, string>> GetFirewallRules()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                //Filtrado por DENY como Seatbelt??
                // GUID for HNetCfg.FwPolicy2 COM object
                Type firewall = Type.GetTypeFromCLSID(new Guid("E2B3C97F-6AE1-41AC-817A-F6F92166D7DD"));
                Object firewallObj = Activator.CreateInstance(firewall);

                // now grab all the rules
                Object rules = firewallObj.GetType().InvokeMember("Rules", BindingFlags.GetProperty, null, firewallObj, null);

                // manually get the enumerator() method
                System.Collections.IEnumerator enumerator = (System.Collections.IEnumerator)rules.GetType().InvokeMember("GetEnumerator", BindingFlags.InvokeMethod, null, rules, null);

                // move to the first item
                enumerator.MoveNext();
                Object currentItem = enumerator.Current;

                while (currentItem != null)
                {
                    // only display enabled rules
                    Object Enabled = currentItem.GetType().InvokeMember("Enabled", BindingFlags.GetProperty, null, currentItem, null);
                    if (Enabled.ToString() == "True")
                    {
                        Object Action = currentItem.GetType().InvokeMember("Action", BindingFlags.GetProperty, null, currentItem, null);
                        if (Action.ToString() == "0") //Only DENY rules
                        {
                            // extract all of our fields
                            Object Name = currentItem.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, currentItem, null);
                            Object Description = currentItem.GetType().InvokeMember("Description", BindingFlags.GetProperty, null, currentItem, null);
                            Object Protocol = currentItem.GetType().InvokeMember("Protocol", BindingFlags.GetProperty, null, currentItem, null);
                            Object ApplicationName = currentItem.GetType().InvokeMember("ApplicationName", BindingFlags.GetProperty, null, currentItem, null);
                            Object LocalAddresses = currentItem.GetType().InvokeMember("LocalAddresses", BindingFlags.GetProperty, null, currentItem, null);
                            Object LocalPorts = currentItem.GetType().InvokeMember("LocalPorts", BindingFlags.GetProperty, null, currentItem, null);
                            Object RemoteAddresses = currentItem.GetType().InvokeMember("RemoteAddresses", BindingFlags.GetProperty, null, currentItem, null);
                            Object RemotePorts = currentItem.GetType().InvokeMember("RemotePorts", BindingFlags.GetProperty, null, currentItem, null);
                            Object Direction = currentItem.GetType().InvokeMember("Direction", BindingFlags.GetProperty, null, currentItem, null);
                            Object Profiles = currentItem.GetType().InvokeMember("Profiles", BindingFlags.GetProperty, null, currentItem, null);

                            string ruleAction = "ALLOW";
                            if (Action.ToString() != "1")
                                ruleAction = "DENY";

                            string ruleDirection = "IN";
                            if (Direction.ToString() != "1")
                                ruleDirection = "OUT";

                            string ruleProtocol = "TCP";
                            if (Protocol.ToString() != "6")
                                ruleProtocol = "UDP";

                            Dictionary<string, string> rule = new Dictionary<string, string> { };
                            rule["Name"] = String.Format("{0}", Name);
                            rule["Description"] = String.Format("{0}", Description);
                            rule["AppName"] = String.Format("{0}", ApplicationName);
                            rule["Protocol"] = String.Format("{0}", ruleProtocol);
                            rule["Action"] = String.Format("{0}", ruleAction);
                            rule["Direction"] = String.Format("{0}", ruleDirection);
                            rule["Profiles"] = String.Format("{0}", Int32.Parse(Profiles.ToString()));
                            rule["Local"] = String.Format("{0}:{1}", LocalAddresses, LocalPorts);
                            rule["Remote"] = String.Format("{0}:{1}", RemoteAddresses, RemotePorts);
                            results.Add(rule);
                        }
                    }
                    // manually move the enumerator
                    enumerator.MoveNext();
                    currentItem = enumerator.Current;
                }
                Marshal.ReleaseComObject(firewallObj);
                firewallObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        // https://stackoverflow.com/questions/3567063/get-a-list-of-all-unc-shared-folders-on-a-local-network-server
        // v2: https://stackoverflow.com/questions/6227892/reading-share-permissions-in-c-sharp
        public static List<Dictionary<string, string>> GetNetworkShares(string pcname)
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                ManagementClass manClass = new ManagementClass(@"\\" + pcname + @"\root\cimv2:Win32_Share"); //get shares

                foreach (ManagementObject objShare in manClass.GetInstances())
                {
                    int current_perm = 0;
                    string perm_str = "";

                    try
                    {
                        //get the access values you have
                        ManagementBaseObject result = objShare.InvokeMethod("GetAccessMask", null, null);

                        //value meanings: http://msdn.microsoft.com/en-us/library/aa390438(v=vs.85).aspx
                        current_perm = Convert.ToInt32(result.Properties["ReturnValue"].Value);
                        perm_str = MyUtils.permInt2Str(current_perm);
                    }
                    catch (ManagementException me)
                    {
                        perm_str = ""; //no permissions are set on the share
                    }

                    Dictionary<string, string> share = new Dictionary<string, string> { };
                    share["Name"] = String.Format("{0}", objShare.Properties["Name"].Value);
                    share["Path"] = String.Format("{0}", objShare.Properties["Path"].Value);
                    share["Permissions"] = perm_str;
                    results.Add(share);
                }

            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        //From Seatbelt
        public static List<Dictionary<string, string>> GetDNSCache()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {

                ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\standardcimv2", "SELECT * FROM MSFT_DNSClientCache");
                ManagementObjectCollection data = wmiData.Get();

                foreach (ManagementObject result in data)
                {
                    Dictionary<string, string> dnsEntry = new Dictionary<string, string>();
                    string entry = String.Format("{0}", result["Entry"]);
                    string name = String.Format("{0}", result["Name"]);
                    string dataDns = String.Format("{0}", result["Data"]);
                    dnsEntry["Entry"] = (entry.Length > 33) ? "..." + result["Entry"].ToString().Substring(entry.Length - 32) : entry;
                    dnsEntry["Name"] = (name.Length > 33) ? "..." + name.Substring(name.Length - 32) : name;
                    dnsEntry["Data"] = (dataDns.Length > 33) ? "..." + dataDns.Substring(dataDns.Length - 32) : dataDns;
                    results.Add(dnsEntry);
                }
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
            {
                Console.WriteLine("  [X] 'MSFT_DNSClientCache' WMI class unavailable (minimum supported versions of Windows: 8/2012)", ex.Message);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }
    }
}
