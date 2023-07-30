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
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "lastError")]
      [SecurityCritical]
      internal static SafeFindFileHandle FindFirstStreamNative(KernelTransaction transaction, string pathLp, SafeGlobalMemoryBufferHandle buffer)
      {
         var safeFindFileHandle = null == transaction

            // FindFirstStreamW() / FindFirstStreamTransactedW()
            // 2018-01-15: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

            ? NativeMethods.FindFirstStreamW(pathLp, NativeMethods.STREAM_INFO_LEVELS.FindStreamInfoStandard, buffer, 0)
            : NativeMethods.FindFirstStreamTransactedW(pathLp, NativeMethods.STREAM_INFO_LEVELS.FindStreamInfoStandard, buffer, 0, transaction.SafeHandle);

         var lastError = Marshal.GetLastWin32Error();

         return NativeMethods.IsValidHandle(safeFindFileHandle, false) ? safeFindFileHandle : null;
      }
   }
}
