using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
	internal static partial class NativeMethods
	{
		const string KERNEL32 = "Kernel32.dll";

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport(KERNEL32, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr handle);

		public partial class SafeTokenHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
		{

			internal SafeTokenHandle(IntPtr handle, bool own = true) : base(own)
			{
				SetHandle(handle);
			}

			protected override bool ReleaseHandle() => CloseHandle(handle);
		}
	}
}
