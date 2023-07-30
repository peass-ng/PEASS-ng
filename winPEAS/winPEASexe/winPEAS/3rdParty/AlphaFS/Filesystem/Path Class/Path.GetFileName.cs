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
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      #region .NET

      /// <summary>Returns the file name and extension of the specified path string.</summary>
      /// <returns>
      ///   The characters after the last directory character in <paramref name="path"/>. If the last character of <paramref name="path"/> is a
      ///   directory or volume separator character, this method returns <c>string.Empty</c>. If path is null, this method returns null.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <param name="path">The path string from which to obtain the file name and extension. The path cannot contain any of the characters defined in <see cref="GetInvalidPathChars"/>.</param>
      [SecurityCritical]
      public static string GetFileName(string path)
      {
         return GetFileNameCore(path, true);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Returns the file name and extension of the specified path string.</summary>
      /// <returns>
      ///   The characters after the last directory character in <paramref name="path"/>. If the last character of <paramref name="path"/> is a
      ///   directory or volume separator character, this method returns <c>string.Empty</c>. If path is null, this method returns null.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <param name="path">The path string from which to obtain the file name and extension.</param>
      /// <param name="checkInvalidPathChars"><c>true</c> will check <paramref name="path"/> for invalid path characters.</param>
      [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Utils.IsNullOrWhiteSpace validates arguments.")]
      public static string GetFileName(string path, bool checkInvalidPathChars)
      {
         return GetFileNameCore(path, checkInvalidPathChars);
      }
   }
}
