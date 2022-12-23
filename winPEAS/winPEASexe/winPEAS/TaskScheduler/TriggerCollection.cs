using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using winPEAS.TaskScheduler.TaskEditor.Native;
using winPEAS.TaskScheduler.V1;
using winPEAS.TaskScheduler.V2;

namespace winPEAS.TaskScheduler
{
    [XmlRoot("Triggers", Namespace = TaskDefinition.tns, IsNullable = false)]
    public sealed class TriggerCollection : IList<Trigger>, IDisposable, IXmlSerializable, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private const string IndexerName = "Item[]";
        private readonly ITriggerCollection v2Coll;
        private bool inV2set;
        private ITask v1Task;
        private ITaskDefinition v2Def;

        internal TriggerCollection([NotNull] ITask iTask) => v1Task = iTask;

        internal TriggerCollection([NotNull] ITaskDefinition iTaskDef)
        {
            v2Def = iTaskDef;
            v2Coll = v2Def.Triggers;
        }

        /// <summary>Occurs when a collection changes.</summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Gets the number of triggers in the collection.</summary>
        public int Count => v2Coll?.Count ?? v1Task.GetTriggerCount();

        bool IList.IsFixedSize => false;

        bool ICollection<Trigger>.IsReadOnly => false;

        bool IList.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        /// <summary>Gets or sets a specified trigger from the collection.</summary>
        /// <value>The <see cref="Trigger"/>.</value>
        /// <param name="triggerId">The id ( <see cref="Trigger.Id"/>) of the trigger to be retrieved.</param>
        /// <returns>Specialized <see cref="Trigger"/> instance.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="InvalidOperationException">Mismatching Id for trigger and lookup.</exception>
        public Trigger this[[NotNull] string triggerId]
        {
            get
            {
                if (string.IsNullOrEmpty(triggerId))
                    throw new ArgumentNullException(nameof(triggerId));
                foreach (var t in this)
                    if (string.Equals(t.Id, triggerId))
                        return t;
                throw new ArgumentOutOfRangeException(nameof(triggerId));
            }
            set
            {
                if (value == null)
                    throw new NullReferenceException();
                if (string.IsNullOrEmpty(triggerId))
                    throw new ArgumentNullException(nameof(triggerId));
                if (triggerId != value.Id)
                    throw new InvalidOperationException("Mismatching Id for trigger and lookup.");
                var index = IndexOf(triggerId);
                if (index >= 0)
                {
                    var orig = this[index].Clone();
                    inV2set = true;
                    try
                    {
                        RemoveAt(index);
                        Insert(index, value);
                    }
                    finally
                    {
                        inV2set = true;
                    }
                    OnNotifyPropertyChanged(IndexerName);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, orig, index));
                }
                else
                    Add(value);
            }
        }

        /// <summary>Gets a specified trigger from the collection.</summary>
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
                if (index < 0 || Count <= index)
                    throw new ArgumentOutOfRangeException(nameof(index), index, @"Index is not a valid index in the TriggerCollection");
                var orig = this[index].Clone();
                inV2set = true;
                try
                {
                    Insert(index, value);
                    RemoveAt(index + 1);
                }
                finally
                {
                    inV2set = false;
                }
                OnNotifyPropertyChanged(IndexerName);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, orig, index));
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (Trigger)value;
        }

        /*/// <summary>
		/// Add an unbound <see cref="Trigger"/> to the task. </summary> <param name="unboundTrigger"><see cref="Trigger"/> derivative to
		/// add to the task.</param> <returns>Bound trigger.</returns> <exception cref="System.ArgumentNullException"><c>unboundTrigger</c>
		/// is <c>null</c>.</exception>
		public Trigger Add([NotNull] Trigger unboundTrigger)
		{
			if (unboundTrigger == null)
				throw new ArgumentNullException(nameof(unboundTrigger));
			if (v2Def != null)
				unboundTrigger.Bind(v2Def);
			else
				unboundTrigger.Bind(v1Task);
			return unboundTrigger;
		}*/

        /// <summary>Add an unbound <see cref="Trigger"/> to the task.</summary>
        /// <typeparam name="TTrigger">A type derived from <see cref="Trigger"/>.</typeparam>
        /// <param name="unboundTrigger"><see cref="Trigger"/> derivative to add to the task.</param>
        /// <returns>Bound trigger.</returns>
        /// <exception cref="ArgumentNullException"><c>unboundTrigger</c> is <c>null</c>.</exception>
        public TTrigger Add<TTrigger>([NotNull] TTrigger unboundTrigger) where TTrigger : Trigger
        {
            if (unboundTrigger == null)
                throw new ArgumentNullException(nameof(unboundTrigger));
            if (v2Def != null)
                unboundTrigger.Bind(v2Def);
            else
                unboundTrigger.Bind(v1Task);
            OnNotifyPropertyChanged(nameof(Count));
            OnNotifyPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, unboundTrigger));
            return unboundTrigger;
        }

        /// <summary>Add a new trigger to the collections of triggers for the task.</summary>
        /// <param name="taskTriggerType">The type of trigger to create.</param>
        /// <returns>A <see cref="Trigger"/> instance of the specified type.</returns>
        public Trigger AddNew(TaskTriggerType taskTriggerType)
        {
            if (v1Task != null)
                return Trigger.CreateTrigger(v1Task.CreateTrigger(out _), Trigger.ConvertToV1TriggerType(taskTriggerType));

            return Trigger.CreateTrigger(v2Coll.Create(taskTriggerType), v2Def);
        }

        /// <summary>Adds a collection of unbound triggers to the end of the <see cref="TriggerCollection"/>.</summary>
        /// <param name="triggers">
        /// The triggers to be added to the end of the <see cref="TriggerCollection"/>. The collection itself cannot be <c>null</c> and
        /// cannot contain <c>null</c> elements.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="triggers"/> is <c>null</c>.</exception>
        public void AddRange([NotNull] IEnumerable<Trigger> triggers)
        {
            if (triggers == null)
                throw new ArgumentNullException(nameof(triggers));
            foreach (var item in triggers)
                Add(item);
        }

        /// <summary>Clears all triggers from the task.</summary>
        public void Clear()
        {
            if (v2Coll != null)
                v2Coll.Clear();
            else
            {
                inV2set = true;
                try
                {
                    for (var i = Count - 1; i >= 0; i--)
                        RemoveAt(i);
                }
                finally
                {
                    inV2set = false;
                }
            }
            OnNotifyPropertyChanged(nameof(Count));
            OnNotifyPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>Determines whether the <see cref="ICollection{T}"/> contains a specific value.</summary>
        /// <param name="item">The object to locate in the <see cref="ICollection{T}"/>.</param>
        /// <returns>true if <paramref name="item"/> is found in the <see cref="ICollection{T}"/>; otherwise, false.</returns>
        public bool Contains([NotNull] Trigger item) => Find(a => a.Equals(item)) != null;

        /// <summary>Determines whether the specified trigger type is contained in this collection.</summary>
        /// <param name="triggerType">Type of the trigger.</param>
        /// <returns><c>true</c> if the specified trigger type is contained in this collection; otherwise, <c>false</c>.</returns>
        public bool ContainsType(Type triggerType) => Find(a => a.GetType() == triggerType) != null;

        /// <summary>
        /// Copies the elements of the <see cref="ICollection{T}"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection{T}"/>. The
        /// <see cref="Array"/> must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(Trigger[] array, int arrayIndex) => CopyTo(0, array, arrayIndex, Count);

        /// <summary>
        /// Copies the elements of the <see cref="TriggerCollection"/> to a <see cref="Trigger"/> array, starting at a particular <see
        /// cref="Trigger"/> array index.
        /// </summary>
        /// <param name="index">The zero-based index in the source at which copying begins.</param>
        /// <param name="array">
        /// The <see cref="Trigger"/> array that is the destination of the elements copied from <see cref="TriggerCollection"/>. The <see
        /// cref="Trigger"/> array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <see cref="Trigger"/> array at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">
        /// The number of elements in the source <see cref="TriggerCollection"/> is greater than the available space from <paramref
        /// name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
        /// </exception>
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
            for (var i = 0; i < count; i++)
                array[arrayIndex + i] = (Trigger)this[index + i].Clone();
        }

        /// <summary>Releases all resources used by this class.</summary>
        public void Dispose()
        {
            if (v2Coll != null) Marshal.ReleaseComObject(v2Coll);
            v2Def = null;
            v1Task = null;
        }

        /// <summary>
        /// Searches for an <see cref="Trigger"/> that matches the conditions defined by the specified predicate, and returns the first
        /// occurrence within the entire collection.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{Trigger}"/> delegate that defines the conditions of the <see cref="Trigger"/> to search for.
        /// </param>
        /// <returns>
        /// The first <see cref="Trigger"/> that matches the conditions defined by the specified predicate, if found; otherwise, <c>null</c>.
        /// </returns>
        public Trigger Find([NotNull] Predicate<Trigger> match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            foreach (var item in this)
                if (match(item)) return item;
            return null;
        }

        /// <summary>
        /// Searches for an <see cref="Trigger"/> that matches the conditions defined by the specified predicate, and returns the zero-based
        /// index of the first occurrence within the collection that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the collection to search.</param>
        /// <param name="match">The <see cref="Predicate{Trigger}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.
        /// </returns>
        public int FindIndexOf(int startIndex, int count, [NotNull] Predicate<Trigger> match)
        {
            if (startIndex < 0 || startIndex >= Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex + count > Count)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            for (var i = startIndex; i < startIndex + count; i++)
                if (match(this[i])) return i;
            return -1;
        }

        /// <summary>
        /// Searches for an <see cref="Trigger"/> that matches the conditions defined by the specified predicate, and returns the zero-based
        /// index of the first occurrence within the collection.
        /// </summary>
        /// <param name="match">The <see cref="Predicate{Trigger}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, –1.
        /// </returns>
        public int FindIndexOf([NotNull] Predicate<Trigger> match) => FindIndexOf(0, Count, match);

        /// <summary>Gets the collection enumerator for this collection.</summary>
        /// <returns>The <see cref="IEnumerator{T}"/> for this collection.</returns>
        public IEnumerator<Trigger> GetEnumerator()
        {
            if (v1Task != null)
                return new V1TriggerEnumerator(v1Task);
            return new ComEnumerator<Trigger, ITrigger>(() => v2Coll.Count, i => v2Coll[i], o => Trigger.CreateTrigger(o, v2Def));
        }

        /// <summary>Determines the index of a specific item in the <see cref="IList{T}"/>.</summary>
        /// <param name="item">The object to locate in the <see cref="IList{T}"/>.</param>
        /// <returns>The index of <paramref name="item"/> if found in the list; otherwise, -1.</returns>
        public int IndexOf([NotNull] Trigger item) => FindIndexOf(a => a.Equals(item));

        /// <summary>Determines the index of a specific item in the <see cref="IList{T}"/>.</summary>
        /// <param name="triggerId">The id ( <see cref="Trigger.Id"/>) of the trigger to be retrieved.</param>
        /// <returns>The index of <paramref name="triggerId"/> if found in the list; otherwise, -1.</returns>
        public int IndexOf([NotNull] string triggerId)
        {
            if (string.IsNullOrEmpty(triggerId))
                throw new ArgumentNullException(triggerId);
            return FindIndexOf(a => string.Equals(a.Id, triggerId));
        }

        /// <summary>Inserts an trigger at the specified index.</summary>
        /// <param name="index">The zero-based index at which trigger should be inserted.</param>
        /// <param name="trigger">The trigger to insert into the list.</param>
        public void Insert(int index, [NotNull] Trigger trigger)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));
            if (index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var pushItems = new Trigger[Count - index];
            CopyTo(index, pushItems, 0, Count - index);
            for (var j = Count - 1; j >= index; j--)
                RemoveAt(j);
            Add(trigger);
            foreach (var t in pushItems)
                Add(t);
        }

        /// <summary>Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>.</summary>
        /// <param name="item">The object to remove from the <see cref="ICollection{T}"/>.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the <see cref="ICollection{T}"/>; otherwise, false. This method
        /// also returns false if <paramref name="item"/> is not found in the original <see cref="ICollection{T}"/>.
        /// </returns>
        public bool Remove([NotNull] Trigger item)
        {
            var idx = IndexOf(item);
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

        /// <summary>Removes the trigger at a specified index.</summary>
        /// <param name="index">Index of trigger to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, @"Failed to remove Trigger. Index out of range.");
            var item = this[index].Clone();
            if (v2Coll != null)
                v2Coll.Remove(++index);
            else
                v1Task.DeleteTrigger((ushort)index); //Remove the trigger from the Task Scheduler
            if (!inV2set)
            {
                OnNotifyPropertyChanged(nameof(Count));
                OnNotifyPropertyChanged(IndexerName);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            }
        }

        /// <summary>Copies the elements of the <see cref="TriggerCollection"/> to a new array.</summary>
        /// <returns>An array containing copies of the elements of the <see cref="TriggerCollection"/>.</returns>
        public Trigger[] ToArray()
        {
            var ret = new Trigger[Count];
            CopyTo(ret, 0);
            return ret;
        }

        /// <summary>Returns a <see cref="string"/> that represents the triggers in this collection.</summary>
        /// <returns>A <see cref="string"/> that represents the triggers in this collection.</returns>
        public override string ToString()
        {
            if (Count == 1)
                return this[0].ToString();
            if (Count > 1)
                return Properties.Resources.MultipleTriggers;
            return string.Empty;
        }

        void ICollection<Trigger>.Add(Trigger item) => Add(item);

        int IList.Add(object value)
        {
            Add((Trigger)value);
            return Count - 1;
        }

        bool IList.Contains(object value) => Contains((Trigger)value);

        void ICollection.CopyTo(Array array, int index)
        {
            if (array != null && array.Rank != 1)
                throw new RankException("Multi-dimensional arrays are not supported.");
            var src = new Trigger[Count];
            CopyTo(src, 0);
            Array.Copy(src, 0, array, index, Count);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

        int IList.IndexOf(object value) => IndexOf((Trigger)value);

        void IList.Insert(int index, object value) => Insert(index, (Trigger)value);

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

        void IList.Remove(object value) => Remove((Trigger)value);

        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
        {
            foreach (var t in this)
                XmlSerializationHelper.WriteObject(writer, t);
        }

        internal void Bind()
        {
            foreach (var t in this)
                t.SetV1TriggerData();
        }

        /// <summary>Called when a property has changed to notify any attached elements.</summary>
        /// <param name="propertyName">Name of the property.</param>
        private void OnNotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private sealed class V1TriggerEnumerator : IEnumerator<Trigger>
        {
            private short curItem = -1;
            private ITask iTask;

            internal V1TriggerEnumerator(ITask task) => iTask = task;

            public Trigger Current => Trigger.CreateTrigger(iTask.GetTrigger((ushort)curItem));

            object IEnumerator.Current => Current;

            /// <summary>Releases all resources used by this class.</summary>
            public void Dispose() => iTask = null;

            public bool MoveNext() => (++curItem < iTask.GetTriggerCount());

            public void Reset() => curItem = -1;
        }
    }
}
