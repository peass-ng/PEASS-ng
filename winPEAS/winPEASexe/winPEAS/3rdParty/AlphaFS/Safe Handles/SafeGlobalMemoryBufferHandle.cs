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
using System.Runtime.InteropServices;
using System.Text;

namespace Alphaleonis.Win32
{
   /// <summary>Represents a block of native memory of a specified size allocated using the LocalAlloc function from Kernel32.dll.</summary>
   internal sealed class SafeGlobalMemoryBufferHandle : SafeNativeMemoryBufferHandle
   {
      /// <summary>Initializes a new instance of the <see cref="SafeGlobalMemoryBufferHandle"/> class, with zero IntPtr.</summary>
      public SafeGlobalMemoryBufferHandle() : base(true)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="SafeGlobalMemoryBufferHandle"/> class allocating the specified number of bytes of unmanaged memory.</summary>
      /// <param name="capacity">The capacity.</param>
      public SafeGlobalMemoryBufferHandle(int capacity) : base(capacity)
      {
         SetHandle(Marshal.AllocHGlobal(capacity));
      }


      private SafeGlobalMemoryBufferHandle(IntPtr buffer, int capacity) : base(buffer, capacity)
      {
      }


      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      public static SafeGlobalMemoryBufferHandle FromLong(long? value)
      {
         if (value.HasValue)
         {
            var safeBuffer = new SafeGlobalMemoryBufferHandle(Marshal.SizeOf(typeof(long)));

            Marshal.WriteInt64(safeBuffer.handle, value.Value);

            return safeBuffer;
         }

         return new SafeGlobalMemoryBufferHandle();
      }


      public static SafeGlobalMemoryBufferHandle FromStringUni(string str)
      {
         if (str == null)
            throw new ArgumentNullException("str");

         return new SafeGlobalMemoryBufferHandle(Marshal.StringToHGlobalUni(str), str.Length * UnicodeEncoding.CharSize + UnicodeEncoding.CharSize);
      }


      /// <summary>When overridden in a derived class, executes the code required to free the handle.</summary>
      /// <returns>
      /// <c>true</c> if the handle is released successfully; otherwise, in the event of a catastrophic failure,
      /// <c>false</c>. In this case, it generates a ReleaseHandleFailed Managed Debugging Assistant.
      /// </returns>
      protected override bool ReleaseHandle()
      {
         Marshal.FreeHGlobal(handle);
         return true;
      }
   }
}
