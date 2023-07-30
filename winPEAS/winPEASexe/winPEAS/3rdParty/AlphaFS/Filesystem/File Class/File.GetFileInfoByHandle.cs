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
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] Retrieves file information for the specified file.</summary>
      /// <returns>A <see cref="ByHandleFileInfo"/> object containing the requested information.</returns>
      /// <remarks>File IDs are not guaranteed to be unique over time, because file systems are free to reuse them. In some cases, the file ID for a file can change over time.</remarks>
      /// <param name="path">The path to the file.</param>
      [SecurityCritical]
      public static ByHandleFileInfo GetFileInfoByHandle(string path)
      {
         return GetFileInfoByHandleCore(null, false, path, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves file information for the specified file.</summary>
      /// <returns>A <see cref="ByHandleFileInfo"/> object containing the requested information.</returns>
      /// <remarks>File IDs are not guaranteed to be unique over time, because file systems are free to reuse them. In some cases, the file ID for a file can change over time.</remarks>
      /// <param name="path">The path to the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static ByHandleFileInfo GetFileInfoByHandle(string path, PathFormat pathFormat)
      {
         return GetFileInfoByHandleCore(null, false, path, pathFormat);
      }
      
      
      /// <summary>[AlphaFS] Retrieves file information for the specified <see cref="SafeFileHandle"/>.</summary>
      /// <returns>A <see cref="ByHandleFileInfo"/> object containing the requested information.</returns>
      /// <returns>A <see cref="ByHandleFileInfo"/> object containing the requested information.</returns>
      /// <param name="handle">A <see cref="SafeFileHandle"/> connected to the open file or directory from which to retrieve the information.</param>
      [SecurityCritical]
      public static ByHandleFileInfo GetFileInfoByHandle(SafeFileHandle handle)
      {
         NativeMethods.IsValidHandle(handle);

         NativeMethods.BY_HANDLE_FILE_INFORMATION info;

         var success = NativeMethods.GetFileInformationByHandle(handle, out info);

         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            NativeError.ThrowException(lastError);


         return new ByHandleFileInfo(info);
      }
   }
}
