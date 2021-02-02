using System.Net;
using System.Runtime.InteropServices;
using winPEAS.Info.NetworkInfo.Enums;

namespace winPEAS.Info.NetworkInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public abstract class NetworkConnection
    {
        public Protocol Protocol { get; set; }
        public IPAddress LocalAddress { get; set; }
        public ushort LocalPort { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }

        protected NetworkConnection(
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
