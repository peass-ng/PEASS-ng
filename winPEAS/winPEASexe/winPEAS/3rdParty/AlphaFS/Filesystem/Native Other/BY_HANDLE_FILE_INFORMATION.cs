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
using System.Runtime.InteropServices;

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>Contains information that the GetFileInformationByHandle function retrieves.</summary>
      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      internal struct BY_HANDLE_FILE_INFORMATION
      {
         /// <summary>The file attributes.</summary>
         public readonly FileAttributes dwFileAttributes;


         /// <summary>A <see cref="FILETIME"/> structure that specifies when a file or directory is created.</summary>
         public readonly FILETIME ftCreationTime;


         /// <summary>A <see cref="FILETIME"/> structure. For a file, the structure specifies the last time that a file is read from or written to.
         /// For a directory, the structure specifies when the directory is created.
         /// For both files and directories, the specified date is correct, but the time of day is always set to midnight.
         /// </summary>
         public readonly FILETIME ftLastAccessTime;


         /// <summary>A <see cref="FILETIME"/> structure. For a file, the structure specifies the last time that a file is written to.
         /// For a directory, the structure specifies when the directory is created.</summary>
         public readonly FILETIME ftLastWriteTime;


         /// <summary>The serial number of the volume that contains a file.</summary>
         [MarshalAs(UnmanagedType.U4)] public readonly uint dwVolumeSerialNumber;


         /// <summary>The high-order part of the file size.</summary>
         [MarshalAs(UnmanagedType.U4)] public readonly uint nFileSizeHigh;


         /// <summary>The low-order part of the file size.</summary>
         [MarshalAs(UnmanagedType.U4)] public readonly uint nFileSizeLow;

         /// <summary>The number of links to this file. For the FAT file system this member is always 1. For the NTFS file system, it can be more than 1.</summary>
         [MarshalAs(UnmanagedType.U4)] public readonly uint nNumberOfLinks;

         /// <summary>The high-order part of a unique identifier that is associated with a file.</summary>
         [MarshalAs(UnmanagedType.U4)] public readonly uint nFileIndexHigh;

         /// <summary>The low-order part of a unique identifier that is associated with a file.</summary>
         [MarshalAs(UnmanagedType.U4)] public readonly uint nFileIndexLow;
      }
   }
}
