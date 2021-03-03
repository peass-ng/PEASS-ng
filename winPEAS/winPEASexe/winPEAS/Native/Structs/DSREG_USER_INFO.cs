using System.Runtime.InteropServices;

namespace winPEAS.Native.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DSREG_USER_INFO
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string UserEmail;
        [MarshalAs(UnmanagedType.LPWStr)] public string UserKeyId;
        [MarshalAs(UnmanagedType.LPWStr)] public string UserKeyName;
    }
}
