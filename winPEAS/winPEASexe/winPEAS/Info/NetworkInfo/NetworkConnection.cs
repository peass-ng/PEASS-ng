using System.Net;
using System.Runtime.InteropServices;
using winPEAS.Info.NetworkInfo.Enums;

namespace winPEAS.Info.NetworkInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public abstract class NetworkConnection
    {
        public virtual Protocol Protocol { get; set; }
        public virtual IPAddress LocalAddress { get; set; }
        public virtual ushort LocalPort { get; set; }
        public virtual int ProcessId { get; set; }
        public virtual string ProcessName { get; set; }

        public NetworkConnection(
            Protocol protocol,
            IPAddress localAddress,
            ushort localPort,
            int processId,
            string processName)
        {
            Protocol = protocol;
            LocalAddress = localAddress;
            LocalPort = localPort;
            ProcessId = processId;
            ProcessName = processName;
        }
    }
}
