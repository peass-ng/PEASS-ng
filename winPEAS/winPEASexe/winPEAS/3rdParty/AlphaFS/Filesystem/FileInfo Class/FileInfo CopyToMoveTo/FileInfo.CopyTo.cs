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

      /// <summary>Copies an existing file to a new file, disallowing the overwriting of an existing file.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Use this method to prevent overwriting of an existing file by default.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, CopyOptions.FailIfExists, null, false, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>Copies an existing file to a new file, allowing the overwriting of an existing file.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="overwrite"><c>true</c> to allow an existing file to be overwritten; otherwise, <c>false</c>.</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath, bool overwrite)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, overwrite ? CopyOptions.None : CopyOptions.FailIfExists, null, false, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Copies an existing file to a new file, disallowing the overwriting of an existing file.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Use this method to prevent overwriting of an existing file by default.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, CopyOptions.FailIfExists, null, false, null, null, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="overwrite"><c>true</c> to allow an existing file to be overwritten; otherwise, <c>false</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath, bool overwrite, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, overwrite ? CopyOptions.None : CopyOptions.FailIfExists, null, false, null, null, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath, CopyOptions copyOptions)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, copyOptions, null, false, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }

      
      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath, CopyOptions copyOptions, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, copyOptions, null, false, null, null, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }
      

      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, copyOptions, null, preserveDates, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A new <see cref="FileInfo"/> instance with a fully qualified path.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public FileInfo CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, copyOptions, null, preserveDates, null, null, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return new FileInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }

      
      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      ///   <para>and the possibility of notifying the application of its progress through a callback function.</para>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, copyOptions, null, false, progressHandler, userProgressData, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return cmr;
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, copyOptions, null, false, progressHandler, userProgressData, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return cmr;
      }
      

      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      ///   <para>and the possibility of notifying the application of its progress through a callback function.</para>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, copyOptions, null, preserveDates, progressHandler, userProgressData, out destinationPathLp, PathFormat.RelativePath);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return cmr;
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file, allowing the overwriting of an existing file, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      ///   <para>and the possibility of notifying the application of its progress through a callback function.</para>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing file.</para>
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
      /// <param name="destinationPath">The name of the new file to copy to.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, copyOptions, null, preserveDates, progressHandler, userProgressData, out destinationPathLp, pathFormat);

         UpdateDestinationPath(destinationPath, destinationPathLp);

         return cmr;
      }
   }
}
