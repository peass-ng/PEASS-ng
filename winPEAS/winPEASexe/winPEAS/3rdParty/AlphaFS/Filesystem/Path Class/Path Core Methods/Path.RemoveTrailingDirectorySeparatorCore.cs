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
   public static partial class Path
   {
      /// <summary>Removes the trailing <see cref="DirectorySeparatorChar"/> or <see cref="AltDirectorySeparatorChar"/> character from the string, when present.</summary>
      /// <returns>A text string where the trailing <see cref="DirectorySeparatorChar"/> or <see cref="AltDirectorySeparatorChar"/> character has been removed. The function returns <c>null</c> when <paramref name="path"/> is <c>null</c>.</returns>
      /// <param name="path">A text string from which the trailing <see cref="DirectorySeparatorChar"/> or <see cref="AltDirectorySeparatorChar"/> is to be removed, when present.</param>
      /// <param name="removeAlternateSeparator">If <c>true</c> the trailing <see cref="AltDirectorySeparatorChar"/> character will be removed instead.</param>
      [SecurityCritical]
      internal static string RemoveTrailingDirectorySeparatorCore(string path, bool removeAlternateSeparator)
      {
         return null != path ? path.TrimEnd(removeAlternateSeparator ? AltDirectorySeparatorChar : DirectorySeparatorChar) : null;
      }
   }
}
