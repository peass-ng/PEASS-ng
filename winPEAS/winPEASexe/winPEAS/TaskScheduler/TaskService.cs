using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Quick simple trigger types for the
	/// <see cref="TaskService.AddTask(string,Microsoft.Win32.TaskScheduler.Trigger,Microsoft.Win32.TaskScheduler.Action,string,string,Microsoft.Win32.TaskScheduler.TaskLogonType,string)"/> method.
	/// </summary>
	public enum QuickTriggerType
	{
		/// <summary>At boot.</summary>
		Boot,

		/// <summary>On system idle.</summary>
		Idle,

		/// <summary>At logon of any user.</summary>
		Logon,

		/// <summary>When the task is registered.</summary>
		TaskRegistration,

		/// <summary>Hourly, starting now.</summary>
		Hourly,

		/// <summary>Daily, starting now.</summary>
		Daily,

		/// <summary>Weekly, starting now.</summary>
		Weekly,

		/// <summary>Monthly, starting now.</summary>
		Monthly
	}

	/// <summary>
	/// Known versions of the native Task Scheduler library. This can be used as a decoder for the
	/// <see cref="TaskService.HighestSupportedVersion"/> and <see cref="TaskService.LibraryVersion"/> values.
	/// </summary>
	public static class TaskServiceVersion
	{
		/// <summary>Task Scheduler 1.0 (Windows Server™ 2003, Windows® XP, or Windows® 2000).</summary>
		[Description("Task Scheduler 1.0 (Windows Server™ 2003, Windows® XP, or Windows® 2000).")]
		public static readonly Version V1_1 = new Version(1, 1);

		/// <summary>Task Scheduler 2.0 (Windows Vista™, Windows Server™ 2008).</summary>
		[Description("Task Scheduler 2.0 (Windows Vista™, Windows Server™ 2008).")]
		public static readonly Version V1_2 = new Version(1, 2);

		/// <summary>Task Scheduler 2.1 (Windows® 7, Windows Server™ 2008 R2).</summary>
		[Description("Task Scheduler 2.1 (Windows® 7, Windows Server™ 2008 R2).")]
		public static readonly Version V1_3 = new Version(1, 3);

		/// <summary>Task Scheduler 2.2 (Windows® 8.x, Windows Server™ 2012).</summary>
		[Description("Task Scheduler 2.2 (Windows® 8.x, Windows Server™ 2012).")]
		public static readonly Version V1_4 = new Version(1, 4);

		/// <summary>Task Scheduler 2.3 (Windows® 10, Windows Server™ 2016).</summary>
		[Description("Task Scheduler 2.3 (Windows® 10, Windows Server™ 2016).")]
		public static readonly Version V1_5 = new Version(1, 5);

		/// <summary>Task Scheduler 2.3 (Windows® 10, Windows Server™ 2016 post build 1703).</summary>
		[Description("Task Scheduler 2.3 (Windows® 10, Windows Server™ 2016 post build 1703).")]
		public static readonly Version V1_6 = new Version(1, 6);
	}

	/// <summary>Provides access to the Task Scheduler service for managing registered tasks.</summary>
	[Description("Provides access to the Task Scheduler service.")]
	[ToolboxItem(true), Serializable]
	public sealed partial class TaskService : Component, ISupportInitialize, System.Runtime.Serialization.ISerializable
	{
		internal static readonly bool LibraryIsV2 = Environment.OSVersion.Version.Major >= 6;
		internal static readonly Guid PowerShellActionGuid = new Guid("dab4c1e3-cd12-46f1-96fc-3981143c9bab");
		private static Guid CLSID_Ctask = typeof(V1Interop.CTask).GUID;
		private static Guid IID_ITask = typeof(V1Interop.ITask).GUID;
		[ThreadStatic]
		private static TaskService instance;
		private static Version osLibVer;

		internal V1Interop.ITaskScheduler v1TaskScheduler;
		internal V2Interop.ITaskService v2TaskService;
		private bool connecting;
		private bool forceV1;
		private bool initializing;
		private Version maxVer;
		private bool maxVerSet;
		private string targetServer;
		private bool targetServerSet;
		private string userDomain;
		private bool userDomainSet;
		private string userName;
		private bool userNameSet;
		private string userPassword;
		private bool userPasswordSet;
		private WindowsImpersonatedIdentity v1Impersonation;

		/// <summary>Creates a new instance of a TaskService connecting to the local machine as the current user.</summary>
		public TaskService()
		{
			ResetHighestSupportedVersion();
			Connect();
		}

		/// <summary>Initializes a new instance of the <see cref="TaskService"/> class.</summary>
		/// <param name="targetServer">
		/// The name of the computer that you want to connect to. If the this parameter is empty, then this will connect to the local computer.
		/// </param>
		/// <param name="userName">
		/// The user name that is used during the connection to the computer. If the user is not specified, then the current token is used.
		/// </param>
		/// <param name="accountDomain">The domain of the user specified in the <paramref name="userName"/> parameter.</param>
		/// <param name="password">
		/// The password that is used to connect to the computer. If the user name and password are not specified, then the current token is used.
		/// </param>
		/// <param name="forceV1">If set to <c>true</c> force Task Scheduler 1.0 compatibility.</param>
		public TaskService(string targetServer, string userName = null, string accountDomain = null, string password = null, bool forceV1 = false)
		{
			BeginInit();
			TargetServer = targetServer;
			UserName = userName;
			UserAccountDomain = accountDomain;
			UserPassword = password;
			this.forceV1 = forceV1;
			ResetHighestSupportedVersion();
			EndInit();
		}

		/// <summary>Delegate for methods that support update calls during COM handler execution.</summary>
		/// <param name="percentage">The percentage of completion (0 to 100).</param>
		/// <param name="message">An optional message.</param>
		public delegate void ComHandlerUpdate(short percentage, string message);

		/// <summary>Occurs when the Task Scheduler is connected to the local or remote target.</summary>
		public event EventHandler ServiceConnected;

		/// <summary>Occurs when the Task Scheduler is disconnected from the local or remote target.</summary>
		public event EventHandler ServiceDisconnected;

		/// <summary>Gets a local instance of the <see cref="TaskService"/> using the current user's credentials.</summary>
		/// <value>Local user <see cref="TaskService"/> instance.</value>
		public static TaskService Instance
		{
			get
			{
				if (instance is null)
				{
					instance = new TaskService();
					instance.ServiceDisconnected += Instance_ServiceDisconnected;
				}
				return instance;
			}
		}

		/// <summary>
		/// Gets the library version. This is the highest version supported by the local library. Tasks cannot be created using any
		/// compatibility level higher than this version.
		/// </summary>
		/// <value>The library version.</value>
		/// <remarks>
		/// The following table list the various versions and their host operating system:
		/// <list type="table">
		/// <listheader>
		/// <term>Version</term>
		/// <term>Operating System</term>
		/// </listheader>
		/// <item>
		/// <term>1.1</term>
		/// <term>Task Scheduler 1.0 (Windows Server™ 2003, Windows® XP, or Windows® 2000).</term>
		/// </item>
		/// <item>
		/// <term>1.2</term>
		/// <term>Task Scheduler 2.0 (Windows Vista™, Windows Server™ 2008).</term>
		/// </item>
		/// <item>
		/// <term>1.3</term>
		/// <term>Task Scheduler 2.1 (Windows® 7, Windows Server™ 2008 R2).</term>
		/// </item>
		/// <item>
		/// <term>1.4</term>
		/// <term>Task Scheduler 2.2 (Windows® 8.x, Windows Server™ 2012).</term>
		/// </item>
		/// <item>
		/// <term>1.5</term>
		/// <term>Task Scheduler 2.3 (Windows® 10, Windows Server™ 2016).</term>
		/// </item>
		/// <item>
		/// <term>1.6</term>
		/// <term>Task Scheduler 2.4 (Windows® 10 Version 1703, Windows Server™ 2016 Version 1703).</term>
		/// </item>
		/// </list>
		/// </remarks>
		[Browsable(false)]
		public static Version LibraryVersion { get; } = Instance.HighestSupportedVersion;

		/// <summary>
		/// Gets or sets a value indicating whether to allow tasks from later OS versions with new properties to be retrieved as read only tasks.
		/// </summary>
		/// <value><c>true</c> if allow read only tasks; otherwise, <c>false</c>.</value>
		[DefaultValue(false), Category("Behavior"), Description("Allow tasks from later OS versions with new properties to be retrieved as read only tasks.")]
		public bool AllowReadOnlyTasks { get; set; }

		/// <summary>Gets the highest version of Task Scheduler that a computer supports.</summary>
		/// <remarks>
		/// The following table list the various versions and their host operating system:
		/// <list type="table">
		/// <listheader>
		/// <term>Version</term>
		/// <term>Operating System</term>
		/// </listheader>
		/// <item>
		/// <term>1.1</term>
		/// <term>Task Scheduler 1.0 (Windows Server™ 2003, Windows® XP, or Windows® 2000).</term>
		/// </item>
		/// <item>
		/// <term>1.2</term>
		/// <term>Task Scheduler 2.0 (Windows Vista™, Windows Server™ 2008).</term>
		/// </item>
		/// <item>
		/// <term>1.3</term>
		/// <term>Task Scheduler 2.1 (Windows® 7, Windows Server™ 2008 R2).</term>
		/// </item>
		/// <item>
		/// <term>1.4</term>
		/// <term>Task Scheduler 2.2 (Windows® 8.x, Windows Server™ 2012).</term>
		/// </item>
		/// <item>
		/// <term>1.5</term>
		/// <term>Task Scheduler 2.3 (Windows® 10, Windows Server™ 2016).</term>
		/// </item>
		/// <item>
		/// <term>1.6</term>
		/// <term>Task Scheduler 2.4 (Windows® 10 Version 1703, Windows Server™ 2016 Version 1703).</term>
		/// </item>
		/// </list>
		/// </remarks>
		[Category("Data"), TypeConverter(typeof(VersionConverter)), Description("Highest version of library that should be used.")]
		public Version HighestSupportedVersion
		{
			get => maxVer;
			set
			{
				if (value > GetLibraryVersionFromLocalOS())
					throw new ArgumentOutOfRangeException(nameof(HighestSupportedVersion), @"The value of HighestSupportedVersion cannot exceed that of the underlying Windows version library.");
				maxVer = value;
				maxVerSet = true;
				var localForceV1 = value <= TaskServiceVersion.V1_1;
				if (localForceV1 == forceV1) return;
				forceV1 = localForceV1;
				Connect();
			}
		}

		/// <summary>Gets the root ("\") folder. For Task Scheduler 1.0, this is the only folder.</summary>
		[Browsable(false)]
		public TaskFolder RootFolder => GetFolder(TaskFolder.rootString);

		/// <summary>Gets or sets the name of the computer that is running the Task Scheduler service that the user is connected to.</summary>
		[Category("Data"), DefaultValue(null), Description("The name of the computer to connect to.")]
		public string TargetServer
		{
			get => ShouldSerializeTargetServer() ? targetServer : null;
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.Compare(value, targetServer, StringComparison.OrdinalIgnoreCase) != 0)
				{
					targetServerSet = true;
					targetServer = value;
					Connect();
				}
			}
		}

		/// <summary>Gets or sets the user account domain to be used when connecting to the <see cref="TargetServer"/>.</summary>
		/// <value>The user account domain.</value>
		[Category("Data"), DefaultValue(null), Description("The user account domain to be used when connecting.")]
		public string UserAccountDomain
		{
			get => ShouldSerializeUserAccountDomain() ? userDomain : null;
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.Compare(value, userDomain, StringComparison.OrdinalIgnoreCase) != 0)
				{
					userDomainSet = true;
					userDomain = value;
					Connect();
				}
			}
		}

		/// <summary>Gets or sets the user name to be used when connecting to the <see cref="TargetServer"/>.</summary>
		/// <value>The user name.</value>
		[Category("Data"), DefaultValue(null), Description("The user name to be used when connecting.")]
		public string UserName
		{
			get => ShouldSerializeUserName() ? userName : null;
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.Compare(value, userName, StringComparison.OrdinalIgnoreCase) != 0)
				{
					userNameSet = true;
					userName = value;
					Connect();
				}
			}
		}

		/// <summary>Gets or sets the user password to be used when connecting to the <see cref="TargetServer"/>.</summary>
		/// <value>The user password.</value>
		[Category("Data"), DefaultValue(null), Description("The user password to be used when connecting.")]
		public string UserPassword
		{
			get => userPassword;
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.CompareOrdinal(value, userPassword) != 0)
				{
					userPasswordSet = true;
					userPassword = value;
					Connect();
				}
			}
		}

		/// <summary>Gets a Boolean value that indicates if you are connected to the Task Scheduler service.</summary>
		[Browsable(false)]
		public bool Connected => v2TaskService != null && v2TaskService.Connected || v1TaskScheduler != null;

		/// <summary>Creates a new task, registers the task, and returns the instance.</summary>
		/// <param name="path">
		/// The task name. If this value is NULL, the task will be registered in the root task folder and the task name will be a GUID value
		/// that is created by the Task Scheduler service. A task name cannot begin or end with a space character. The '.' character cannot
		/// be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.
		/// </param>
		/// <param name="trigger">The <see cref="Trigger"/> to determine when to run the task.</param>
		/// <param name="action">The <see cref="Action"/> to determine what happens when the task is triggered.</param>
		/// <param name="userId">The user credentials used to register the task.</param>
		/// <param name="password">The password for the userId used to register the task.</param>
		/// <param name="logonType">
		/// A <see cref="TaskLogonType"/> value that defines what logon technique is used to run the registered task.
		/// </param>
		/// <param name="description">The task description.</param>
		/// <returns>A <see cref="Task"/> instance of the registered task.</returns>
		/// <remarks>
		/// This method is shorthand for creating a new TaskDescription, adding a trigger and action, and then registering it in the root folder.
		/// </remarks>
		/// <example>
		/// <code lang="cs">
		/// <![CDATA[
		/// // Display a log file every other day
		/// TaskService.Instance.AddTask("Test", new DailyTrigger { DaysInterval = 2 }, new ExecAction("notepad.exe", "c:\\test.log", null));
		/// ]]>
		/// </code>
		/// </example>
		public Task AddTask([NotNull] string path, [NotNull] Trigger trigger, [NotNull] Action action, string userId = null, string password = null, TaskLogonType logonType = TaskLogonType.InteractiveToken, string description = null)
		{
			var td = NewTask();
			if (!string.IsNullOrEmpty(description))
				td.RegistrationInfo.Description = description;

			// Create a trigger that will fire the task at a specific date and time
			td.Triggers.Add(trigger);

			// Create an action that will launch Notepad whenever the trigger fires
			td.Actions.Add(action);

			// Register the task in the root folder
			return RootFolder.RegisterTaskDefinition(path, td, TaskCreation.CreateOrUpdate, userId, password, logonType);
		}

		/// <summary>Signals the object that initialization is starting.</summary>
		public void BeginInit() => initializing = true;

		/// <summary>Signals the object that initialization is complete.</summary>
		public void EndInit()
		{
			initializing = false;
			Connect();
		}

		/// <summary>Determines whether the specified <see cref="System.Object"/>, is equal to this instance.</summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj)
		{
			var tsobj = obj as TaskService;
			if (tsobj != null)
				return tsobj.TargetServer == TargetServer && tsobj.UserAccountDomain == UserAccountDomain && tsobj.UserName == UserName && tsobj.UserPassword == UserPassword && tsobj.forceV1 == forceV1;
			return base.Equals(obj);
		}

		/// <summary>Gets the path to a folder of registered tasks.</summary>
		/// <param name="folderName">
		/// The path to the folder to retrieve. Do not use a backslash following the last folder name in the path. The root task folder is
		/// specified with a backslash (\). An example of a task folder path, under the root task folder, is \MyTaskFolder. The '.' character
		/// cannot be used to specify the current task folder and the '..' characters cannot be used to specify the parent task folder in the path.
		/// </param>
		/// <returns><see cref="TaskFolder"/> instance for the requested folder or <c>null</c> if <paramref name="folderName"/> was unrecognized.</returns>
		/// <exception cref="NotV1SupportedException">
		/// Folder other than the root (\) was requested on a system not supporting Task Scheduler 2.0.
		/// </exception>
		public TaskFolder GetFolder(string folderName)
		{
			TaskFolder f = null;
			if (v2TaskService != null)
			{
				if (string.IsNullOrEmpty(folderName)) folderName = TaskFolder.rootString;
				try
				{
					var ifld = v2TaskService.GetFolder(folderName);
					if (ifld != null)
						f = new TaskFolder(this, ifld);
				}
				catch (System.IO.DirectoryNotFoundException) { }
				catch (System.IO.FileNotFoundException) { }
			}
			else if (folderName == TaskFolder.rootString || string.IsNullOrEmpty(folderName))
				f = new TaskFolder(this);
			else
				throw new NotV1SupportedException("Folder other than the root (\\) was requested on a system only supporting Task Scheduler 1.0.");
			return f;
		}

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode() => new { A = TargetServer, B = UserAccountDomain, C = UserName, D = UserPassword, E = forceV1 }.GetHashCode();

		/// <summary>
		/// Returns an empty task definition object to be filled in with settings and properties and then registered using the
		/// <see cref="TaskFolder.RegisterTaskDefinition(string, TaskDefinition)"/> method.
		/// </summary>
		/// <returns>A <see cref="TaskDefinition"/> instance for setting properties.</returns>
		public TaskDefinition NewTask()
		{
			if (v2TaskService != null)
				return new TaskDefinition(v2TaskService.NewTask(0));
			var v1Name = "Temp" + Guid.NewGuid().ToString("B");
			return new TaskDefinition(v1TaskScheduler.NewWorkItem(v1Name, CLSID_Ctask, IID_ITask), v1Name);
		}


		[System.Security.SecurityCritical]
		void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			info.AddValue("TargetServer", TargetServer, typeof(string));
			info.AddValue("UserName", UserName, typeof(string));
			info.AddValue("UserAccountDomain", UserAccountDomain, typeof(string));
			info.AddValue("UserPassword", UserPassword, typeof(string));
			info.AddValue("forceV1", forceV1, typeof(bool));
		}

		internal static V2Interop.IRegisteredTask GetTask([NotNull] V2Interop.ITaskService iSvc, [NotNull] string name)
		{
			V2Interop.ITaskFolder fld = null;
			try
			{
				fld = iSvc.GetFolder("\\");
				return fld.GetTask(name);
			}
			catch
			{
				return null;
			}
			finally
			{
				if (fld != null) Marshal.ReleaseComObject(fld);
			}
		}

		internal static V1Interop.ITask GetTask([NotNull] V1Interop.ITaskScheduler iSvc, [NotNull] string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));
			try
			{
				return iSvc.Activate(name, IID_ITask);
			}
			catch (UnauthorizedAccessException)
			{
				// TODO: Take ownership of the file and try again
				throw;
			}
			catch (ArgumentException)
			{
				return iSvc.Activate(name + ".job", IID_ITask);
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.ComponentModel.Component"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			if (v2TaskService != null)
			{
				try
				{
					Marshal.ReleaseComObject(v2TaskService);
				}
				catch { }
				v2TaskService = null;
			}
			if (v1TaskScheduler != null)
			{
				try
				{
					Marshal.ReleaseComObject(v1TaskScheduler);
				}
				catch { }
				v1TaskScheduler = null;
			}
			if (v1Impersonation != null)
			{
				v1Impersonation.Dispose();
				v1Impersonation = null;
			}
			if (!connecting)
				ServiceDisconnected?.Invoke(this, EventArgs.Empty);
			base.Dispose(disposing);
		}

		private static Version GetLibraryVersionFromLocalOS()
		{
			if (osLibVer == null)
			{
				if (Environment.OSVersion.Version.Major < 6)
					osLibVer = TaskServiceVersion.V1_1;
				else
				{
					if (Environment.OSVersion.Version.Minor == 0)
						osLibVer = TaskServiceVersion.V1_2;
					else if (Environment.OSVersion.Version.Minor == 1)
						osLibVer = TaskServiceVersion.V1_3;
					else
					{
						try
						{
							var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(Path.Combine(Environment.SystemDirectory, "taskschd.dll"));
							if (fvi.FileBuildPart > 9600 && fvi.FileBuildPart <= 14393)
								osLibVer = TaskServiceVersion.V1_5;
							else if (fvi.FileBuildPart >= 15063)
								osLibVer = TaskServiceVersion.V1_6;
							else // fvi.FileBuildPart <= 9600
								osLibVer = TaskServiceVersion.V1_4;
						}
						catch { /* ignored */ };
					}
				}

				if (osLibVer == null)
					throw new NotSupportedException(@"The Task Scheduler library version for this system cannot be determined.");
			}
			return osLibVer;
		}

		private static void Instance_ServiceDisconnected(object sender, EventArgs e) => instance?.Connect();

		/// <summary>Connects this instance of the <see cref="TaskService"/> class to a running Task Scheduler.</summary>
		private void Connect()
		{
			ResetUnsetProperties();

			if (!initializing && !DesignMode)
			{
				if (!string.IsNullOrEmpty(userDomain) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userPassword) || string.IsNullOrEmpty(userDomain) && string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userPassword))
				{
					// Clear stuff if already connected
					connecting = true;
					Dispose(true);

					if (LibraryIsV2 && !forceV1)
					{
						v2TaskService = new V2Interop.ITaskService();
						if (!string.IsNullOrEmpty(targetServer))
						{
							// Check to ensure character only server name. (Suggested by bigsan)
							if (targetServer.StartsWith(@"\"))
								targetServer = targetServer.TrimStart('\\');
							// Make sure null is provided for local machine to compensate for a native library oddity (Found by ctrollen)
							if (targetServer.Equals(Environment.MachineName, StringComparison.CurrentCultureIgnoreCase))
								targetServer = null;
						}
						else
							targetServer = null;
						v2TaskService.Connect(targetServer, userName, userDomain, userPassword);
						targetServer = v2TaskService.TargetServer;
						userName = v2TaskService.ConnectedUser;
						userDomain = v2TaskService.ConnectedDomain;
						maxVer = GetV2Version();
					}
					else
					{
						v1Impersonation = new WindowsImpersonatedIdentity(userName, userDomain, userPassword);
						v1TaskScheduler = new V1Interop.ITaskScheduler();
						if (!string.IsNullOrEmpty(targetServer))
						{
							// Check to ensure UNC format for server name. (Suggested by bigsan)
							if (!targetServer.StartsWith(@"\\"))
								targetServer = @"\\" + targetServer;
						}
						else
							targetServer = null;
						v1TaskScheduler.SetTargetComputer(targetServer);
						targetServer = v1TaskScheduler.GetTargetComputer();
						maxVer = TaskServiceVersion.V1_1;
					}
					ServiceConnected?.Invoke(this, EventArgs.Empty);
					connecting = false;
				}
				else
				{
					throw new ArgumentException("A username, password, and domain must be provided.");
				}
			}
		}

		/// <summary>Finds the task in folder.</summary>
		/// <param name="fld">The folder.</param>
		/// <param name="taskName">The wildcard expression to compare task names with.</param>
		/// <param name="results">The results.</param>
		/// <param name="recurse">if set to <c>true</c> recurse folders.</param>
		/// <returns>True if any tasks are found, False if not.</returns>
		private bool FindTaskInFolder([NotNull] TaskFolder fld, System.Text.RegularExpressions.Regex taskName, ref System.Collections.Generic.List<Task> results, bool recurse = true)
		{
			results.AddRange(fld.GetTasks(taskName));

			if (recurse)
			{
				foreach (var f in fld.SubFolders)
				{
					if (FindTaskInFolder(f, taskName, ref results))
						return true;
				}
			}
			return false;
		}

		/// <summary>Finds the task in folder.</summary>
		/// <param name="fld">The folder.</param>
		/// <param name="filter">The filter to use when looking for tasks.</param>
		/// <param name="results">The results.</param>
		/// <param name="recurse">if set to <c>true</c> recurse folders.</param>
		/// <returns>True if any tasks are found, False if not.</returns>
		private bool FindTaskInFolder([NotNull] TaskFolder fld, Predicate<Task> filter, ref System.Collections.Generic.List<Task> results, bool recurse = true)
		{
			foreach (var t in fld.GetTasks())
				try
				{
					if (filter(t))
						results.Add(t);
				}
				catch
				{
					System.Diagnostics.Debug.WriteLine($"Unable to evaluate filter for task '{t.Path}'.");
				}

			if (recurse)
			{
				foreach (var f in fld.SubFolders)
				{
					if (FindTaskInFolder(f, filter, ref results))
						return true;
				}
			}
			return false;
		}

		private Version GetV2Version()
		{
			var v = v2TaskService.HighestVersion;
			return new Version((int)(v >> 16), (int)(v & 0x0000FFFF));
		}

		private void ResetHighestSupportedVersion() => maxVer = Connected ? (v2TaskService != null ? GetV2Version() : TaskServiceVersion.V1_1) : GetLibraryVersionFromLocalOS();

		private void ResetUnsetProperties()
		{
			if (!maxVerSet) ResetHighestSupportedVersion();
			if (!targetServerSet) targetServer = null;
			if (!userDomainSet) userDomain = null;
			if (!userNameSet) userName = null;
			if (!userPasswordSet) userPassword = null;
		}

		private bool ShouldSerializeTargetServer() => targetServer != null && !targetServer.Trim('\\').Equals(Environment.MachineName.Trim('\\'), StringComparison.InvariantCultureIgnoreCase);

		private bool ShouldSerializeUserAccountDomain() => userDomain != null && !userDomain.Equals(Environment.UserDomainName, StringComparison.InvariantCultureIgnoreCase);

		private bool ShouldSerializeUserName() => userName != null && !userName.Equals(Environment.UserName, StringComparison.InvariantCultureIgnoreCase);

		/// <summary>
		/// Represents a valid, connected session to a Task Scheduler instance. This token is thread-safe and should be the means of passing
		/// information about a <see cref="TaskService"/> between threads.
		/// </summary>
		public struct ConnectionToken
		{
			internal int token;

			internal ConnectionToken(int value) => token = value;
		}

		private class ComHandlerThread
		{
			private readonly System.Threading.AutoResetEvent completed = new System.Threading.AutoResetEvent(false);
			private readonly string Data;
			private readonly Type objType;
			private readonly TaskHandlerStatus status;
			private readonly int Timeout;

			private class TaskHandlerStatus : ITaskHandlerStatus
			{
				private readonly Action<int> OnCompleted;
				private readonly ComHandlerUpdate OnUpdate;

				public void TaskCompleted([In, MarshalAs(UnmanagedType.Error)] int taskErrCode) => OnCompleted?.Invoke(taskErrCode);

				public void UpdateStatus([In] short percentComplete, [In, MarshalAs(UnmanagedType.BStr)] string statusMessage) => OnUpdate?.Invoke(percentComplete, statusMessage);
			}
		}

		// This private class holds information needed to create a new TaskService instance
		private class ConnectionData : IEquatable<ConnectionData>
		{
			public bool ForceV1;
			public string TargetServer, UserAccountDomain, UserName, UserPassword;

			public bool Equals(ConnectionData other) => string.Equals(TargetServer, other.TargetServer, StringComparison.InvariantCultureIgnoreCase) &&
					   string.Equals(UserAccountDomain, other.UserAccountDomain, StringComparison.InvariantCultureIgnoreCase) &&
					   string.Equals(UserName, other.UserName, StringComparison.InvariantCultureIgnoreCase) &&
					   string.Equals(UserPassword, other.UserPassword, StringComparison.InvariantCultureIgnoreCase) &&
					   ForceV1 == other.ForceV1;
		}

		private class VersionConverter : TypeConverter
		{
		}
	}
}