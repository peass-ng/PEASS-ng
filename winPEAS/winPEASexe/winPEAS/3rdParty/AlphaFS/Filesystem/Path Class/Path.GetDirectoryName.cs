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
      #region .NET

      /// <summary>Returns the directory information for the specified path string.</summary>
      /// <returns>
      ///   <para>Directory information for <paramref name="path"/>, or <c>null</c> if <paramref name="path"/> denotes a root directory or is
      ///   <c>null</c>.</para>
      ///   <para>Returns <see cref="string.Empty"/> if <paramref name="path"/> does not contain directory information.</para>
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <param name="path">The path of a file or directory.</param>
      [SecurityCritical]
      public static string GetDirectoryName(string path)
      {
         return GetDirectoryNameCore(path, true);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Returns the directory information for the specified path string.</summary>
      /// <returns>
      ///   Directory information for <paramref name="path"/>, or <c>null</c> if <paramref name="path"/> denotes a root directory or is
      ///   <c>null</c>. Returns <see cref="string.Empty"/> if <paramref name="path"/> does not contain directory information.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <param name="path">The path of a file or directory.</param>
      /// <param name="checkInvalidPathChars"><c>true</c> will check <paramref name="path"/> for invalid path characters.</param>
      [SecurityCritical]
      public static string GetDirectoryName(string path, bool checkInvalidPathChars)
      {
         return GetDirectoryNameCore(path, checkInvalidPathChars);
      }
   }
}
