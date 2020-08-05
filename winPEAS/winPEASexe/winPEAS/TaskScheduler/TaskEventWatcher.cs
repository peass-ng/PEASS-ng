using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Information about the task event.
	/// </summary>
	[PublicAPI]
	public class TaskEventArgs : EventArgs
	{
		private readonly TaskService taskService;

		internal TaskEventArgs([NotNull] TaskEvent evt, TaskService ts = null)
		{
			TaskEvent = evt;
			TaskPath = evt.TaskPath;
			taskService = ts;
		}

		/// <summary>
		/// Gets the <see cref="TaskEvent"/>.
		/// </summary>
		/// <value>
		/// The TaskEvent.
		/// </value>
		[NotNull]
		public TaskEvent TaskEvent { get; }

		/// <summary>
		/// Gets the task path.
		/// </summary>
		/// <value>
		/// The task path.
		/// </value>
		public string TaskPath { get; }
	}

	/// <summary>
	/// Watches system events related to tasks and issues a <see cref="TaskEventWatcher.EventRecorded"/> event when the filtered conditions are met.
	/// <note>Only available for Task Scheduler 2.0 on Windows Vista or Windows Server 2003 and later.</note>
	/// </summary>
	/// <remarks>Sometimes, a developer will need to know about events as they occur. In this case, they can use the TaskEventWatcher component that enables the developer to watch a task, a folder, or the entire system for filtered events.</remarks>
	/// <example>
	/// <para>Below is information on how to watch a folder for all task events. For a complete example, look at this sample project: TestTaskWatcher.zip</para>
	/// <code lang="cs"><![CDATA[
	/// private TaskEventWatcher watcher;
	/// 
	/// // Create and configure a new task watcher for the task folder
	/// private void SetupWatcher(TaskFolder tf)
	/// {
	/// 	if (tf != null)
	/// 	{
	/// 		// Set up a watch over the supplied task folder.
	/// 		watcher = new TaskEventWatcher(tf);
	/// 
	/// 		// Assign a SynchronizingObject to a local UI class to synchronize the events in this thread.
	/// 		watcher.SynchronizingObject = this;
	/// 
	/// 		// Only watch for tasks that start with my company name
	/// 		watcher.Filter.TaskName = "MyCo*";
	/// 
	/// 		// Only watch for task events that are informational
	/// 		watcher.Filter.EventLevels = new int[]
	/// 		   { 0 /* StandardEventLevel.LogAlways */, (int)StandardEventLevel.Informational };
	/// 
	/// 		// Assign an event handler for when events are recorded
	/// 		watcher.EventRecorded += Watcher_EventRecorded;
	/// 
	/// 		// Start watching the folder by enabling the watcher
	/// 		watcher.Enabled = true;
	/// 	}
	/// }
	/// 
	/// // Cleanup and release the task watcher
	/// private void TearDownWatcher()
	/// {
	/// 	if (watcher != null)
	/// 	{
	/// 		// Unhook the event
	/// 		watcher.EventRecorded -= Watcher_EventRecorded;
	/// 		// Stop watching for events
	/// 		watcher.Enabled = false;
	/// 		// Initiate garbage collection for the watcher
	/// 		watcher = null;
	/// 	}
	/// }
	/// 
	/// // Update ListView instance when task events occur
	/// private void Watcher_EventRecorded(object sender, TaskEventArgs e)
	/// {
	/// 	int idx = IndexOfTask(e.TaskName);
	/// 
	/// 	// If event is for a task we already have in the list...
	/// 	if (idx != -1)
	/// 	{
	/// 		// If event indicates that task has been deleted, remove it from the list
	/// 		if (e.TaskEvent.StandardEventId == StandardTaskEventId.TaskDeleted)
	/// 		{
	/// 			listView1.Items.RemoveAt(idx);
	/// 		}
	/// 
	/// 		// If event is anything else, it most likely represents a change,
	/// 		// so update the item using information supplied through the
	/// 		// TaskEventArgs instance.
	/// 		else
	/// 		{
	/// 			var lvi = listView1.Items[idx];
	/// 			lvi.Subitems[0].Text = e.TaskName;
	/// 			lvi.Subitems[1].Text = e.Task.State.ToString();
	/// 			lvi.Subitems[2].Text = GetNextRunTimeString(e.Task);
	/// 		}
	/// 	}
	/// 
	/// 	// If event is for a task we don't have in our list, add it
	/// 	else
	/// 	{
	/// 		var lvi = new ListViewItem(new string[] { e.TaskName,
	/// 	 e.Task.State.ToString(), GetNextRunTimeString(e.Task) });
	/// 		listView1.Items.Add(lvi);
	/// 		listView1.Sort();
	/// 	}
	/// }
	/// 
	/// // Get the next run time for a task
	/// private string GetNextRunTimeString(Task t)
	/// {
	/// 	if (t.State == TaskState.Disabled || t.NextRunTime < DateTime.Now)
	/// 		return string.Empty;
	/// 	return t.NextRunTime.ToString("G");
	/// }
	/// ]]></code></example>
	[DefaultEvent(nameof(EventRecorded)), DefaultProperty(nameof(Folder))]
