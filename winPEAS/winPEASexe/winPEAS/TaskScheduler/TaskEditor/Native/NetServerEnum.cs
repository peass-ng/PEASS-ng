using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace winPEAS.TaskScheduler.TaskEditor.Native
{
	internal static partial class NativeMethods
	{
		const int MAX_PREFERRED_LENGTH = -1;

		[Flags]
		public enum ServerTypes : uint
		{
			Workstation = 0x00000001,
			Server = 0x00000002,
			SqlServer = 0x00000004,
			DomainCtrl = 0x00000008,
			BackupDomainCtrl = 0x00000010,
			TimeSource = 0x00000020,
			AppleFilingProtocol = 0x00000040,
			Novell = 0x00000080,
			DomainMember = 0x00000100,
			PrintQueueServer = 0x00000200,
			DialinServer = 0x00000400,
			XenixServer = 0x00000800,
			UnixServer = 0x00000800,
			NT = 0x00001000,
			WindowsForWorkgroups = 0x00002000,
			MicrosoftFileAndPrintServer = 0x00004000,
			NTServer = 0x00008000,
			BrowserService = 0x00010000,
			BackupBrowserService = 0x00020000,
			MasterBrowserService = 0x00040000,
			DomainMaster = 0x00080000,
			OSF1Server = 0x00100000,
			VMSServer = 0x00200000,
			Windows = 0x00400000,
			DFS = 0x00800000,
			NTCluster = 0x01000000,
			TerminalServer = 0x02000000,
			VirtualNTCluster = 0x04000000,
			DCE = 0x10000000,
			AlternateTransport = 0x20000000,
			LocalListOnly = 0x40000000,
			PrimaryDomain = 0x80000000,
			All = 0xFFFFFFFF
		};

		public enum ServerPlatform
		{
			DOS = 300,
			OS2 = 400,
			NT = 500,
			OSF = 600,
			VMS = 700
		}

		[DllImport("Netapi32", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int NetServerGetInfo(string serverName, int level, out IntPtr pSERVER_INFO_XXX);

		[DllImport("Netapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
		private static extern int NetServerEnum(
			[MarshalAs(UnmanagedType.LPWStr)] string servernane, // must be null
			int level,
			out IntPtr bufptr,
			int prefmaxlen,
			out int entriesread,
			out int totalentries,
			ServerTypes servertype,
			[MarshalAs(UnmanagedType.LPWStr)] string domain, // null for login domain
			IntPtr resume_handle // Must be IntPtr.Zero
			);

		[DllImport("Netapi32", SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
		private static extern int NetApiBufferFree(IntPtr pBuf);

		[StructLayout(LayoutKind.Sequential)]
		public struct SERVER_INFO_100
		{
			public ServerPlatform PlatformId;
			[MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
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
			[MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
			string sv101_name;
			int sv101_version_major;
			int sv101_version_minor;
			ServerTypes sv101_type;
			[MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
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

				int ret = NetServerEnum(null, level, out bufptr, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, serverTypes, domain, resumeHandle);
				if (ret == 0)
					return InteropUtil.ToArray<T>(bufptr, entriesRead);
				throw new System.ComponentModel.Win32Exception(ret);
			}
			finally
			{
				NetApiBufferFree(bufptr);
			}
		}

		public static T NetServerGetInfo<T>(string serverName, int level = 0) where T : struct
		{
			if (level == 0)
				level = int.Parse(System.Text.RegularExpressions.Regex.Replace(typeof(T).Name, @"[^\d]", ""));

			IntPtr ptr = IntPtr.Zero;
			try
			{
				int ret = NetServerGetInfo(serverName, level, out ptr);
				if (ret != 0)
					throw new System.ComponentModel.Win32Exception(ret);
				return (T)Marshal.PtrToStructure(ptr, typeof(T));
			}
			finally
			{
				if (ptr != IntPtr.Zero)
					NetApiBufferFree(ptr);
			}
		}
	}
}
