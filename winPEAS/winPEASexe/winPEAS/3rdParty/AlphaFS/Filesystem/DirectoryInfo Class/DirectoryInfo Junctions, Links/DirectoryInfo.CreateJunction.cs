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
      /// <summary>[AlphaFS] Converts the <see cref="DirectoryInfo"/> instance into a directory junction instance (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// <para>&#160;</para>
      /// <para>The directory must be empty and reside on a local volume.</para>
      /// <para></para>
      /// <para></para>
      /// <para>&#160;</para>
      /// <para>MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,</para>
      /// <para>and a junction can link directories located on different local volumes on the same computer.</para>
      /// <para>Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      [SecurityCritical]
      public void CreateJunction(string junctionPath)
      {
         UpdateSourcePath(junctionPath, Directory.CreateJunctionCore(Transaction, junctionPath, LongFullName, false, false, PathFormat.RelativePath));

         RefreshEntryInfo();
      }


      /// <summary>[AlphaFS] Converts the <see cref="DirectoryInfo"/> instance into a directory junction instance (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// <para>&#160;</para>
      /// <para>The directory must be empty and reside on a local volume.</para>
      /// <para></para>
      /// <para></para>
      /// <para>&#160;</para>
      /// <para>MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,</para>
      /// <para>and a junction can link directories located on different local volumes on the same computer.</para>
      /// <para>Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public void CreateJunction(string junctionPath, PathFormat pathFormat)
      {
         UpdateSourcePath(junctionPath, Directory.CreateJunctionCore(Transaction, junctionPath, LongFullName, false, false, pathFormat));

         RefreshEntryInfo();
      }


      /// <summary>[AlphaFS] Converts the <see cref="DirectoryInfo"/> instance into a directory junction instance (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// <para>&#160;</para>
      /// <para>The directory must be empty and reside on a local volume.</para>
      /// <para></para>
      /// <para></para>
      /// <para>&#160;</para>
      /// <para>MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,</para>
      /// <para>and a junction can link directories located on different local volumes on the same computer.</para>
      /// <para>Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      [SecurityCritical]
      public void CreateJunction(string junctionPath, bool overwrite)
      {
         UpdateSourcePath(junctionPath, Directory.CreateJunctionCore(Transaction, junctionPath, LongFullName, overwrite, false, PathFormat.RelativePath));

         RefreshEntryInfo();
      }


      /// <summary>[AlphaFS] Converts the <see cref="DirectoryInfo"/> instance into a directory junction instance (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// <para>&#160;</para>
      /// <para>The directory must be empty and reside on a local volume.</para>
      /// <para></para>
      /// <para></para>
      /// <para>&#160;</para>
      /// <para>MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,</para>
      /// <para>and a junction can link directories located on different local volumes on the same computer.</para>
      /// <para>Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public void CreateJunction(string junctionPath, bool overwrite, PathFormat pathFormat)
      {
         UpdateSourcePath(junctionPath, Directory.CreateJunctionCore(Transaction, junctionPath, LongFullName, overwrite, false, pathFormat));

         RefreshEntryInfo();
      }


      /// <summary>[AlphaFS] Converts the <see cref="DirectoryInfo"/> instance into a directory junction instance (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// <para>&#160;</para>
      /// <para>The directory must be empty and reside on a local volume.</para>
      /// <para></para>
      /// <para></para>
      /// <para>&#160;</para>
      /// <para>MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,</para>
      /// <para>and a junction can link directories located on different local volumes on the same computer.</para>
      /// <para>Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="copyTargetTimestamps"><c>true</c> to copy the target date and time stamps to the directory junction.</param>
      [SecurityCritical]
      public void CreateJunction(string junctionPath, bool overwrite, bool copyTargetTimestamps)
      {
         UpdateSourcePath(junctionPath, Directory.CreateJunctionCore(Transaction, junctionPath, LongFullName, overwrite, copyTargetTimestamps, PathFormat.RelativePath));

         RefreshEntryInfo();
      }


      /// <summary>[AlphaFS] Converts the <see cref="DirectoryInfo"/> instance into a directory junction instance (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// <para>&#160;</para>
      /// <para>The directory must be empty and reside on a local volume.</para>
      /// <para></para>
      /// <para></para>
      /// <para>&#160;</para>
      /// <para>MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,</para>
      /// <para>and a junction can link directories located on different local volumes on the same computer.</para>
      /// <para>Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="copyTargetTimestamps"><c>true</c> to copy the target date and time stamps to the directory junction.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public void CreateJunction(string junctionPath, bool overwrite, bool copyTargetTimestamps, PathFormat pathFormat)
      {
         UpdateSourcePath(junctionPath, Directory.CreateJunctionCore(Transaction, junctionPath, LongFullName, overwrite, copyTargetTimestamps, pathFormat));

         RefreshEntryInfo();
      }
   }
}
