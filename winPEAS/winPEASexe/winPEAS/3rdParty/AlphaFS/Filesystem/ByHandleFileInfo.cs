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
using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Contains information that the GetFileInformationByHandle function retrieves.</summary>
   [Serializable]
   [SecurityCritical]
   public sealed class ByHandleFileInfo
   {
      internal ByHandleFileInfo(NativeMethods.BY_HANDLE_FILE_INFORMATION fibh)
      {
         CreationTimeUtc = DateTime.FromFileTimeUtc(fibh.ftCreationTime);
         LastAccessTimeUtc = DateTime.FromFileTimeUtc(fibh.ftLastAccessTime);
         LastWriteTimeUtc = DateTime.FromFileTimeUtc(fibh.ftLastWriteTime);

         Attributes = fibh.dwFileAttributes;
         FileIndex = NativeMethods.ToLong(fibh.nFileIndexHigh, fibh.nFileIndexLow);
         FileSize = NativeMethods.ToLong(fibh.nFileSizeHigh, fibh.nFileSizeLow);
         NumberOfLinks = (int) fibh.nNumberOfLinks;
         VolumeSerialNumber = fibh.dwVolumeSerialNumber;
      }


      /// <summary>Gets the file attributes.</summary>
      /// <value>The file attributes.</value>
      public FileAttributes Attributes { get; private set; }


      /// <summary>Gets the time this entry was created.</summary>
      /// <value>The time this entry was created.</value>
      public DateTime CreationTime
      {
         get { return CreationTimeUtc.ToLocalTime(); }
      }


      /// <summary>Gets the time, in coordinated universal time (UTC), this entry was created.</summary>
      /// <value>The time, in coordinated universal time (UTC), this entry was created.</value>
      public DateTime CreationTimeUtc { get; private set; }


      /// <summary>Gets the time this entry was last accessed.
      /// For a file, the structure specifies the last time that a file is read from or written to. 
      /// For a directory, the structure specifies when the directory is created. 
      /// For both files and directories, the specified date is correct, but the time of day is always set to midnight. 
      /// If the underlying file system does not support the last access time, this member is zero (0).
      /// </summary>
      /// <value>The time this entry was last accessed.</value>
      public DateTime LastAccessTime
      {
         get { return LastAccessTimeUtc.ToLocalTime(); }
      }


      /// <summary>Gets the time, in coordinated universal time (UTC), this entry was last accessed.
      /// For a file, the structure specifies the last time that a file is read from or written to. 
      /// For a directory, the structure specifies when the directory is created. 
      /// For both files and directories, the specified date is correct, but the time of day is always set to midnight. 
      /// If the underlying file system does not support the last access time, this member is zero (0).
      /// </summary>
      /// <value>The time, in coordinated universal time (UTC), this entry was last accessed.</value>
      public DateTime LastAccessTimeUtc { get; private set; }


      /// <summary>Gets the time this entry was last modified.
      /// For a file, the structure specifies the last time that a file is written to. 
      /// For a directory, the structure specifies when the directory is created. 
      /// If the underlying file system does not support the last access time, this member is zero (0).
      /// </summary>
      /// <value>The time this entry was last modified.</value>
      public DateTime LastWriteTime
      {
         get { return LastWriteTimeUtc.ToLocalTime(); }
      }


      /// <summary>Gets the time, in coordinated universal time (UTC), this entry was last modified.
      /// For a file, the structure specifies the last time that a file is written to. 
      /// For a directory, the structure specifies when the directory is created. 
      /// If the underlying file system does not support the last access time, this member is zero (0).
      /// </summary>
      /// <value>The time, in coordinated universal time (UTC), this entry was last modified.</value>
      public DateTime LastWriteTimeUtc { get; private set; }


      /// <summary>Gets the serial number of the volume that contains a file.</summary>
      /// <value>The serial number of the volume that contains a file.</value>
      public long VolumeSerialNumber { get; private set; }


      /// <summary>Gets the size of the file.</summary>
      /// <value>The size of the file.</value>
      public long FileSize { get; private set; }


      /// <summary>Gets the number of links to this file. For the FAT file system this member is always 1. For the NTFS file system, it can be more than 1.</summary>
      /// <value>The number of links to this file. </value>
      public int NumberOfLinks { get; private set; }


      /// <summary>
      /// Gets the unique identifier associated with the file. The identifier and the volume serial number uniquely identify a 
      /// file on a single computer. To determine whether two open handles represent the same file, combine the identifier 
      /// and the volume serial number for each file and compare them.
      /// </summary>
      /// <value>The unique identifier of the file.</value>
      public long FileIndex { get; private set; }
   }
}
