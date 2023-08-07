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
   public static partial class File
   {
      #region Obsolete

      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.</summary>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="overwrite"><c>true</c> if the destination file should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.</summary>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="overwrite"><c>true</c> if the destination file should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }
      

      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed. <see cref="CopyOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved; otherwise, <c>false</c>.</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed. <see cref="CopyOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved; otherwise, <c>false</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.  <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved; otherwise, <c>false</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.  <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved; otherwise, <c>false</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }

      #endregion // Obsolete

      
      #region .NET

      /// <summary>Copies an existing file to a new file. Overwriting a file of the same name is not allowed.</summary>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
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
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory or an existing file.</param>
      [SecurityCritical]
      public static void Copy(string sourcePath, string destinationPath)
      {
         CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = CopyOptions.FailIfExists

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>Copies an existing file to a new file. Overwriting a file of the same name is allowed.</summary>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
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
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="overwrite"><c>true</c> if the destination file should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      [SecurityCritical]
      public static void Copy(string sourcePath, string destinationPath, bool overwrite)
      {
         CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists

         }, false, false, sourcePath, destinationPath, null);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is not allowed.</summary>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = CopyOptions.FailIfExists,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }

      /// <summary>
      /// [AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is
      /// allowed.
      /// </summary>
      /// <remarks>
      /// <para>The attributes of the original file are retained in the copied file.</para>
      /// <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this
      /// method.</para>
      /// <para>If two files have equivalent short file names then this method may fail and raise an
      /// exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException">.</exception>
      /// <exception cref="ArgumentNullException">.</exception>
      /// <exception cref="DirectoryNotFoundException">.</exception>
      /// <exception cref="FileNotFoundException">.</exception>
      /// <exception cref="IOException">.</exception>
      /// <exception cref="NotSupportedException">.</exception>
      /// <exception cref="UnauthorizedAccessException">.</exception>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="overwrite"><c>true</c> if the destination file should ignoring the read-only and
      /// hidden attributes and overwrite; otherwise, <c>false</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      /// Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.
      /// </returns>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, PathFormat pathFormat)
      {
         return CopyMoveCore(true, new CopyMoveArguments
         {
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is not allowed.</summary>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory or an existing file.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, int retry, int retryTimeout)
      {
         return CopyMoveCore(true, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = CopyOptions.FailIfExists

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is not allowed.</summary>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, int retry, int retryTimeout, PathFormat pathFormat)
      {
         return CopyMoveCore(true, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = CopyOptions.FailIfExists,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is not allowed. Possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
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
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is not allowed. Possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
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
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is not allowed. Possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
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
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, int retry, int retryTimeout, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is not allowed. Possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
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
      /// <param name="sourcePath">The file to copy. </param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, int retry, int retryTimeout, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }
      



      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed. <see cref="CopyOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = copyOptions

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed. <see cref="CopyOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = copyOptions,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }
      

      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed. <see cref="CopyOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, int retry, int retryTimeout)
      {
         return CopyMoveCore(true, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = copyOptions

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed. <see cref="CopyOptions"/> can be specified.</summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, int retry, int retryTimeout, PathFormat pathFormat)
      {
         return CopyMoveCore(true, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = copyOptions,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }
      

      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.  <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.  <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(false, new CopyMoveArguments
         {
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }
      

      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.  <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, int retry, int retryTimeout, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(true, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData

         }, false, false, sourcePath, destinationPath, null);
      }


      /// <summary>[AlphaFS] Copies an existing file to a new file. Overwriting a file of the same name is allowed.  <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>The attributes of the original file are retained in the copied file.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two files have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <returns>Returns a <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="sourcePath">The file to copy.</param>
      /// <param name="destinationPath">The name of the destination file. This cannot be a directory.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the file is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="retry">The number of retries on failed copies.</param>
      /// <param name="retryTimeout">The wait time in seconds between retries.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the file has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, int retry, int retryTimeout, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(true, new CopyMoveArguments
         {
            Retry = retry,
            RetryTimeout = retryTimeout,
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat

         }, false, false, sourcePath, destinationPath, null);
      }
   }
}
