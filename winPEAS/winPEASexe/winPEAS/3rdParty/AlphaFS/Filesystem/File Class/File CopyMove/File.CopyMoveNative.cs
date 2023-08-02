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
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      // Symbolic Link Effects on File Systems Functions: https://msdn.microsoft.com/en-us/library/windows/desktop/aa365682(v=vs.85).aspx


      // MSDN: If lpProgressRoutine returns PROGRESS_CANCEL due to the user canceling the operation,
      // CopyFileEx will return zero and GetLastError will return ERROR_REQUEST_ABORTED.
      // In this case, the partially copied destination file is deleted.
      //
      // If lpProgressRoutine returns PROGRESS_STOP due to the user stopping the operation,
      // CopyFileEx will return zero and GetLastError will return ERROR_REQUEST_ABORTED.
      // In this case, the partially copied destination file is left intact.


      // Note: MoveFileXxx fails if one of the paths is a UNC path, even though both paths refer to the same volume.
      // For example, src = C:\TempSrc and dst = \\localhost\C$\TempDst

      // MoveFileXxx fails if it cannot access the registry. The function stores the locations of the files to be renamed at restart in the following registry value:
      //
      //    HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\PendingFileRenameOperations
      //
      // This registry value is of type REG_MULTI_SZ. Each rename operation stores one of the following NULL-terminated strings, depending on whether the rename is a delete or not:
      //
      //    szDstFile\0\0              : indicates that the file szDstFile is to be deleted on reboot.
      //    szSrcFile\0szDstFile\0     : indicates that szSrcFile is to be renamed szDstFile on reboot.


      [SecurityCritical]
      private static bool CopyMoveNative(CopyMoveArguments cma, bool isMove, string sourcePathLp, string destinationPathLp, out bool cancel, out int lastError)
      {
         cancel = false;

         var success = null == cma.Transaction || !NativeMethods.IsAtLeastWindowsVista

            // CopyFileEx() / CopyFileTransacted() / MoveFileWithProgress() / MoveFileTransacted()
            // 2013-04-15: MSDN confirms LongPath usage.


            ? isMove
               ? NativeMethods.MoveFileWithProgress(sourcePathLp, destinationPathLp, cma.Routine, IntPtr.Zero, (MoveOptions) cma.MoveOptions)

               : NativeMethods.CopyFileEx(sourcePathLp, destinationPathLp, cma.Routine, IntPtr.Zero, out cancel, (CopyOptions) cma.CopyOptions)

            : isMove
               ? NativeMethods.MoveFileTransacted(sourcePathLp, destinationPathLp, cma.Routine, IntPtr.Zero, (MoveOptions) cma.MoveOptions, cma.Transaction.SafeHandle)

               : NativeMethods.CopyFileTransacted(sourcePathLp, destinationPathLp, cma.Routine, IntPtr.Zero, out cancel, (CopyOptions) cma.CopyOptions, cma.Transaction.SafeHandle);


         lastError = Marshal.GetLastWin32Error();


         return success;
      }
   }
}
