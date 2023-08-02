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
 *
 * 
 * Copyright (c) Damien Guard.  All rights reserved.
 * AlphaFS has written permission from the author to include the CRC code.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Alphaleonis.Win32.Security
{
   /// <summary>Implements a 32-bit CRC hash algorithm compatible with Zip etc.</summary>
   /// <remarks>
   ///   Crc32 should only be used for backward compatibility with older file formats and algorithms.
   ///   It is not secure enough for new applications. If you need to call multiple times for the same data
   ///   either use the HashAlgorithm interface or remember that the result of one Compute call needs to be ~ (XOR)
   ///   before being passed in as the seed for the next Compute call.
   /// </remarks>
   [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Crc")]
   internal sealed class Crc32 : HashAlgorithm
   {
      private const uint DefaultPolynomial = 0xedb88320u;
      private const uint DefaultSeed = 0xffffffffu;

      private uint m_hash;
      private readonly uint m_seed;
      private readonly uint[] m_table;
      private static uint[] s_defaultTable;
      

      /// <summary>Initializes a new instance of Crc32.</summary>
      public Crc32() : this(DefaultPolynomial, DefaultSeed)
      {
      }


      /// <summary>Initializes a new instance of Crc32.</summary>
      /// <param name="polynomial">The polynomial.</param>
      /// <param name="seed">The seed.</param>
      private Crc32(uint polynomial, uint seed)
      {
         m_table = InitializeTable(polynomial);
         m_seed = seed;
         m_hash = seed;
      }


      /// <summary>Initializes an implementation of the <see cref="T:System.Security.Cryptography.HashAlgorithm"/> class.</summary>
      public override void Initialize()
      {
         m_hash = m_seed;
      }


      /// <summary>When overridden in a derived class, routes data written to the object into the hash algorithm for computing the hash.</summary>
      /// <param name="array">The input to compute the hash code for..</param>
      /// <param name="ibStart">The offset into the byte array from which to begin using data.</param>
      /// <param name="cbSize">The number of bytes in the byte array to use as data.</param>
      protected override void HashCore(byte[] array, int ibStart, int cbSize)
      {
         m_hash = CalculateHash(m_table, m_hash, array, ibStart, cbSize);
      }


      /// <summary>Finalizes the hash computation after the last data is processed by the cryptographic stream object.</summary>
      /// <returns>This method finalizes any partial computation and returns the correct hash value for the data stream.</returns>
      protected override byte[] HashFinal()
      {
         var hashBuffer = UInt32ToBigEndianBytes(~m_hash);
         HashValue = hashBuffer;
         return hashBuffer;
      }


      /// <summary>Gets the size, in bits, of the computed hash code.</summary>
      /// <value>The size, in bits, of the computed hash code.</value>
      public override int HashSize
      {
         get { return 32; }
      }


      /// <summary>Initializes the table.</summary>
      /// <returns>The table.</returns>
      /// <param name="polynomial">The polynomial.</param>
      private static uint[] InitializeTable(uint polynomial)
      {
         if (polynomial == DefaultPolynomial && s_defaultTable != null)
            return s_defaultTable;

         var createTable = new uint[256];

         for (var i = 0; i < 256; i++)
         {
            var entry = (uint) i;

            for (var j = 0; j < 8; j++)
               entry = (entry & 1) == 1 ? (entry >> 1) ^ polynomial : entry >> 1;

            createTable[i] = entry;
         }

         if (polynomial == DefaultPolynomial)
            s_defaultTable = createTable;

         return createTable;
      }


      /// <summary>Calculates the hash.</summary>
      /// <returns>The calculated hash.</returns>
      /// <param name="table">The table.</param>
      /// <param name="seed">The seed.</param>
      /// <param name="buffer">The buffer.</param>
      /// <param name="start">The start.</param>
      /// <param name="size">The size.</param>
      private static uint CalculateHash(uint[] table, uint seed, IList<byte> buffer, int start, int size)
      {
         var hash = seed;

         for (var i = start; i < start + size; i++)
            hash = (hash >> 8) ^ table[buffer[i] ^ hash & 0xff];

         return hash;
      }


      /// <summary>Int 32 to big endian bytes.</summary>
      /// <returns>A byte[].</returns>
      /// <param name="uint32">The second uint 3.</param>
      private static byte[] UInt32ToBigEndianBytes(uint uint32)
      {
         var result = BitConverter.GetBytes(uint32);

         if (BitConverter.IsLittleEndian)
            Array.Reverse(result);

         return result;
      }
   }
}
