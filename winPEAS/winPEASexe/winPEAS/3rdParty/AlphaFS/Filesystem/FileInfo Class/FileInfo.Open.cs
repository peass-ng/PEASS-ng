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

namespace Alphaleonis.Win32.Filesystem
{
   partial class FileInfo
   {
      #region .NET

      /// <summary>Opens a file in the specified mode.</summary>
      /// <returns>A <see cref="FileStream"/> file opened in the specified mode, with read/write access and unshared.</returns>
      /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
      [SecurityCritical]
      public FileStream Open(FileMode mode)
      {
         return File.OpenCore(Transaction, LongFullName, mode, FileAccess.Read, FileShare.None, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath);
      }


      /// <summary>Opens a file in the specified mode with read, write, or read/write access.</summary>
      /// <returns>A <see cref="FileStream"/> object opened in the specified mode and access, and unshared.</returns>
      /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with Read, Write, or ReadWrite file access.</param>
      [SecurityCritical]
      public FileStream Open(FileMode mode, FileAccess access)
      {
         return File.OpenCore(Transaction, LongFullName, mode, access, FileShare.None, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath);
      }


      /// <summary>Opens a file in the specified mode with read, write, or read/write access and the specified sharing option.</summary>
      /// <returns>A <see cref="FileStream"/> object opened with the specified mode, access, and sharing options.</returns>
      /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
      /// <param name="access">A <see cref="FileAccess"/> constant specifying whether to open the file with Read, Write, or ReadWrite file access.</param>
      /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other <see cref="FileStream"/> objects have to this file.</param>
      [SecurityCritical]
      public FileStream Open(FileMode mode, FileAccess access, FileShare share)
      {
         return File.OpenCore(Transaction, LongFullName, mode, access, share, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Opens a file in the specified mode with read, write, or read/write access.</summary>
      /// <returns>A <see cref="FileStream"/> object opened in the specified mode and access, and unshared.</returns>
      /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
      /// <param name="rights">A <see cref="FileSystemRights"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten along with additional options.</param>
      [SecurityCritical]
      public FileStream Open(FileMode mode, FileSystemRights rights)
      {
         return File.OpenCore(Transaction, LongFullName, mode, rights, FileShare.None, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Opens a file in the specified mode with read, write, or read/write access and the specified sharing option.</summary>
      /// <returns>A <see cref="FileStream"/> object opened with the specified mode, access, and sharing options.</returns>
      /// <param name="mode">A <see cref="FileMode"/> constant specifying the mode (for example, Open or Append) in which to open the file.</param>
      /// <param name="rights">A <see cref="FileSystemRights"/> value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten along with additional options.</param>
      /// <param name="share">A <see cref="FileShare"/> constant specifying the type of access other <see cref="FileStream"/> objects have to this file.</param>
      [SecurityCritical]
      public FileStream Open(FileMode mode, FileSystemRights rights, FileShare share)
      {
         return File.OpenCore(Transaction, LongFullName, mode, rights, share, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath);
      }
   }
}
