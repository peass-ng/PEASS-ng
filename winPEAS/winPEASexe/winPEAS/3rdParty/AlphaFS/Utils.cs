/*  Copyright (C) 2008-2018 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy 
 *  of this software and associated documentation files (the "Software"), to deal 
 *  in the Software without restriction, including without limitation the rights 
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 *  copies of the Software, and to permit persons to whom the Software is 
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 *  THE SOFTWARE. 
 */

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Alphaleonis
{
   internal static class Utils
   {
      // Source: https://stackoverflow.com/questions/6275980/string-replace-ignoring-case/45756981#45756981


      /// <summary>Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string, ignoring any casing difference.</summary>
      internal static string ReplaceIgnoreCase(this string str, string oldValue, string newValue)
      {
         if (null == str)
            throw new ArgumentNullException("str");

         if (str.Trim().Length == 0)
            return str;

         if (null == oldValue)
            throw new ArgumentNullException("oldValue");

         if (oldValue.Trim().Length == 0)
            throw new ArgumentException("String cannot be of zero length.");


         // Prepare string builder for storing the processed string.
         var resultStringBuilder = new StringBuilder(str.Length);

         // Analyze the replacement: replace or remove.
         var isReplacementNullOrWhiteSpace = IsNullOrWhiteSpace(newValue);


         int foundAt;
         const int valueNotFound = -1;
         var startSearchFromIndex = 0;

         while ((foundAt = str.IndexOf(oldValue, startSearchFromIndex, StringComparison.OrdinalIgnoreCase)) != valueNotFound)
         {
            // Append all characters until the found replacement.
            var charsUntilReplacment = foundAt - startSearchFromIndex;

            var isNothingToAppend = charsUntilReplacment == 0;

            if (!isNothingToAppend)
               resultStringBuilder.Append(str, startSearchFromIndex, charsUntilReplacment);

            if (!isReplacementNullOrWhiteSpace)
               resultStringBuilder.Append(newValue);


            // Prepare start index for the next search.
            // This needed to prevent infinite loop, otherwise method always start search 
            // from the start of the string. For example: if an oldValue == "EXAMPLE", newValue == "example"
            // and comparisonType == "any ignore case" will conquer to replacing:
            // "EXAMPLE" to "example" to "example" to "example" … infinite loop.
            startSearchFromIndex = foundAt + oldValue.Length;

            if (startSearchFromIndex == str.Length)
            {
               // It is end of the input string: no more space for the next search.
               // The input string ends with a value that has already been replaced. 
               // Therefore, the string builder with the result is complete and no further action is required.
               return resultStringBuilder.ToString();
            }
         }


         // Append the last part to the result.
         var charsUntilStringEnd = str.Length - startSearchFromIndex;

         resultStringBuilder.Append(str, startSearchFromIndex, charsUntilStringEnd);


         return resultStringBuilder.ToString();
      }


      /// <summary>Gets an attribute on an enum field value.</summary>
      /// <returns>The description belonging to the enum option, as a string</returns>
      /// <param name="enumValue">One of the <see cref="Alphaleonis.Win32.Filesystem.DeviceGuid"/> enum types.</param>
      [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
      public static string GetEnumDescription(Enum enumValue)
      {
         var enumValueString = enumValue.ToString();

         var fi = enumValue.GetType().GetField(enumValueString);

         var attributes = (DescriptionAttribute[]) fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

         return attributes.Length > 0 ? attributes[0].Description : enumValueString;
      }


      /// <summary>Checks that the object is not null.</summary>
      public static bool IsNotNull<T>(T obj)
      {
         return !Equals(null, obj);
      }


      /// <summary>Indicates whether a specified string is null, empty, or consists only of white-space characters.</summary>
      /// <returns><c>true</c> if the <paramref name="value"/> parameter is null or <see cref="string.Empty"/>, or if <paramref name="value"/> consists exclusively of white-space characters.</returns>
      /// <param name="value">The string to test.</param>
      public static bool IsNullOrWhiteSpace(string value)
      {
#if NET35
         if (null != value)
         {
            for (int index = 0, l = value.Length; index < l; ++index)
               if (!char.IsWhiteSpace(value[index]))
                  return false;
         }

         return true;
#else
         return string.IsNullOrWhiteSpace(value);
#endif
      }


      /// <summary>Converts a number of type T to string formated using <see cref="CultureInfo.InvariantCulture"/>, suffixed with a unit size.</summary>
      public static string UnitSizeToText<T>(T numberOfBytes)
      {
         // CultureInfo.CurrentCulture uses the culture as set in the Region applet.

         return UnitSizeToText(numberOfBytes, CultureInfo.CurrentCulture);
      }


      /// <summary>Converts a number of type T to string formated using the specified <paramref name="cultureInfo"/>, suffixed with a unit size.</summary>
      public static string UnitSizeToText<T>(T numberOfBytes, CultureInfo cultureInfo)
      {
         var sizeFormats = new[] {"B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};
         const int kb = 1024;
         var index = 0;

         var bytes = Convert.ToDouble(numberOfBytes, CultureInfo.InvariantCulture);

         if (bytes < 0)
            bytes = 0;
         
         else
            while (bytes > kb)
            {
               bytes /= kb;
               index++;
            }


         // Will return "512 B" instead of "512,00 B".

         return string.Format(cultureInfo, "{0} {1}", bytes.ToString(index == 0 ? "0" : "0.##", cultureInfo), sizeFormats[index]);
      }


      public static int CombineHashCodesOf<T1, T2>(T1 arg1, T2 arg2)
      {
         unchecked
         {
            var hash = 17;

            hash = hash * 23 + (!Equals(arg1, default(T1)) ? arg1.GetHashCode() : 0);
            hash = hash * 23 + (!Equals(arg2, default(T2)) ? arg2.GetHashCode() : 0);

            return hash;
         }
      }


      public static int CombineHashCodesOf<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
      {
         unchecked
         {
            var hash = CombineHashCodesOf(arg1, arg2);

            hash = hash * 23 + (!Equals(arg3, default(T3)) ? arg3.GetHashCode() : 0);

            return hash;
         }
      }


      public static int CombineHashCodesOf<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
      {
         return CombineHashCodesOf(CombineHashCodesOf(arg1, arg2), CombineHashCodesOf(arg3, arg4));
      }
   }
}
