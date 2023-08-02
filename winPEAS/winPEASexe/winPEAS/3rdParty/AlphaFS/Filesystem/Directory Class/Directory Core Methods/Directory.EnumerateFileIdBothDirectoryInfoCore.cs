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

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>Returns an enumerable collection of information about files in the directory handle specified.</summary>
      /// <returns>An IEnumerable of <see cref="FileIdBothDirectoryInfo"/> records for each file system entry in the specified diretory.</returns>    
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <remarks>
      ///   <para>Either use <paramref name="path"/> or <paramref name="safeFileHandle"/>, not both.</para>
      ///   <para>
      ///   The number of files that are returned for each call to GetFileInformationByHandleEx depends on the size of the buffer that is passed to the function.
      ///   Any subsequent calls to GetFileInformationByHandleEx on the same handle will resume the enumeration operation after the last file is returned.
      /// </para>
      /// </remarks>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="safeFileHandle">An open handle to the directory from which to retrieve information.</param>
      /// <param name="path">A path to the directory.</param>
      /// <param name="shareMode">The <see cref="FileShare"/> mode with which to open a handle to the directory.</param>
      /// <param name="continueOnException"><c>true</c> suppress any Exception that might be thrown as a result from a failure, such as ACLs protected directories or non-accessible reparse points.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static IEnumerable<FileIdBothDirectoryInfo> EnumerateFileIdBothDirectoryInfoCore(KernelTransaction transaction, SafeFileHandle safeFileHandle, string path, FileShare shareMode, bool continueOnException, PathFormat pathFormat)
      {
         if (!NativeMethods.IsAtLeastWindowsVista)
            throw new PlatformNotSupportedException(new Win32Exception((int) Win32Errors.ERROR_OLD_WIN_VERSION).Message);


         var pathLp = path;

         var callerHandle = null != safeFileHandle;
         if (!callerHandle)
         {
            if (Utils.IsNullOrWhiteSpace(path))
               throw new ArgumentNullException("path");

            pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

            safeFileHandle = File.CreateFileCore(transaction, true, pathLp, ExtendedFileAttributes.BackupSemantics, null, FileMode.Open, FileSystemRights.ReadData, shareMode, true, false, PathFormat.LongFullPath);
         }


         try
         {
            if (!NativeMethods.IsValidHandle(safeFileHandle, Marshal.GetLastWin32Error(), !continueOnException))
               yield break;

            var fileNameOffset = (int) Marshal.OffsetOf(typeof(NativeMethods.FILE_ID_BOTH_DIR_INFO), "FileName");

            using (var safeBuffer = new SafeGlobalMemoryBufferHandle(NativeMethods.DefaultFileBufferSize))
            {
               while (true)
               {
                  var success = NativeMethods.GetFileInformationByHandleEx(safeFileHandle, NativeMethods.FILE_INFO_BY_HANDLE_CLASS.FILE_ID_BOTH_DIR_INFO, safeBuffer, (uint) safeBuffer.Capacity);

                  var lastError = Marshal.GetLastWin32Error();
                  if (!success)
                  {
                     switch ((uint) lastError)
                     {
                        case Win32Errors.ERROR_SUCCESS:
                        case Win32Errors.ERROR_NO_MORE_FILES:
                        case Win32Errors.ERROR_HANDLE_EOF:
                           yield break;

                        case Win32Errors.ERROR_MORE_DATA:
                           continue;

                        default:
                           NativeError.ThrowException(lastError, pathLp);

                           // Keep the compiler happy as we never get here.
                           yield break;
                     }
                  }
                  

                  var offset = 0;
                  NativeMethods.FILE_ID_BOTH_DIR_INFO fibdi;

                  do
                  {
                     fibdi = safeBuffer.PtrToStructure<NativeMethods.FILE_ID_BOTH_DIR_INFO>(offset);

                     var fileName = safeBuffer.PtrToStringUni(offset + fileNameOffset, (int) (fibdi.FileNameLength / UnicodeEncoding.CharSize));

                     offset += fibdi.NextEntryOffset;


                     if (File.IsDirectory(fibdi.FileAttributes) &&
                         (fileName.Equals(Path.CurrentDirectoryPrefix, StringComparison.Ordinal) ||
                          fileName.Equals(Path.ParentDirectoryPrefix, StringComparison.Ordinal)))
                        continue;


                     yield return new FileIdBothDirectoryInfo(fibdi, fileName);

                  } while (fibdi.NextEntryOffset != 0);
               }                           
            }
         }
         finally
         {
            // Handle is ours, dispose.
            if (!callerHandle && null != safeFileHandle && !safeFileHandle.IsClosed)
               safeFileHandle.Close();
         }
      }
   }
}
