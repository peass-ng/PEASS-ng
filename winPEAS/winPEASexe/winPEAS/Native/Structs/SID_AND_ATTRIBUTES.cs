using System;
using System.Runtime.InteropServices;

namespace winPEAS.Native.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public UInt32 Attributes;
    }
}
