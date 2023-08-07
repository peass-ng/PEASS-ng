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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Retrieves the size of the specified file.</summary>
      /// <returns>The file size of the first or all streams, in bytes.</returns>
      /// <remarks>Use either <paramref name="path"/> or <paramref name="safeFileHandle"/>, not both.</remarks>
      /// <param name="safeFileHandle">The <see cref="SafeFileHandle"/> to the file.</param>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file.</param>
      /// <param name="sizeOfAllStreams"><c>true</c> to retrieve the size of all alternate data streams, <c>false</c> to get the size of the first stream.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      internal static long GetSizeCore(SafeFileHandle safeFileHandle, KernelTransaction transaction, string path, bool sizeOfAllStreams, PathFormat pathFormat)
      {
         var pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

         if (sizeOfAllStreams)
            return FindAllStreamsCore(transaction, pathLp);


         var callerHandle = null != safeFileHandle;

         if (!callerHandle)
            safeFileHandle = CreateFileCore(transaction, false, pathLp, ExtendedFileAttributes.Normal, null, FileMode.Open, FileSystemRights.ReadData, FileShare.ReadWrite, true, false, PathFormat.LongFullPath);
         
         long fileSize;

         try
         {
            var success = NativeMethods.GetFileSizeEx(safeFileHandle, out fileSize);

            var lastError = Marshal.GetLastWin32Error();

            if (!success && lastError != Win32Errors.ERROR_SUCCESS)
               NativeError.ThrowException(lastError, path);
         }
         finally
         {
            // Handle is ours, dispose.
            if (!callerHandle && null != safeFileHandle && !safeFileHandle.IsClosed)
               safeFileHandle.Close();
         }

         return fileSize;
      }
   }
}
