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
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Retrieves the volume mount point where the specified path is mounted.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to the volume, for example: "C:\Windows".</param>
      /// <returns>
      ///   <para>Returns the nearest volume root path for a given directory.</para>
      ///   <para>The volume path name, for example: "C:\Windows" returns: "C:\".</para>
      /// </returns>
      [SecurityCritical]
      public static string GetVolumePathName(string path)
      {
         if (Utils.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException("path");


         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
         {
            var volumeRootPath = new StringBuilder(NativeMethods.MaxPathUnicode / 32);
            var pathLp = Path.GetFullPathCore(null, false, path, GetFullPathOptions.AsLongPath | GetFullPathOptions.FullCheck);


            // GetVolumePathName()
            // 2013-07-18: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

            var success = NativeMethods.GetVolumePathName(pathLp, volumeRootPath, (uint) volumeRootPath.Capacity);

            var lastError = Marshal.GetLastWin32Error();

            if (success)
               return Path.GetRegularPathCore(volumeRootPath.ToString(), GetFullPathOptions.None, false);


            switch ((uint) lastError)
            {
               // Don't throw exception on these errors.
               case Win32Errors.ERROR_NO_MORE_FILES:
               case Win32Errors.ERROR_INVALID_PARAMETER:
               case Win32Errors.ERROR_INVALID_NAME:
                  break;

               default:
                  NativeError.ThrowException(lastError, path);
                  break;
            }


            // Return original path.
            return path;
         }
      }
   }
}
