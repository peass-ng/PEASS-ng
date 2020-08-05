using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Microsoft.Win32.TaskScheduler
{
	public abstract partial class Trigger
	{
		internal class CronExpression
		{
			private FieldVal[] Fields = new FieldVal[5];

			private CronExpression() { }

			public enum CronFieldType { Minutes, Hours, Days, Months, DaysOfWeek };

            public struct FieldVal
			{
				private const string rangeRegEx = @"^(?:(?<A>\*)|(?<D1>\d+)(?:-(?<D2>\d+))?)(?:\/(?<I>\d+))?$";
				private readonly static Dictionary<string, string> dow = new Dictionary<string, string>(7)
				{
					{ "SUN", "0" },
					{ "MON", "1" },
					{ "TUE", "2" },
					{ "WED", "3" },
					{ "THU", "4" },
					{ "FRI", "5" },
					{ "SAT", "6" },
				};
				private readonly static Dictionary<string, string> mon = new Dictionary<string, string>(12)
				{
					{ "JAN", "1" },
					{ "FEB", "2" },
					{ "MAR", "3" },
					{ "APR", "4" },
					{ "MAY", "5" },
					{ "JUN", "6" },
					{ "JUL", "7" },
					{ "AUG", "8" },
					{ "SEP", "9" },
					{ "OCT", "10" },
					{ "NOV", "11" },
					{ "DEC", "12" },
				};
				private readonly static Dictionary<CronFieldType, MinMax> validRange = new Dictionary<CronFieldType, MinMax>(5)
				{
					{ CronFieldType.Days, new MinMax(1, 31) },
					{ CronFieldType.DaysOfWeek, new MinMax(0, 6) },
					{ CronFieldType.Hours, new MinMax(0, 23) },
					{ CronFieldType.Minutes, new MinMax(0, 59) },
					{ CronFieldType.Months, new MinMax(1, 12) },
				};
				private CronFieldType cft;
				private FieldFlags flags;
				private int incr;
				private int[] vals;

				enum FieldFlags { List, Every, Range, Increment };

				public override string ToString() => $"Type:{flags}; Vals:{string.Join(",", vals.Select(i => i.ToString()).ToArray())}; Incr:{incr}";

				private struct MinMax
				{
					public int Min, Max;
					public MinMax(int min, int max) { Min = min; Max = max; }
				}
			}
		}
	}
}