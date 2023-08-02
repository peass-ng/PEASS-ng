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
using System.Diagnostics.CodeAnalysis;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Flags that specify how a file or directoryis to be copied.</summary>
   [Flags]
   public enum CopyOptions
   {
      /// <summary>No CopyOptions used, this allows overwriting the file.</summary>
      None = 0,


      /// <summary>The copy operation fails immediately if the target file already exists.</summary>
      FailIfExists = NativeMethods.COPY_FILE_FLAGS.COPY_FILE_FAIL_IF_EXISTS,


      /// <summary>
      ///   Progress of the copy is tracked in the target file in case the copy fails. The failed copy can be restarted at a later time by specifying the same values
      ///   forexisting file name and new file name as those used in the call that failed. This can significantly slow down the copy operation as the new file may be
      ///   flushed multiple times during the copy operation.
      /// </summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Restartable")]
      Restartable = NativeMethods.COPY_FILE_FLAGS.COPY_FILE_RESTARTABLE,


      /// <summary>The file is copied and the original file is opened for write access.</summary>
      OpenSourceForWrite = NativeMethods.COPY_FILE_FLAGS.COPY_FILE_OPEN_SOURCE_FOR_WRITE,


      /// <summary>An attempt to copy an encrypted file will succeed even if the destination copy cannot be encrypted.</summary>
      AllowDecryptedDestination = NativeMethods.COPY_FILE_FLAGS.COPY_FILE_ALLOW_DECRYPTED_DESTINATION,


      /// <summary>
      ///   Similar to XCOPY /B parameter: Copies the Symbolic Link itself versus the target of the link.
      ///   If the source file is a symbolic link, the destination file is also a symbolic link pointing to the same file that the source symbolic link is pointing to.
      /// </summary>
      CopySymbolicLink = NativeMethods.COPY_FILE_FLAGS.COPY_FILE_COPY_SYMLINK,


      /// <summary>The copy operation is performed using unbuffered I/O, bypassing system I/O cache resources. Recommended for very large file transfers.</summary>
      NoBuffering = NativeMethods.COPY_FILE_FLAGS.COPY_FILE_NO_BUFFERING,


      /// <summary>The original source timestamp is preserved.</summary>
      CopyTimestamp = 8192
   }
}
