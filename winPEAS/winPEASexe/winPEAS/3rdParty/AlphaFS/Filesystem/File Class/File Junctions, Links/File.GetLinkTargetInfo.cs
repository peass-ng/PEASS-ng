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
      /// <summary>[AlphaFS] Gets information about the target of a mount point or symbolic link on an NTFS file system.</summary>
      /// <param name="path">The path to the reparse point.</param>
      /// <returns>
      ///   An instance of <see cref="LinkTargetInfo"/> or <see cref="SymbolicLinkTargetInfo"/> containing information about the symbolic link
      ///   or mount point pointed to by <paramref name="path"/>.
      /// </returns>
      [SecurityCritical]
      public static LinkTargetInfo GetLinkTargetInfo(string path)
      {
         return GetLinkTargetInfoCore(null, path, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Gets information about the target of a mount point or symbolic link on an NTFS file system.</summary>
      /// <param name="path">The path to the reparse point.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      /// <returns>
      ///   An instance of <see cref="LinkTargetInfo"/> or <see cref="SymbolicLinkTargetInfo"/> containing information about the symbolic link
      ///   or mount point pointed to by <paramref name="path"/>.
      /// </returns>
      [SecurityCritical]
      public static LinkTargetInfo GetLinkTargetInfo(string path, PathFormat pathFormat)
      {
         return GetLinkTargetInfoCore(null, path, false, pathFormat);
      }
   }
}
