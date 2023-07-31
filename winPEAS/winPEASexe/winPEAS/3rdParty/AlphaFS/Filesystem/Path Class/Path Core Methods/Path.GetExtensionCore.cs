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
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>Returns the extension of the specified path string.</summary>
      /// <returns>
      ///   <para>The extension of the specified path (including the period "."), or null, or <see cref="string.Empty"/>.</para>
      ///   <para>If <paramref name="path"/> is null, this method returns null.</para>
      ///   <para>If <paramref name="path"/> does not have extension information,
      ///   this method returns <see cref="string.Empty"/>.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <param name="path">The path string from which to get the extension. The path cannot contain any of the characters defined in <see cref="GetInvalidPathChars"/>.</param>
      /// <param name="checkInvalidPathChars"><c>true</c> will check <paramref name="path"/> for invalid path characters.</param>
      [SecurityCritical]
      internal static string GetExtensionCore(string path, bool checkInvalidPathChars)
      {
         if (null == path)
            return null;

         if (checkInvalidPathChars)
            CheckInvalidPathChars(path, false, true);

         var length = path.Length;
         var index = length;

         while (--index >= 0)
         {
            var ch = path[index];

            if (ch == ExtensionSeparatorChar)
               return index != length - 1 ? path.Substring(index, length - index) : string.Empty;

            if (IsDVsc(ch, null))
               break;
         }

         return string.Empty;
      }
   }
}
