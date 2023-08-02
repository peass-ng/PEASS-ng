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
using System.Globalization;
using System.IO;
using System.Security;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
      [SecurityCritical]
      private static bool RestartMoveOrThrowException(bool retry, int lastError, bool isFolder, bool isMove, CopyMoveArguments cma, string sourcePathLp, string destinationPathLp)
      {
         var restart = false;
         var srcExists = ExistsCore(cma.Transaction, isFolder, sourcePathLp, PathFormat.LongFullPath);
         var dstExists = ExistsCore(cma.Transaction, isFolder, destinationPathLp, PathFormat.LongFullPath);


         switch ((uint) lastError)
         {
            // File.Copy()
            // File.Move()
            // MSDN: .NET 3.5+: FileNotFoundException: sourcePath was not found. 
            //
            // File.Copy()
            // File.Move()
            // Directory.Move()
            // MSDN: .NET 3.5+: DirectoryNotFoundException: The path specified in sourcePath or destinationPath is invalid (for example, it is on an unmapped drive).
            case Win32Errors.ERROR_FILE_NOT_FOUND: // On files.
            case Win32Errors.ERROR_PATH_NOT_FOUND: // On folders.

               if (!srcExists)
                  Directory.ExistsDriveOrFolderOrFile(cma.Transaction, sourcePathLp, isFolder, lastError, false, true);

               if (!dstExists)
                  Directory.ExistsDriveOrFolderOrFile(cma.Transaction, destinationPathLp, isFolder, lastError, false, true);

               break;


            case Win32Errors.ERROR_NOT_READY: // DeviceNotReadyException: Floppy device or network drive not ready.
               Directory.ExistsDriveOrFolderOrFile(cma.Transaction, sourcePathLp, false, lastError, true, false);
               Directory.ExistsDriveOrFolderOrFile(cma.Transaction, destinationPathLp, false, lastError, true, false);
               break;


            // File.Copy()
            // Directory.Copy()
            case Win32Errors.ERROR_ALREADY_EXISTS: // On folders.
            case Win32Errors.ERROR_FILE_EXISTS:    // On files.
               lastError = (int) (isFolder ? Win32Errors.ERROR_ALREADY_EXISTS : Win32Errors.ERROR_FILE_EXISTS);

               if (!retry)
                  NativeError.ThrowException(lastError, isFolder, destinationPathLp);

               break;


            default:
               var attrs = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();

               FillAttributeInfoCore(cma.Transaction, destinationPathLp, ref attrs, false, false);

               var destIsFolder = IsDirectory(attrs.dwFileAttributes);


               // For a number of error codes (sharing violation, path not found, etc)
               // we don't know if the problem was with the source or destination file.

               // Check if destination directory already exists.
               // Directory.Move()
               // MSDN: .NET 3.5+: IOException: destDirName already exists.

               if (destIsFolder && dstExists && !retry)
                  NativeError.ThrowException(Win32Errors.ERROR_ALREADY_EXISTS, destinationPathLp);




               if (isMove)
               {
                  // Ensure that the source file or folder exists.
                  // Directory.Move()
                  // MSDN: .NET 3.5+: DirectoryNotFoundException: The path specified by sourceDirName is invalid (for example, it is on an unmapped drive). 

                  if (!srcExists && !retry)
                     NativeError.ThrowException(isFolder ? Win32Errors.ERROR_PATH_NOT_FOUND : Win32Errors.ERROR_FILE_NOT_FOUND, sourcePathLp);
               }


               // Try reading the source file.
               var fileNameLp = destinationPathLp;

               if (!isFolder)
               {
                  using (var safeHandle = CreateFileCore(cma.Transaction, false, sourcePathLp, ExtendedFileAttributes.Normal, null, FileMode.Open, 0, FileShare.Read, false, false, PathFormat.LongFullPath))
                     if (null != safeHandle)
                        fileNameLp = sourcePathLp;
               }


               if (lastError == Win32Errors.ERROR_ACCESS_DENIED)
               {
                  // File.Copy()
                  // File.Move()
                  // MSDN: .NET 3.5+: IOException: An I/O error has occurred.


                  // Directory exists with the same name as the file.

                  if (dstExists && !isFolder && destIsFolder && !retry)

                     NativeError.ThrowException(lastError, false, string.Format(CultureInfo.InvariantCulture, Resources.Target_File_Is_A_Directory, destinationPathLp));

                  
                  // MSDN: .NET 3.5+: IOException: The directory specified by path is read-only.

                  if (isMove && IsReadOnlyOrHidden(attrs.dwFileAttributes))
                  {
                     if (HasReplaceExisting(cma.MoveOptions))
                     {
                        // Reset attributes to Normal.
                        SetAttributesCore(cma.Transaction, isFolder, destinationPathLp, FileAttributes.Normal, PathFormat.LongFullPath);


                        restart = true;
                        break;
                     }


                     // MSDN: .NET 3.5+: UnauthorizedAccessException: destinationPath is read-only.
                     // MSDN: Win32 CopyFileXxx: This function fails with ERROR_ACCESS_DENIED if the destination file already exists
                     // and has the FILE_ATTRIBUTE_HIDDEN or FILE_ATTRIBUTE_READONLY attribute set.

                     if (!retry)
                        throw new FileReadOnlyException(destinationPathLp);
                  }
               }


               // MSDN: .NET 3.5+: An I/O error has occurred. 
               // File.Copy(): IOException: destinationPath exists and overwrite is false.
               // File.Move(): The destination file already exists or sourcePath was not found.

               if (!retry)
                  NativeError.ThrowException(lastError, isFolder, fileNameLp);

               break;
         }


         return restart;
      }
   }
}
