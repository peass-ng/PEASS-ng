using System.Runtime.InteropServices;

namespace winPEAS.Native.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;

        public static LUID FromName(string name, string systemName = null)
        {
            LUID val;
            if (!Advapi32.LookupPrivilegeValue(systemName, name, out val))
                throw new System.ComponentModel.Win32Exception();
            return val;
        }
    }
}
