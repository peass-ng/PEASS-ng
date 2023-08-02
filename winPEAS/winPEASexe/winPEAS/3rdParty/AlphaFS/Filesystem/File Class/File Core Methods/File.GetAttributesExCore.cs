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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Gets the <see cref="FileAttributes"/> or <see cref="NativeMethods.WIN32_FILE_ATTRIBUTE_DATA"/> of the specified file or directory.</summary>
      /// <returns>The <see cref="FileAttributes"/> or <see cref="NativeMethods.WIN32_FILE_ATTRIBUTE_DATA"/> of the specified file or directory.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <typeparam name="T">Generic type parameter.</typeparam>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file or directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <param name="returnErrorOnNotFound"></param>
      [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Justification = "Marshal.GetLastWin32Error() is manipulated.")]
      [SecurityCritical]
      internal static T GetAttributesExCore<T>(KernelTransaction transaction, string path, PathFormat pathFormat, bool returnErrorOnNotFound)
      {
         if (pathFormat == PathFormat.RelativePath)
            Path.CheckSupportedPathFormat(path, true, true);

         var pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.CheckInvalidPathChars);

         var data = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA();
         var dataInitialised = FillAttributeInfoCore(transaction, pathLp, ref data, false, returnErrorOnNotFound);

         if (dataInitialised != Win32Errors.ERROR_SUCCESS)
            NativeError.ThrowException(dataInitialised, pathLp);

         return (T) (typeof(T) == typeof(FileAttributes) ? (object) data.dwFileAttributes : data);
      }

      /// <summary>
      ///   Calls NativeMethods.GetFileAttributesEx to retrieve WIN32_FILE_ATTRIBUTE_DATA.
      ///   Note that classes should use -1 as the uninitialized state for dataInitialized when relying on this method.
      /// </summary>
      /// <remarks>No path (null, empty string) checking or normalization is performed.</remarks>
      /// <param name="transaction">.</param>
      /// <param name="pathLp">.</param>
      /// <param name="win32AttrData">[in,out].</param>
      /// <param name="tryAgain">.</param>
      /// <param name="returnErrorOnNotFound">.</param>
      /// <returns>0 on success, otherwise a Win32 error code.</returns>
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      [SecurityCritical]
      internal static int FillAttributeInfoCore(KernelTransaction transaction, string pathLp, ref NativeMethods.WIN32_FILE_ATTRIBUTE_DATA win32AttrData, bool tryAgain, bool returnErrorOnNotFound)
      {
         var lastError = (int) Win32Errors.NO_ERROR;

         #region Try Again

         // Someone has a handle to the file open, or other error.
         if (tryAgain)
         {
            NativeMethods.WIN32_FIND_DATA win32FindData;

            using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
            {
               var handle = FileSystemInfo.FindFirstFileNative(transaction, pathLp, NativeMethods.FindexInfoLevel, NativeMethods.FINDEX_SEARCH_OPS.SearchNameMatch, NativeMethods.UseLargeCache, out lastError, out win32FindData);

               if (null == handle)
               {
                  switch ((uint) lastError)
                  {
                     case Win32Errors.ERROR_INVALID_NAME:
                     case Win32Errors.ERROR_FILE_NOT_FOUND: // On files.
                     case Win32Errors.ERROR_PATH_NOT_FOUND: // On folders.
                     case Win32Errors.ERROR_NOT_READY:      // DeviceNotReadyException: Floppy device or network drive not ready.

                        if (!returnErrorOnNotFound)
                        {
                           // Return default value for backward compatibility.
                           lastError = (int) Win32Errors.NO_ERROR;

                           win32AttrData.dwFileAttributes = NativeMethods.InvalidFileAttributes;
                        }

                        break;
                  }

                  return lastError;
               }
            }

            // Copy the attribute information.
            win32AttrData = new NativeMethods.WIN32_FILE_ATTRIBUTE_DATA(win32FindData);
         }

         #endregion // Try Again

         else
         {
            using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
            {
               if (!(null == transaction || !NativeMethods.IsAtLeastWindowsVista

                  // GetFileAttributesEx() / GetFileAttributesTransacted()
                  // 2013-01-13: MSDN confirms LongPath usage.

                  ? NativeMethods.GetFileAttributesEx(pathLp, NativeMethods.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out win32AttrData)

                  : NativeMethods.GetFileAttributesTransacted(pathLp, NativeMethods.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out win32AttrData, transaction.SafeHandle)))
               {
                  lastError = Marshal.GetLastWin32Error();

                  switch ((uint) lastError)
                  {
                     case Win32Errors.ERROR_FILE_NOT_FOUND: // On files.
                     case Win32Errors.ERROR_PATH_NOT_FOUND: // On folders.
                     case Win32Errors.ERROR_NOT_READY:      // DeviceNotReadyException: Floppy device or network drive not ready.

                        // In case someone latched onto the file. Take the perf hit only for failure.
                        return FillAttributeInfoCore(transaction, pathLp, ref win32AttrData, true, returnErrorOnNotFound);
                  }


                  if (!returnErrorOnNotFound)
                  {
                     // Return default value for backward compatibility.
                     lastError = (int) Win32Errors.NO_ERROR;

                     win32AttrData.dwFileAttributes = NativeMethods.InvalidFileAttributes;
                  }
               }
            }
         }

         return lastError;
      }
   }
}
