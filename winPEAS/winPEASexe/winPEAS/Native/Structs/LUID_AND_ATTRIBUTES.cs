using System.Runtime.InteropServices;
using winPEAS.Native.Enums;

namespace winPEAS.Native.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public PrivilegeAttributes Attributes;

        public LUID_AND_ATTRIBUTES(LUID luid, PrivilegeAttributes attr)
        {
            Luid = luid;
            Attributes = attr;
        }
    }
}
