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
   public static partial class Path
   {
      /// <summary>Determines if a path string is a valid Universal Naming Convention (UNC) path, optionally skip invalid path character check.</summary>
      /// <returns><c>true</c> if the specified path is a Universal Naming Convention (UNC) path, <c>false</c> otherwise.</returns>
      /// <param name="path">The path to check.</param>
      /// <param name="isRegularPath">When <c>true</c> indicates that <paramref name="path"/> is already in regular path format.</param>
      /// <param name="checkInvalidPathChars"><c>true</c> will check <paramref name="path"/> for invalid path characters.</param>
      [SecurityCritical]
      internal static bool IsUncPathCore(string path, bool isRegularPath, bool checkInvalidPathChars)
      {
         if (!isRegularPath)
            path = GetRegularPathCore(path, checkInvalidPathChars ? GetFullPathOptions.CheckInvalidPathChars : GetFullPathOptions.None, false);

         else if (checkInvalidPathChars)
            CheckInvalidPathChars(path, false, false);


         Uri uri;

         return Uri.TryCreate(path, UriKind.Absolute, out uri) && uri.IsUnc;
      }
   }
}
