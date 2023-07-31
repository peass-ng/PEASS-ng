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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Gets the shortest display name for the specified <paramref name="volumeName"/>.</summary>
      /// <remarks>This method basically returns the shortest string returned by <see cref="EnumerateVolumePathNames"/></remarks>
      /// <param name="volumeName">A volume <see cref="Guid"/> path: \\?\Volume{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}\.</param>
      /// <returns>
      ///   The shortest display name for the specified volume found, or <c>null</c> if no display names were found.
      /// </returns>
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      [SecurityCritical]
      public static string GetVolumeDisplayName(string volumeName)
      {
         string[] smallestMountPoint = { new string(Path.WildcardStarMatchAllChar, NativeMethods.MaxPathUnicode) };

         try
         {
            foreach (var m in EnumerateVolumePathNames(volumeName).Where(m => !Utils.IsNullOrWhiteSpace(m) && m.Length < smallestMountPoint[0].Length))
               smallestMountPoint[0] = m;
         }
         catch
         {
         }

         var result = smallestMountPoint[0][0] == Path.WildcardStarMatchAllChar ? null : smallestMountPoint[0];
         return Utils.IsNullOrWhiteSpace(result) ? null : result;
      }
   }
}
