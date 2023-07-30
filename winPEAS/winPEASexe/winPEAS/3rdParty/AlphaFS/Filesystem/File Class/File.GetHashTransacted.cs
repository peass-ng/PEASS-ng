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

using System.Security;
using Alphaleonis.Win32.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>
      /// [AlphaFS] Calculates the hash/checksum for the given <paramref name="fileFullPath"/>.
      /// </summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileFullPath">The path to the file.</param>
      /// <param name="hashType">One of the <see cref="HashType"/> values.</param>
      /// <returns>The hash.</returns>
      [SecurityCritical]
      public static string GetHashTransacted(KernelTransaction transaction, string fileFullPath, HashType hashType)
      {
         return GetHashCore(transaction, fileFullPath, hashType, PathFormat.RelativePath);
      }


      /// <summary>
      /// [AlphaFS] Calculates the hash/checksum for the given <paramref name="fileFullPath"/>.
      /// </summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="fileFullPath">The path to the file.</param>
      /// <param name="hashType">One of the <see cref="HashType"/> values.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>The hash.</returns>
      [SecurityCritical]
      public static string GetHashTransacted(KernelTransaction transaction, string fileFullPath, HashType hashType, PathFormat pathFormat)
      {
         return GetHashCore(transaction, fileFullPath, hashType, pathFormat);
      }
   }
}
