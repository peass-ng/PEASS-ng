using System;
using System.Management;

namespace winPEAS.Info.SystemInfo.WindowsDefender
{
    internal enum DefenderCve41091Status
    {
        Unknown,
        Candidate,
        Fixed
    }

    internal sealed class DefenderEngineInfo
    {
        public string EngineVersion { get; set; }
        public bool? ServiceEnabled { get; set; }
    }

    internal class WindowsDefender
    {
        private static readonly Version Cve41091FixedEngine = new Version(1, 1, 26040, 8);

        public static WindowsDefenderSettingsInfo GetDefenderSettingsInfo()
        {
            return new WindowsDefenderSettingsInfo(
                new WindowsDefenderSettings(@"SOFTWARE\Microsoft\Windows Defender\"),
                new WindowsDefenderSettings(@"SOFTWARE\Policies\Microsoft\Windows Defender\")
            );
        }

        internal static DefenderCve41091Status AssessCve41091Engine(string engineVersion)
        {
            if (string.IsNullOrWhiteSpace(engineVersion))
                return DefenderCve41091Status.Unknown;

            string value = engineVersion.Trim();
            if (value.Split('.').Length != 4)
                return DefenderCve41091Status.Unknown;

            Version parsed;
            if (!Version.TryParse(value, out parsed) || parsed.Equals(new Version(0, 0, 0, 0)))
                return DefenderCve41091Status.Unknown;

            return parsed.CompareTo(Cve41091FixedEngine) < 0
                ? DefenderCve41091Status.Candidate
                : DefenderCve41091Status.Fixed;
        }

        public static DefenderEngineInfo GetDefenderEngineInfo()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Defender");
                var query = new ObjectQuery("SELECT AMEngineVersion, AMServiceEnabled FROM MSFT_MpComputerStatus");
                var options = new EnumerationOptions
                {
                    ReturnImmediately = true,
                    Rewindable = false
                };

                using (var searcher = new ManagementObjectSearcher(scope, query, options))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject status in results)
                    {
                        using (status)
                        {
                            return new DefenderEngineInfo
                            {
                                EngineVersion = Convert.ToString(status["AMEngineVersion"]),
                                ServiceEnabled = status["AMServiceEnabled"] == null
                                    ? (bool?)null
                                    : Convert.ToBoolean(status["AMServiceEnabled"])
                            };
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Defender, its WMI provider, or the requested properties may be unavailable.
            }

            return null;
        }
    }
}
