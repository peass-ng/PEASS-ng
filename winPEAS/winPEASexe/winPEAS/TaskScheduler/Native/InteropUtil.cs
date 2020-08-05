using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System.Runtime.InteropServices
{
	internal class ComEnumerator<T, TIn> : IEnumerator<T> where T : class where TIn : class
	{
		protected readonly Func<TIn, T> converter;
		protected IEnumerator<TIn> iEnum;

		public ComEnumerator(Func<int> getCount, Func<int, TIn> indexer, Func<TIn, T> converter)
		{
			IEnumerator<TIn> Enumerate()
			{
				for (var x = 1; x <= getCount(); x++)
					yield return indexer(x);
			}

			this.converter = converter;
			iEnum = Enumerate();
		}

		public ComEnumerator(Func<int> getCount, Func<object, TIn> indexer, Func<TIn, T> converter)
		{
			IEnumerator<TIn> Enumerate()
			{
				for (var x = 1; x <= getCount(); x++)
					yield return indexer(x);
			}

			this.converter = converter;
			iEnum = Enumerate();
		}

		object IEnumerator.Current => Current;

		public virtual T Current => converter(iEnum?.Current);

		public virtual void Dispose()
		{
			iEnum?.Dispose();
			iEnum = null;
		}

		public virtual bool MoveNext() => iEnum?.MoveNext() ?? false;

		public virtual void Reset()
		{
			iEnum?.Reset();
		}
	}
}