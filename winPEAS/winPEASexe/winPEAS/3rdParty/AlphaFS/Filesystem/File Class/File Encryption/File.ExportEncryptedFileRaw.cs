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

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] Backs up (export) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is
      /// intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <remarks>
      ///   <para>
      ///      The file being backed up is not decrypted; it is backed up in its encrypted state.
      ///   </para>
      ///   <para>
      ///      If the caller does not have access to the key for the file, the caller needs
      ///      <see cref="Security.Privilege.Backup"/> to export encrypted files. See
      ///      <see cref="Security.PrivilegeEnabler"/>.
      ///   </para>
      ///   <para>
      ///      To backup an encrypted file call one of the
      ///      <see cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/> overloads and specify the file to backup
      ///      along with the destination stream of the backup data.
      ///   </para>
      ///   <para>
      ///      This function is intended for the backup of only encrypted files; see <see cref="BackupFileStream"/> for backup
      ///      of unencrypted files.
      ///   </para>
      /// </remarks>
      /// <param name="fileName">The name of the file to be backed up.</param>
      /// <param name="outputStream">The destination stream to which the backup data will be written.</param>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/>      
      public static void ExportEncryptedFileRaw(string fileName, Stream outputStream)
      {
         ImportExportEncryptedFileDirectoryRawCore(true, false, outputStream, fileName, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Backs up (export) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <remarks>
      ///   <para>
      ///      The file being backed up is not decrypted; it is backed up in its encrypted state.
      ///   </para>
      ///   <para>
      ///      If the caller does not have access to the key for the file, the caller needs
      ///      <see cref="Security.Privilege.Backup"/> to export encrypted files. See
      ///      <see cref="Security.PrivilegeEnabler"/>.
      ///   </para>
      ///   <para>
      ///      To backup an encrypted file call one of the
      ///      <see cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/> overloads and specify the file to backup
      ///      along with the destination stream of the backup data.
      ///   </para>
      ///   <para>
      ///      This function is intended for the backup of only encrypted files; see <see cref="BackupFileStream"/> for backup
      ///      of unencrypted files.
      ///   </para>
      /// </remarks>
      /// <param name="fileName">The name of the file to be backed up.</param>
      /// <param name="outputStream">The destination stream to which the backup data will be written.</param>
      /// <param name="pathFormat">The path format of the <paramref name="fileName"/> parameter.</param>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/>
      public static void ExportEncryptedFileRaw(string fileName, Stream outputStream, PathFormat pathFormat)
      {
         ImportExportEncryptedFileDirectoryRawCore(true, false, outputStream, fileName, false, pathFormat);
      }
   }
}
