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
      /// <summary>Gets the regular path from a long path.</summary>
      /// <returns>
      ///   Returns the regular form of a long <paramref name="path"/>.
      ///   For example: "\\?\C:\Temp\file.txt" to: "C:\Temp\file.txt", or: "\\?\UNC\Server\share\file.txt" to: "\\Server\share\file.txt".
      /// </returns>
      /// <remarks>
      ///   MSDN: String.TrimEnd Method notes to Callers: http://msdn.microsoft.com/en-us/library/system.string.trimend%28v=vs.110%29.aspx
      /// </remarks>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path.</param>
      /// <param name="options">Options for controlling the full path retrieval.</param>
      /// <param name="allowEmpty">When <c>false</c>, throws an <see cref="ArgumentException"/>.</param>
      [SecurityCritical]
      internal static string GetRegularPathCore(string path, GetFullPathOptions options, bool allowEmpty)
      {
         if (null == path)
            throw new ArgumentNullException("path");

         if (!allowEmpty && (path.Trim().Length == 0 || Utils.IsNullOrWhiteSpace(path)))
            throw new ArgumentException(Resources.Path_Is_Zero_Length_Or_Only_White_Space, "path");

         if (options != GetFullPathOptions.None)
            path = ApplyFullPathOptions(path, options);


         if (path.StartsWith(DosDeviceUncPrefix, StringComparison.OrdinalIgnoreCase))
            return UncPrefix + path.Substring(DosDeviceUncPrefix.Length);


         if (path.StartsWith(LogicalDrivePrefix, StringComparison.Ordinal))
            return path.Substring(LogicalDrivePrefix.Length);


         if (path.StartsWith(NonInterpretedPathPrefix, StringComparison.Ordinal))
            return path.Substring(NonInterpretedPathPrefix.Length);


         return path.StartsWith(GlobalRootPrefix, StringComparison.OrdinalIgnoreCase) || path.StartsWith(VolumePrefix, StringComparison.OrdinalIgnoreCase) ||
                !path.StartsWith(LongPathPrefix, StringComparison.Ordinal)

            ? path
            : (path.StartsWith(LongPathUncPrefix, StringComparison.OrdinalIgnoreCase) ? UncPrefix + path.Substring(LongPathUncPrefix.Length) : path.Substring(LongPathPrefix.Length));
      }
   }
}
