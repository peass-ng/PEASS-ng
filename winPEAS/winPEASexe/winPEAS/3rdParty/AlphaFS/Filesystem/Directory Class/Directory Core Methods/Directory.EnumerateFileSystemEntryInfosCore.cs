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
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Returns an enumerable collection of file system entries in a specified path using <see cref="DirectoryEnumerationOptions"/> and <see cref="DirectoryEnumerationFilters"/>.</summary>
      /// <returns>The matching file system entries. The type of the items is determined by the type <typeparamref name="T"/>.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <typeparam name="T">The type to return. This may be one of the following types:
      ///    <list type="definition">
      ///    <item>
      ///       <term><see cref="FileSystemEntryInfo"/></term>
      ///       <description>This method will return instances of <see cref="FileSystemEntryInfo"/> instances.</description>
      ///    </item>
      ///    <item>
      ///       <term><see cref="FileSystemInfo"/></term>
      ///       <description>This method will return instances of <see cref="DirectoryInfo"/> and <see cref="FileInfo"/> instances.</description>
      ///    </item>
      ///    <item>
      ///       <term><see cref="string"/></term>
      ///       <description>This method will return the full path of each item.</description>
      ///    </item>
      /// </list>
      /// </typeparam>
      /// <param name="onlyFolders"></param>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The directory to search.</param>
      /// <param name="searchPattern">
      ///    The search string to match against the names of directories in <paramref name="path"/>.
      ///    This parameter can contain a combination of valid literal path and wildcard
      ///    (<see cref="Path.WildcardStarMatchAll"/> and <see cref="Path.WildcardQuestion"/>) characters, but does not support regular expressions.
      /// </param>
      /// <param name="searchOption"></param>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static IEnumerable<T> EnumerateFileSystemEntryInfosCore<T>(bool? onlyFolders, KernelTransaction transaction, string path, string searchPattern, SearchOption? searchOption, DirectoryEnumerationOptions? options, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         if (null == options)
            options = DirectoryEnumerationOptions.None;


         if (searchOption == SearchOption.AllDirectories)
            options |= DirectoryEnumerationOptions.Recursive;


         if (null != onlyFolders)
         {
            // Adhere to the method name by validating the DirectoryEnumerationOptions value.
            // For example, method Directory.EnumerateDirectories() should only return folders
            // and method Directory.EnumerateFiles() should only return files.


            // Folders only.
            if ((bool) onlyFolders)
            {
               options &= ~DirectoryEnumerationOptions.Files;  // Remove enumeration of files.
               options |= DirectoryEnumerationOptions.Folders; // Add enumeration of folders.
            }

            // Files only.
            else
            {
               options &= ~DirectoryEnumerationOptions.Folders; // Remove enumeration of folders.
               options |= DirectoryEnumerationOptions.Files;    // Add enumeration of files.
            }
         }
         

         return new FindFileSystemEntryInfo(transaction, true, path, searchPattern, options, filters, pathFormat, typeof(T)).Enumerate<T>();
      }
   }
}
