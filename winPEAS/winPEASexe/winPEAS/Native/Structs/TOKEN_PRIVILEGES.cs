using System.Runtime.InteropServices;
using winPEAS.Native.Enums;

namespace winPEAS.Native.Structs
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;

        public TOKEN_PRIVILEGES(LUID luid, PrivilegeAttributes attribute)
        {
            PrivilegeCount = 1;
            Privileges.Luid = luid;
            Privileges.Attributes = attribute;
        }

        public static uint SizeInBytes => (uint)Marshal.SizeOf(typeof(TOKEN_PRIVILEGES));
    }
}
