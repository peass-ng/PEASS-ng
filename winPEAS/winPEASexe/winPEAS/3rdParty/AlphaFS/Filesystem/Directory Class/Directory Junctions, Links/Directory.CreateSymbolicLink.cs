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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Creates a symbolic link  to a directory (similar to CMD command: "MKLINK /D").</summary>
      /// <para>&#160;</para>
      /// <remarks>
      /// <para>Symbolic links can point to a non-existent target.</para>
      /// <para>When creating a symbolic link, the operating system does not check to see if the target exists.</para>
      /// <para>Symbolic links are reparse points.</para>
      /// <para>There is a maximum of 31 reparse points (and therefore symbolic links) allowed in a particular path.</para>
      /// <para>See <see cref="Security.Privilege.CreateSymbolicLink"/> to run this method in an elevated state.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="symlinkDirectoryName">The name of the target for the symbolic link to be created.</param>
      /// <param name="targetDirectoryName">The symbolic link to be created.</param>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "symlink")]
      [SecurityCritical]
      public static void CreateSymbolicLink(string symlinkDirectoryName, string targetDirectoryName)
      {
         File.CreateSymbolicLinkCore(null, symlinkDirectoryName, targetDirectoryName, SymbolicLinkTarget.Directory, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates a symbolic link  to a directory (similar to CMD command: "MKLINK /D").</summary>
      /// <para>&#160;</para>
      /// <remarks>
      /// <para>Symbolic links can point to a non-existent target.</para>
      /// <para>When creating a symbolic link, the operating system does not check to see if the target exists.</para>
      /// <para>Symbolic links are reparse points.</para>
      /// <para>There is a maximum of 31 reparse points (and therefore symbolic links) allowed in a particular path.</para>
      /// <para>See <see cref="Security.Privilege.CreateSymbolicLink"/> to run this method in an elevated state.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="PlatformNotSupportedException">The operating system is older than Windows Vista.</exception>
      /// <param name="symlinkDirectoryName">The name of the target for the symbolic link to be created.</param>
      /// <param name="targetDirectoryName">The symbolic link to be created.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>      
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "symlink")]
      [SecurityCritical]
      public static void CreateSymbolicLink(string symlinkDirectoryName, string targetDirectoryName, PathFormat pathFormat)
      {
         File.CreateSymbolicLinkCore(null, symlinkDirectoryName, targetDirectoryName, SymbolicLinkTarget.Directory, pathFormat);
      }
   }
}
