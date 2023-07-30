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
using System.Security;
using System.Security.AccessControl;
using FileStream = System.IO.FileStream;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Creates or overwrites a file in the specified path.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The name of the file.</param>
      /// <param name="attributes">The <see cref="ExtendedFileAttributes"/> additional advanced options to create a file.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> instance that determines the access control and audit security for the file.</param>
      /// <param name="mode">The <see cref="FileMode"/> option gives you more precise control over how you want to create a file.</param>
      /// <param name="access">The <see cref="FileAccess"/> allow you additionally specify to default read/write capability - just write, bypassing any cache.</param>
      /// <param name="share">The <see cref="FileShare"/> option controls how you would like to share created file with other requesters.</param>
      ///  <param name="pathFormat">Indicates the format of the <paramref name="path"/> parameter.</param>
      /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
      /// <returns>A <see cref="FileStream"/> that provides read/write access to the file specified in path.</returns>      
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive")]
      [SecurityCritical]
      internal static FileStream CreateFileStreamCore(KernelTransaction transaction, string path, ExtendedFileAttributes attributes, FileSecurity fileSecurity, FileMode mode, FileAccess access, FileShare share, int bufferSize, PathFormat pathFormat)
      {
         SafeFileHandle safeHandle = null;

         try
         {
            safeHandle = CreateFileCore(transaction, false, path, attributes, fileSecurity, mode, (FileSystemRights) access, share, true, false, pathFormat);

            return new FileStream(safeHandle, access, bufferSize, (attributes & ExtendedFileAttributes.Overlapped) != 0);
         }
         catch
         {
            NativeMethods.IsValidHandle(safeHandle, false);

            throw;
         }
      }
   }
}
