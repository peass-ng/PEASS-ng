using System;
using System.Runtime.InteropServices;
using winPEAS.Info.NetworkInfo.Enums;

namespace winPEAS.Native
{
    internal class Iphlpapi
    {
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize,
            bool bOrder, int ulAf, TcpTableClass tableClass, uint reserved = 0);

        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize,
            bool bOrder, int ulAf, UdpTableClass tableClass, uint reserved = 0);

        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int GetIpNetTable(IntPtr pIpNetTable, [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);

        [DllImport("IpHlpApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int FreeMibTable(IntPtr plpNetTable);
    }
}
