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
using System.IO;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Volume
   {
      /// <summary>[AlphaFS] Determines the disk <see cref="DriveType"/>, based on the root of the current directory.</summary>
      /// <returns>A <see cref="DriveType"/> enum value.</returns>
      [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
      [SecurityCritical]
      public static DriveType GetCurrentDriveType()
      {
         return GetDriveType(null);
      }


      /// <summary>[AlphaFS] Determines the disk <see cref="DriveType"/>.</summary>
      /// <param name="drivePath">A path to a drive. For example: "C:\", "\\server\share", or "\\?\Volume{c0580d5e-2ad6-11dc-9924-806e6f6e6963}\"</param>
      /// <returns>A <see cref="DriveType"/> enum value.</returns>
      [SecurityCritical]
      public static DriveType GetDriveType(string drivePath)
      {
         // drivePath is allowed to be == null.

         drivePath = Path.AddTrailingDirectorySeparator(drivePath, false);


         // ChangeErrorMode is for the Win32 SetThreadErrorMode() method, used to suppress possible pop-ups. 

         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))

            return NativeMethods.GetDriveType(drivePath);
      }
   }
}
