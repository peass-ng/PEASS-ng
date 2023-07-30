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

using System.Diagnostics.CodeAnalysis;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>The type of the data contained in the backup stream. This member can be one of the following values.</summary>
   public enum StreamId
   {
      /// <summary>This indicates an error.</summary>
      None = NativeMethods.STREAM_ID.NONE,


      /// <summary>Standard data. This corresponds to the NTFS $DATA stream type on the default (unnamed) data stream.</summary>
      BackupData = NativeMethods.STREAM_ID.BACKUP_DATA,


      /// <summary>Extended attribute data. This corresponds to the NTFS $EA stream type.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ea")]
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ea")]
      BackupEaData = NativeMethods.STREAM_ID.BACKUP_EA_DATA,


      /// <summary>Security descriptor data.</summary>
      BackupSecurityData = NativeMethods.STREAM_ID.BACKUP_SECURITY_DATA,


      /// <summary>Alternative data streams. This corresponds to the NTFS $DATA stream type on a named data stream.</summary>
      BackupAlternateData = NativeMethods.STREAM_ID.BACKUP_ALTERNATE_DATA,


      /// <summary>Hard link information. This corresponds to the NTFS $FILE_NAME stream type.</summary>
      BackupLink = NativeMethods.STREAM_ID.BACKUP_LINK,


      /// <summary>Property data.</summary>
      BackupPropertyData = NativeMethods.STREAM_ID.BACKUP_PROPERTY_DATA,


      /// <summary>Objects identifiers. This corresponds to the NTFS $OBJECT_ID stream type.</summary>
      BackupObjectId = NativeMethods.STREAM_ID.BACKUP_OBJECT_ID,


      /// <summary>Reparse points. This corresponds to the NTFS $REPARSE_POINT stream type.</summary>
      BackupReparseData = NativeMethods.STREAM_ID.BACKUP_REPARSE_DATA,


      /// <summary>Sparse file. This corresponds to the NTFS $DATA stream type for a sparse file.</summary>
      BackupSparseBlock = NativeMethods.STREAM_ID.BACKUP_SPARSE_BLOCK,


      /// <summary>Transactional NTFS (TxF) data stream.</summary>
      /// <remarks>Windows Server 2003 and Windows XP:  This value is not supported.</remarks>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Txfs")]
      BackupTxfsData = NativeMethods.STREAM_ID.BACKUP_TXFS_DATA
   }
}
