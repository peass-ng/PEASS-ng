using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	internal static partial class NativeMethods
	{
		const string ADVAPI32 = "advapi32.dll";

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail), DllImport(ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]

		public static extern int LogonUser(string lpszUserName, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

    
		public partial class SafeTokenHandle
		{
			private const Int32 ERROR_NO_TOKEN = 0x000003F0;
			private const Int32 ERROR_INSUFFICIENT_BUFFER = 122;
			private static SafeTokenHandle currentProcessToken = null;
            }
	}
}
