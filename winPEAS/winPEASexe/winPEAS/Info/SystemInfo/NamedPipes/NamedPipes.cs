using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace winPEAS.Info.SystemInfo.NamedPipes
{
    internal class NamedPipes
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA
           lpFindFileData);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FindClose(IntPtr hFindFile);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        public static IEnumerable<NamedPipeInfo> GetNamedPipeInfos()
        {
            var namedPipes = new List<string>();

            var ptr = FindFirstFile(@"\\.\pipe\*", out var lpFindFileData);
            namedPipes.Add(lpFindFileData.cFileName);
            while (FindNextFile(ptr, out lpFindFileData))
            {
                namedPipes.Add(lpFindFileData.cFileName);
            }
            FindClose(ptr);

            namedPipes.Sort();

            foreach (var namedPipe in namedPipes)
            {
                string sddl;
                bool isError = false;

                try
                {
                    var security = File.GetAccessControl($"\\\\.\\pipe\\{namedPipe}");
                    sddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                }
                catch
                {
                    isError = true;
                    sddl = "ERROR";
                }

                if (!isError && !string.IsNullOrEmpty(sddl))
                {
                    yield return new NamedPipeInfo(namedPipe, sddl);
                }
            }
        }
    }
}
