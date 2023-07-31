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

using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Alphaleonis.Win32.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Calculates the hash/checksum for the given <paramref name="fileFullPath"/>.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileFullPath">The path to the file.</param>
      /// <param name="hashType">One of the <see cref="HashType"/> values.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>The hash core.</returns>
      [SecurityCritical]
      internal static string GetHashCore(KernelTransaction transaction, string fileFullPath, HashType hashType, PathFormat pathFormat)
      {
         const GetFullPathOptions options = GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck;

         var fileNameLp = Path.GetExtendedLengthPathCore(transaction, fileFullPath, pathFormat, options);

         byte[] hash = null;


         using (var fs = OpenCore(transaction, fileNameLp, FileMode.Open, FileAccess.Read, FileShare.Read, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath))
         {
            switch (hashType)
            {
               case HashType.CRC32:
                  using (var hType = new Crc32())
                     hash = hType.ComputeHash(fs);
                  break;


               case HashType.CRC64ISO3309:
                  using (var hType = new Crc64())
                     hash = hType.ComputeHash(fs);
                  break;


               case HashType.MD5:
                  using (var hType = MD5.Create())
                     hash = hType.ComputeHash(fs);
                  break;

#if !NETSTANDARD20
               case HashType.RIPEMD160:
                  using (var hType = RIPEMD160.Create())
                     hash = hType.ComputeHash(fs);
                  break;
#endif

               case HashType.SHA1:
                  using (var hType = SHA1.Create())
                     hash = hType.ComputeHash(fs);
                  break;


               case HashType.SHA256:
                  using (var hType = SHA256.Create())
                     hash = hType.ComputeHash(fs);
                  break;


               case HashType.SHA384:
                  using (var hType = SHA384.Create())
                     hash = hType.ComputeHash(fs);
                  break;


               case HashType.SHA512:
                  using (var hType = SHA512.Create())
                     hash = hType.ComputeHash(fs);
                  break;
            }
         }


         if (null != hash)
         {
            var sb = new StringBuilder(hash.Length);

            foreach (var b in hash)
               sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));

            return sb.ToString().ToUpperInvariant();
         }

         return string.Empty;
      }
   }
}
