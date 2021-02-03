using System;
using System.Runtime.InteropServices;
using winPEAS.Wifi.NativeWifiApi;

namespace winPEAS.Native
{
    internal class WlanApi
    {
        [DllImport("wlanapi.dll")]
        internal static extern int WlanOpenHandle(
            [In] UInt32 clientVersion,
            [In, Out] IntPtr pReserved,
            [Out] out UInt32 negotiatedVersion,
            [Out] out IntPtr clientHandle);

        [DllImport("wlanapi.dll")]
        internal static extern int WlanCloseHandle(
            [In] IntPtr clientHandle,
            [In, Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        internal static extern int WlanEnumInterfaces(
            [In] IntPtr clientHandle,
            [In, Out] IntPtr pReserved,
            [Out] out IntPtr ppInterfaceList);

        /// <param name="flags">Not supported on Windows XP SP2: must be a <c>null</c> reference.</param>
        [DllImport("wlanapi.dll")]
        internal static extern int WlanGetProfile(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In, MarshalAs(UnmanagedType.LPWStr)] string profileName,
            [In] IntPtr pReserved,
            [Out] out IntPtr profileXml,
            [Out, Optional] out WlanProfileFlags flags,
            [Out, Optional] out WlanAccess grantedAccess);

        [DllImport("wlanapi.dll")]
        internal static extern int WlanGetProfileList(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] IntPtr pReserved,
            [Out] out IntPtr profileList
        );

        [DllImport("wlanapi.dll")]
        internal static extern void WlanFreeMemory(IntPtr pMemory);

        [DllImport("wlanapi.dll")]
        internal static extern int WlanConnect(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] ref WlanConnectionParameters connectionParameters,
            IntPtr pReserved);
    }
}
