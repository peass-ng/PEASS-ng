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
      /// <summary>Copy/move an existing file to a new file, allowing the overwriting of an existing file.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with the status of the Copy or Move action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <param name="destinationPath"><para>A full path string to the destination directory</para></param>
      /// <param name="copyOptions"><para>This parameter can be <c>null</c>. Use <see cref="CopyOptions"/> to specify how the file is to be copied.</para></param>
      /// <param name="moveOptions"><para>This parameter can be <c>null</c>. Use <see cref="MoveOptions"/> that specify how the file is to be moved.</para></param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="progressHandler"><para>This parameter can be <c>null</c>. A callback function that is called each time another portion of the file has been copied.</para></param>
      /// <param name="userProgressData"><para>This parameter can be <c>null</c>. The argument to be passed to the callback function.</para></param>
      /// <param name="longFullPath">[out] Returns the retrieved long full path.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      private CopyMoveResult CopyToMoveToCore(string destinationPath, CopyOptions? copyOptions, MoveOptions? moveOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData, out string longFullPath, PathFormat pathFormat)
      {
         longFullPath = Path.GetExtendedLengthPathCore(Transaction, destinationPath, pathFormat, GetFullPathOptions.TrimEnd | GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

         return File.CopyMoveCore(false, new CopyMoveArguments
         {
            Transaction = Transaction,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            MoveOptions = moveOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = PathFormat.LongFullPath

         }, false, false, LongFullName, longFullPath, null);
      }


      /// <summary>Refreshes the current <see cref="FileInfo"/> instance with a new destination path.</summary>
      private void UpdateDestinationPath(string destinationPath, string destinationPathLp)
      {
         _name = Path.GetFileName(destinationPathLp, true);

         UpdateSourcePath(destinationPath, destinationPathLp);
      }
   }
}
