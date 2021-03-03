using System;
using System.Collections.Generic;
using System.Linq;

namespace winPEAS.Info.SystemInfo.DotNet
{
    internal class DotNetInfo
    {
        public const int AmsiSupportedByDotNetMinMajorVersion = 4;
        public const int AmsiSupportedByDotNetMinMinorVersion = 8;

        private const int AmsiSupportedByOsMinVersion = 10;

        public IEnumerable<string> ClrVersions { get; }
        public IEnumerable<string> DotNetVersions { get; }
        public bool IsAmsiSupportedByOs { get; }
        public bool IsAmsiSupportedByDotNet { get; }
        public Version LowestVersion { get; set; }
        public Version HighestVersion { get; set; }

        public DotNetInfo(
            IEnumerable<string> installedClrVersions,
            IEnumerable<string> installedDotNetVersions,
            int osVersionMajor)
        {
            ClrVersions = (installedClrVersions ?? new List<string>()).ToList();
            DotNetVersions = (installedDotNetVersions ?? new List<string>()).ToList(); ;
            IsAmsiSupportedByOs = osVersionMajor >= AmsiSupportedByOsMinVersion;

            LowestVersion = DotNetVersions.Min(v => (new Version(v)));
            HighestVersion = DotNetVersions.Max(v => (new Version(v)));

            IsAmsiSupportedByDotNet = (HighestVersion.Major >= AmsiSupportedByDotNetMinMajorVersion) &&
                                      (LowestVersion.Minor >= AmsiSupportedByDotNetMinMinorVersion);
        }
    }
}
