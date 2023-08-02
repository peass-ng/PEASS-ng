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
      /// <summary>[AlphaFS] Removes the directory junction.</summary>
      /// <para>&#160;</para>
      /// <remarks>
      /// <para>Only the directory junction is removed, not the target.</para>
      /// </remarks>
      /// <returns>A <see cref="DirectoryInfo"/> instance referencing the junction point.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      [SecurityCritical]
      public void DeleteJunction()
      {
         Directory.DeleteJunctionCore(Transaction, null, LongFullName, false, PathFormat.LongFullPath);

         RefreshEntryInfo();
      }


      /// <summary>[AlphaFS] Removes the directory junction.</summary>
      /// <para>&#160;</para>
      /// <remarks>
      /// <para>Only the directory junction is removed, not the target.</para>
      /// </remarks>
      /// <returns>A <see cref="DirectoryInfo"/> instance referencing the junction point.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="DirectoryNotFoundException"/>
      /// <exception cref="IOException"/>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="NotSupportedException"/>
      /// <exception cref="UnauthorizedAccessException"/>
      /// <param name="removeDirectory">When <c>true</c>, also removes the directory and all its contents.</param>
      [SecurityCritical]
      public void DeleteJunction(bool removeDirectory)
      {
         Directory.DeleteJunctionCore(Transaction, null, LongFullName, removeDirectory, PathFormat.LongFullPath);

         RefreshEntryInfo();
      }
   }
}
