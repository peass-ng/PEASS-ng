using System;
using System.ComponentModel;

namespace winPEAS.Info.SystemInfo.SysMon
{
    [Flags]
    public enum SysmonOptions
    {
        [Description("Not Defined")]
        NotDefined = 0,

        [Description("Network Connection")]
        NetworkConnection = 1,

        [Description("Image Loading")]
        ImageLoading = 2
    }
}
