using System;
using System.Collections;

namespace winPEAS._3rdParty.BouncyCastle.util.collections
{
	public class UnmodifiableDictionaryProxy
	 : UnmodifiableDictionary
	{
		private readonly IDictionary d;

		public UnmodifiableDictionaryProxy(IDictionary d)
		{
			this.d = d;
		}

		public override bool Contains(object k)
		{
			return d.Contains(k);
		}

		public override void CopyTo(Array array, int index)
		{
			d.CopyTo(array, index);
		}

		public override int Count
		{
			get { return d.Count; }
		}

		public override IDictionaryEnumerator GetEnumerator()
		{
			return d.GetEnumerator();
		}

		public override bool IsFixedSize
		{
			get { return d.IsFixedSize; }
		}

		public override bool IsSynchronized
		{
			get { return d.IsSynchronized; }
		}

		public override object SyncRoot
		{
			get { return d.SyncRoot; }
		}

		public override ICollection Keys
		{
			get { return d.Keys; }
		}

		public override ICollection Values
		{
			get { return d.Values; }
		}

		protected override object GetValue(object k)
		{
			return d[k];
		}
	}
}
