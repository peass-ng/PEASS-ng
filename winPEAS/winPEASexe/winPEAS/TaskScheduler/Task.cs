using JetBrains.Annotations;
using Microsoft.Win32.TaskScheduler.V1Interop;
using Microsoft.Win32.TaskScheduler.V2Interop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using IPrincipal = Microsoft.Win32.TaskScheduler.V2Interop.IPrincipal;
// ReSharper disable UnusedMember.Global

// ReSharper disable InconsistentNaming ReSharper disable SuspiciousTypeConversion.Global

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>Defines what versions of Task Scheduler or the AT command that the task is compatible with.</summary>
	public enum TaskCompatibility
	{
		/// <summary>The task is compatible with the AT command.</summary>
		AT,

		/// <summary>The task is compatible with Task Scheduler 1.0 (Windows Server™ 2003, Windows® XP, or Windows® 2000).
		/// <para>Items not available when compared to V2:</para>
		/// <list type="bullet">
		/// <item><term>TaskDefinition.Principal.GroupId - All account information can be retrieved via the UserId property.</term></item>
		/// <item><term>TaskLogonType values Group, None and S4U are not supported.</term></item>
		/// <item><term>TaskDefinition.Principal.RunLevel == TaskRunLevel.Highest is not supported.</term></item>
		/// <item><term>Assigning access security to a task is not supported using TaskDefinition.RegistrationInfo.SecurityDescriptorSddlForm or in RegisterTaskDefinition.</term></item>
		/// <item><term>TaskDefinition.RegistrationInfo.Documentation, Source, URI and Version properties are only supported using this library. See details in the remarks for <see cref="TaskDefinition.Data"/>.</term></item>
		/// <item><term>TaskDefinition.Settings.AllowDemandStart cannot be false.</term></item>
		/// <item><term>TaskDefinition.Settings.AllowHardTerminate cannot be false.</term></item>
		/// <item><term>TaskDefinition.Settings.MultipleInstances can only be IgnoreNew.</term></item>
		/// <item><term>TaskDefinition.Settings.NetworkSettings cannot have any values.</term></item>
		/// <item><term>TaskDefinition.Settings.RestartCount can only be 0.</term></item>
		/// <item><term>TaskDefinition.Settings.StartWhenAvailable can only be false.</term></item>
		/// <item><term>TaskDefinition.Actions can only contain ExecAction instances unless the TaskDefinition.Actions.PowerShellConversion property has the Version1 flag set.</term></item>
		/// <item><term>TaskDefinition.Triggers cannot contain CustomTrigger, EventTrigger, SessionStateChangeTrigger, or RegistrationTrigger instances.</term></item>
		/// <item><term>TaskDefinition.Triggers cannot contain instances with delays set.</term></item>
		/// <item><term>TaskDefinition.Triggers cannot contain instances with ExecutionTimeLimit or Id properties set.</term></item>
		/// <item><term>TaskDefinition.Triggers cannot contain LogonTriggers instances with the UserId property set.</term></item>
		/// <item><term>TaskDefinition.Triggers cannot contain MonthlyDOWTrigger instances with the RunOnLastWeekOfMonth property set to <c>true</c>.</term></item>
		/// <item><term>TaskDefinition.Triggers cannot contain MonthlyTrigger instances with the RunOnDayWeekOfMonth property set to <c>true</c>.</term></item>
		/// </list>
		/// </summary>
		V1,

		/// <summary>The task is compatible with Task Scheduler 2.0 (Windows Vista™, Windows Server™ 2008).
		/// <para>
		/// This version is the baseline for the new, non-file based Task Scheduler. See <see cref="TaskCompatibility.V1"/> remarks for functionality that was
		/// not forward-compatible.
		/// </para></summary>
		V2,

		/// <summary>The task is compatible with Task Scheduler 2.1 (Windows® 7, Windows Server™ 2008 R2).
		/// <para>Changes from V2:</para>
		/// <list type="bullet">
		/// <item><term>TaskDefinition.Principal.ProcessTokenSidType can be defined as a value other than Default.</term></item>
		/// <item><term>TaskDefinition.Actions may not contain EmailAction or ShowMessageAction instances unless the TaskDefinition.Actions.PowerShellConversion property has
		/// the Version2 flag set.</term></item>
		/// <item><term>TaskDefinition.Principal.RequiredPrivileges can have privilege values assigned.</term></item>
		/// <item><term>TaskDefinition.Settings.DisallowStartOnRemoteAppSession can be set to true.</term></item>
		/// <item><term>TaskDefinition.UseUnifiedSchedulingEngine can be set to true.</term></item>
		/// </list>
		/// </summary>
		V2_1,

		/// <summary>The task is compatible with Task Scheduler 2.2 (Windows® 8.x, Windows Server™ 2012).
		/// <para>Changes from V2_1:</para>
		/// <list type="bullet">
		/// <item><term>TaskDefinition.Settings.MaintenanceSettings can have Period or Deadline be values other than TimeSpan.Zero or the Exclusive property set to true.</term></item>
		/// <item><term>TaskDefinition.Settings.Volatile can be set to true.</term></item>
		/// </list>
		/// </summary>
		V2_2,

		/// <summary>The task is compatible with Task Scheduler 2.3 (Windows® 10, Windows Server™ 2016).
		/// <para>Changes from V2_2:</para>
		/// <list type="bullet">
		/// <item><term>None published.</term></item>
		/// </list>
		/// </summary>
		V2_3
	}

	/// <summary>Defines how the Task Scheduler service creates, updates, or disables the task.</summary>
	[DefaultValue(CreateOrUpdate)]
	public enum TaskCreation
	{
		/// <summary>The Task Scheduler service registers the task as a new task.</summary>
		Create = 2,

		/// <summary>
		/// The Task Scheduler service either registers the task as a new task or as an updated version if the task already exists. Equivalent to Create | Update.
		/// </summary>
		CreateOrUpdate = 6,

		/// <summary>
		/// The Task Scheduler service registers the disabled task. A disabled task cannot run until it is enabled. For more information, see Enabled Property of
		/// TaskSettings and Enabled Property of RegisteredTask.
		/// </summary>
		Disable = 8,

		/// <summary>
		/// The Task Scheduler service is prevented from adding the allow access-control entry (ACE) for the context principal. When the
		/// TaskFolder.RegisterTaskDefinition or TaskFolder.RegisterTask functions are called with this flag to update a task, the Task Scheduler service does
		/// not add the ACE for the new context principal and does not remove the ACE from the old context principal.
		/// </summary>
		DontAddPrincipalAce = 0x10,

		/// <summary>
		/// The Task Scheduler service creates the task, but ignores the registration triggers in the task. By ignoring the registration triggers, the task will
		/// not execute when it is registered unless a time-based trigger causes it to execute on registration.
		/// </summary>
		IgnoreRegistrationTriggers = 0x20,

		/// <summary>
		/// The Task Scheduler service registers the task as an updated version of an existing task. When a task with a registration trigger is updated, the task
		/// will execute after the update occurs.
		/// </summary>
		Update = 4,

		/// <summary>
		/// The Task Scheduler service checks the syntax of the XML that describes the task but does not register the task. This constant cannot be combined with
		/// the Create, Update, or CreateOrUpdate values.
		/// </summary>
		ValidateOnly = 1
	}

	/// <summary>Defines how the Task Scheduler handles existing instances of the task when it starts a new instance of the task.</summary>
	[DefaultValue(IgnoreNew)]
	public enum TaskInstancesPolicy
	{
		/// <summary>Starts new instance while an existing instance is running.</summary>
		Parallel,

		/// <summary>Starts a new instance of the task after all other instances of the task are complete.</summary>
		Queue,

		/// <summary>Does not start a new instance if an existing instance of the task is running.</summary>
		IgnoreNew,

		/// <summary>Stops an existing instance of the task before it starts a new instance.</summary>
		StopExisting
	}

	/// <summary>Defines what logon technique is required to run a task.</summary>
	[DefaultValue(S4U)]
	public enum TaskLogonType
	{
		/// <summary>The logon method is not specified. Used for non-NT credentials.</summary>
		None,

		/// <summary>Use a password for logging on the user. The password must be supplied at registration time.</summary>
		Password,

		/// <summary>
		/// Use an existing interactive token to run a task. The user must log on using a service for user (S4U) logon. When an S4U logon is used, no password is
		/// stored by the system and there is no access to either the network or to encrypted files.
		/// </summary>
		S4U,

		/// <summary>User must already be logged on. The task will be run only in an existing interactive session.</summary>
		InteractiveToken,

		/// <summary>Group activation. The groupId field specifies the group.</summary>
		Group,

		/// <summary>Indicates that a Local System, Local Service, or Network Service account is being used as a security context to run the task.</summary>
		ServiceAccount,

		/// <summary>
		/// First use the interactive token. If the user is not logged on (no interactive token is available), then the password is used. The password must be
		/// specified when a task is registered. This flag is not recommended for new tasks because it is less reliable than Password.
		/// </summary>
		InteractiveTokenOrPassword
	}

	/// <summary>Defines which privileges must be required for a secured task.</summary>
	public enum TaskPrincipalPrivilege
	{
		/// <summary>Required to create a primary token. User Right: Create a token object.</summary>
		SeCreateTokenPrivilege = 1,

		/// <summary>Required to assign the primary token of a process. User Right: Replace a process-level token.</summary>
		SeAssignPrimaryTokenPrivilege,

		/// <summary>Required to lock physical pages in memory. User Right: Lock pages in memory.</summary>
		SeLockMemoryPrivilege,

		/// <summary>Required to increase the quota assigned to a process. User Right: Adjust memory quotas for a process.</summary>
		SeIncreaseQuotaPrivilege,

		/// <summary>Required to read unsolicited input from a terminal device. User Right: Not applicable.</summary>
		SeUnsolicitedInputPrivilege,

		/// <summary>Required to create a computer account. User Right: Add workstations to domain.</summary>
		SeMachineAccountPrivilege,

		/// <summary>
		/// This privilege identifies its holder as part of the trusted computer base. Some trusted protected subsystems are granted this privilege. User Right:
		/// Act as part of the operating system.
		/// </summary>
		SeTcbPrivilege,

		/// <summary>
		/// Required to perform a number of security-related functions, such as controlling and viewing audit messages. This privilege identifies its holder as a
		/// security operator. User Right: Manage auditing and the security log.
		/// </summary>
		SeSecurityPrivilege,

		/// <summary>
		/// Required to take ownership of an object without being granted discretionary access. This privilege allows the owner value to be set only to those
		/// values that the holder may legitimately assign as the owner of an object. User Right: Take ownership of files or other objects.
		/// </summary>
		SeTakeOwnershipPrivilege,

		/// <summary>Required to load or unload a device driver. User Right: Load and unload device drivers.</summary>
		SeLoadDriverPrivilege,

		/// <summary>Required to gather profiling information for the entire system. User Right: Profile system performance.</summary>
		SeSystemProfilePrivilege,

		/// <summary>Required to modify the system time. User Right: Change the system time.</summary>
		SeSystemtimePrivilege,

		/// <summary>Required to gather profiling information for a single process. User Right: Profile single process.</summary>
		SeProfileSingleProcessPrivilege,

		/// <summary>Required to increase the base priority of a process. User Right: Increase scheduling priority.</summary>
		SeIncreaseBasePriorityPrivilege,

		/// <summary>Required to create a paging file. User Right: Create a pagefile.</summary>
		SeCreatePagefilePrivilege,

		/// <summary>Required to create a permanent object. User Right: Create permanent shared objects.</summary>
		SeCreatePermanentPrivilege,

		/// <summary>
		/// Required to perform backup operations. This privilege causes the system to grant all read access control to any file, regardless of the access
		/// control list (ACL) specified for the file. Any access request other than read is still evaluated with the ACL. This privilege is required by the
		/// RegSaveKey and RegSaveKeyExfunctions. The following access rights are granted if this privilege is held: READ_CONTROL, ACCESS_SYSTEM_SECURITY,
		/// FILE_GENERIC_READ, FILE_TRAVERSE. User Right: Back up files and directories.
		/// </summary>
		SeBackupPrivilege,

		/// <summary>
		/// Required to perform restore operations. This privilege causes the system to grant all write access control to any file, regardless of the ACL
		/// specified for the file. Any access request other than write is still evaluated with the ACL. Additionally, this privilege enables you to set any
		/// valid user or group security identifier (SID) as the owner of a file. This privilege is required by the RegLoadKey function. The following access
		/// rights are granted if this privilege is held: WRITE_DAC, WRITE_OWNER, ACCESS_SYSTEM_SECURITY, FILE_GENERIC_WRITE, FILE_ADD_FILE,
		/// FILE_ADD_SUBDIRECTORY, DELETE. User Right: Restore files and directories.
		/// </summary>
		SeRestorePrivilege,

		/// <summary>Required to shut down a local system. User Right: Shut down the system.</summary>
		SeShutdownPrivilege,

		/// <summary>Required to debug and adjust the memory of a process owned by another account. User Right: Debug programs.</summary>
		SeDebugPrivilege,

		/// <summary>Required to generate audit-log entries. Give this privilege to secure servers. User Right: Generate security audits.</summary>
		SeAuditPrivilege,

		/// <summary>
		/// Required to modify the nonvolatile RAM of systems that use this type of memory to store configuration information. User Right: Modify firmware
		/// environment values.
		/// </summary>
		SeSystemEnvironmentPrivilege,

		/// <summary>
		/// Required to receive notifications of changes to files or directories. This privilege also causes the system to skip all traversal access checks. It
		/// is enabled by default for all users. User Right: Bypass traverse checking.
		/// </summary>
		SeChangeNotifyPrivilege,

		/// <summary>Required to shut down a system by using a network request. User Right: Force shutdown from a remote system.</summary>
		SeRemoteShutdownPrivilege,

		/// <summary>Required to undock a laptop. User Right: Remove computer from docking station.</summary>
		SeUndockPrivilege,

		/// <summary>
		/// Required for a domain controller to use the LDAP directory synchronization services. This privilege allows the holder to read all objects and
		/// properties in the directory, regardless of the protection on the objects and properties. By default, it is assigned to the Administrator and
		/// LocalSystem accounts on domain controllers. User Right: Synchronize directory service data.
		/// </summary>
		SeSyncAgentPrivilege,

		/// <summary>
		/// Required to mark user and computer accounts as trusted for delegation. User Right: Enable computer and user accounts to be trusted for delegation.
		/// </summary>
		SeEnableDelegationPrivilege,

		/// <summary>Required to enable volume management privileges. User Right: Manage the files on a volume.</summary>
		SeManageVolumePrivilege,

		/// <summary>
		/// Required to impersonate. User Right: Impersonate a client after authentication. Windows XP/2000: This privilege is not supported. Note that this
		/// value is supported starting with Windows Server 2003, Windows XP with SP2, and Windows 2000 with SP4.
		/// </summary>
		SeImpersonatePrivilege,

		/// <summary>
		/// Required to create named file mapping objects in the global namespace during Terminal Services sessions. This privilege is enabled by default for
		/// administrators, services, and the local system account. User Right: Create global objects. Windows XP/2000: This privilege is not supported. Note
		/// that this value is supported starting with Windows Server 2003, Windows XP with SP2, and Windows 2000 with SP4.
		/// </summary>
		SeCreateGlobalPrivilege,

		/// <summary>Required to access Credential Manager as a trusted caller. User Right: Access Credential Manager as a trusted caller.</summary>
		SeTrustedCredManAccessPrivilege,

		/// <summary>Required to modify the mandatory integrity level of an object. User Right: Modify an object label.</summary>
		SeRelabelPrivilege,

		/// <summary>Required to allocate more memory for applications that run in the context of users. User Right: Increase a process working set.</summary>
		SeIncreaseWorkingSetPrivilege,

		/// <summary>Required to adjust the time zone associated with the computer's internal clock. User Right: Change the time zone.</summary>
		SeTimeZonePrivilege,

		/// <summary>Required to create a symbolic link. User Right: Create symbolic links.</summary>
		SeCreateSymbolicLinkPrivilege
	}

	/// <summary>
	/// Defines the types of process security identifier (SID) that can be used by tasks. These changes are used to specify the type of process SID in the
	/// IPrincipal2 interface.
	/// </summary>
	public enum TaskProcessTokenSidType
	{
		/// <summary>No changes will be made to the process token groups list.</summary>
		None = 0,

		/// <summary>
		/// A task SID that is derived from the task name will be added to the process token groups list, and the token default discretionary access control list
		/// (DACL) will be modified to allow only the task SID and local system full control and the account SID read control.
		/// </summary>
		Unrestricted = 1,

		/// <summary>A Task Scheduler will apply default settings to the task process.</summary>
		Default = 2
	}

	/// <summary>Defines LUA elevation flags that specify with what privilege level the task will be run.</summary>
	public enum TaskRunLevel
	{
		/// <summary>Tasks will be run with the least privileges.</summary>
		[XmlEnum("LeastPrivilege")]
		LUA,

		/// <summary>Tasks will be run with the highest privileges.</summary>
		[XmlEnum("HighestAvailable")]
		Highest
	}

	/// <summary>
	/// Defines what kind of Terminal Server session state change you can use to trigger a task to start. These changes are used to specify the type of state
	/// change in the SessionStateChangeTrigger.
	/// </summary>
	public enum TaskSessionStateChangeType
	{
		/// <summary>
		/// Terminal Server console connection state change. For example, when you connect to a user session on the local computer by switching users on the computer.
		/// </summary>
		ConsoleConnect = 1,

		/// <summary>
		/// Terminal Server console disconnection state change. For example, when you disconnect to a user session on the local computer by switching users on
		/// the computer.
		/// </summary>
		ConsoleDisconnect = 2,

		/// <summary>
		/// Terminal Server remote connection state change. For example, when a user connects to a user session by using the Remote Desktop Connection program
		/// from a remote computer.
		/// </summary>
		RemoteConnect = 3,

		/// <summary>
		/// Terminal Server remote disconnection state change. For example, when a user disconnects from a user session while using the Remote Desktop Connection
		/// program from a remote computer.
		/// </summary>
		RemoteDisconnect = 4,

		/// <summary>Terminal Server session locked state change. For example, this state change causes the task to run when the computer is locked.</summary>
		SessionLock = 7,

		/// <summary>Terminal Server session unlocked state change. For example, this state change causes the task to run when the computer is unlocked.</summary>
		SessionUnlock = 8
	}

	/// <summary>Options for use when calling the SetSecurityDescriptorSddlForm methods.</summary>
	[Flags]
	public enum TaskSetSecurityOptions
	{
		/// <summary>No special handling.</summary>
		None = 0,

		/// <summary>The Task Scheduler service is prevented from adding the allow access-control entry (ACE) for the context principal.</summary>
		DontAddPrincipalAce = 0x10
	}

	/// <summary>Defines the different states that a registered task can be in.</summary>
	public enum TaskState
	{
		/// <summary>The state of the task is unknown.</summary>
		Unknown,

		/// <summary>The task is registered but is disabled and no instances of the task are queued or running. The task cannot be run until it is enabled.</summary>
		Disabled,

		/// <summary>Instances of the task are queued.</summary>
		Queued,

		/// <summary>The task is ready to be executed, but no instances are queued or running.</summary>
		Ready,

		/// <summary>One or more instances of the task is running.</summary>
		Running
	}

	/// <summary>
	/// Specifies how the Task Scheduler performs tasks when the computer is in an idle condition. For information about idle conditions, see Task Idle Conditions.
	/// </summary>
	[PublicAPI]
	public sealed class IdleSettings : IDisposable
	{
		private ITask v1Task;
		private IIdleSettings v2Settings;

		internal IdleSettings([NotNull] IIdleSettings iSettings)
		{
			v2Settings = iSettings;
		}

		internal IdleSettings([NotNull] ITask iTask)
		{
			v1Task = iTask;
		}

		/// <summary>Gets or sets a value that indicates the amount of time that the computer must be in an idle state before the task is run.</summary>
		/// <value>
		/// A value that indicates the amount of time that the computer must be in an idle state before the task is run. The minimum value is one minute. If this
		/// value is <c>TimeSpan.Zero</c>, then the delay will be set to the default of 10 minutes.
		/// </value>
		[DefaultValue(typeof(TimeSpan), "00:10:00")]
		[XmlElement("Duration")]
		public TimeSpan IdleDuration
		{
			get
			{
				if (v2Settings != null)
					return Task.StringToTimeSpan(v2Settings.IdleDuration);
				v1Task.GetIdleWait(out ushort _, out var deadMin);
				return TimeSpan.FromMinutes(deadMin);
			}
			set
			{
				if (v2Settings != null)
				{
					if (value != TimeSpan.Zero && value < TimeSpan.FromMinutes(1))
						throw new ArgumentOutOfRangeException(nameof(IdleDuration));
					v2Settings.IdleDuration = Task.TimeSpanToString(value);
				}
				else
				{
					v1Task.SetIdleWait((ushort)WaitTimeout.TotalMinutes, (ushort)value.TotalMinutes);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value that indicates the amount of time that the Task Scheduler will wait for an idle condition to occur. If no value is specified for
		/// this property, then the Task Scheduler service will wait indefinitely for an idle condition to occur.
		/// </summary>
		/// <value>
		/// A value that indicates the amount of time that the Task Scheduler will wait for an idle condition to occur. The minimum time allowed is 1 minute. If
		/// this value is <c>TimeSpan.Zero</c>, then the delay will be set to the default of 1 hour.
		/// </value>
		[DefaultValue(typeof(TimeSpan), "01:00:00")]
		public TimeSpan WaitTimeout
		{
			get
			{
				if (v2Settings != null)
					return Task.StringToTimeSpan(v2Settings.WaitTimeout);
				v1Task.GetIdleWait(out var idleMin, out var _);
				return TimeSpan.FromMinutes(idleMin);
			}
			set
			{
				if (v2Settings != null)
				{
					if (value != TimeSpan.Zero && value < TimeSpan.FromMinutes(1))
						throw new ArgumentOutOfRangeException(nameof(WaitTimeout));
					v2Settings.WaitTimeout = Task.TimeSpanToString(value);
				}
				else
				{
					v1Task.SetIdleWait((ushort)value.TotalMinutes, (ushort)IdleDuration.TotalMinutes);
				}
			}
		}

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			if (v2Settings != null)
				Marshal.ReleaseComObject(v2Settings);
			v1Task = null;
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString()
		{
			if (v2Settings != null || v1Task != null)
				return DebugHelper.GetDebugString(this);
			return base.ToString();
		}
	}

	/// <summary>Specifies the task settings the Task scheduler will use to start task during Automatic maintenance.</summary>
	[XmlType(IncludeInSchema = false)]
	[PublicAPI]
	public sealed class MaintenanceSettings : IDisposable
	{
		private IMaintenanceSettings iMaintSettings;
		private ITaskSettings3 iSettings;

		internal MaintenanceSettings([CanBeNull] ITaskSettings3 iSettings3)
		{
			iSettings = iSettings3;
			if (iSettings3 != null)
				iMaintSettings = iSettings.MaintenanceSettings;
		}

		/// <summary>
		/// Gets or sets the amount of time after which the Task scheduler attempts to run the task during emergency Automatic maintenance, if the task failed to
		/// complete during regular Automatic maintenance. The minimum value is one day. The value of the <see cref="Deadline"/> property should be greater than
		/// the value of the <see cref="Period"/> property. If the deadline is not specified the task will not be started during emergency Automatic maintenance.
		/// </summary>
		/// <exception cref="NotSupportedPriorToException">Property set for a task on a Task Scheduler version prior to 2.2.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		public TimeSpan Deadline
		{
			get => iMaintSettings != null ? Task.StringToTimeSpan(iMaintSettings.Deadline) : TimeSpan.Zero;
			set
			{
				if (iSettings != null)
				{
					if (iMaintSettings == null && value != TimeSpan.Zero)
						iMaintSettings = iSettings.CreateMaintenanceSettings();
					if (iMaintSettings != null)
						iMaintSettings.Deadline = Task.TimeSpanToString(value);
				}
				else
					throw new NotSupportedPriorToException(TaskCompatibility.V2_2);
			}
		}

		/// <summary>Gets or sets the amount of time the task needs to be started during Automatic maintenance. The minimum value is one minute.</summary>
		/// <exception cref="NotSupportedPriorToException">Property set for a task on a Task Scheduler version prior to 2.2.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		public TimeSpan Period
		{
			get => iMaintSettings != null ? Task.StringToTimeSpan(iMaintSettings.Period) : TimeSpan.Zero;
			set
			{
				if (iSettings != null)
				{
					if (iMaintSettings == null && value != TimeSpan.Zero)
						iMaintSettings = iSettings.CreateMaintenanceSettings();
					if (iMaintSettings != null)
						iMaintSettings.Period = Task.TimeSpanToString(value);
				}
				else
					throw new NotSupportedPriorToException(TaskCompatibility.V2_2);
			}
		}

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			if (iMaintSettings != null)
				Marshal.ReleaseComObject(iMaintSettings);
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString() => iMaintSettings != null ? DebugHelper.GetDebugString(this) : base.ToString();
    }

	/// <summary>Provides the settings that the Task Scheduler service uses to obtain a network profile.</summary>
	[XmlType(IncludeInSchema = false)]
	[PublicAPI]
	public sealed class NetworkSettings : IDisposable
	{
		private INetworkSettings v2Settings;

		internal NetworkSettings([CanBeNull] INetworkSettings iSettings)
		{
			v2Settings = iSettings;
		}

		/// <summary>Gets or sets a GUID value that identifies a network profile.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(Guid), "00000000-0000-0000-0000-000000000000")]
		public Guid Id
		{
			get
			{
				string id = null;
				if (v2Settings != null)
					id = v2Settings.Id;
				return string.IsNullOrEmpty(id) ? Guid.Empty : new Guid(id);
			}
			set
			{
				if (v2Settings != null)
					v2Settings.Id = value == Guid.Empty ? null : value.ToString();
				else
					throw new NotV1SupportedException();
			}
		}

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			if (v2Settings != null)
				Marshal.ReleaseComObject(v2Settings);
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString()
		{
			if (v2Settings != null)
				return DebugHelper.GetDebugString(this);
			return base.ToString();
		}
        }

	/// <summary>Provides the methods to get information from and control a running task.</summary>
	[XmlType(IncludeInSchema = false)]
	[PublicAPI]
	public sealed class RunningTask : Task
	{
		private IRunningTask v2RunningTask;

		internal RunningTask([NotNull] TaskService svc, [NotNull] IRegisteredTask iTask, [NotNull] IRunningTask iRunningTask)
			: base(svc, iTask)
		{
			v2RunningTask = iRunningTask;
		}

		internal RunningTask([NotNull] TaskService svc, [NotNull] ITask iTask)
			: base(svc, iTask)
		{
		}

		/// <summary>Gets the operational state of the running task.</summary>
		public override TaskState State => v2RunningTask?.State ?? base.State; }

	/// <summary>
	/// Provides the methods that are used to run the task immediately, get any running instances of the task, get or set the credentials that are used to
	/// register the task, and the properties that describe the task.
	/// </summary>
	[XmlType(IncludeInSchema = false)]
	[PublicAPI]
	public class Task : IDisposable, IComparable, IComparable<Task>
	{
		internal const AccessControlSections defaultAccessControlSections = AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access;
		internal const SecurityInfos defaultSecurityInfosSections = SecurityInfos.Owner | SecurityInfos.Group | SecurityInfos.DiscretionaryAcl;
		internal ITask v1Task;

		private static readonly int osLibMinorVer = GetOSLibraryMinorVersion();
		private static readonly DateTime v2InvalidDate = new DateTime(1899, 12, 30);
		private TaskDefinition myTD;
		private IRegisteredTask v2Task;

		internal Task([NotNull] TaskService svc, [NotNull] ITask iTask)
		{
			TaskService = svc;
			v1Task = iTask;
			ReadOnly = false;
        }

		internal Task([NotNull] TaskService svc, [NotNull] IRegisteredTask iTask, ITaskDefinition iDef = null)
		{
			TaskService = svc;
			v2Task = iTask;
			if (iDef != null)
				myTD = new TaskDefinition(iDef);
			ReadOnly = false;
		}

		/// <summary>Gets the definition of the task.</summary>
		[NotNull]
		public TaskDefinition Definition => myTD ?? (myTD = v2Task != null ? new TaskDefinition(GetV2Definition(TaskService, v2Task, true)) : new TaskDefinition(v1Task, Name));

		/// <summary>Gets or sets a Boolean value that indicates if the registered task is enabled.</summary>
		/// <remarks>
		/// As of version 1.8.1, under V1 systems (prior to Vista), this property will immediately update the Disabled state and re-save the current task. If
		/// changes have been made to the <see cref="TaskDefinition"/>, then those changes will be saved.
		/// </remarks>
		public bool Enabled
		{
			get => v2Task?.Enabled ?? Definition.Settings.Enabled;
			set
			{
				if (v2Task != null)
					v2Task.Enabled = value;
				else
				{
					Definition.Settings.Enabled = value;
					Definition.V1Save(null);
				}
			}
		}

		/// <summary>Gets the path to where the registered task is stored.</summary>
		[NotNull]
		public string Path => v2Task != null ? v2Task.Path : "\\" + Name;

		/// <summary>
		/// Gets a value indicating whether this task is read only. Only available if <see cref="Microsoft.Win32.TaskScheduler.TaskService.AllowReadOnlyTasks"/>
		/// is <c>true</c>.
		/// </summary>
		/// <value><c>true</c> if read only; otherwise, <c>false</c>.</value>
		public bool ReadOnly { get; internal set; }

		/// <summary>Gets the operational state of the registered task.</summary>
		public virtual TaskState State
		{
			get
			{
				if (v2Task != null)
					return v2Task.State;

				V1Reactivate();
				if (!Enabled)
					return TaskState.Disabled;
				switch (v1Task.GetStatus())
				{
					case TaskStatus.Ready:
					case TaskStatus.NeverRun:
					case TaskStatus.NoMoreRuns:
					case TaskStatus.Terminated:
						return TaskState.Ready;

					case TaskStatus.Running:
						return TaskState.Running;

					case TaskStatus.Disabled:
						return TaskState.Disabled;
					// case TaskStatus.NotScheduled: case TaskStatus.NoTriggers: case TaskStatus.NoTriggerTime:
					default:
						return TaskState.Unknown;
				}
			}
		}

		/// <summary>Gets or sets the <see cref="TaskService"/> that manages this task.</summary>
		/// <value>The task service.</value>
		public TaskService TaskService { get; }

		/// <summary>Gets the name of the registered task.</summary>
		[NotNull]
		public string Name => v2Task != null ? v2Task.Name : System.IO.Path.GetFileNameWithoutExtension(GetV1Path(v1Task));

		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes,
		/// follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="other">An object to compare with this instance.</param>
		/// <returns>A value that indicates the relative order of the objects being compared.</returns>
		public int CompareTo(Task other) => string.Compare(Path, other?.Path, StringComparison.InvariantCulture);

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			if (v2Task != null)
				Marshal.ReleaseComObject(v2Task);
			v1Task = null;
		}

		/// <summary>
		/// Gets a <see cref="TaskSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the task described by the
		/// current <see cref="Task"/> object.
		/// </summary>
		/// <returns>A <see cref="TaskSecurity"/> object that encapsulates the access control rules for the current task.</returns>
		public TaskSecurity GetAccessControl() => GetAccessControl(defaultAccessControlSections);

		/// <summary>
		/// Gets a <see cref="TaskSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the task described by the
		/// current <see cref="Task"/> object.
		/// </summary>
		/// <param name="includeSections">
		/// One of the <see cref="System.Security.AccessControl.AccessControlSections"/> values that specifies which group of access control entries to retrieve.
		/// </param>
		/// <returns>A <see cref="TaskSecurity"/> object that encapsulates the access control rules for the current task.</returns>
		public TaskSecurity GetAccessControl(AccessControlSections includeSections) => new TaskSecurity(this, includeSections);

		
		/// <summary>Gets the security descriptor for the task. Not available to Task Scheduler 1.0.</summary>
		/// <param name="includeSections">Section(s) of the security descriptor to return.</param>
		/// <returns>The security descriptor for the task.</returns>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		public string GetSecurityDescriptorSddlForm(SecurityInfos includeSections = defaultSecurityInfosSections) => v2Task != null ? v2Task.GetSecurityDescriptor((int) includeSections) : throw new NotV1SupportedException();

		/// <summary>
		/// Applies access control list (ACL) entries described by a <see cref="TaskSecurity"/> object to the file described by the current <see cref="Task"/> object.
		/// </summary>
		/// <param name="taskSecurity">A <see cref="TaskSecurity"/> object that describes an access control list (ACL) entry to apply to the current task.</param>
		/// <example>
		/// <para>Give read access to all authenticated users for a task.</para>
		/// <code lang="cs">
		/// <![CDATA[
		/// // Assume variable 'task' is a valid Task instance
		/// var taskSecurity = task.GetAccessControl();
		/// taskSecurity.AddAccessRule(new TaskAccessRule("Authenticated Users", TaskRights.Read, System.Security.AccessControl.AccessControlType.Allow));
		/// task.SetAccessControl(taskSecurity);
		/// ]]>
		/// </code>
		/// </example>
		public void SetAccessControl([NotNull] TaskSecurity taskSecurity)
		{
			taskSecurity.Persist(this);
		}

		/// <summary>Sets the security descriptor for the task. Not available to Task Scheduler 1.0.</summary>
		/// <param name="sddlForm">The security descriptor for the task.</param>
		/// <param name="options">Flags that specify how to set the security descriptor.</param>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		public void SetSecurityDescriptorSddlForm([NotNull] string sddlForm, TaskSetSecurityOptions options = TaskSetSecurityOptions.None)
		{
			if (v2Task != null)
				v2Task.SetSecurityDescriptor(sddlForm, (int)options);
			else
				throw new NotV1SupportedException();
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString() => Name;

		int IComparable.CompareTo(object other) => CompareTo(other as Task);

		internal static Task CreateTask(TaskService svc, IRegisteredTask iTask, bool throwError = false)
		{
			var iDef = GetV2Definition(svc, iTask, throwError);
			if (iDef != null || !svc.AllowReadOnlyTasks) return new Task(svc, iTask, iDef);
			iDef = GetV2StrippedDefinition(svc, iTask);
			return new Task(svc, iTask, iDef) { ReadOnly = true };
		}

		internal static int GetOSLibraryMinorVersion() => TaskService.LibraryVersion.Minor;

		[NotNull]
		internal static string GetV1Path(ITask v1Task)
		{
			var iFile = (IPersistFile)v1Task;
			iFile.GetCurFile(out var fileName);
			return fileName ?? string.Empty;
		}

		/// <summary>Gets the ITaskDefinition for a V2 task and prevents the errors that come when connecting remotely to a higher version of the Task Scheduler.</summary>
		/// <param name="svc">The local task service.</param>
		/// <param name="iTask">The task instance.</param>
		/// <param name="throwError">if set to <c>true</c> this method will throw an exception if unable to get the task definition.</param>
		/// <returns>A valid ITaskDefinition that should not throw errors on the local instance.</returns>
		/// <exception cref="System.InvalidOperationException">Unable to get a compatible task definition for this version of the library.</exception>
		internal static ITaskDefinition GetV2Definition(TaskService svc, IRegisteredTask iTask, bool throwError = false)
		{
			var xmlVer = new Version();
			try
			{
				var dd = new DefDoc(iTask.Xml);
				xmlVer = dd.Version;
				if (xmlVer.Minor > osLibMinorVer)
				{
					var newMinor = xmlVer.Minor;
					if (!dd.Contains("Volatile", "false", true) &&
						!dd.Contains("MaintenanceSettings"))
						newMinor = 3;
					if (!dd.Contains("UseUnifiedSchedulingEngine", "false", true) &&
						!dd.Contains("DisallowStartOnRemoteAppSession", "false", true) &&
						!dd.Contains("RequiredPrivileges") &&
						!dd.Contains("ProcessTokenSidType", "Default", true))
						newMinor = 2;
					if (!dd.Contains("DisplayName") &&
						!dd.Contains("GroupId") &&
						!dd.Contains("RunLevel", "LeastPrivilege", true) &&
						!dd.Contains("SecurityDescriptor") &&
						!dd.Contains("Source") &&
						!dd.Contains("URI") &&
						!dd.Contains("AllowStartOnDemand", "true", true) &&
						!dd.Contains("AllowHardTerminate", "true", true) &&
						!dd.Contains("MultipleInstancesPolicy", "IgnoreNew", true) &&
						!dd.Contains("NetworkSettings") &&
						!dd.Contains("StartWhenAvailable", "false", true) &&
						!dd.Contains("SendEmail") &&
						!dd.Contains("ShowMessage") &&
						!dd.Contains("ComHandler") &&
						!dd.Contains("EventTrigger") &&
						!dd.Contains("SessionStateChangeTrigger") &&
						!dd.Contains("RegistrationTrigger") &&
						!dd.Contains("RestartOnFailure") &&
						!dd.Contains("LogonType", "None", true))
						newMinor = 1;

					if (newMinor > osLibMinorVer && throwError)
						throw new InvalidOperationException($"The current version of the native library (1.{osLibMinorVer}) does not support the original or minimum version of the \"{iTask.Name}\" task ({xmlVer}/1.{newMinor}). This is likely due to attempting to read the remote tasks of a newer version of Windows from a down-level client.");

					if (newMinor != xmlVer.Minor)
					{
						dd.Version = new Version(1, newMinor);
						var def = svc.v2TaskService.NewTask(0);
						def.XmlText = dd.Xml;
						return def;
					}
				}
				return iTask.Definition;
			}
			catch (COMException comEx)
			{
				if (throwError)
				{
					if ((uint)comEx.ErrorCode == 0x80041318 && xmlVer.Minor != osLibMinorVer) // Incorrect XML value
						throw new InvalidOperationException($"The current version of the native library (1.{osLibMinorVer}) does not support the version of the \"{iTask.Name}\" task ({xmlVer})");
					throw;
				}
			}
			catch
			{
				if (throwError)
					throw;
			}
			return null;
		}

		internal static ITaskDefinition GetV2StrippedDefinition(TaskService svc, IRegisteredTask iTask)
		{
			try
			{
				var dd = new DefDoc(iTask.Xml);
				var xmlVer = dd.Version;
				if (xmlVer.Minor > osLibMinorVer)
				{
					if (osLibMinorVer < 4)
					{
						dd.RemoveTag("Volatile");
						dd.RemoveTag("MaintenanceSettings");
					}
					if (osLibMinorVer < 3)
					{
						dd.RemoveTag("UseUnifiedSchedulingEngine");
						dd.RemoveTag("DisallowStartOnRemoteAppSession");
						dd.RemoveTag("RequiredPrivileges");
						dd.RemoveTag("ProcessTokenSidType");
					}
					if (osLibMinorVer < 2)
					{
						dd.RemoveTag("DisplayName");
						dd.RemoveTag("GroupId");
						dd.RemoveTag("RunLevel");
						dd.RemoveTag("SecurityDescriptor");
						dd.RemoveTag("Source");
						dd.RemoveTag("URI");
						dd.RemoveTag("AllowStartOnDemand");
						dd.RemoveTag("AllowHardTerminate");
						dd.RemoveTag("MultipleInstancesPolicy");
						dd.RemoveTag("NetworkSettings");
						dd.RemoveTag("StartWhenAvailable");
						dd.RemoveTag("SendEmail");
						dd.RemoveTag("ShowMessage");
						dd.RemoveTag("ComHandler");
						dd.RemoveTag("EventTrigger");
						dd.RemoveTag("SessionStateChangeTrigger");
						dd.RemoveTag("RegistrationTrigger");
						dd.RemoveTag("RestartOnFailure");
						dd.RemoveTag("LogonType");
					}
					dd.RemoveTag("WnfStateChangeTrigger"); // Remove custom triggers that can't be sent to Xml
					dd.Version = new Version(1, osLibMinorVer);
					var def = svc.v2TaskService.NewTask(0);
#if DEBUG
					var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
						$"TS_Stripped_Def_{xmlVer.Minor}-{osLibMinorVer}_{iTask.Name}.xml");
					File.WriteAllText(path, dd.Xml, Encoding.Unicode);
#endif
					def.XmlText = dd.Xml;
					return def;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in GetV2StrippedDefinition: {ex}");
#if DEBUG
				throw;
#endif
			}
			return iTask.Definition;
		}

		internal static TimeSpan StringToTimeSpan(string input)
		{
			if (!string.IsNullOrEmpty(input))
				try { return XmlConvert.ToTimeSpan(input); } catch { }
			return TimeSpan.Zero;
		}

		internal static string TimeSpanToString(TimeSpan span)
		{
			if (span != TimeSpan.Zero)
				try { return XmlConvert.ToString(span); } catch { }
			return null;
		}

		internal void V1Reactivate()
		{
			var iTask = TaskService.GetTask(TaskService.v1TaskScheduler, Name);
			if (iTask != null)
				v1Task = iTask;
		}

		private class DefDoc
		{
			private readonly XmlDocument doc;

			public DefDoc(string xml)
			{
				doc = new XmlDocument();
				doc.LoadXml(xml);
			}

			public Version Version
			{
				get
				{
					try
					{
						return new Version(doc["Task"].Attributes["version"].Value);
					}
					catch
					{
						throw new InvalidOperationException("Task definition does not contain a version.");
					}
				}
				set
				{
					var task = doc["Task"];
					if (task != null) task.Attributes["version"].Value = value.ToString(2);
				}
			}

			public string Xml => doc.OuterXml;

			public bool Contains(string tag, string defaultVal = null, bool removeIfFound = false)
			{
				var nl = doc.GetElementsByTagName(tag);
				while (nl.Count > 0)
				{
					var e = nl[0];
					if (e.InnerText != defaultVal || !removeIfFound || e.ParentNode == null)
						return true;
					e.ParentNode?.RemoveChild(e);
					nl = doc.GetElementsByTagName(tag);
				}
				return false;
			}

			public void RemoveTag(string tag)
			{
				var nl = doc.GetElementsByTagName(tag);
				while (nl.Count > 0)
				{
					var e = nl[0];
					e.ParentNode?.RemoveChild(e);
					nl = doc.GetElementsByTagName(tag);
				}
			}
		}
	}

	/// <summary>Contains information about the compatibility of the current configuration with a specified version.</summary>
	[PublicAPI]
	public class TaskCompatibilityEntry
	{
		internal TaskCompatibilityEntry(TaskCompatibility comp, string prop, string reason)
		{
			CompatibilityLevel = comp;
			Property = prop;
			Reason = reason;
		}

		/// <summary>Gets the compatibility level.</summary>
		/// <value>The compatibility level.</value>
		public TaskCompatibility CompatibilityLevel { get; }

		/// <summary>Gets the property name with the incompatibility.</summary>
		/// <value>The property name.</value>
		public string Property { get; }

		/// <summary>Gets the reason for the incompatibility.</summary>
		/// <value>The reason.</value>
		public string Reason { get; }
	}

	/// <summary>Defines all the components of a task, such as the task settings, triggers, actions, and registration information.</summary>
	[XmlRoot("Task", Namespace = tns, IsNullable = false)]
	[XmlSchemaProvider("GetV1SchemaFile")]
	[PublicAPI, Serializable]
	public sealed class TaskDefinition : IDisposable, IXmlSerializable
	{
		internal const string tns = "http://schemas.microsoft.com/windows/2004/02/mit/task";

		internal string v1Name = string.Empty;
		internal ITask v1Task;
		internal ITaskDefinition v2Def;

		private ActionCollection actions;
		private TaskPrincipal principal;
		private TaskRegistrationInfo regInfo;
		private TaskSettings settings;
		private TriggerCollection triggers;

		internal TaskDefinition([NotNull] ITask iTask, string name)
		{
			v1Task = iTask;
			v1Name = name;
		}

		internal TaskDefinition([NotNull] ITaskDefinition iDef)
		{
			v2Def = iDef;
		}

		/// <summary>Gets a collection of actions that are performed by the task.</summary>
		[XmlArrayItem(ElementName = "Exec", IsNullable = true, Type = typeof(ExecAction))]
		[XmlArrayItem(ElementName = "ShowMessage", IsNullable = true, Type = typeof(ShowMessageAction))]
		[XmlArrayItem(ElementName = "ComHandler", IsNullable = true, Type = typeof(ComHandlerAction))]
		[XmlArrayItem(ElementName = "SendEmail", IsNullable = true, Type = typeof(EmailAction))]
		[XmlArray]
		[NotNull, ItemNotNull]
		public ActionCollection Actions => actions ?? (actions = v2Def != null ? new ActionCollection(v2Def) : new ActionCollection(v1Task));

		/// <summary>
		/// Gets or sets the data that is associated with the task. This data is ignored by the Task Scheduler service, but is used by third-parties who wish to
		/// extend the task format.
		/// </summary>
		/// <remarks>
		/// For V1 tasks, this library makes special use of the SetWorkItemData and GetWorkItemData methods and does not expose that data stream directly.
		/// Instead, it uses that data stream to hold a dictionary of properties that are not supported under V1, but can have values under V2. An example of
		/// this is the <see cref="TaskRegistrationInfo.URI"/> value which is stored in the data stream.
		/// <para>
		/// The library does not provide direct access to the V1 work item data. If using V2 properties with a V1 task, programmatic access to the task using the
		/// native API will retrieve unreadable results from GetWorkItemData and will eliminate those property values if SetWorkItemData is used.
		/// </para>
		/// </remarks>
		[CanBeNull]
		public string Data
		{
			get => v2Def != null ? v2Def.Data : v1Task.GetDataItem(nameof(Data));
			set
			{
				if (v2Def != null)
					v2Def.Data = value;
				else
					v1Task.SetDataItem(nameof(Data), value);
			}
		}

		/// <summary>Gets a collection of triggers that are used to start a task.</summary>
		[XmlArrayItem(ElementName = "BootTrigger", IsNullable = true, Type = typeof(BootTrigger))]
		[XmlArrayItem(ElementName = "CalendarTrigger", IsNullable = true, Type = typeof(CalendarTrigger))]
		[XmlArrayItem(ElementName = "IdleTrigger", IsNullable = true, Type = typeof(IdleTrigger))]
		[XmlArrayItem(ElementName = "LogonTrigger", IsNullable = true, Type = typeof(LogonTrigger))]
		[XmlArrayItem(ElementName = "TimeTrigger", IsNullable = true, Type = typeof(TimeTrigger))]
		[XmlArray]
		[NotNull, ItemNotNull]
		public TriggerCollection Triggers => triggers ?? (triggers = v2Def != null ? new TriggerCollection(v2Def) : new TriggerCollection(v1Task));

		/// <summary>Gets or sets the XML-formatted definition of the task.</summary>
		[XmlIgnore]
		public string XmlText
		{
			get => v2Def != null ? v2Def.XmlText : XmlSerializationHelper.WriteObjectToXmlText(this);
			set
			{
				if (v2Def != null)
					v2Def.XmlText = value;
				else
					XmlSerializationHelper.ReadObjectFromXmlText(value, this);
			}
		}

		/// <summary>Gets the principal for the task that provides the security credentials for the task.</summary>
		[NotNull]
		public TaskPrincipal Principal => principal ?? (principal = v2Def != null ? new TaskPrincipal(v2Def.Principal, () => XmlText) : new TaskPrincipal(v1Task));

		/// <summary>
		/// Gets a class instance of registration information that is used to describe a task, such as the description of the task, the author of the task, and
		/// the date the task is registered.
		/// </summary>
		public TaskRegistrationInfo RegistrationInfo => regInfo ?? (regInfo = v2Def != null ? new TaskRegistrationInfo(v2Def.RegistrationInfo) : new TaskRegistrationInfo(v1Task));

		/// <summary>Gets the settings that define how the Task Scheduler service performs the task.</summary>
		[NotNull]
		public TaskSettings Settings => settings ?? (settings = v2Def != null ? new TaskSettings(v2Def.Settings) : new TaskSettings(v1Task));

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			regInfo = null;
			triggers = null;
			settings = null;
			principal = null;
			actions = null;
			if (v2Def != null) Marshal.ReleaseComObject(v2Def);
			v1Task = null;
		}

		XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(XmlReader reader)
		{
			reader.ReadStartElement(XmlSerializationHelper.GetElementName(this), tns);
			XmlSerializationHelper.ReadObjectProperties(reader, this);
			reader.ReadEndElement();
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{
			// TODO:FIX writer.WriteAttributeString("version", "1.1");
			XmlSerializationHelper.WriteObjectProperties(writer, this);
		}

		internal static Dictionary<string, string> GetV1TaskDataDictionary(ITask v1Task)
		{
			Dictionary<string, string> dict;
			var o = GetV1TaskData(v1Task);
			if (o is string)
				dict = new Dictionary<string, string>(2) { { "Data", o.ToString() }, { "Documentation", o.ToString() } };
			else
				dict = o as Dictionary<string, string>;
			return dict ?? new Dictionary<string, string>();
		}

		internal static void SetV1TaskData(ITask v1Task, object value)
		{
			if (value == null)
				v1Task.SetWorkItemData(0, null);
			else
			{
				var b = new BinaryFormatter();
				var stream = new MemoryStream();
				b.Serialize(stream, value);
				v1Task.SetWorkItemData((ushort)stream.Length, stream.ToArray());
			}
		}

		internal void V1Save(string newName)
		{
			if (v1Task != null)
			{
				Triggers.Bind();

				var iFile = (IPersistFile)v1Task;
				if (string.IsNullOrEmpty(newName) || newName == v1Name)
				{
					try
					{
						iFile.Save(null, false);
						iFile = null;
						return;
					}
					catch { }
				}

				iFile.GetCurFile(out var path);
				File.Delete(path);
				path = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + newName + Path.GetExtension(path);
				File.Delete(path);
				iFile.Save(path, true);
			}
		}

		private static object GetV1TaskData(ITask v1Task)
		{
			var Data = IntPtr.Zero;
			try
			{
				v1Task.GetWorkItemData(out var DataLen, out Data);
				if (DataLen == 0)
					return null;
				var bytes = new byte[DataLen];
				Marshal.Copy(Data, bytes, 0, DataLen);
				var stream = new MemoryStream(bytes, false);
				var b = new BinaryFormatter();
				return b.Deserialize(stream);
			}
			catch { }
			finally
			{
				if (Data != IntPtr.Zero)
					Marshal.FreeCoTaskMem(Data);
			}
			return null;
		}
    }

	/// <summary>
	/// Provides the security credentials for a principal. These security credentials define the security context for the tasks that are associated with the principal.
	/// </summary>
	[XmlRoot("Principals", Namespace = TaskDefinition.tns, IsNullable = true)]
	[PublicAPI]
	public sealed class TaskPrincipal : IDisposable, IXmlSerializable
	{
		private const string localSystemAcct = "SYSTEM";
		private TaskPrincipalPrivileges reqPriv;
		private string v1CachedAcctInfo;
		private ITask v1Task;
		private readonly IPrincipal v2Principal;
		private readonly IPrincipal2 v2Principal2;
		private readonly Func<string> xmlFunc;

		internal TaskPrincipal([NotNull] IPrincipal iPrincipal, Func<string> defXml)
		{
			xmlFunc = defXml;
			v2Principal = iPrincipal;
			try { if (Environment.OSVersion.Version >= new Version(6, 1)) v2Principal2 = (IPrincipal2)v2Principal; }
			catch { }
		}

		internal TaskPrincipal([NotNull] ITask iTask)
		{
			v1Task = iTask;
		}

		/// <summary>Gets the account associated with this principal. This value is pulled from the TaskDefinition's XMLText property if set.</summary>
		/// <value>The account.</value>
		[DefaultValue(null), Browsable(false)]
		public string Account
		{
			get
			{
				try
				{
					var xml = xmlFunc?.Invoke();
					if (!string.IsNullOrEmpty(xml))
					{
						var doc = new XmlDocument();
						doc.LoadXml(xml);
						var pn = doc.DocumentElement?["Principals"]?["Principal"]; 
						if (pn != null)
						{
							var un = pn["UserId"] ?? pn["GroupId"];
							if (un != null)
								try { return User.FromSidString(un.InnerText).Name; }
								catch
								{
									try { return new User(un.InnerText).Name; }
									catch { }
								}
						}
					}
					return new User(ToString()).Name;
				}
				catch
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the identifier of the user group that is required to run the tasks that are associated with the principal. Setting this property to
		/// something other than a null or empty string, will set the <see cref="UserId"/> property to NULL and will set the <see cref="LogonType"/> property to TaskLogonType.Group;
		/// </summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(null)]
		[XmlIgnore]
		public string GroupId
		{
			get => v2Principal?.GroupId;
			set
			{
				if (v2Principal != null)
				{
					if (string.IsNullOrEmpty(value))
						value = null;
					else
					{
						v2Principal.UserId = null;
						v2Principal.LogonType = TaskLogonType.Group;
					}
					v2Principal.GroupId = value;
				}
				else
					throw new NotV1SupportedException();
			}
		}

		/// <summary>Gets or sets the identifier of the principal.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(null)]
		[XmlAttribute(AttributeName = "id", DataType = "ID")]
		public string Id
		{
			get => v2Principal != null ? v2Principal.Id : v1Task.GetDataItem("PrincipalId");
			set
			{
				if (v2Principal != null)
					v2Principal.Id = value;
				else
					v1Task.SetDataItem("PrincipalId", value);
			}
		}

		/// <summary>Gets or sets the security logon method that is required to run the tasks that are associated with the principal.</summary>
		/// <exception cref="NotV1SupportedException">TaskLogonType values of Group, None, or S4UNot are not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TaskLogonType), "None")]
		public TaskLogonType LogonType
		{
			get
			{
				if (v2Principal != null)
					return v2Principal.LogonType;
				if (UserId == localSystemAcct)
					return TaskLogonType.ServiceAccount;
				if (v1Task.HasFlags(TaskFlags.RunOnlyIfLoggedOn))
					return TaskLogonType.InteractiveToken;
				return TaskLogonType.InteractiveTokenOrPassword;
			}
			set
			{
				if (v2Principal != null)
					v2Principal.LogonType = value;
				else
				{
					if (value == TaskLogonType.Group || value == TaskLogonType.None || value == TaskLogonType.S4U)
						throw new NotV1SupportedException();
					v1Task.SetFlags(TaskFlags.RunOnlyIfLoggedOn, value == TaskLogonType.InteractiveToken);
				}
			}
		}

		
		/// <summary>
		/// Gets or sets the user identifier that is required to run the tasks that are associated with the principal. Setting this property to something other
		/// than a null or empty string, will set the <see cref="GroupId"/> property to NULL;
		/// </summary>
		[DefaultValue(null)]
		public string UserId
		{
			get
			{
				if (v2Principal != null)
					return v2Principal.UserId;
				if (v1CachedAcctInfo == null)
				{
					try
					{
						string acct = v1Task.GetAccountInformation();
						v1CachedAcctInfo = string.IsNullOrEmpty(acct) ? localSystemAcct : acct;
					}
					catch { v1CachedAcctInfo = string.Empty; }
				}
				return v1CachedAcctInfo == string.Empty ? null : v1CachedAcctInfo;
			}
			set
			{
				if (v2Principal != null)
				{
					if (string.IsNullOrEmpty(value))
						value = null;
					else
					{
						v2Principal.GroupId = null;
						//if (value.Contains(@"\") && !value.Contains(@"\\"))
						//	value = value.Replace(@"\", @"\\");
					}
					v2Principal.UserId = value;
				}
				else
				{
					if (value.Equals(localSystemAcct, StringComparison.CurrentCultureIgnoreCase))
						value = "";
					v1Task.SetAccountInformation(value, IntPtr.Zero);
					v1CachedAcctInfo = null;
				}
			}
		}

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			if (v2Principal != null)
				Marshal.ReleaseComObject(v2Principal);
			v1Task = null;
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString() => LogonType == TaskLogonType.Group ? GroupId : UserId;

		XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(XmlReader reader)
		{
			reader.ReadStartElement(XmlSerializationHelper.GetElementName(this), TaskDefinition.tns);
			if (reader.HasAttributes)
				Id = reader.GetAttribute("id");
			reader.Read();
			while (reader.MoveToContent() == XmlNodeType.Element)
			{
				switch (reader.LocalName)
				{
					case "Principal":
						reader.Read();
						XmlSerializationHelper.ReadObjectProperties(reader, this);
						reader.ReadEndElement();
						break;

					default:
						reader.Skip();
						break;
				}
			}
			reader.ReadEndElement();
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{
			if (string.IsNullOrEmpty(ToString()) && LogonType == TaskLogonType.None) return;
			writer.WriteStartElement("Principal");
			if (!string.IsNullOrEmpty(Id))
				writer.WriteAttributeString("id", Id);
			XmlSerializationHelper.WriteObjectProperties(writer, this);
			writer.WriteEndElement();
		}
	}

	/// <summary>
	/// List of security credentials for a principal under version 1.3 of the Task Scheduler. These security credentials define the security context for the
	/// tasks that are associated with the principal.
	/// </summary>
	[PublicAPI]
	public sealed class TaskPrincipalPrivileges : IList<TaskPrincipalPrivilege>
	{
		private IPrincipal2 v2Principal2;

		/// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
		/// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
		public int Count => v2Principal2?.RequiredPrivilegeCount ?? 0;

		/// <summary>Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</summary>
		/// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.</returns>
		public bool IsReadOnly => false;

		/// <summary>Gets or sets the element at the specified index.</summary>
		/// <returns>The element at the specified index.</returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception>
		/// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		public TaskPrincipalPrivilege this[int index]
		{
			get
			{
				if (v2Principal2 != null)
					return (TaskPrincipalPrivilege)Enum.Parse(typeof(TaskPrincipalPrivilege), v2Principal2[index + 1]);
				throw new IndexOutOfRangeException();
			}
			set => throw new NotImplementedException();
		}

		/// <summary>Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		public void Add(TaskPrincipalPrivilege item)
		{
			if (v2Principal2 != null)
				v2Principal2.AddRequiredPrivilege(item.ToString());
			else
				throw new NotSupportedPriorToException(TaskCompatibility.V2_1);
		}

		/// <summary>Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.</summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		/// <returns>true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.</returns>
		public bool Contains(TaskPrincipalPrivilege item) => IndexOf(item) != -1;

		/// <summary>Copies to.</summary>
		/// <param name="array">The array.</param>
		/// <param name="arrayIndex">Index of the array.</param>
		public void CopyTo(TaskPrincipalPrivilege[] array, int arrayIndex)
		{
			using var pEnum = GetEnumerator();
			for (var i = arrayIndex; i < array.Length; i++)
			{
				if (!pEnum.MoveNext())
					break;
				array[i] = pEnum.Current;
			}
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.</returns>
		public IEnumerator<TaskPrincipalPrivilege> GetEnumerator() => new TaskPrincipalPrivilegesEnumerator(v2Principal2);

		/// <summary>Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.</summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		/// <returns>The index of <paramref name="item"/> if found in the list; otherwise, -1.</returns>
		public int IndexOf(TaskPrincipalPrivilege item)
		{
			for (var i = 0; i < Count; i++)
			{
				if (item == this[i])
					return i;
			}
			return -1;
		}

		/// <summary>Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		void ICollection<TaskPrincipalPrivilege>.Clear()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.</summary>
		/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
		/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		void IList<TaskPrincipalPrivilege>.Insert(int index, TaskPrincipalPrivilege item)
		{
			throw new NotImplementedException();
		}

		/// <summary>Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</summary>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This
		/// method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
		bool ICollection<TaskPrincipalPrivilege>.Remove(TaskPrincipalPrivilege item)
		{
			throw new NotImplementedException();
		}

		/// <summary>Removes the <see cref="T:System.Collections.Generic.IList`1"/> item at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
		void IList<TaskPrincipalPrivilege>.RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		/// <summary>Enumerates the privileges set for a principal under version 1.3 of the Task Scheduler.</summary>
		public sealed class TaskPrincipalPrivilegesEnumerator : IEnumerator<TaskPrincipalPrivilege>
		{
			private readonly IPrincipal2 v2Principal2;
			private int cur;

			internal TaskPrincipalPrivilegesEnumerator(IPrincipal2 iPrincipal2 = null)
			{
				v2Principal2 = iPrincipal2;
				Reset();
			}

			/// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
			/// <returns>The element in the collection at the current position of the enumerator.</returns>
			public TaskPrincipalPrivilege Current { get; private set; }

			object IEnumerator.Current => Current;

			/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
			public void Dispose() { }

			/// <summary>Advances the enumerator to the next element of the collection.</summary>
			/// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
			public bool MoveNext()
			{
				if (v2Principal2 != null && cur < v2Principal2.RequiredPrivilegeCount)
				{
					cur++;
					Current = (TaskPrincipalPrivilege)Enum.Parse(typeof(TaskPrincipalPrivilege), v2Principal2[cur]);
					return true;
				}
				Current = 0;
				return false;
			}

			/// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
			public void Reset()
			{
				cur = 0;
				Current = 0;
			}
		}
	}

	/// <summary>
	/// Provides the administrative information that can be used to describe the task. This information includes details such as a description of the task, the
	/// author of the task, the date the task is registered, and the security descriptor of the task.
	/// </summary>
	[XmlRoot("RegistrationInfo", Namespace = TaskDefinition.tns, IsNullable = true)]
	[PublicAPI]
	public sealed class TaskRegistrationInfo : IDisposable, IXmlSerializable
	{
		private ITask v1Task;
		private IRegistrationInfo v2RegInfo;

		internal TaskRegistrationInfo([NotNull] IRegistrationInfo iRegInfo)
		{
			v2RegInfo = iRegInfo;
		}

		internal TaskRegistrationInfo([NotNull] ITask iTask)
		{
			v1Task = iTask;
		}

		/// <summary>Gets or sets the author of the task.</summary>
		[DefaultValue(null)]
		public string Author
		{
			get => v2RegInfo != null ? v2RegInfo.Author : v1Task.GetCreator();
			set
			{
				if (v2RegInfo != null)
					v2RegInfo.Author = value;
				else
					v1Task.SetCreator(value);
			}
		}

		/// <summary>Gets or sets the date and time when the task is registered.</summary>
		[DefaultValue(typeof(DateTime), "0001-01-01T00:00:00")]
		public DateTime Date
		{
			get
			{
				if (v2RegInfo != null)
				{
					if (DateTime.TryParse(v2RegInfo.Date, Trigger.DefaultDateCulture, DateTimeStyles.AssumeLocal, out var ret))
						return ret;
				}
				else
				{
					var v1Path = Task.GetV1Path(v1Task);
					if (!string.IsNullOrEmpty(v1Path) && File.Exists(v1Path))
						return File.GetLastWriteTime(v1Path);
				}
				return DateTime.MinValue;
			}
			set
			{
				if (v2RegInfo != null)
					v2RegInfo.Date = value == DateTime.MinValue ? null : value.ToString(Trigger.V2BoundaryDateFormat, Trigger.DefaultDateCulture);
				else
				{
					var v1Path = Task.GetV1Path(v1Task);
					if (!string.IsNullOrEmpty(v1Path) && File.Exists(v1Path))
						File.SetLastWriteTime(v1Path, value);
				}
			}
		}

		/// <summary>Gets or sets the description of the task.</summary>
		[DefaultValue(null)]
		public string Description
		{
			get => v2RegInfo != null ? FixCrLf(v2RegInfo.Description) : v1Task.GetComment();
			set
			{
				if (v2RegInfo != null)
					v2RegInfo.Description = value;
				else
					v1Task.SetComment(value);
			}
		}

		/// <summary>Gets or sets any additional documentation for the task.</summary>
		[DefaultValue(null)]
		public string Documentation
		{
			get => v2RegInfo != null ? FixCrLf(v2RegInfo.Documentation) : v1Task.GetDataItem(nameof(Documentation));
			set
			{
				if (v2RegInfo != null)
					v2RegInfo.Documentation = value;
				else
					v1Task.SetDataItem(nameof(Documentation), value);
			}
		}

		/// <summary>Gets or sets where the task originated from. For example, a task may originate from a component, service, application, or user.</summary>
		[DefaultValue(null)]
		public string Source
		{
			get => v2RegInfo != null ? v2RegInfo.Source : v1Task.GetDataItem(nameof(Source));
			set
			{
				if (v2RegInfo != null)
					v2RegInfo.Source = value;
				else
					v1Task.SetDataItem(nameof(Source), value);
			}
		}

		/// <summary>Gets or sets the URI of the task.</summary>
		/// <remarks>
		/// <c>Note:</c> Breaking change in version 2.0. This property was previously of type <see cref="Uri"/>. It was found that in Windows 8, many of the
		/// native tasks use this property in a string format rather than in a URI format.
		/// </remarks>
		[DefaultValue(null)]
		public string URI
		{
			get
			{
				var uri = v2RegInfo != null ? v2RegInfo.URI : v1Task.GetDataItem(nameof(URI));
				return string.IsNullOrEmpty(uri) ? null : uri;
			}
			set
			{
				if (v2RegInfo != null)
					v2RegInfo.URI = value;
				else
					v1Task.SetDataItem(nameof(URI), value);
			}
		}

		/// <summary>Gets or sets the version number of the task.</summary>
		[DefaultValueEx(typeof(Version), "1.0")]
		public Version Version
		{
			get
			{
				var sver = v2RegInfo != null ? v2RegInfo.Version : v1Task.GetDataItem(nameof(Version));
				if (sver != null) try { return new Version(sver); } catch { }
				return new Version(1, 0);
			}
			set
			{
				if (v2RegInfo != null)
					v2RegInfo.Version = value?.ToString();
				else
					v1Task.SetDataItem(nameof(Version), value.ToString());
			}
		}

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			v1Task = null;
			if (v2RegInfo != null)
				Marshal.ReleaseComObject(v2RegInfo);
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString()
		{
			if (v2RegInfo != null || v1Task != null)
				return DebugHelper.GetDebugString(this);
			return base.ToString();
		}

		XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(XmlReader reader)
		{
			if (!reader.IsEmptyElement)
			{
				reader.ReadStartElement(XmlSerializationHelper.GetElementName(this), TaskDefinition.tns);
				XmlSerializationHelper.ReadObjectProperties(reader, this, ProcessVersionXml);
				reader.ReadEndElement();
			}
			else
				reader.Skip();
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{
			XmlSerializationHelper.WriteObjectProperties(writer, this, ProcessVersionXml);
		}

		internal static string FixCrLf(string text) => text == null ? null : Regex.Replace(text, "(?<!\r)\n|\r(?!\n)", "\r\n");

		private bool ProcessVersionXml(PropertyInfo pi, object obj, ref object value)
		{
			if (pi.Name != "Version" || value == null) return false;
			if (value is Version)
				value = value.ToString();
			else if (value is string)
				value = new Version(value.ToString());
			return true;
		}
	}

	/// <summary>Provides the settings that the Task Scheduler service uses to perform the task.</summary>
	[XmlRoot("Settings", Namespace = TaskDefinition.tns, IsNullable = true)]
	[PublicAPI]
	public sealed class TaskSettings : IDisposable, IXmlSerializable
	{
		private const uint InfiniteRunTimeV1 = 0xFFFFFFFF;

		private IdleSettings idleSettings;
		private MaintenanceSettings maintenanceSettings;
		private NetworkSettings networkSettings;
		private ITask v1Task;
		private ITaskSettings v2Settings;
		private ITaskSettings2 v2Settings2;
		private ITaskSettings3 v2Settings3;

		internal TaskSettings([NotNull] ITaskSettings iSettings)
		{
			v2Settings = iSettings;
			try { if (Environment.OSVersion.Version >= new Version(6, 1)) v2Settings2 = (ITaskSettings2)v2Settings; }
			catch { }
			try { if (Environment.OSVersion.Version >= new Version(6, 2)) v2Settings3 = (ITaskSettings3)v2Settings; }
			catch { }
		}

		internal TaskSettings([NotNull] ITask iTask)
		{
			v1Task = iTask;
		}


		/// <summary>Gets or sets a Boolean value that indicates that the task is enabled. The task can be performed only when this setting is TRUE.</summary>
		[DefaultValue(true)]
		public bool Enabled
		{
			get => v2Settings?.Enabled ?? !v1Task.HasFlags(TaskFlags.Disabled);
			set
			{
				if (v2Settings != null)
					v2Settings.Enabled = value;
				else
					v1Task.SetFlags(TaskFlags.Disabled, !value);
			}
		}


		/// <summary>Gets or sets the number of times that the Task Scheduler will attempt to restart the task.</summary>
		/// <value>
		/// The number of times that the Task Scheduler will attempt to restart the task. If this property is set, the <see cref="RestartInterval"/> property
		/// must also be set.
		/// </value>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(0)]
		[XmlIgnore]
		public int RestartCount
		{
			get => v2Settings?.RestartCount ?? 0;
			set
			{
				if (v2Settings != null)
					v2Settings.RestartCount = value;
				else
					throw new NotV1SupportedException();
			}
		}

		/// <summary>Gets or sets a value that specifies how long the Task Scheduler will attempt to restart the task.</summary>
		/// <value>
		/// A value that specifies how long the Task Scheduler will attempt to restart the task. If this property is set, the <see cref="RestartCount"/> property
		/// must also be set. The maximum time allowed is 31 days, and the minimum time allowed is 1 minute.
		/// </value>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan RestartInterval
		{
			get => v2Settings != null ? Task.StringToTimeSpan(v2Settings.RestartInterval) : TimeSpan.Zero;
			set
			{
				if (v2Settings != null)
					v2Settings.RestartInterval = Task.TimeSpanToString(value);
				else
					throw new NotV1SupportedException();
			}
		}

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			if (v2Settings != null)
				Marshal.ReleaseComObject(v2Settings);
			idleSettings = null;
			networkSettings = null;
			v1Task = null;
		}

		/// <summary>Returns a <see cref="System.String"/> that represents this instance.</summary>
		/// <returns>A <see cref="System.String"/> that represents this instance.</returns>
		public override string ToString()
		{
			if (v2Settings != null || v1Task != null)
				return DebugHelper.GetDebugString(this);
			return base.ToString();
		}

		XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(XmlReader reader)
		{
			if (!reader.IsEmptyElement)
			{
				reader.ReadStartElement(XmlSerializationHelper.GetElementName(this), TaskDefinition.tns);
				XmlSerializationHelper.ReadObjectProperties(reader, this, ConvertXmlProperty);
				reader.ReadEndElement();
			}
			else
				reader.Skip();
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{
			XmlSerializationHelper.WriteObjectProperties(writer, this, ConvertXmlProperty);
		}

		private bool ConvertXmlProperty(PropertyInfo pi, object obj, ref object value)
		{
			if (pi.Name == "Priority" && value != null)
			{
				if (value is int)
					value = GetPriorityFromInt((int)value);
				else if (value is ProcessPriorityClass)
					value = GetPriorityAsInt((ProcessPriorityClass)value);
				return true;
			}
			return false;
		}

		private int GetPriorityAsInt(ProcessPriorityClass value)
		{
			// Check for back-door case where exact value is being passed and cast to ProcessPriorityClass
			if ((int)value <= 10 && value >= 0) return (int)value;
			int p = 7;
			switch (value)
			{
				case ProcessPriorityClass.AboveNormal:
					p = 3;
					break;

				case ProcessPriorityClass.High:
					p = 1;
					break;

				case ProcessPriorityClass.Idle:
					p = 10;
					break;

				case ProcessPriorityClass.Normal:
					p = 5;
					break;

				case ProcessPriorityClass.RealTime:
					p = 0;
					break;
					// case ProcessPriorityClass.BelowNormal: default: break;
			}
			return p;
		}

		private ProcessPriorityClass GetPriorityFromInt(int value)
		{
			switch (value)
			{
				case 0:
					return ProcessPriorityClass.RealTime;

				case 1:
					return ProcessPriorityClass.High;

				case 2:
				case 3:
					return ProcessPriorityClass.AboveNormal;

				case 4:
				case 5:
				case 6:
					return ProcessPriorityClass.Normal;
				// case 7: case 8:
				default:
					return ProcessPriorityClass.BelowNormal;

				case 9:
				case 10:
					return ProcessPriorityClass.Idle;
			}
		}
	}

	internal static class DebugHelper
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members", Justification = "<Pending>")]
		public static string GetDebugString(object inst)
		{
#if DEBUG
			var sb = new StringBuilder();
			foreach (var pi in inst.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			{
				if (pi.Name.StartsWith("Xml"))
					continue;
				var outval = pi.GetValue(inst, null);
				if (outval != null)
				{
					var defval = XmlSerializationHelper.GetDefaultValue(pi);
					if (!outval.Equals(defval))
					{
						var s = $"{pi.Name}:{outval}";
						if (s.Length > 30) s = s.Remove(30);
						sb.Append(s + "; ");
					}
				}
			}
			return sb.ToString();
#else
			return inst.GetType().ToString();
#endif
		}
	}

	internal static class TSInteropExt
	{
		public static string GetDataItem(this ITask v1Task, string name)
		{
			TaskDefinition.GetV1TaskDataDictionary(v1Task).TryGetValue(name, out var ret);
			return ret;
		}

		public static bool HasFlags(this ITask v1Task, TaskFlags flags) => v1Task.GetFlags().IsFlagSet(flags);

		public static void SetDataItem(this ITask v1Task, string name, string value)
		{
			var d = TaskDefinition.GetV1TaskDataDictionary(v1Task);
			d[name] = value;
			TaskDefinition.SetV1TaskData(v1Task, d);
		}

		public static void SetFlags(this ITask v1Task, TaskFlags flags, bool value = true)
		{
			v1Task.SetFlags(v1Task.GetFlags().SetFlags(flags, value));
		}
	}

	internal class DefaultValueExAttribute : DefaultValueAttribute
	{
		public DefaultValueExAttribute(Type type, string value) : base(null)
		{
			try
			{
				if (type == typeof(Version))
				{
					SetValue(new Version(value));
					return;
				}
				SetValue(TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value));
			}
			catch
			{
				Debug.Fail("Default value attribute of type " + type.FullName + " threw converting from the string '" + value + "'.");
			}
		}
	}
}