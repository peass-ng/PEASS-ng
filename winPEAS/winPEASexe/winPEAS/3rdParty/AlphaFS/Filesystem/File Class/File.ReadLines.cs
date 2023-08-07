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

using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      #region .NET

      /// <summary>Reads the lines of a file.</summary>
      /// <param name="path">The file to read.</param>
      /// <returns>All the lines of the file, or the lines that are the result of a query.</returns>
      [SecurityCritical]
      public static IEnumerable<string> ReadLines(string path)
      {
         return ReadLinesCore(null, path, NativeMethods.DefaultFileEncoding, PathFormat.RelativePath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Reads the lines of a file.</summary>
      /// <param name="path">The file to read.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>All the lines of the file, or the lines that are the result of a query.</returns>
      [SecurityCritical]
      public static IEnumerable<string> ReadLines(string path, PathFormat pathFormat)
      {
         return ReadLinesCore(null, path, NativeMethods.DefaultFileEncoding, pathFormat);
      }


      /// <summary>Read the lines of a file that has a specified encoding.</summary>
      /// <param name="path">The file to read.</param>
      /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
      /// <returns>All the lines of the file, or the lines that are the result of a query.</returns>
      [SecurityCritical]
      public static IEnumerable<string> ReadLines(string path, Encoding encoding)
      {
         return ReadLinesCore(null, path, encoding, PathFormat.RelativePath);
      }
      

      /// <summary>[AlphaFS] Read the lines of a file that has a specified encoding.</summary>
      /// <param name="path">The file to read.</param>
      /// <param name="encoding">The encoding that is applied to the contents of the file.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>All the lines of the file, or the lines that are the result of a query.</returns>
      [SecurityCritical]
      public static IEnumerable<string> ReadLines(string path, Encoding encoding, PathFormat pathFormat)
      {
         return ReadLinesCore(null, path, encoding, pathFormat);
      }
   }
}
