using System.Runtime.InteropServices;
using winPEAS.Info.NetworkInfo.Enums;

namespace winPEAS.Info.NetworkInfo.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public MibTcpState state;
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public uint remoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] remotePort;
        public int owningPid;
    }
}
