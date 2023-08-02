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

using System.IO;
using System.Security;
using System.Security.AccessControl;
using FileStream = System.IO.FileStream;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path with read/write access.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">
      ///   A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents
      ///   of existing files are retained or overwritten.
      /// </param>
      /// <returns>A <see cref="FileStream"/> opened in the specified mode and path, with read/write access and not shared.</returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode)
      {
         return OpenCore(transaction, path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None, ExtendedFileAttributes.Normal, null, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path with read/write access.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">
      ///   A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents
      ///   of existing files are retained or overwritten.
      /// </param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>A <see cref="FileStream"/> opened in the specified mode and path, with read/write access and not shared.</returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None, ExtendedFileAttributes.Normal, null, null, pathFormat);
      }


      #region Using FileAccess

      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path, with the specified mode and access.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">
      ///   A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents
      ///   of existing files are retained or overwritten.
      /// </param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
      /// <returns>
      ///   An unshared <see cref="FileStream"/> that provides access to the specified file, with the specified mode and access.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access)
      {
         return OpenCore(transaction, path, mode, access, FileShare.None, ExtendedFileAttributes.Normal, null, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">
      ///   A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents
      ///   of existing files are retained or overwritten.
      /// </param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
      /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the
      ///   specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share)
      {
         return OpenCore(transaction, path, mode, access, share, ExtendedFileAttributes.Normal, null, null, PathFormat.RelativePath);
      }
      

      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path, with the specified mode and access.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">
      ///   A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents
      ///   of existing files are retained or overwritten.
      /// </param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      ///   An unshared <see cref="FileStream"/> that provides access to the specified file, with the specified mode and access.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, access, FileShare.None, ExtendedFileAttributes.Normal, null, null, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
      /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, access, share, ExtendedFileAttributes.Normal, null, null, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">
      ///   A <see cref="FileMode"/> value that specifies whether a file is created if one does not exist, and determines whether the contents
      ///   of existing files are retained or overwritten.
      /// </param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file.</param>
      /// <param name="share">A <see cref="FileShare"/> value specifying the type of access other threads have to the file.</param>
      /// <param name="extendedAttributes">The extended attributes.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write access and the
      ///   specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, ExtendedFileAttributes extendedAttributes, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, access, share, extendedAttributes, null, null, pathFormat);
      }

      
      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
      {
         return OpenCore(transaction, path, mode, access, share, ExtendedFileAttributes.Normal, bufferSize, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="useAsync">Specifies whether to use asynchronous I/O or synchronous I/O. However, note that the
      /// underlying operating system might not support asynchronous I/O, so when specifying true, the handle might be
      /// opened synchronously depending on the platform. When opened asynchronously, the BeginRead and BeginWrite methods
      /// perform better on large reads or writes, but they might be much slower for small reads or writes. If the
      /// application is designed to take advantage of asynchronous I/O, set the useAsync parameter to true. Using
      /// asynchronous I/O correctly can speed up applications by as much as a factor of 10, but using it without
      /// redesigning the application for asynchronous I/O can decrease performance by as much as a factor of 10.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
      {
         return OpenCore(transaction, path, mode, access, share, ExtendedFileAttributes.Normal | (useAsync ? ExtendedFileAttributes.Overlapped : ExtendedFileAttributes.Normal), bufferSize, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="options">A value that specifies additional file options.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
      {
         return OpenCore(transaction, path, mode, access, share, (ExtendedFileAttributes) options, bufferSize, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="extendedAttributes">The extended attributes specifying additional options.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, ExtendedFileAttributes extendedAttributes)
      {
         return OpenCore(transaction, path, mode, access, share, extendedAttributes, bufferSize, null, PathFormat.RelativePath);
      }
      

      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, access, share, ExtendedFileAttributes.Normal, bufferSize, null, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="useAsync">Specifies whether to use asynchronous I/O or synchronous I/O. However, note that the
      /// underlying operating system might not support asynchronous I/O, so when specifying true, the handle might be
      /// opened synchronously depending on the platform. When opened asynchronously, the BeginRead and BeginWrite methods
      /// perform better on large reads or writes, but they might be much slower for small reads or writes. If the
      /// application is designed to take advantage of asynchronous I/O, set the useAsync parameter to true. Using
      /// asynchronous I/O correctly can speed up applications by as much as a factor of 10, but using it without
      /// redesigning the application for asynchronous I/O can decrease performance by as much as a factor of 10.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, access, share, ExtendedFileAttributes.Normal | (useAsync ? ExtendedFileAttributes.Overlapped : ExtendedFileAttributes.Normal), bufferSize, null, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="options">A value that specifies additional file options.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, access, share, (ExtendedFileAttributes) options, bufferSize, null, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="extendedAttributes">The extended attributes specifying additional options.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, ExtendedFileAttributes extendedAttributes, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, access, share, extendedAttributes, bufferSize, null, pathFormat);
      }

      #endregion // Using FileAccess


      #region Using FileSystemRights

      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="options">A value that specifies additional file options.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options)
      {
         return OpenCore(transaction, path, mode, rights, share, (ExtendedFileAttributes) options, bufferSize, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="extendedAttributes">Extended attributes specifying additional options.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, ExtendedFileAttributes extendedAttributes)
      {
         return OpenCore(transaction, path, mode, rights, share, extendedAttributes, bufferSize, null, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="options">A value that specifies additional file options.</param>
      /// <param name="security">A value that determines the access control and audit security for the file.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity security)
      {
         return OpenCore(transaction, path, mode, rights, share, (ExtendedFileAttributes) options, bufferSize, security, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="extendedAttributes">Extended attributes specifying additional options.</param>
      /// <param name="security">A value that determines the access control and audit security for the file.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, ExtendedFileAttributes extendedAttributes, FileSecurity security)
      {
         return OpenCore(transaction, path, mode, rights, share, extendedAttributes, bufferSize, security, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="options">A value that specifies additional file options.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, rights, share, (ExtendedFileAttributes) options, bufferSize, null, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified creation mode, read/write and sharing permission, and buffer size.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="extendedAttributes">Extended attributes specifying additional options.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, ExtendedFileAttributes extendedAttributes, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, rights, share, extendedAttributes, bufferSize, null, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified  creation mode, access rights and sharing permission, the buffer size, additional file options, access control and audit security.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="options">A value that specifies additional file options.</param>
      /// <param name="security">A value that determines the access control and audit security for the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity security, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, rights, share, (ExtendedFileAttributes) options, bufferSize, security, pathFormat);
      }


      /// <summary>[AlphaFS] (Transacted) Opens a <see cref="FileStream"/> on the specified path using the specified  creation mode, access rights and sharing permission, the buffer size, additional file options, access control and audit security.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to open.</param>
      /// <param name="mode">A constant that determines how to open or create the file.</param>
      /// <param name="rights">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the
      /// file.</param>
      /// <param name="share">A constant that determines how the file will be shared by processes.</param>
      /// <param name="bufferSize">A positive <see cref="System.Int32"/> value greater than 0 indicating the buffer size. The
      /// default buffer size is 4096.</param>
      /// <param name="extendedAttributes">Extended attributes specifying additional options.</param>
      /// <param name="security">A value that determines the access control and audit security for the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter.</param>
      /// <returns>
      ///   A <see cref="FileStream"/> on the specified path, having the specified mode with read, write, or read/write
      ///   access and the specified sharing option.
      /// </returns>
      [SecurityCritical]
      public static FileStream OpenTransacted(KernelTransaction transaction, string path, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, ExtendedFileAttributes extendedAttributes, FileSecurity security, PathFormat pathFormat)
      {
         return OpenCore(transaction, path, mode, rights, share, extendedAttributes, bufferSize, security, pathFormat);
      }

      #endregion // Using FileSystemRights
   }
}
