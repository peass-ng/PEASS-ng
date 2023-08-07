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
   partial class FileInfo
   {
      #region .NET

      /// <summary>Moves a specified file to a new location, providing the option to specify a new file name.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Move action.</returns>
      /// <remarks>
      ///   <para>Use this method to prevent overwriting of an existing file by default.</para>
      ///   <para>This method works across disk volumes.</para>
      ///   <para>For example, the file c:\MyFile.txt can be moved to d:\public and renamed NewFile.txt.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The path to move the file to, which can specify a different file name.</param>
      [SecurityCritical]
      public void MoveTo(string destinationPath)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, null, MoveOptions.CopyAllowed, false, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Moves a specified file to a new location, providing the option to specify a new file name.</summary>
      /// <returns>Returns a new <see cref="FileInfo"/> instance with a fully qualified path when successfully moved.</returns>
      /// <remarks>
      ///   <para>Use this method to prevent overwriting of an existing file by default.</para>
      ///   <para>This method works across disk volumes.</para>
      ///   <para>For example, the file c:\MyFile.txt can be moved to d:\public and renamed NewFile.txt.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The path to move the file to, which can specify a different file name.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo MoveTo(string destinationPath, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, null, MoveOptions.CopyAllowed, false, null, null, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }
      

      /// <summary>[AlphaFS] Moves a specified file to a new location, providing the option to specify a new file name, <see cref="MoveOptions"/> can be specified.</summary>
      /// <returns>Returns a new <see cref="FileInfo"/> instance with a fully qualified path when successfully moved.</returns>
      /// <remarks>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
      ///   <para>This method works across disk volumes.</para>
      ///   <para>For example, the file c:\MyFile.txt can be moved to d:\public and renamed NewFile.txt.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The path to move the file to, which can specify a different file name.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public FileInfo MoveTo(string destinationPath, MoveOptions moveOptions)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, null, moveOptions, false, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return null != destinationPathLp ? new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath) : null;
      }


      /// <summary>[AlphaFS] Moves a specified file to a new location, providing the option to specify a new file name, <see cref="MoveOptions"/> can be specified.</summary>
      /// <returns>Returns a new <see cref="FileInfo"/> instance with a fully qualified path when successfully moved.</returns>
      /// <remarks>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
      ///   <para>This method works across disk volumes.</para>
      ///   <para>For example, the file c:\MyFile.txt can be moved to d:\public and renamed NewFile.txt.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The path to move the file to, which can specify a different file name.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo MoveTo(string destinationPath, MoveOptions moveOptions, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, null, moveOptions, false, null, null, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return null != destinationPathLp ? new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath) : null;
      }
      

      /// <summary>[AlphaFS] Moves a specified file to a new location, providing the option to specify a new file name, <see cref="MoveOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with the status of the Move action.</returns>
      /// <remarks>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
      ///   <para>This method works across disk volumes.</para>
      ///   <para>For example, the file c:\MyFile.txt can be moved to d:\public and renamed NewFile.txt.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The path to move the file to, which can specify a different file name.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been moved. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public CopyMoveResult MoveTo(string destinationPath, MoveOptions moveOptions, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         // Reject DelayUntilReboot.

         if ((moveOptions & MoveOptions.DelayUntilReboot) != 0)

            throw new ArgumentException("The DelayUntilReboot flag is invalid for this method.", "moveOptions");


         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, null, moveOptions, false, progressHandler, userProgressData, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return cmr;
      }


      /// <summary>[AlphaFS] Moves a specified file to a new location, providing the option to specify a new file name, <see cref="MoveOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with the status of the Move action.</returns>
      /// <remarks>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
      ///   <para>This method works across disk volumes.</para>
      ///   <para>For example, the file c:\MyFile.txt can be moved to d:\public and renamed NewFile.txt.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The path to move the file to, which can specify a different file name.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the directory is to be moved. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been moved. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public CopyMoveResult MoveTo(string destinationPath, MoveOptions moveOptions, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         // Reject DelayUntilReboot.

         if ((moveOptions & MoveOptions.DelayUntilReboot) != 0)

            throw new ArgumentException("The DelayUntilReboot flag is invalid for this method.", "moveOptions");


         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, null, moveOptions, false, progressHandler, userProgressData, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return cmr;
      }
   }
}
