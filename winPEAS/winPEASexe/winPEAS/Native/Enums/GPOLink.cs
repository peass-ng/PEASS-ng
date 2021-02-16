using System.ComponentModel;

namespace winPEAS.Native.Enums
{
    enum GPOLink
    {
        [Description("No Link Information")]
        NO_LINK_INFORMATION = 0,

        [Description("Local Machine")]
        LOCAL_MACHINE = 1,

        [Description("Site")]
        SITE = 2,

        [Description("Domain")]
        DOMAIN = 3,

        [Description("Organizational Unit")]
        ORGANIZATIONAL_UNIT = 4
    }
}
