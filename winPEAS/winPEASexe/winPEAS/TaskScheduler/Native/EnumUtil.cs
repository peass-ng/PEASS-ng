using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace System
{
	internal static class EnumUtil
	{
		public static void CheckIsEnum<T>(bool checkHasFlags = false)
		{
			if (!typeof(T).IsEnum)
				throw new ArgumentException($"Type '{typeof(T).FullName}' is not an enum");
			if (checkHasFlags && !IsFlags<T>())
				throw new ArgumentException($"Type '{typeof(T).FullName}' doesn't have the 'Flags' attribute");
		}

		public static bool IsFlags<T>() => Attribute.IsDefined(typeof(T), typeof(FlagsAttribute));

		public static bool IsFlagSet<T>(this T flags, T flag) where T : struct, IConvertible
		{
			CheckIsEnum<T>(true);
			var flagValue = Convert.ToInt64(flag);
			return (Convert.ToInt64(flags) & flagValue) == flagValue;
		}

		public static bool IsValidFlagValue<T>(this T flags) where T : struct, IConvertible
		{
			CheckIsEnum<T>(true);
			var found = 0L;
			foreach (T flag in Enum.GetValues(typeof(T)))
			{
				if (flags.IsFlagSet(flag))
					found |= Convert.ToInt64(flag);
			}
			return found == Convert.ToInt64(flags);
		}

		public static void SetFlags<T>(ref T flags, T flag, bool set = true) where T : struct, IConvertible
		{
			CheckIsEnum<T>(true);
			var flagsValue = Convert.ToInt64(flags);
			var flagValue = Convert.ToInt64(flag);
			if (set)
				flagsValue |= flagValue;
			else
				flagsValue &= (~flagValue);
			flags = (T)Enum.ToObject(typeof(T), flagsValue);
		}

		public static T SetFlags<T>(this T flags, T flag, bool set = true) where T : struct, IConvertible
		{
			var ret = flags;
			SetFlags<T>(ref ret, flag, set);
			return ret;
		}
		}
}
