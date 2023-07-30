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
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] Creates an enumeration of all the hard links to the specified <paramref name="path"/>.</summary>
      /// <returns>An enumerable collection of <see cref="string"/> of all the hard links to the specified <paramref name="path"/></returns>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The name of the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      internal static IEnumerable<string> EnumerateHardLinksCore(KernelTransaction transaction, string path, PathFormat pathFormat)
      {
         if (!NativeMethods.IsAtLeastWindowsVista)
            throw new PlatformNotSupportedException(new Win32Exception((int) Win32Errors.ERROR_OLD_WIN_VERSION).Message);

         var pathLp = Path.GetExtendedLengthPathCore(transaction, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

         // Default buffer length, will be extended if needed, although this should not happen.
         uint length = NativeMethods.MaxPathUnicode;
         var builder = new StringBuilder((int) length);


      getFindFirstFileName:

         using (var safeHandle = null == transaction

            // FindFirstFileNameW() / FindFirstFileNameTransactedW() / FindNextFileNameW()
            // 2013-01-13: MSDN does not confirm LongPath usage but a Unicode version of this function exists.
            // 2017-05-30: FindFirstFileNameW() MSDN confirms LongPath usage: Starting with Windows 10, version 1607

            ? NativeMethods.FindFirstFileNameW(pathLp, 0, out length, builder)

            : NativeMethods.FindFirstFileNameTransactedW(pathLp, 0, out length, builder, transaction.SafeHandle))
         {
            var lastError = Marshal.GetLastWin32Error();

            if (!NativeMethods.IsValidHandle(safeHandle, false))
            {
               switch ((uint) lastError)
               {
                  case Win32Errors.ERROR_MORE_DATA:
                     builder = new StringBuilder((int) length);
                     goto getFindFirstFileName;

                  default:
                     // If the function fails, the return value is INVALID_HANDLE_VALUE.
                     NativeError.ThrowException(lastError, pathLp);
                     break;
               }
            }

            yield return builder.ToString();


            do
            {
               while (!NativeMethods.FindNextFileNameW(safeHandle, out length, builder))
               {
                  lastError = Marshal.GetLastWin32Error();

                  switch ((uint) lastError)
                  {
                     // We've reached the end of the enumeration.
                     case Win32Errors.ERROR_HANDLE_EOF:
                        yield break;

                     case Win32Errors.ERROR_MORE_DATA:
                        builder = new StringBuilder((int) length);
                        continue;

                     default:
                        // If the function fails, the return value is zero (0).
                        NativeError.ThrowException(lastError);
                        break;
                  }
               }


               yield return builder.ToString();

            } while (true);
         }
      }
   }
}
