using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo.DotNet
{
    internal class DotNet
    {
        public static DotNetInfo GetDotNetInfo()
        {
            var installedDotNetVersions = new List<string>();
            var installedClrVersions = new List<string>();
            installedClrVersions.AddRange(GetClrVersions());

            var dotNet35Version = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5", "Version");
            if (!string.IsNullOrEmpty(dotNet35Version))
            {
                installedDotNetVersions.Add(dotNet35Version);
            }

            var dotNet4Version = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Version");
            if (!string.IsNullOrEmpty(dotNet4Version))
            {
                installedDotNetVersions.Add(dotNet4Version);
            }

            var osVersion = GetOSVersion().Split('.')[0];
            int osVersionMajor = int.Parse(osVersion);

            return new DotNetInfo(
                installedClrVersions,
                installedDotNetVersions,
                osVersionMajor
            );
        }

        private static string GetOSVersion()
        {

            try
            {
                using (var wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT Version FROM Win32_OperatingSystem"))
                {
                    using (var data = wmiData.Get())
                    {
                        foreach (var os in data)
                        {
                            return os["Version"].ToString();
                        }
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        private static IEnumerable<string> GetClrVersions()
        {
            var dirs = Directory.EnumerateDirectories("\\Windows\\Microsoft.Net\\Framework\\");

            return (from dir in dirs
                    where File.Exists($"{dir}\\System.dll")
                    select Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar))
                    into fileName
                    select fileName.TrimStart('v')).ToList();
        }
    }
}
