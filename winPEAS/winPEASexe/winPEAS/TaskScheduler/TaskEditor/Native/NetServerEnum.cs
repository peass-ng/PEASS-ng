using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using winPEAS.Native;
using winPEAS.Native.Enums;

namespace winPEAS.TaskScheduler.TaskEditor.Native
{
    internal static partial class NativeMethods
    {
        const int MAX_PREFERRED_LENGTH = -1;

        public enum ServerPlatform
        {
            DOS = 300,
            OS2 = 400,
            NT = 500,
            OSF = 600,
            VMS = 700
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVER_INFO_100
        {
            public ServerPlatform PlatformId;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVER_INFO_101
        {
            public ServerPlatform PlatformId;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
            public int VersionMajor;
            public int VersionMinor;
            public ServerTypes Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVER_INFO_102
        {
            public ServerPlatform PlatformId;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
            public int VersionMajor;
            public int VersionMinor;
            public ServerTypes Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            public int MaxUsers;
            public int AutoDisconnectMinutes;
            [MarshalAs(UnmanagedType.Bool)]
            public bool Hidden;
            public int NetworkAnnounceRate;
            public int NetworkAnnounceRateDelta;
            public int UsersPerLicense;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserDirectoryPath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NetworkComputerInfo // SERVER_INFO_101
        {
            ServerPlatform sv101_platform_id;
            [MarshalAs(UnmanagedType.LPWStr)]
            string sv101_name;
            int sv101_version_major;
            int sv101_version_minor;
            ServerTypes sv101_type;
            [MarshalAs(UnmanagedType.LPWStr)]
            string sv101_comment;

            public ServerPlatform Platform => sv101_platform_id;
            public string Name => sv101_name;
            public string Comment => sv101_comment;
            public ServerTypes ServerTypes => sv101_type;
            public Version Version => new Version(sv101_version_major, sv101_version_minor);
        };

        public static IEnumerable<string> GetNetworkComputerNames(ServerTypes serverTypes = ServerTypes.Workstation | ServerTypes.Server, string domain = null) =>
            Array.ConvertAll(NetServerEnum<SERVER_INFO_100>(serverTypes, domain), si => si.Name);

        public static IEnumerable<NetworkComputerInfo> GetNetworkComputerInfo(ServerTypes serverTypes = ServerTypes.Workstation | ServerTypes.Server, string domain = null) =>
            NetServerEnum<NetworkComputerInfo>(serverTypes, domain, 101);

        public static T[] NetServerEnum<T>(ServerTypes serverTypes = ServerTypes.Workstation | ServerTypes.Server, string domain = null, int level = 0) where T : struct
        {
            if (level == 0)
                level = int.Parse(System.Text.RegularExpressions.Regex.Replace(typeof(T).Name, @"[^\d]", ""));

            IntPtr bufptr = IntPtr.Zero;
            try
            {
                int entriesRead, totalEntries;
                IntPtr resumeHandle = IntPtr.Zero;

                int ret = Netapi32.NetServerEnum(null, level, out bufptr, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, serverTypes, domain, resumeHandle);
                if (ret == 0)
                    return InteropUtil.ToArray<T>(bufptr, entriesRead);
                throw new System.ComponentModel.Win32Exception(ret);
            }
            finally
            {
                Netapi32.NetApiBufferFree(bufptr);
            }
        }

        public static T NetServerGetInfo<T>(string serverName, int level = 0) where T : struct
        {
            if (level == 0)
                level = int.Parse(System.Text.RegularExpressions.Regex.Replace(typeof(T).Name, @"[^\d]", ""));

            IntPtr ptr = IntPtr.Zero;
            try
            {
                int ret = Netapi32.NetServerGetInfo(serverName, level, out ptr);
                if (ret != 0)
                {
                    throw new System.ComponentModel.Win32Exception(ret);
                }

                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Netapi32.NetApiBufferFree(ptr);
                }
            }
        }
    }
}
