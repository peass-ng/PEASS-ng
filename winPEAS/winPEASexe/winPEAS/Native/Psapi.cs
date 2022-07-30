using System;
using System.Runtime.InteropServices;
using System.Text;

namespace winPEAS.Native
{
    internal class Psapi
    {
        [DllImport("psapi")]
        internal static extern bool EnumDeviceDrivers(
            UIntPtr[] driversList,
            UInt32 arraySizeBytes,
            out UInt32 bytesNeeded
        );

        [DllImport("psapi")]
        internal static extern int GetDeviceDriverFileName(
            UIntPtr baseAddr,
            StringBuilder name,
            UInt32 nameSize
        );

        [DllImport("psapi")]
        internal static extern int GetDeviceDriverBaseName(
            UIntPtr baseAddr,
            StringBuilder name,
            UInt32 nameSize
        );

        [DllImport("psapi.dll")]
        internal static extern uint GetProcessImageFileName(
            IntPtr hProcess,
            [Out] StringBuilder lpImageFileName,
            [In][MarshalAs(UnmanagedType.U4)] int nSize
        );
    }
}
