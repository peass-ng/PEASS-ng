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
      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing files.</summary>
      /// <remarks>This method does not change last access time for the source file.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="sourcePath">The source file to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination file to set the date and time stamps.</param>
      [SecurityCritical]
      public static void CopyTimestampsTransacted(KernelTransaction transaction, string sourcePath, string destinationPath)
      {
         CopyTimestampsCore(transaction, false, sourcePath, destinationPath, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing files.</summary>
      /// <remarks>This method does not change last access time for the source file.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="sourcePath">The source file to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination file to set the date and time stamps.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CopyTimestampsTransacted(KernelTransaction transaction, string sourcePath, string destinationPath, PathFormat pathFormat)
      {
         CopyTimestampsCore(transaction, false, sourcePath, destinationPath, false, pathFormat);
      }


      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing files.</summary>
      /// <remarks>This method does not change last access time for the source file.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="sourcePath">The source file to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination file to set the date and time stamps.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the file linked to. No effect if <paramref name="destinationPath"/> does not refer to a reparse point.</param>
      [SecurityCritical]
      public static void CopyTimestampsTransacted(KernelTransaction transaction, string sourcePath, string destinationPath, bool modifyReparsePoint)
      {
         CopyTimestampsCore(transaction, false, sourcePath, destinationPath, modifyReparsePoint, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Copies the date and timestamps for the specified existing files.</summary>
      /// <remarks>This method does not change last access time for the source file.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="FileNotFoundException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="transaction">The transaction.</param>
      /// <param name="sourcePath">The source file to get the date and time stamps from.</param>
      /// <param name="destinationPath">The destination file to set the date and time stamps.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the file linked to. No effect if <paramref name="destinationPath"/> does not refer to a reparse point.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void CopyTimestampsTransacted(KernelTransaction transaction, string sourcePath, string destinationPath, bool modifyReparsePoint, PathFormat pathFormat)
      {
         CopyTimestampsCore(transaction, false, sourcePath, destinationPath, modifyReparsePoint, pathFormat);
      }
   }
}
