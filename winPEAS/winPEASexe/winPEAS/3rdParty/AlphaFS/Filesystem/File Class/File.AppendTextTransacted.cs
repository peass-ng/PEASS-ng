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
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] Creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file, or to a new file if the specified file does not exist.</summary>
      /// <returns>A stream writer that appends UTF-8 encoded text to the specified file or to a new file.</returns>
      /// <exception cref="ArgumentException">path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by InvalidPathChars.</exception>
      /// <exception cref="ArgumentNullException">path is null.</exception>
      /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
      /// <exception cref="NotSupportedException">path is in an invalid format.</exception>
      /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file to append to.</param>
      [SecurityCritical]
      public static StreamWriter AppendTextTransacted(KernelTransaction transaction, string path)
      {
         return AppendTextCore(transaction, path, NativeMethods.DefaultFileEncoding, PathFormat.RelativePath);
      }

      
      /// <summary>[AlphaFS] Creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file, or to a new file if the specified file does not exist.</summary>
      /// <returns>A stream writer that appends UTF-8 encoded text to the specified file or to a new file.</returns>
      /// <exception cref="ArgumentException">path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by InvalidPathChars.</exception>
      /// <exception cref="ArgumentNullException">path is null.</exception>
      /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
      /// <exception cref="NotSupportedException">path is in an invalid format.</exception>
      /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file to append to.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static StreamWriter AppendTextTransacted(KernelTransaction transaction, string path, PathFormat pathFormat)
      {
         return AppendTextCore(transaction, path, NativeMethods.DefaultFileEncoding, pathFormat);
      }


      /// <summary>[AlphaFS] Creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file, or to a new file if the specified file does not exist.</summary>
      /// <returns>A stream writer that appends UTF-8 encoded text to the specified file or to a new file.</returns>
      /// <exception cref="ArgumentException">path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by InvalidPathChars.</exception>
      /// <exception cref="ArgumentNullException">path is null.</exception>
      /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
      /// <exception cref="NotSupportedException">path is in an invalid format.</exception>
      /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file to append to.</param>
      /// <param name="encoding">The character <see cref="Encoding"/> to use.</param>
      [SecurityCritical]
      public static StreamWriter AppendTextTransacted(KernelTransaction transaction, string path, Encoding encoding)
      {
         return AppendTextCore(transaction, path, encoding, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates a <see cref="StreamWriter"/> that appends UTF-8 encoded text to an existing file, or to a new file if the specified file does not exist.</summary>
      /// <returns>A stream writer that appends UTF-8 encoded text to the specified file or to a new file.</returns>
      /// <exception cref="ArgumentException">path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by InvalidPathChars.</exception>
      /// <exception cref="ArgumentNullException">path is null.</exception>
      /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, the directory doesn’t exist or it is on an unmapped drive).</exception>
      /// <exception cref="NotSupportedException">path is in an invalid format.</exception>
      /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The path to the file to append to.</param>
      /// <param name="encoding">The character <see cref="Encoding"/> to use.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static StreamWriter AppendTextTransacted(KernelTransaction transaction, string path, Encoding encoding, PathFormat pathFormat)
      {
         return AppendTextCore(transaction, path, encoding, pathFormat);
      }
   }
}
