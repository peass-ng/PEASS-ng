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
using System.IO;
using System.Security.AccessControl;
using FileStream = System.IO.FileStream;
using System.Diagnostics.CodeAnalysis;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Opens a <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access, the specified sharing option and additional options specified.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
      /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
      /// <param name="attributes">Advanced <see cref="ExtendedFileAttributes"/> options for this file.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The default buffer size is 4096.</param>
      /// <param name="security">The security.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      ///   <para>A <see cref="FileStream"/> instance on the specified path, having the specified mode with</para>
      ///   <para>read, write, or read/write access and the specified sharing option.</para>
      /// </returns>
      internal static FileStream OpenCore(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, ExtendedFileAttributes attributes, int? bufferSize, FileSecurity security, PathFormat pathFormat)
      {
         var rights = access == FileAccess.Read ? FileSystemRights.Read : (access == FileAccess.Write ? FileSystemRights.Write : FileSystemRights.Read | FileSystemRights.Write);

         return OpenCore(transaction, path, mode, rights, share, attributes, bufferSize, security, pathFormat);
      }


      /// <summary>Opens a <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access, the specified sharing option and additional options specified.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
      /// <param name="rights">A <see cref="FileSystemRights"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten along with additional options.</param>
      /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
      /// <param name="attributes">Advanced <see cref="ExtendedFileAttributes"/> options for this file.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The default buffer size is 4096.</param>
      /// <param name="security">The security.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      ///   <para>A <see cref="FileStream"/> instance on the specified path, having the specified mode with</para>
      ///   <para>read, write, or read/write access and the specified sharing option.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      internal static FileStream OpenCore(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, ExtendedFileAttributes attributes, int? bufferSize, FileSecurity security, PathFormat pathFormat)
      {
         var access = ((rights & FileSystemRights.ReadData) != 0 ? FileAccess.Read : 0) |
                      ((rights & FileSystemRights.WriteData) != 0 || (rights & FileSystemRights.AppendData) != 0 ? FileAccess.Write : 0);


         SafeFileHandle safeHandle = null;

         try
         {
            safeHandle = CreateFileCore(transaction, false, path, attributes, security, mode, rights, share, true, false, pathFormat);

            return new FileStream(safeHandle, access, bufferSize ?? NativeMethods.DefaultFileBufferSize, (attributes & ExtendedFileAttributes.Overlapped) != 0);
         }
         catch
         {
            if (null != safeHandle && !safeHandle.IsClosed)
               safeHandle.Close();

            throw;
         }
      }
   }
}
