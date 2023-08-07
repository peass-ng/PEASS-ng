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
      #region .NET

      /// <summary>Moves a file or a directory and its contents to a new location.</summary>
      /// <remarks>
      ///   <para>This method does not work across disk volumes.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The source directory path.</param>
      /// <param name="destinationPath">The destination directory path.</param>
      [SecurityCritical]
      public static void Move(string sourcePath, string destinationPath)
      {
         CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            MoveOptions = MoveOptions.None
         });
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Moves a file or a directory and its contents to a new location.</summary>
      /// <remarks>
      ///   <para>This method does not work across disk volumes.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Move action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The source directory path.</param>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Move(string sourcePath, string destinationPath, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            MoveOptions = MoveOptions.None,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Moves a file or a directory and its contents to a new location, <see cref="MoveOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>This method does not work across disk volumes unless <paramref name="moveOptions"/> contains <see cref="MoveOptions.CopyAllowed"/>.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Move action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The source directory path.</param>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Move(string sourcePath, string destinationPath, MoveOptions moveOptions)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            MoveOptions = moveOptions
         });
      }


      /// <summary>[AlphaFS] Moves a file or a directory and its contents to a new location, <see cref="MoveOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>This method does not work across disk volumes unless <paramref name="moveOptions"/> contains <see cref="MoveOptions.CopyAllowed"/>.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Move action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The source directory path.</param>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Move(string sourcePath, string destinationPath, MoveOptions moveOptions, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            MoveOptions = moveOptions,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Moves a file or a directory and its contents to a new location, <see cref="MoveOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>This method does not work across disk volumes unless <paramref name="moveOptions"/> contains <see cref="MoveOptions.CopyAllowed"/>.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Move action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The source directory path.</param>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been moved. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Move(string sourcePath, string destinationPath, MoveOptions moveOptions, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            MoveOptions = moveOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData
         });
      }


      /// <summary>[AlphaFS] Moves a file or a directory and its contents to a new location, <see cref="MoveOptions"/> can be specified,
      ///   and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>This method does not work across disk volumes unless <paramref name="moveOptions"/> contains <see cref="MoveOptions.CopyAllowed"/>.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Move action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The source directory path.</param>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been moved. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Move(string sourcePath, string destinationPath, MoveOptions moveOptions, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            MoveOptions = moveOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat
         });
      }
   }
}
