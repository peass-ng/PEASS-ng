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

using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      [SecurityCritical]
      internal static long FindAllStreamsCore(KernelTransaction transaction, string pathLp)
      {
         var streamSizes = new Collection<long>();

         using (var buffer = new SafeGlobalMemoryBufferHandle(Marshal.SizeOf(typeof(NativeMethods.WIN32_FIND_STREAM_DATA))))
         using (var safeFindFileHandle = FindFirstStreamNative(transaction, pathLp, buffer))
         {
            if (null != safeFindFileHandle)
               while (true)
               {
                  streamSizes.Add(buffer.PtrToStructure<NativeMethods.WIN32_FIND_STREAM_DATA>(0).StreamSize);

                  var success = NativeMethods.FindNextStreamW(safeFindFileHandle, buffer);

                  var lastError = Marshal.GetLastWin32Error();

                  if (!success)
                  {
                     if (lastError == Win32Errors.ERROR_HANDLE_EOF)
                        break;

                     NativeError.ThrowException(lastError, pathLp);
                  }
               }
         }
         
         return streamSizes.Sum();
      }
   }
}
