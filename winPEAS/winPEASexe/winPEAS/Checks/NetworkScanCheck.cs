using winPEAS.Helpers;

namespace winPEAS.Checks
{
    /// <summary>
    /// Dedicated system check for the -network scan.
    /// Registered as "networkscan" so that passing -network alongside a subset of
    /// checks (e.g. "systeminfo -network=auto") runs only the scan and not the full
    /// NetworkInfo sub-checks (shares, firewall rules, DNS cache, etc.).
    /// When all checks run (no subset selected), this check silently no-ops unless
    /// -network was explicitly passed.
    /// </summary>
    internal class NetworkScanCheck : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            if (!Checks.IsNetworkScan)
                return;

            CheckRunner.Run(new NetworkInfo().PrintNetworkScan, isDebug);
        }
    }
}
