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
using System.Security;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>Deletes an NTFS directory junction.</summary>
      /// <remarks>Only the directory junction is removed, not the target.</remarks>
      /// <returns>A <see cref="DirectoryInfo"/> instance referencing the junction point.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fsEntryInfo">A FileSystemEntryInfo instance. Use either <paramref name="fsEntryInfo"/> or <paramref name="junctionPath"/>, not both.</param>
      /// <param name="junctionPath">The path of the junction point to remove.</param>
      /// <param name="removeDirectory">When <c>true</c>, also removes the directory and all its contents.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void DeleteJunctionCore(KernelTransaction transaction, FileSystemEntryInfo fsEntryInfo, string junctionPath, bool removeDirectory, PathFormat pathFormat)
      {
         if (null == fsEntryInfo)
         {
            if (pathFormat != PathFormat.LongFullPath)
            {
               Path.CheckSupportedPathFormat(junctionPath, true, true);

               junctionPath = Path.GetExtendedLengthPathCore(transaction, junctionPath, pathFormat, GetFullPathOptions.CheckInvalidPathChars | GetFullPathOptions.RemoveTrailingDirectorySeparator);

               pathFormat = PathFormat.LongFullPath;
            }


            fsEntryInfo = File.GetFileSystemEntryInfoCore(transaction, true, junctionPath, false, pathFormat);

            if (!fsEntryInfo.IsMountPoint)
               throw new NotAReparsePointException(string.Format(CultureInfo.InvariantCulture, Resources.Directory_Is_Not_A_MountPoint, fsEntryInfo.LongFullPath), (int) Win32Errors.ERROR_NOT_A_REPARSE_POINT);
         }
         

         pathFormat = PathFormat.LongFullPath;


         // Remove the directory junction.

         using (var safeHandle = OpenDirectoryJunction(transaction, fsEntryInfo.LongFullPath, pathFormat))

            Device.DeleteDirectoryJunction(safeHandle);


         // Optionally the folder itself, which should and must be empty.

         if (removeDirectory)

            DeleteDirectoryCore(transaction, fsEntryInfo, null, false, false, true, pathFormat);
      }
   }
}
