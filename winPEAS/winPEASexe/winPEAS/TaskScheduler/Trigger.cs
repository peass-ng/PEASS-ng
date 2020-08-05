using JetBrains.Annotations;
using Microsoft.Win32.TaskScheduler.V2Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>Values for days of the week (Monday, Tuesday, etc.)</summary>
	[Flags]
	public enum DaysOfTheWeek : short
	{
		/// <summary>Sunday</summary>
		Sunday = 0x1,
		/// <summary>Monday</summary>
		Monday = 0x2,
		/// <summary>Tuesday</summary>
		Tuesday = 0x4,
		/// <summary>Wednesday</summary>
		Wednesday = 0x8,
		/// <summary>Thursday</summary>
		Thursday = 0x10,
		/// <summary>Friday</summary>
		Friday = 0x20,
		/// <summary>Saturday</summary>
		Saturday = 0x40,
		/// <summary>All days</summary>
		AllDays = 0x7F
	}

	/// <summary>Values for months of the year (January, February, etc.)</summary>
	[Flags]
	public enum MonthsOfTheYear : short
	{
		/// <summary>January</summary>
		January = 0x1,
		/// <summary>February</summary>
		February = 0x2,
		/// <summary>March</summary>
		March = 0x4,
		/// <summary>April</summary>
		April = 0x8,
		/// <summary>May</summary>
		May = 0x10,
		/// <summary>June</summary>
		June = 0x20,
		/// <summary>July</summary>
		July = 0x40,
		/// <summary>August</summary>
		August = 0x80,
		/// <summary>September</summary>
		September = 0x100,
		/// <summary>October</summary>
		October = 0x200,
		/// <summary>November</summary>
		November = 0x400,
		/// <summary>December</summary>
		December = 0x800,
		/// <summary>All months</summary>
		AllMonths = 0xFFF
	}

	/// <summary>Defines the type of triggers that can be used by tasks.</summary>
	[DefaultValue(Time)]
	public enum TaskTriggerType
	{
		/// <summary>Triggers the task when a specific event occurs. Version 1.2 only.</summary>
		Event = 0,
		/// <summary>Triggers the task at a specific time of day.</summary>
		Time = 1,
		/// <summary>Triggers the task on a daily schedule.</summary>
		Daily = 2,
		/// <summary>Triggers the task on a weekly schedule.</summary>
		Weekly = 3,
		/// <summary>Triggers the task on a monthly schedule.</summary>
		Monthly = 4,
		/// <summary>Triggers the task on a monthly day-of-week schedule.</summary>
		MonthlyDOW = 5,
		/// <summary>Triggers the task when the computer goes into an idle state.</summary>
		Idle = 6,
		/// <summary>Triggers the task when the task is registered. Version 1.2 only.</summary>
		Registration = 7,
		/// <summary>Triggers the task when the computer boots.</summary>
		Boot = 8,
		/// <summary>Triggers the task when a specific user logs on.</summary>
		Logon = 9,
		/// <summary>Triggers the task when a specific user session state changes. Version 1.2 only.</summary>
		SessionStateChange = 11,
		/// <summary>Triggers the custom trigger. Version 1.3 only.</summary>
		Custom = 12
	}

	/// <summary>Values for week of month (first, second, ..., last)</summary>
	[Flags]
	public enum WhichWeek : short
	{
		/// <summary>First week of the month</summary>
		FirstWeek = 1,
		/// <summary>Second week of the month</summary>
		SecondWeek = 2,
		/// <summary>Third week of the month</summary>
		ThirdWeek = 4,
		/// <summary>Fourth week of the month</summary>
		FourthWeek = 8,
		/// <summary>Last week of the month</summary>
		LastWeek = 0x10,
		/// <summary>Every week of the month</summary>
		AllWeeks = 0x1F
	}

	/// <summary>Interface that categorizes the trigger as a calendar trigger.</summary>
	public interface ICalendarTrigger { }

	/// <summary>Interface for triggers that support a delay.</summary>
	public interface ITriggerDelay
	{
		/// <summary>Gets or sets a value that indicates the amount of time before the task is started.</summary>
		/// <value>The delay duration.</value>
		TimeSpan Delay { get; set; }
	}

	/// <summary>Interface for triggers that support a user identifier.</summary>
	public interface ITriggerUserId
	{
		/// <summary>Gets or sets the user for the <see cref="Trigger"/>.</summary>
		string UserId { get; set; }
	}

	/// <summary>Represents a trigger that starts a task when the system is booted.</summary>
	/// <remarks>A BootTrigger will fire when the system starts. It can only be delayed. All triggers that support a delay implement the ITriggerDelay interface.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Create trigger that fires 5 minutes after the system starts.
	/// BootTrigger bt = new BootTrigger();
	/// bt.Delay = TimeSpan.FromMinutes(5);  // V2 only
	/// ]]>
	/// </code>
	/// </example>
	public sealed class BootTrigger : Trigger, ITriggerDelay
	{
		/// <summary>Creates an unbound instance of a <see cref="BootTrigger"/>.</summary>
		public BootTrigger() : base(TaskTriggerType.Boot) { }

		internal BootTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.OnSystemStart) { }

		internal BootTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets a value that indicates the amount of time between when the system is booted and when the task is started.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan Delay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((IBootTrigger)v2Trigger).Delay) : GetUnboundValueOrDefault(nameof(Delay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((IBootTrigger)v2Trigger).Delay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(Delay)] = value;
			}
		}

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString() => winPEAS.Properties.Resources.TriggerBoot1;
	}

	/// <summary>
	/// Represents a custom trigger. This class is based on undocumented features and may change. <note>This type of trigger is only available for reading custom
	/// triggers. It cannot be used to create custom triggers.</note>
	/// </summary>
	public sealed class CustomTrigger : Trigger, ITriggerDelay
	{
		private readonly NamedValueCollection nvc = new NamedValueCollection();
		private TimeSpan delay = TimeSpan.MinValue;
		private string name = string.Empty;

		internal CustomTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets a value that indicates the amount of time between the trigger events and when the task is started.</summary>
		/// <exception cref="System.NotImplementedException">This value cannot be set.</exception>
		public TimeSpan Delay
		{
			get => delay;
			set => throw new NotImplementedException();
		}

		/// <summary>Clones this instance.</summary>
		/// <returns>This method will always throw an exception.</returns>
		/// <exception cref="System.InvalidOperationException">CustomTrigger cannot be cloned due to OS restrictions.</exception>
		public override object Clone() => throw new InvalidOperationException("CustomTrigger cannot be cloned due to OS restrictions.");

		/// <summary>Updates custom properties from XML provided by definition.</summary>
		/// <param name="xml">The XML from the TaskDefinition.</param>
		internal void UpdateFromXml(string xml)
		{
			nvc.Clear();
			try
			{
				var xmlDoc = new System.Xml.XmlDocument();
				xmlDoc.LoadXml(xml);
				var nsmgr = new System.Xml.XmlNamespaceManager(xmlDoc.NameTable);
				nsmgr.AddNamespace("n", "http://schemas.microsoft.com/windows/2004/02/mit/task");
				var elem = xmlDoc.DocumentElement?.SelectSingleNode("n:Triggers/*[@id='" + Id + "']", nsmgr);
				if (elem == null)
				{
					var nodes = xmlDoc.GetElementsByTagName("WnfStateChangeTrigger");
					if (nodes.Count == 1)
						elem = nodes[0];
				}

				if (elem == null) return;

				name = elem.LocalName;
				foreach (System.Xml.XmlNode node in elem.ChildNodes)
				{
					switch (node.LocalName)
					{
						case "Delay":
							delay = Task.StringToTimeSpan(node.InnerText);
							break;

						case "StartBoundary":
						case "Enabled":
						case "EndBoundary":
						case "ExecutionTimeLimit":
							break;

						default:
							nvc.Add(node.LocalName, node.InnerText);
							break;
					}
				}
			}
			catch { /* ignored */ }
		}

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString() => winPEAS.Properties.Resources.TriggerCustom1;
	}

	/// <summary>
	/// Represents a trigger that starts a task based on a daily schedule. For example, the task starts at a specific time every day, every other day, every
	/// third day, and so on.
	/// </summary>
	/// <remarks>A DailyTrigger will fire at a specified time every day or interval of days.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Create a trigger that runs every other day and will start randomly between 10 a.m. and 12 p.m.
	/// DailyTrigger dt = new DailyTrigger();
	/// dt.StartBoundary = DateTime.Today + TimeSpan.FromHours(10);
	/// dt.DaysInterval = 2;
	/// dt.RandomDelay = TimeSpan.FromHours(2); // V2 only
	/// ]]>
	/// </code>
	/// </example>
	[XmlRoot("CalendarTrigger", Namespace = TaskDefinition.tns, IsNullable = false)]
	public sealed class DailyTrigger : Trigger, ICalendarTrigger, ITriggerDelay, IXmlSerializable
	{
		/// <summary>Creates an unbound instance of a <see cref="DailyTrigger"/>.</summary>
		/// <param name="daysInterval">Interval between the days in the schedule.</param>
		public DailyTrigger(short daysInterval = 1) : base(TaskTriggerType.Daily) { DaysInterval = daysInterval; }

		internal DailyTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.RunDaily)
		{
			if (v1TriggerData.Data.daily.DaysInterval == 0)
				v1TriggerData.Data.daily.DaysInterval = 1;
		}

		internal DailyTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Sets or retrieves the interval between the days in the schedule.</summary>
		[DefaultValue(1)]
		public short DaysInterval
		{
			get
			{
				if (v2Trigger != null)
					return ((IDailyTrigger)v2Trigger).DaysInterval;
				return (short)v1TriggerData.Data.daily.DaysInterval;
			}
			set
			{
				if (v2Trigger != null)
					((IDailyTrigger)v2Trigger).DaysInterval = value;
				else
				{
					v1TriggerData.Data.daily.DaysInterval = (ushort)value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(DaysInterval)] = value;
				}
			}
		}

		/// <summary>Gets or sets a delay time that is randomly added to the start time of the trigger.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan RandomDelay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((IDailyTrigger)v2Trigger).RandomDelay) : GetUnboundValueOrDefault(nameof(RandomDelay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((IDailyTrigger)v2Trigger).RandomDelay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(RandomDelay)] = value;
			}
		}

		/// <summary>Gets or sets a value that indicates the amount of time before the task is started.</summary>
		/// <value>The delay duration.</value>
		TimeSpan ITriggerDelay.Delay
		{
			get => RandomDelay;
			set => RandomDelay = value;
		}

		/// <summary>
		/// Copies the properties from another <see cref="Trigger"/> the current instance. This will not copy any properties associated with any derived triggers
		/// except those supporting the <see cref="ITriggerDelay"/> interface.
		/// </summary>
		/// <param name="sourceTrigger">The source <see cref="Trigger"/>.</param>
		public override void CopyProperties(Trigger sourceTrigger)
		{
			base.CopyProperties(sourceTrigger);
			if (sourceTrigger is DailyTrigger dt)
			{
				DaysInterval = dt.DaysInterval;
			}
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public override bool Equals(Trigger other) => other is DailyTrigger dt && base.Equals(dt) && DaysInterval == dt.DaysInterval;

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { CalendarTrigger.ReadXml(reader, this, ReadMyXml); }

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { CalendarTrigger.WriteXml(writer, this, WriteMyXml); }

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString() => DaysInterval == 1 ?
			string.Format(winPEAS.Properties.Resources.TriggerDaily1, AdjustToLocal(StartBoundary)) :
			string.Format(winPEAS.Properties.Resources.TriggerDaily2, AdjustToLocal(StartBoundary), DaysInterval);

		private void ReadMyXml(System.Xml.XmlReader reader)
		{
			reader.ReadStartElement("ScheduleByDay");
			if (reader.MoveToContent() == System.Xml.XmlNodeType.Element && reader.LocalName == "DaysInterval")
				// ReSharper disable once AssignNullToNotNullAttribute
				DaysInterval = (short)reader.ReadElementContentAs(typeof(short), null);
			reader.Read();
			reader.ReadEndElement();
		}

		private void WriteMyXml(System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement("ScheduleByDay");
			writer.WriteElementString("DaysInterval", DaysInterval.ToString());
			writer.WriteEndElement();
		}
	}

	/// <summary>
	/// Represents a trigger that starts a task when a system event occurs. <note>Only available for Task Scheduler 2.0 on Windows Vista or Windows Server 2003
	/// and later.</note>
	/// </summary>
	/// <remarks>The EventTrigger runs when a system event fires.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Create a trigger that will fire whenever a level 2 system event fires.
	/// EventTrigger eTrigger = new EventTrigger();
	/// eTrigger.Subscription = @"<QueryList><Query Id='1'><Select Path='System'>*[System/Level=2]</Select></Query></QueryList>";
	/// eTrigger.ValueQueries.Add("Name", "Value");
	/// ]]>
	/// </code>
	/// </example>
	[XmlType(IncludeInSchema = false)]
	public sealed class EventTrigger : Trigger, ITriggerDelay
	{
		private NamedValueCollection nvc;

		/// <summary>Creates an unbound instance of a <see cref="EventTrigger"/>.</summary>
		public EventTrigger() : base(TaskTriggerType.Event) { }

	    internal EventTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets a value that indicates the amount of time between when the system is booted and when the task is started.</summary>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		public TimeSpan Delay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((IEventTrigger)v2Trigger).Delay) : GetUnboundValueOrDefault(nameof(Delay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((IEventTrigger)v2Trigger).Delay = Task.TimeSpanToString(value);
				else
					unboundValues[nameof(Delay)] = value;
			}
		}

		/// <summary>Gets or sets the XPath query string that identifies the event that fires the trigger.</summary>
		[DefaultValue(null)]
		public string Subscription
		{
			get => v2Trigger != null ? ((IEventTrigger)v2Trigger).Subscription : GetUnboundValueOrDefault<string>(nameof(Subscription));
			set
			{
				if (v2Trigger != null)
					((IEventTrigger)v2Trigger).Subscription = value;
				else
					unboundValues[nameof(Subscription)] = value;
			}
		}

		/// <summary>
		/// Gets a collection of named XPath queries. Each query in the collection is applied to the last matching event XML returned from the subscription query
		/// specified in the Subscription property. The name of the query can be used as a variable in the message of a <see cref="ShowMessageAction"/> action.
		/// </summary>
		[XmlArray]
		[XmlArrayItem("Value", typeof(NameValuePair))]
		public NamedValueCollection ValueQueries => nvc ?? (nvc = v2Trigger == null ? new NamedValueCollection() : new NamedValueCollection(((IEventTrigger)v2Trigger).ValueQueries));

		/// <summary>
		/// Copies the properties from another <see cref="Trigger"/> the current instance. This will not copy any properties associated with any derived triggers
		/// except those supporting the <see cref="ITriggerDelay"/> interface.
		/// </summary>
		/// <param name="sourceTrigger">The source <see cref="Trigger"/>.</param>
		public override void CopyProperties(Trigger sourceTrigger)
		{
			base.CopyProperties(sourceTrigger);
			if (sourceTrigger is EventTrigger et)
			{
				Subscription = et.Subscription;
				et.ValueQueries.CopyTo(ValueQueries);
			}
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public override bool Equals(Trigger other) => other is EventTrigger et && base.Equals(et) && Subscription == et.Subscription;

		/// <summary>Gets basic event information.</summary>
		/// <param name="log">The event's log.</param>
		/// <param name="source">The event's source. Can be <c>null</c>.</param>
		/// <param name="eventId">The event's id. Can be <c>null</c>.</param>
		/// <returns><c>true</c> if subscription represents a basic event, <c>false</c> if not.</returns>
		public bool GetBasic(out string log, out string source, out int? eventId)
		{
			log = source = null;
			eventId = null;
			if (!string.IsNullOrEmpty(Subscription))
			{
				using (var str = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(Subscription)))
				{
					using (var rdr = new System.Xml.XmlTextReader(str))
					{
						rdr.WhitespaceHandling = System.Xml.WhitespaceHandling.None;
						try
						{
							rdr.MoveToContent();
							rdr.ReadStartElement("QueryList");
							if (rdr.Name == "Query" && rdr.MoveToAttribute("Path"))
							{
								var path = rdr.Value;
								if (rdr.MoveToElement() && rdr.ReadToDescendant("Select") && path.Equals(rdr["Path"], StringComparison.InvariantCultureIgnoreCase))
								{
									var content = rdr.ReadString();
									var m = System.Text.RegularExpressions.Regex.Match(content,
										@"\*(?:\[System\[(?:Provider\[\@Name='(?<s>[^']+)'\])?(?:\s+and\s+)?(?:EventID=(?<e>\d+))?\]\])",
										System.Text.RegularExpressions.RegexOptions.IgnoreCase |
										System.Text.RegularExpressions.RegexOptions.Compiled |
										System.Text.RegularExpressions.RegexOptions.Singleline |
										System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);
									if (m.Success)
									{
										log = path;
										if (m.Groups["s"].Success)
											source = m.Groups["s"].Value;
										if (m.Groups["e"].Success)
											eventId = Convert.ToInt32(m.Groups["e"].Value);
										return true;
									}
								}
							}
						}
						catch { /* ignored */ }
					}
				}
			}
			return false;
		}

		internal override void Bind(ITaskDefinition iTaskDef)
		{
			base.Bind(iTaskDef);
			nvc?.Bind(((IEventTrigger)v2Trigger).ValueQueries);
		}

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString()
		{
			if (!GetBasic(out var log, out var source, out var id))
				return winPEAS.Properties.Resources.TriggerEvent1;
			var sb = new StringBuilder();
			sb.AppendFormat(winPEAS.Properties.Resources.TriggerEventBasic1, log);
			if (!string.IsNullOrEmpty(source))
				sb.AppendFormat(winPEAS.Properties.Resources.TriggerEventBasic2, source);
			if (id.HasValue)
				sb.AppendFormat(winPEAS.Properties.Resources.TriggerEventBasic3, id.Value);
			return sb.ToString();
		}
	}

	/// <summary>
	/// Represents a trigger that starts a task when the computer goes into an idle state. For information about idle conditions, see Task Idle Conditions.
	/// </summary>
	/// <remarks>
	/// An IdleTrigger will fire when the system becomes idle. It is generally a good practice to set a limit on how long it can run using the ExecutionTimeLimit property.
	/// </remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// IdleTrigger it = new IdleTrigger();
	/// ]]>
	/// </code>
	/// </example>
	public sealed class IdleTrigger : Trigger
	{
		/// <summary>Creates an unbound instance of a <see cref="IdleTrigger"/>.</summary>
		public IdleTrigger() : base(TaskTriggerType.Idle) { }

		internal IdleTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.OnIdle) { }

		internal IdleTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString() => winPEAS.Properties.Resources.TriggerIdle1;
	}

	/// <summary>
	/// Represents a trigger that starts a task when a user logs on. When the Task Scheduler service starts, all logged-on users are enumerated and any tasks
	/// registered with logon triggers that match the logged on user are run. Not available on Task Scheduler 1.0.
	/// </summary>
	/// <remarks>A LogonTrigger will fire after a user logs on. It can only be delayed. Under V2, you can specify which user it applies to.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Add a general logon trigger
	/// LogonTrigger lt1 = new LogonTrigger();
	///
	/// // V2 only: Add a delayed logon trigger for a specific user
	/// LogonTrigger lt2 = new LogonTrigger { UserId = "LocalUser" };
	/// lt2.Delay = TimeSpan.FromMinutes(15);
	/// ]]>
	/// </code>
	/// </example>
	public sealed class LogonTrigger : Trigger, ITriggerDelay, ITriggerUserId
	{
		/// <summary>Creates an unbound instance of a <see cref="LogonTrigger"/>.</summary>
		public LogonTrigger() : base(TaskTriggerType.Logon) { }

		internal LogonTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.OnLogon) { }

		internal LogonTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets a value that indicates the amount of time between when the system is booted and when the task is started.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan Delay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((ILogonTrigger)v2Trigger).Delay) : GetUnboundValueOrDefault(nameof(Delay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((ILogonTrigger)v2Trigger).Delay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(Delay)] = value;
			}
		}

		/// <summary>
		/// <para>Gets or sets The identifier of the user. For example, "MyDomain\MyName" or for a local account, "Administrator".</para>
		/// <para>This property can be in one of the following formats:</para>
		/// <para>• User name or SID: The task is started when the user logs on to the computer.</para>
		/// <para>• NULL: The task is started when any user logs on to the computer.</para>
		/// </summary>
		/// <remarks>
		/// If you want a task to be triggered when any member of a group logs on to the computer rather than when a specific user logs on, then do not assign a
		/// value to the LogonTrigger.UserId property. Instead, create a logon trigger with an empty LogonTrigger.UserId property and assign a value to the
		/// principal for the task using the Principal.GroupId property.
		/// </remarks>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(null)]
		[XmlIgnore]
		public string UserId
		{
			get => v2Trigger != null ? ((ILogonTrigger)v2Trigger).UserId : GetUnboundValueOrDefault<string>(nameof(UserId));
			set
			{
				if (v2Trigger != null)
					((ILogonTrigger)v2Trigger).UserId = value;
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(UserId)] = value;
			}
		}

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString()
		{
			var user = string.IsNullOrEmpty(UserId) ? winPEAS.Properties.Resources.TriggerAnyUser : UserId;
			return string.Format(winPEAS.Properties.Resources.TriggerLogon1, user);
		}
	}

	/// <summary>
	/// Represents a trigger that starts a task on a monthly day-of-week schedule. For example, the task starts on every first Thursday, May through October.
	/// </summary>
	[XmlRoot("CalendarTrigger", Namespace = TaskDefinition.tns, IsNullable = false)]
	public sealed class MonthlyDOWTrigger : Trigger, ICalendarTrigger, ITriggerDelay, IXmlSerializable
	{
		/// <summary>Creates an unbound instance of a <see cref="MonthlyDOWTrigger"/>.</summary>
		/// <param name="daysOfWeek">The days of the week.</param>
		/// <param name="monthsOfYear">The months of the year.</param>
		/// <param name="weeksOfMonth">The weeks of the month.</param>
		public MonthlyDOWTrigger(DaysOfTheWeek daysOfWeek = DaysOfTheWeek.Sunday, MonthsOfTheYear monthsOfYear = MonthsOfTheYear.AllMonths, WhichWeek weeksOfMonth = WhichWeek.FirstWeek) : base(TaskTriggerType.MonthlyDOW)
		{
			DaysOfWeek = daysOfWeek;
			MonthsOfYear = monthsOfYear;
			WeeksOfMonth = weeksOfMonth;
		}

		internal MonthlyDOWTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.RunMonthlyDOW)
		{
			if (v1TriggerData.Data.monthlyDOW.Months == 0)
				v1TriggerData.Data.monthlyDOW.Months = MonthsOfTheYear.AllMonths;
			if (v1TriggerData.Data.monthlyDOW.DaysOfTheWeek == 0)
				v1TriggerData.Data.monthlyDOW.DaysOfTheWeek = DaysOfTheWeek.Sunday;
		}

		internal MonthlyDOWTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets the days of the week during which the task runs.</summary>
		[DefaultValue(0)]
		public DaysOfTheWeek DaysOfWeek
		{
			get => v2Trigger != null
				? (DaysOfTheWeek)((IMonthlyDOWTrigger)v2Trigger).DaysOfWeek
				: v1TriggerData.Data.monthlyDOW.DaysOfTheWeek;
			set
			{
				if (v2Trigger != null)
					((IMonthlyDOWTrigger)v2Trigger).DaysOfWeek = (short)value;
				else
				{
					v1TriggerData.Data.monthlyDOW.DaysOfTheWeek = value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(DaysOfWeek)] = (short)value;
				}
			}
		}

		/// <summary>Gets or sets the months of the year during which the task runs.</summary>
		[DefaultValue(0)]
		public MonthsOfTheYear MonthsOfYear
		{
			get => v2Trigger != null
				? (MonthsOfTheYear)((IMonthlyDOWTrigger)v2Trigger).MonthsOfYear
				: v1TriggerData.Data.monthlyDOW.Months;
			set
			{
				if (v2Trigger != null)
					((IMonthlyDOWTrigger)v2Trigger).MonthsOfYear = (short)value;
				else
				{
					v1TriggerData.Data.monthlyDOW.Months = value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(MonthsOfYear)] = (short)value;
				}
			}
		}

		/// <summary>Gets or sets a delay time that is randomly added to the start time of the trigger.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan RandomDelay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((IMonthlyDOWTrigger)v2Trigger).RandomDelay) : GetUnboundValueOrDefault(nameof(RandomDelay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((IMonthlyDOWTrigger)v2Trigger).RandomDelay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(RandomDelay)] = value;
			}
		}

		/// <summary>Gets or sets a Boolean value that indicates that the task runs on the last week of the month.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(false)]
		[XmlIgnore]
		public bool RunOnLastWeekOfMonth
		{
			get => ((IMonthlyDOWTrigger)v2Trigger)?.RunOnLastWeekOfMonth ?? GetUnboundValueOrDefault(nameof(RunOnLastWeekOfMonth), false);
			set
			{
				if (v2Trigger != null)
					((IMonthlyDOWTrigger)v2Trigger).RunOnLastWeekOfMonth = value;
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(RunOnLastWeekOfMonth)] = value;
			}
		}

		/// <summary>Gets or sets the weeks of the month during which the task runs.</summary>
		[DefaultValue(0)]
		public WhichWeek WeeksOfMonth
		{
			get
			{
				if (v2Trigger == null)
					return v1Trigger != null
						? v1TriggerData.Data.monthlyDOW.V2WhichWeek
						: GetUnboundValueOrDefault(nameof(WeeksOfMonth), WhichWeek.FirstWeek);
				var ww = (WhichWeek)((IMonthlyDOWTrigger)v2Trigger).WeeksOfMonth;
				// Following addition give accurate results for confusing RunOnLastWeekOfMonth property (thanks kbergeron)
				if (((IMonthlyDOWTrigger)v2Trigger).RunOnLastWeekOfMonth)
					ww |= WhichWeek.LastWeek;
				return ww;
			}
			set
			{
				// In Windows 10, the native library no longer acknowledges the LastWeek value and requires the RunOnLastWeekOfMonth to
				// be expressly set. I think this is wrong so I am correcting their changed functionality. (thanks @SebastiaanPolfliet)
				if (value.IsFlagSet(WhichWeek.LastWeek))
					RunOnLastWeekOfMonth = true;
				if (v2Trigger != null)
				{
					((IMonthlyDOWTrigger)v2Trigger).WeeksOfMonth = (short)value;
				}
				else
				{
					try
					{
						v1TriggerData.Data.monthlyDOW.V2WhichWeek = value;
					}
					catch (NotV1SupportedException)
					{
						if (v1Trigger != null) throw;
					}
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(WeeksOfMonth)] = (short)value;
				}
			}
		}

		/// <summary>Gets or sets a value that indicates the amount of time before the task is started.</summary>
		/// <value>The delay duration.</value>
		TimeSpan ITriggerDelay.Delay
		{
			get => RandomDelay;
			set => RandomDelay = value;
		}

		/// <summary>
		/// Copies the properties from another <see cref="Trigger"/> the current instance. This will not copy any properties associated with any derived triggers
		/// except those supporting the <see cref="ITriggerDelay"/> interface.
		/// </summary>
		/// <param name="sourceTrigger">The source <see cref="Trigger"/>.</param>
		public override void CopyProperties(Trigger sourceTrigger)
		{
			base.CopyProperties(sourceTrigger);
			if (sourceTrigger is MonthlyDOWTrigger mt)
			{
				DaysOfWeek = mt.DaysOfWeek;
				MonthsOfYear = mt.MonthsOfYear;
				try { RunOnLastWeekOfMonth = mt.RunOnLastWeekOfMonth; } catch { /* ignored */ }
				WeeksOfMonth = mt.WeeksOfMonth;
			}
			if (sourceTrigger is MonthlyTrigger m)
				MonthsOfYear = m.MonthsOfYear;
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public override bool Equals(Trigger other) => other is MonthlyDOWTrigger mt && base.Equals(other) && DaysOfWeek == mt.DaysOfWeek &&
			MonthsOfYear == mt.MonthsOfYear && WeeksOfMonth == mt.WeeksOfMonth && v1Trigger == null && RunOnLastWeekOfMonth == mt.RunOnLastWeekOfMonth;

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { CalendarTrigger.ReadXml(reader, this, ReadMyXml); }

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { CalendarTrigger.WriteXml(writer, this, WriteMyXml); }

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString()
		{
			var ww = TaskEnumGlobalizer.GetString(WeeksOfMonth);
			var days = TaskEnumGlobalizer.GetString(DaysOfWeek);
			var months = TaskEnumGlobalizer.GetString(MonthsOfYear);
			return string.Format(winPEAS.Properties.Resources.TriggerMonthlyDOW1, AdjustToLocal(StartBoundary), ww, days, months);
		}

		/// <summary>Reads the subclass XML for V1 streams.</summary>
		/// <param name="reader">The reader.</param>
		private void ReadMyXml([NotNull] System.Xml.XmlReader reader)
		{
			reader.ReadStartElement("ScheduleByMonthDayOfWeek");
			while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
			{
				switch (reader.LocalName)
				{
					case "Weeks":
						reader.Read();
						while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
						{
							if (reader.LocalName == "Week")
							{
								var wk = reader.ReadElementContentAsString();
								if (wk == "Last")
									WeeksOfMonth = WhichWeek.LastWeek;
								else
								{
									switch (Int32.Parse(wk))
									{
										case 1:
											WeeksOfMonth = WhichWeek.FirstWeek;
											break;

										case 2:
											WeeksOfMonth = WhichWeek.SecondWeek;
											break;

										case 3:
											WeeksOfMonth = WhichWeek.ThirdWeek;
											break;

										case 4:
											WeeksOfMonth = WhichWeek.FourthWeek;
											break;

										default:
											throw new System.Xml.XmlException("Week element must contain a 1-4 or Last as content.");
									}
								}
							}
						}
						reader.ReadEndElement();
						break;

					case "DaysOfWeek":
						reader.Read();
						DaysOfWeek = 0;
						while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
						{
							try
							{
								DaysOfWeek |= (DaysOfTheWeek)Enum.Parse(typeof(DaysOfTheWeek), reader.LocalName);
							}
							catch
							{
								throw new System.Xml.XmlException("Invalid days of the week element.");
							}
							reader.Read();
						}
						reader.ReadEndElement();
						break;

					case "Months":
						reader.Read();
						MonthsOfYear = 0;
						while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
						{
							try
							{
								MonthsOfYear |= (MonthsOfTheYear)Enum.Parse(typeof(MonthsOfTheYear), reader.LocalName);
							}
							catch
							{
								throw new System.Xml.XmlException("Invalid months of the year element.");
							}
							reader.Read();
						}
						reader.ReadEndElement();
						break;

					default:
						reader.Skip();
						break;
				}
			}
			reader.ReadEndElement();
		}

		/// <summary>Writes the subclass XML for V1 streams.</summary>
		/// <param name="writer">The writer.</param>
		private void WriteMyXml([NotNull] System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement("ScheduleByMonthDayOfWeek");

			writer.WriteStartElement("Weeks");
			if ((WeeksOfMonth & WhichWeek.FirstWeek) == WhichWeek.FirstWeek)
				writer.WriteElementString("Week", "1");
			if ((WeeksOfMonth & WhichWeek.SecondWeek) == WhichWeek.SecondWeek)
				writer.WriteElementString("Week", "2");
			if ((WeeksOfMonth & WhichWeek.ThirdWeek) == WhichWeek.ThirdWeek)
				writer.WriteElementString("Week", "3");
			if ((WeeksOfMonth & WhichWeek.FourthWeek) == WhichWeek.FourthWeek)
				writer.WriteElementString("Week", "4");
			if ((WeeksOfMonth & WhichWeek.LastWeek) == WhichWeek.LastWeek)
				writer.WriteElementString("Week", "Last");
			writer.WriteEndElement();

			writer.WriteStartElement("DaysOfWeek");
			foreach (DaysOfTheWeek e in Enum.GetValues(typeof(DaysOfTheWeek)))
				if (e != DaysOfTheWeek.AllDays && (DaysOfWeek & e) == e)
					writer.WriteElementString(e.ToString(), null);
			writer.WriteEndElement();

			writer.WriteStartElement("Months");
			foreach (MonthsOfTheYear e in Enum.GetValues(typeof(MonthsOfTheYear)))
				if (e != MonthsOfTheYear.AllMonths && (MonthsOfYear & e) == e)
					writer.WriteElementString(e.ToString(), null);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}
	}

	/// <summary>Represents a trigger that starts a job based on a monthly schedule. For example, the task starts on specific days of specific months.</summary>
	[XmlRoot("CalendarTrigger", Namespace = TaskDefinition.tns, IsNullable = false)]
	public sealed class MonthlyTrigger : Trigger, ICalendarTrigger, ITriggerDelay, IXmlSerializable
	{
		/// <summary>Creates an unbound instance of a <see cref="MonthlyTrigger"/>.</summary>
		/// <param name="dayOfMonth">
		/// The day of the month. This must be a value between 1 and 32. If this value is set to 32, then the
		/// <see cref="RunOnLastDayOfMonth"/> value will be set and no days will be added regardless of the month.
		/// </param>
		/// <param name="monthsOfYear">The months of the year.</param>
		public MonthlyTrigger(int dayOfMonth = 1, MonthsOfTheYear monthsOfYear = MonthsOfTheYear.AllMonths) : base(TaskTriggerType.Monthly)
		{
			if (dayOfMonth < 1 || dayOfMonth > 32) throw new ArgumentOutOfRangeException(nameof(dayOfMonth));
			if (!monthsOfYear.IsValidFlagValue()) throw new ArgumentOutOfRangeException(nameof(monthsOfYear));
			if (dayOfMonth == 32)
			{
				DaysOfMonth = new int[0];
				RunOnLastDayOfMonth = true;
			}
			else
				DaysOfMonth = new[] { dayOfMonth };
			MonthsOfYear = monthsOfYear;
		}

		internal MonthlyTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.RunMonthly)
		{
			if (v1TriggerData.Data.monthlyDate.Months == 0)
				v1TriggerData.Data.monthlyDate.Months = MonthsOfTheYear.AllMonths;
			if (v1TriggerData.Data.monthlyDate.Days == 0)
				v1TriggerData.Data.monthlyDate.Days = 1;
		}

		internal MonthlyTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets the days of the month during which the task runs.</summary>
		public int[] DaysOfMonth
		{
			get => v2Trigger != null ? MaskToIndices(((IMonthlyTrigger)v2Trigger).DaysOfMonth) : MaskToIndices((int)v1TriggerData.Data.monthlyDate.Days);
			set
			{
				var mask = IndicesToMask(value);
				if (v2Trigger != null)
					((IMonthlyTrigger)v2Trigger).DaysOfMonth = mask;
				else
				{
					v1TriggerData.Data.monthlyDate.Days = (uint)mask;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(DaysOfMonth)] = mask;
				}
			}
		}

		/// <summary>Gets or sets the months of the year during which the task runs.</summary>
		[DefaultValue(0)]
		public MonthsOfTheYear MonthsOfYear
		{
			get => v2Trigger != null
				? (MonthsOfTheYear)((IMonthlyTrigger)v2Trigger).MonthsOfYear
				: v1TriggerData.Data.monthlyDOW.Months;
			set
			{
				if (v2Trigger != null)
					((IMonthlyTrigger)v2Trigger).MonthsOfYear = (short)value;
				else
				{
					v1TriggerData.Data.monthlyDOW.Months = value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(MonthsOfYear)] = (short)value;
				}
			}
		}

		/// <summary>Gets or sets a delay time that is randomly added to the start time of the trigger.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan RandomDelay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((IMonthlyTrigger)v2Trigger).RandomDelay) : GetUnboundValueOrDefault(nameof(RandomDelay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((IMonthlyTrigger)v2Trigger).RandomDelay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(RandomDelay)] = value;
			}
		}

		/// <summary>Gets or sets a Boolean value that indicates that the task runs on the last day of the month.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(false)]
		[XmlIgnore]
		public bool RunOnLastDayOfMonth
		{
			get => ((IMonthlyTrigger)v2Trigger)?.RunOnLastDayOfMonth ?? GetUnboundValueOrDefault(nameof(RunOnLastDayOfMonth), false);
			set
			{
				if (v2Trigger != null)
					((IMonthlyTrigger)v2Trigger).RunOnLastDayOfMonth = value;
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(RunOnLastDayOfMonth)] = value;
			}
		}

		/// <summary>Gets or sets a value that indicates the amount of time before the task is started.</summary>
		/// <value>The delay duration.</value>
		TimeSpan ITriggerDelay.Delay
		{
			get => RandomDelay;
			set => RandomDelay = value;
		}

		/// <summary>
		/// Copies the properties from another <see cref="Trigger"/> the current instance. This will not copy any properties associated with any derived triggers
		/// except those supporting the <see cref="ITriggerDelay"/> interface.
		/// </summary>
		/// <param name="sourceTrigger">The source <see cref="Trigger"/>.</param>
		public override void CopyProperties(Trigger sourceTrigger)
		{
			base.CopyProperties(sourceTrigger);
			if (sourceTrigger is MonthlyTrigger mt)
			{
				DaysOfMonth = mt.DaysOfMonth;
				MonthsOfYear = mt.MonthsOfYear;
				try { RunOnLastDayOfMonth = mt.RunOnLastDayOfMonth; } catch { /* ignored */ }
			}
			if (sourceTrigger is MonthlyDOWTrigger mdt)
				MonthsOfYear = mdt.MonthsOfYear;
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public override bool Equals(Trigger other) => other is MonthlyTrigger mt && base.Equals(mt) && ListsEqual(DaysOfMonth, mt.DaysOfMonth) &&
			MonthsOfYear == mt.MonthsOfYear && v1Trigger == null && RunOnLastDayOfMonth == mt.RunOnLastDayOfMonth;

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { CalendarTrigger.ReadXml(reader, this, ReadMyXml); }

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { CalendarTrigger.WriteXml(writer, this, WriteMyXml); }

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString()
		{
			var days = string.Join(winPEAS.Properties.Resources.ListSeparator, Array.ConvertAll(DaysOfMonth, i => i.ToString()));
			if (RunOnLastDayOfMonth)
				days += (days.Length == 0 ? "" : winPEAS.Properties.Resources.ListSeparator) + winPEAS.Properties.Resources.WWLastWeek;
			var months = TaskEnumGlobalizer.GetString(MonthsOfYear);
			return string.Format(winPEAS.Properties.Resources.TriggerMonthly1, AdjustToLocal(StartBoundary), days, months);
		}

		/// <summary>
		/// Converts an array of bit indices into a mask with bits turned ON at every index contained in the array. Indices must be from 1 to 32 and bits are
		/// numbered the same.
		/// </summary>
		/// <param name="indices">An array with an element for each bit of the mask which is ON.</param>
		/// <returns>An integer to be interpreted as a mask.</returns>
		private static int IndicesToMask(int[] indices)
		{
			if (indices is null || indices.Length == 0) return 0;
			var mask = 0;
			foreach (var index in indices)
			{
				if (index < 1 || index > 31) throw new ArgumentException("Days must be in the range 1..31");
				mask = mask | 1 << (index - 1);
			}
			return mask;
		}

		/// <summary>Compares two collections.</summary>
		/// <typeparam name="T">Item type of collections.</typeparam>
		/// <param name="left">The first collection.</param>
		/// <param name="right">The second collection</param>
		/// <returns><c>true</c> if the collections values are equal; <c>false</c> otherwise.</returns>
		private static bool ListsEqual<T>(ICollection<T> left, ICollection<T> right) where T : IComparable
		{
			if (left == null && right == null) return true;
			if (left == null || right == null) return false;
			if (left.Count != right.Count) return false;
			List<T> l1 = new List<T>(left), l2 = new List<T>(right);
			l1.Sort(); l2.Sort();
			for (var i = 0; i < l1.Count; i++)
				if (l1[i].CompareTo(l2[i]) != 0) return false;
			return true;
		}

		/// <summary>
		/// Convert an integer representing a mask to an array where each element contains the index of a bit that is ON in the mask. Bits are considered to
		/// number from 1 to 32.
		/// </summary>
		/// <param name="mask">An integer to be interpreted as a mask.</param>
		/// <returns>An array with an element for each bit of the mask which is ON.</returns>
		private static int[] MaskToIndices(int mask)
		{
			//count bits in mask
			var cnt = 0;
			for (var i = 0; mask >> i > 0; i++)
				cnt = cnt + (1 & (mask >> i));
			//allocate return array with one entry for each bit
			var indices = new int[cnt];
			//fill array with bit indices
			cnt = 0;
			for (var i = 0; mask >> i > 0; i++)
				if ((1 & (mask >> i)) == 1)
					indices[cnt++] = i + 1;
			return indices;
		}

		/// <summary>Reads the subclass XML for V1 streams.</summary>
		/// <param name="reader">The reader.</param>
		private void ReadMyXml([NotNull] System.Xml.XmlReader reader)
		{
			reader.ReadStartElement("ScheduleByMonth");
			while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
			{
				switch (reader.LocalName)
				{
					case "DaysOfMonth":
						reader.Read();
						var days = new List<int>();
						while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
						{
							if (reader.LocalName != "Day") continue;
							var sday = reader.ReadElementContentAsString();
							if (sday.Equals("Last", StringComparison.InvariantCultureIgnoreCase)) continue;
							var day = int.Parse(sday);
							if (day >= 1 && day <= 31)
								days.Add(day);
						}
						DaysOfMonth = days.ToArray();
						reader.ReadEndElement();
						break;

					case "Months":
						reader.Read();
						MonthsOfYear = 0;
						while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
						{
							try
							{
								MonthsOfYear |= (MonthsOfTheYear)Enum.Parse(typeof(MonthsOfTheYear), reader.LocalName);
							}
							catch
							{
								throw new System.Xml.XmlException("Invalid months of the year element.");
							}
							reader.Read();
						}
						reader.ReadEndElement();
						break;

					default:
						reader.Skip();
						break;
				}
			}
			reader.ReadEndElement();
		}

		private void WriteMyXml([NotNull] System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement("ScheduleByMonth");

			writer.WriteStartElement("DaysOfMonth");
			foreach (var day in DaysOfMonth)
				writer.WriteElementString("Day", day.ToString());
			writer.WriteEndElement();

			writer.WriteStartElement("Months");
			foreach (MonthsOfTheYear e in Enum.GetValues(typeof(MonthsOfTheYear)))
				if (e != MonthsOfTheYear.AllMonths && (MonthsOfYear & e) == e)
					writer.WriteElementString(e.ToString(), null);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}
	}

	/// <summary>
	/// Represents a trigger that starts a task when the task is registered or updated. Not available on Task Scheduler 1.0. <note>Only available for Task
	/// Scheduler 2.0 on Windows Vista or Windows Server 2003 and later.</note>
	/// </summary>
	/// <remarks>The RegistrationTrigger will fire after the task is registered (saved). It is advisable to put in a delay.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Create a trigger that will fire the task 5 minutes after its registered
	/// RegistrationTrigger rTrigger = new RegistrationTrigger();
	/// rTrigger.Delay = TimeSpan.FromMinutes(5);
	/// ]]>
	/// </code>
	/// </example>
	[XmlType(IncludeInSchema = false)]
	public sealed class RegistrationTrigger : Trigger, ITriggerDelay
	{
		/// <summary>Creates an unbound instance of a <see cref="RegistrationTrigger"/>.</summary>
		public RegistrationTrigger() : base(TaskTriggerType.Registration) { }

		internal RegistrationTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets a value that indicates the amount of time between when the system is booted and when the task is started.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan Delay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((IRegistrationTrigger)v2Trigger).Delay) : GetUnboundValueOrDefault(nameof(Delay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((IRegistrationTrigger)v2Trigger).Delay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(Delay)] = value;
			}
		}

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString() => winPEAS.Properties.Resources.TriggerRegistration1;
	}

	/// <summary>Defines how often the task is run and how long the repetition pattern is repeated after the task is started.</summary>
	/// <remarks>This can be used directly or by assignment for a <see cref="Trigger"/>.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Create a time trigger with a repetition
	/// var tt = new TimeTrigger(new DateTime().Now.AddHours(1));
	/// // Set the time in between each repetition of the task after it starts to 30 minutes.
	/// tt.Repetition.Interval = TimeSpan.FromMinutes(30); // Default is TimeSpan.Zero (or never)
	/// // Set the time the task will repeat to 1 day.
	/// tt.Repetition.Duration = TimeSpan.FromDays(1); // Default is TimeSpan.Zero (or never)
	/// // Set the task to end even if running when the duration is over
	/// tt.Repetition.StopAtDurationEnd = true; // Default is false;
	///
	/// // Do the same as above with a constructor
	/// tt = new TimeTrigger(new DateTime().Now.AddHours(1)) { Repetition = new RepetitionPattern(TimeSpan.FromMinutes(30), TimeSpan.FromDays(1), true) };
	/// ]]>
	/// </code>
	/// </example>
	[XmlRoot("Repetition", Namespace = TaskDefinition.tns, IsNullable = true)]
	[TypeConverter(typeof(RepetitionPatternConverter))]
	public sealed class RepetitionPattern : IDisposable, IXmlSerializable, IEquatable<RepetitionPattern>
	{
		private readonly Trigger pTrigger;
		private readonly IRepetitionPattern v2Pattern;
		private TimeSpan unboundInterval = TimeSpan.Zero, unboundDuration = TimeSpan.Zero;
		private bool unboundStopAtDurationEnd;

		internal RepetitionPattern([NotNull] Trigger parent)
		{
			pTrigger = parent;
			if (pTrigger?.v2Trigger != null)
				v2Pattern = pTrigger.v2Trigger.Repetition;
		}

		/// <summary>Gets or sets how long the pattern is repeated.</summary>
		/// <value>
		/// The duration that the pattern is repeated. The minimum time allowed is one minute. If <c>TimeSpan.Zero</c> is specified, the pattern is repeated indefinitely.
		/// </value>
		/// <remarks>If you specify a repetition duration for a task, you must also specify the repetition interval.</remarks>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		public TimeSpan Duration
		{
			get => v2Pattern != null
				? Task.StringToTimeSpan(v2Pattern.Duration)
				: (pTrigger != null ? TimeSpan.FromMinutes(pTrigger.v1TriggerData.MinutesDuration) : unboundDuration);
			set
			{
				if (value.Ticks < 0 || value != TimeSpan.Zero && value < TimeSpan.FromMinutes(1))
					throw new ArgumentOutOfRangeException(nameof(Duration));
				if (v2Pattern != null)
				{
					v2Pattern.Duration = Task.TimeSpanToString(value);
				}
				else if (pTrigger != null)
				{
					pTrigger.v1TriggerData.MinutesDuration = (uint)value.TotalMinutes;
					Bind();
				}
				else
					unboundDuration = value;
			}
		}

		/// <summary>Gets or sets the amount of time between each restart of the task.</summary>
		/// <value>The amount of time between each restart of the task. The maximum time allowed is 31 days, and the minimum time allowed is 1 minute.</value>
		/// <remarks>If you specify a repetition duration for a task, you must also specify the repetition interval.</remarks>
		/// <exception cref="System.ArgumentOutOfRangeException">The maximum time allowed is 31 days, and the minimum time allowed is 1 minute.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		public TimeSpan Interval
		{
			get => v2Pattern != null
				? Task.StringToTimeSpan(v2Pattern.Interval)
				: (pTrigger != null ? TimeSpan.FromMinutes(pTrigger.v1TriggerData.MinutesInterval) : unboundInterval);
			set
			{
				if (value.Ticks < 0 || (v2Pattern != null || pTrigger == null) && value != TimeSpan.Zero && (value < TimeSpan.FromMinutes(1) || value > TimeSpan.FromDays(31)))
					throw new ArgumentOutOfRangeException(nameof(Interval));
				if (v2Pattern != null)
				{
					v2Pattern.Interval = Task.TimeSpanToString(value);
				}
				else if (pTrigger != null)
				{
					if (value != TimeSpan.Zero && value < TimeSpan.FromMinutes(1))
						throw new ArgumentOutOfRangeException(nameof(Interval));
					pTrigger.v1TriggerData.MinutesInterval = (uint)value.TotalMinutes;
					Bind();
				}
				else
					unboundInterval = value;
			}
		}

		/// <summary>Gets or sets a Boolean value that indicates if a running instance of the task is stopped at the end of repetition pattern duration.</summary>
		[DefaultValue(false)]
		public bool StopAtDurationEnd
		{
			get
			{
				if (v2Pattern != null)
					return v2Pattern.StopAtDurationEnd;
				if (pTrigger != null)
					return (pTrigger.v1TriggerData.Flags & V1Interop.TaskTriggerFlags.KillAtDurationEnd) == V1Interop.TaskTriggerFlags.KillAtDurationEnd;
				return unboundStopAtDurationEnd;
			}
			set
			{
				if (v2Pattern != null)
					v2Pattern.StopAtDurationEnd = value;
				else if (pTrigger != null)
				{
					if (value)
						pTrigger.v1TriggerData.Flags |= V1Interop.TaskTriggerFlags.KillAtDurationEnd;
					else
						pTrigger.v1TriggerData.Flags &= ~V1Interop.TaskTriggerFlags.KillAtDurationEnd;
					Bind();
				}
				else
					unboundStopAtDurationEnd = value;
			}
		}

		/// <summary>Releases all resources used by this class.</summary>
		public void Dispose()
		{
			if (v2Pattern != null) Marshal.ReleaseComObject(v2Pattern);
		}

		/// <summary>Determines whether the specified <see cref="System.Object"/>, is equal to this instance.</summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
		// ReSharper disable once BaseObjectEqualsIsObjectEquals
		public override bool Equals(object obj) => obj is RepetitionPattern pattern ? Equals(pattern) : base.Equals(obj);

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public bool Equals(RepetitionPattern other) => other != null && Duration == other.Duration && Interval == other.Interval && StopAtDurationEnd == other.StopAtDurationEnd;

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode() => new { A = Duration, B = Interval, C = StopAtDurationEnd }.GetHashCode();

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
		{
			if (!reader.IsEmptyElement)
			{
				reader.ReadStartElement(XmlSerializationHelper.GetElementName(this), TaskDefinition.tns);
				XmlSerializationHelper.ReadObjectProperties(reader, this, ReadXmlConverter);
				reader.ReadEndElement();
			}
			else
				reader.Skip();
		}

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { XmlSerializationHelper.WriteObjectProperties(writer, this); }

		internal void Bind()
		{
			if (pTrigger.v1Trigger != null)
				pTrigger.SetV1TriggerData();
			else if (pTrigger.v2Trigger != null)
			{
				if (pTrigger.v1TriggerData.MinutesInterval != 0)
					v2Pattern.Interval = $"PT{pTrigger.v1TriggerData.MinutesInterval}M";
				if (pTrigger.v1TriggerData.MinutesDuration != 0)
					v2Pattern.Duration = $"PT{pTrigger.v1TriggerData.MinutesDuration}M";
				v2Pattern.StopAtDurationEnd = (pTrigger.v1TriggerData.Flags & V1Interop.TaskTriggerFlags.KillAtDurationEnd) == V1Interop.TaskTriggerFlags.KillAtDurationEnd;
			}
		}

		internal void Set([NotNull] RepetitionPattern value)
		{
			Duration = value.Duration;
			Interval = value.Interval;
			StopAtDurationEnd = value.StopAtDurationEnd;
		}

		private bool ReadXmlConverter(System.Reflection.PropertyInfo pi, Object obj, ref Object value)
		{
			if (pi.Name != "Interval" || !(value is TimeSpan span) || span.Equals(TimeSpan.Zero) || Duration > span)
				return false;
			Duration = span.Add(TimeSpan.FromMinutes(1));
			return true;
		}
	}

	/// <summary>
	/// Triggers tasks for console connect or disconnect, remote connect or disconnect, or workstation lock or unlock notifications. <note>Only available for
	/// Task Scheduler 2.0 on Windows Vista or Windows Server 2003 and later.</note>
	/// </summary>
	/// <remarks>
	/// The SessionStateChangeTrigger will fire after six different system events: connecting or disconnecting locally or remotely, or locking or unlocking the session.
	/// </remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// new SessionStateChangeTrigger { StateChange = TaskSessionStateChangeType.ConsoleConnect, UserId = "joe" };
	/// new SessionStateChangeTrigger { StateChange = TaskSessionStateChangeType.ConsoleDisconnect };
	/// new SessionStateChangeTrigger { StateChange = TaskSessionStateChangeType.RemoteConnect };
	/// new SessionStateChangeTrigger { StateChange = TaskSessionStateChangeType.RemoteDisconnect };
	/// new SessionStateChangeTrigger { StateChange = TaskSessionStateChangeType.SessionLock, UserId = "joe" };
	/// new SessionStateChangeTrigger { StateChange = TaskSessionStateChangeType.SessionUnlock };
	/// ]]>
	/// </code>
	/// </example>
	[XmlType(IncludeInSchema = false)]
	public sealed class SessionStateChangeTrigger : Trigger, ITriggerDelay, ITriggerUserId
	{
		/// <summary>Creates an unbound instance of a <see cref="SessionStateChangeTrigger"/>.</summary>
		public SessionStateChangeTrigger() : base(TaskTriggerType.SessionStateChange) { }

		internal SessionStateChangeTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets a value that indicates the amount of time between when the system is booted and when the task is started.</summary>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		public TimeSpan Delay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((ISessionStateChangeTrigger)v2Trigger).Delay) : GetUnboundValueOrDefault(nameof(Delay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((ISessionStateChangeTrigger)v2Trigger).Delay = Task.TimeSpanToString(value);
				else
					unboundValues[nameof(Delay)] = value;
			}
		}

		/// <summary>Gets or sets the kind of Terminal Server session change that would trigger a task launch.</summary>
		[DefaultValue(1)]
		public TaskSessionStateChangeType StateChange
		{
			get => ((ISessionStateChangeTrigger)v2Trigger)?.StateChange ?? GetUnboundValueOrDefault(nameof(StateChange), TaskSessionStateChangeType.ConsoleConnect);
			set
			{
				if (v2Trigger != null)
					((ISessionStateChangeTrigger)v2Trigger).StateChange = value;
				else
					unboundValues[nameof(StateChange)] = value;
			}
		}

		/// <summary>Gets or sets the user for the Terminal Server session. When a session state change is detected for this user, a task is started.</summary>
		[DefaultValue(null)]
		public string UserId
		{
			get => v2Trigger != null ? ((ISessionStateChangeTrigger)v2Trigger).UserId : GetUnboundValueOrDefault<string>(nameof(UserId));
			set
			{
				if (v2Trigger != null)
					((ISessionStateChangeTrigger)v2Trigger).UserId = value;
				else
					unboundValues[nameof(UserId)] = value;
			}
		}

		/// <summary>
		/// Copies the properties from another <see cref="Trigger"/> the current instance. This will not copy any properties associated with any derived triggers
		/// except those supporting the <see cref="ITriggerDelay"/> interface.
		/// </summary>
		/// <param name="sourceTrigger">The source <see cref="Trigger"/>.</param>
		public override void CopyProperties(Trigger sourceTrigger)
		{
			base.CopyProperties(sourceTrigger);
			if (sourceTrigger is SessionStateChangeTrigger st && !StateChangeIsSet())
				StateChange = st.StateChange;
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public override bool Equals(Trigger other) => other is SessionStateChangeTrigger st && base.Equals(st) && StateChange == st.StateChange;

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString()
		{
			var str = winPEAS.Properties.Resources.ResourceManager.GetString("TriggerSession" + StateChange.ToString());
			var user = string.IsNullOrEmpty(UserId) ? winPEAS.Properties.Resources.TriggerAnyUser : UserId;
			if (StateChange != TaskSessionStateChangeType.SessionLock && StateChange != TaskSessionStateChangeType.SessionUnlock)
				user = string.Format(winPEAS.Properties.Resources.TriggerSessionUserSession, user);
			return string.Format(str, user);
		}

		/// <summary>Returns a value indicating if the StateChange property has been set.</summary>
		/// <returns>StateChange property has been set.</returns>
		private bool StateChangeIsSet() => v2Trigger != null || (unboundValues?.ContainsKey("StateChange") ?? false);
	}

	/// <summary>Represents a trigger that starts a task at a specific date and time.</summary>
	/// <remarks>A TimeTrigger runs at a specified date and time.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Create a trigger that runs the last minute of this year
	/// TimeTrigger tTrigger = new TimeTrigger();
	/// tTrigger.StartBoundary = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 0);
	/// ]]>
	/// </code>
	/// </example>
	public sealed class TimeTrigger : Trigger, ITriggerDelay, ICalendarTrigger
	{
		/// <summary>Creates an unbound instance of a <see cref="TimeTrigger"/>.</summary>
		public TimeTrigger() : base(TaskTriggerType.Time) { }

		internal TimeTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.RunOnce) { }

		internal TimeTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets a delay time that is randomly added to the start time of the trigger.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan RandomDelay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((ITimeTrigger)v2Trigger).RandomDelay) : GetUnboundValueOrDefault(nameof(RandomDelay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((ITimeTrigger)v2Trigger).RandomDelay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(RandomDelay)] = value;
			}
		}

		/// <summary>Gets or sets a value that indicates the amount of time before the task is started.</summary>
		/// <value>The delay duration.</value>
		TimeSpan ITriggerDelay.Delay
		{
			get => RandomDelay;
			set => RandomDelay = value;
		}

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString() => string.Format(winPEAS.Properties.Resources.TriggerTime1, AdjustToLocal(StartBoundary));
	}

	/// <summary>
	/// Abstract base class which provides the common properties that are inherited by all trigger classes. A trigger can be created using the
	/// <see cref="TriggerCollection.Add{TTrigger}"/> or the <see cref="TriggerCollection.AddNew"/> method.
	/// </summary>
	public abstract partial class Trigger : IDisposable, ICloneable, IEquatable<Trigger>, IComparable, IComparable<Trigger>
	{
		internal const string V2BoundaryDateFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFK";
		internal static readonly CultureInfo DefaultDateCulture = CultureInfo.CreateSpecificCulture("en-US");

		internal V1Interop.ITaskTrigger v1Trigger;
		internal V1Interop.TaskTrigger v1TriggerData;
		internal ITrigger v2Trigger;
		/// <summary>In testing and may change. Do not use until officially introduced into library.</summary>
		protected Dictionary<string, object> unboundValues = new Dictionary<string, object>();
		private static bool? foundTimeSpan2;
		private static Type timeSpan2Type;
		private readonly TaskTriggerType ttype;
		private RepetitionPattern repititionPattern;

		internal Trigger([NotNull] V1Interop.ITaskTrigger trigger, V1Interop.TaskTriggerType type)
		{
			v1Trigger = trigger;
			v1TriggerData = trigger.GetTrigger();
			v1TriggerData.Type = type;
			ttype = ConvertFromV1TriggerType(type);
		}

		internal Trigger([NotNull] ITrigger iTrigger)
		{
			v2Trigger = iTrigger;
			ttype = iTrigger.Type;
			if (string.IsNullOrEmpty(v2Trigger.StartBoundary) && this is ICalendarTrigger)
				StartBoundary = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
		}

		internal Trigger(TaskTriggerType triggerType)
		{
			ttype = triggerType;

			v1TriggerData.TriggerSize = (ushort)Marshal.SizeOf(typeof(V1Interop.TaskTrigger));
			if (ttype != TaskTriggerType.Registration && ttype != TaskTriggerType.Event && ttype != TaskTriggerType.SessionStateChange)
				v1TriggerData.Type = ConvertToV1TriggerType(ttype);

			if (this is ICalendarTrigger)
				StartBoundary = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
		}

		/// <summary>Gets or sets a Boolean value that indicates whether the trigger is enabled.</summary>
		public bool Enabled
		{
			get => v2Trigger?.Enabled ?? GetUnboundValueOrDefault(nameof(Enabled), !v1TriggerData.Flags.IsFlagSet(V1Interop.TaskTriggerFlags.Disabled));
			set
			{
				if (v2Trigger != null)
					v2Trigger.Enabled = value;
				else
				{
					v1TriggerData.Flags = v1TriggerData.Flags.SetFlags(V1Interop.TaskTriggerFlags.Disabled, !value);
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(Enabled)] = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the date and time when the trigger is deactivated. The trigger cannot start the task after it is deactivated.
		/// <note>While the maximum value for this property is <see cref="DateTime.MaxValue"/>, the Windows Task Scheduler management
		/// application that is part of the OS will fail if this value is greater than December 31, 9998.</note>
		/// </summary>
		/// <remarks>
		/// <para>
		/// Version 1 (1.1 on all systems prior to Vista) of the native library only allows for the Day, Month and Year values of the <see
		/// cref="DateTime"/> structure.
		/// </para>
		/// <para>
		/// Version 2 (1.2 or higher) of the native library only allows for both date and time and all <see cref="DateTime.Kind"/> values.
		/// However, the user interface and <see cref="Trigger.ToString()"/> methods will always show the time translated to local time. The
		/// library makes every attempt to maintain the Kind value. When using the UI elements provided in the TaskSchedulerEditor library,
		/// the "Synchronize across time zones" checkbox will be checked if the Kind is Local or Utc. If the Kind is Unspecified and the
		/// user selects the checkbox, the Kind will be changed to Utc and the time adjusted from the value displayed as the local time.
		/// </para>
		/// </remarks>
		[DefaultValue(typeof(DateTime), "9999-12-31T23:59:59.9999999")]
		public DateTime EndBoundary
		{
			get
			{
				if (v2Trigger != null)
					return string.IsNullOrEmpty(v2Trigger.EndBoundary) ? DateTime.MaxValue : DateTime.Parse(v2Trigger.EndBoundary, DefaultDateCulture);
				return GetUnboundValueOrDefault(nameof(EndBoundary), v1TriggerData.EndDate.GetValueOrDefault(DateTime.MaxValue));
			}
			set
			{
				if (v2Trigger != null)
				{
					if (value <= StartBoundary)
						throw new ArgumentException(winPEAS.Properties.Resources.Error_TriggerEndBeforeStart);
					v2Trigger.EndBoundary = value == DateTime.MaxValue ? null : value.ToString(V2BoundaryDateFormat, DefaultDateCulture);
				}
				else
				{
					v1TriggerData.EndDate = value == DateTime.MaxValue ? (DateTime?)null : value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(EndBoundary)] = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the maximum amount of time that the task launched by this trigger is allowed to run. Not available with Task Scheduler 1.0.
		/// </summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan ExecutionTimeLimit
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(v2Trigger.ExecutionTimeLimit) : GetUnboundValueOrDefault(nameof(ExecutionTimeLimit), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					v2Trigger.ExecutionTimeLimit = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(ExecutionTimeLimit)] = value;
			}
		}

		/// <summary>Gets or sets the identifier for the trigger. Cannot set with Task Scheduler 1.0.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(null)]
		[XmlIgnore]
		public string Id
		{
			get => v2Trigger != null ? v2Trigger.Id : GetUnboundValueOrDefault<string>(nameof(Id));
			set
			{
				if (v2Trigger != null)
					v2Trigger.Id = value;
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(Id)] = value;
			}
		}

		/// <summary>
		/// Gets a <see cref="RepetitionPattern"/> instance that indicates how often the task is run and how long the repetition pattern is repeated after the
		/// task is started.
		/// </summary>
		public RepetitionPattern Repetition
		{
			get => repititionPattern ?? (repititionPattern = new RepetitionPattern(this));
			set => Repetition.Set(value);
		}

		/// <summary>Gets or sets the date and time when the trigger is activated.</summary>
		/// <remarks>
		/// <para>
		/// Version 1 (1.1 on all systems prior to Vista) of the native library only allows for <see cref="DateTime"/> values where the
		/// <see cref="DateTime.Kind"/> is unspecified. If the DateTime value Kind is <see cref="DateTimeKind.Local"/> then it will be used as is. If the
		/// DateTime value Kind is <see cref="DateTimeKind.Utc"/> then it will be converted to the local time and then used.
		/// </para>
		/// <para>
		/// Version 2 (1.2 or higher) of the native library only allows for all <see cref="DateTime.Kind"/> values. However, the user interface and
		/// <see cref="Trigger.ToString()"/> methods will always show the time translated to local time. The library makes every attempt to maintain the Kind
		/// value. When using the UI elements provided in the TaskSchedulerEditor library, the "Synchronize across time zones" checkbox will be checked if the
		/// Kind is Local or Utc. If the Kind is Unspecified and the user selects the checkbox, the Kind will be changed to Utc and the time adjusted from the
		/// value displayed as the local time.
		/// </para>
		/// <para>
		/// Under Version 2, when converting the string used in the native library for this value (ITrigger.Startboundary) this library will behave as follows:
		/// <list type="bullet">
		/// <item><description>YYYY-MM-DDTHH:MM:SS format uses DateTimeKind.Unspecified and the time specified.</description></item>
		/// <item><description>YYYY-MM-DDTHH:MM:SSZ format uses DateTimeKind.Utc and the time specified as the GMT time.</description></item>
		/// <item><description>YYYY-MM-DDTHH:MM:SS±HH:MM format uses DateTimeKind.Local and the time specified in that time zone.</description></item>
		/// </list>
		/// </para>
		/// </remarks>
		public DateTime StartBoundary
		{
			get
			{
				if (v2Trigger == null) return GetUnboundValueOrDefault(nameof(StartBoundary), v1TriggerData.BeginDate);
				if (string.IsNullOrEmpty(v2Trigger.StartBoundary))
					return DateTime.MinValue;
				var ret = DateTime.Parse(v2Trigger.StartBoundary, DefaultDateCulture);
				if (v2Trigger.StartBoundary.EndsWith("Z"))
					ret = ret.ToUniversalTime();
				return ret;
			}
			set
			{
				if (v2Trigger != null)
				{
					if (value > EndBoundary)
						throw new ArgumentException(winPEAS.Properties.Resources.Error_TriggerEndBeforeStart);
					v2Trigger.StartBoundary = value == DateTime.MinValue ? null : value.ToString(V2BoundaryDateFormat, DefaultDateCulture);
				}
				else
				{
					v1TriggerData.BeginDate = value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(StartBoundary)] = value;
				}
			}
		}

		/// <summary>Gets the type of the trigger.</summary>
		/// <value>The <see cref="TaskTriggerType"/> of the trigger.</value>
		[XmlIgnore]
		public TaskTriggerType TriggerType => ttype;

		/// <summary>Creates the specified trigger.</summary>
		/// <param name="triggerType">Type of the trigger to instantiate.</param>
		/// <returns><see cref="Trigger"/> of specified type.</returns>
		public static Trigger CreateTrigger(TaskTriggerType triggerType)
		{
			switch (triggerType)
			{
				case TaskTriggerType.Boot:
					return new BootTrigger();

				case TaskTriggerType.Daily:
					return new DailyTrigger();

				case TaskTriggerType.Event:
					return new EventTrigger();

				case TaskTriggerType.Idle:
					return new IdleTrigger();

				case TaskTriggerType.Logon:
					return new LogonTrigger();

				case TaskTriggerType.Monthly:
					return new MonthlyTrigger();

				case TaskTriggerType.MonthlyDOW:
					return new MonthlyDOWTrigger();

				case TaskTriggerType.Registration:
					return new RegistrationTrigger();

				case TaskTriggerType.SessionStateChange:
					return new SessionStateChangeTrigger();

				case TaskTriggerType.Time:
					return new TimeTrigger();

				case TaskTriggerType.Weekly:
					return new WeeklyTrigger();

				case TaskTriggerType.Custom:
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(triggerType), triggerType, null);
			}
			return null;
		}

		/// <summary>Creates a new <see cref="Trigger"/> that is an unbound copy of this instance.</summary>
		/// <returns>A new <see cref="Trigger"/> that is an unbound copy of this instance.</returns>
		public virtual object Clone()
		{
			var ret = CreateTrigger(TriggerType);
			ret.CopyProperties(this);
			return ret;
		}

		/// <summary>
		/// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes,
		/// follows, or occurs in the same position in the sort order as the other object.
		/// </summary>
		/// <param name="other">An object to compare with this instance.</param>
		/// <returns>A value that indicates the relative order of the objects being compared.</returns>
		public int CompareTo(Trigger other) => string.Compare(Id, other?.Id, StringComparison.InvariantCulture);

		/// <summary>
		/// Copies the properties from another <see cref="Trigger"/> the current instance. This will not copy any properties associated with any derived triggers
		/// except those supporting the <see cref="ITriggerDelay"/> interface.
		/// </summary>
		/// <param name="sourceTrigger">The source <see cref="Trigger"/>.</param>
		public virtual void CopyProperties(Trigger sourceTrigger)
		{
			if (sourceTrigger == null)
				return;
			Enabled = sourceTrigger.Enabled;
			EndBoundary = sourceTrigger.EndBoundary;
			try { ExecutionTimeLimit = sourceTrigger.ExecutionTimeLimit; }
			catch { /* ignored */ }
			Id = sourceTrigger.Id;
			Repetition.Duration = sourceTrigger.Repetition.Duration;
			Repetition.Interval = sourceTrigger.Repetition.Interval;
			Repetition.StopAtDurationEnd = sourceTrigger.Repetition.StopAtDurationEnd;
			StartBoundary = sourceTrigger.StartBoundary;
			if (sourceTrigger is ITriggerDelay delay && this is ITriggerDelay)
				try { ((ITriggerDelay)this).Delay = delay.Delay; }
				catch { /* ignored */ }
			if (sourceTrigger is ITriggerUserId id && this is ITriggerUserId)
				try { ((ITriggerUserId)this).UserId = id.UserId; }
				catch { /* ignored */ }
		}

		/// <summary>Releases all resources used by this class.</summary>
		public virtual void Dispose()
		{
			if (v2Trigger != null)
				Marshal.ReleaseComObject(v2Trigger);
			if (v1Trigger != null)
				Marshal.ReleaseComObject(v1Trigger);
		}

		/// <summary>Determines whether the specified <see cref="System.Object"/>, is equal to this instance.</summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
		/// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
		// ReSharper disable once BaseObjectEqualsIsObjectEquals
		public override bool Equals(object obj) => obj is Trigger trigger ? Equals(trigger) : base.Equals(obj);

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public virtual bool Equals(Trigger other)
		{
			if (other == null) return false;
			var ret = TriggerType == other.TriggerType && Enabled == other.Enabled && EndBoundary == other.EndBoundary &&
				ExecutionTimeLimit == other.ExecutionTimeLimit && Id == other.Id && Repetition.Equals(other.Repetition) &&
				StartBoundary == other.StartBoundary;
			if (other is ITriggerDelay delay && this is ITriggerDelay)
				try { ret = ret && ((ITriggerDelay)this).Delay == delay.Delay; }
				catch { /* ignored */ }
			if (other is ITriggerUserId id && this is ITriggerUserId)
				try { ret = ret && ((ITriggerUserId)this).UserId == id.UserId; }
				catch { /* ignored */ }
			return ret;
		}

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode() => new
		{
			A = TriggerType,
			B = Enabled,
			C = EndBoundary,
			D = ExecutionTimeLimit,
			E = Id,
			F = Repetition,
			G = StartBoundary,
			H = (this as ITriggerDelay)?.Delay ?? TimeSpan.Zero,
			I = (this as ITriggerUserId)?.UserId
		}.GetHashCode();

		/// <summary>Returns a string representing this trigger.</summary>
		/// <returns>String value of trigger.</returns>
		public override string ToString() => v1Trigger != null ? v1Trigger.GetTriggerString() : V2GetTriggerString() + V2BaseTriggerString();

		int IComparable.CompareTo(object obj) => CompareTo(obj as Trigger);

		internal static DateTime AdjustToLocal(DateTime dt) => dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;

		internal static V1Interop.TaskTriggerType ConvertToV1TriggerType(TaskTriggerType type)
		{
			if (type == TaskTriggerType.Registration || type == TaskTriggerType.Event || type == TaskTriggerType.SessionStateChange)
				throw new NotV1SupportedException();
			var tv1 = (int)type - 1;
			if (tv1 >= 7) tv1--;
			return (V1Interop.TaskTriggerType)tv1;
		}

		internal static Trigger CreateTrigger([NotNull] V1Interop.ITaskTrigger trigger) => CreateTrigger(trigger, trigger.GetTrigger().Type);

		internal static Trigger CreateTrigger([NotNull] V1Interop.ITaskTrigger trigger, V1Interop.TaskTriggerType triggerType)
		{
			Trigger t;
			switch (triggerType)
			{
				case V1Interop.TaskTriggerType.RunOnce:
					t = new TimeTrigger(trigger);
					break;

				case V1Interop.TaskTriggerType.RunDaily:
					t = new DailyTrigger(trigger);
					break;

				case V1Interop.TaskTriggerType.RunWeekly:
					t = new WeeklyTrigger(trigger);
					break;

				case V1Interop.TaskTriggerType.RunMonthly:
					t = new MonthlyTrigger(trigger);
					break;

				case V1Interop.TaskTriggerType.RunMonthlyDOW:
					t = new MonthlyDOWTrigger(trigger);
					break;

				case V1Interop.TaskTriggerType.OnIdle:
					t = new IdleTrigger(trigger);
					break;

				case V1Interop.TaskTriggerType.OnSystemStart:
					t = new BootTrigger(trigger);
					break;

				case V1Interop.TaskTriggerType.OnLogon:
					t = new LogonTrigger(trigger);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(triggerType), triggerType, null);
			}
			return t;
		}

		internal static Trigger CreateTrigger([NotNull] ITrigger iTrigger, ITaskDefinition iDef = null)
		{
			switch (iTrigger.Type)
			{
				case TaskTriggerType.Boot:
					return new BootTrigger((IBootTrigger)iTrigger);

				case TaskTriggerType.Daily:
					return new DailyTrigger((IDailyTrigger)iTrigger);

				case TaskTriggerType.Event:
					return new EventTrigger((IEventTrigger)iTrigger);

				case TaskTriggerType.Idle:
					return new IdleTrigger((IIdleTrigger)iTrigger);

				case TaskTriggerType.Logon:
					return new LogonTrigger((ILogonTrigger)iTrigger);

				case TaskTriggerType.Monthly:
					return new MonthlyTrigger((IMonthlyTrigger)iTrigger);

				case TaskTriggerType.MonthlyDOW:
					return new MonthlyDOWTrigger((IMonthlyDOWTrigger)iTrigger);

				case TaskTriggerType.Registration:
					return new RegistrationTrigger((IRegistrationTrigger)iTrigger);

				case TaskTriggerType.SessionStateChange:
					return new SessionStateChangeTrigger((ISessionStateChangeTrigger)iTrigger);

				case TaskTriggerType.Time:
					return new TimeTrigger((ITimeTrigger)iTrigger);

				case TaskTriggerType.Weekly:
					return new WeeklyTrigger((IWeeklyTrigger)iTrigger);

				case TaskTriggerType.Custom:
					var ct = new CustomTrigger(iTrigger);
					if (iDef != null)
						try { ct.UpdateFromXml(iDef.XmlText); } catch { /* ignored */ }
					return ct;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>Gets the best time span string.</summary>
		/// <param name="span">The <see cref="TimeSpan"/> to display.</param>
		/// <returns>Either the full string representation created by TimeSpan2 or the default TimeSpan representation.</returns>
		internal static string GetBestTimeSpanString(TimeSpan span)
		{
			// See if the TimeSpan2 assembly is accessible
			if (!foundTimeSpan2.HasValue)
			{
				try
				{
					foundTimeSpan2 = false;
					timeSpan2Type = System.Reflection.ReflectionHelper.LoadType("System.TimeSpan2", "TimeSpan2.dll");
					if (timeSpan2Type != null)
						foundTimeSpan2 = true;
				}
				catch { /* ignored */ }
			}

			// If the TimeSpan2 assembly is available, try to call the ToString("f") method and return the result.
			if (foundTimeSpan2 == true && timeSpan2Type != null)
			{
				try
				{
					return System.Reflection.ReflectionHelper.InvokeMethod<string>(timeSpan2Type, new object[] { span }, "ToString", "f");
				}
				catch { /* ignored */ }
			}

			return span.ToString();
		}

		internal virtual void Bind([NotNull] V1Interop.ITask iTask)
		{
			if (v1Trigger == null)
			{
				v1Trigger = iTask.CreateTrigger(out var _);
			}
			SetV1TriggerData();
		}

		internal virtual void Bind([NotNull] ITaskDefinition iTaskDef)
		{
			var iTriggers = iTaskDef.Triggers;
			v2Trigger = iTriggers.Create(ttype);
			Marshal.ReleaseComObject(iTriggers);
			if ((unboundValues.TryGetValue("StartBoundary", out var dt) ? (DateTime)dt : StartBoundary) > (unboundValues.TryGetValue("EndBoundary", out dt) ? (DateTime)dt : EndBoundary))
				throw new ArgumentException(winPEAS.Properties.Resources.Error_TriggerEndBeforeStart);
			foreach (var key in unboundValues.Keys)
			{
				try
				{
					var o = unboundValues[key];
					CheckBindValue(key, ref o);
					v2Trigger.GetType().InvokeMember(key, System.Reflection.BindingFlags.SetProperty, null, v2Trigger, new[] { o });
				}
				catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null) { throw tie.InnerException; }
				catch { /* ignored */ }
			}
			unboundValues.Clear();
			unboundValues = null;

			repititionPattern = new RepetitionPattern(this);
			repititionPattern.Bind();
		}

		/// <summary>Assigns the unbound TriggerData structure to the V1 trigger instance.</summary>
		internal void SetV1TriggerData()
		{
			if (v1TriggerData.MinutesInterval != 0 && v1TriggerData.MinutesInterval >= v1TriggerData.MinutesDuration)
				throw new ArgumentException("Trigger.Repetition.Interval must be less than Trigger.Repetition.Duration under Task Scheduler 1.0.");
			if (v1TriggerData.EndDate <= v1TriggerData.BeginDate)
				throw new ArgumentException(winPEAS.Properties.Resources.Error_TriggerEndBeforeStart);
			if (v1TriggerData.BeginDate == DateTime.MinValue)
				v1TriggerData.BeginDate = DateTime.Now;
			v1Trigger?.SetTrigger(ref v1TriggerData);
			System.Diagnostics.Debug.WriteLine(v1TriggerData);
		}

		/// <summary>Checks the bind value for any conversion.</summary>
		/// <param name="key">The key (property) name.</param>
		/// <param name="o">The value.</param>
		protected virtual void CheckBindValue(string key, ref object o)
		{
			if (o is TimeSpan ts)
				o = Task.TimeSpanToString(ts);
			if (o is DateTime dt)
			{
				if (key == "EndBoundary" && dt == DateTime.MaxValue || key == "StartBoundary" && dt == DateTime.MinValue)
					o = null;
				else
					o = dt.ToString(V2BoundaryDateFormat, DefaultDateCulture);
			}
		}

		/// <summary>Gets the unbound value or a default.</summary>
		/// <typeparam name="T">Return type.</typeparam>
		/// <param name="prop">The property name.</param>
		/// <param name="def">The default value if not found in unbound value list.</param>
		/// <returns>The unbound value, if set, or the default value.</returns>
		protected T GetUnboundValueOrDefault<T>(string prop, T def = default) => unboundValues.TryGetValue(prop, out var val) ? (T)val : def;

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected virtual string V2GetTriggerString() => string.Empty;

		private static TaskTriggerType ConvertFromV1TriggerType(V1Interop.TaskTriggerType v1Type)
		{
			var tv2 = (int)v1Type + 1;
			if (tv2 > 6) tv2++;
			return (TaskTriggerType)tv2;
		}

		private string V2BaseTriggerString()
		{
			var ret = new StringBuilder();
			if (Repetition.Interval != TimeSpan.Zero)
			{
				var sduration = Repetition.Duration == TimeSpan.Zero ? winPEAS.Properties.Resources.TriggerDuration0 : string.Format(winPEAS.Properties.Resources.TriggerDurationNot0, GetBestTimeSpanString(Repetition.Duration));
				ret.AppendFormat(winPEAS.Properties.Resources.TriggerRepetition, GetBestTimeSpanString(Repetition.Interval), sduration);
			}
			if (EndBoundary != DateTime.MaxValue)
				ret.AppendFormat(winPEAS.Properties.Resources.TriggerEndBoundary, AdjustToLocal(EndBoundary));
			if (ret.Length > 0)
				ret.Insert(0, winPEAS.Properties.Resources.HyphenSeparator);
			return ret.ToString();
		}
	}

	/// <summary>
	/// Represents a trigger that starts a task based on a weekly schedule. For example, the task starts at 8:00 A.M. on a specific day of the week every week or
	/// every other week.
	/// </summary>
	/// <remarks>A WeeklyTrigger runs at a specified time on specified days of the week every week or interval of weeks.</remarks>
	/// <example>
	/// <code lang="cs">
	/// <![CDATA[
	/// // Create a trigger that runs on Monday every third week just after midnight.
	/// WeeklyTrigger wTrigger = new WeeklyTrigger();
	/// wTrigger.StartBoundary = DateTime.Today + TimeSpan.FromSeconds(15);
	/// wTrigger.DaysOfWeek = DaysOfTheWeek.Monday;
	/// wTrigger.WeeksInterval = 3;
	/// ]]>
	/// </code>
	/// </example>
	[XmlRoot("CalendarTrigger", Namespace = TaskDefinition.tns, IsNullable = false)]
	public sealed class WeeklyTrigger : Trigger, ICalendarTrigger, ITriggerDelay, IXmlSerializable
	{
		/// <summary>Creates an unbound instance of a <see cref="WeeklyTrigger"/>.</summary>
		/// <param name="daysOfWeek">The days of the week.</param>
		/// <param name="weeksInterval">The interval between the weeks in the schedule.</param>
		public WeeklyTrigger(DaysOfTheWeek daysOfWeek = DaysOfTheWeek.Sunday, short weeksInterval = 1) : base(TaskTriggerType.Weekly)
		{
			DaysOfWeek = daysOfWeek;
			WeeksInterval = weeksInterval;
		}

		internal WeeklyTrigger([NotNull] V1Interop.ITaskTrigger iTrigger) : base(iTrigger, V1Interop.TaskTriggerType.RunWeekly)
		{
			if (v1TriggerData.Data.weekly.DaysOfTheWeek == 0)
				v1TriggerData.Data.weekly.DaysOfTheWeek = DaysOfTheWeek.Sunday;
			if (v1TriggerData.Data.weekly.WeeksInterval == 0)
				v1TriggerData.Data.weekly.WeeksInterval = 1;
		}

		internal WeeklyTrigger([NotNull] ITrigger iTrigger) : base(iTrigger) { }

		/// <summary>Gets or sets the days of the week on which the task runs.</summary>
		[DefaultValue(0)]
		public DaysOfTheWeek DaysOfWeek
		{
			get => v2Trigger != null
				? (DaysOfTheWeek)((IWeeklyTrigger)v2Trigger).DaysOfWeek
				: v1TriggerData.Data.weekly.DaysOfTheWeek;
			set
			{
				if (v2Trigger != null)
					((IWeeklyTrigger)v2Trigger).DaysOfWeek = (short)value;
				else
				{
					v1TriggerData.Data.weekly.DaysOfTheWeek = value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(DaysOfWeek)] = (short)value;
				}
			}
		}

		/// <summary>Gets or sets a delay time that is randomly added to the start time of the trigger.</summary>
		/// <exception cref="NotV1SupportedException">Not supported under Task Scheduler 1.0.</exception>
		[DefaultValue(typeof(TimeSpan), "00:00:00")]
		[XmlIgnore]
		public TimeSpan RandomDelay
		{
			get => v2Trigger != null ? Task.StringToTimeSpan(((IWeeklyTrigger)v2Trigger).RandomDelay) : GetUnboundValueOrDefault(nameof(RandomDelay), TimeSpan.Zero);
			set
			{
				if (v2Trigger != null)
					((IWeeklyTrigger)v2Trigger).RandomDelay = Task.TimeSpanToString(value);
				else if (v1Trigger != null)
					throw new NotV1SupportedException();
				else
					unboundValues[nameof(RandomDelay)] = value;
			}
		}

		/// <summary>Gets or sets the interval between the weeks in the schedule.</summary>
		[DefaultValue(1)]
		public short WeeksInterval
		{
			get => ((IWeeklyTrigger)v2Trigger)?.WeeksInterval ?? (short)v1TriggerData.Data.weekly.WeeksInterval;
			set
			{
				if (v2Trigger != null)
					((IWeeklyTrigger)v2Trigger).WeeksInterval = value;
				else
				{
					v1TriggerData.Data.weekly.WeeksInterval = (ushort)value;
					if (v1Trigger != null)
						SetV1TriggerData();
					else
						unboundValues[nameof(WeeksInterval)] = value;
				}
			}
		}

		/// <summary>Gets or sets a value that indicates the amount of time before the task is started.</summary>
		/// <value>The delay duration.</value>
		TimeSpan ITriggerDelay.Delay
		{
			get => RandomDelay;
			set => RandomDelay = value;
		}

		/// <summary>
		/// Copies the properties from another <see cref="Trigger"/> the current instance. This will not copy any properties associated with any derived triggers
		/// except those supporting the <see cref="ITriggerDelay"/> interface.
		/// </summary>
		/// <param name="sourceTrigger">The source <see cref="Trigger"/>.</param>
		public override void CopyProperties(Trigger sourceTrigger)
		{
			base.CopyProperties(sourceTrigger);
			if (sourceTrigger is WeeklyTrigger wt)
			{
				DaysOfWeek = wt.DaysOfWeek;
				WeeksInterval = wt.WeeksInterval;
			}
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public override bool Equals(Trigger other) => other is WeeklyTrigger wt && base.Equals(wt) && DaysOfWeek == wt.DaysOfWeek && WeeksInterval == wt.WeeksInterval;

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { CalendarTrigger.ReadXml(reader, this, ReadMyXml); }

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { CalendarTrigger.WriteXml(writer, this, WriteMyXml); }

		/// <summary>Gets the non-localized trigger string for V2 triggers.</summary>
		/// <returns>String describing the trigger.</returns>
		protected override string V2GetTriggerString()
		{
			var days = TaskEnumGlobalizer.GetString(DaysOfWeek);
			return string.Format(WeeksInterval == 1 ? winPEAS.Properties.Resources.TriggerWeekly1Week : winPEAS.Properties.Resources.TriggerWeeklyMultWeeks, AdjustToLocal(StartBoundary), days, WeeksInterval);
		}

		/// <summary>Reads the subclass XML for V1 streams.</summary>
		/// <param name="reader">The reader.</param>
		private void ReadMyXml(System.Xml.XmlReader reader)
		{
			reader.ReadStartElement("ScheduleByWeek");
			while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
			{
				switch (reader.LocalName)
				{
					case "WeeksInterval":
						WeeksInterval = (short)reader.ReadElementContentAsInt();
						break;

					case "DaysOfWeek":
						reader.Read();
						DaysOfWeek = 0;
						while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
						{
							try
							{
								DaysOfWeek |= (DaysOfTheWeek)Enum.Parse(typeof(DaysOfTheWeek), reader.LocalName);
							}
							catch
							{
								throw new System.Xml.XmlException("Invalid days of the week element.");
							}
							reader.Read();
						}
						reader.ReadEndElement();
						break;

					default:
						reader.Skip();
						break;
				}
			}
			reader.ReadEndElement();
		}

		/// <summary>Writes the subclass XML for V1 streams.</summary>
		/// <param name="writer">The writer.</param>
		private void WriteMyXml(System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement("ScheduleByWeek");

			if (WeeksInterval != 1)
				writer.WriteElementString("WeeksInterval", WeeksInterval.ToString());

			writer.WriteStartElement("DaysOfWeek");
			foreach (DaysOfTheWeek e in Enum.GetValues(typeof(DaysOfTheWeek)))
				if (e != DaysOfTheWeek.AllDays && (DaysOfWeek & e) == e)
					writer.WriteElementString(e.ToString(), null);
			writer.WriteEndElement();

			writer.WriteEndElement();
		}
	}

	internal static class CalendarTrigger
	{
		internal delegate void CalendarXmlReader(System.Xml.XmlReader reader);

		internal delegate void CalendarXmlWriter(System.Xml.XmlWriter writer);

		public static void WriteXml([NotNull] System.Xml.XmlWriter writer, [NotNull] Trigger t, [NotNull] CalendarXmlWriter calWriterProc)
		{
			if (!t.Enabled)
				writer.WriteElementString("Enabled", System.Xml.XmlConvert.ToString(t.Enabled));
			if (t.EndBoundary != DateTime.MaxValue)
				writer.WriteElementString("EndBoundary", System.Xml.XmlConvert.ToString(t.EndBoundary, System.Xml.XmlDateTimeSerializationMode.RoundtripKind));
			XmlSerializationHelper.WriteObject(writer, t.Repetition);
			writer.WriteElementString("StartBoundary", System.Xml.XmlConvert.ToString(t.StartBoundary, System.Xml.XmlDateTimeSerializationMode.RoundtripKind));
			calWriterProc(writer);
		}

		internal static Trigger GetTriggerFromXml([NotNull] System.Xml.XmlReader reader)
		{
			Trigger t = null;
			var xml = reader.ReadOuterXml();
			var match = System.Text.RegularExpressions.Regex.Match(xml, @"\<(?<T>ScheduleBy.+)\>");
			if (match.Success && match.Groups.Count == 2)
			{
				switch (match.Groups[1].Value)
				{
					case "ScheduleByDay":
						t = new DailyTrigger();
						break;

					case "ScheduleByWeek":
						t = new WeeklyTrigger();
						break;

					case "ScheduleByMonth":
						t = new MonthlyTrigger();
						break;

					case "ScheduleByMonthDayOfWeek":
						t = new MonthlyDOWTrigger();
						break;
				}

				if (t != null)
				{
					using (var ms = new System.IO.StringReader(xml))
					{
						using (var iReader = System.Xml.XmlReader.Create(ms))
						{
							((IXmlSerializable)t).ReadXml(iReader);
						}
					}
				}
			}
			return t;
		}

		internal static void ReadXml([NotNull] System.Xml.XmlReader reader, [NotNull] Trigger t, [NotNull] CalendarXmlReader calReaderProc)
		{
			reader.ReadStartElement("CalendarTrigger", TaskDefinition.tns);
			while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
			{
				switch (reader.LocalName)
				{
					case "Enabled":
						t.Enabled = reader.ReadElementContentAsBoolean();
						break;

					case "EndBoundary":
						t.EndBoundary = reader.ReadElementContentAsDateTime();
						break;

					case "RandomDelay":
						((ITriggerDelay)t).Delay = Task.StringToTimeSpan(reader.ReadElementContentAsString());
						break;

					case "StartBoundary":
						t.StartBoundary = reader.ReadElementContentAsDateTime();
						break;

					case "Repetition":
						XmlSerializationHelper.ReadObject(reader, t.Repetition);
						break;

					case "ScheduleByDay":
					case "ScheduleByWeek":
					case "ScheduleByMonth":
					case "ScheduleByMonthDayOfWeek":
						calReaderProc(reader);
						break;

					default:
						reader.Skip();
						break;
				}
			}
			reader.ReadEndElement();
		}
	}

	internal sealed class RepetitionPatternConverter : TypeConverter
	{
	}
}