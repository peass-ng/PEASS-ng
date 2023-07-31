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
   /// <summary>Implements an ISO-3309 compliant 64-bit CRC hash algorithm.</summary>
   [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Crc")]
   internal class Crc64 : HashAlgorithm
   {
      private const ulong Iso3309Polynomial = 0xD800000000000000;
      private const ulong DefaultSeed = 0x0;


      private ulong m_hash;
      private readonly ulong m_seed;
      private readonly ulong[] m_table;
      private static ulong[] s_defaultTable;


      /// <summary>Initializes a new instance of <see cref="Crc64"/> </summary>
      public Crc64() : this(Iso3309Polynomial, DefaultSeed)
      {
      }


      /// <summary>Initializes a new instance of <see cref="Crc64"/>.</summary>
      /// <param name="polynomial">The polynomial.</param>
      /// <param name="seed">The seed.</param>
      private Crc64(ulong polynomial, ulong seed)
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
         m_hash = CalculateHash(m_hash, m_table, array, ibStart, cbSize);
      }


      /// <summary>Finalizes the hash computation after the last data is processed by the cryptographic stream object.</summary>
      /// <returns>This method finalizes any partial computation and returns the correct hash value for the data stream.</returns>
      protected override byte[] HashFinal()
      {
         var hashBuffer = UInt64ToBigEndianBytes(m_hash);
         HashValue = hashBuffer;
         return hashBuffer;
      }

      /// <summary>Gets the size, in bits, of the computed hash code.</summary>
      /// <value>The size, in bits, of the computed hash code.</value>
      public override int HashSize
      {
         get { return 64; }
      }


      /// <summary>Initializes the table.</summary>
      /// <returns>The table.</returns>
      /// <param name="polynomial">The polynomial.</param>
      private static ulong[] InitializeTable(ulong polynomial)
      {
         if (polynomial == Iso3309Polynomial && s_defaultTable != null)
            return s_defaultTable;

         var createTable = CreateTable(polynomial);

         if (polynomial == Iso3309Polynomial)
            s_defaultTable = createTable;

         return createTable;
      }


      /// <summary>Creates a table.</summary>
      /// <returns>A new array of ulong.</returns>
      /// <param name="polynomial">The polynomial.</param>
      private static ulong[] CreateTable(ulong polynomial)
      {
         var createTable = new ulong[256];

         for (var i = 0; i < 256; ++i)
         {
            var entry = (ulong)i;

            for (var j = 0; j < 8; ++j)
               entry = (entry & 1) == 1 ? (entry >> 1) ^ polynomial : entry >> 1;

            createTable[i] = entry;
         }

         return createTable;
      }


      /// <summary>Calculates the hash.</summary>
      /// <returns>The calculated hash.</returns>
      /// <param name="seed">The seed.</param>
      /// <param name="table">The table.</param>
      /// <param name="buffer">The buffer.</param>
      /// <param name="start">The start.</param>
      /// <param name="size">The size.</param>
      private static ulong CalculateHash(ulong seed, ulong[] table, IList<byte> buffer, int start, int size)
      {
         var hash = seed;

         for (var i = start; i < start + size; i++)
            unchecked
            {
               hash = (hash >> 8) ^ table[(buffer[i] ^ hash) & 0xff];
            }

         return hash;
      }


      /// <summary>Int 64 to big endian bytes.</summary>
      /// <returns>A byte[].</returns>
      /// <param name="value">The value.</param>
      private static byte[] UInt64ToBigEndianBytes(ulong value)
      {
         var result = BitConverter.GetBytes(value);

         if (BitConverter.IsLittleEndian)
            Array.Reverse(result);

         return result;
      }
   }
}
