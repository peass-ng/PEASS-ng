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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Returns an enumerable collection of <see cref="String"/> volumes on the computer.</summary>
      /// <returns>An enumerable collection of <see cref="String"/> volume names on the computer.</returns>
      [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
      [SecurityCritical]
      public static IEnumerable<string> EnumerateVolumes()
      {
         var buffer = new StringBuilder(NativeMethods.MaxPathUnicode);

         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
         using (var handle = NativeMethods.FindFirstVolume(buffer, (uint)buffer.Capacity))
         {
            var lastError = Marshal.GetLastWin32Error();

            var throwException = lastError != Win32Errors.ERROR_NO_MORE_FILES && lastError != Win32Errors.ERROR_PATH_NOT_FOUND;

            if (!NativeMethods.IsValidHandle(handle, lastError, String.Empty, throwException))
               yield break;

            yield return buffer.ToString();


            while (NativeMethods.FindNextVolume(handle, buffer, (uint)buffer.Capacity))
            {
               lastError = Marshal.GetLastWin32Error();

               throwException = lastError != Win32Errors.ERROR_NO_MORE_FILES && lastError != Win32Errors.ERROR_PATH_NOT_FOUND && lastError != Win32Errors.ERROR_MORE_DATA;

               if (!NativeMethods.IsValidHandle(handle, lastError, String.Empty, throwException))
                  yield break;

               yield return buffer.ToString();
            }
         }
      }
   }
}
