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
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Associates a volume with a Drive letter or a directory on another volume.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="volumeMountPoint">
      ///   The user-mode path to be associated with the volume. This may be a Drive letter (for example, "X:\")
      ///   or a directory on another volume (for example, "Y:\MountX\").
      /// </param>
      /// <param name="volumeGuid">A <see cref="string"/> containing the volume <see cref="Guid"/>.</param>      
      [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "Utils.IsNullOrWhiteSpace validates arguments.")]
      [SecurityCritical]
      public static void SetVolumeMountPoint(string volumeMountPoint, string volumeGuid)
      {
         if (Utils.IsNullOrWhiteSpace(volumeMountPoint))
            throw new ArgumentNullException("volumeMountPoint");

         if (Utils.IsNullOrWhiteSpace(volumeGuid))
            throw new ArgumentNullException("volumeGuid");

         if (!volumeGuid.StartsWith(Path.VolumePrefix + "{", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(Resources.Not_A_Valid_Guid, "volumeGuid");


         volumeMountPoint = Path.GetFullPathCore(null, false, volumeMountPoint, GetFullPathOptions.AsLongPath | GetFullPathOptions.AddTrailingDirectorySeparator | GetFullPathOptions.FullCheck);


         // This string must be of the form "\\?\Volume{GUID}\"
         volumeGuid = Path.AddTrailingDirectorySeparator(volumeGuid, false);


         // ChangeErrorMode is for the Win32 SetThreadErrorMode() method, used to suppress possible pop-ups.
         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
         {
            // SetVolumeMountPoint()
            // 2014-01-29: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

            // The string must end with a trailing backslash.
            var success = NativeMethods.SetVolumeMountPoint(volumeMountPoint, volumeGuid);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
            {
               // If the lpszVolumeMountPoint parameter contains a path to a mounted folder,
               // GetLastError returns ERROR_DIR_NOT_EMPTY, even if the directory is empty.

               if (lastError != Win32Errors.ERROR_DIR_NOT_EMPTY)
                  NativeError.ThrowException(lastError, volumeGuid);
            }
         }
      }
   }
}
