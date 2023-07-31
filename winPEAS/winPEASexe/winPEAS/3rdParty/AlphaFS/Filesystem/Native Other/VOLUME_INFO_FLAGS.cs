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

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>Volume Attributes used by the GetVolumeInfo() function.</summary>
      [Flags]
      internal enum VOLUME_INFO_FLAGS
      {
         /// <summary>The specified volume supports case-sensitive file names.</summary>
         FILE_CASE_SENSITIVE_SEARCH = 1,


         /// <summary>The specified volume supports preserved case of file names when it places a name on disk.</summary>
         FILE_CASE_PRESERVED_NAMES = 2,


         /// <summary>The specified volume supports Unicode in file names as they appear on disk.</summary>
         FILE_UNICODE_ON_DISK = 4,


         /// <summary>The specified volume preserves and enforces access control lists (ACL). For example, the NTFS file system preserves and enforces ACLs, and the FAT file system does not.</summary>
         FILE_PERSISTENT_ACLS = 8,


         /// <summary>The specified volume supports file-based compression.</summary>
         FILE_FILE_COMPRESSION = 16,


         /// <summary>The specified volume supports disk quotas.</summary>
         FILE_VOLUME_QUOTAS = 32,


         /// <summary>The specified volume supports sparse files.</summary>
         FILE_SUPPORTS_SPARSE_FILES = 64,


         /// <summary>The specified volume supports re-parse points.</summary>
         FILE_SUPPORTS_REPARSE_POINTS = 128,


         /// <summary>(does not appear on MSDN)</summary>
         FILE_SUPPORTS_REMOTE_STORAGE = 256,


         /// <summary>The specified volume is a compressed volume, for example, a DoubleSpace volume.</summary>
         FILE_VOLUME_IS_COMPRESSED = 32768,


         /// <summary>The specified volume supports object identifiers.</summary>
         FILE_SUPPORTS_OBJECT_IDS = 65536,


         /// <summary>The specified volume supports the Encrypted File System (EFS). For more information, see File Encryption.</summary>
         FILE_SUPPORTS_ENCRYPTION = 131072,


         /// <summary>The specified volume supports named streams.</summary>
         FILE_NAMED_STREAMS = 262144,


         /// <summary>The specified volume is read-only.</summary>
         FILE_READ_ONLY_VOLUME = 524288,


         /// <summary>The specified volume is read-only.</summary>
         FILE_SEQUENTIAL_WRITE_ONCE = 1048576,


         /// <summary>The specified volume supports transactions.For more information, see About KTM.</summary>
         FILE_SUPPORTS_TRANSACTIONS = 2097152,


         /// <summary>The specified volume supports hard links. For more information, see Hard Links and Junctions.</summary>
         /// <remarks>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
         FILE_SUPPORTS_HARD_LINKS = 4194304,


         /// <summary>The specified volume supports extended attributes. An extended attribute is a piece of application-specific metadata that an application can associate with a file and is not part of the file's data.</summary>
         /// <remarks>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
         FILE_SUPPORTS_EXTENDED_ATTRIBUTES = 8388608,


         /// <summary>The file system supports open by FileID. For more information, see FILE_ID_BOTH_DIR_INFO.</summary>
         /// <remarks>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
         FILE_SUPPORTS_OPEN_BY_FILE_ID = 16777216,


         /// <summary>The specified volume supports update sequence number (USN) journals. For more information, see Change Journal Records.</summary>
         /// <remarks>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This value is not supported until Windows Server 2008 R2 and Windows 7.</remarks>
         FILE_SUPPORTS_USN_JOURNAL = 33554432,


         /// <summary>The specified volume is a direct access (DAX) volume.</summary>
         /// <remarks>This flag was introduced in Windows 10, version 1607.</remarks>
         FILE_DAX_VOLUME = 536870912
      }
   }
}
