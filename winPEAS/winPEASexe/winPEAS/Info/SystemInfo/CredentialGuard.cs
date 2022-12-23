using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using winPEAS.Helpers;

namespace winPEAS.Info.SystemInfo
{
    internal class CredentialGuard
    {
        const string NOT_ENABLED = "Not enabled";
        const string ENABLED_NOT_RUNNING = "Enabled not running";
        const string ENABLED_AND_RUNNING = "Enabled and running";
        const string UNDEFINED = "Undefined";

        internal static void PrintInfo()
        {
            var colors = new Dictionary<string, string>()
            {
                {  "False", Beaprint.ansi_color_bad },
                {  "True", Beaprint.ansi_color_good },
                {  NOT_ENABLED, Beaprint.ansi_color_bad },
                {  ENABLED_NOT_RUNNING, Beaprint.ansi_color_bad },
                {  ENABLED_AND_RUNNING, Beaprint.ansi_color_good },
                {  UNDEFINED, Beaprint.ansi_color_bad },
            };

            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\Microsoft\Windows\DeviceGuard", "SELECT * FROM Win32_DeviceGuard"))
                {
                    using (var data = searcher.Get())
                    {
                        foreach (var result in data)
                        {
                            var configCheck = (int[])result.GetPropertyValue("SecurityServicesConfigured");
                            var serviceCheck = (int[])result.GetPropertyValue("SecurityServicesRunning");

                            var configured = false;
                            var running = false;

                            uint? vbs = (uint)result.GetPropertyValue("VirtualizationBasedSecurityStatus");
                            string vbsSettingString = GetVbsSettingString(vbs);

                            if (configCheck.Contains(1))
                            {
                                configured = true;
                            }

                            if (serviceCheck.Contains(1))
                            {
                                running = true;
                            }

                            Beaprint.AnsiPrint($"    Virtualization Based Security Status:      {vbsSettingString}\n" +
                                               $"    Configured:                                {configured}\n" +
                                               $"    Running:                                   {running}",
                                                  colors);

                        }
                    }
                }
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace)
            {
                Beaprint.PrintException(string.Format("  [X] 'Win32_DeviceGuard' WMI class unavailable", ex.Message));
            }
            catch (Exception ex)
            {
                //Beaprint.PrintException(ex.Message);
            }
        }

        private static string GetVbsSettingString(uint? vbs)
        {
            /*
                NOT_ENABLED = 0,
                ENABLED_NOT_RUNNING = 1,
                ENABLED_AND_RUNNING = 2
            */
            switch (vbs)
            {
                case 0: return NOT_ENABLED;
                case 1: return ENABLED_NOT_RUNNING;
                case 2: return ENABLED_AND_RUNNING;

                default: return UNDEFINED;
            }
        }
    }
}