#if DESIGNER
	[Designer(typeof(Design.TaskEventWatcherDesigner))]
#endif
	[ToolboxItem(true), Serializable]
	[PublicAPI]
	public class TaskEventWatcher : Component, ISupportInitialize
	{
		private const string root = "\\";
		private const string star = "*";

		private static readonly TimeSpan MaxV1EventLapse = TimeSpan.FromSeconds(1);

		private bool disposed;
		private bool enabled;
		private string folder = root;
		private bool includeSubfolders;
		private bool initializing;
		private StandardTaskEventId lastId = 0;
		private DateTime lastIdTime = DateTime.MinValue;
		private TaskService ts;
		private FileSystemWatcher v1Watcher;
		private EventLogWatcher watcher;
		private ISynchronizeInvoke synchronizingObject;

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskEventWatcher"/> class. If other
		/// properties are not set, this will watch for all events for all tasks on the local machine.
		/// </summary>
		public TaskEventWatcher() : this(TaskService.Instance)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskEventWatcher" /> class watching only
		/// those events for the task with the provided path on the local machine.
		/// </summary>
		/// <param name="taskPath">The full path (folders and name) of the task to watch.</param>
		/// <param name="taskService">The task service.</param>
		/// <exception cref="System.ArgumentException">$Invalid task name: {taskPath}</exception>
		public TaskEventWatcher(string taskPath, TaskService taskService = null) : this(taskService ?? TaskService.Instance)
		{
			InitTask(taskPath);
		}

		private TaskEventWatcher(TaskService ts)
		{
			TaskService = ts;
			Filter = new EventFilter(this);
		}

		/// <summary>
		/// Occurs when a task or the task engine records an event.
		/// </summary>
		[Category("Action"), Description("Event recorded by a task or the task engine.")]
		public event EventHandler<TaskEventArgs> EventRecorded;

		/// <summary>
		/// Gets or sets a value indicating whether the component is enabled.
		/// </summary>
		/// <value>
		///   <c>true</c> if enabled; otherwise, <c>false</c>.
		/// </value>
		[DefaultValue(false), Category("Behavior"), Description("Indicates whether the component is enabled.")]
		public bool Enabled
		{
			get { return enabled; }
			set
			{
				if (enabled != value)
				{
					System.Diagnostics.Debug.WriteLine($"TaskEventWather: Set {nameof(Enabled)} = {value}");
					enabled = value;
					if (!IsSuspended())
					{
						if (enabled)
							StartRaisingEvents();
						else
							StopRaisingEvents();
					}
				}
			}
		}

		/// <summary>
		/// Gets the filter for this <see cref="TaskEventWatcher"/>.
		/// </summary>
		/// <value>
		/// The filter.
		/// </value>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Category("Behavior"), Description("Indicates the filter for the watcher.")]
		public EventFilter Filter { get; }

		/// <summary>
		/// Gets or sets the folder to watch.
		/// </summary>
		/// <value>
		/// The folder path to watch. This value should include the leading "\" to indicate the root folder.
		/// </value>
		/// <exception cref="System.ArgumentException">Thrown if the folder specified does not exist or contains invalid characters.</exception>
		[DefaultValue(root), Category("Behavior"), Description("Indicates the folder to watch.")]
		public string Folder
		{
			get { return folder; }
			set
			{
				if (string.IsNullOrEmpty(value))
					value = root;
				if (!value.EndsWith("\\"))
					value += "\\";
				if (string.Compare(folder, value, StringComparison.OrdinalIgnoreCase) == 0) return;
				if ((DesignMode && (value.IndexOfAny(new[] { '*', '?' }) != -1 || value.IndexOfAny(Path.GetInvalidPathChars()) != -1)) || (TaskService.GetFolder(value == root ? value : value.TrimEnd('\\')) == null))
					throw new ArgumentException($"Invalid folder name: {value}");
				folder = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to include events from subfolders when the
		/// <see cref="Folder"/> property is set. If the <see cref="TaskEventWatcher.EventFilter.TaskName"/> property is set,
		/// this property is ignored.
		/// </summary>
		/// <value><c>true</c> if include events from subfolders; otherwise, <c>false</c>.</value>
		[DefaultValue(false), Category("Behavior"), Description("Indicates whether to include events from subfolders.")]
		public bool IncludeSubfolders
		{
			get { return includeSubfolders; }
			set
			{
				if (includeSubfolders == value) return;
				includeSubfolders = value;
				Restart();
			}
		}

		/// <summary>
		/// Gets or sets the synchronizing object.
		/// </summary>
		/// <value>
		/// The synchronizing object.
		/// </value>
		[Browsable(false), DefaultValue(null)]
		public ISynchronizeInvoke SynchronizingObject
		{
			get
			{
				if (synchronizingObject == null && DesignMode)
				{
					var so = ((IDesignerHost)GetService(typeof(IDesignerHost)))?.RootComponent as ISynchronizeInvoke;
					if (so != null)
						synchronizingObject = so;
				}
				return synchronizingObject;
			}
			set { synchronizingObject = value; }
		}

		/// <summary>
		/// Gets or sets the name of the computer that is running the Task Scheduler service that the user is connected to.
		/// </summary>
		[Category("Connection"), Description("The name of the computer to connect to."), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string TargetServer
		{
			get { return TaskService.TargetServer; }
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.Compare(value, TaskService.TargetServer, StringComparison.OrdinalIgnoreCase) == 0) return;
				TaskService.TargetServer = value;
				Restart();
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="TaskService"/> instance associated with this event watcher. Setting this value
		/// will override any values set for <see cref="TargetServer"/>, <see cref="UserAccountDomain"/>,
		/// <see cref="UserName"/>, and <see cref="UserPassword"/> and set them to those values in the supplied
		/// <see cref="TaskService"/> instance.
		/// </summary>
		/// <value>The TaskService.</value>
		[Category("Data"), Description("The TaskService for this event watcher.")]
		public TaskService TaskService
		{
			get { return ts; }
			set { ts = value; Restart(); }
		}

		/// <summary>
		/// Gets or sets the user account domain to be used when connecting to the <see cref="TargetServer"/>.
		/// </summary>
		/// <value>The user account domain.</value>
		[Category("Connection"), Description("The user account domain to be used when connecting."), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string UserAccountDomain
		{
			get { return TaskService.UserAccountDomain; }
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.Compare(value, TaskService.UserAccountDomain, StringComparison.OrdinalIgnoreCase) == 0) return;
				TaskService.UserAccountDomain = value;
				Restart();
			}
		}

		/// <summary>
		/// Gets or sets the user name to be used when connecting to the <see cref="TargetServer"/>.
		/// </summary>
		/// <value>The user name.</value>
		[Category("Connection"), Description("The user name to be used when connecting."), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string UserName
		{
			get { return TaskService.UserName; }
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.Compare(value, TaskService.UserName, StringComparison.OrdinalIgnoreCase) == 0) return;
				TaskService.UserName = value;
				Restart();
			}
		}

		/// <summary>
		/// Gets or sets the user password to be used when connecting to the <see cref="TargetServer"/>.
		/// </summary>
		/// <value>The user password.</value>
		[Category("Connection"), Description("The user password to be used when connecting."), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string UserPassword
		{
			get { return TaskService.UserPassword; }
			set
			{
				if (value == null || value.Trim() == string.Empty) value = null;
				if (string.Compare(value, TaskService.UserPassword, StringComparison.OrdinalIgnoreCase) == 0) return;
				TaskService.UserPassword = value;
				Restart();
			}
		}

		/// <summary>
		/// Gets a value indicating if watching is available.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		private bool IsHandleInvalid => IsV1 ? v1Watcher == null : watcher == null;

		private static bool IsV1 => Environment.OSVersion.Version.Major < 6;

		/// <summary>
		/// Signals the object that initialization is starting.
		/// </summary>
		public void BeginInit()
		{
			System.Diagnostics.Debug.WriteLine($"TaskEventWather: {nameof(BeginInit)}");
			initializing = true;
			var localEnabled = enabled;
			StopRaisingEvents();
			enabled = localEnabled;
			TaskService.BeginInit();
		}

		/// <summary>
		/// Signals the object that initialization is complete.
		/// </summary>
		public void EndInit()
		{
			System.Diagnostics.Debug.WriteLine($"TaskEventWather: {nameof(EndInit)}");
			initializing = false;
			TaskService.EndInit();
			if (enabled)
				StartRaisingEvents();
		}

		/// <summary>
		/// Releases the unmanaged resources used by the FileSystemWatcher and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					StopRaisingEvents();
					TaskService = null;
				}
				else
				{
					StopListening();
				}
			}
			finally
			{
				disposed = true;
				base.Dispose(disposing);
			}
		}

		/// <summary>
		/// Fires the <see cref="EventRecorded"/> event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="Microsoft.Win32.TaskScheduler.TaskEventArgs" /> instance containing the event data.</param>
		protected virtual void OnEventRecorded(object sender, TaskEventArgs e)
		{
			var h = EventRecorded;
			if (h == null) return;
			if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
				SynchronizingObject.BeginInvoke(h, new object[] { this, e });
			else
				h(sender, e);
		}

		private void InitTask(string taskPath)
		{
			Filter.TaskName = Path.GetFileNameWithoutExtension(taskPath);
			Folder = Path.GetDirectoryName(taskPath);
		}

		private bool IsSuspended() => initializing || DesignMode;

		private void ReleaseWatcher()
		{
			if (IsV1)
			{
				if (v1Watcher == null) return;
				v1Watcher.EnableRaisingEvents = false;
				v1Watcher.Changed -= Watcher_DirectoryChanged;
				v1Watcher.Created -= Watcher_DirectoryChanged;
				v1Watcher.Deleted -= Watcher_DirectoryChanged;
				v1Watcher.Renamed -= Watcher_DirectoryChanged;
				v1Watcher = null;
			}
			else
			{
				if (watcher == null) return;
				watcher.Enabled = false;
				watcher.EventRecordWritten -= Watcher_EventRecordWritten;
				watcher = null;
			}
		}

		private void Restart()
		{
			if (IsSuspended() || !enabled) return;
			System.Diagnostics.Debug.WriteLine($"TaskEventWather: {nameof(Restart)}");
			StopRaisingEvents();
			StartRaisingEvents();
		}

		private void SetupWatcher()
		{
			ReleaseWatcher();
			string taskPath = null;
			if (Filter.Wildcard == null)
				taskPath = Path.Combine(folder, Filter.TaskName);
			if (IsV1)
			{
				var di = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.System));
				string dir = di.Parent != null ? Path.Combine(di.Parent.FullName, "Tasks") : "Tasks";
				v1Watcher = new FileSystemWatcher(dir, "*.job") { NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Attributes };
				v1Watcher.Changed += Watcher_DirectoryChanged;
				v1Watcher.Created += Watcher_DirectoryChanged;
				v1Watcher.Deleted += Watcher_DirectoryChanged;
				v1Watcher.Renamed += Watcher_DirectoryChanged;
			}
			else
			{
				var log = new TaskEventLog(taskPath, Filter.EventIds, Filter.EventLevels, DateTime.UtcNow, TargetServer, UserAccountDomain, UserName, UserPassword);
				log.Query.ReverseDirection = false;
				watcher = new EventLogWatcher(log.Query);
				watcher.EventRecordWritten += Watcher_EventRecordWritten;
			}
		}

		private void StartRaisingEvents()
		{
			if (disposed)
				throw new ObjectDisposedException(GetType().Name);

			if (IsSuspended()) return;
			System.Diagnostics.Debug.WriteLine($"TaskEventWather: {nameof(StartRaisingEvents)}");
			enabled = true;
			SetupWatcher();
			if (IsV1)
				try { v1Watcher.EnableRaisingEvents = true; } catch { }
			else
				try { watcher.Enabled = true; } catch { }
		}

		private void StopListening()
		{
			enabled = false;
			ReleaseWatcher();
		}

		private void StopRaisingEvents()
		{
			System.Diagnostics.Debug.WriteLine($"TaskEventWather: {nameof(StopRaisingEvents)}");
			if (IsSuspended())
				enabled = false;
			else if (!IsHandleInvalid)
				StopListening();
		}

		private void Watcher_DirectoryChanged(object sender, FileSystemEventArgs e)
		{
			StandardTaskEventId id = StandardTaskEventId.TaskUpdated;
			if (e.ChangeType == WatcherChangeTypes.Deleted)
				id = StandardTaskEventId.TaskDeleted;
			else if (e.ChangeType == WatcherChangeTypes.Created)
				id = StandardTaskEventId.JobRegistered;
			if (lastId == id && DateTime.Now.Subtract(lastIdTime) <= MaxV1EventLapse) return;
			OnEventRecorded(this, new TaskEventArgs(new TaskEvent(Path.Combine("\\", e.Name.Replace(".job", "")), id, DateTime.Now), TaskService));
			lastId = id;
			lastIdTime = DateTime.Now;
		}

		private void Watcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
		{
			try
			{
				var taskEvent = new TaskEvent(e.EventRecord);
				System.Diagnostics.Debug.WriteLine("Task event: " + taskEvent.ToString());

				// Get the task name and folder
				if (string.IsNullOrEmpty(taskEvent.TaskPath)) return;
				int cpos = taskEvent.TaskPath.LastIndexOf('\\');
				string name = taskEvent.TaskPath.Substring(cpos + 1);
				string fld = taskEvent.TaskPath.Substring(0, cpos + 1);

				// Check folder and name filters
				if (!string.IsNullOrEmpty(Filter.TaskName) && string.Compare(Filter.TaskName, taskEvent.TaskPath, StringComparison.OrdinalIgnoreCase) != 0)
				{
					if (Filter.Wildcard != null && !Filter.Wildcard.IsMatch(name))
						return;
					if (IncludeSubfolders && !fld.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
						return;
					if (!IncludeSubfolders && string.Compare(folder, fld, StringComparison.OrdinalIgnoreCase) != 0)
						return;
				}

				OnEventRecorded(this, new TaskEventArgs(taskEvent, TaskService));
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"{nameof(Watcher_EventRecordWritten)} has failed. Error: {ex.ToString()}");
			}
		}

		/// <summary>
		/// Holds filter information for a <see cref="TaskEventWatcher"/>.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter)), Serializable]
		[PublicAPI]
		public class EventFilter
		{
			private string filter = star;
			private int[] ids;
			private int[] levels;
			private readonly TaskEventWatcher parent;

			internal EventFilter([NotNull] TaskEventWatcher parent)
			{
				this.parent = parent;
			}

			/// <summary>
			/// Gets or sets an optional array of event identifiers to use when filtering those events that will fire a <see cref="TaskEventWatcher.EventRecorded"/> event.
			/// </summary>
			/// <value>
			/// The array of event identifier filters. All know task event identifiers are declared in the <see cref="StandardTaskEventId"/> enumeration.
			/// </value>
			[DefaultValue(null), Category("Filter"), Description("An array of event identifiers to use when filtering.")]
			public int[] EventIds
			{
				get { return ids; }
				set
				{
					if (ids != value)
					{
						ids = value;
						parent.Restart();
					}
				}
			}

			/// <summary>
			/// Gets or sets an optional array of event levels to use when filtering those events that will fire a <see cref="TaskEventWatcher.EventRecorded"/> event.
			/// </summary>
			/// <value>
			/// The array of event levels. While event providers can define custom levels, most will use integers defined in the System.Diagnostics.Eventing.Reader.StandardEventLevel enumeration.
			/// </value>
			[DefaultValue(null), Category("Filter"), Description("An array of event levels to use when filtering.")]
			public int[] EventLevels
			{
				get { return levels; }
				set
				{
					if (levels != value)
					{
						levels = value;
						parent.Restart();
					}
				}
			}

			/// <summary>
			/// Gets or sets the task name, which can utilize wildcards, to look for when watching a folder.
			/// </summary>
			/// <value>A task name or wildcard.</value>
			[DefaultValue(star), Category("Filter"), Description("A task name, which can utilize wildcards, for filtering.")]
			public string TaskName
			{
				get { return filter; }
				set
				{
					if (string.IsNullOrEmpty(value))
						value = star;
					if (string.Compare(filter, value, StringComparison.OrdinalIgnoreCase) != 0)
					{
						filter = value;
						Wildcard = (value.IndexOfAny(new[] { '?', '*' }) == -1) ? null : new Wildcard(value);
						parent.Restart();
					}
				}
			}

			internal Wildcard Wildcard { get; private set; } = new Wildcard(star);

			/// <summary>
			/// Returns a <see cref="System.String" /> that represents this instance.
			/// </summary>
			/// <returns>
			/// A <see cref="System.String" /> that represents this instance.
			/// </returns>
			public override string ToString() => filter + (levels == null ? "" : " +levels") + (ids == null ? "" : " +id's");
        }
	}

#if DESIGNER
	namespace Design
	{
		internal class TaskEventWatcherDesigner : ComponentDesigner
		{
			public override void InitializeNewComponent(IDictionary defaultValues)
			{
				base.InitializeNewComponent(defaultValues);
				var refs = GetService<IReferenceService>();
				var tsColl = refs?.GetReferences(typeof(TaskService));
				System.Diagnostics.Debug.Assert(refs != null && tsColl != null && tsColl.Length > 0, "Designer couldn't find host, reference service, or existing TaskService.");
				if (tsColl != null && tsColl.Length > 0)
				{
					TaskEventWatcher tsComp = Component as TaskEventWatcher;
					TaskService ts = tsColl[0] as TaskService;
					if (tsComp != null)
						tsComp.TaskService = ts;
				}
			}

			protected virtual T GetService<T>() => (T)Component?.Site?.GetService(typeof(T));
		}
	}
#endif
}