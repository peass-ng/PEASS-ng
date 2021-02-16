using System;
using System.Runtime.InteropServices;

namespace winPEAS.Info.UserInfo.Token
{
    public struct TOKEN_PRIVILEGES
    {
        public UInt32 PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public UInt32 Attributes;
    }
}
