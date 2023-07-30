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
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] Creates a new file, writes the specified byte array to the file, and then closes the file. If the target file already exists, it is overwritten.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to write to.</param>
      /// <param name="bytes">The bytes to write to the file.</param>
      [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "bytes")]
      [SecurityCritical]
      public static void WriteAllBytesTransacted(KernelTransaction transaction, string path, byte[] bytes)
      {
         WriteAllBytesCore(transaction, path, bytes, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates a new file, writes the specified byte array to the file, and then closes the file. If the target file already exists, it is overwritten.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The file to write to.</param>
      /// <param name="bytes">The bytes to write to the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "bytes")]
      [SecurityCritical]
      public static void WriteAllBytesTransacted(KernelTransaction transaction, string path, byte[] bytes, PathFormat pathFormat)
      {
         WriteAllBytesCore(transaction, path, bytes, pathFormat);
      }
   }
}
