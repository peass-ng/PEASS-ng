using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPass.BouncyCastle.util.collections
{
	public class UnmodifiableSetProxy
		 : UnmodifiableSet
	{
		private readonly ISet s;

		public UnmodifiableSetProxy(ISet s)
		{
			this.s = s;
		}

		public override bool Contains(object o)
		{
			return s.Contains(o);
		}

		public override void CopyTo(Array array, int index)
		{
			s.CopyTo(array, index);
		}

		public override int Count
		{
			get { return s.Count; }
		}

		public override IEnumerator GetEnumerator()
		{
			return s.GetEnumerator();
		}

		public override bool IsEmpty
		{
			get { return s.IsEmpty; }
		}

		public override bool IsFixedSize
		{
			get { return s.IsFixedSize; }
		}

		public override bool IsSynchronized
		{
			get { return s.IsSynchronized; }
		}

		public override object SyncRoot
		{
			get { return s.SyncRoot; }
		}
	}
}
