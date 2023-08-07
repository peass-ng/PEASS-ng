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

      /// <summary>Retrieves the parent directory of the specified path, including both absolute and relative paths.</summary>
      /// <param name="path">The path for which to retrieve the parent directory.</param>
      /// <returns>The parent directory, or <c>null</c> if <paramref name="path"/> is the root directory, including the root of a UNC server or share name.</returns>
      [SecurityCritical]
      public static DirectoryInfo GetParent(string path)
      {
         return GetParentCore(null, path, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Retrieves the parent directory of the specified path, including both absolute and relative paths.</summary>
      /// <returns>The parent directory, or <c>null</c> if <paramref name="path"/> is the root directory, including the root of a UNC server or share name.</returns>
      /// <param name="path">The path for which to retrieve the parent directory.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static DirectoryInfo GetParent(string path, PathFormat pathFormat)
      {
         return GetParentCore(null, path, pathFormat);
      }

      #endregion // .NET
   }
}
