using System;
using System.Runtime.InteropServices;

namespace winPEAS.Native.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PRIVILEGE_SET : IDisposable
    {
        public uint PrivilegeCount;
        public uint Control;
        public IntPtr Privilege;

        public PRIVILEGE_SET(uint control, params LUID_AND_ATTRIBUTES[] privileges)
        {
            PrivilegeCount = (uint)privileges.Length;
            Control = control;
            Privilege = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(LUID_AND_ATTRIBUTES)) * (int)PrivilegeCount);
            for (int i = 0; i < PrivilegeCount; i++)
                Marshal.StructureToPtr(privileges[i], (IntPtr)((int)Privilege + (Marshal.SizeOf(typeof(LUID_AND_ATTRIBUTES)) * i)), false);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(Privilege);
        }
    }
}
