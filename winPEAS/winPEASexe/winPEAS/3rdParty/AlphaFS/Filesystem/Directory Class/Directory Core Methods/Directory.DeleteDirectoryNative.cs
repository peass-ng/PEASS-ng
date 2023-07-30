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
using System.Runtime.InteropServices;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      private static void DeleteDirectoryNative(KernelTransaction transaction, string pathLp, bool ignoreReadOnly, bool continueOnNotFound, FileAttributes attributes)
      {

      startRemoveDirectory:

         var success = null == transaction || !NativeMethods.IsAtLeastWindowsVista

            // RemoveDirectory() / RemoveDirectoryTransacted()
            // 2014-09-09: MSDN confirms LongPath usage.

            // RemoveDirectory on a symbolic link will remove the link itself.

            ? NativeMethods.RemoveDirectory(pathLp)

            : NativeMethods.RemoveDirectoryTransacted(pathLp, transaction.SafeHandle);


         var lastError = Marshal.GetLastWin32Error();

         if (!success)
         {
            switch ((uint) lastError)
            {
               case Win32Errors.ERROR_DIR_NOT_EMPTY:
                  // MSDN: .NET 3.5+: IOException: The directory specified by path is not an empty directory. 
                  throw new DirectoryNotEmptyException(pathLp, true);


               case Win32Errors.ERROR_DIRECTORY:
                  // MSDN: .NET 3.5+: DirectoryNotFoundException: Path refers to a file instead of a directory.
                  if (File.ExistsCore(transaction, false, pathLp, PathFormat.LongFullPath))
                     throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "({0}) {1}", lastError, string.Format(CultureInfo.InvariantCulture, Resources.Target_Directory_Is_A_File, pathLp)));
                  break;


               case Win32Errors.ERROR_PATH_NOT_FOUND:
                  if (continueOnNotFound)
                     return;
                  break;


               case Win32Errors.ERROR_SHARING_VIOLATION:
                  // MSDN: .NET 3.5+: IOException: The directory is being used by another process or there is an open handle on the directory.
                  NativeError.ThrowException(lastError, pathLp);
                  break;


               case Win32Errors.ERROR_ACCESS_DENIED:

                  if (attributes == 0)
                  {
                     var attrs = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();

                     if (File.FillAttributeInfoCore(transaction, pathLp, ref attrs, false, true) == Win32Errors.NO_ERROR)

                        attributes = attrs.dwFileAttributes;
                  }


                  if (File.IsReadOnlyOrHidden(attributes))
                  {
                     // MSDN: .NET 3.5+: IOException: The directory specified by path is read-only.

                     if (ignoreReadOnly)
                     {
                        // Reset attributes to Normal.
                        File.SetAttributesCore(transaction, true, pathLp, FileAttributes.Normal, PathFormat.LongFullPath);

                        goto startRemoveDirectory;
                     }


                     // MSDN: .NET 3.5+: IOException: The directory is read-only.
                     throw new DirectoryReadOnlyException(pathLp);
                  }


                  // MSDN: .NET 3.5+: UnauthorizedAccessException: The caller does not have the required permission.
                  if (attributes == 0)
                     NativeError.ThrowException(lastError, File.IsDirectory(attributes), pathLp);

                  break;
            }

            // MSDN: .NET 3.5+: IOException:
            // A file with the same name and location specified by path exists.
            // The directory specified by path is read-only, or recursive is false and path is not an empty directory. 
            // The directory is the application's current working directory. 
            // The directory contains a read-only file.
            // The directory is being used by another process.

            NativeError.ThrowException(lastError, pathLp);
         }
      }
   }
}
