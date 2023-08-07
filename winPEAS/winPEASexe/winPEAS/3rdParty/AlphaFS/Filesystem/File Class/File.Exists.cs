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

using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Determines whether the specified file exists.</summary>
      /// <remarks>
      ///   <para>MSDN: .NET 3.5+: Trailing spaces are removed from the end of the
      ///   <paramref name="path"/> parameter before checking whether the directory exists.</para>
      ///   <para>The Exists method returns <c>false</c> if any error occurs while trying to
      ///   determine if the specified file exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a file name with
      ///   invalid characters or too many characters, a failing or missing disk, or if the caller does not have permission to read the
      ///   file.</para>
      ///   <para>The Exists method should not be used for path validation,
      ///   this method merely checks if the file specified in path exists.</para>
      ///   <para>Passing an invalid path to Exists returns false.</para>
      ///   <para>Be aware that another process can potentially do something with the file in
      ///   between the time you call the Exists method and perform another operation on the file, such as Delete.</para>
      /// </remarks>
      /// <param name="path">The file to check.</param>
      /// <returns>
      ///   Returns <c>true</c> if the caller has the required permissions and
      ///   <paramref name="path"/> contains the name of an existing file; otherwise,
      ///   <c>false</c>
      /// </returns>
      [SecurityCritical]
      public static bool Exists(string path)
      {
         return ExistsCore(null, false, path, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Determines whether the specified file exists.</summary>
      /// <remarks>
      ///   <para>MSDN: .NET 3.5+: Trailing spaces are removed from the end of the
      ///   <paramref name="path"/> parameter before checking whether the directory exists.</para>
      ///   <para>The Exists method returns <c>false</c> if any error occurs while trying to
      ///   determine if the specified file exists.</para>
      ///   <para>This can occur in situations that raise exceptions such as passing a file name with
      ///   invalid characters or too many characters,</para>
      ///   <para>a failing or missing disk, or if the caller does not have permission to read the
      ///   file.</para>
      ///   <para>The Exists method should not be used for path validation, this method merely checks
      ///   if the file specified in path exists.</para>
      ///   <para>Passing an invalid path to Exists returns false.</para>
      ///   <para>Be aware that another process can potentially do something with the file in
      ///   between the time you call the Exists method and perform another operation on the file, such
      ///   as Delete.</para>
      /// </remarks>
      /// <param name="path">The file to check.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      ///   <para>Returns <c>true</c> if the caller has the required permissions and
      ///   <paramref name="path"/> contains the name of an existing file; otherwise,
      ///   <c>false</c></para>
      /// </returns>
      [SecurityCritical]
      public static bool Exists(string path, PathFormat pathFormat)
      {
         return ExistsCore(null, false, path, pathFormat);
      }
   }
}
