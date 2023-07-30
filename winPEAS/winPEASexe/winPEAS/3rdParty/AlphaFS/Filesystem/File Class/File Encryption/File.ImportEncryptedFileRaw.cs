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
      /// <summary>[AlphaFS] Restores (import) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <remarks>
      ///   <para>
      ///     If the caller does not have access to the key for the file, the caller needs
      ///     <see cref="Security.Privilege.Backup"/> to restore encrypted files. See
      ///     <see cref="Security.PrivilegeEnabler"/>.
      ///   </para>
      ///   <para>
      ///     To restore an encrypted file call one of the
      ///     <see cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/> overloads and specify the file to restore
      ///     along with the destination stream of the restored data.
      ///   </para>
      ///   <para>
      ///     This function is intended for the restoration of only encrypted files; see <see cref="BackupFileStream"/> for
      ///     backup of unencrypted files.
      ///   </para>
      /// </remarks>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationFilePath">The path of the destination file to restore to.</param>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>
      public static void ImportEncryptedFileRaw(Stream inputStream, string destinationFilePath)
      {
         ImportExportEncryptedFileDirectoryRawCore(false, false, inputStream, destinationFilePath, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Restores (import) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <remarks>
      ///   <para>
      ///     If the caller does not have access to the key for the file, the caller needs
      ///     <see cref="Security.Privilege.Backup"/> to restore encrypted files. See
      ///     <see cref="Security.PrivilegeEnabler"/>.
      ///   </para>
      ///   <para>
      ///     To restore an encrypted file call one of the
      ///     <see cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/> overloads and specify the file to restore
      ///     along with the destination stream of the restored data.
      ///   </para>
      ///   <para>
      ///     This function is intended for the restoration of only encrypted files; see <see cref="BackupFileStream"/> for
      ///     backup of unencrypted files.
      ///   </para>
      /// </remarks>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationFilePath">The path of the destination file to restore to.</param>
      /// <param name="pathFormat">The path format of the <paramref name="destinationFilePath"/> parameter.</param>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>
      public static void ImportEncryptedFileRaw(Stream inputStream, string destinationFilePath, PathFormat pathFormat)
      {
         ImportExportEncryptedFileDirectoryRawCore(false, false, inputStream, destinationFilePath, false, pathFormat);
      }


      /// <summary>[AlphaFS] Restores (import) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <remarks>
      ///   <para>
      ///     If the caller does not have access to the key for the file, the caller needs
      ///     <see cref="Security.Privilege.Backup"/> to restore encrypted files. See
      ///     <see cref="Security.PrivilegeEnabler"/>.
      ///   </para>
      ///   <para>
      ///     To restore an encrypted file call one of the
      ///     <see cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/> overloads and specify the file to restore
      ///     along with the destination stream of the restored data.
      ///   </para>
      ///   <para>
      ///     This function is intended for the restoration of only encrypted files; see <see cref="BackupFileStream"/> for
      ///     backup of unencrypted files.
      ///   </para>
      /// </remarks>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationFilePath">The path of the destination file to restore to.</param>
      /// <param name="overwriteHidden">If set to <c>true</c> a hidden file will be overwritten on import.</param>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>
      public static void ImportEncryptedFileRaw(Stream inputStream, string destinationFilePath, bool overwriteHidden)
      {
         ImportExportEncryptedFileDirectoryRawCore(false, false, inputStream, destinationFilePath, overwriteHidden, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Restores (import) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <remarks>
      ///   <para>
      ///     If the caller does not have access to the key for the file, the caller needs
      ///     <see cref="Security.Privilege.Backup"/> to restore encrypted files. See
      ///     <see cref="Security.PrivilegeEnabler"/>.
      ///   </para>
      ///   <para>
      ///     To restore an encrypted file call one of the
      ///     <see cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/> overloads and specify the file to restore
      ///     along with the destination stream of the restored data.
      ///   </para>
      ///   <para>
      ///     This function is intended for the restoration of only encrypted files; see <see cref="BackupFileStream"/> for
      ///     backup of unencrypted files.
      ///   </para>
      /// </remarks>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationFilePath">The path of the destination file to restore to.</param>
      /// <param name="overwriteHidden">If set to <c>true</c> a hidden file will be overwritten on import.</param>
      /// <param name="pathFormat">The path format of the <paramref name="destinationFilePath"/> parameter.</param>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>
      public static void ImportEncryptedFileRaw(Stream inputStream, string destinationFilePath, bool overwriteHidden, PathFormat pathFormat)
      {
         ImportExportEncryptedFileDirectoryRawCore(false, false, inputStream, destinationFilePath, overwriteHidden, pathFormat);
      }
   }
}
