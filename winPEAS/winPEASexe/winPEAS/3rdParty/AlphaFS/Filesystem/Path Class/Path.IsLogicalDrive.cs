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

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>[AlphaFS] Checks if <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</summary>
      /// <returns>true when <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The absolute path to check.</param>
      public static bool IsLogicalDrive(string path)
      {
         return IsLogicalDriveCore(path, false, PathFormat.FullPath);
      }


      /// <summary>[AlphaFS] Checks if <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</summary>
      /// <returns>true when <paramref name="path"/> is in a logical drive format, such as "C:", "D:".</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The absolute path to check.</param>
      /// <param name="isRegularPath"><c>true</c> indicates the path is already a regular path.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      internal static bool IsLogicalDriveCore(string path, bool isRegularPath, PathFormat pathFormat)
      {
         if (pathFormat != PathFormat.LongFullPath)
         {
            if (Utils.IsNullOrWhiteSpace(path))
               throw new ArgumentNullException("path");

            CheckSupportedPathFormat(path, true, true);
         }


         if (!isRegularPath)
            path = GetRegularPathCore(path, GetFullPathOptions.None, false);

         var regularPath = path.StartsWith(LogicalDrivePrefix, StringComparison.OrdinalIgnoreCase) ? path.Substring(LogicalDrivePrefix.Length) : path;
         
         var c = regularPath.ToUpperInvariant()[0];

         // Don't use char.IsLetter() here as that can be misleading; The only valid drive letters are: A-Z.

         return regularPath[1] == VolumeSeparatorChar && c >= 'A' && c <= 'Z';
      }
   }
}
