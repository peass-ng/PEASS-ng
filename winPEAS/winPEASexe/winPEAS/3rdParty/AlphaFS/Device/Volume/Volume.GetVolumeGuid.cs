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
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] 
      ///   Retrieves a volume <see cref="Guid"/> path for the volume that is associated with the specified volume mount point (drive letter,
      ///   volume GUID path, or mounted folder).
      /// </summary>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="volumeMountPoint">
      ///   The path of a mounted folder (for example, "Y:\MountX\") or a drive letter (for example, "X:\").
      /// </param>
      /// <returns>The unique volume name of the form: "\\?\Volume{GUID}\".</returns>
      [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke", Justification = "Marshal.GetLastWin32Error() is manipulated.")]
      [SecurityCritical]
      public static string GetVolumeGuid(string volumeMountPoint)
      {
         if (Utils.IsNullOrWhiteSpace(volumeMountPoint))
            throw new ArgumentNullException("volumeMountPoint");

         // The string must end with a trailing backslash ('\').
         volumeMountPoint = Path.GetFullPathCore(null, false, volumeMountPoint, GetFullPathOptions.AsLongPath | GetFullPathOptions.AddTrailingDirectorySeparator | GetFullPathOptions.FullCheck);

         var volumeGuid = new StringBuilder(100);
         var uniqueName = new StringBuilder(100);

         try
         {
            using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
            {
               // GetVolumeNameForVolumeMountPoint()
               // 2013-07-18: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

               return NativeMethods.GetVolumeNameForVolumeMountPoint(volumeMountPoint, volumeGuid, (uint)volumeGuid.Capacity)

                  // The string must end with a trailing backslash.
                  ? NativeMethods.GetVolumeNameForVolumeMountPoint(Path.AddTrailingDirectorySeparator(volumeGuid.ToString(), false), uniqueName, (uint)uniqueName.Capacity)
                     ? uniqueName.ToString()
                     : null

                  : null;
            }
         }
         finally
         {
            var lastError = (uint) Marshal.GetLastWin32Error();

            switch (lastError)
            {
               case Win32Errors.ERROR_MORE_DATA:
                  // (1) When GetVolumeNameForVolumeMountPoint() succeeds, lastError is set to Win32Errors.ERROR_MORE_DATA.
                  break;

               default:
                  // (2) When volumeMountPoint is a network drive mapping or UNC path, lastError is set to Win32Errors.ERROR_INVALID_PARAMETER.

                  // Throw IOException.
                  NativeError.ThrowException(lastError, volumeMountPoint);
                  break;
            }
         }
      }
   }
}
