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

      /// <summary>[AlphaFS] Determines whether the given path refers to an existing directory junction on disk.</summary>
      /// <returns>
      ///   <para>Returns <c>true</c> if <paramref name="junctionPath"/> refers to an existing directory junction.</para>
      ///   <para>Returns <c>false</c> if the directory junction does not exist or an error occurs when trying to determine if the specified file exists.</para>
      /// </returns>
      /// <para>&#160;</para>
      /// <remarks>
      ///   <para>The Exists method returns <c>false</c> if any error occurs while trying to determine if the specified file exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a file name with invalid characters or too many characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the file.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path to test.</param>
      [Obsolete("Use ExistsJunctionTransacted method.")]
      [SecurityCritical]
      public static bool ExistsJunction(KernelTransaction transaction, string junctionPath)
      {
         return ExistsJunctionCore(transaction, null, junctionPath, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Determines whether the given path refers to an existing directory junction on disk.</summary>
      /// <returns>
      ///   <para>Returns <c>true</c> if <paramref name="junctionPath"/> refers to an existing directory junction.</para>
      ///   <para>Returns <c>false</c> if the directory junction does not exist or an error occurs when trying to determine if the specified file exists.</para>
      /// </returns>
      /// <para>&#160;</para>
      /// <remarks>
      ///   <para>The Exists method returns <c>false</c> if any error occurs while trying to determine if the specified file exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a file name with invalid characters or too many characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the file.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path to test.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [Obsolete("Use ExistsJunctionTransacted method.")]
      [SecurityCritical]
      public static bool ExistsJunction(KernelTransaction transaction, string junctionPath, PathFormat pathFormat)
      {
         return ExistsJunctionCore(transaction, null, junctionPath, pathFormat);
      }

      #endregion // Obsolete


      /// <summary>[AlphaFS] Determines whether the given path refers to an existing directory junction on disk.</summary>
      /// <returns>
      ///   <para>Returns <c>true</c> if <paramref name="junctionPath"/> refers to an existing directory junction.</para>
      ///   <para>Returns <c>false</c> if the directory junction does not exist or an error occurs when trying to determine if the specified file exists.</para>
      /// </returns>
      /// <para>&#160;</para>
      /// <remarks>
      ///   <para>The Exists method returns <c>false</c> if any error occurs while trying to determine if the specified file exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a file name with invalid characters or too many characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the file.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path to test.</param>
      [SecurityCritical]
      public static bool ExistsJunctionTransacted(KernelTransaction transaction, string junctionPath)
      {
         return ExistsJunctionCore(transaction, null, junctionPath, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Determines whether the given path refers to an existing directory junction on disk.</summary>
      /// <returns>
      ///   <para>Returns <c>true</c> if <paramref name="junctionPath"/> refers to an existing directory junction.</para>
      ///   <para>Returns <c>false</c> if the directory junction does not exist or an error occurs when trying to determine if the specified file exists.</para>
      /// </returns>
      /// <para>&#160;</para>
      /// <remarks>
      ///   <para>The Exists method returns <c>false</c> if any error occurs while trying to determine if the specified file exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a file name with invalid characters or too many characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the file.</para>
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="junctionPath">The path to test.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static bool ExistsJunctionTransacted(KernelTransaction transaction, string junctionPath, PathFormat pathFormat)
      {
         return ExistsJunctionCore(transaction, null, junctionPath, pathFormat);
      }
   }
}
