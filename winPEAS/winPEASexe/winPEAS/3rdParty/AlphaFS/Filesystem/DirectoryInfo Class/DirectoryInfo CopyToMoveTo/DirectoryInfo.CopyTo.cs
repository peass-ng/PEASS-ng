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
      // .NET: Directory class does not contain the Copy() method, so mimic .NET File.Copy() methods.


      #region Obsolete

      /// <summary>[AlphaFS] Copies a <see cref="DirectoryInfo"/> instance and its contents to a new path.</summary>
      /// <returns>Returns a new <see cref="DirectoryInfo"/> instance.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, bool preserveDates)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, preserveDates, CopyOptions.FailIfExists, null, null, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>Returns a new <see cref="DirectoryInfo"/> instance.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, bool preserveDates, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, preserveDates, CopyOptions.FailIfExists, null, null, null, null, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, preserveDates, copyOptions, null, null, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, preserveDates, copyOptions, null, null, null, null, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, preserveDates, copyOptions, null, null, progressHandler, userProgressData, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return cmr;
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="preserveDates"><c>true</c> if original Timestamps must be preserved, <c>false</c> otherwise.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("Use other overload and add CopyOptions.CopyTimestamp enum flag.")]
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, bool preserveDates, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, preserveDates, copyOptions, null, null, progressHandler, userProgressData, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return cmr;
      }
      
      #endregion // Obsolete


      /// <summary>[AlphaFS] Copies a <see cref="DirectoryInfo"/> instance and its contents to a new path.</summary>
      /// <returns>A new <see cref="DirectoryInfo"/> instance if the directory was completely copied.</returns>
      /// <remarks>
      ///   <para>Use this method to prevent overwriting of an existing directory by default.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, CopyOptions.FailIfExists, null, null, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies a <see cref="DirectoryInfo"/> instance and its contents to a new path.</summary>
      /// <returns>A new <see cref="DirectoryInfo"/> instance if the directory was completely copied.</returns>
      /// <remarks>
      ///   <para>Use this method to prevent overwriting of an existing directory by default.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, CopyOptions.FailIfExists, null, null, null, null, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }
      

      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, copyOptions, null, null, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, copyOptions, null, null, null, null, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }
      

      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified
      /// and the possibility of notifying the application of its progress through a callback function.
      /// </summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, false, copyOptions, null, null, progressHandler, userProgressData, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return cmr;
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>A <see cref="CopyMoveResult"/> class with details of the Copy action.</returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public CopyMoveResult CopyTo(string destinationPath, CopyOptions copyOptions, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         string destinationPathLp;

         var cmr = CopyToMoveToCore(destinationPath, false, copyOptions, null, null, progressHandler, userProgressData, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return cmr;
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, copyOptions, null, filters, null, null, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, copyOptions, null, filters, null, null, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, copyOptions, null, filters, progressHandler, userProgressData, out destinationPathLp, PathFormat.RelativePath);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }


      /// <summary>[AlphaFS] Copies an existing directory to a new directory, allowing the overwriting of an existing directory, <see cref="CopyOptions"/> can be specified
      /// and the possibility of notifying the application of its progress through a callback function.</summary>
      /// <returns>
      ///   <para>Returns a new directory, or an overwrite of an existing directory if <paramref name="copyOptions"/> is not <see cref="CopyOptions.FailIfExists"/>.</para>
      ///   <para>If the directory exists and <paramref name="copyOptions"/> contains <see cref="CopyOptions.FailIfExists"/>, an <see cref="IOException"/> is thrown.</para>
      /// </returns>
      /// <remarks>
      ///   <para>Option <see cref="CopyOptions.NoBuffering"/> is recommended for very large file transfers.</para>
      ///   <para>Use this method to allow or prevent overwriting of an existing directory.</para>
      ///   <para>Whenever possible, avoid using short file names (such as <c>XXXXXX~1.XXX</c>) with this method.</para>
      ///   <para>If two directories have equivalent short file names then this method may fail and raise an exception and/or result in undesirable behavior.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="destinationPath">The destination directory path.</param>
      /// <param name="copyOptions"><see cref="CopyOptions"/> that specify how the directory is to be copied. This parameter can be <c>null</c>.</param>
      /// <param name="filters">The specification of custom filters to be used in the process.</param>
      /// <param name="progressHandler">A callback function that is called each time another portion of the directory has been copied. This parameter can be <c>null</c>.</param>
      /// <param name="userProgressData">The argument to be passed to the callback function. This parameter can be <c>null</c>.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public DirectoryInfo CopyTo(string destinationPath, CopyOptions copyOptions, DirectoryEnumerationFilters filters, CopyMoveProgressRoutine progressHandler, object userProgressData, PathFormat pathFormat)
      {
         string destinationPathLp;

         CopyToMoveToCore(destinationPath, false, copyOptions, null, filters, progressHandler, userProgressData, out destinationPathLp, pathFormat);

         UpdateSourcePath(destinationPath, destinationPathLp);

         return new DirectoryInfo(Transaction, destinationPathLp, PathFormat.LongFullPath);
      }
   }
}
