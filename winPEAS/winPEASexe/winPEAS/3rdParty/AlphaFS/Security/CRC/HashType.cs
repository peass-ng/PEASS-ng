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

namespace Alphaleonis.Win32.Security
{
   /// <summary>Enum containing the supported hash types.</summary>
   public enum HashType
   {
      /// <summary>CRC-32 (Cyclic Redundancy Check)</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CRC")]
      CRC32,

      /// <summary>CRC-64 ISO-3309 compliant.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CRC")]
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ISO")]
      CRC64ISO3309,

      /// <summary>MD5 (Message digest)</summary>
      MD5,

      /// <summary>RIPEMD-160 is a 160-bit cryptographic hash function. It is intended for use as a replacement for the 128-bit hash functions MD4, MD5, and RIPEMD.</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "RIPEMD")]
      RIPEMD160,

      /// <summary>SHA-1 (Secure Hash Algorithm)</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SHA")]
      SHA1,

      /// <summary>SHA-256 (Secure Hash Algorithm)</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SHA")]
      SHA256,

      /// <summary>SHA-384 (Secure Hash Algorithm)</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SHA")]
      SHA384,

      /// <summary>SHA-512 (Secure Hash Algorithm)</summary>
      [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SHA")]
      SHA512
   }
}
