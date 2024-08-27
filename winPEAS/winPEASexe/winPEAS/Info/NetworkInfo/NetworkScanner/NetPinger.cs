using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace winPEAS.Info.NetworkInfo.NetworkScanner
{
    internal class NetPinger
    {     
        private int PingTimeout = 1000;
        
        public List<string> HostsAlive = new List<string>();

        private List<string> ipRange = new List<string>();

        public void AddRange(string baseIpAddress, string netmask)
        {
            var addresses = NetworkUtils.GetIPAddressesByNetmask(baseIpAddress, netmask).ToList();
            var range = NetworkUtils.GetIPRange(IPAddress.Parse(addresses[0]), IPAddress.Parse(addresses[1]));

            ipRange.AddRange(range);
        }

        public void AddRange(IEnumerable<string> ipAddressList)
        {
            ipRange.AddRange(ipAddressList);
        }

        public async Task RunPingSweepAsync()
        {
            var tasks = new List<Task>();

            foreach (var ip in ipRange)
            {
                Ping p = new Ping();
                var task = PingAndUpdateStatus(p, ip);
                tasks.Add(task);
            }
            
            await Task.WhenAll(tasks);
        }

        private async Task PingAndUpdateStatus(Ping ping, string ip)
        {
            var reply = await ping.SendPingAsync(ip, PingTimeout);

            if (reply.Status == IPStatus.Success)
            {
                HostsAlive.Add(ip);             
                await Console.Out.WriteLineAsync(ip);
            }
        }
    }
}
