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

using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      #region .NET

      /// <summary>Opens an existing file or creates a new file for writing.</summary>
      /// <param name="path">The file to be opened for writing.</param>
      /// <returns>An unshared <see cref="FileStream"/> object on the specified path with <see cref="FileAccess.Write"/> access.</returns>
      /// <remarks>This method is equivalent to the <see cref="FileStream"/>(String, FileMode, FileAccess, FileShare) constructor overload with file mode set to OpenOrCreate, the access set to Write, and the share mode set to None.</remarks>
      [SecurityCritical]
      public static FileStream OpenWrite(string path)
      {
         return Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Opens an existing file or creates a new file for writing.</summary>
      /// <param name="path">The file to be opened for writing.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>An unshared <see cref="FileStream"/> object on the specified path with <see cref="FileAccess.Write"/> access.</returns>
      /// <remarks>This method is equivalent to the <see cref="FileStream"/>(String, FileMode, FileAccess, FileShare) constructor overload with file mode set to OpenOrCreate, the access set to Write, and the share mode set to None.</remarks>
      [SecurityCritical]
      public static FileStream OpenWrite(string path, PathFormat pathFormat)
      {
         return Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, pathFormat);
      }
   }
}
