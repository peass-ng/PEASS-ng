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
      /// <summary>Gets the <see cref="FileSystemEntryInfo"/> for a Non-/Transacted file or directory on the path.</summary>
      /// <returns>The <see cref="FileSystemEntryInfo"/> instance of the file or directory, or <c>null</c> on Exception when <paramref name="continueOnException"/> is <c>true</c>.</returns>
      /// <remarks>BasicSearch <see cref="NativeMethods.FINDEX_INFO_LEVELS.Basic"/> and LargeCache <see cref="NativeMethods.FIND_FIRST_EX_FLAGS.LARGE_FETCH"/> are used by default, if possible.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="isFolder">When <c>true</c> indicates the source is a directory; file otherwise. Use <c>false</c> if unknown.</param>
      /// <param name="path">The path to the file.</param>
      /// <param name="continueOnException">
      ///    <para><c>true</c> suppress any Exception that might be thrown as a result from a failure,</para>
      ///    <para>such as ACLs protected filesor non-accessible reparse points.</para>
      /// </param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static FileSystemEntryInfo GetFileSystemEntryInfoCore(KernelTransaction transaction, bool isFolder, string path, bool continueOnException, PathFormat pathFormat)
      {
         var options = continueOnException ? DirectoryEnumerationOptions.ContinueOnException : DirectoryEnumerationOptions.None;

         return new FindFileSystemEntryInfo(transaction, isFolder, path, Path.WildcardStarMatchAll, options, null, pathFormat, typeof(FileSystemEntryInfo)).Get<FileSystemEntryInfo>();
      }
   }
}
