using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace winPEAS.Wifi.NativeWifiApi
{
    public static class Wlan
    {
        #region P/Invoke API
 
        public const uint WLAN_CLIENT_VERSION_XP_SP2 = 1;
        public const uint WLAN_CLIENT_VERSION_LONGHORN = 2;
        public const uint WLAN_MAX_NAME_LENGTH = 256;

        [DllImport("wlanapi.dll")]
        public static extern int WlanOpenHandle(
            [In] UInt32 clientVersion,
            [In, Out] IntPtr pReserved,
            [Out] out UInt32 negotiatedVersion,
            [Out] out IntPtr clientHandle);

        [DllImport("wlanapi.dll")]
        public static extern int WlanCloseHandle(
            [In] IntPtr clientHandle,
            [In, Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern int WlanEnumInterfaces(
            [In] IntPtr clientHandle,
            [In, Out] IntPtr pReserved,
            [Out] out IntPtr ppInterfaceList);             
       
        /// <param name="flags">Not supported on Windows XP SP2: must be a <c>null</c> reference.</param>
        [DllImport("wlanapi.dll")]
        public static extern int WlanGetProfile(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In, MarshalAs(UnmanagedType.LPWStr)] string profileName,
            [In] IntPtr pReserved,
            [Out] out IntPtr profileXml,
            [Out, Optional] out WlanProfileFlags flags,
            [Out, Optional] out WlanAccess grantedAccess);

        [DllImport("wlanapi.dll")]
        public static extern int WlanGetProfileList(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] IntPtr pReserved,
            [Out] out IntPtr profileList
        );

        [DllImport("wlanapi.dll")]
        public static extern void WlanFreeMemory(IntPtr pMemory);                                       

        [DllImport("wlanapi.dll")]
        public static extern int WlanConnect(
            [In] IntPtr clientHandle,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] ref WlanConnectionParameters connectionParameters,
            IntPtr pReserved);   
  
        #endregion

        /// <summary>
        /// Helper method to wrap calls to Native WiFi API methods.
        /// If the method falls, throws an exception containing the error code.
        /// </summary>
        /// <param name="win32ErrorCode">The error code.</param>
        [DebuggerStepThrough]
        internal static void ThrowIfError(int win32ErrorCode)
        {
            if (win32ErrorCode != 0)
                throw new Win32Exception(win32ErrorCode);
        }
    }
}
