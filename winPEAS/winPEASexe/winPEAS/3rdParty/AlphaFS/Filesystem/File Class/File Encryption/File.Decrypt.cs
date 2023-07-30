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
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Decrypts a file that was encrypted by the current account using the Encrypt method.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryReadOnlyException"/>
      /// <exception cref="FileReadOnlyException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">A path that describes a file to decrypt.</param>
      [SecurityCritical]
      public static void Decrypt(string path)
      {
         EncryptDecryptFileCore(false, path, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Decrypts a file that was encrypted by the current account using the Encrypt method.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryReadOnlyException"/>
      /// <exception cref="FileReadOnlyException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">A path that describes a file to decrypt.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void Decrypt(string path, PathFormat pathFormat)
      {
         EncryptDecryptFileCore(false, path, false, pathFormat);
      }
   }
}
