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
using System.Globalization;
using System.IO;
using System.Security;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Delete empty subdirectories from the specified directory.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <exception cref="DirectoryReadOnlyException"/>
      /// <param name="fsEntryInfo">A FileSystemEntryInfo instance. Use either <paramref name="fsEntryInfo"/> or <paramref name="path"/>, not both.</param>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The name of the directory to remove empty subdirectories from. Use either <paramref name="path"/> or <paramref name="fsEntryInfo"/>, not both.</param>
      /// <param name="recursive"><c>true</c> deletes empty subdirectories from this directory and its subdirectories.</param>
      /// <param name="ignoreReadOnly"><c>true</c> overrides read only <see cref="FileAttributes"/> of empty directories.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void DeleteEmptySubdirectoriesCore(FileSystemEntryInfo fsEntryInfo, KernelTransaction transaction, string path, bool recursive, bool ignoreReadOnly, PathFormat pathFormat)
      {
         #region Setup

         if (null == fsEntryInfo)
         {
            if (pathFormat == PathFormat.RelativePath)
               Path.CheckSupportedPathFormat(path, true, true);

            if (!File.ExistsCore(transaction, true, path, pathFormat))
               NativeError.ThrowException(Win32Errors.ERROR_PATH_NOT_FOUND, path);

            fsEntryInfo = File.GetFileSystemEntryInfoCore(transaction, true, Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.TrimEnd | GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck), false, pathFormat);

            if (null == fsEntryInfo)
               return;
         }

         #endregion // Setup


         // Ensure path is a directory.
         if (!fsEntryInfo.IsDirectory)
            throw new IOException(string.Format(CultureInfo.InvariantCulture, Resources.Target_Directory_Is_A_File, fsEntryInfo.LongFullPath));


         var dirs = new Stack<string>(1000);
         dirs.Push(fsEntryInfo.LongFullPath);

         while (dirs.Count > 0)
         {
            foreach (var fsei in EnumerateFileSystemEntryInfosCore<FileSystemEntryInfo>(true, transaction, dirs.Pop(), Path.WildcardStarMatchAll, null, DirectoryEnumerationOptions.ContinueOnException, null, PathFormat.LongFullPath))
            {
               // Ensure the directory is empty.
               if (IsEmptyCore(transaction, fsei.LongFullPath, pathFormat))
                  DeleteDirectoryCore(transaction, fsei, null, false, ignoreReadOnly, true, PathFormat.LongFullPath);

               else if (recursive)
                  dirs.Push(fsei.LongFullPath);
            }
         }
      }
   }
}
