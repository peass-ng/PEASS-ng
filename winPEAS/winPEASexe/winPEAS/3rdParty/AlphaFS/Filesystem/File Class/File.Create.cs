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
      #region .NET

      /// <summary>Creates or overwrites a file in the specified path.</summary>
      /// <param name="path">The path and name of the file to create.</param>
      /// <returns>A <see cref="FileStream"/> that provides read/write access to the file specified in <paramref name="path"/>.</returns>
      [SecurityCritical]
      public static FileStream Create(string path)
      {
         return CreateFileStreamCore(null, path, ExtendedFileAttributes.Normal, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, NativeMethods.DefaultFileBufferSize, PathFormat.RelativePath);
      }


      /// <summary>Creates or overwrites the specified file.</summary>
      /// <param name="path">The name of the file.</param>
      /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
      /// <returns>A <see cref="FileStream"/> with the specified buffer size that provides read/write access to the file specified in <paramref name="path"/>.</returns>
      [SecurityCritical]
      public static FileStream Create(string path, int bufferSize)
      {
         return CreateFileStreamCore(null, path, ExtendedFileAttributes.Normal, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, PathFormat.RelativePath);
      }


      /// <summary>Creates or overwrites the specified file, specifying a buffer size and a <see cref="FileOptions"/> value that describes how to create or overwrite the file.</summary>
      /// <param name="path">The name of the file.</param>
      /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
      /// <param name="options">One of the <see cref="FileOptions"/> values that describes how to create or overwrite the file.</param>
      /// <returns>A new file with the specified buffer size.</returns>
      [SecurityCritical]
      public static FileStream Create(string path, int bufferSize, FileOptions options)
      {
         return CreateFileStreamCore(null, path, (ExtendedFileAttributes) options, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, PathFormat.RelativePath);
      }


      /// <summary>Creates or overwrites the specified file, specifying a buffer size and a <see cref="FileOptions"/> value that describes how to create or overwrite the file.</summary>
      /// <param name="path">The name of the file.</param>
      /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
      /// <param name="options">One of the <see cref="FileOptions"/> values that describes how to create or overwrite the file.</param>
      /// <param name="fileSecurity">One of the <see cref="FileSecurity"/> values that determines the access control and audit security for the file.</param>
      /// <returns>A new file with the specified buffer size, file options, and file security.</returns>
      [SecurityCritical]
      public static FileStream Create(string path, int bufferSize, FileOptions options, FileSecurity fileSecurity)
      {
         return CreateFileStreamCore(null, path, (ExtendedFileAttributes) options, fileSecurity, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, PathFormat.RelativePath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Creates or overwrites a file in the specified path.</summary>
      /// <param name="path">The path and name of the file to create.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>A <see cref="FileStream"/> that provides read/write access to the file specified in <paramref name="path"/>.</returns>
      [SecurityCritical]
      public static FileStream Create(string path, PathFormat pathFormat)
      {
         return CreateFileStreamCore(null, path, ExtendedFileAttributes.Normal, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, NativeMethods.DefaultFileBufferSize, pathFormat);
      }


      /// <summary>[AlphaFS] Creates or overwrites the specified file.</summary>
      /// <param name="path">The name of the file.</param>
      /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>A <see cref="FileStream"/> with the specified buffer size that provides read/write access to the file specified in <paramref name="path"/>.</returns>
      [SecurityCritical]
      public static FileStream Create(string path, int bufferSize, PathFormat pathFormat)
      {
         return CreateFileStreamCore(null, path, ExtendedFileAttributes.Normal, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, pathFormat);
      }


      /// <summary>[AlphaFS] Creates or overwrites the specified file, specifying a buffer size and a <see cref="FileOptions"/> value that describes how to create or overwrite the file.</summary>
      /// <param name="path">The name of the file.</param>
      /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
      /// <param name="options">One of the <see cref="FileOptions"/> values that describes how to create or overwrite the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>A new file with the specified buffer size.</returns>
      [SecurityCritical]
      public static FileStream Create(string path, int bufferSize, FileOptions options, PathFormat pathFormat)
      {
         return CreateFileStreamCore(null, path, (ExtendedFileAttributes) options, null, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, pathFormat);
      }


      /// <summary>[AlphaFS] Creates or overwrites the specified file, specifying a buffer size and a <see cref="FileOptions"/> value that describes how to create or overwrite the file.</summary>
      /// <param name="path">The name of the file.</param>
      /// <param name="bufferSize">The number of bytes buffered for reads and writes to the file.</param>
      /// <param name="options">One of the <see cref="FileOptions"/> values that describes how to create or overwrite the file.</param>
      /// <param name="fileSecurity">One of the <see cref="FileSecurity"/> values that determines the access control and audit security for the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>A new file with the specified buffer size, file options, and file security.</returns>
      [SecurityCritical]
      public static FileStream Create(string path, int bufferSize, FileOptions options, FileSecurity fileSecurity, PathFormat pathFormat)
      {
         return CreateFileStreamCore(null, path, (ExtendedFileAttributes) options, fileSecurity, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, pathFormat);
      }
   }
}
