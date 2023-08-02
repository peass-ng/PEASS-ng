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

using Alphaleonis.Win32.Security;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Creates or opens a file, directory or I/O device.</summary>
      /// <returns>A <see cref="SafeFileHandle"/> that provides read/write access to the file or directory specified by <paramref name="path"/>.</returns>
      /// <remarks>
      ///   <para>To obtain a directory handle using CreateFile, specify the FILE_FLAG_BACKUP_SEMANTICS flag as part of dwFlagsAndAttributes.</para>
      ///   <para>The most commonly used I/O devices are as follows: file, file stream, directory, physical disk, volume, console buffer, tape drive, communications resource, mailslot, and pipe.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="Exception"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="isFolder">When <c>true</c> indicates the source is a directory, <c>false</c> indicates a file and <c>null</c> specifies a physical device.</param>
      /// <param name="path">The path and name of the file or directory to create.</param>
      /// <param name="attributes">One of the <see cref="ExtendedFileAttributes"/> values that describes how to create or overwrite the file or directory.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> instance that determines the access control and audit security for the file or directory.</param>
      /// <param name="fileMode">A <see cref="FileMode"/> constant that determines how to open or create the file or directory.</param>
      /// <param name="fileSystemRights">A <see cref="FileSystemRights"/> constant that determines the access rights to use when creating access and audit rules for the file or directory.</param>
      /// <param name="fileShare">A <see cref="FileShare"/> constant that determines how the file or directory will be shared by processes.</param>
      /// <param name="checkPath"></param>
      /// <param name="continueOnException"><c>true</c> suppress any Exception that might be thrown as a result from a failure, such as ACLs protected directories or non-accessible reparse points.</param>
      /// <param name="pathFormat">Indicates the format of the <paramref name="path"/> parameter.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Object needs to be disposed by caller.")]
      [SecurityCritical]
      internal static SafeFileHandle CreateFileCore(KernelTransaction transaction, bool? isFolder, string path, ExtendedFileAttributes attributes, FileSecurity fileSecurity, FileMode fileMode, FileSystemRights fileSystemRights, FileShare fileShare, bool checkPath, bool continueOnException, PathFormat pathFormat)
      {
         if (checkPath && pathFormat == PathFormat.RelativePath)

            Path.CheckSupportedPathFormat(path, true, true);


         // When isFile == null, we're working with a device.
         // When opening a VOLUME or removable media drive (for example, a floppy disk drive or flash memory thumb drive),
         // the path string should be the following form: "\\.\X:"
         // Do not use a trailing backslash ('\'), which indicates the root.

         var pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.TrimEnd | GetFullPathOptions.RemoveTrailingDirectorySeparator);


         // CreateFileXxx() does not support FileMode.Append mode.
         var isAppend = fileMode == FileMode.Append;
         if (isAppend)
         {
            fileMode = FileMode.OpenOrCreate;
            fileSystemRights |= FileSystemRights.AppendData;
         }


         if (null != fileSecurity)
            fileSystemRights |= (FileSystemRights) SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION;


         using ((fileSystemRights & (FileSystemRights)SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION) != 0 || (fileSystemRights & (FileSystemRights)SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION) != 0 ? new PrivilegeEnabler(Privilege.Security) : null)

         using (var securityAttributes = new Security.NativeMethods.SecurityAttributes(fileSecurity))
         {
            var safeHandle = transaction == null || !NativeMethods.IsAtLeastWindowsVista

               // CreateFile() / CreateFileTransacted()
               // 2013-01-13: MSDN confirms LongPath usage.

               ? NativeMethods.CreateFile(pathLp, fileSystemRights, fileShare, securityAttributes, fileMode, attributes, IntPtr.Zero)

               : NativeMethods.CreateFileTransacted(pathLp, fileSystemRights, fileShare, securityAttributes, fileMode, attributes, IntPtr.Zero, transaction.SafeHandle, IntPtr.Zero, IntPtr.Zero);


            var lastError = Marshal.GetLastWin32Error();

            NativeMethods.CloseHandleAndPossiblyThrowException(safeHandle, lastError, isFolder, path, !continueOnException);


            if (isAppend)
            {
               var success = NativeMethods.SetFilePointerEx(safeHandle, 0, IntPtr.Zero, SeekOrigin.End);

               lastError = Marshal.GetLastWin32Error();

               if (!success)
               {
                  NativeMethods.CloseHandleAndPossiblyThrowException(safeHandle, lastError, isFolder, path, !continueOnException);

                  return null;
               }
            }

            return safeHandle;
         }
      }
   }
}
