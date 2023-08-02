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
      /// <summary>Contains information about files in the specified directory. Used for directory handles. Use only when calling GetFileInformationByHandleEx.</summary>
      /// <remarks>
      /// The number of files that are returned for each call to GetFileInformationByHandleEx depends on the size of the buffer that is passed to the function.
      /// Any subsequent calls to GetFileInformationByHandleEx on the same handle will resume the enumeration operation after the last file is returned.
      /// </remarks>
      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      internal struct FILE_ID_BOTH_DIR_INFO
      {
         /// <summary>The offset for the next FILE_ID_BOTH_DIR_INFO structure that is returned. Contains zero (0) if no other entries follow this one.</summary>
         [MarshalAs(UnmanagedType.U4)]
         public readonly int NextEntryOffset;

         /// <summary>The byte offset of the file within the parent directory. This member is undefined for file systems, such as NTFS,
         /// in which the position of a file within the parent directory is not fixed and can be changed at any time to maintain sort order.
         /// </summary>
         [MarshalAs(UnmanagedType.U4)]
         public readonly uint FileIndex;

         /// <summary>The time that the file was created.</summary>
         public FILETIME CreationTime;

         /// <summary>The time that the file was last accessed.</summary>
         public FILETIME LastAccessTime;

         /// <summary>The time that the file was last written to.</summary>
         public FILETIME LastWriteTime;

         /// <summary>The time that the file was last changed.</summary>
         public FILETIME ChangeTime;

         /// <summary>The absolute new end-of-file position as a byte offset from the start of the file to the end of the file.
         /// Because this value is zero-based, it actually refers to the first free byte in the file.
         /// In other words, EndOfFile is the offset to the byte that immediately follows the last valid byte in the file.
         /// </summary>
         public readonly long EndOfFile;

         /// <summary>The number of bytes that are allocated for the file. This value is usually a multiple of the sector or cluster size of the underlying physical device.</summary>
         public readonly long AllocationSize;

         /// <summary>The file attributes.</summary>
         public readonly FileAttributes FileAttributes;

         /// <summary>The length of the file name.</summary>
         [MarshalAs(UnmanagedType.U4)]
         public readonly uint FileNameLength;

         /// <summary>The size of the extended attributes for the file.</summary>
         [MarshalAs(UnmanagedType.U4)]
         public readonly int EaSize;

         /// <summary>The length of ShortName.</summary>
         [MarshalAs(UnmanagedType.U1)]
         public readonly byte ShortNameLength;

         /// <summary>The short 8.3 file naming convention (for example, "FILENAME.TXT") name of the file.</summary>
         [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12, ArraySubType = UnmanagedType.U2)]
         public readonly char[] ShortName;

         /// <summary>The file ID.</summary>
         public readonly long FileId;

         /// <summary>The first character of the file name string. This is followed in memory by the remainder of the string.</summary>
         public IntPtr FileName;
      }
   }
}