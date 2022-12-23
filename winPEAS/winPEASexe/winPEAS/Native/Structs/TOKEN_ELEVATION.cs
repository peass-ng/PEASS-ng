using System;
using System.Runtime.InteropServices;

namespace winPEAS.Native.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_ELEVATION
    {
        public Int32 TokenIsElevated;
    }
}
