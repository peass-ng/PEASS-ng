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

using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Gets the <see cref="FileSystemEntryInfo"/> of the directory on the path.</summary>
      /// <returns>The <see cref="FileSystemEntryInfo"/> instance of the directory.</returns>
      /// <param name="path">The path to the directory.</param>
      [SecurityCritical]
      public static FileSystemEntryInfo GetFileSystemEntryInfo(string path)
      {
         return File.GetFileSystemEntryInfoCore(null, true, path, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Gets the <see cref="FileSystemEntryInfo"/> of the directory on the path.</summary>
      /// <returns>The <see cref="FileSystemEntryInfo"/> instance of the directory.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static FileSystemEntryInfo GetFileSystemEntryInfo(string path, PathFormat pathFormat)
      {
         return File.GetFileSystemEntryInfoCore(null, true, path, false, pathFormat);
      }


      /// <summary>[AlphaFS] Gets the <see cref="FileSystemEntryInfo"/> of the directory on the path.</summary>
      /// <returns>The <see cref="FileSystemEntryInfo"/> instance of the directory.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="continueOnException">
      ///    <para><c>true</c> suppress any Exception that might be thrown as a result from a failure,</para>
      ///    <para>such as ACLs protected directories or non-accessible reparse points.</para>
      /// </param>
      [SecurityCritical]
      public static FileSystemEntryInfo GetFileSystemEntryInfo(string path, bool continueOnException)
      {
         return File.GetFileSystemEntryInfoCore(null, true, path, continueOnException, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Gets the <see cref="FileSystemEntryInfo"/> of the directory on the path.</summary>
      /// <returns>The <see cref="FileSystemEntryInfo"/> instance of the directory.</returns>
      /// <param name="path">The path to the directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <param name="continueOnException">
      ///    <para><c>true</c> suppress any Exception that might be thrown as a result from a failure,</para>
      ///    <para>such as ACLs protected directories or non-accessible reparse points.</para>
      /// </param>
      [SecurityCritical]
      public static FileSystemEntryInfo GetFileSystemEntryInfo(string path, bool continueOnException, PathFormat pathFormat)
      {
         return File.GetFileSystemEntryInfoCore(null, true, path, continueOnException, pathFormat);
      }
   }
}
