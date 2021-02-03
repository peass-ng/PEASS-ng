using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Native;

namespace winPEAS.Info.ApplicationInfo
{
    internal static class DeviceDrivers
    {
        public static Dictionary<string, FileVersionInfo> GetDeviceDriversNoMicrosoft()
        {
            Dictionary<string, FileVersionInfo> results = new Dictionary<string, FileVersionInfo>();

            // ignore ghosts
            // https://devblogs.microsoft.com/oldnewthing/20160913-00/?p=94305
            Regex ignoreGhosts = new Regex("^dump_", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // manufacturer/providers to ignore
            Regex ignoreCompany = new Regex("^Microsoft", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            string system32 = Environment.SystemDirectory;

            // Get a list of loaded kernel modules
            Psapi.EnumDeviceDrivers(null, 0, out var neededBytes);
            UIntPtr[] drivers = new UIntPtr[neededBytes / UIntPtr.Size];
            Psapi.EnumDeviceDrivers(drivers, (UInt32)(drivers.Length * UIntPtr.Size), out neededBytes);

            // iterate over modules
            foreach (UIntPtr baseAddr in drivers)
            {
                StringBuilder buffer = new StringBuilder(1024);
                Psapi.GetDeviceDriverBaseName(baseAddr, buffer, (UInt32)buffer.Capacity);
                if (ignoreGhosts.IsMatch(buffer.ToString()))
                {
                    continue;
                }
                Psapi.GetDeviceDriverFileName(baseAddr, buffer, (UInt32)buffer.Capacity);
                string pathname = buffer.ToString();

                // GetDeviceDriverFileName can return a path in a various number of formats, below code tries to handle them.
                // https://community.osr.com/discussion/228671/querying-device-driver-list-from-kernel-mode
                if (pathname.StartsWith("\\??\\"))
                {
                    pathname = pathname.Remove(0, 4);
                }

                if (File.Exists(pathname))
                {
                    // intentionally empty
                }
                else if (pathname[0] == '\\')
                {
                    // path could be either in the NtObject namespace or from the filesystem root (without drive)
                    if (File.Exists("\\\\.\\GLOBALROOT" + pathname))
                    {
                        pathname = "\\\\.\\GLOBALROOT" + pathname;
                    }
                    else if (File.Exists(system32.Substring(0, 2) + pathname))
                    {
                        pathname = system32.Substring(0, 2) + pathname;
                    }
                    else
                    {
                        Beaprint.GrayPrint($"Ignoring unknown path {pathname}");
                        continue;
                    }
                }
                else
                {
                    // probably module is a boot driver without a full path
                    if (File.Exists(system32 + "\\drivers\\" + pathname))
                    {
                        pathname = system32 + "\\drivers\\" + pathname;
                    }
                    else if (File.Exists(system32 + "\\" + pathname))
                    {
                        pathname = system32 + "\\" + pathname;
                    }
                    else
                    {
                        Beaprint.GrayPrint($"Ignoring unknown path {pathname}");
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(pathname))
                {
                    var info = FileVersionInfo.GetVersionInfo(pathname.ToString());

                    if (!string.IsNullOrEmpty(info.CompanyName) && !ignoreCompany.IsMatch(info.CompanyName))
                    {
                        results[pathname] = info;
                    }
                }
            }
            return results;
        }
    }
}
