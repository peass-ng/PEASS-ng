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

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Checks if specified <paramref name="path"/> is a local- or network drive.</summary>
      /// <param name="path">The path to check, such as: "C:" or "\\server\c$".</param>
      /// <returns><c>true</c> if the drive exists, <c>false</c> otherwise.</returns>
      public static bool ExistsDrive(string path)
      {
         return ExistsDriveOrFolderOrFile(null, path, false, (int) Win32Errors.NO_ERROR, false, false);
      }


      /// <summary>[AlphaFS] Checks if specified <paramref name="path"/> is a local- or network drive.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to check, such as: "C:" or "\\server\c$".</param>
      /// <returns><c>true</c> if the drive exists, <c>false</c> otherwise.</returns>
      public static bool ExistsDrive(KernelTransaction transaction, string path)
      {
         return ExistsDriveOrFolderOrFile(transaction, path, false, (int) Win32Errors.NO_ERROR, false, false);
      }


      /// <summary>[AlphaFS] Checks if specified <paramref name="path"/> is a local- or network drive.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to check, such as: "C:" or "\\server\c$".</param>
      /// <param name="throwIfDriveNotExists">Throws DeviceNotReadyException when drive is not found.</param>
      /// <returns><c>true</c> if the drive exists, <c>false</c> otherwise.</returns>
      [Obsolete("This function will be removed.")]
      public static bool ExistsDrive(KernelTransaction transaction, string path, bool throwIfDriveNotExists)
      {
         return ExistsDriveOrFolderOrFile(transaction, path, false, (int) Win32Errors.NO_ERROR, throwIfDriveNotExists, false);
      }


      /// <summary>[AlphaFS] Checks if specified <paramref name="path"/> is a local- or network drive.</summary>
      /// <returns><c>true</c> if the drive exists, <c>false</c> otherwise.</returns>
      internal static bool ExistsDriveOrFolderOrFile(KernelTransaction transaction, string path, bool isFolder, int lastError, bool throwIfDriveNotExists, bool throwIfFolderOrFileNotExists)
      {
         if (Utils.IsNullOrWhiteSpace(path))
            return false;


         var drive = GetDirectoryRootCore(transaction, path, Path.IsPathRooted(path) ? PathFormat.FullPath : PathFormat.RelativePath);

         var driveExists = null != drive && File.ExistsCore(transaction, true, drive, PathFormat.FullPath);

         var regularPath = Path.GetCleanExceptionPath(path);


         if (!driveExists && throwIfDriveNotExists || lastError == Win32Errors.ERROR_NOT_READY)
            throw new DeviceNotReadyException(drive, true);


         throwIfFolderOrFileNotExists = throwIfFolderOrFileNotExists && lastError != Win32Errors.NO_ERROR;

         if (throwIfFolderOrFileNotExists)
         {
            if (lastError != Win32Errors.NO_ERROR)
            {
               if (lastError == Win32Errors.ERROR_PATH_NOT_FOUND)
                  throw new DirectoryNotFoundException(regularPath);


               if (lastError == Win32Errors.ERROR_FILE_NOT_FOUND)
               {
                  if (isFolder)
                     throw new DirectoryNotFoundException(regularPath);

                  throw new FileNotFoundException(regularPath);
               }
            }
         }


         return driveExists;
      }
   }
}
