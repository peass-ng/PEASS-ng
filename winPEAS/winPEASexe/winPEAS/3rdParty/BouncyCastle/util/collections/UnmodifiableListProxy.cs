using System;
using System.Collections;

namespace winPEAS._3rdParty.BouncyCastle.util.collections
{
	public class UnmodifiableListProxy
	 : UnmodifiableList
	{
		private readonly IList l;

		public UnmodifiableListProxy(IList l)
		{
			this.l = l;
		}

		public override bool Contains(object o)
		{
			return l.Contains(o);
		}

		public override void CopyTo(Array array, int index)
		{
			l.CopyTo(array, index);
		}

		public override int Count
		{
			get { return l.Count; }
		}

		public override IEnumerator GetEnumerator()
		{
			return l.GetEnumerator();
		}

		public override int IndexOf(object o)
		{
			return l.IndexOf(o);
		}

		public override bool IsFixedSize
		{
			get { return l.IsFixedSize; }
		}

		public override bool IsSynchronized
		{
			get { return l.IsSynchronized; }
		}

		public override object SyncRoot
		{
			get { return l.SyncRoot; }
		}

		protected override object GetValue(int i)
		{
			return l[i];
		}
	}
}
