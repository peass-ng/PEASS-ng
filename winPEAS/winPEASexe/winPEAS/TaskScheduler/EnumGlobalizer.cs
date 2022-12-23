using System;
using System.Collections.Generic;
using System.Globalization;

namespace winPEAS.TaskScheduler
{
    /// <summary>
    /// Functions to provide localized strings for enumerated types and values.
    /// </summary>
    public static class TaskEnumGlobalizer
    {
        /// <summary>
        /// Gets a string representing the localized value of the provided enum.
        /// </summary>
        /// <param name="enumValue">The enum value.</param>
        /// <returns>A localized string, if available.</returns>
        public static string GetString(object enumValue)
        {
            switch (enumValue.GetType().Name)
            {
                case "DaysOfTheWeek":
                    return GetCultureEquivalentString((DaysOfTheWeek)enumValue);
                case "MonthsOfTheYear":
                    return GetCultureEquivalentString((MonthsOfTheYear)enumValue);
                case "TaskTriggerType":
                    return BuildEnumString("TriggerType", enumValue);
                case "WhichWeek":
                    return BuildEnumString("WW", enumValue);
                case "TaskActionType":
                    return BuildEnumString("ActionType", enumValue);
                case "TaskState":
                    return BuildEnumString("TaskState", enumValue);
            }
            return enumValue.ToString();
        }

        private static string GetCultureEquivalentString(DaysOfTheWeek val)
        {
            if (val == DaysOfTheWeek.AllDays)
                return Properties.Resources.DOWAllDays;

            var s = new List<string>(7);
            var vals = Enum.GetValues(val.GetType());
            for (var i = 0; i < vals.Length - 1; i++)
            {
                if ((val & (DaysOfTheWeek)vals.GetValue(i)) > 0)
                    s.Add(DateTimeFormatInfo.CurrentInfo.GetDayName((DayOfWeek)i));
            }

            return string.Join(Properties.Resources.ListSeparator, s.ToArray());
        }

        private static string GetCultureEquivalentString(MonthsOfTheYear val)
        {
            if (val == MonthsOfTheYear.AllMonths)
                return Properties.Resources.MOYAllMonths;

            var s = new List<string>(12);
            var vals = Enum.GetValues(val.GetType());
            for (var i = 0; i < vals.Length - 1; i++)
            {
                if ((val & (MonthsOfTheYear)vals.GetValue(i)) > 0)
                    s.Add(DateTimeFormatInfo.CurrentInfo.GetMonthName(i + 1));
            }

            return string.Join(Properties.Resources.ListSeparator, s.ToArray());
        }

        private static string BuildEnumString(string preface, object enumValue)
        {
            var vals = enumValue.ToString().Split(new[] { ", " }, StringSplitOptions.None);
            if (vals.Length == 0)
                return string.Empty;
            for (var i = 0; i < vals.Length; i++)
                vals[i] = Properties.Resources.ResourceManager.GetString(preface + vals[i]);
            return string.Join(Properties.Resources.ListSeparator, vals);
        }
    }
}
