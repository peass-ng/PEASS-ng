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
using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public sealed partial class DirectoryInfo
   {
      /// <summary>[AlphaFS] Decompresses an NTFS compressed directory.</summary>
      /// <remarks>This will only decompress the root items (non recursive).</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      public void Decompress()
      {
         Directory.CompressDecompressCore(Transaction, LongFullName, Path.WildcardStarMatchAll, null, null, false, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Decompresses an NTFS compressed directory.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="options"><see cref="DirectoryEnumerationOptions"/> flags that specify how the directory is to be enumerated.</param>
      [SecurityCritical]
      public void Decompress(DirectoryEnumerationOptions options)
      {
         Directory.CompressDecompressCore(Transaction, LongFullName, Path.WildcardStarMatchAll, options, null, false, PathFormat.LongFullPath);
      }
   }
}
