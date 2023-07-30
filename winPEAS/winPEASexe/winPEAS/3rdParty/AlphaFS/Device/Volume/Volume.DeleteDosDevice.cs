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

using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Deletes an MS-DOS device name.</summary>
      /// <param name="deviceName">An MS-DOS device name specifying the device to delete.</param>      
      [SecurityCritical]
      public static void DeleteDosDevice(string deviceName)
      {
         DefineDosDeviceCore(false, deviceName, null, DosDeviceAttributes.RemoveDefinition, false);
      }

      /// <summary>[AlphaFS] Deletes an MS-DOS device name.</summary>
      /// <param name="deviceName">An MS-DOS device name string specifying the device to delete.</param>
      /// <param name="targetPath">
      ///   A pointer to a path string that will implement this device. The string is an MS-DOS path string unless the
      ///   <see cref="DosDeviceAttributes.RawTargetPath"/> flag is specified, in which case this string is a path string.
      /// </param>      
      [SecurityCritical]
      public static void DeleteDosDevice(string deviceName, string targetPath)
      {
         DefineDosDeviceCore(false, deviceName, targetPath, DosDeviceAttributes.RemoveDefinition, false);
      }

      /// <summary>[AlphaFS] Deletes an MS-DOS device name.</summary>
      /// <param name="deviceName">An MS-DOS device name string specifying the device to delete.</param>
      /// <param name="targetPath">
      ///   A pointer to a path string that will implement this device. The string is an MS-DOS path string unless the
      ///   <see cref="DosDeviceAttributes.RawTargetPath"/> flag is specified, in which case this string is a path string.
      /// </param>
      /// <param name="exactMatch">
      ///   Only delete MS-DOS device on an exact name match. If <paramref name="exactMatch"/> is <c>true</c>,
      ///   <paramref name="targetPath"/> must be the same path used to create the mapping.
      /// </param>      
      [SecurityCritical]
      public static void DeleteDosDevice(string deviceName, string targetPath, bool exactMatch)
      {
         DefineDosDeviceCore(false, deviceName, targetPath, DosDeviceAttributes.RemoveDefinition, exactMatch);
      }

      /// <summary>[AlphaFS] Deletes an MS-DOS device name.</summary>
      /// <param name="deviceName">An MS-DOS device name string specifying the device to delete.</param>
      /// <param name="targetPath">
      ///   A pointer to a path string that will implement this device. The string is an MS-DOS path string unless the
      ///   <see cref="DosDeviceAttributes.RawTargetPath"/> flag is specified, in which case this string is a path string.
      /// </param>
      /// <param name="deviceAttributes">
      ///   The controllable aspects of the DefineDosDevice function <see cref="DosDeviceAttributes"/> flags which will be combined with the
      ///   default.
      /// </param>
      /// <param name="exactMatch">
      ///   Only delete MS-DOS device on an exact name match. If <paramref name="exactMatch"/> is <c>true</c>,
      ///   <paramref name="targetPath"/> must be the same path used to create the mapping.
      /// </param>      
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      [SecurityCritical]
      public static void DeleteDosDevice(string deviceName, string targetPath, DosDeviceAttributes deviceAttributes, bool exactMatch)
      {
         DefineDosDeviceCore(false, deviceName, targetPath, deviceAttributes, exactMatch);
      }
   }
}
