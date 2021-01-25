using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using winPEAS.Helpers;

namespace winPEAS.Info.NetworkInfo
{
    class NetworkInfoHelper
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
                    adapters[arpEntry.dwIndex]["arp"] += $"          {ipAddr,-22}{physAddr,-22}{entryType}\n";
                }

                FreeMibTable(buffer);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
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
                results.Add(new List<string>() { "Proto", "Local Address", "Foreign Address", "State" });

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
                Beaprint.PrintException(ex.Message);
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
                    string permStr = "";

                    try
                    {
                        //get the access values you have
                        ManagementBaseObject result = objShare.InvokeMethod("GetAccessMask", null, null);

                        //value meanings: http://msdn.microsoft.com/en-us/library/aa390438(v=vs.85).aspx
                        var currentPerm = Convert.ToInt32(result.Properties["ReturnValue"].Value);
                        permStr = PermissionsHelper.PermInt2Str(currentPerm);
                    }
                    catch (ManagementException)
                    {
                        permStr = ""; //no permissions are set on the share
                    }

                    Dictionary<string, string> share = new Dictionary<string, string> { };
                    share["Name"] = $"{objShare.Properties["Name"].Value}";
                    share["Path"] = $"{objShare.Properties["Path"].Value}";
                    share["Permissions"] = permStr;
                    results.Add(share);
                }

            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        //From Seatbelt
        public static List<Dictionary<string, string>> GetDNSCache()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                using (ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\standardcimv2", "SELECT * FROM MSFT_DNSClientCache"))
                {
                    using (ManagementObjectCollection data = wmiData.Get())
                    {
                        foreach (ManagementObject result in data)
                        {
                            Dictionary<string, string> dnsEntry = new Dictionary<string, string>();
                            string entry = $"{result["Entry"]}";
                            string name = $"{result["Name"]}";
                            string dataDns = $"{result["Data"]}";
                            dnsEntry["Entry"] = (entry.Length > 33) ? "..." + result["Entry"].ToString().Substring(entry.Length - 32) : entry;
                            dnsEntry["Name"] = (name.Length > 33) ? "..." + name.Substring(name.Length - 32) : name;
                            dnsEntry["Data"] = (dataDns.Length > 33) ? "..." + dataDns.Substring(dataDns.Length - 32) : dataDns;
                            results.Add(dnsEntry);
                        }
                    }
                }
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
            {
                Console.WriteLine("  [X] 'MSFT_DNSClientCache' WMI class unavailable (minimum supported versions of Windows: 8/2012)", ex.Message);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
