using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace winPEAS.Info.NetworkInfo.NetworkScanner
{
    internal static class NetworkUtils
    {

        /// <summary>
        /// IPAddress to UInteger 
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static uint IPToUInt(this string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return 0;

            if (IPAddress.TryParse(ipAddress, out IPAddress ip))
            {
                var bytes = ip.GetAddressBytes();
                Array.Reverse(bytes);
                return BitConverter.ToUInt32(bytes, 0);
            }
            else
                return 0;

        }

        /// <summary>
        /// IP in Uinteger to string
        /// </summary>
        /// <param name="ipUInt"></param>
        /// <returns></returns>
        public static string IPToString(this uint ipUInt)
        {
            return ToIPAddress(ipUInt).ToString();
        }


        /// <summary>
        /// IP in Uinteger to IPAddress
        /// </summary>
        /// <param name="ipUInt"></param>
        /// <returns></returns>
        public static IPAddress ToIPAddress(this uint ipUInt)
        {
            var bytes = BitConverter.GetBytes(ipUInt);
            Array.Reverse(bytes);
            return new IPAddress(bytes);
        }

        /// <summary>
        /// First and Last IPv4 from IP + Mask
        /// </summary>
        /// <param name="ipv4"></param>
        /// <param name="mask">Accepts CIDR or IP. Example 255.255.255.0 or 24</param>
        /// <param name="filterUsable">Removes not usable IPs from Range</param>
        /// <returns></returns>
        /// <remarks>
        /// If ´filterUsable=false´ first IP is not usable and last is reserved for broadcast.
        /// </remarks>
        public static string[] GetIpRange(string ipv4, string mask, bool filterUsable)
        {
            uint[] uiIpRange = GetIpUintRange(ipv4, mask, filterUsable);

            return Array.ConvertAll(uiIpRange, x => IPToString(x));
        }

        /// <summary>
        /// First and Last IPv4 + Mask. 
        /// </summary>
        /// <param name="ipv4"></param>
        /// <param name="mask">Accepts CIDR or IP. Example 255.255.255.0 or 24</param>
        /// <param name="filterUsable">Removes not usable IPs from Range</param>
        /// <returns></returns>
        /// <remarks>
        /// First IP is not usable and last is reserverd for broadcast.
        /// Can use all IPs in between
        /// </remarks>
        public static uint[] GetIpUintRange(string ipv4, string mask, bool filterUsable)
        {
            uint sub;
            //check if mask is CIDR Notation
            if (mask.Contains("."))
            {
                sub = IPToUInt(mask);
            }
            else
            {
                sub = ~(0xffffffff >> Convert.ToInt32(mask));
            }

            uint ip2 = IPToUInt(ipv4);


            uint first = ip2 & sub;
            uint last = first | (0xffffffff & ~sub);

            if (filterUsable)
            {
                first += 1;
                last -= 1;
            }

            return new uint[] { first, last };
        }

        public static IEnumerable<string> GetIPRange(IPAddress startIP, IPAddress endIP)
        {
            uint sIP = ipToUint(startIP.GetAddressBytes());
            uint eIP = ipToUint(endIP.GetAddressBytes());
            while (sIP <= eIP)
            {
                yield return new IPAddress(reverseBytesArray(sIP)).ToString();
                sIP++;
            }
        }

        public static string CidrToNetmask(int cidr)
        {
            var nmask = 0xFFFFFFFF;
            nmask <<= 32 - cidr;
            byte[] bytes = BitConverter.GetBytes(nmask);
            Array.Reverse(bytes);
            nmask = BitConverter.ToUInt32(bytes, 0);
            var netmask = new System.Net.IPAddress(nmask);
            return netmask.ToString();
        }

        public static IEnumerable<string> GetIPAddressesByNetmask(string ipAddress, string netmask)
        {
            // TODO
            // e.g. 
            // netmask should be e.g. 24 - currently we only support this format            
            string[] range = NetworkUtils.GetIpRange(ipAddress, netmask, false);

            return range;
        }

        public static IEnumerable<string> GetHostsByIPAndNetmask(string ipAddressAndNetmask)
        {
            // TODO
            // get hosts by ip address & netmask

            // https://itecnote.com/tecnote/c-proper-way-to-scan-a-range-of-ip-addresses/
            // we nned to (maybe in parallel)
            // - ping e.g. 3 times
            // - scan top 5 ports 
            var parts = ipAddressAndNetmask.Split(':');

            return new List<string>
            {
                parts[0]
            };
        }

        public static List<Tuple<string, string>> GetInternalInterfaces()
        {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    // Console.WriteLine();
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            // we need ip address and a netmask as well
                            result.Add(new Tuple<string, string>(ip.Address.ToString(), ip.IPv4Mask.ToString()));
                        }
                    }
                }
            }

            return result;
        }

        /* Convert bytes array to 32 bit long value */
        static uint ipToUint(byte[] ipBytes)
        {
            ByteConverter bConvert = new ByteConverter();
            uint ipUint = 0;

            int shift = 24; // indicates number of bits left for shifting
            foreach (byte b in ipBytes)
            {
                if (ipUint == 0)
                {
                    ipUint = (uint)bConvert.ConvertTo(b, typeof(uint)) << shift;
                    shift -= 8;
                    continue;
                }

                if (shift >= 8)
                    ipUint += (uint)bConvert.ConvertTo(b, typeof(uint)) << shift;
                else
                    ipUint += (uint)bConvert.ConvertTo(b, typeof(uint));

                shift -= 8;
            }

            return ipUint;
        }

        /* reverse byte order in array */
        private static uint reverseBytesArray(uint ip)
        {
            byte[] bytes = BitConverter.GetBytes(ip);
            bytes = bytes.Reverse().ToArray();
            return (uint)BitConverter.ToInt32(bytes, 0);
        }
    }
}
