using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using winPEAS.Checks;
using winPEAS.Helpers;

namespace winPEAS.Info.NetworkInfo.NetworkScanner
{
    internal class NetPinger
    {     
        private int PingTimeout = 1000;
        private const int MaxConcurrentPings = 50;
        
        public ConcurrentBag<string> HostsAlive = new ConcurrentBag<string>();

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
            using (var semaphore = new SemaphoreSlim(MaxConcurrentPings))
            {
                var tasks = new List<Task>();

                foreach (var ip in ipRange)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(PingAndUpdateStatus(ip, semaphore));
                }

                await Task.WhenAll(tasks);
            }
        }

        private async Task PingAndUpdateStatus(string ip, SemaphoreSlim semaphore)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ip, PingTimeout);

                    if (reply.Status == IPStatus.Success)
                    {
                        HostsAlive.Add(ip);
                        Beaprint.GoodPrint($"    [+] Host alive: {ip}");
                    }
                }
            }
            catch (PingException)
            {
                // ICMP blocked, invalid address, or host unreachable — treat as down.
            }
            catch (Exception ex) when (Checks.IsDebug)
            {
                Beaprint.PrintException($"    [!] Ping error for {ip}: {ex.Message}");
            }
            catch (Exception)
            {
                // Isolate per-IP failures so a single bad target can't abort the sweep.
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
