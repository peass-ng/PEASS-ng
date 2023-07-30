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
using System.IO;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   partial class FileInfo
   {
      /// <summary>Creates a <see cref="StreamReader"/> with NativeMethods.DefaultFileEncoding encoding that reads from an existing text file.</summary>
      /// <returns>A new <see cref="StreamReader"/> with NativeMethods.DefaultFileEncoding encoding.</returns>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public StreamReader OpenText()
      {
         return new StreamReader(File.OpenCore(Transaction, LongFullName, FileMode.Open, FileAccess.Read, FileShare.None, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath), NativeMethods.DefaultFileEncoding);
      }


      /// <summary>[AlphaFS] Creates a <see cref="StreamReader"/> with <see cref="Encoding"/> that reads from an existing text file.</summary>
      /// <returns>A new <see cref="StreamReader"/> with the specified <see cref="Encoding"/>.</returns>
      /// <param name="encoding">The <see cref="Encoding"/> applied to the contents of the file.</param>
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      [SecurityCritical]
      public StreamReader OpenText(Encoding encoding)
      {
         return new StreamReader(File.OpenCore(Transaction, LongFullName, FileMode.Open, FileAccess.Read, FileShare.None, ExtendedFileAttributes.Normal, null, null, PathFormat.LongFullPath), encoding);
      }

   }
}
