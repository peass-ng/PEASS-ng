using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using winPEAS.Helpers;

namespace winPEAS.Info.NetworkInfo.NetworkScanner
{
    internal class NetworkScanner
    {
        enum ScanMode
        {
            Auto,
            IPAddressList,
            IPAddressNetmask,
        }

        private string[] ipAddressList;
        private bool isAuto = false;
        private ScanMode scanMode = ScanMode.IPAddressList;
        private string baseAddress;
        private string netmask;
        IEnumerable<int> ports;

        public NetworkScanner(string options, IEnumerable<int> ports = null)
        {
            /*
               --network "auto"                          -    find interfaces/hosts automatically
               --network "10.10.10.10,10.10.10.20"       -    scan only selected ip address(es)
               --network "10.10.10.10/24"                -    scan host based on ip address/netmask
             */ 
            this.ports = ports;

            if (string.Equals(options, "auto", StringComparison.InvariantCultureIgnoreCase))
            {
                scanMode = ScanMode.Auto;
            }
            else if (options.Contains("/"))
            {
                var parts = options.Split('/');
                baseAddress = parts[0];
                netmask = parts[1];
                scanMode = ScanMode.IPAddressNetmask;
            }
            else
            {
                ipAddressList = options.Split(',');
                scanMode = ScanMode.IPAddressList;
            }
        }

        public void Scan()
        {
            try
            {

                List<string> aliveHosts = new List<string>();
                NetPinger netPinger = new NetPinger();

                if (scanMode == ScanMode.Auto)
                {
                    // this is the "auto" mode
                    foreach (var ipAddressAndNetmask in NetworkUtils.GetInternalInterfaces())
                    {
                        netPinger.AddRange(ipAddressAndNetmask.Item1, ipAddressAndNetmask.Item2);
                    }
                }
                else if (scanMode == ScanMode.IPAddressNetmask)
                {
                    netPinger.AddRange(baseAddress, netmask);
                }
                else if (scanMode == ScanMode.IPAddressList)
                {
                    netPinger.AddRange(ipAddressList);
                }

                var task = netPinger.RunPingSweepAsync();
                task.Wait();
                aliveHosts.AddRange(netPinger.HostsAlive);

                var outerOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };
                Parallel.ForEach(aliveHosts, outerOptions, host =>
                {
                    var ps = new PortScanner(this.ports);
                    ps.Start(host);
                });
            }
            catch (Exception e)
            {
                Beaprint.PrintException(e.Message);
            }           
        }             
    }
}
