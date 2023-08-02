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
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Deletes a Drive letter or mounted folder.</summary>
      /// <remarks>Deleting a mounted folder does not cause the underlying directory to be deleted.</remarks>
      /// <remarks>
      ///   If the <paramref name="volumeMountPoint"/> parameter is a directory that is not a mounted folder, the function does nothing. The
      ///   directory is not deleted.
      /// </remarks>
      /// <remarks>
      ///   It's not an error to attempt to unmount a volume from a volume mount point when there is no volume actually mounted at that volume
      ///   mount point.
      /// </remarks>
      /// <param name="volumeMountPoint">The Drive letter or mounted folder to be deleted. For example, X:\ or Y:\MountX\.</param>      
      [SecurityCritical]
      public static void DeleteVolumeMountPoint(string volumeMountPoint)
      {
         DeleteVolumeMountPointCore(null, volumeMountPoint, false, false, PathFormat.RelativePath);
      }




      /// <summary>Deletes a Drive letter or mounted folder.
      /// <remarks>
      ///   <para>It's not an error to attempt to unmount a volume from a volume mount point when there is no volume actually mounted at that volume mount point.</para>
      ///   <para>Deleting a mounted folder does not cause the underlying directory to be deleted.</para>
      /// </remarks>
      /// </summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="volumeMountPoint">The Drive letter or mounted folder to be deleted. For example, X:\ or Y:\MountX\.</param>
      /// <param name="continueOnException"><c>true</c> suppress any Exception that might be thrown as a result from a failure, such as unavailable resources.</param>
      /// <param name="continueIfJunction"><c>true</c> suppress an exception due to this mount point being a Junction.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void DeleteVolumeMountPointCore(KernelTransaction transaction, string volumeMountPoint, bool continueOnException, bool continueIfJunction, PathFormat pathFormat)
      {
         if (pathFormat != PathFormat.LongFullPath)
            Path.CheckSupportedPathFormat(volumeMountPoint, true, true);

         volumeMountPoint = Path.GetExtendedLengthPathCore(transaction, volumeMountPoint, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator);


         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
         {
            // DeleteVolumeMountPoint()
            // 2013-01-13: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

            // A trailing backslash is required.
            var success = NativeMethods.DeleteVolumeMountPoint(Path.AddTrailingDirectorySeparator(volumeMountPoint, false));

            var lastError = Marshal.GetLastWin32Error();
            if (!success && !continueOnException)
            {
               if (lastError == Win32Errors.ERROR_INVALID_PARAMETER && continueIfJunction)
                  return;

               if (lastError == Win32Errors.ERROR_FILE_NOT_FOUND)
                  lastError = (int)Win32Errors.ERROR_PATH_NOT_FOUND;

               NativeError.ThrowException(lastError, volumeMountPoint);
            }
         }
      }
   }
}
