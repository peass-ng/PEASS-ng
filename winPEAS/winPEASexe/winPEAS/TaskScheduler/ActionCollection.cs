using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Options for when to convert actions to PowerShell equivalents.
	/// </summary>
	[Flags]
	public enum PowerShellActionPlatformOption
	{
		/// <summary>
		/// Never convert any actions to PowerShell. This will force exceptions to be thrown when unsupported actions our action quantities are found.
		/// </summary>
		Never = 0,
		/// <summary>
		/// Convert actions under Version 1 of the library (Windows XP or Windows Server 2003 and earlier). This option supports multiple actions of all types.
		/// If not specified, only a single <see cref="ExecAction"/> is supported. Developer must ensure that PowerShell v2 or higher is installed on the target computer.
		/// </summary>
		Version1 = 1,
		/// <summary>
		/// Convert all <see cref="ShowMessageAction"/> and <see cref="EmailAction"/> references to their PowerShell equivalents on systems on or after Windows 8 / Server 2012.
		/// </summary>
		Version2 = 2,
		/// <summary>
		/// Convert all actions regardless of version or operating system.
		/// </summary>
		All = 3
	}

	/// <summary>
	/// Collection that contains the actions that are performed by the task.
	/// </summary>
	[XmlRoot("Actions", Namespace = TaskDefinition.tns, IsNullable = false)]
	[PublicAPI]
	public sealed class ActionCollection : IList<Action>, IDisposable, IXmlSerializable, IList
	{
		internal const int MaxActions = 32;

		private List<Action> v1Actions;
		private V1Interop.ITask v1Task;
		private V2Interop.IActionCollection v2Coll;
		private V2Interop.ITaskDefinition v2Def;
		private PowerShellActionPlatformOption psConvert = PowerShellActionPlatformOption.Version2;
		private static readonly string psV2IdRegex = $"(?:; )?{nameof(PowerShellConversion)}=(?<v>0|1)";

		internal ActionCollection([NotNull] V1Interop.ITask task)
		{
			v1Task = task;
			v1Actions = GetV1Actions();
			PowerShellConversion = Action.TryParse(v1Task.GetDataItem(nameof(PowerShellConversion)), psConvert | PowerShellActionPlatformOption.Version2);
		}

		internal ActionCollection([NotNull] V2Interop.ITaskDefinition iTaskDef)
		{
			v2Def = iTaskDef;
			v2Coll = iTaskDef.Actions;
			System.Text.RegularExpressions.Match match;
			if (iTaskDef.Data != null && (match = System.Text.RegularExpressions.Regex.Match(iTaskDef.Data, psV2IdRegex)).Success)
			{
				var on = false;
				try { on = bool.Parse(match.Groups["v"].Value); } catch { try { on = int.Parse(match.Groups["v"].Value) == 1; } catch { } }
				if (on)
					psConvert |= PowerShellActionPlatformOption.Version2;
				else
					psConvert &= ~PowerShellActionPlatformOption.Version2;
			}
			UnconvertUnsupportedActions();
		}

		/// <summary>
		/// Gets or sets the identifier of the principal for the task.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		[CanBeNull]
		public string Context
		{
			get
			{
				if (v2Coll != null)
					return v2Coll.Context;
				return v1Task.GetDataItem("ActionCollectionContext");
			}
			set
			{
				if (v2Coll != null)
					v2Coll.Context = value;
				else
					v1Task.SetDataItem("ActionCollectionContext", value);
			}
		}

		/// <summary>
		/// Gets the number of actions in the collection.
		/// </summary>
		public int Count => (v2Coll != null) ? v2Coll.Count : v1Actions.Count;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;

		bool ICollection<Action>.IsReadOnly => false;

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		/// <summary>Gets or sets the systems under which unsupported actions will be converted to PowerShell <see cref="ExecAction"/> instances.</summary>
		/// <value>The PowerShell platform options.</value>
		/// <remarks>This property will affect how many actions are physically stored in the system and is tied to the version of Task Scheduler.
		/// <para>If set to <see cref="PowerShellActionPlatformOption.Never"/>, then no actions will ever be converted to PowerShell. This will force exceptions to be thrown when unsupported actions our action quantities are found.</para>
		/// <para>If set to <see cref="PowerShellActionPlatformOption.Version1"/>, then actions will be converted only under Version 1 of the library (Windows XP or Windows Server 2003 and earlier). This option supports multiple actions of all types. If not specified, only a single <see cref="ExecAction"/> is supported. Developer must ensure that PowerShell v2 or higher is installed on the target computer.</para>
		/// <para>If set to <see cref="PowerShellActionPlatformOption.Version2"/> (which is the default value), then <see cref="ShowMessageAction"/> and <see cref="EmailAction"/> references will be converted to their PowerShell equivalents on systems on or after Windows 8 / Server 2012.</para>
		/// <para>If set to <see cref="PowerShellActionPlatformOption.All"/>, then any actions not supported by the Task Scheduler version will be converted to PowerShell.</para>
		/// </remarks>
		[DefaultValue(typeof(PowerShellActionPlatformOption), "Version2")]
		public PowerShellActionPlatformOption PowerShellConversion
		{
			get { return psConvert; }
			set
			{
				if (psConvert != value)
				{
					psConvert = value;
					if (v1Task != null)
						v1Task.SetDataItem(nameof(PowerShellConversion), value.ToString());
					if (v2Def != null)
					{
						if (!string.IsNullOrEmpty(v2Def.Data))
							v2Def.Data = System.Text.RegularExpressions.Regex.Replace(v2Def.Data, psV2IdRegex, "");
						if (!SupportV2Conversion)
							v2Def.Data = string.Format("{0}; {1}=0", v2Def.Data, nameof(PowerShellConversion));
					}
				}
			}
		}

		private bool SupportV1Conversion => (PowerShellConversion & PowerShellActionPlatformOption.Version1) != 0;

		private bool SupportV2Conversion => (PowerShellConversion & PowerShellActionPlatformOption.Version2) != 0;

		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (Action)value; }
		}

		/// <summary>
		/// Gets or sets a an action at the specified index.
		/// </summary>
		/// <value>The zero-based index of the action to get or set.</value>
		[NotNull]
		public Action this[int index]
		{
			get
			{
				if (v2Coll != null)
					return Action.CreateAction(v2Coll[++index]);
				if (v1Task != null)
				{
					if (SupportV1Conversion)
						return v1Actions[index];
					else
					{
						if (index == 0)
							return v1Actions[0];
					}
				}
				throw new ArgumentOutOfRangeException();
			}
			set
			{
				if (Count <= index)
					throw new ArgumentOutOfRangeException(nameof(index), index, "Index is not a valid index in the ActionCollection");
				if (v2Coll != null)
				{
					Insert(index, value);
					RemoveAt(index + 1);
				}
				else
				{
					v1Actions[index] = value;
					SaveV1Actions();
				}
			}
		}

		/// <summary>
		/// Adds an action to the task.
		/// </summary>
		/// <typeparam name="TAction">A type derived from <see cref="Action"/>.</typeparam>
		/// <param name="action">A derived <see cref="Action"/> class.</param>
		/// <returns>The bound <see cref="Action"/> that was added to the collection.</returns>
		[NotNull]
		public TAction Add<TAction>([NotNull] TAction action) where TAction : Action
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (v2Def != null)
				action.Bind(v2Def);
			else
			{
				if (!SupportV1Conversion && (v1Actions.Count >= 1 || !(action is ExecAction)))
					throw new NotV1SupportedException($"Only a single {nameof(ExecAction)} is supported unless the {nameof(PowerShellConversion)} property includes the {nameof(PowerShellActionPlatformOption.Version1)} value.");
				v1Actions.Add(action);
				SaveV1Actions();
			}
			return action;
		}

		/// <summary>
		/// Adds a new <see cref="Action"/> instance to the task.
		/// </summary>
		/// <param name="actionType">Type of task to be created</param>
		/// <returns>Specialized <see cref="Action"/> instance.</returns>
		[NotNull]
		public Action AddNew(TaskActionType actionType)
		{
			if (Count >= MaxActions)
				throw new ArgumentOutOfRangeException(nameof(actionType), "A maximum of 32 actions is allowed within a single task.");
			if (v1Task != null)
			{
				if (!SupportV1Conversion && (v1Actions.Count >= 1 || actionType != TaskActionType.Execute))
					throw new NotV1SupportedException($"Only a single {nameof(ExecAction)} is supported unless the {nameof(PowerShellConversion)} property includes the {nameof(PowerShellActionPlatformOption.Version1)} value.");
				return Action.CreateAction(v1Task);
			}
			return Action.CreateAction(v2Coll.Create(actionType));
		}

		/// <summary>
		/// Clears all actions from the task.
		/// </summary>
		public void Clear()
		{
			if (v2Coll != null)
				v2Coll.Clear();
			else
			{
				v1Actions.Clear();
				SaveV1Actions();
			}
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <returns>
		/// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
		/// </returns>
		public bool Contains([NotNull] Action item) => Find(a => a.Equals(item)) != null;

		/// <summary>
		/// Copies the elements of the <see cref="ActionCollection" /> to an array of <see cref="Action" />, starting at a particular  index.
		/// </summary>
		/// <param name="array">The <see cref="Action" /> array that is the destination of the elements copied from <see cref="ActionCollection" />. The <see cref="Action" /> array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <see cref="Action" /> array at which copying begins.</param>
		public void CopyTo(Action[] array, int arrayIndex)
		{
			CopyTo(0, array, arrayIndex, Count);
		}

		/// <summary>
		/// Copies the elements of the <see cref="ActionCollection" /> to an <see cref="Action" /> array, starting at a particular <see cref="Action" /> array index.
		/// </summary>
		/// <param name="index">The zero-based index in the source at which copying begins.</param>
		/// <param name="array">The <see cref="Action" /> array that is the destination of the elements copied from <see cref="ActionCollection" />. The <see cref="Action" /> array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <see cref="Action" /> array at which copying begins.</param>
		/// <param name="count">The number of elements to copy.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="array" /> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than 0.</exception>
		/// <exception cref="System.ArgumentException">The number of elements in the source <see cref="ActionCollection" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
		public void CopyTo(int index, [NotNull] Action[] array, int arrayIndex, int count)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (index < 0 || index >= Count)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			if (count < 0 || count > (Count - index))
				throw new ArgumentOutOfRangeException(nameof(count));
			if ((Count - index) > (array.Length - arrayIndex))
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			for (int i = 0; i < count; i++)
				array[arrayIndex + i] = (Action)this[index + i].Clone();
		}

		/// <summary>
		/// Releases all resources used by this class.
		/// </summary>
		public void Dispose()
		{
			v1Task = null;
			v2Def = null;
			if (v2Coll != null) Marshal.ReleaseComObject(v2Coll);
		}

		/// <summary>
		/// Searches for an <see cref="Action"/> that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire collection.
		/// </summary>
		/// <param name="match">The <see cref="Predicate{Action}"/> delegate that defines the conditions of the <see cref="Action"/> to search for.</param>
		/// <returns>The first <see cref="Action"/> that matches the conditions defined by the specified predicate, if found; otherwise, <c>null</c>.</returns>
		public Action Find(Predicate<Action> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			foreach (var item in this)
				if (match(item)) return item;
			return null;
		}

		/// <summary>
		/// Searches for an <see cref="Action"/> that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the collection that starts at the specified index and contains the specified number of elements.
		/// </summary>
		/// <param name="startIndex">The zero-based starting index of the search.</param>
		/// <param name="count">The number of elements in the collection to search.</param>
		/// <param name="match">The <see cref="Predicate{Action}"/> delegate that defines the conditions of the element to search for.</param>
		/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
		public int FindIndexOf(int startIndex, int count, [NotNull] Predicate<Action> match)
		{
			if (startIndex < 0 || startIndex >= Count)
				throw new ArgumentOutOfRangeException(nameof(startIndex));
			if (startIndex + count > Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			for (int i = startIndex; i < startIndex + count; i++)
				if (match(this[i])) return i;
			return -1;
		}

		/// <summary>
		/// Searches for an <see cref="Action"/> that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the collection.
		/// </summary>
		/// <param name="match">The <see cref="Predicate{Action}"/> delegate that defines the conditions of the element to search for.</param>
		/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
		public int FindIndexOf([NotNull] Predicate<Action> match) => FindIndexOf(0, Count, match);

		/// <summary>
		/// Retrieves an enumeration of each of the actions.
		/// </summary>
		/// <returns>Returns an object that implements the <see cref="IEnumerator"/> interface and that can iterate through the <see cref="Action"/> objects within the <see cref="ActionCollection"/>.</returns>
		public IEnumerator<Action> GetEnumerator()
		{
			if (v2Coll != null)
				return new ComEnumerator<Action, V2Interop.IAction>(() => v2Coll.Count, i => v2Coll[i], Action.CreateAction);
			return v1Actions.GetEnumerator();
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array != null && array.Rank != 1)
				throw new RankException("Multi-dimensional arrays are not supported.");
			Action[] src = new Action[Count];
			CopyTo(src, 0);
			Array.Copy(src, 0, array, index, Count);
		}

		void ICollection<Action>.Add(Action item)
		{
			Add(item);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		int IList.Add(object value)
		{
			Add((Action)value);
			return Count - 1;
		}

		bool IList.Contains(object value) => Contains((Action)value);

		int IList.IndexOf(object value) => IndexOf((Action)value);

		void IList.Insert(int index, object value)
		{
			Insert(index, (Action)value);
		}

		void IList.Remove(object value)
		{
			Remove((Action)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		/// <returns>
		/// The index of <paramref name="item" /> if found in the list; otherwise, -1.
		/// </returns>
		public int IndexOf(Action item) => FindIndexOf(a => a.Equals(item));

		/// <summary>
		/// Inserts an action at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which action should be inserted.</param>
		/// <param name="action">The action to insert into the list.</param>
		public void Insert(int index, [NotNull] Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (index < 0 || index > Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (v2Coll != null)
			{
				Action[] pushItems = new Action[Count - index];
				if (Count != index)
				{
					CopyTo(index, pushItems, 0, Count - index);
					for (int j = Count - 1; j >= index; j--)
						RemoveAt(j);
				}
				Add(action);
				if (Count != index)
					for (int k = 0; k < pushItems.Length; k++)
						Add(pushItems[k]);
			}
			else
			{
				if (!SupportV1Conversion && (index > 0 || !(action is ExecAction)))
					throw new NotV1SupportedException($"Only a single {nameof(ExecAction)} is supported unless the {nameof(PowerShellConversion)} property includes the {nameof(PowerShellActionPlatformOption.Version1)} value.");
				v1Actions.Insert(index, action);
				SaveV1Actions();
			}
		}

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
		{
			reader.ReadStartElement(XmlSerializationHelper.GetElementName(this), TaskDefinition.tns);
			Context = reader.GetAttribute("Context");
			while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
			{
				Action a = Action.CreateAction(Action.TryParse(reader.LocalName == "Exec" ? "Execute" : reader.LocalName, TaskActionType.Execute));
				XmlSerializationHelper.ReadObject(reader, a);
				this.Add(a);
			}
			reader.ReadEndElement();
		}

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
		{
			// TODO:FIX if (!string.IsNullOrEmpty(Context)) writer.WriteAttributeString("Context", Context);
			foreach (var item in this)
				XmlSerializationHelper.WriteObject(writer, item);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <returns>
		/// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </returns>
		public bool Remove([NotNull] Action item)
		{
			int idx = IndexOf(item);
			if (idx != -1)
			{
				try
				{
					RemoveAt(idx);
					return true;
				}
				catch { }
			}
			return false;
		}

		/// <summary>
		/// Removes the action at a specified index.
		/// </summary>
		/// <param name="index">Index of action to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
		public void RemoveAt(int index)
		{
			if (index >= Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, "Failed to remove action. Index out of range.");
			if (v2Coll != null)
				v2Coll.Remove(++index);
			else
			{
				v1Actions.RemoveAt(index);
				SaveV1Actions();
			}
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the actions in this collection.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the actions in this collection.
		/// </returns>
		public override string ToString()
		{
			if (Count == 1)
				return this[0].ToString();
			if (Count > 1)
				return winPEAS.Properties.Resources.MultipleActions;
			return string.Empty;
		}

		internal void ConvertUnsupportedActions()
		{
			if (TaskService.LibraryVersion.Minor > 3 && SupportV2Conversion)
			{
				for (int i = 0; i < Count; i++)
				{
					Action action = this[i];
					var bindable = action as IBindAsExecAction;
					if (bindable != null && !(action is ComHandlerAction))
						this[i] = ExecAction.ConvertToPowerShellAction(action);
				}
			}
		}

		private void UnconvertUnsupportedActions()
		{
			if (TaskService.LibraryVersion.Minor > 3)
			{
				for (int i = 0; i < Count; i++)
				{
					ExecAction action = this[i] as ExecAction;
					if (action != null)
					{
						Action newAction = Action.ConvertFromPowerShellAction(action);
						if (newAction != null)
							this[i] = newAction;
					}
				}
			}
		}

		private List<Action> GetV1Actions()
		{
			List<Action> ret = new List<Action>();
			if (v1Task != null && v1Task.GetDataItem("ActionType") != "EMPTY")
			{
				var exec = new ExecAction(v1Task);
				var items = exec.ParsePowerShellItems();
				if (items != null)
				{
					if (items.Length == 2 && items[0] == "MULTIPLE")
					{
						PowerShellConversion |= PowerShellActionPlatformOption.Version1;
						var mc = System.Text.RegularExpressions.Regex.Matches(items[1], @"<# (?<id>\w+):(?<t>\w+) #>\s*(?<c>[^<#]*)\s*");
						foreach (System.Text.RegularExpressions.Match ms in mc)
						{
							var a = Action.ActionFromScript(ms.Groups["t"].Value, ms.Groups["c"].Value);
							if (a != null)
							{
								if (ms.Groups["id"].Value != "NO_ID")
									a.Id = ms.Groups["id"].Value;
								ret.Add(a);
							}
						}
					}
					else
						ret.Add(ExecAction.ConvertFromPowerShellAction(exec));
				}
				else if (!string.IsNullOrEmpty(exec.Path))
				{
					ret.Add(exec);
				}
			}
			return ret;
		}

		private void SaveV1Actions()
		{
			if (v1Task == null)
				throw new ArgumentNullException(nameof(v1Task));
			if (v1Actions.Count == 0)
			{
				v1Task.SetApplicationName(string.Empty);
				v1Task.SetParameters(string.Empty);
				v1Task.SetWorkingDirectory(string.Empty);
				v1Task.SetDataItem("ActionId", null);
				v1Task.SetDataItem("ActionType", "EMPTY");
			}
			else if (v1Actions.Count == 1)
			{
				if (!SupportV1Conversion && v1Actions[0].ActionType != TaskActionType.Execute)
					throw new NotV1SupportedException($"Only a single {nameof(ExecAction)} is supported unless the {nameof(PowerShellConversion)} property includes the {nameof(PowerShellActionPlatformOption.Version1)} value.");
				v1Task.SetDataItem("ActionType", null);
				v1Actions[0].Bind(v1Task);
			}
			else
			{
				if (!SupportV1Conversion)
					throw new NotV1SupportedException($"Only a single {nameof(ExecAction)} is supported unless the {nameof(PowerShellConversion)} property includes the {nameof(PowerShellActionPlatformOption.Version1)} value.");
				// Build list of internal PowerShell scripts
				var sb = new System.Text.StringBuilder();
				foreach (var item in v1Actions)
					sb.Append($"<# {item.Id ?? "NO_ID"}:{item.ActionType} #> {item.GetPowerShellCommand()} ");

				// Build and save PS ExecAction
				var ea = ExecAction.CreatePowerShellAction("MULTIPLE", sb.ToString());
				ea.Bind(v1Task);
				v1Task.SetDataItem("ActionId", null);
				v1Task.SetDataItem("ActionType", "MULTIPLE");
			}
		}
	}
}