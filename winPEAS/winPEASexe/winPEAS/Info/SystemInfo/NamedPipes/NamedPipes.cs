using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using winPEAS.Native;


namespace winPEAS.Info.SystemInfo.NamedPipes
{
    internal class NamedPipes
    {
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

            var ptr = Kernel32.FindFirstFile(@"\\.\pipe\*", out var lpFindFileData);
            namedPipes.Add(lpFindFileData.cFileName);
            while (Kernel32.FindNextFile(ptr, out lpFindFileData))
            {
                namedPipes.Add(lpFindFileData.cFileName);
            }
            Kernel32.FindClose(ptr);

            namedPipes.Sort();

            foreach (var namedPipe in namedPipes)
            {
                string sddl;
                string currentUserPerms;
                bool isError = false;

                try
                {
                    var security = File.GetAccessControl($"\\\\.\\pipe\\{namedPipe}");
                    sddl = security.GetSecurityDescriptorSddlForm(AccessControlSections.All);
                    List<string> currentUserPermsList = Helpers.PermissionsHelper.GetMyPermissionsF(security, Checks.Checks.CurrentUserSiDs);
                    currentUserPerms = string.Join(", ", currentUserPermsList);
                }
                catch
                {
                    isError = true;
                    sddl = "ERROR";
                    currentUserPerms = "ERROR";
                }

                if (!isError && !string.IsNullOrEmpty(sddl))
                {
                    yield return new NamedPipeInfo(namedPipe, sddl, currentUserPerms);
                }
            }
        }
    }
}
