using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Microsoft.Win32.TaskScheduler.V1Interop
{
	#region class HRESULT -- Values peculiar to the task scheduler.
	internal class HResult
	{
		// The task is ready to run at its next scheduled time.
		public const int SCHED_S_TASK_READY = 0x00041300;
		// The task is currently running.
		public const int SCHED_S_TASK_RUNNING = 0x00041301;
		// The task will not run at the scheduled times because it has been disabled.
		public const int SCHED_S_TASK_DISABLED = 0x00041302;
		// The task has not yet run.
		public const int SCHED_S_TASK_HAS_NOT_RUN = 0x00041303;
		// There are no more runs scheduled for this task.
		public const int SCHED_S_TASK_NO_MORE_RUNS = 0x00041304;
		// One or more of the properties that are needed to run this task on a schedule have not been set.
		public const int SCHED_S_TASK_NOT_SCHEDULED = 0x00041305;
		// The last run of the task was terminated by the user.
		public const int SCHED_S_TASK_TERMINATED = 0x00041306;
		// Either the task has no triggers or the existing triggers are disabled or not set.
		public const int SCHED_S_TASK_NO_VALID_TRIGGERS = 0x00041307;
		// Event triggers don't have set run times.
		public const int SCHED_S_EVENT_TRIGGER = 0x00041308;
		// Trigger not found.
		public const int SCHED_E_TRIGGER_NOT_FOUND = unchecked((int)0x80041309);
		// One or more of the properties that are needed to run this task have not been set.
		public const int SCHED_E_TASK_NOT_READY = unchecked((int)0x8004130A);
		// There is no running instance of the task to terminate.
		public const int SCHED_E_TASK_NOT_RUNNING = unchecked((int)0x8004130B);
		// The Task Scheduler Service is not installed on this computer.
		public const int SCHED_E_SERVICE_NOT_INSTALLED = unchecked((int)0x8004130C);
		// The task object could not be opened.
		public const int SCHED_E_CANNOT_OPEN_TASK = unchecked((int)0x8004130D);
		// The object is either an invalid task object or is not a task object.
		public const int SCHED_E_INVALID_TASK = unchecked((int)0x8004130E);
		// No account information could be found in the Task Scheduler security database for the task indicated.
		public const int SCHED_E_ACCOUNT_INFORMATION_NOT_SET = unchecked((int)0x8004130F);
		// Unable to establish existence of the account specified.
		public const int SCHED_E_ACCOUNT_NAME_NOT_FOUND = unchecked((int)0x80041310);
		// Corruption was detected in the Task Scheduler security database; the database has been reset.
		public const int SCHED_E_ACCOUNT_DBASE_CORRUPT = unchecked((int)0x80041311);
		// Task Scheduler security services are available only on Windows NT.
		public const int SCHED_E_NO_SECURITY_SERVICES = unchecked((int)0x80041312);
		// The task object version is either unsupported or invalid.
		public const int SCHED_E_UNKNOWN_OBJECT_VERSION = unchecked((int)0x80041313);
		// The task has been configured with an unsupported combination of account settings and run time options.
		public const int SCHED_E_UNSUPPORTED_ACCOUNT_OPTION = unchecked((int)0x80041314);
		// The Task Scheduler Service is not running.
		public const int SCHED_E_SERVICE_NOT_RUNNING = unchecked((int)0x80041315);
		// The Task Scheduler service must be configured to run in the System account to function properly.  Individual tasks may be configured to run in other accounts.
		public const int SCHED_E_SERVICE_NOT_LOCALSYSTEM = unchecked((int)0x80041316);
	}
	#endregion

	#region Enums

	/// <summary>
	/// Options for a task, used for the Flags property of a Task. Uses the
	/// "Flags" attribute, so these values are combined with |. 
	/// Some flags are documented as Windows 95 only, but they have a
	/// user interface in Windows XP so that may not be true.
	/// </summary>
	[Flags]
	internal enum TaskFlags
	{
		/// <summary>
		/// The interactive flag is set if the task is intended to be displayed to the user. 
		/// If the flag is not set, no user interface associated with the task is presented
		/// to the user when the task is executed.
		/// </summary>
		Interactive = 0x1,
		/// <summary>
		/// The task will be deleted when there are no more scheduled run times.
		/// </summary>
		DeleteWhenDone = 0x2,
		/// <summary>
		/// The task is disabled. This is useful to temporarily prevent a task from running
		/// at the scheduled time(s).
		/// </summary>
		Disabled = 0x4,
		/// <summary>
		/// The task begins only if the computer is not in use at the scheduled start time. Windows 95 only.
		/// </summary>
		StartOnlyIfIdle = 0x10,
		/// <summary>
		/// The task terminates if the computer makes an idle to non-idle transition while the task is running.
		/// The computer is not considered idle until the IdleWait triggers' time elapses with no user input.
		/// Windows 95 only. For information regarding idle triggers, see <see cref="IdleTrigger"/>.
		/// </summary>
		KillOnIdleEnd = 0x20,
		/// <summary>
		/// The task does not start if its target computer is running on battery power. Windows 95 only.
		/// </summary>
		DontStartIfOnBatteries = 0x40,
		/// <summary>
		/// The task ends, and the associated application quits if the task's target computer switches
		/// to battery power. Windows 95 only.
		/// </summary>
		KillIfGoingOnBatteries = 0x80,
		/// <summary>
		/// The task runs only if the system is docked. Windows 95 only.
		/// </summary>
		RunOnlyIfDocked = 0x100,
		/// <summary>
		/// The work item created will be hidden.
		/// </summary>
		Hidden = 0x200,
		/// <summary>
		/// The task runs only if there is currently a valid Internet connection.
		/// This feature is currently not implemented.
		/// </summary>
		RunIfConnectedToInternet = 0x400,
		/// <summary>
		/// The task starts again if the computer makes a non-idle to idle transition before all the
		/// task's task_triggers elapse. (Use this flag in conjunction with KillOnIdleEnd.) Windows 95 only.
		/// </summary>
		RestartOnIdleResume = 0x800,
		/// <summary>
		/// The task runs only if the SYSTEM account is available.
		/// </summary>
		SystemRequired = 0x1000,
		/// <summary>
		/// The task runs only if the user specified in SetAccountInformation is logged on interactively. 
		/// This flag has no effect on work items set to run in the local account.
		/// </summary>
		RunOnlyIfLoggedOn = 0x2000
	}

	/// <summary>
	/// Status values returned for a task.  Some values have been determined to occur although
	/// they do no appear in the Task Scheduler system documentation.
	/// </summary>
	internal enum TaskStatus
	{
		/// <summary>The task is ready to run at its next scheduled time.</summary>
		Ready = HResult.SCHED_S_TASK_READY,
		/// <summary>The task is currently running.</summary>
		Running = HResult.SCHED_S_TASK_RUNNING,
		/// <summary>One or more of the properties that are needed to run this task on a schedule have not been set. </summary>
		NotScheduled = HResult.SCHED_S_TASK_NOT_SCHEDULED,
		/// <summary>The task has not yet run.</summary>
		NeverRun = HResult.SCHED_S_TASK_HAS_NOT_RUN,
		/// <summary>The task will not run at the scheduled times because it has been disabled.</summary>
		Disabled = HResult.SCHED_S_TASK_DISABLED,
		/// <summary>There are no more runs scheduled for this task.</summary>
		NoMoreRuns = HResult.SCHED_S_TASK_NO_MORE_RUNS,
		/// <summary>The last run of the task was terminated by the user.</summary>
		Terminated = HResult.SCHED_S_TASK_TERMINATED,
		/// <summary>Either the task has no triggers or the existing triggers are disabled or not set.</summary>
		NoTriggers = HResult.SCHED_S_TASK_NO_VALID_TRIGGERS,
		/// <summary>Event triggers don't have set run times.</summary>
		NoTriggerTime = HResult.SCHED_S_EVENT_TRIGGER
	}

	/// <summary>Valid types of triggers</summary>
	internal enum TaskTriggerType
	{
		/// <summary>Trigger is set to run the task a single time. </summary>
		RunOnce = 0,
		/// <summary>Trigger is set to run the task on a daily interval. </summary>
		RunDaily = 1,
		/// <summary>Trigger is set to run the work item on specific days of a specific week of a specific month. </summary>
		RunWeekly = 2,
		/// <summary>Trigger is set to run the task on a specific day(s) of the month.</summary>
		RunMonthly = 3,
		/// <summary>Trigger is set to run the task on specific days, weeks, and months.</summary>
		RunMonthlyDOW = 4,
		/// <summary>Trigger is set to run the task if the system remains idle for the amount of time specified by the idle wait time of the task.</summary>
		OnIdle = 5,
		/// <summary>Trigger is set to run the task at system startup.</summary>
		OnSystemStart = 6,
		/// <summary>Trigger is set to run the task when a user logs on. </summary>
		OnLogon = 7
	}

	[Flags]
	internal enum TaskTriggerFlags : uint
	{
		HasEndDate = 0x1,
		KillAtDurationEnd = 0x2,
		Disabled = 0x4
	}

	#endregion

	#region Structs

	[StructLayout(LayoutKind.Sequential)]
	internal struct Daily
	{
		public ushort DaysInterval;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct Weekly
	{
		public ushort WeeksInterval;
		public DaysOfTheWeek DaysOfTheWeek;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MonthlyDate
	{
		public uint Days;
		public MonthsOfTheYear Months;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MonthlyDOW
	{
		public ushort WhichWeek;
		public DaysOfTheWeek DaysOfTheWeek;
		public MonthsOfTheYear Months;

		public WhichWeek V2WhichWeek
		{
			get
			{
				return (WhichWeek)(1 << ((short)WhichWeek - 1));
			}
			set
			{
				int idx = Array.IndexOf(new short[] { 0x1, 0x2, 0x4, 0x8, 0x10 }, (short)value);
				if (idx >= 0)
					WhichWeek = (ushort)(idx + 1);
				else
					throw new NotV1SupportedException("Only a single week can be set with Task Scheduler 1.0.");
			}
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct TriggerTypeData
	{
		[FieldOffset(0)]
		public Daily daily;
		[FieldOffset(0)]
		public Weekly weekly;
		[FieldOffset(0)]
		public MonthlyDate monthlyDate;
		[FieldOffset(0)]
		public MonthlyDOW monthlyDOW;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct TaskTrigger
	{
		public ushort TriggerSize;             // Structure size.
		public ushort Reserved1;               // Reserved. Must be zero.
		public ushort BeginYear;               // Trigger beginning date year.
		public ushort BeginMonth;              // Trigger beginning date month.
		public ushort BeginDay;                // Trigger beginning date day.
		public ushort EndYear;                 // Optional trigger ending date year.
		public ushort EndMonth;                // Optional trigger ending date month.
		public ushort EndDay;                  // Optional trigger ending date day.
		public ushort StartHour;               // Run bracket start time hour.
		public ushort StartMinute;             // Run bracket start time minute.
		public uint MinutesDuration;           // Duration of run bracket.
		public uint MinutesInterval;           // Run bracket repetition interval.
		public TaskTriggerFlags Flags;         // Trigger flags.
		public TaskTriggerType Type;           // Trigger type.
		public TriggerTypeData Data;           // Trigger data peculiar to this type (union).
		public ushort Reserved2;               // Reserved. Must be zero.
		public ushort RandomMinutesInterval;   // Maximum number of random minutes after start time.

		public DateTime BeginDate
		{
			get { try { return BeginYear == 0 ? DateTime.MinValue : new DateTime(BeginYear, BeginMonth, BeginDay, StartHour, StartMinute, 0, DateTimeKind.Unspecified); } catch { return DateTime.MinValue; } }
			set
			{
				if (value != DateTime.MinValue)
				{
					DateTime local = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
					BeginYear = (ushort)local.Year;
					BeginMonth = (ushort)local.Month;
					BeginDay = (ushort)local.Day;
					StartHour = (ushort)local.Hour;
					StartMinute = (ushort)local.Minute;
				}
				else
					BeginYear = BeginMonth = BeginDay = StartHour = StartMinute = 0;
			}
		}

		public DateTime? EndDate
		{
			get { try { return EndYear == 0 ? (DateTime?)null : new DateTime(EndYear, EndMonth, EndDay); } catch { return DateTime.MaxValue; } }
			set
			{
				if (value.HasValue)
				{
					EndYear = (ushort)value.Value.Year;
					EndMonth = (ushort)value.Value.Month;
					EndDay = (ushort)value.Value.Day;
					Flags |= TaskTriggerFlags.HasEndDate;
				}
				else
				{
					EndYear = EndMonth = EndDay = 0;
					Flags &= ~TaskTriggerFlags.HasEndDate;
				}
			}
		}

		public override string ToString() => $"Trigger Type: {Type};\n> Start: {BeginDate}; End: {(EndYear == 0 ? "null" : EndDate?.ToString())};\n> DurMin: {MinutesDuration}; DurItv: {MinutesInterval};\n>";
	}

	#endregion

	[ComImport, Guid("148BD527-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), System.Security.SuppressUnmanagedCodeSecurity, CoClass(typeof(CTaskScheduler))]
	internal interface ITaskScheduler
	{
		void SetTargetComputer([In, MarshalAs(UnmanagedType.LPWStr)] string Computer);
		CoTaskMemString GetTargetComputer();
		[return: MarshalAs(UnmanagedType.Interface)]
		IEnumWorkItems Enum();
		[return: MarshalAs(UnmanagedType.Interface)]
		ITask Activate([In, MarshalAs(UnmanagedType.LPWStr)][NotNull] string Name, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);
		[return: MarshalAs(UnmanagedType.Interface)]
		ITask NewWorkItem([In, MarshalAs(UnmanagedType.LPWStr)][NotNull] string TaskName, [In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);
	}

	[Guid("148BD528-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), System.Security.SuppressUnmanagedCodeSecurity]
	internal interface IEnumWorkItems
	{
		[PreserveSig()]
		//int Next([In] uint celt, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0)] out string[] rgpwszNames, [Out] out uint pceltFetched);
		int Next([In] uint RequestCount, [Out] out IntPtr Names, [Out] out uint Fetched);
		void Skip([In] uint Count);
		void Reset();
		[return: MarshalAs(UnmanagedType.Interface)]
		IEnumWorkItems Clone();
	}

#if WorkItem
	// The IScheduledWorkItem interface is actually never used because ITask inherits all of its
	// methods.  As ITask is the only kind of WorkItem (in 2002) it is the only interface we need.
	[Guid("a6b952f0-a4b1-11d0-997d-00aa006887ec"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IScheduledWorkItem
	{
		void CreateTrigger([Out] out ushort NewTriggerIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ITaskTrigger Trigger);
		void DeleteTrigger([In] ushort TriggerIndex);
		void GetTriggerCount([Out] out ushort Count);
		void GetTrigger([In] ushort TriggerIndex, [Out, MarshalAs(UnmanagedType.Interface)] out ITaskTrigger Trigger);
		void GetTriggerString([In] ushort TriggerIndex, out System.IntPtr TriggerString);
		void GetRunTimes([In, MarshalAs(UnmanagedType.Struct)] SystemTime Begin, [In, MarshalAs(UnmanagedType.Struct)] SystemTime End, ref ushort Count, [Out] out System.IntPtr TaskTimes);
		void GetNextRunTime([In, Out, MarshalAs(UnmanagedType.Struct)] ref SystemTime NextRun);
		void SetIdleWait([In] ushort IdleMinutes, [In] ushort DeadlineMinutes);
		void GetIdleWait([Out] out ushort IdleMinutes, [Out] out ushort DeadlineMinutes);
		void Run();
		void Terminate();
		void EditWorkItem([In] uint hParent, [In] uint dwReserved);
		void GetMostRecentRunTime([In, Out, MarshalAs(UnmanagedType.Struct)] ref SystemTime LastRun);
		void GetStatus([Out, MarshalAs(UnmanagedType.Error)] out int Status);
		void GetExitCode([Out] out uint ExitCode);
		void SetComment([In, MarshalAs(UnmanagedType.LPWStr)] string Comment);
		void GetComment(out System.IntPtr Comment);
		void SetCreator([In, MarshalAs(UnmanagedType.LPWStr)] string Creator);
		void GetCreator(out System.IntPtr Creator);
		void SetWorkItemData([In] ushort DataLen, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0, ArraySubType=UnmanagedType.U1)] byte[] Data);
		void GetWorkItemData([Out] out ushort DataLen, [Out] out System.IntPtr Data);
		void SetErrorRetryCount([In] ushort RetryCount);
		void GetErrorRetryCount([Out] out ushort RetryCount);
		void SetErrorRetryInterval([In] ushort RetryInterval);
		void GetErrorRetryInterval([Out] out ushort RetryInterval);
		void SetFlags([In] uint Flags);
		void GetFlags([Out] out uint Flags);
		void SetAccountInformation([In, MarshalAs(UnmanagedType.LPWStr)] string AccountName, [In, MarshalAs(UnmanagedType.LPWStr)] string Password);
		void GetAccountInformation(out System.IntPtr AccountName);
	}
#endif

	[ComImport, Guid("148BD524-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), System.Security.SuppressUnmanagedCodeSecurity, CoClass(typeof(CTask))]
	internal interface ITask
	{
		[return: MarshalAs(UnmanagedType.Interface)]
		ITaskTrigger CreateTrigger([Out] out ushort NewTriggerIndex);
		void DeleteTrigger([In] ushort TriggerIndex);
		[return: MarshalAs(UnmanagedType.U2)]
		ushort GetTriggerCount();
		[return: MarshalAs(UnmanagedType.Interface)]
		ITaskTrigger GetTrigger([In] ushort TriggerIndex);
		[return: MarshalAs(UnmanagedType.Struct)]
		void SetIdleWait([In] ushort IdleMinutes, [In] ushort DeadlineMinutes);
		void GetIdleWait([Out] out ushort IdleMinutes, [Out] out ushort DeadlineMinutes);
		TaskStatus GetStatus();
		void SetComment([In, MarshalAs(UnmanagedType.LPWStr)] string Comment);
		CoTaskMemString GetComment();
		void SetCreator([In, MarshalAs(UnmanagedType.LPWStr)] string Creator);
		CoTaskMemString GetCreator();
		void SetWorkItemData([In] ushort DataLen, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0, ArraySubType = UnmanagedType.U1)] byte[] Data);
		void GetWorkItemData(out ushort DataLen, [Out] out IntPtr Data);
		void SetFlags([In] TaskFlags Flags);
		TaskFlags GetFlags();
		void SetAccountInformation([In, MarshalAs(UnmanagedType.LPWStr)] string AccountName, [In] IntPtr Password);
		CoTaskMemString GetAccountInformation();
		void SetApplicationName([In, MarshalAs(UnmanagedType.LPWStr)] string ApplicationName);
		CoTaskMemString GetApplicationName();
		void SetParameters([In, MarshalAs(UnmanagedType.LPWStr)] string Parameters);
		CoTaskMemString GetParameters();
		void SetWorkingDirectory([In, MarshalAs(UnmanagedType.LPWStr)] string WorkingDirectory);
		CoTaskMemString GetWorkingDirectory();
		void SetPriority([In] uint Priority);
		uint GetPriority();
		void SetTaskFlags([In] uint Flags);
		void SetMaxRunTime([In] uint MaxRunTimeMS);
		uint GetMaxRunTime();
	}

	[Guid("148BD52B-A2AB-11CE-B11F-00AA00530503"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), System.Security.SuppressUnmanagedCodeSecurity]
	internal interface ITaskTrigger
	{
		void SetTrigger([In, Out, MarshalAs(UnmanagedType.Struct)] ref TaskTrigger Trigger);
		[return: MarshalAs(UnmanagedType.Struct)]
		TaskTrigger GetTrigger();
		CoTaskMemString GetTriggerString();
	}

	[ComImport, Guid("148BD52A-A2AB-11CE-B11F-00AA00530503"), System.Security.SuppressUnmanagedCodeSecurity, ClassInterface(ClassInterfaceType.None)]
	internal class CTaskScheduler
	{
	}

	[ComImport, Guid("148BD520-A2AB-11CE-B11F-00AA00530503"), System.Security.SuppressUnmanagedCodeSecurity, ClassInterface(ClassInterfaceType.None)]
	internal class CTask
	{
	}

	internal sealed class CoTaskMemString : SafeHandle
	{
		public CoTaskMemString() : base(IntPtr.Zero, true) { }
		public CoTaskMemString(IntPtr handle) : this() { SetHandle(handle); }
		public CoTaskMemString(string text) : this() { SetHandle(Marshal.StringToCoTaskMemUni(text)); }

		public static implicit operator string (CoTaskMemString cmem) => cmem.ToString();

		public override bool IsInvalid => handle == IntPtr.Zero;

		protected override bool ReleaseHandle()
		{
			Marshal.FreeCoTaskMem(handle);
			return true;
		}

		public override string ToString() => Marshal.PtrToStringUni(handle);
	}
}