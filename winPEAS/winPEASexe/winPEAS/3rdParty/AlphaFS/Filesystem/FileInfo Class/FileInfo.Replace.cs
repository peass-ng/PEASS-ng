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

using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   partial class FileInfo
   {
      #region .NET

      /// <summary>Replaces the contents of a specified file with the file described by the current <see cref="FileInfo"/> object, deleting the original file, and creating a backup of the replaced file.</summary>
      /// <returns>A <see cref="FileInfo"/> object that encapsulates information about the file described by the <paramref name="destinationFileName"/> parameter.</returns>
      /// <remarks>
      ///   The Replace method replaces the contents of a specified file with the contents of the file described by the current
      ///   <see cref="FileInfo"/> object. It also creates a backup of the file that was replaced. Finally, it returns a new
      ///    <see cref="FileInfo"/> object that describes the overwritten file.
      /// </remarks>
      /// <remarks>Pass null to the <paramref name="destinationBackupFileName"/> parameter if you do not want to create a backup of the file being replaced.</remarks>
      /// <param name="destinationFileName">The name of a file to replace with the current file.</param>
      /// <param name="destinationBackupFileName">The name of a file with which to create a backup of the file described by the <paramref name="destinationFileName"/> parameter.</param>
      [SecurityCritical]
      public FileInfo Replace(string destinationFileName, string destinationBackupFileName)
      {
         return Replace(destinationFileName, destinationBackupFileName, false, PathFormat.RelativePath);
      }


      /// <summary>Replaces the contents of a specified file with the file described by the current <see cref="FileInfo"/> object, deleting the original file, and creating a backup of the replaced file. Also specifies whether to ignore merge errors.</summary>
      /// <returns>A <see cref="FileInfo"/> object that encapsulates information about the file described by the <paramref name="destinationFileName"/> parameter.</returns>
      /// <remarks>
      ///   The Replace method replaces the contents of a specified file with the contents of the file described by the current
      ///   <see cref="FileInfo"/> object. It also creates a backup of the file that was replaced. Finally, it returns a new
      ///   <see cref="FileInfo"/> object that describes the overwritten file.
      /// </remarks>
      /// <remarks>Pass null to the <paramref name="destinationBackupFileName"/> parameter if you do not want to create a backup of the file being replaced.</remarks>
      /// <param name="destinationFileName">The name of a file to replace with the current file.</param>
      /// <param name="destinationBackupFileName">The name of a file with which to create a backup of the file described by the <paramref name="destinationFileName"/> parameter.</param>
      /// <param name="ignoreMetadataErrors"><c>true</c> to ignore merge errors (such as attributes and ACLs) from the replaced file to the replacement file; otherwise, <c>false</c>.</param>
      [SecurityCritical]
      public FileInfo Replace(string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
      {
         return Replace(destinationFileName, destinationBackupFileName, ignoreMetadataErrors, PathFormat.RelativePath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Replaces the contents of a specified file with the file described by the current <see cref="FileInfo"/> object, deleting the original file, and creating a backup of the replaced file. Also specifies whether to ignore merge errors.</summary>
      /// <returns>A <see cref="FileInfo"/> object that encapsulates information about the file described by the <paramref name="destinationFileName"/> parameter.</returns>
      /// <remarks>
      ///   The Replace method replaces the contents of a specified file with the contents of the file described by the current
      ///   <see cref="FileInfo"/> object. It also creates a backup of the file that was replaced. Finally, it returns a new
      ///   <see cref="FileInfo"/> object that describes the overwritten file.
      /// </remarks>
      /// <remarks>Pass null to the <paramref name="destinationBackupFileName"/> parameter if you do not want to create a backup of the file being replaced.</remarks>
      /// <param name="destinationFileName">The name of a file to replace with the current file.</param>
      /// <param name="destinationBackupFileName">The name of a file with which to create a backup of the file described by the <paramref name="destinationFileName"/> parameter.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo Replace(string destinationFileName, string destinationBackupFileName, PathFormat pathFormat)
      {
         return Replace(destinationFileName, destinationBackupFileName, false, pathFormat);
      }


      /// <summary>[AlphaFS] Replaces the contents of a specified file with the file described by the current <see cref="FileInfo"/> object, deleting the original file, and creating a backup of the replaced file. Also specifies whether to ignore merge errors.</summary>
      /// <returns>A <see cref="FileInfo"/> object that encapsulates information about the file described by the <paramref name="destinationFileName"/> parameter.</returns>
      /// <remarks>
      ///   The Replace method replaces the contents of a specified file with the contents of the file described by the current
      ///   <see cref="FileInfo"/> object. It also creates a backup of the file that was replaced. Finally, it returns a new
      ///   <see cref="FileInfo"/> object that describes the overwritten file.
      /// </remarks>
      /// <remarks>Pass null to the <paramref name="destinationBackupFileName"/> parameter if you do not want to create a backup of the file being replaced.</remarks>
      /// <param name="destinationFileName">The name of a file to replace with the current file.</param>
      /// <param name="destinationBackupFileName">The name of a file with which to create a backup of the file described by the <paramref name="destinationFileName"/> parameter.</param>
      /// <param name="ignoreMetadataErrors"><c>true</c> to ignore merge errors (such as attributes and ACLs) from the replaced file to the replacement file; otherwise, <c>false</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo Replace(string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors, PathFormat pathFormat)
      {
         const GetFullPathOptions options = GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck;

         var destinationFileNameLp = Path.GetExtendedLengthPathCore(Transaction, destinationFileName, pathFormat, options);

         var destinationBackupFileNameLp = destinationBackupFileName != null
            ? Path.GetExtendedLengthPathCore(Transaction, destinationBackupFileName, pathFormat, options)
            : null;

         File.ReplaceCore(LongFullName, destinationFileNameLp, destinationBackupFileNameLp, ignoreMetadataErrors, PathFormat.LongFullPath);

         return new FileInfo(Transaction, destinationFileNameLp, PathFormat.LongFullPath);
      }
   }
}
