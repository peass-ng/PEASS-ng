using System;

namespace winPEAS.Native.Enums
{
    [Flags]
    public enum PrivilegeAttributes : uint
    {
        Disabled = 0x00000000,
        EnabledByDefault = 0x00000001,
        Enabled = 0x00000002,
        UsedForAccess = 0x80000000,
    }
}
