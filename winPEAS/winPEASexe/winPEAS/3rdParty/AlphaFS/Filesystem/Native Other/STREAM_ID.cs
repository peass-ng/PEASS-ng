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

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>The type of the data contained in the backup stream.</summary>
      internal enum STREAM_ID
      {
         /// <summary>This indicates an error.</summary>
         NONE = 0,

         /// <summary>Standard data. This corresponds to the NTFS $DATA stream type on the default (unnamed) data stream.</summary>
         BACKUP_DATA = 1,

         /// <summary>Extended attribute data. This corresponds to the NTFS $EA stream type.</summary>
         BACKUP_EA_DATA = 2,

         /// <summary>Security descriptor data.</summary>
         BACKUP_SECURITY_DATA = 3,

         /// <summary>Alternative data streams. This corresponds to the NTFS $DATA stream type on a named data stream.</summary>
         BACKUP_ALTERNATE_DATA = 4,

         /// <summary>Hard link information. This corresponds to the NTFS $FILE_NAME stream type.</summary>
         BACKUP_LINK = 5,

         /// <summary>Property data.</summary>
         BACKUP_PROPERTY_DATA = 6,

         /// <summary>Objects identifiers. This corresponds to the NTFS $OBJECT_ID stream type.</summary>
         BACKUP_OBJECT_ID = 7,

         /// <summary>Reparse points. This corresponds to the NTFS $REPARSE_POINT stream type.</summary>
         BACKUP_REPARSE_DATA = 8,

         /// <summary>Sparse file. This corresponds to the NTFS $DATA stream type for a sparse file.</summary>
         BACKUP_SPARSE_BLOCK = 9,

         /// <summary>Transactional NTFS (TxF) data stream.</summary>
         /// <remarks>Windows Server 2003 and Windows XP:  This value is not supported.</remarks>
         BACKUP_TXFS_DATA = 10
      }
   }
}
