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
   public static partial class Directory
   {
      #region .NET

      /// <summary>Determines whether the given path refers to an existing directory on disk.</summary>
      /// <returns>
      ///   Returns <c>true</c> if <paramref name="path"/> refers to an existing directory.
      ///   Returns <c>false</c> if the directory does not exist or an error occurs when trying to determine if the specified file exists.
      /// </returns>
      /// <remarks>
      ///   The Exists method returns <c>false</c> if any error occurs while trying to determine if the specified file exists.
      ///   This can occur in situations that raise exceptions such as passing a file name with invalid characters or too many characters,
      ///   a failing or missing disk, or if the caller does not have permission to read the file.
      /// </remarks>
      /// <param name="path">The path to test.</param>
      [SecurityCritical]
      public static bool Exists(string path)
      {
         return File.ExistsCore(null, true, path, PathFormat.RelativePath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Determines whether the given path refers to an existing directory on disk.</summary>
      /// <returns>
      ///   Returns <c>true</c> if <paramref name="path"/> refers to an existing directory.
      ///   Returns <c>false</c> if the directory does not exist or an error occurs when trying to determine if the specified file exists.
      /// </returns>
      /// <remarks>
      ///   The Exists method returns <c>false</c> if any error occurs while trying to determine if the specified file exists.
      ///   This can occur in situations that raise exceptions such as passing a file name with invalid characters or too many characters,
      ///   a failing or missing disk, or if the caller does not have permission to read the file.
      /// </remarks>
      /// <param name="path">The path to test.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static bool Exists(string path, PathFormat pathFormat)
      {
         return File.ExistsCore(null, true, path, pathFormat);
      }
   }
}
