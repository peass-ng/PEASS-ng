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

namespace Alphaleonis.Win32.Filesystem
{
   partial class FileInfo
   {
      #region .NET

      /// <summary>Permanently deletes a file.</summary>
      /// <remarks>If the file does not exist, this method does nothing.</remarks>
      /// <exception cref="IOException"/>
      public override void Delete()
      {
         File.DeleteFileCore(Transaction, LongFullName, false, Attributes, PathFormat.LongFullPath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Permanently deletes a file.</summary>
      /// <remarks>If the file does not exist, this method does nothing.</remarks>
      /// <exception cref="IOException"/>
      /// <param name="ignoreReadOnly"><c>true</c> overrides the read only <see cref="FileAttributes"/> of the file.</param>      
      public void Delete(bool ignoreReadOnly)
      {
         File.DeleteFileCore(Transaction, LongFullName, ignoreReadOnly, Attributes, PathFormat.LongFullPath);
      }
   }
}
