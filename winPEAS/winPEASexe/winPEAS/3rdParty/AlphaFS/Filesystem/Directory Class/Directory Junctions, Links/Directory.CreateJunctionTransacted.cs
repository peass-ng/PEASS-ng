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
   public static partial class Directory
   {
      #region Obsolete

      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      [SecurityCritical]
      public static void CreateJunction(KernelTransaction transaction, string junctionPath, string directoryPath)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, false, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CreateJunction(KernelTransaction transaction, string junctionPath, string directoryPath, PathFormat pathFormat)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, false, false, pathFormat);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      [SecurityCritical]
      public static void CreateJunction(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CreateJunction(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite, PathFormat pathFormat)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, false, pathFormat);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// The directory date and time stamps from <paramref name="directoryPath"/> (the target) are copied to the directory junction.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="copyTargetTimestamps"><c>true</c> to copy the target date and time stamps to the directory junction.</param>
      [SecurityCritical]
      public static void CreateJunction(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite, bool copyTargetTimestamps)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, copyTargetTimestamps, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// The directory date and time stamps from <paramref name="directoryPath"/> (the target) are copied to the directory junction.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="copyTargetTimestamps"><c>true</c> to copy the target date and time stamps to the directory junction.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CreateJunction(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite, bool copyTargetTimestamps, PathFormat pathFormat)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, copyTargetTimestamps, pathFormat);
      }

      #endregion // Obsolete


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      [SecurityCritical]
      public static void CreateJunctionTransacted(KernelTransaction transaction, string junctionPath, string directoryPath)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, false, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J").</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CreateJunctionTransacted(KernelTransaction transaction, string junctionPath, string directoryPath, PathFormat pathFormat)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, false, false, pathFormat);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      [SecurityCritical]
      public static void CreateJunctionTransacted(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CreateJunctionTransacted(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite, PathFormat pathFormat)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, false, pathFormat);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// The directory date and time stamps from <paramref name="directoryPath"/> (the target) are copied to the directory junction.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="copyTargetTimestamps"><c>true</c> to copy the target date and time stamps to the directory junction.</param>
      [SecurityCritical]
      public static void CreateJunctionTransacted(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite, bool copyTargetTimestamps)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, copyTargetTimestamps, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J"). Overwriting a junction point of the same name is allowed.</summary>
      /// <remarks>
      /// The directory must be empty and reside on a local volume.
      /// The directory date and time stamps from <paramref name="directoryPath"/> (the target) are copied to the directory junction.
      /// <para>
      ///   MSDN: A junction (also called a soft link) differs from a hard link in that the storage objects it references are separate directories,
      ///   and a junction can link directories located on different local volumes on the same computer.
      ///   Otherwise, junctions operate identically to hard links. Junctions are implemented through reparse points.
      /// </para>
      /// </remarks>
      /// <exception cref="AlreadyExistsException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path of the junction point to create.</param>
      /// <param name="directoryPath">The path to the directory. If the directory does not exist it will be created.</param>
      /// <param name="overwrite"><c>true</c> to overwrite an existing junction point. The directory is removed and recreated.</param>
      /// <param name="copyTargetTimestamps"><c>true</c> to copy the target date and time stamps to the directory junction.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CreateJunctionTransacted(KernelTransaction transaction, string junctionPath, string directoryPath, bool overwrite, bool copyTargetTimestamps, PathFormat pathFormat)
      {
         CreateJunctionCore(transaction, junctionPath, directoryPath, overwrite, copyTargetTimestamps, pathFormat);
      }
   }
}
