using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Microsoft.Win32.TaskScheduler.V2Interop;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	/// <summary>
	/// Pair of name and value.
	/// </summary>
	[PublicAPI]
	public class NameValuePair : IXmlSerializable, INotifyPropertyChanged, ICloneable, IEquatable<NameValuePair>, IEquatable<ITaskNamedValuePair>
	{
		private readonly ITaskNamedValuePair v2Pair;
		private string name, value;

		/// <summary>
		/// Occurs when a property has changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="NameValuePair"/> class.
		/// </summary>
		public NameValuePair() { }

		internal NameValuePair([NotNull] ITaskNamedValuePair iPair)
		{
			v2Pair = iPair;
		}

		internal NameValuePair([NotNull] string name, [NotNull] string value)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
				throw new ArgumentException("Both name and value must be non-empty strings.");
			this.name = name; this.value = value;
		}

		[XmlIgnore]
		internal bool AttributedXmlFormat { get; set; } = true;

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		[NotNull]
		public string Name
		{
			get { return v2Pair == null ? name : v2Pair.Name; }
			set { if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Name)); if (v2Pair == null) name = value; else v2Pair.Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
		}

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>
		/// The value.
		/// </value>
		[NotNull]
		public string Value
		{
			get { return v2Pair == null ? value : v2Pair.Value; }
			set { if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Value)); if (v2Pair == null) this.value = value; else v2Pair.Value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
		}

		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <returns>A copy of an unbound <see cref="NameValuePair"/>.</returns>
		[NotNull]
		public NameValuePair Clone() => new NameValuePair(Name, Value);

		object ICloneable.Clone() => Clone();

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/>, is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			var valuePair = obj as ITaskNamedValuePair;
			if (valuePair != null)
				return (this as IEquatable<ITaskNamedValuePair>).Equals(valuePair);
			var pair = obj as NameValuePair;
			if (pair != null)
				return Equals(pair);
			return base.Equals(obj);
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		public bool Equals([NotNull] NameValuePair other) => other.Name == Name && other.Value == Value;

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
		bool IEquatable<ITaskNamedValuePair>.Equals(ITaskNamedValuePair other) => other.Name == Name && other.Value == Value;

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode() => new { A = Name, B = Value }.GetHashCode();

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString() => $"{Name}={Value}";

		/// <summary>
		/// Implements the operator implicit NameValuePair.
		/// </summary>
		/// <param name="kvp">The KeyValuePair.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static implicit operator NameValuePair(KeyValuePair<string, string> kvp) => new NameValuePair(kvp.Key, kvp.Value);

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

		void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
		{
			if (reader.MoveToContent() == System.Xml.XmlNodeType.Element && reader.LocalName == "Value")
			{
				Name = reader.GetAttribute("name");
				Value = reader.ReadString();
				reader.Read();
			}
			else
			{
				reader.ReadStartElement();
				XmlSerializationHelper.ReadObjectProperties(reader, this);
				reader.ReadEndElement();
			}
		}

		void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
		{
			if (AttributedXmlFormat)
			{
				writer.WriteAttributeString("name", Name);
				writer.WriteString(Value);
			}
			else
			{
				XmlSerializationHelper.WriteObjectProperties(writer, this);
			}
		}
	}

	/// <summary>
	/// Contains a collection of name-value pairs.
	/// </summary>
	[PublicAPI]
	public sealed class NamedValueCollection : IDisposable, ICollection<NameValuePair>, IDictionary<string, string>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		private ITaskNamedValueCollection v2Coll;
		private readonly List<NameValuePair> unboundDict;

		/// <summary>
		/// Occurs when the collection has changed.
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Occurs when a property has changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		internal NamedValueCollection([NotNull] ITaskNamedValueCollection iColl) { v2Coll = iColl; }

		internal NamedValueCollection()
		{
			unboundDict = new List<NameValuePair>(5);
		}

		[XmlIgnore]
		internal bool AttributedXmlFormat { get; set; } = true;

		internal void Bind([NotNull] ITaskNamedValueCollection iTaskNamedValueCollection)
		{
			v2Coll = iTaskNamedValueCollection;
			v2Coll.Clear();
			foreach (var item in unboundDict)
				v2Coll.Create(item.Name, item.Value);
		}

		/// <summary>
		/// Copies current <see cref="NamedValueCollection"/> to another.
		/// </summary>
		/// <param name="destCollection">The destination collection.</param>
		public void CopyTo([NotNull] NamedValueCollection destCollection)
		{
			if (v2Coll != null)
			{
				for (var i = 1; i <= Count; i++)
					destCollection.Add(v2Coll[i].Name, v2Coll[i].Value);
			}
			else
			{
				foreach (var item in unboundDict)
					destCollection.Add(item.Name, item.Value);
			}
		}

		/// <summary>
		/// Releases all resources used by this class.
		/// </summary>
		public void Dispose()
		{
			if (v2Coll != null) Marshal.ReleaseComObject(v2Coll);
		}

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count => v2Coll?.Count ?? unboundDict.Count;

		/// <summary>
		/// Gets a collection of the names.
		/// </summary>
		/// <value>
		/// The names.
		/// </value>
		[ItemNotNull, NotNull]
		public ICollection<string> Names
		{
			get
			{
				if (v2Coll == null)
					return unboundDict.ConvertAll(p => p.Name);

				var ret = new List<string>(v2Coll.Count);
				foreach (var item in this)
					ret.Add(item.Name);
				return ret;
			}
		}

		/// <summary>
		/// Gets a collection of the values.
		/// </summary>
		/// <value>
		/// The values.
		/// </value>
		[ItemNotNull, NotNull]
		public ICollection<string> Values
		{
			get
			{
				if (v2Coll == null)
					return unboundDict.ConvertAll(p => p.Value);

				var ret = new List<string>(v2Coll.Count);
				foreach (var item in this)
					ret.Add(item.Value);
				return ret;
			}
		}


		/// <summary>
		/// Gets the value of the item with the specified name.
		/// </summary>
		/// <param name="name">Name to get the value for.</param>
		/// <returns>Value for the name, or null if not found.</returns>
		public string this[string name]
		{
			[CanBeNull]
			get
			{
				string ret;
				TryGetValue(name, out ret);
				return ret;
			}
			[NotNull]
			set
			{
				int idx;
				NameValuePair old = null;
				var nvp = new NameValuePair(name, value);
				if (v2Coll == null)
				{
					idx = unboundDict.FindIndex(p => p.Name == name);
					if (idx == -1)
						unboundDict.Add(nvp);
					else
					{
						old = unboundDict[idx];
						unboundDict[idx] = nvp;
					}
				}
				else
				{
					var array = new KeyValuePair<string, string>[Count];
					((ICollection<KeyValuePair<string, string>>)this).CopyTo(array, 0);
					idx = Array.FindIndex(array, p => p.Key == name);
					if (idx == -1)
						v2Coll.Create(name, value);
					else
					{
						old = array[idx];
						array[idx] = new KeyValuePair<string, string>(name, value);
						v2Coll.Clear();
						foreach (KeyValuePair<string, string> t in array)
							v2Coll.Create(t.Key, t.Value);
					}
				}
				if (idx == -1)
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, nvp));
				else
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, nvp, old, idx));
			}
		}

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		public void Add([NotNull] NameValuePair item)
		{
			if (v2Coll != null)
				v2Coll.Create(item.Name, item.Value);
			else
				unboundDict.Add(item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		/// <summary>
		/// Adds a name-value pair to the collection.
		/// </summary>
		/// <param name="name">The name associated with a value in a name-value pair.</param>
		/// <param name="value">The value associated with a name in a name-value pair.</param>
		public void Add([NotNull] string name, [NotNull] string value)
		{
			Add(new NameValuePair(name, value));
		}

		/// <summary>
		/// Clears the entire collection of name-value pairs.
		/// </summary>
		public void Clear()
		{
			if (v2Coll != null)
				v2Coll.Clear();
			else
				unboundDict.Clear();
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<NameValuePair> GetEnumerator()
		{
			if (v2Coll == null)
				return unboundDict.GetEnumerator();

			return new ComEnumerator<NameValuePair, ITaskNamedValuePair>(() => v2Coll.Count, i => v2Coll[i], o => new NameValuePair(o));
		}

		private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
				foreach (NameValuePair item in e.NewItems)
					item.AttributedXmlFormat = AttributedXmlFormat;
			CollectionChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Removes the name-value pair with the specified key from the collection.
		/// </summary>
		/// <param name="name">The name associated with a value in a name-value pair.</param>
		/// <returns><c>true</c> if item successfully removed; <c>false</c> otherwise.</returns>
		public bool Remove([NotNull] string name)
		{
			var i = -1;
			NameValuePair nvp = null;
			try
			{
				if (v2Coll == null)
				{
					i = unboundDict.FindIndex(p => p.Name == name);
					if (i != -1)
					{
						nvp = unboundDict[i];
						unboundDict.RemoveAt(i);
					}
					return (i != -1);
				}

				for (i = 0; i < v2Coll.Count; i++)
				{
					if (name == v2Coll[i].Name)
					{
						nvp = new NameValuePair(v2Coll[i]).Clone();
						v2Coll.Remove(i);
						return true;
					}
				}
				i = -1;
			}
			finally
			{
				if (i != -1)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, nvp, i));
				}
			}
			return false;
		}


		/// <summary>
		/// Gets the value associated with the specified name.
		/// </summary>
		/// <param name="name">The name whose value to get.</param>
		/// <param name="value">When this method returns, the value associated with the specified name, if the name is found; otherwise, <c>null</c>. This parameter is passed uninitialized.</param>
		/// <returns><c>true</c> if the collection contains an element with the specified name; otherwise, <c>false</c>.</returns>
		public bool TryGetValue(string name, out string value)
		{
			if (v2Coll != null)
			{
				foreach (var item in this)
				{
					if (string.CompareOrdinal(item.Name, name) == 0)
					{
						value = item.Value;
						return true;
					}
				}
				value = null;
				return false;
			}

			var nvp = unboundDict.Find(p => p.Name == name);
			value = nvp?.Value;
			return nvp != null;
		}

		/// <summary>
		/// Gets the collection enumerator for the name-value collection.
		/// </summary>
		/// <returns>An <see cref="System.Collections.IEnumerator"/> for the collection.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		bool ICollection<NameValuePair>.Contains(NameValuePair item)
		{
			if (v2Coll == null)
				return unboundDict.Contains(item);

			foreach (var invp in this)
				if (Equals(item, invp)) return true;
			return false;
		}

		void ICollection<NameValuePair>.CopyTo(NameValuePair[] array, int arrayIndex)
		{
			if (v2Coll == null)
				unboundDict.CopyTo(array, arrayIndex);
			else
			{
				if (array.Length - arrayIndex < v2Coll.Count)
					throw new ArgumentException("Items in collection exceed available items in destination array.");
				if (arrayIndex < 0)
					throw new ArgumentException(@"Array index must be 0 or greater.", nameof(arrayIndex));
				for (var i = 0; i < v2Coll.Count; i++)
					array[i + arrayIndex] = new NameValuePair(v2Coll[i]);
			}
		}

		bool ICollection<NameValuePair>.IsReadOnly => false;

		ICollection<string> IDictionary<string, string>.Keys => Names;

		bool ICollection<KeyValuePair<string, string>>.IsReadOnly => false;

		bool ICollection<NameValuePair>.Remove(NameValuePair item)
		{
			var i = -1;
			try
			{
				if (v2Coll == null)
				{
					if ((i = unboundDict.IndexOf(item)) != -1)
						return unboundDict.Remove(item);
				}
				else
				{
					for (i = 0; i < v2Coll.Count; i++)
					{
						if (item.Equals(v2Coll[i]))
						{
							v2Coll.Remove(i);
							return true;
						}
					}
				}
				i = -1;
			}
			finally
			{
				if (i != -1)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, i));
				}
			}
			return false;
		}

		bool IDictionary<string, string>.ContainsKey(string key) => Names.Contains(key);

		void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
		{
			Add(item.Key, item.Value);
		}

		bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) =>
			((ICollection<NameValuePair>)this).Contains(new NameValuePair(item.Key, item.Value));

		void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
		{
			if (array.Length < Count + arrayIndex)
				throw new ArgumentOutOfRangeException(nameof(array), @"Array has insufficient capacity to support copy.");
			foreach (var item in ((IEnumerable<KeyValuePair<string, string>>)this))
				array[arrayIndex++] = item;
		}

		bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) =>
			((ICollection<NameValuePair>)this).Remove(new NameValuePair(item.Key, item.Value));

		IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
		{
			foreach (var nvp in this)
				yield return new KeyValuePair<string, string>(nvp.Name, nvp.Value);
		}
	}
}
