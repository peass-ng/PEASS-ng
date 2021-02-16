
using System;
using System.Runtime.InteropServices;
using winPEAS.Native.Enums;

namespace winPEAS.Native
{
    internal class Wtsapi32
    {
        [DllImport("wtsapi32.dll")]
        internal static extern void WTSCloseServer(IntPtr hServer);


        [DllImport("Wtsapi32.dll", SetLastError = true)]
        internal static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            uint sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out uint pBytesReturned
        );

        [DllImport("wtsapi32.dll", SetLastError = true)]
        internal static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        internal static extern Int32 WTSEnumerateSessionsEx(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pLevel,
            [MarshalAs(UnmanagedType.U4)] Int32 Filter,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        [DllImport("wtsapi32.dll")]
        internal static extern void WTSFreeMemory(IntPtr pMemory);
    }
}
