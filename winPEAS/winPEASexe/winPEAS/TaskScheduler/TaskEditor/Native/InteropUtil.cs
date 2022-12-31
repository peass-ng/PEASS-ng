using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace winPEAS.TaskScheduler.TaskEditor.Native
{
    internal static class InteropUtil
    {
        private const int cbBuffer = 256;

        public static T ToStructure<T>(IntPtr ptr) => (T)Marshal.PtrToStructure(ptr, typeof(T));

        public static IntPtr StructureToPtr(object value)
        {
            IntPtr ret = Marshal.AllocHGlobal(Marshal.SizeOf(value));
            Marshal.StructureToPtr(value, ret, false);
            return ret;
        }

        public static void AllocString(ref IntPtr ptr, ref uint size)
        {
            FreeString(ref ptr, ref size);
            if (size == 0) size = cbBuffer;
            ptr = Marshal.AllocHGlobal(cbBuffer);
        }

        public static void FreeString(ref IntPtr ptr, ref uint size)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
                ptr = IntPtr.Zero;
                size = 0;
            }
        }

        public static string GetString(IntPtr pString) => Marshal.PtrToStringUni(pString);

        public static bool SetString(ref IntPtr ptr, ref uint size, string value = null)
        {
            string s = GetString(ptr);
            if (value == string.Empty) value = null;
            if (string.CompareOrdinal(s, value) != 0)
            {
                FreeString(ref ptr, ref size);
                if (value != null)
                {
                    ptr = Marshal.StringToHGlobalUni(value);
                    size = (uint)value.Length + 1;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Converts an <see cref="IntPtr"/> that points to a C-style array into a CLI array.
        /// </summary>
        /// <typeparam name="TS">Type of native structure used by the C-style array.</typeparam>
        /// <typeparam name="T">Output type for the CLI array. <typeparamref name="TS"/> must be able to convert to <typeparamref name="T"/>.</typeparam>
        /// <param name="ptr">The <see cref="IntPtr"/> pointing to the native array.</param>
        /// <param name="count">The number of items in the native array.</param>
        /// <returns>An array of type <typeparamref name="T"/> containing the converted elements of the native array.</returns>
        public static T[] ToArray<TS, T>(IntPtr ptr, int count) where TS : IConvertible
        {
            var ret = new T[count];
            var stSize = Marshal.SizeOf(typeof(TS));
            for (var i = 0; i < count; i++)
            {
                var tempPtr = new IntPtr(ptr.ToInt64() + (i * stSize));
                var val = ToStructure<TS>(tempPtr);
                ret[i] = (T)Convert.ChangeType(val, typeof(T));
            }
            return ret;
        }

        /// <summary>
        /// Converts an <see cref="IntPtr"/> that points to a C-style array into a CLI array.
        /// </summary>
        /// <typeparam name="T">Type of native structure used by the C-style array.</typeparam>
        /// <param name="ptr">The <see cref="IntPtr"/> pointing to the native array.</param>
        /// <param name="count">The number of items in the native array.</param>
        /// <returns>An array of type <typeparamref name="T"/> containing the elements of the native array.</returns>
        public static T[] ToArray<T>(IntPtr ptr, int count)
        {
            var ret = new T[count];
            var stSize = Marshal.SizeOf(typeof(T));
            for (var i = 0; i < count; i++)
            {
                var tempPtr = new IntPtr(ptr.ToInt64() + (i * stSize));
                ret[i] = ToStructure<T>(tempPtr);
            }
            return ret;
        }
    }

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
