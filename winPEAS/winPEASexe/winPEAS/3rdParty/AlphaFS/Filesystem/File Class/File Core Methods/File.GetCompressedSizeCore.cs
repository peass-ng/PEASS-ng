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
      /// <summary>Retrieves the actual number of bytes of disk storage used to store a
      ///   specified file as part of a transaction. If the file is located on a volume that supports compression and the file is compressed,
      ///   the value obtained is the compressed size of the specified file. If the file is located on a volume that supports sparse files and
      ///   the file is a sparse file, the value obtained is the sparse size of the specified file.
      /// </summary>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path"><para>The name of the file.</para></param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>The actual number of bytes of disk storage used to store the specified file.</returns>      
      [SecurityCritical]
      internal static long GetCompressedSizeCore(KernelTransaction transaction, string path, PathFormat pathFormat)
      {
         if (pathFormat != PathFormat.LongFullPath && Utils.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException("path");

         var pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

         uint fileSizeHigh;
         var fileSizeLow = null == transaction || !NativeMethods.IsAtLeastWindowsVista

            // GetCompressedFileSize() / GetCompressedFileSizeTransacted()
            // 2013-01-13: MSDN does not confirm LongPath usage but a Unicode version of this function exists.
            // 2017-05-30: GetCompressedFileSize() MSDN confirms LongPath usage: Starting with Windows 10, version 1607

            ? NativeMethods.GetCompressedFileSize(pathLp, out fileSizeHigh)

            : NativeMethods.GetCompressedFileSizeTransacted(pathLp, out fileSizeHigh, transaction.SafeHandle);

         var lastError = Marshal.GetLastWin32Error();


         // If the function fails, and lpFileSizeHigh is NULL, the return value is INVALID_FILE_SIZE.

         if (fileSizeLow == Win32Errors.ERROR_INVALID_FILE_SIZE && fileSizeHigh == 0)

            NativeError.ThrowException(lastError, pathLp);


         return NativeMethods.ToLong(fileSizeHigh, fileSizeLow);
      }
   }
}
