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
using Microsoft.Win32.SafeHandles;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Retrieves information about the file system and volume associated with the specified root file or directorystream.</summary>
      /// <param name="volumePath">A path that contains the root directory.</param>
      /// <returns>A <see cref="VolumeInfo"/> instance describing the volume associatied with the specified root directory.</returns>
      [SecurityCritical]
      public static VolumeInfo GetVolumeInfo(string volumePath)
      {
         return new VolumeInfo(volumePath, true, false);
      }


      /// <summary>[AlphaFS] Retrieves information about the file system and volume associated with the specified root file or directorystream.</summary>
      /// <param name="volumeHandle">An instance to a <see cref="SafeFileHandle"/> handle.</param>
      /// <returns>A <see cref="VolumeInfo"/> instance describing the volume associatied with the specified root directory.</returns>
      [SecurityCritical]
      public static VolumeInfo GetVolumeInfo(SafeFileHandle volumeHandle)
      {
         return new VolumeInfo(volumeHandle, true, true);
      }
   }
}
