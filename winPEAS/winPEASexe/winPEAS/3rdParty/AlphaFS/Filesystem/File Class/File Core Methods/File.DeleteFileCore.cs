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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Deletes a Non-/Transacted file.</summary>
      /// <remarks>If the file to be deleted does not exist, no exception is thrown.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <exception cref="FileReadOnlyException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The name of the file to be deleted.</param>
      /// <param name="ignoreReadOnly"><c>true</c> overrides the read only <see cref="FileAttributes"/> of the file.</param>
      /// <param name="attributes"></param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void DeleteFileCore(KernelTransaction transaction, string path, bool ignoreReadOnly, FileAttributes attributes, PathFormat pathFormat)
      {
         if (null == path)
            throw new ArgumentNullException("path");

         if (pathFormat == PathFormat.RelativePath)
            Path.CheckSupportedPathFormat(path, true, true);

         var pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.TrimEnd | GetFullPathOptions.RemoveTrailingDirectorySeparator);


         // Reset attributes to Normal if we already know the facts.

         if (ignoreReadOnly && IsReadOnlyOrHidden(attributes))

            SetAttributesCore(transaction, false, pathLp, FileAttributes.Normal, PathFormat.LongFullPath);


      startDeleteFile:

         if (!(null == transaction || !NativeMethods.IsAtLeastWindowsVista

            // DeleteFile() / DeleteFileTransacted()
            // 2013-01-13: MSDN confirms LongPath usage.
            //
            // If the path points to a symbolic link, the symbolic link is deleted, not the target.

            ? NativeMethods.DeleteFile(pathLp)

            : NativeMethods.DeleteFileTransacted(pathLp, transaction.SafeHandle)))
         {
            var lastError = Marshal.GetLastWin32Error();


            switch ((uint) lastError)
            {
               case Win32Errors.ERROR_FILE_NOT_FOUND:
                  // MSDN: .NET 3.5+: If the file to be deleted does not exist, no exception is thrown.
                  return;


               case Win32Errors.ERROR_PATH_NOT_FOUND:
                  // MSDN: .NET 3.5+: DirectoryNotFoundException: The specified path is invalid (for example, it is on an unmapped drive).
                  NativeError.ThrowException(lastError, pathLp);
                  return;


               case Win32Errors.ERROR_SHARING_VIOLATION:
                  // MSDN: .NET 3.5+: IOException: The specified file is in use or there is an open handle on the file.
                  NativeError.ThrowException(lastError, pathLp);
                  break;


               case Win32Errors.ERROR_ACCESS_DENIED:

                  if (attributes == 0)
                  {
                     var attrs = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();

                     if (FillAttributeInfoCore(transaction, pathLp, ref attrs, false, true) == Win32Errors.NO_ERROR)

                        attributes = attrs.dwFileAttributes;
                  }


                  // MSDN: .NET 3.5+: UnauthorizedAccessException: Path is a directory.
                  if (IsDirectory(attributes))
                     throw new UnauthorizedAccessException(string.Format(CultureInfo.InvariantCulture, "({0}) {1}", lastError.ToString(CultureInfo.InvariantCulture), string.Format(CultureInfo.InvariantCulture, Resources.Target_File_Is_A_Directory, pathLp)));


                  if (IsReadOnlyOrHidden(attributes))
                  {
                     if (ignoreReadOnly)
                     {
                        // Reset attributes to Normal.
                        SetAttributesCore(transaction, false, pathLp, FileAttributes.Normal, PathFormat.LongFullPath);

                        goto startDeleteFile;
                     }


                     // MSDN: .NET 3.5+: UnauthorizedAccessException: Path specified a read-only file.
                     throw new FileReadOnlyException(pathLp);
                  }

                  
                  // MSDN: .NET 3.5+: UnauthorizedAccessException: The caller does not have the required permission.
                  if (attributes == 0)
                     NativeError.ThrowException(lastError, pathLp);

                  break;
            }

            // MSDN: .NET 3.5+: IOException:
            // The specified file is in use.
            // There is an open handle on the file, and the operating system is Windows XP or earlier.

            NativeError.ThrowException(lastError, IsDirectory(attributes), pathLp);
         }
      }
   }
}
