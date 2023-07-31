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
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Restores (import) encrypted directories. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.
      /// </summary>
      /// <remarks>
      ///   <para>If the caller does not have access to the key for the directory, the caller needs <see cref="Security.Privilege.Backup"/> to restore encrypted directories. See <see cref="Security.PrivilegeEnabler"/>.</para>
      ///   <para>To restore an encrypted directory call one of the <see cref="O:Alphaleonis.Win32.Filesystem.Directory.ImportEncryptedDirectoryRaw"/> overloads and specify the file to restore along with the destination stream of the restored data.</para>
      ///   <para>This function is intended for the restoration of only encrypted directories; see <see cref="BackupFileStream"/> for backup of unencrypted files.</para>
      /// </remarks>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>      
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.Directory.ExportEncryptedDirectoryRaw"/>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationPath">The path of the destination directory to restore to.</param>
      public static void ImportEncryptedDirectoryRaw(Stream inputStream, string destinationPath)
      {
         File.ImportExportEncryptedFileDirectoryRawCore(false, true, inputStream, destinationPath, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Restores (import) encrypted directories. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.
      /// </summary>
      /// <remarks>
      ///   <para>If the caller does not have access to the key for the directory, the caller needs <see cref="Security.Privilege.Backup"/> to restore encrypted directories. See <see cref="Security.PrivilegeEnabler"/>.</para>
      ///   <para>To restore an encrypted directory call one of the <see cref="O:Alphaleonis.Win32.Filesystem.Directory.ImportEncryptedDirectoryRaw"/> overloads and specify the file to restore along with the destination stream of the restored data.</para>
      ///   <para>This function is intended for the restoration of only encrypted directories; see <see cref="BackupFileStream"/> for backup of unencrypted files.</para>
      /// </remarks>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.Directory.ExportEncryptedDirectoryRaw"/>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationPath">The path of the destination directory to restore to.</param>
      /// <param name="pathFormat">The path format of the <paramref name="destinationPath"/> parameter.</param>
      public static void ImportEncryptedDirectoryRaw(Stream inputStream, string destinationPath, PathFormat pathFormat)
      {
         File.ImportExportEncryptedFileDirectoryRawCore(false, true, inputStream, destinationPath, false, pathFormat);
      }


      /// <summary>[AlphaFS] Restores (import) encrypted directories. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.
      /// </summary>
      /// <remarks>
      ///   <para>If the caller does not have access to the key for the directory, the caller needs <see cref="Security.Privilege.Backup"/> to restore encrypted directories. See <see cref="Security.PrivilegeEnabler"/>.</para>
      ///   <para>To restore an encrypted directory call one of the <see cref="O:Alphaleonis.Win32.Filesystem.Directory.ImportEncryptedDirectoryRaw"/> overloads and specify the file to restore along with the destination stream of the restored data.</para>
      ///   <para>This function is intended for the restoration of only encrypted directories; see <see cref="BackupFileStream"/> for backup of unencrypted files.</para>
      /// </remarks>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.Directory.ExportEncryptedDirectoryRaw"/>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationPath">The path of the destination directory to restore to.</param>
      /// <param name="overwriteHidden">If set to <c>true</c> a hidden directory will be overwritten on import.</param>
      public static void ImportEncryptedDirectoryRaw(Stream inputStream, string destinationPath, bool overwriteHidden)
      {
         File.ImportExportEncryptedFileDirectoryRawCore(false, true, inputStream, destinationPath, overwriteHidden, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Restores (import) encrypted directories. This is one of a group of Encrypted File System (EFS) functions that is
      ///   intended to implement backup and restore functionality, while maintaining files in their encrypted state.
      /// </summary>
      /// <remarks>
      ///   <para>If the caller does not have access to the key for the directory, the caller needs <see cref="Security.Privilege.Backup"/> to restore encrypted directories. See <see cref="Security.PrivilegeEnabler"/>.</para>
      ///   <para>To restore an encrypted directory call one of the <see cref="O:Alphaleonis.Win32.Filesystem.Directory.ImportEncryptedDirectoryRaw"/> overloads and specify the file to restore along with the destination stream of the restored data.</para>
      ///   <para>This function is intended for the restoration of only encrypted directories; see <see cref="BackupFileStream"/> for backup of unencrypted files.</para>
      /// </remarks>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ExportEncryptedFileRaw"/>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.File.ImportEncryptedFileRaw"/>
      /// <seealso cref="O:Alphaleonis.Win32.Filesystem.Directory.ExportEncryptedDirectoryRaw"/>
      /// <param name="inputStream">The stream to read previously backed up data from.</param>
      /// <param name="destinationPath">The path of the destination directory to restore to.</param>
      /// <param name="overwriteHidden">If set to <c>true</c> a hidden directory will be overwritten on import.</param>
      /// <param name="pathFormat">The path format of the <paramref name="destinationPath"/> parameter.</param>
      public static void ImportEncryptedDirectoryRaw(Stream inputStream, string destinationPath, bool overwriteHidden, PathFormat pathFormat)
      {
         File.ImportExportEncryptedFileDirectoryRawCore(false, true, inputStream, destinationPath, overwriteHidden, pathFormat);
      }
   }
}
