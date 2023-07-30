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
using System.Globalization;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>Adds a trailing <see cref="DirectorySeparatorChar"/> or <see cref="AltDirectorySeparatorChar"/> character to the string, when absent.</summary>
      /// <returns>A text string with a trailing <see cref="DirectorySeparatorChar"/> or <see cref="AltDirectorySeparatorChar"/> character. The function returns <c>null</c> when <paramref name="path"/> is <c>null</c>.</returns>
      /// <param name="path">A text string to which the trailing <see cref="DirectorySeparatorChar"/> or <see cref="AltDirectorySeparatorChar"/> is to be added, when absent.</param>
      /// <param name="addAlternateSeparator">If <c>true</c> the <see cref="AltDirectorySeparatorChar"/> character will be added instead.</param>
      [SecurityCritical]
      internal static string AddTrailingDirectorySeparatorCore(string path, bool addAlternateSeparator)
      {
         return null == path

            ? null

            : (addAlternateSeparator ? (!path.EndsWith(AltDirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ? path + AltDirectorySeparatorChar : path)

               : (!path.EndsWith(DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ? path + DirectorySeparatorChar : path));
      }
   }
}
