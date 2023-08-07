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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Gets the change date and time of the specified file.</summary>
      /// <returns>A <see cref="DateTime"/> structure set to the change date and time for the specified file. This value is expressed in local time.</returns>
      /// <remarks><para>Use either <paramref name="path"/> or <paramref name="safeFileHandle"/>, not both.</para></remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="safeFileHandle">An open handle to the file or directory from which to retrieve information.</param>
      /// <param name="isFolder">Specifies that <paramref name="path"/> is a file or directory.</param>
      /// <param name="path">The file or directory for which to obtain creation date and time information.</param>
      /// <param name="getUtc"><c>true</c> gets the Coordinated Universal Time (UTC), <c>false</c> gets the local time.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposing is controlled.")]
      [SecurityCritical]
      internal static DateTime GetChangeTimeCore(KernelTransaction transaction, SafeFileHandle safeFileHandle, bool isFolder, string path, bool getUtc, PathFormat pathFormat)
      {
         if (!NativeMethods.IsAtLeastWindowsVista)
            throw new PlatformNotSupportedException(new Win32Exception((int) Win32Errors.ERROR_OLD_WIN_VERSION).Message);


         var callerHandle = null != safeFileHandle;
         if (!callerHandle)
         {
            if (pathFormat != PathFormat.LongFullPath && Utils.IsNullOrWhiteSpace(path))
               throw new ArgumentNullException("path");

            var pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.CheckInvalidPathChars);

            safeFileHandle = CreateFileCore(transaction, isFolder, pathLp, ExtendedFileAttributes.BackupSemantics, null, FileMode.Open, FileSystemRights.ReadData, FileShare.ReadWrite, true, false, PathFormat.LongFullPath);
         }


         try
         {
            NativeMethods.IsValidHandle(safeFileHandle);
            
            using (var safeBuffer = new SafeGlobalMemoryBufferHandle(IntPtr.Size + Marshal.SizeOf(typeof(NativeMethods.FILE_BASIC_INFO))))
            {
               NativeMethods.FILE_BASIC_INFO fbi;

               var success = NativeMethods.GetFileInformationByHandleEx_FileBasicInfo(safeFileHandle, NativeMethods.FILE_INFO_BY_HANDLE_CLASS.FILE_BASIC_INFO, out fbi, (uint) safeBuffer.Capacity);
               
               var lastError = Marshal.GetLastWin32Error();
               if (!success)
                  NativeError.ThrowException(lastError, !Utils.IsNullOrWhiteSpace(path) ? path : null);


               safeBuffer.StructureToPtr(fbi, true);
               var changeTime = safeBuffer.PtrToStructure<NativeMethods.FILE_BASIC_INFO>(0).ChangeTime;


               return getUtc ? DateTime.FromFileTimeUtc(changeTime) : DateTime.FromFileTime(changeTime);
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
