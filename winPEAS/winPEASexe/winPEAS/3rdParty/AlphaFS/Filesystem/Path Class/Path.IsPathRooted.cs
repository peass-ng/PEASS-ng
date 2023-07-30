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

      /// <summary>Gets a value indicating whether the specified path string contains absolute or relative path information.</summary>
      /// <returns><c>true</c> if <paramref name="path"/> contains a root; otherwise, <c>false</c>.</returns>
      /// <remarks>
      ///   The IsPathRooted method returns <c>true</c> if the first character is a directory separator character such as
      ///   <see cref="DirectorySeparatorChar"/>, or if the path starts with a drive letter and colon (<see cref="VolumeSeparatorChar"/>).
      ///   For example, it returns true for path strings such as "\\MyDir\\MyFile.txt", "C:\\MyDir", or "C:MyDir".
      ///   It returns <c>false</c> for path strings such as "MyDir".
      /// </remarks>
      /// <remarks>This method does not verify that the path or file name exists.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to test. The path cannot contain any of the characters defined in <see cref="GetInvalidPathChars"/>.</param>
      [SecurityCritical]
      public static bool IsPathRooted(string path)
      {
         return IsPathRootedCore(path, true);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Gets a value indicating whether the specified path string contains absolute or relative path information.</summary>
      /// <returns><c>true</c> if <paramref name="path"/> contains a root; otherwise, <c>false</c>.</returns>
      /// <remarks>
      ///   The IsPathRooted method returns true if the first character is a directory separator character such as
      ///   <see cref="DirectorySeparatorChar"/>, or if the path starts with a drive letter and colon (<see cref="VolumeSeparatorChar"/>).
      ///   For example, it returns <c>true</c> for path strings such as "\\MyDir\\MyFile.txt", "C:\\MyDir", or "C:MyDir".
      ///   It returns <c>false</c> for path strings such as "MyDir".
      /// </remarks>
      /// <remarks>This method does not verify that the path or file name exists.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to test. The path cannot contain any of the characters defined in <see cref="GetInvalidPathChars"/>.</param>
      /// <param name="checkInvalidPathChars"><c>true</c> will check <paramref name="path"/> for invalid path characters.</param>
      [SecurityCritical]
      public static bool IsPathRooted(string path, bool checkInvalidPathChars)
      {
         return IsPathRootedCore(path, checkInvalidPathChars);
      }
   }
}
