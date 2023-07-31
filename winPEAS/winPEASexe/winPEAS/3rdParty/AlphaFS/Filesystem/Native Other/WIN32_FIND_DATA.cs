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
using System.Runtime.InteropServices;

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>Contains information about the file that is found by the FindFirstFile, FindFirstFileEx, or FindNextFile function.</summary>
      /// <remarks>
      /// If a file has a long file name, the complete name appears in the cFileName member, and the 8.3 format truncated version of the name appears
      /// in the cAlternateFileName member. Otherwise, cAlternateFileName is empty. If the FindFirstFileEx function was called with a value of FindExInfoBasic
      /// in the fInfoLevelId parameter, the cAlternateFileName member will always contain a <c>null</c> string value. This remains true for all subsequent calls to the
      /// FindNextFile function. As an alternative method of retrieving the 8.3 format version of a file name, you can use the GetShortPathName function.
      /// For more information about file names, see File Names, Paths, and Namespaces.
      /// </remarks>
      /// <remarks>
      /// Not all file systems can record creation and last access times, and not all file systems record them in the same manner.
      /// For example, on the FAT file system, create time has a resolution of 10 milliseconds, write time has a resolution of 2 seconds,
      /// and access time has a resolution of 1 day. The NTFS file system delays updates to the last access time for a file by up to 1 hour
      /// after the last access. For more information, see File Times.
      /// </remarks>
      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      [Serializable]
      internal struct WIN32_FIND_DATA
      {
         /// <summary>The file attributes of a file.</summary>
         public FileAttributes dwFileAttributes;

         
         /// <summary>A <see cref="FILETIME"/> structure that specifies when a file or directory was created.
         /// If the underlying file system does not support creation time, this member is zero.</summary>
         public FILETIME ftCreationTime;

         
         /// <summary>A <see cref="FILETIME"/> structure.
         /// For a file, the structure specifies when the file was last read from, written to, or for executable files, run.
         /// For a directory, the structure specifies when the directory is created. If the underlying file system does not support last access time, this member is zero.
         /// On the FAT file system, the specified date for both files and directories is correct, but the time of day is always set to midnight.
         /// </summary>
         public FILETIME ftLastAccessTime;

         
         /// <summary>A <see cref="FILETIME"/> structure.
         /// For a file, the structure specifies when the file was last written to, truncated, or overwritten, for example, when WriteFile or SetEndOfFile are used.
         /// The date and time are not updated when file attributes or security descriptors are changed.
         /// For a directory, the structure specifies when the directory is created. If the underlying file system does not support last write time, this member is zero.
         /// </summary>
         public FILETIME ftLastWriteTime;

         
         /// <summary>The high-order DWORD of the file size. This member does not have a meaning for directories.
         /// This value is zero unless the file size is greater than MAXDWORD.
         /// The size of the file is equal to (nFileSizeHigh * (MAXDWORD+1)) + nFileSizeLow.
         /// </summary>
         public uint nFileSizeHigh;

         
         /// <summary>The low-order DWORD of the file size. This member does not have a meaning for directories.</summary>
         public uint nFileSizeLow;

         
         /// <summary>If the dwFileAttributes member includes the FILE_ATTRIBUTE_REPARSE_POINT attribute, this member specifies the reparse point tag.
         /// Otherwise, this value is undefined and should not be used.
         /// </summary>
         public readonly ReparsePointTag dwReserved0;

         
         /// <summary>Reserved for future use.</summary>
         private readonly uint dwReserved1;

         
         /// <summary>The name of the file.</summary>
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)] public string cFileName;

         
         /// <summary>An alternative name for the file. This name is in the classic 8.3 file name format.</summary>
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public readonly string cAlternateFileName;
      }
   }
}
