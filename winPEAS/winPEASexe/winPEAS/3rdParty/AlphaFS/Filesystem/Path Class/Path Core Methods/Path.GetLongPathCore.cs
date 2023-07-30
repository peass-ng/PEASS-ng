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
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>Makes an extended long path from the specified <paramref name="path"/> by prefixing <see cref="LongPathPrefix"/>.</summary>
      /// <returns>The <paramref name="path"/> prefixed with a <see cref="LongPathPrefix"/>, the minimum required full path is: "C:\".</returns>
      /// <remarks>This method does not verify that the resulting path and file name are valid, or that they see an existing file on the associated volume.</remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to the file or directory, this can also be an UNC path.</param>
      /// <param name="options">Options for controlling the full path retrieval.</param>
      [SecurityCritical]
      internal static string GetLongPathCore(string path, GetFullPathOptions options)
      {
         if (null == path)
            throw new ArgumentNullException("path");

         if (path.Trim().Length == 0)
            throw new ArgumentException(Resources.Path_Is_Zero_Length_Or_Only_White_Space, "path");


         if (options != GetFullPathOptions.None)
            path = ApplyFullPathOptions(path, options);


         // ".", "C:"
         if (path.Length <= 2 || path.StartsWith(LongPathPrefix, StringComparison.Ordinal) || path.StartsWith(LogicalDrivePrefix, StringComparison.Ordinal) || path.StartsWith(NonInterpretedPathPrefix, StringComparison.Ordinal))
            return path;


         if (path.StartsWith(UncPrefix, StringComparison.Ordinal))
            return LongPathUncPrefix + path.Substring(UncPrefix.Length);


         return IsPathRooted(path, false) && IsLogicalDriveCore(path, false, PathFormat.LongFullPath) ? LongPathPrefix + path : path;
      }
   }
}
