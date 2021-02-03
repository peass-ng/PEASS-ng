using System;
using System.Runtime.InteropServices;
using winPEAS.Info.UserInfo.SAM;
using winPEAS.Native.Classes;

namespace winPEAS.Native
{
    internal class Samlib
    {
        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamConnect(UNICODE_STRING ServerName, out IntPtr ServerHandle, SERVER_ACCESS_MASK DesiredAccess, IntPtr ObjectAttributes);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamCloseHandle(IntPtr ServerHandle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamFreeMemory(IntPtr Handle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamOpenDomain(IntPtr ServerHandle, DOMAIN_ACCESS_MASK DesiredAccess, byte[] DomainId, out IntPtr DomainHandle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamLookupDomainInSamServer(IntPtr ServerHandle, UNICODE_STRING name, out IntPtr DomainId);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamQueryInformationDomain(IntPtr DomainHandle, DOMAIN_INFORMATION_CLASS DomainInformationClass, out IntPtr Buffer);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamSetInformationDomain(IntPtr DomainHandle, DOMAIN_INFORMATION_CLASS DomainInformationClass, IntPtr Buffer);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS SamEnumerateDomainsInSamServer(IntPtr ServerHandle, ref int EnumerationContext, out IntPtr EnumerationBuffer, int PreferedMaximumLength, out int CountReturned);
    }
}
