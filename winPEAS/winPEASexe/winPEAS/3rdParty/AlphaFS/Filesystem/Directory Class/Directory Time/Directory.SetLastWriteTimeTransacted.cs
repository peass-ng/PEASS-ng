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
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Sets the date and time that the specified directory was last written to.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The directory for which to set the date and time information.</param>
      /// <param name="lastWriteTime">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in local time.</param>
      [SecurityCritical]
      public static void SetLastWriteTimeTransacted(KernelTransaction transaction, string path, DateTime lastWriteTime)
      {
         File.SetFsoDateTimeCore(transaction, false, path, null, null, lastWriteTime.ToUniversalTime(), false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Sets the date and time that the specified directory was last written to.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The directory for which to set the date and time information.</param>
      /// <param name="lastWriteTime">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in local time.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void SetLastWriteTimeTransacted(KernelTransaction transaction, string path, DateTime lastWriteTime, PathFormat pathFormat)
      {
         File.SetFsoDateTimeCore(transaction, false, path, null, null, lastWriteTime.ToUniversalTime(), false, pathFormat);
      }


      /// <summary>[AlphaFS] Sets the date and time that the specified directory was last written to.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The directory for which to set the date and time information.</param>
      /// <param name="lastWriteTime">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in local time.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the file or directory linked to. No effect if <paramref name="path"/> does not refer to a reparse point.</param>
      [SecurityCritical]
      public static void SetLastWriteTimeTransacted(KernelTransaction transaction, string path, DateTime lastWriteTime, bool modifyReparsePoint)
      {
         File.SetFsoDateTimeCore(transaction, false, path, null, null, lastWriteTime.ToUniversalTime(), modifyReparsePoint, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Sets the date and time that the specified directory was last written to.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="path">The directory for which to set the date and time information.</param>
      /// <param name="lastWriteTime">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in local time.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the file or directory linked to. No effect if <paramref name="path"/> does not refer to a reparse point.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static void SetLastWriteTimeTransacted(KernelTransaction transaction, string path, DateTime lastWriteTime, bool modifyReparsePoint, PathFormat pathFormat)
      {
         File.SetFsoDateTimeCore(transaction, false, path, null, null, lastWriteTime.ToUniversalTime(), modifyReparsePoint, pathFormat);
      }
   }
}
