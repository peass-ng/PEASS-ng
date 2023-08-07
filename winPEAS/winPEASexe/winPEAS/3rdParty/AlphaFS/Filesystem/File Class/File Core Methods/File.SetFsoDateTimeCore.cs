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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Sets the date and time, in coordinated universal time (UTC), that the file or directory was created and/or last accessed and/or written to.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="isFolder">Specifies that <paramref name="path"/> is a file or directory.</param>
      /// <param name="path">The file or directory for which to set the date and time information.</param>
      /// <param name="creationTimeUtc">A <see cref="DateTime"/> containing the value to set for the creation date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
      /// <param name="lastAccessTimeUtc">A <see cref="DateTime"/> containing the value to set for the last access date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
      /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the file or directory linked to. No effect if <paramref name="path"/> does not refer to a reparse point.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void SetFsoDateTimeCore(KernelTransaction transaction, bool isFolder, string path, DateTime? creationTimeUtc, DateTime? lastAccessTimeUtc, DateTime? lastWriteTimeUtc, bool modifyReparsePoint, PathFormat pathFormat)
      {
         if (pathFormat == PathFormat.RelativePath)
            Path.CheckSupportedPathFormat(path, false, false);

         var eaAttributes = ExtendedFileAttributes.BackupSemantics;

         if (modifyReparsePoint)
            eaAttributes |= ExtendedFileAttributes.OpenReparsePoint;


         //// Prevent a System.UnauthorizedAccessException from being thrown by resetting attributes to Normal.

         //var fileAttributes = FileAttributes.Normal;
         //var isReadOnly = IsReadOnly((FileAttributes) eaAttributes);
         //var isHidden = IsHidden((FileAttributes) eaAttributes);

         //if (isReadOnly || isHidden)
         //   SetAttributesCore(transaction, isFolder, path, fileAttributes, PathFormat.LongFullPath);


         using (var creationTime = SafeGlobalMemoryBufferHandle.FromLong(creationTimeUtc.HasValue ? creationTimeUtc.Value.ToFileTimeUtc() : (long?) null))

         using (var lastAccessTime = SafeGlobalMemoryBufferHandle.FromLong(lastAccessTimeUtc.HasValue ? lastAccessTimeUtc.Value.ToFileTimeUtc() : (long?) null))

         using (var lastWriteTime = SafeGlobalMemoryBufferHandle.FromLong(lastWriteTimeUtc.HasValue ? lastWriteTimeUtc.Value.ToFileTimeUtc() : (long?) null))

         using (var safeFileHandle = CreateFileCore(transaction, isFolder, path, eaAttributes, null, FileMode.Open, FileSystemRights.WriteAttributes, FileShare.Delete | FileShare.Write, false, false, pathFormat))
         {
            var success = NativeMethods.SetFileTime(safeFileHandle, creationTime, lastAccessTime, lastWriteTime);

            var lastError = Marshal.GetLastWin32Error();
            
            // Reset file system object attributes.

            if (success)
            {
               //if (isReadOnly || isHidden)
               //{
               //   if (isReadOnly)
               //      fileAttributes |= FileAttributes.ReadOnly;

               //   if (isHidden)
               //      fileAttributes |= FileAttributes.Hidden;

               //   fileAttributes &= ~FileAttributes.Normal;

               //   SetAttributesCore(transaction, isFolder, path, fileAttributes, PathFormat.LongFullPath);
               //}
            }

            else
               NativeError.ThrowException(lastError, isFolder, path);
         }
      }
   }
}
