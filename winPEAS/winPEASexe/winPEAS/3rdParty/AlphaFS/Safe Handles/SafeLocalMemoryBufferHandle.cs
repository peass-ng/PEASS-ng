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
using Microsoft.Win32.SafeHandles;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Security
{
   /// <summary>An IntPtr wrapper which can be used as the result of a Marshal.AllocHGlobal operation.
   /// <para>Calls Marshal.FreeHGlobal when disposed or finalized.</para>
   /// </summary>
   internal sealed class SafeLocalMemoryBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
   {
      /// <summary>Initializes a new instance of the <see cref="SafeLocalMemoryBufferHandle"/> class, with zero IntPtr.</summary>
      public SafeLocalMemoryBufferHandle() : base(true)
      {
      }


      /// <summary>Copies data from a one-dimensional, managed 8-bit unsigned integer array to the unmanaged memory pointer referenced by this instance.</summary>
      /// <param name="source">The one-dimensional array to copy from.</param>
      /// <param name="startIndex">The zero-based index into the array where Copy should start.</param>
      /// <param name="length">The number of array elements to copy.</param>      
      public void CopyFrom(byte[] source, int startIndex, int length)
      {
         Marshal.Copy(source, startIndex, handle, length);
      }
      
      
      public void CopyTo(byte[] destination, int destinationOffset, int length)
      {
         if (destination == null)
            throw new ArgumentNullException("destination");

         if (destinationOffset < 0)
            throw new ArgumentOutOfRangeException("destinationOffset", Resources.Negative_Destination_Offset);

         if (length < 0)
            throw new ArgumentOutOfRangeException("length", Resources.Negative_Length);

         if (destinationOffset + length > destination.Length)
            throw new ArgumentException(Resources.Destination_Buffer_Not_Large_Enough, "length");

         Marshal.Copy(handle, destination, destinationOffset, length);
      }


      public byte[] ToByteArray(int startIndex, int length)
      {
         if (IsInvalid)
            return null;

         var arr = new byte[length];
         Marshal.Copy(handle, arr, startIndex, length);
         return arr;
      }


      /// <summary>When overridden in a derived class, executes the code required to free the handle.</summary>
      /// <returns>
      /// <c>true</c> if the handle is released successfully; otherwise, in the event of a catastrophic failure,
      /// <c>false</c>. In this case, it generates a ReleaseHandleFailed Managed Debugging Assistant.
      /// </returns>
      protected override bool ReleaseHandle()
      {
         return handle == IntPtr.Zero || NativeMethods.LocalFree(handle) == IntPtr.Zero;
      }
   }
}
