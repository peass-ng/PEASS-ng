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

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Defines, redefines, or deletes MS-DOS device names.</summary>
      /// <param name="deviceName">An MS-DOS device name string specifying the device the function is defining, redefining, or deleting.</param>
      /// <param name="targetPath">An MS-DOS path that will implement this device.</param>
      [SecurityCritical]
      public static void DefineDosDevice(string deviceName, string targetPath)
      {
         DefineDosDeviceCore(true, deviceName, targetPath, DosDeviceAttributes.None, false);
      }

      /// <summary>[AlphaFS] Defines, redefines, or deletes MS-DOS device names.</summary>
      /// <param name="deviceName">
      ///   An MS-DOS device name string specifying the device the function is defining, redefining, or deleting.
      /// </param>
      /// <param name="targetPath">
      ///   &gt;An MS-DOS path that will implement this device. If <paramref name="deviceAttributes"/> parameter has the
      ///   <see cref="DosDeviceAttributes.RawTargetPath"/> flag specified, <paramref name="targetPath"/> is used as-is.
      /// </param>
      /// <param name="deviceAttributes">
      ///   The controllable aspects of the DefineDosDevice function, <see cref="DosDeviceAttributes"/> flags which will be combined with the
      ///   default.
      /// </param>      
      [SecurityCritical]
      public static void DefineDosDevice(string deviceName, string targetPath, DosDeviceAttributes deviceAttributes)
      {
         DefineDosDeviceCore(true, deviceName, targetPath, deviceAttributes, false);
      }




      /// <summary>Defines, redefines, or deletes MS-DOS device names.</summary>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="isDefine">
      ///   <c>true</c> defines a new MS-DOS device. <c>false</c> deletes a previously defined MS-DOS device.
      /// </param>
      /// <param name="deviceName">
      ///   An MS-DOS device name string specifying the device the function is defining, redefining, or deleting.
      /// </param>
      /// <param name="targetPath">
      ///   A pointer to a path string that will implement this device. The string is an MS-DOS path string unless the
      ///   <see cref="DosDeviceAttributes.RawTargetPath"/> flag is specified, in which case this string is a path string.
      /// </param>
      /// <param name="deviceAttributes">
      ///   The controllable aspects of the DefineDosDevice function, <see cref="DosDeviceAttributes"/> flags which will be combined with the
      ///   default.
      /// </param>
      /// <param name="exactMatch">
      ///   Only delete MS-DOS device on an exact name match. If <paramref name="exactMatch"/> is <c>true</c>,
      ///   <paramref name="targetPath"/> must be the same path used to create the mapping.
      /// </param>
      [SecurityCritical]
      internal static void DefineDosDeviceCore(bool isDefine, string deviceName, string targetPath, DosDeviceAttributes deviceAttributes, bool exactMatch)
      {
         if (Utils.IsNullOrWhiteSpace(deviceName))
            throw new ArgumentNullException("deviceName");

         if (isDefine)
         {
            // targetPath is allowed to be null.

            // In no case is a trailing backslash ("\") allowed.
            deviceName = Path.GetRegularPathCore(deviceName, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.CheckInvalidPathChars, false);

            using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
            {
               var success = NativeMethods.DefineDosDevice(deviceAttributes, deviceName, targetPath);

               var lastError = Marshal.GetLastWin32Error();
               if (!success)
                  NativeError.ThrowException(lastError, deviceName, targetPath);
            }
         }

         else
         {
            // A pointer to a path string that will implement this device.
            // The string is an MS-DOS path string unless the DDD_RAW_TARGET_PATH flag is specified, in which case this string is a path string.

            if (exactMatch && !Utils.IsNullOrWhiteSpace(targetPath))
               deviceAttributes = deviceAttributes | DosDeviceAttributes.ExactMatchOnRemove | DosDeviceAttributes.RawTargetPath;

            // Remove the MS-DOS device name. First, get the name of the Windows NT device
            // from the symbolic link and then delete the symbolic link from the namespace.

            DefineDosDevice(deviceName, targetPath, deviceAttributes);
         }
      }
   }
}
