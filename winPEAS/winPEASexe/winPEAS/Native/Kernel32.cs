using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using winPEAS.Info.SystemInfo.NamedPipes;

namespace winPEAS.Native
{
    internal class Kernel32
    {
        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr FindFirstFile(string lpFileName, out NamedPipes.WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool FindNextFile(IntPtr hFindFile, out NamedPipes.WIN32_FIND_DATA
            lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr LoadLibrary(string dllFilePath);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);


        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetCurrentThread();

        /// <summary>
        /// The GlobalLock function locks a global memory object and returns a pointer to the first byte of the object's memory block.
        /// GlobalLock function increments the lock count by one.
        /// Needed for the clipboard functions when getting the data from IDataObject
        /// </summary>
        /// <param name="hMem"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GlobalLock(IntPtr hMem);

        /// <summary>
        /// The GlobalUnlock function decrements the lock count associated with a memory object.
        /// </summary>
        /// <param name="hMem"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr lib);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetPrivateProfileSection(string section, IntPtr keyValue, int size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetPrivateProfileString(string? section, string? key, string defaultValue, [In, Out] char[] value, int size, string filePath);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(Helpers.HandlesHelper.ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(Helpers.HandlesHelper.ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, UIntPtr dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, ushort hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetProcessIdOfThread(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetProcessId(IntPtr handle);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetFileInformationByHandleEx(IntPtr hFile, int infoClass, out Helpers.HandlesHelper.FILE_NAME_INFO lpFileInformation, uint dwBufferSize);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern unsafe int GetFullPathName(string lpFileName, int nBufferLength, [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpBuffer, [Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpFilePart);

    }
}
