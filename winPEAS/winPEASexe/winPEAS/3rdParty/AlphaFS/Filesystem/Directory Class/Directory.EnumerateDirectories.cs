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
using SearchOption = System.IO.SearchOption;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      #region .NET

      /// <summary>Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, null, null, PathFormat.RelativePath);
      }


      /// <summary>Returns an enumerable collection of directory names that match a <paramref name="searchPattern"/> in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/> and that match the specified <paramref name="searchPattern"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, null, null, PathFormat.RelativePath);
      }


      /// <summary>Returns an enumerable collection of directory names that match a <paramref name="searchPattern"/> in a specified <paramref name="path"/>, and optionally searches subdirectories.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/> and that match the specified <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="searchOption">
      ///   One of the <see cref="SearchOption"/> enumeration values that specifies whether the <paramref name="searchOption"/>
      ///   should include only the current directory or should include all subdirectories.
      /// </param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, searchOption, null, null, PathFormat.RelativePath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, null, null, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names that match a <paramref name="searchPattern"/> in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/> and that match the specified <paramref name="searchPattern"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, null, null, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names that match a <paramref name="searchPattern"/> in a specified <paramref name="path"/>, and optionally searches subdirectories.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/> and that match the specified <paramref name="searchPattern"/> and <paramref name="searchOption"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="searchOption">
      ///   One of the <see cref="SearchOption"/> enumeration values that specifies whether the <paramref name="searchOption"/>
      ///   should include only the current directory or should include all subdirectories.
      /// </param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, searchOption, null, null, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, DirectoryEnumerationOptions options)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, options, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, DirectoryEnumerationOptions options, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, options, null, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, DirectoryEnumerationOptions options)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, options, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, DirectoryEnumerationOptions options, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, options, null, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, DirectoryEnumerationFilters filters)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, null, filters, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, null, filters, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, DirectoryEnumerationFilters filters)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, null, filters, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, null, filters, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, DirectoryEnumerationOptions options, DirectoryEnumerationFilters filters)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, options, filters, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static IEnumerable<string> EnumerateDirectories(string path, DirectoryEnumerationOptions options, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, Path.WildcardStarMatchAll, null, options, filters, pathFormat);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, DirectoryEnumerationOptions options, DirectoryEnumerationFilters filters)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, options, filters, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Returns an enumerable collection of directory names in a specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of the full names (including paths) for the directories in the directory specified by <paramref name="path"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///   The search string to match against the names of directories in <paramref name="path"/>.
      ///   This parameter can contain a combination of valid literal path and wildcard
      ///   (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Argument searchPattern is obsolete. The DirectoryEnumerationFilters argument provides better filter criteria.")]
      public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, DirectoryEnumerationOptions options, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return EnumerateFileSystemEntryInfosCore<string>(true, null, path, searchPattern, null, options, filters, pathFormat);
      }
   }
}
