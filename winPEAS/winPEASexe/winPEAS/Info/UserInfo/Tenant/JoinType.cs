using System.ComponentModel;

namespace winPEAS.Info.UserInfo.Tenant
{
    public enum JoinType
    {
        [Description("Unknown Join")]
        DSREG_UNKNOWN_JOIN,

        [Description("Device Join")]
        DSREG_DEVICE_JOIN,

        [Description("Workplace Join")]
        DSREG_WORKPLACE_JOIN,

        [Description("No Join")]
        DSREG_NO_JOIN
    }
}
