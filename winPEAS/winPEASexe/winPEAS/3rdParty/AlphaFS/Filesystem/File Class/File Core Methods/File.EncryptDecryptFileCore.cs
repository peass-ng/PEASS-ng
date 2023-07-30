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

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Decrypts/encrypts a file or directory so that only the account used to encrypt the file can decrypt it.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryReadOnlyException"/>
      /// <exception cref="FileReadOnlyException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="isFolder">Specifies that <paramref name="path"/> is a file or directory.</param>
      /// <param name="path">A path that describes a file to encrypt.</param>
      /// <param name="encrypt"><c>true</c> encrypt, <c>false</c> decrypt.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void EncryptDecryptFileCore(bool isFolder, string path, bool encrypt, PathFormat pathFormat)
      {
         if (pathFormat != PathFormat.LongFullPath)
         {
            path = Path.GetExtendedLengthPathCore(null, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

            pathFormat = PathFormat.LongFullPath;
         }


         // MSDN: If lpFileName specifies a read-only file, the function fails and GetLastError returns ERROR_FILE_READ_ONLY.

         var attrs = GetAttributesExCore<NativeMethods.WIN32_FILE_ATTRIBUTE_DATA>(null, path, pathFormat, true);

         var isReadOnly = IsReadOnly(attrs.dwFileAttributes);
         var isHidden = IsHidden(attrs.dwFileAttributes);

         if (isReadOnly || isHidden)
         {
            if (isReadOnly)
               attrs.dwFileAttributes &= ~FileAttributes.ReadOnly;

            if (isHidden)
               attrs.dwFileAttributes &= ~FileAttributes.Hidden;

            SetAttributesCore(null, isFolder, path, attrs.dwFileAttributes, pathFormat);
         }


         // EncryptFile() / DecryptFile()
         // 2013-01-13: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

         var success = encrypt ? NativeMethods.EncryptFile(path) : NativeMethods.DecryptFile(path, 0);

         var lastError = Marshal.GetLastWin32Error();


         if (isReadOnly || isHidden)
         {
            if (isReadOnly)
               attrs.dwFileAttributes |= FileAttributes.ReadOnly;

            if (isHidden)
               attrs.dwFileAttributes |= FileAttributes.Hidden;

            SetAttributesCore(null, isFolder, path, attrs.dwFileAttributes, pathFormat);
         }


         if (!success)
         {
            switch ((uint) lastError)
            {
               case Win32Errors.ERROR_ACCESS_DENIED:

                  if (!string.Equals("NTFS", new DriveInfo(path).DriveFormat, StringComparison.OrdinalIgnoreCase))

                     throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "The drive does not support NTFS encryption: [{0}]", Path.GetPathRoot(path, false)));

                  break;


               case Win32Errors.ERROR_FILE_READ_ONLY:

                  if (isFolder)
                     throw new DirectoryReadOnlyException(path);

                  else
                     throw new FileReadOnlyException(path);


               default:
                  NativeError.ThrowException(lastError, isFolder, path);
                  break;
            }
         }
      }
   }
}
