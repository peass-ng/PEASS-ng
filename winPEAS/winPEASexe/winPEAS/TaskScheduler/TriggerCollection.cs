using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Provides the methods that are used to add to, remove from, and get the triggers of a task.
	/// </summary>
	[XmlRoot("Triggers", Namespace = TaskDefinition.tns, IsNullable = false)]
	public sealed class TriggerCollection : IList<Trigger>, IDisposable, IXmlSerializable, IList
	{
		private V1Interop.ITask v1Task;
		private readonly V2Interop.ITriggerCollection v2Coll;
		private V2Interop.ITaskDefinition v2Def;

		internal TriggerCollection([NotNull] V1Interop.ITask iTask)
		{
			v1Task = iTask;
		}

		internal TriggerCollection([NotNull] V2Interop.ITaskDefinition iTaskDef)
		{
			v2Def = iTaskDef;
			v2Coll = v2Def.Triggers;
		}

		/// <summary>
		/// Gets the number of triggers in the collection.
		/// </summary>
		public int Count => v2Coll?.Count ?? v1Task.GetTriggerCount();

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;

		bool ICollection<Trigger>.IsReadOnly => false;

		bool IList.IsFixedSize => false;

		bool IList.IsReadOnly => false;

		object IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (Trigger)value; }
		}

		
		/// <summary>
		/// Gets a specified trigger from the collection.
		/// </summary>
		/// <param name="index">The index of the trigger to be retrieved.</param>
		/// <returns>Specialized <see cref="Trigger"/> instance.</returns>
		public Trigger this[int index]
		{
			get
			{
				if (v2Coll != null)
					return Trigger.CreateTrigger(v2Coll[++index], v2Def);
				return Trigger.CreateTrigger(v1Task.GetTrigger((ushort)index));
			}
			set
			{
				if (Count <= index)
					throw new ArgumentOutOfRangeException(nameof(index), index, @"Index is not a valid index in the TriggerCollection");
				Insert(index, value);
				RemoveAt(index + 1);
			}
		}

		/// <summary>
		/// Add an unbound <see cref="Trigger"/> to the task.
		/// </summary>
		/// <typeparam name="TTrigger">A type derived from <see cref="Trigger"/>.</typeparam>
		/// <param name="unboundTrigger"><see cref="Trigger"/> derivative to add to the task.</param>
		/// <returns>Bound trigger.</returns>
		/// <exception cref="System.ArgumentNullException"><c>unboundTrigger</c> is <c>null</c>.</exception>
		public TTrigger Add<TTrigger>([NotNull] TTrigger unboundTrigger) where TTrigger : Trigger
		{
			if (unboundTrigger == null)
				throw new ArgumentNullException(nameof(unboundTrigger));
			if (v2Def != null)
				unboundTrigger.Bind(v2Def);
			else
				unboundTrigger.Bind(v1Task);
			return unboundTrigger;
		}

		/// <summary>
		/// Add a new trigger to the collections of triggers for the task.
		/// </summary>
		/// <param name="taskTriggerType">The type of trigger to create.</param>
		/// <returns>A <see cref="Trigger"/> instance of the specified type.</returns>
		public Trigger AddNew(TaskTriggerType taskTriggerType)
		{
			if (v1Task != null)
			{
				ushort idx;
				return Trigger.CreateTrigger(v1Task.CreateTrigger(out idx), Trigger.ConvertToV1TriggerType(taskTriggerType));
			}

			return Trigger.CreateTrigger(v2Coll.Create(taskTriggerType), v2Def);
		}

		/// <summary>
		/// Clears all triggers from the task.
		/// </summary>
		public void Clear()
		{
			if (v2Coll != null)
				v2Coll.Clear();
			else
			{
				for (int i = Count - 1; i >= 0; i--)
					RemoveAt(i);
			}
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <returns>
		/// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
		/// </returns>
		public bool Contains([NotNull] Trigger item) => Find(a => a.Equals(item)) != null;

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		public void CopyTo(Trigger[] array, int arrayIndex) { CopyTo(0, array, arrayIndex, Count); }

		/// <summary>
		/// Copies the elements of the <see cref="TriggerCollection" /> to a <see cref="Trigger" /> array, starting at a particular <see cref="Trigger" /> array index.
		/// </summary>
		/// <param name="index">The zero-based index in the source at which copying begins.</param>
		/// <param name="array">The <see cref="Trigger" /> array that is the destination of the elements copied from <see cref="TriggerCollection" />. The <see cref="Trigger" /> array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <see cref="Trigger" /> array at which copying begins.</param>
		/// <param name="count">The number of elements to copy.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="array" /> is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="arrayIndex" /> is less than 0.</exception>
		/// <exception cref="System.ArgumentException">The number of elements in the source <see cref="TriggerCollection" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
		public void CopyTo(int index, Trigger[] array, int arrayIndex, int count)
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
				array[arrayIndex + i] = (Trigger)this[index + i].Clone();
		}

		/// <summary>
		/// Releases all resources used by this class.
		/// </summary>
		public void Dispose()
		{
			if (v2Coll != null) Marshal.ReleaseComObject(v2Coll);
			v2Def = null;
			v1Task = null;
		}

		/// <summary>
		/// Searches for an <see cref="Trigger"/> that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire collection.
		/// </summary>
		/// <param name="match">The <see cref="Predicate{Trigger}"/> delegate that defines the conditions of the <see cref="Trigger"/> to search for.</param>
		/// <returns>The first <see cref="Trigger"/> that matches the conditions defined by the specified predicate, if found; otherwise, <c>null</c>.</returns>
		public Trigger Find([NotNull] Predicate<Trigger> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			foreach (var item in this)
				if (match(item)) return item;
			return null;
		}

		/// <summary>
		/// Searches for an <see cref="Trigger"/> that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the collection that starts at the specified index and contains the specified number of elements.
		/// </summary>
		/// <param name="startIndex">The zero-based starting index of the search.</param>
		/// <param name="count">The number of elements in the collection to search.</param>
		/// <param name="match">The <see cref="Predicate{Trigger}"/> delegate that defines the conditions of the element to search for.</param>
		/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
		public int FindIndexOf(int startIndex, int count, [NotNull] Predicate<Trigger> match)
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
		/// Searches for an <see cref="Trigger"/> that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the collection.
		/// </summary>
		/// <param name="match">The <see cref="Predicate{Trigger}"/> delegate that defines the conditions of the element to search for.</param>
		/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.</returns>
		public int FindIndexOf([NotNull] Predicate<Trigger> match) => FindIndexOf(0, Count, match);

		/// <summary>
		/// Gets the collection enumerator for this collection.
		/// </summary>
		/// <returns>The <see cref="IEnumerator{T}"/> for this collection.</returns>
		public IEnumerator<Trigger> GetEnumerator()
		{
			if (v1Task != null)
				return new V1TriggerEnumerator(v1Task);
			return new ComEnumerator<Trigger, V2Interop.ITrigger>(() => v2Coll.Count, i => v2Coll[i], o => Trigger.CreateTrigger(o, v2Def));
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array != null && array.Rank != 1)
				throw new RankException("Multi-dimensional arrays are not supported.");
			var src = new Trigger[Count];
			CopyTo(src, 0);
			Array.Copy(src, 0, array, index, Count);
		}

		void ICollection<Trigger>.Add(Trigger item) { Add(item); }

		int IList.Add(object value)
		{
			Add((Trigger)value);
			return Count - 1;
		}

		bool IList.Contains(object value) => Contains((Trigger)value);

		int IList.IndexOf(object value) => IndexOf((Trigger)value);

		void IList.Insert(int index, object value) { Insert(index, (Trigger)value); }

		void IList.Remove(object value) { Remove((Trigger)value); }

		/// <summary>
		/// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		/// <returns>
		/// The index of <paramref name="item" /> if found in the list; otherwise, -1.
		/// </returns>
		public int IndexOf([NotNull] Trigger item) => FindIndexOf(a => a.Equals(item));

		/// <summary>
		/// Inserts an trigger at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which trigger should be inserted.</param>
		/// <param name="trigger">The trigger to insert into the list.</param>
		public void Insert(int index, [NotNull] Trigger trigger)
		{
			if (trigger == null)
				throw new ArgumentNullException(nameof(trigger));
			if (index >= Count)
				throw new ArgumentOutOfRangeException(nameof(index));

			Trigger[] pushItems = new Trigger[Count - index];
			CopyTo(index, pushItems, 0, Count - index);
			for (int j = Count - 1; j >= index; j--)
				RemoveAt(j);
			Add(trigger);
			foreach (Trigger t in pushItems)
				Add(t);
		}

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
		{
			reader.ReadStartElement(XmlSerializationHelper.GetElementName(this), TaskDefinition.tns);
			while (reader.MoveToContent() == System.Xml.XmlNodeType.Element)
			{
				switch (reader.LocalName)
				{
					case "BootTrigger":
						XmlSerializationHelper.ReadObject(reader, AddNew(TaskTriggerType.Boot));
						break;

					case "IdleTrigger":
						XmlSerializationHelper.ReadObject(reader, AddNew(TaskTriggerType.Idle));
						break;

					case "TimeTrigger":
						XmlSerializationHelper.ReadObject(reader, AddNew(TaskTriggerType.Time));
						break;

					case "LogonTrigger":
						XmlSerializationHelper.ReadObject(reader, AddNew(TaskTriggerType.Logon));
						break;

					case "CalendarTrigger":
						Add(CalendarTrigger.GetTriggerFromXml(reader));
						break;

					default:
						reader.Skip();
						break;
				}
			}
			reader.ReadEndElement();
		}

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
		{
			foreach (var t in this)
				XmlSerializationHelper.WriteObject(writer, t);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <returns>
		/// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </returns>
		public bool Remove([NotNull] Trigger item)
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
		/// Removes the trigger at a specified index.
		/// </summary>
		/// <param name="index">Index of trigger to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
		public void RemoveAt(int index)
		{
			if (index >= Count)
				throw new ArgumentOutOfRangeException(nameof(index), index, @"Failed to remove Trigger. Index out of range.");
			if (v2Coll != null)
				v2Coll.Remove(++index);
			else
				v1Task.DeleteTrigger((ushort)index); //Remove the trigger from the Task Scheduler
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the triggers in this collection.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the triggers in this collection.
		/// </returns>
		public override string ToString()
		{
			if (Count == 1)
				return this[0].ToString();
			if (Count > 1)
				return winPEAS.Properties.Resources.MultipleTriggers;
			return string.Empty;
		}

		internal void Bind()
		{
			foreach (Trigger t in this)
				t.SetV1TriggerData();
		}

		private sealed class V1TriggerEnumerator : IEnumerator<Trigger>
		{
			private short curItem = -1;
			private V1Interop.ITask iTask;

			internal V1TriggerEnumerator(V1Interop.ITask task) { iTask = task; }

			public Trigger Current => Trigger.CreateTrigger(iTask.GetTrigger((ushort)curItem));

			object IEnumerator.Current => Current;

			/// <summary>
			/// Releases all resources used by this class.
			/// </summary>
			public void Dispose() { iTask = null; }

			public bool MoveNext() => (++curItem < iTask.GetTriggerCount());

			public void Reset() { curItem = -1; }
		}
	}
}