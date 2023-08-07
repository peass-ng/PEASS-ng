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
      // .NET: Directory class does not contain the Copy() method, so mimic .NET File.Copy() methods.


      #region Obsolete

      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, DirectoryEnumerationFilters filters)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            DirectoryEnumerationFilters = filters
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            DirectoryEnumerationFilters = filters,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            DirectoryEnumerationFilters = filters,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="overwrite"><c>true</c> if the destination directory should ignoring the read-only and hidden attributes and overwrite; otherwise, <c>false</c>.</param>      
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("To disable/enable overwrite, use other overload and use CopyOptions.None enum flag or remove CopyOptions.FailIfExists enum flag.")]
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, bool overwrite, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = overwrite ? CopyOptions.None : CopyOptions.FailIfExists,
            DirectoryEnumerationFilters = filters,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, DirectoryEnumerationFilters filters)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            DirectoryEnumerationFilters = filters
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            DirectoryEnumerationFilters = filters,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            DirectoryEnumerationFilters = filters
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, bool preserveDates, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = preserveDates ? copyOptions | CopyOptions.CopyTimestamp : copyOptions & ~CopyOptions.CopyTimestamp,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            DirectoryEnumerationFilters = filters,
            PathFormat = pathFormat
         });
      }

      #endregion // Obsolete


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      public static CopyMoveResult Copy(string sourcePath, string destinationPath)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = CopyOptions.FailIfExists
         });
      }
      

      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = CopyOptions.FailIfExists,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, DirectoryEnumerationFilters filters)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = CopyOptions.FailIfExists,
            DirectoryEnumerationFilters = filters
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = CopyOptions.FailIfExists,
            DirectoryEnumerationFilters = filters,
            PathFormat = pathFormat
         });
      }
      

      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            DirectoryEnumerationFilters = filters,
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData
         });
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory. Overwriting a directory of the same name is not allowed.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
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
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            DirectoryEnumerationFilters = filters,
            CopyOptions = CopyOptions.FailIfExists,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat
         });
      }
      
      

      
      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions,
            PathFormat = pathFormat
         });
      }
      

      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            PathFormat = pathFormat
         });
      }
      

      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions,
            DirectoryEnumerationFilters = filters
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions,
            DirectoryEnumerationFilters = filters,
            PathFormat = pathFormat
         });
      }
      

      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            DirectoryEnumerationFilters = filters
         });
      }


      /// <summary>[AlphaFS] Copies a directory and its contents to a new location, <see cref="CopyOptions"/> can be specified,
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
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
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static CopyMoveResult Copy(string sourcePath, string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         return CopyMoveCore(new CopyMoveArguments
         {
            SourcePath = sourcePath,
            DestinationPath = destinationPath,
            CopyOptions = copyOptions,
            ProgressHandler = progressHandler,
            UserProgressData = userProgressData,
            DirectoryEnumerationFilters = filters,
            PathFormat = pathFormat
         });
      }
   }
}
