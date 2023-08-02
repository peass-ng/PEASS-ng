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
using System.IO;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>[AlphaFS] Contains information about files in the specified directory. Used for directory handles.</summary>
   [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Dir")]
   [Serializable]
   [SecurityCritical]
   public sealed class FileIdBothDirectoryInfo
   {
      internal FileIdBothDirectoryInfo(NativeMethods.FILE_ID_BOTH_DIR_INFO fibdi, string fileName)
      {
         CreationTimeUtc = DateTime.FromFileTimeUtc(fibdi.CreationTime);
         LastAccessTimeUtc = DateTime.FromFileTimeUtc(fibdi.LastAccessTime);
         LastWriteTimeUtc = DateTime.FromFileTimeUtc(fibdi.LastWriteTime);
         ChangeTimeUtc = DateTime.FromFileTimeUtc(fibdi.ChangeTime);

         AllocationSize = fibdi.AllocationSize;
         EndOfFile = fibdi.EndOfFile;
         ExtendedAttributesSize = fibdi.EaSize;
         
         FileAttributes = fibdi.FileAttributes;
         FileId = fibdi.FileId;
         FileIndex = fibdi.FileIndex;
         FileName = fileName;

         // ShortNameLength is the number of bytes in the short name; since we have a unicode string we must divide that by 2.
         ShortName = new string(fibdi.ShortName, 0, fibdi.ShortNameLength / UnicodeEncoding.CharSize);
      }




      /// <summary>The number of bytes that are allocated for the file. This value is usually a multiple of the sector or cluster size of the underlying physical device.</summary>
      public long AllocationSize { get; set; }


      /// <summary>Gets the time this entry was changed.</summary>
      /// <value>The time this entry was changed.</value>
      public DateTime ChangeTime
      {
         get { return ChangeTimeUtc.ToLocalTime(); }
      }


      /// <summary>Gets the time, in coordinated universal time (UTC), this entry was changed.</summary>
      /// <value>The time, in coordinated universal time (UTC), this entry was changed.</value>
      public DateTime ChangeTimeUtc { get; set; }


      /// <summary>Gets the time this entry was created.</summary>
      /// <value>The time this entry was created.</value>
      public DateTime CreationTime
      {
         get { return CreationTimeUtc.ToLocalTime(); }
      }


      /// <summary>Gets the time, in coordinated universal time (UTC), this entry was created.</summary>
      /// <value>The time, in coordinated universal time (UTC), this entry was created.</value>
      public DateTime CreationTimeUtc { get; set; }


      /// <summary>The size of the extended attributes for the file.</summary>
      public int ExtendedAttributesSize { get; set; }


      /// <summary>The absolute new end-of-file position as a byte offset from the start of the file to the end of the file. 
      /// Because this value is zero-based, it actually refers to the first free byte in the file. In other words, <b>EndOfFile</b> is the offset to 
      /// the byte that immediately follows the last valid byte in the file.
      /// </summary>
      public long EndOfFile { get; set; }


      /// <summary>The file attributes.</summary>
      public FileAttributes FileAttributes { get; set; }


      /// <summary>The file ID.</summary>
      public long FileId { get; set; }


      /// <summary>The byte offset of the file within the parent directory. This member is undefined for file systems, such as NTFS,
      /// in which the position of a file within the parent directory is not fixed and can be changed at any time to maintain sort order.
      /// </summary>
      public long FileIndex { get; set; }


      /// <summary>The name of the file.</summary>
      public string FileName { get; set; }


      /// <summary>Gets the time this entry was last accessed.</summary>
      /// <value>The time this entry was last accessed.</value>
      public DateTime LastAccessTime
      {
         get { return LastAccessTimeUtc.ToLocalTime(); }
      }


      /// <summary>Gets the time, in coordinated universal time (UTC), this entry was last accessed.</summary>
      /// <value>The time, in coordinated universal time (UTC), this entry was last accessed.</value>
      public DateTime LastAccessTimeUtc { get; set; }


      /// <summary>Gets the time this entry was last modified.</summary>
      /// <value>The time this entry was last modified.</value>
      public DateTime LastWriteTime
      {
         get { return LastWriteTimeUtc.ToLocalTime(); }
      }


      /// <summary>Gets the time, in coordinated universal time (UTC), this entry was last modified.</summary>
      /// <value>The time, in coordinated universal time (UTC), this entry was last modified.</value>
      public DateTime LastWriteTimeUtc { get; set; }


      /// <summary>The short 8.3 file naming convention (for example, FILENAME.TXT) name of the file.</summary>
      public string ShortName { get; set; }
   }
}
