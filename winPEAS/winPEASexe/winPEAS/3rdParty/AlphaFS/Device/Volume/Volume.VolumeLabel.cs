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
      /// <summary>[AlphaFS] Deletes the label of the file system volume that is the root of the current directory.</summary>
      [SecurityCritical]
      public static void DeleteCurrentVolumeLabel()
      {
         SetVolumeLabel(null, null);
      }


      /// <summary>[AlphaFS] Deletes the label of a file system volume.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="rootPathName">The root directory of a file system volume. This is the volume the function will remove the label.</param>
      [SecurityCritical]
      public static void DeleteVolumeLabel(string rootPathName)
      {
         if (Utils.IsNullOrWhiteSpace(rootPathName))
            throw new ArgumentNullException("rootPathName");


         SetVolumeLabel(rootPathName, null);
      }


      /// <summary>[AlphaFS] Retrieve the label of a file system volume.</summary>
      /// <param name="volumePath">
      ///   A path to a volume. For example: "C:\", "\\server\share", or "\\?\Volume{c0580d5e-2ad6-11dc-9924-806e6f6e6963}\".
      /// </param>
      /// <returns>The the label of the file system volume. This function can return <c>string.Empty</c> since a volume label is generally not mandatory.</returns>
      [SecurityCritical]
      public static string GetVolumeLabel(string volumePath)
      {
         return new VolumeInfo(volumePath, true, true).Name;
      }


      /// <summary>[AlphaFS] Sets the label of the file system volume that is the root of the current directory.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="volumeName">A name for the volume.</param>
      [SecurityCritical]
      public static void SetCurrentVolumeLabel(string volumeName)
      {
         if (Utils.IsNullOrWhiteSpace(volumeName))
            throw new ArgumentNullException("volumeName");


         var success = NativeMethods.SetVolumeLabel(null, volumeName);

         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            NativeError.ThrowException(lastError, volumeName);
      }


      /// <summary>[AlphaFS] Sets the label of a file system volume.</summary>
      /// <param name="volumePath">
      ///   <para>A path to a volume. For example: "C:\", "\\server\share", or "\\?\Volume{c0580d5e-2ad6-11dc-9924-806e6f6e6963}\"</para>
      ///   <para>If this parameter is <c>null</c>, the function uses the current drive.</para>
      /// </param>
      /// <param name="volumeName">
      ///   <para>A name for the volume.</para>
      ///   <para>If this parameter is <c>null</c>, the function deletes any existing label</para>
      ///   <para>from the specified volume and does not assign a new label.</para>
      /// </param>
      [SecurityCritical]
      public static void SetVolumeLabel(string volumePath, string volumeName)
      {
         // rootPathName == null is allowed, means current drive.

         // Setting volume label only applies to Logical Drives pointing to local resources.
         //if (!Path.IsLocalPath(rootPathName))
         //return false;

         volumePath = Path.AddTrailingDirectorySeparator(volumePath, false);

         // NTFS uses a limit of 32 characters for the volume label as of Windows Server 2003.
         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
         {
            var success = NativeMethods.SetVolumeLabel(volumePath, volumeName);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError, volumePath, volumeName);
         }
      }
   }
}
