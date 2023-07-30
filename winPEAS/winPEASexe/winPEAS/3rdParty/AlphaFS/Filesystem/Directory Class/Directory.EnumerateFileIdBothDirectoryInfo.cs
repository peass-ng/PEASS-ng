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
using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Retrieves information about files in the directory specified by <paramref name="path"/> in <see cref="FileShare.ReadWrite"/> mode.</summary>
      /// <returns>An enumeration of <see cref="FileIdBothDirectoryInfo"/> records for each file system entry in the specified diretory.</returns>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="path">A path to a directory from which to retrieve information.</param>
      [SecurityCritical]
      public static IEnumerable<FileIdBothDirectoryInfo> EnumerateFileIdBothDirectoryInfo(string path)
      {
         return EnumerateFileIdBothDirectoryInfoCore(null, null, path, FileShare.ReadWrite, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves information about files in the directory specified by <paramref name="path"/> in <see cref="FileShare.ReadWrite"/> mode.</summary>
      /// <returns>An enumeration of <see cref="FileIdBothDirectoryInfo"/> records for each file system entry in the specified diretory.</returns>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="path">A path to a directory from which to retrieve information.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static IEnumerable<FileIdBothDirectoryInfo> EnumerateFileIdBothDirectoryInfo(string path, PathFormat pathFormat)
      {
         return EnumerateFileIdBothDirectoryInfoCore(null, null, path, FileShare.ReadWrite, false, pathFormat);
      }


      /// <summary>[AlphaFS] Retrieves information about files in the directory specified by <paramref name="path"/> in specified <see cref="FileShare"/> mode.</summary>
      /// <returns>An enumeration of <see cref="FileIdBothDirectoryInfo"/> records for each file system entry in the specified diretory.</returns>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="path">A path to a directory from which to retrieve information.</param>
      /// <param name="shareMode">The <see cref="FileShare"/> mode with which to open a handle to the directory.</param>
      [SecurityCritical]
      public static IEnumerable<FileIdBothDirectoryInfo> EnumerateFileIdBothDirectoryInfo(string path, FileShare shareMode)
      {
         return EnumerateFileIdBothDirectoryInfoCore(null, null, path, shareMode, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves information about files in the directory specified by <paramref name="path"/> in specified <see cref="FileShare"/> mode.</summary>
      /// <returns>An enumeration of <see cref="FileIdBothDirectoryInfo"/> records for each file system entry in the specified diretory.</returns>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="path">A path to a directory from which to retrieve information.</param>
      /// <param name="shareMode">The <see cref="FileShare"/> mode with which to open a handle to the directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static IEnumerable<FileIdBothDirectoryInfo> EnumerateFileIdBothDirectoryInfo(string path, FileShare shareMode, PathFormat pathFormat)
      {
         return EnumerateFileIdBothDirectoryInfoCore(null, null, path, shareMode, false, pathFormat);
      }


      /// <summary>[AlphaFS] Retrieves information about files in the directory handle specified.</summary>
      /// <returns>An IEnumerable of <see cref="FileIdBothDirectoryInfo"/> records for each file system entry in the specified diretory.</returns>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="handle">An open handle to the directory from which to retrieve information.</param>
      [SecurityCritical]
      public static IEnumerable<FileIdBothDirectoryInfo> EnumerateFileIdBothDirectoryInfo(SafeFileHandle handle)
      {
         // FileShare has no effect since a handle is already opened.
         return EnumerateFileIdBothDirectoryInfoCore(null, handle, null, FileShare.ReadWrite, false, PathFormat.RelativePath);
      }
   }
}
