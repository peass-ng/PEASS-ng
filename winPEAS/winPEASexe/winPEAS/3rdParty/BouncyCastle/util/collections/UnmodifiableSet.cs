using System;
using System.Collections;

namespace winPEAS._3rdParty.BouncyCastle.util.collections
{
	public abstract class UnmodifiableSet
		  : ISet
	{
		protected UnmodifiableSet()
		{
		}

		public virtual void Add(object o)
		{
			throw new NotSupportedException();
		}

		public virtual void AddAll(IEnumerable e)
		{
			throw new NotSupportedException();
		}

		public virtual void Clear()
		{
			throw new NotSupportedException();
		}

		public abstract bool Contains(object o);

		public abstract void CopyTo(Array array, int index);

		public abstract int Count { get; }

		public abstract IEnumerator GetEnumerator();

		public abstract bool IsEmpty { get; }

		public abstract bool IsFixedSize { get; }

		public virtual bool IsReadOnly
		{
			get { return true; }
		}

		public abstract bool IsSynchronized { get; }

		public abstract object SyncRoot { get; }

		public virtual void Remove(object o)
		{
			throw new NotSupportedException();
		}

		public virtual void RemoveAll(IEnumerable e)
		{
			throw new NotSupportedException();
		}
	}
}
