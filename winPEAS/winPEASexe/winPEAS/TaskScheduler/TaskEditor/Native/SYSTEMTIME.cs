using System;
using System.Runtime.InteropServices;

namespace winPEAS.TaskScheduler.TaskEditor.Native
{
    internal static partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        internal struct SYSTEMTIME : IConvertible
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                dt = dt.ToLocalTime();
                Year = Convert.ToUInt16(dt.Year);
                Month = Convert.ToUInt16(dt.Month);
                DayOfWeek = Convert.ToUInt16(dt.DayOfWeek);
                Day = Convert.ToUInt16(dt.Day);
                Hour = Convert.ToUInt16(dt.Hour);
                Minute = Convert.ToUInt16(dt.Minute);
                Second = Convert.ToUInt16(dt.Second);
                Milliseconds = Convert.ToUInt16(dt.Millisecond);
            }

            public SYSTEMTIME(ushort year, ushort month, ushort day, ushort hour = 0, ushort minute = 0, ushort second = 0, ushort millisecond = 0)
            {
                Year = year;
                Month = month;
                Day = day;
                Hour = hour;
                Minute = minute;
                Second = second;
                Milliseconds = millisecond;
                DayOfWeek = 0;
            }

            public static implicit operator DateTime(SYSTEMTIME st)
            {
                if (st.Year == 0 || st == MinValue)
                    return DateTime.MinValue;
                if (st == MaxValue)
                    return DateTime.MaxValue;
                return new DateTime(st.Year, st.Month, st.Day, st.Hour, st.Minute, st.Second, st.Milliseconds, DateTimeKind.Local);
            }

            public static implicit operator SYSTEMTIME(DateTime dt) => new SYSTEMTIME(dt);

            public static bool operator ==(SYSTEMTIME s1, SYSTEMTIME s2) => (s1.Year == s2.Year && s1.Month == s2.Month && s1.Day == s2.Day && s1.Hour == s2.Hour && s1.Minute == s2.Minute && s1.Second == s2.Second && s1.Milliseconds == s2.Milliseconds);

            public static bool operator !=(SYSTEMTIME s1, SYSTEMTIME s2) => !(s1 == s2);

            public static readonly SYSTEMTIME MinValue, MaxValue;

            static SYSTEMTIME()
            {
                MinValue = new SYSTEMTIME(1601, 1, 1);
                MaxValue = new SYSTEMTIME(30827, 12, 31, 23, 59, 59, 999);
            }

            public override bool Equals(object obj)
            {
                if (obj is SYSTEMTIME)
                    return ((SYSTEMTIME)obj) == this;
                if (obj is DateTime)
                    return ((DateTime)this).Equals(obj);
                return base.Equals(obj);
            }

            public override int GetHashCode() => ((DateTime)this).GetHashCode();

            public override string ToString() => ((DateTime)this).ToString();

            TypeCode IConvertible.GetTypeCode() => ((IConvertible)(DateTime)this).GetTypeCode();

            bool IConvertible.ToBoolean(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToBoolean(provider);

            byte IConvertible.ToByte(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToByte(provider);

            char IConvertible.ToChar(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToChar(provider);

            DateTime IConvertible.ToDateTime(IFormatProvider provider) => (DateTime)this;

            decimal IConvertible.ToDecimal(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToDecimal(provider);

            double IConvertible.ToDouble(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToDouble(provider);

            short IConvertible.ToInt16(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToInt16(provider);

            int IConvertible.ToInt32(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToInt32(provider);

            long IConvertible.ToInt64(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToInt64(provider);

            sbyte IConvertible.ToSByte(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToSByte(provider);

            float IConvertible.ToSingle(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToSingle(provider);

            string IConvertible.ToString(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToString(provider);

            object IConvertible.ToType(Type conversionType, IFormatProvider provider) => ((IConvertible)(DateTime)this).ToType(conversionType, provider);

            ushort IConvertible.ToUInt16(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToUInt16(provider);

            uint IConvertible.ToUInt32(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToUInt32(provider);

            ulong IConvertible.ToUInt64(IFormatProvider provider) => ((IConvertible)(DateTime)this).ToUInt64(provider);
        }
    }
}
