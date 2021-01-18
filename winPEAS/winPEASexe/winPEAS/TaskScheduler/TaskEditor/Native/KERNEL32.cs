using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace winPEAS.TaskScheduler.TaskEditor.Native
{
	internal static partial class NativeMethods
	{
		const string KERNEL32 = "Kernel32.dll";

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport(KERNEL32, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr handle);

		[DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetCurrentProcess();

		[DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetCurrentThread();

		/// <summary>
		/// The GlobalLock function locks a global memory object and returns a pointer to the first byte of the object's memory block.
		/// GlobalLock function increments the lock count by one.
		/// Needed for the clipboard functions when getting the data from IDataObject
		/// </summary>
		/// <param name="hMem"></param>
		/// <returns></returns>
		[DllImport(KERNEL32, SetLastError = true)]
		public static extern IntPtr GlobalLock(IntPtr hMem);

		/// <summary>
		/// The GlobalUnlock function decrements the lock count associated with a memory object.
		/// </summary>
		/// <param name="hMem"></param>
		/// <returns></returns>
		[DllImport(KERNEL32, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GlobalUnlock(IntPtr hMem);

		[DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr LoadLibrary(string filename);

		[DllImport(KERNEL32, CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FreeLibrary(IntPtr lib);

		public partial class SafeTokenHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
		{
			private SafeTokenHandle() : base(true) { }

			internal SafeTokenHandle(IntPtr handle, bool own = true) : base(own)
			{
				SetHandle(handle);
			}

			protected override bool ReleaseHandle() => CloseHandle(handle);
		}
	}
}
