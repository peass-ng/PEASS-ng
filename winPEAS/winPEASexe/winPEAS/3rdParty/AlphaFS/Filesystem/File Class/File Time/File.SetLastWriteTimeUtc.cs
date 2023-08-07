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
   public static partial class File
   {
      /// <summary>Sets the date and time, in coordinated universal time (UTC), that the specified file was last written to.</summary>
      /// <param name="path">The file for which to set the date and time information.</param>
      /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
      [SecurityCritical]
      public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
      {
         SetFsoDateTimeCore(null, false, path, null, null, lastWriteTimeUtc, false, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Sets the date and time, in coordinated universal time (UTC), that the specified file was last written to.</summary>
      /// <param name="path">The file for which to set the date and time information.</param>
      /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>      
      [SecurityCritical]
      public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc, PathFormat pathFormat)
      {
         SetFsoDateTimeCore(null, false, path, null, null, lastWriteTimeUtc, false, pathFormat);
      }


      /// <summary>[AlphaFS] Sets the date and time, in coordinated universal time (UTC), that the specified file was last written to.</summary>
      /// <param name="path">The file for which to set the date and time information.</param>
      /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the file or directory linked to. No effect if <paramref name="path"/> does not refer to a reparse point.</param>
      [SecurityCritical]
      public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc, bool modifyReparsePoint)
      {
         SetFsoDateTimeCore(null, false, path, null, null, lastWriteTimeUtc, modifyReparsePoint, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Sets the date and time, in coordinated universal time (UTC), that the specified file was last written to.</summary>
      /// <param name="path">The file for which to set the date and time information.</param>
      /// <param name="lastWriteTimeUtc">A <see cref="DateTime"/> containing the value to set for the last write date and time of <paramref name="path"/>. This value is expressed in UTC time.</param>
      /// <param name="modifyReparsePoint">If <c>true</c>, the date and time information will apply to the reparse point (symlink or junction) and not the file or directory linked to. No effect if <paramref name="path"/> does not refer to a reparse point.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>      
      [SecurityCritical]
      public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc, bool modifyReparsePoint, PathFormat pathFormat)
      {
         SetFsoDateTimeCore(null, false, path, null, null, lastWriteTimeUtc, modifyReparsePoint, pathFormat);
      }
   }
}
