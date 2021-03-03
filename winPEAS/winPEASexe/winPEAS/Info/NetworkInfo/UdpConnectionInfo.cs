using System.Net;
using System.Runtime.InteropServices;
using winPEAS.Info.NetworkInfo.Enums;

namespace winPEAS.Info.NetworkInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public class UdpConnectionInfo : NetworkConnection
    {
        public UdpConnectionInfo(Protocol protocol, IPAddress localAddress, ushort localPort, int pId, string processName)
             : base(protocol, localAddress, localPort, pId, processName)
        {
        }
    }
}
