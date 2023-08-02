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
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] Gets a list of processes that have a lock on the files specified by <paramref name="filePath"/>.</summary>
      /// <returns>
      /// <c>null</c> when no processes found that are locking the file specified by <paramref name="filePath"/>.
      /// A list of processes locking the file specified by <paramref name="filePath"/>.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="InvalidOperationException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="filePath">The path to the file.</param>
      public static Collection<Process> GetProcessForFileLock(string filePath)
      {
         return GetProcessForFileLockCore(null, new Collection<string>(new[] {filePath}), PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Gets a list of processes that have a lock on the files specified by <paramref name="filePath"/>.</summary>
      /// <returns>
      /// <c>null</c> when no processes found that are locking the file specified by <paramref name="filePath"/>.
      /// A list of processes locking the file specified by <paramref name="filePath"/>.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="InvalidOperationException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="filePath">The path to the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      public static Collection<Process> GetProcessForFileLock(string filePath, PathFormat pathFormat)
      {
         return GetProcessForFileLockCore(null, new Collection<string>(new[] {filePath}), pathFormat);
      }


      /// <summary>[AlphaFS] Gets a list of processes that have a lock on the file(s) specified by <paramref name="filePaths"/>.</summary>
      /// <returns>
      /// <c>null</c> when no processes found that are locking the file(s) specified by <paramref name="filePaths"/>.
      /// A list of processes locking the file(s) specified by <paramref name="filePaths"/>.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="InvalidOperationException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="filePaths">A list with one or more file paths.</param>
      public static Collection<Process> GetProcessForFileLock(Collection<string> filePaths)
      {
         return GetProcessForFileLockCore(null, filePaths, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Gets a list of processes that have a lock on the file(s) specified by <paramref name="filePaths"/>.</summary>
      /// <returns>
      /// <c>null</c> when no processes found that are locking the file(s) specified by <paramref name="filePaths"/>.
      /// A list of processes locking the file(s) specified by <paramref name="filePaths"/>.
      /// </returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentOutOfRangeException"/>
      /// <exception cref="InvalidOperationException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="filePaths">A list with one or more file paths.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      public static Collection<Process> GetProcessForFileLock(Collection<string> filePaths, PathFormat pathFormat)
      {
         return GetProcessForFileLockCore(null, filePaths, pathFormat);
      }
   }
}
