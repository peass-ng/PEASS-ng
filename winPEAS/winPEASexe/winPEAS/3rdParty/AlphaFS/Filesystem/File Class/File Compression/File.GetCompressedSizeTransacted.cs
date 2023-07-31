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

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>
      ///   [AlphaFS] Retrieves the actual number of bytes of disk storage used to store a specified file as part of a transaction. If the file
      ///   is located on a volume that supports compression and the file is compressed, the value obtained is the compressed size of the
      ///   specified file. If the file is located on a volume that supports sparse files and the file is a sparse file, the value obtained is
      ///   the sparse size of the specified file.
      /// </summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path"><para>The name of the file.</para></param>
      /// <returns>The actual number of bytes of disk storage used to store the specified file.</returns>
      [SecurityCritical]
      public static long GetCompressedSizeTransacted(KernelTransaction transaction, string path)
      {
         return GetCompressedSizeCore(transaction, path, PathFormat.RelativePath);
      }


      /// <summary>
      ///   [AlphaFS] Retrieves the actual number of bytes of disk storage used to store a specified file as part of a transaction. If the file
      ///   is located on a volume that supports compression and the file is compressed, the value obtained is the compressed size of the
      ///   specified file. If the file is located on a volume that supports sparse files and the file is a sparse file, the value obtained is
      ///   the sparse size of the specified file.
      /// </summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path"><para>The name of the file.</para></param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>The actual number of bytes of disk storage used to store the specified file.</returns>
      [SecurityCritical]
      public static long GetCompressedSizeTransacted(KernelTransaction transaction, string path, PathFormat pathFormat)
      {
         return GetCompressedSizeCore(transaction, path, pathFormat);
      }
   }
}
