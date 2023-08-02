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
      #region .NET

      /// <summary>Deletes this <see cref="DirectoryInfo"/> if it is empty.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      public override void Delete()
      {
         Directory.DeleteDirectoryCore(Transaction, EntryInfo, null, false, false, false, PathFormat.LongFullPath);
      }


      /// <summary>Deletes this instance of a <see cref="DirectoryInfo"/>, specifying whether to delete subdirectories and files.</summary>
      /// <remarks>
      ///   <para>If the <see cref="DirectoryInfo"/> has no files and no subdirectories, this method deletes the <see cref="DirectoryInfo"/> even if recursive is <c>false</c>.</para>
      ///   <para>Attempting to delete a <see cref="DirectoryInfo"/> that is not empty when recursive is false throws an <see cref="IOException"/>.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="recursive"><c>true</c> to delete this directory, its subdirectories, and all files; otherwise, <c>false</c>.</param>
      [SecurityCritical]
      public void Delete(bool recursive)
      {
         Directory.DeleteDirectoryCore(Transaction, EntryInfo, null, recursive, false, false, PathFormat.LongFullPath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Deletes this instance of a <see cref="DirectoryInfo"/>, specifying whether to delete files and subdirectories.</summary>
      /// <remarks>
      ///   <para>If the <see cref="DirectoryInfo"/> has no files and no subdirectories, this method deletes the <see cref="DirectoryInfo"/> even if recursive is <c>false</c>.</para>
      ///   <para>Attempting to delete a <see cref="DirectoryInfo"/> that is not empty when recursive is false throws an <see cref="IOException"/>.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="recursive"><c>true</c> to delete this directory, its subdirectories, and all files; otherwise, <c>false</c>.</param>
      /// <param name="ignoreReadOnly"><c>true</c> ignores read only attribute of files and directories.</param>
      [SecurityCritical]
      public void Delete(bool recursive, bool ignoreReadOnly)
      {
         Directory.DeleteDirectoryCore(Transaction, EntryInfo, null, recursive, ignoreReadOnly, false, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Deletes this instance of a <see cref="DirectoryInfo"/>, specifying whether to delete files and subdirectories.</summary>
      /// <remarks>
      ///   <para>If the <see cref="DirectoryInfo"/> has no files and no subdirectories, this method deletes the <see cref="DirectoryInfo"/> even if recursive is <c>false</c>.</para>
      ///   <para>Attempting to delete a <see cref="DirectoryInfo"/> that is not empty when recursive is false throws an <see cref="IOException"/>.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="recursive"><c>true</c> to delete this directory, its subdirectories, and all files; otherwise, <c>false</c>.</param>
      /// <param name="ignoreReadOnly"><c>true</c> ignores read only attribute of files and directories.</param>
      /// <param name="continueOnNotFound">When <c>true</c> does not throw an <see cref="DirectoryNotFoundException"/> when the directory does not exist.</param>
      [SecurityCritical]
      public void Delete(bool recursive, bool ignoreReadOnly, bool continueOnNotFound)
      {
         Directory.DeleteDirectoryCore(Transaction, EntryInfo, null, recursive, ignoreReadOnly, continueOnNotFound, PathFormat.LongFullPath);
      }
   }
}
