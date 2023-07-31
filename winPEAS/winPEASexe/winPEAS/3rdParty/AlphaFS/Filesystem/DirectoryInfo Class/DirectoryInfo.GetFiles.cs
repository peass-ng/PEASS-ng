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
using System.IO;
using System.Linq;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public sealed partial class DirectoryInfo
   {
      #region .NET

      /// <summary>Returns a file list from the current directory.</summary>
      /// <returns>An array of type <see cref="FileInfo"/>.</returns>
      /// <remarks>The order of the returned file names is not guaranteed; use the Sort() method if a specific sort order is required.</remarks>
      /// <remarks>If there are no files in the <see cref="DirectoryInfo"/>, this method returns an empty array.</remarks>
      /// <remarks>
      /// The EnumerateFiles and GetFiles methods differ as follows: When you use EnumerateFiles, you can start enumerating the collection of names
      /// before the whole collection is returned; when you use GetFiles, you must wait for the whole array of names to be returned before you can access the array.
      /// Therefore, when you are working with many files and directories, EnumerateFiles can be more efficient.
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      public FileInfo[] GetFiles()
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, Path.WildcardStarMatchAll, null, null, null, PathFormat.LongFullPath).ToArray();
      }


      /// <summary>Returns a file list from the current directory matching the given search pattern.</summary>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in path.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <returns>An array of type <see cref="FileInfo"/>.</returns>
      /// <remarks>The order of the returned file names is not guaranteed; use the Sort() method if a specific sort order is required.</remarks>
      /// <remarks>If there are no files in the <see cref="DirectoryInfo"/>, this method returns an empty array.</remarks>
      /// <remarks>
      /// The EnumerateFiles and GetFiles methods differ as follows: When you use EnumerateFiles, you can start enumerating the collection of names
      /// before the whole collection is returned; when you use GetFiles, you must wait for the whole array of names to be returned before you can access the array.
      /// Therefore, when you are working with many files and directories, EnumerateFiles can be more efficient.
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      public FileInfo[] GetFiles(string searchPattern)
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, searchPattern, null, null, null, PathFormat.LongFullPath).ToArray();
      }


      /// <summary>Returns a file list from the current directory matching the given search pattern and using a value to determine whether to search subdirectories.</summary>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in path.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="searchOption">
      ///   One of the <see cref="SearchOption"/> enumeration values that specifies whether the <paramref name="searchOption"/>
      ///   should include only the current directory or should include all subdirectories.
      /// </param>
      /// <returns>An array of type <see cref="FileInfo"/>.</returns>
      /// <remarks>The order of the returned file names is not guaranteed; use the Sort() method if a specific sort order is required.</remarks>
      /// <remarks>If there are no files in the <see cref="DirectoryInfo"/>, this method returns an empty array.</remarks>
      /// <remarks>
      /// The EnumerateFiles and GetFiles methods differ as follows: When you use EnumerateFiles, you can start enumerating the collection of names
      /// before the whole collection is returned; when you use GetFiles, you must wait for the whole array of names to be returned before you can access the array.
      /// Therefore, when you are working with many files and directories, EnumerateFiles can be more efficient.
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      public FileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
      {
         return Directory.EnumerateFileSystemEntryInfosCore<FileInfo>(false, Transaction, LongFullName, searchPattern, searchOption, null, null, PathFormat.LongFullPath).ToArray();
      }

      #endregion // .NET
   }
}
