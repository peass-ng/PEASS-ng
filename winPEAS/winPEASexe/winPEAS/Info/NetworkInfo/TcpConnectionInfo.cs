using System.Net;
using System.Runtime.InteropServices;
using winPEAS.Info.NetworkInfo.Enums;

namespace winPEAS.Info.NetworkInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public class TcpConnectionInfo : NetworkConnection
    {
        public IPAddress RemoteAddress { get; set; }
        public ushort RemotePort { get; set; }
        public MibTcpState State { get; set; }

        public TcpConnectionInfo(Protocol protocol, IPAddress localAddress, IPAddress remoteIp, ushort localPort,
            ushort remotePort, int pId, MibTcpState state, string processName)
            : base(protocol, localAddress, localPort, pId, processName)
        {
            RemoteAddress = remoteIp;
            RemotePort = remotePort;
            State = state;
        }
    }
}
