using System.ComponentModel;

namespace winPEAS.Info.NetworkInfo.Enums
{
    public enum MibTcpState
    {
        [Description("None")]
        NONE = 0,

        [Description("Closed")]
        CLOSED = 1,

        [Description("Listening")]
        LISTEN = 2,

        [Description("SYN Sent")]
        SYN_SENT = 3,

        [Description("SYN Received")]
        SYN_RCVD = 4,

        [Description("Established")]
        ESTAB = 5,

        [Description("FIN Wait 1")]
        FIN_WAIT1 = 6,

        [Description("FIN Wait 2")]
        FIN_WAIT2 = 7,

        [Description("Close Wait")]
        CLOSE_WAIT = 8,

        [Description("Closing")]
        CLOSING = 9,

        [Description("Last ACK")]
        LAST_ACK = 10,

        [Description("Time Wait")]
        TIME_WAIT = 11,

        [Description("Delete TCB")]
        DELETE_TCB = 12
    }
}
