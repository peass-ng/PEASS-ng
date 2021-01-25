using System;
using System.Collections.Generic;
using System.Management;

namespace winPEAS._3rdParty.Watson
{
    public class Wmi
    {
        public static List<int> GetInstalledKBs()
        {
            var KbList = new List<int>();

            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2", "SELECT HotFixID FROM Win32_QuickFixEngineering"))
                {
                    using (var hotFixes = searcher.Get())
                    {
                        foreach (var hotFix in hotFixes)
                        {
                            var line = hotFix["HotFixID"].ToString().Remove(0, 2);

                            if (int.TryParse(line, out int kb))
                            {
                                KbList.Add(kb);
                            }
                        }
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.Error.WriteLine(" [!] {0}", e.Message);
            }

            return KbList;
        }

        public static int GetBuildNumber()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2", "SELECT BuildNumber FROM Win32_OperatingSystem"))
                {
                    using (var collection = searcher.Get())
                    {
                        foreach (var num in collection)
                        {
                            if (int.TryParse(num["BuildNumber"] as string, out int buildNumber))
                            {
                                return buildNumber;
                            }
                        }
                    }
                }
            }
            catch (ManagementException e)
            {
                Console.Error.WriteLine(" [!] {0}", e.Message);
            }

            return 0;
        }
    }
}
