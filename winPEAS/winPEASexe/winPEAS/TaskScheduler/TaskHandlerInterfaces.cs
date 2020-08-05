using System.Runtime.InteropServices;

namespace Microsoft.Win32.TaskScheduler
{


	/// <summary>
	/// Provides the methods that are used by COM handlers to notify the Task Scheduler about the status of the handler.
	/// </summary>
	[ComImport, Guid("EAEC7A8F-27A0-4DDC-8675-14726A01A38A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), System.Security.SuppressUnmanagedCodeSecurity]
	public interface ITaskHandlerStatus
	{
		/// <summary>
		/// Tells the Task Scheduler about the percentage of completion of the COM handler.
		/// </summary>
		/// <param name="percentComplete">A value that indicates the percentage of completion for the COM handler.</param>
		/// <param name="statusMessage">The message that is displayed in the Task Scheduler UI.</param>
		void UpdateStatus([In] short percentComplete, [In, MarshalAs(UnmanagedType.BStr)] string statusMessage);
		/// <summary>
		/// Tells the Task Scheduler that the COM handler is completed.
		/// </summary>
		/// <param name="taskErrCode">The error code that the Task Scheduler will raise as an event.</param>
		void TaskCompleted([In, MarshalAs(UnmanagedType.Error)] int taskErrCode);
	}
}
