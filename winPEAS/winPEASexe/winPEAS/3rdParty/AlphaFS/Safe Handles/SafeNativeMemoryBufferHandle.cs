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

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32
{
   /// <summary>Base class for classes representing a block of unmanaged memory.</summary>
   internal abstract class SafeNativeMemoryBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
   {
      private readonly int m_capacity;


      /// <summary>Initializes a new instance of the <see cref="SafeNativeMemoryBufferHandle"/> class, specifying the allocated capacity of the memory block.</summary>
      /// <param name="callerHandle"><c>true</c> to reliably release the handle during the finalization phase; <c>false</c> to prevent reliable release (not recommended).</param>
      protected SafeNativeMemoryBufferHandle(bool callerHandle) : base(callerHandle)
      {
      }


      /// <summary>Initializes a new instance of the <see cref="SafeNativeMemoryBufferHandle"/> class, specifying the allocated capacity of the memory block.</summary>
      /// <param name="capacity">The capacity.</param>
      protected SafeNativeMemoryBufferHandle(int capacity) : this(true)
      {
         m_capacity = capacity;
      }


      protected SafeNativeMemoryBufferHandle(IntPtr memory, int capacity) : this(capacity)
      {
         SetHandle(memory);
      }


      
      
      /// <summary>Gets the capacity. Only valid if this instance was created using a constructor that specifies the size,
      /// it is not correct if this handle was returned by a native method using p/invoke.
      /// </summary>
      public int Capacity
      {
         get { return m_capacity; }
      }




      /// <summary>Copies data from a one-dimensional, managed 8-bit unsigned integer array to the unmanaged memory pointer referenced by this instance.</summary>
      /// <param name="source">The one-dimensional array to copy from. </param>
      /// <param name="startIndex">The zero-based index into the array where Copy should start.</param>
      /// <param name="length">The number of array elements to copy.</param>
      public void CopyFrom(byte[] source, int startIndex, int length)
      {
         Marshal.Copy(source, startIndex, handle, length);
      }


      public void CopyFrom(char[] source, int startIndex, int length)
      {
         Marshal.Copy(source, startIndex, handle, length);
      }


      public void CopyFrom(char[] source, int startIndex, int length, int offset)
      {
         Marshal.Copy(source, startIndex, new IntPtr(handle.ToInt64() + offset), length);
      }


      /// <summary>Copies data from this unmanaged memory pointer to a managed 8-bit unsigned integer array.</summary>
      /// <param name="sourceOffset">The offset in the buffer to start copying from.</param>
      /// <param name="destination">The array to copy to.</param>
      public void CopyTo(int sourceOffset, byte[] destination)
      {
         if (null == destination || destination.Length == 0)
            throw new ArgumentNullException("destination");

         var length = destination.Length;

         if (length > destination.Length)
            throw new ArgumentException(Resources.Destination_Buffer_Not_Large_Enough, "destination");

         if (length > Capacity)
            throw new ArgumentOutOfRangeException("destination", Resources.Source_OffsetAndLength_Outside_Bounds);

         Marshal.Copy(new IntPtr(handle.ToInt64() + sourceOffset), destination, 0, length);
      }


      /// <summary>Copies data from an unmanaged memory pointer to a managed 8-bit unsigned integer array.</summary>
      /// <param name="destination">The array to copy to.</param>
      /// <param name="destinationOffset">The zero-based index in the destination array where copying should start.</param>
      /// <param name="length">The number of array elements to copy.</param>
      public void CopyTo(byte[] destination, int destinationOffset, int length)
      {
         if (null == destination)
            throw new ArgumentNullException("destination");

         if (destinationOffset < 0)
            throw new ArgumentOutOfRangeException("destinationOffset", Resources.Negative_Destination_Offset);

         if (length < 0)
            throw new ArgumentOutOfRangeException("length", Resources.Negative_Length);

         if (destinationOffset + length > destination.Length)
            throw new ArgumentException(Resources.Destination_Buffer_Not_Large_Enough, "length");

         if (length > Capacity)
            throw new ArgumentOutOfRangeException("length", Resources.Source_OffsetAndLength_Outside_Bounds);

         Marshal.Copy(handle, destination, destinationOffset, length);
      }


      /// <summary>Copies data from this unmanaged memory pointer to a managed 8-bit unsigned integer array.</summary>
      /// <param name="sourceOffset">The offset in the buffer to start copying from.</param>
      /// <param name="destination">The array to copy to.</param>
      /// <param name="destinationOffset">The zero-based index in the destination array where copying should start.</param>
      /// <param name="length">The number of array elements to copy.</param>
      public void CopyTo(int sourceOffset, byte[] destination, int destinationOffset, int length)
      {
         if (null == destination)
            throw new ArgumentNullException("destination");

         if (destinationOffset < 0)
            throw new ArgumentOutOfRangeException("destinationOffset", Resources.Negative_Destination_Offset);

         if (length < 0)
            throw new ArgumentOutOfRangeException("length", Resources.Negative_Length);

         if (destinationOffset + length > destination.Length)
            throw new ArgumentException(Resources.Destination_Buffer_Not_Large_Enough, "length");

         if (length > Capacity)
            throw new ArgumentOutOfRangeException("length", Resources.Source_OffsetAndLength_Outside_Bounds);

         Marshal.Copy(new IntPtr(handle.ToInt64() + sourceOffset), destination, destinationOffset, length);
      }


      public byte[] ToByteArray(int startIndex, int length)
      {
         if (IsInvalid)
            return null;

         var arr = new byte[length];
         Marshal.Copy(handle, arr, startIndex, length);

         return arr;
      }


      #region Write

      public void WriteInt16(int offset, short value)
      {
         Marshal.WriteInt16(handle, offset, value);
      }

      public void WriteInt16(int offset, char value)
      {
         Marshal.WriteInt16(handle, offset, value);
      }

      public void WriteInt16(char value)
      {
         Marshal.WriteInt16(handle, value);
      }

      public void WriteInt16(short value)
      {
         Marshal.WriteInt16(handle, value);
      }

      public void WriteInt32(int offset, short value)
      {
         Marshal.WriteInt32(handle, offset, value);
      }

      public void WriteInt32(int value)
      {
         Marshal.WriteInt32(handle, value);
      }

      public void WriteInt64(int offset, long value)
      {
         Marshal.WriteInt64(handle, offset, value);
      }

      public void WriteInt64(long value)
      {
         Marshal.WriteInt64(handle, value);
      }

      public void WriteByte(int offset, byte value)
      {
         Marshal.WriteByte(handle, offset, value);
      }

      public void WriteByte(byte value)
      {
         Marshal.WriteByte(handle, value);
      }

      public void WriteIntPtr(int offset, IntPtr value)
      {
         Marshal.WriteIntPtr(handle, offset, value);
      }

      public void WriteIntPtr(IntPtr value)
      {
         Marshal.WriteIntPtr(handle, value);
      }

      #endregion // Write


      #region Read

      public byte ReadByte()
      {
         return Marshal.ReadByte(handle);
      }

      public byte ReadByte(int offset)
      {
         return Marshal.ReadByte(handle, offset);
      }

      public short ReadInt16()
      {
         return Marshal.ReadInt16(handle);
      }

      public short ReadInt16(int offset)
      {
         return Marshal.ReadInt16(handle, offset);
      }

      public int ReadInt32()
      {
         return Marshal.ReadInt32(handle);
      }

      public int ReadInt32(int offset)
      {
         return Marshal.ReadInt32(handle, offset);
      }

      public long ReadInt64()
      {
         return Marshal.ReadInt64(handle);
      }

      public long ReadInt64(int offset)
      {
         return Marshal.ReadInt64(handle, offset);
      }

      public IntPtr ReadIntPtr()
      {
         return Marshal.ReadIntPtr(handle);
      }

      public IntPtr ReadIntPtr(int offset)
      {
         return Marshal.ReadIntPtr(handle, offset);
      }

      #endregion // Read



      /// <summary>Marshals data from a managed object to an unmanaged block of memory.</summary>
      public void StructureToPtr(object structure, bool deleteOld)
      {
         Marshal.StructureToPtr(structure, handle, deleteOld);
      }


      /// <summary>Marshals data from an unmanaged block of memory to a newly allocated managed object of the specified type.</summary>
      /// <returns>A managed object containing the data pointed to by the ptr parameter.</returns>
      public T PtrToStructure<T>(int offset)
      {
         return (T) Marshal.PtrToStructure(new IntPtr(handle.ToInt64() + offset), typeof (T));
      }


      /// <summary>Allocates a managed System.String and copies a specified number of characters from an unmanaged ANSI string into it.</summary>
      /// <returns>A managed string that holds a copy of the unmanaged string if the value of the ptr parameter is not null; otherwise, this method returns null.</returns>
      public string PtrToStringAnsi(int offset)
      {
         return Marshal.PtrToStringAnsi(new IntPtr(handle.ToInt64() + offset));
      }

      /// <summary>Allocates a managed System.String and copies all characters up to the first null character from an unmanaged Unicode string into it.</summary>
      /// <returns>A managed string that holds a copy of the unmanaged string if the value of the ptr parameter is not null; otherwise, this method returns null.</returns>
      public string PtrToStringUni()
      {
         return Marshal.PtrToStringUni(handle);
      }


      /// <summary>Allocates a managed System.String and copies a specified number of characters from an unmanaged Unicode string into it.</summary>
      /// <returns>A managed string that holds a copy of the unmanaged string if the value of the ptr parameter is not null; otherwise, this method returns null.</returns>
      public string PtrToStringUni(int offset, int length)
      {
         return Marshal.PtrToStringUni(new IntPtr(handle.ToInt64() + offset), length);
      }
   }
}
