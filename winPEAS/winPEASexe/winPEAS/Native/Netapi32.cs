using System;
using System.Runtime.InteropServices;
using System.Security;
using winPEAS.Native.Enums;

namespace winPEAS.Native
{
    internal class Netapi32
    {
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

        //[DllImport("Netapi32.dll")]
        //internal static extern int NetApiBufferFree(IntPtr Buffer);

        [DllImport("Netapi32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int NetServerGetInfo(string serverName, int level, out IntPtr pSERVER_INFO_XXX);

        [DllImport("Netapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurity]
        internal static extern int NetServerEnum(
            [MarshalAs(UnmanagedType.LPWStr)] string servernane, // must be null
            int level,
            out IntPtr bufptr,
            int prefmaxlen,
            out int entriesread,
            out int totalentries,
            ServerTypes servertype,
            [MarshalAs(UnmanagedType.LPWStr)] string domain, // null for login domain
            IntPtr resume_handle // Must be IntPtr.Zero
        );

        [DllImport("Netapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
        internal static extern int NetApiBufferFree(IntPtr pBuf);

        [DllImport("Netapi32.dll")]
        internal static extern uint NetUserEnum(
            [MarshalAs(UnmanagedType.LPWStr)] string serverName,
            uint level,
            uint filter,
            out IntPtr bufPtr,
            uint preferredMaxLength,
            out uint entriesRead,
            out uint totalEntries,
            out IntPtr resumeHandle);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern void NetFreeAadJoinInformation(IntPtr pJoinInfo);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int NetGetAadJoinInformation(string pcszTenantId, out IntPtr ppJoinInfo);
    }
}
