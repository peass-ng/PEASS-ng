using System;
using System.Runtime.InteropServices;

namespace winPEAS.Native
{
    internal class Ntdll
    {
        [DllImport("ntdll.dll")]
        public static extern int NtQueryObject(IntPtr ObjectHandle, int ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, ref int returnLength);

        [DllImport("ntdll.dll")]
        public static extern uint NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, ref int returnLength);

        [DllImport("ntdll.dll")]
        internal static extern int NtQueryInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass, IntPtr[] ProcessInformation, int ProcessInformationLength, ref int ReturnLength);

        [DllImport("ntdll.dll")]
        internal static extern int NtQueryInformationThread(IntPtr hThread, int ThreadInformationClass, IntPtr[] ThreadInformation, int ThreadInformationLength, ref int ReturnLength);

        [DllImport("ntdll.dll")]
        internal static extern int NtDuplicateObject(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, ulong dwOptions);

        [DllImport("ntdll.dll")]
        public static extern uint NtQueryKey(IntPtr KeyHandle, int KeyInformationClass, byte[] KeyInformation, int Length, ref int ResultLength);
    }
}
