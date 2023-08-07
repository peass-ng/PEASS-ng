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
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public sealed partial class DirectoryInfo
   {
      #region .NET

      /// <summary>Returns an enumerable collection of file information in the current directory.</summary>
      /// <returns>An enumerable collection of the files in the current directory.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      public IEnumerable<FileInfo> EnumerateFiles()
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, Path.WildcardStarMatchAll, null, null, null, PathFormat.LongFullPath);
      }


      /// <summary>Returns an enumerable collection of file information that matches a search pattern.</summary>
      /// <returns>An enumerable collection of files that matches <paramref name="searchPattern"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in path.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      [SecurityCritical]
      public IEnumerable<FileInfo> EnumerateFiles(string searchPattern)
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, searchPattern, null, null, null, PathFormat.LongFullPath);
      }


      /// <summary>Returns an enumerable collection of file information that matches a specified search pattern and search subdirectory option.</summary>
      /// <returns>An enumerable collection of files that matches <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in path.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="searchOption">
      ///   One of the <see cref="SearchOption"/> enumeration values that specifies whether the <paramref name="searchOption"/>
      ///   should include only the current directory or should include all subdirectories.
      /// </param>
      [SecurityCritical]
      public IEnumerable<FileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, searchPattern, searchOption, null, null, PathFormat.LongFullPath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Returns an enumerable collection of file information in the current directory.</summary>
      /// <returns>An enumerable collection of the files in the current directory.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      [SecurityCritical]
      public IEnumerable<FileInfo> EnumerateFiles(DirectoryEnumerationOptions options)
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, Path.WildcardStarMatchAll, null, options, null, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of file information that matches a search pattern.</summary>
      /// <returns>An enumerable collection of files that matches <paramref name="searchPattern"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in path.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      [SecurityCritical]
      public IEnumerable<FileInfo> EnumerateFiles(string searchPattern, DirectoryEnumerationOptions options)
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, searchPattern, null, options, null, PathFormat.LongFullPath);
      }
   }
}
