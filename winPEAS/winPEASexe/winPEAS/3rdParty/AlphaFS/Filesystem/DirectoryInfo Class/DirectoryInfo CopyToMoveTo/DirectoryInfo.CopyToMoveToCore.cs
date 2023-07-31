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
      /// <summary>Copy/move a Non-/Transacted file or directory including its children to a new location,
      /// <see cref="CopyOptions"/> or <see cref="MoveOptions"/> can be specified, and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy or Move action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>You cannot use the Move method to overwrite an existing file, unless <paramref name="moveOptions"/> contains <see cref="MoveOptions.ReplaceExisting"/>.</para>
      ///   <para>Note that if you attempt to replace a file by moving a file of the same name into that directory, you get an IOException.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="moveOptions"><see cref="MoveOptions"/> that specify how the file is to be moved. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="longFullPath">Returns the retrieved long full path.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      private CopyMoveResult CopyToMoveToCore(string destinationPath, bool preserveDates, CopyOptions? copyOptions, MoveOptions? moveOptions, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData, out string longFullPath, PathFormat pathFormat)
      {
         longFullPath = Path.GetExtendedLengthPathCore(Transaction, destinationPath, pathFormat, GetFullPathOptions.TrimEnd | GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

         return Directory.CopyMoveCore(new CopyMoveArguments
         {
            Transaction = Transaction,
            SourcePathLp = LongFullName,
            SourcePath = LongFullName,
            DestinationPathLp = longFullPath,
            DestinationPath = longFullPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            DirectoryEnumerationFilters = filters,
            MoveOptions = moveOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = PathFormat.LongFullPath
         });
      }
   }
}
