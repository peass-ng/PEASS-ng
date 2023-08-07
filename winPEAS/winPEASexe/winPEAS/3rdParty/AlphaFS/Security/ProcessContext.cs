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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Security
{
   /// <summary>[AlphaFS] Class to determine the context of the current process.</summary>
   public static class ProcessContext
   {
      #region Properties

      /// <summary>[AlphaFS] Determines if the current process is run in the context of an Administrator.</summary>
      /// <returns><c>true</c> if the current process is run in the context of an Administrator; otherwise, <c>false</c>.</returns>
      public static bool IsAdministrator
      {
         get
         {
            WindowsIdentity windowsIdentity;
            var principal = GetWindowsPrincipal(out windowsIdentity);

            using (windowsIdentity)
               return

                  // Local Administrator.
                  principal.IsInRole(WindowsBuiltInRole.Administrator) ||

                  // Domain Administrator.
                  principal.IsInRole(512);
         }
      }


      /// <summary>[AlphaFS] Determines if UAC is enabled and that the current process is in an elevated state.
      /// <para>If the current User is the default Administrator then the process is assumed to be in an elevated state.</para>
      /// <para>This assumption is made because by default, the default Administrator (disabled by default) gets all access rights without showing an UAC prompt.</para>
      /// </summary>
      /// <returns><c>true</c> if UAC is enabled and the current process is in an elevated state; otherwise, <c>false</c>.</returns>
      public static bool IsElevatedProcess
      {
         get
         {
            return IsUacEnabled && (GetProcessElevationType() == NativeMethods.TOKEN_ELEVATION_TYPE.TokenElevationTypeFull || IsAdministrator);
         }
      }


      /// <summary>[AlphaFS] Determines if UAC is enabled by reading the "EnableLUA" registry key of the local Computer.</summary>
      /// <returns><c>true</c> if the UAC status was successfully read from registry; otherwise, <c>false</c>.</returns>
      [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uac")]
      public static bool IsUacEnabled
      {
         get
         {
            using (var uacKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", false))

               return null != uacKey && uacKey.GetValue("EnableLUA").Equals(1);
         }
      }


      /// <summary>[AlphaFS] Determines if the current process is run in the context of a Windows Service.</summary>
      /// <returns><c>true</c> if the current process is run in the context of a Windows Service; otherwise, <c>false</c>.</returns>
      public static bool IsWindowsService
      {
         get
         {
            WindowsIdentity windowsIdentity;
            var principal = GetWindowsPrincipal(out windowsIdentity);

            using (windowsIdentity)
               return principal.IsInRole(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null)) ||
                      principal.IsInRole(new SecurityIdentifier(WellKnownSidType.ServiceSid, null));
         }
      }

      #endregion // Properties


      private static WindowsPrincipal GetWindowsPrincipal(out WindowsIdentity windowsIdentity)
      {
         windowsIdentity = WindowsIdentity.GetCurrent();

         if (null == windowsIdentity)
            throw new InvalidOperationException(Resources.GetCurrentWindowsIdentityFailed);

         return new WindowsPrincipal(windowsIdentity);
      }


      /// <summary>[AlphaFS] Retrieves the elevation type of the current process.</summary>
      /// <returns>A <see cref="NativeMethods.TOKEN_ELEVATION_TYPE"/> value.</returns>
      [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "GetTokenInformation")]
      [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "OpenProcessToken")]
      private static NativeMethods.TOKEN_ELEVATION_TYPE GetProcessElevationType()
      {
         SafeTokenHandle tokenHandle;

         var success = NativeMethods.OpenProcessToken(Process.GetCurrentProcess().Handle, NativeMethods.TOKEN.TOKEN_READ, out tokenHandle);

         var lastError = Marshal.GetLastWin32Error();
         if (!success)
            throw new Win32Exception(lastError, string.Format(CultureInfo.CurrentCulture, "{0}: OpenProcessToken failed with error: {1}", MethodBase.GetCurrentMethod().Name, lastError.ToString(CultureInfo.CurrentCulture)));


         using (tokenHandle)
         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(Marshal.SizeOf(Enum.GetUnderlyingType(typeof(NativeMethods.TOKEN_ELEVATION_TYPE)))))
         {
            uint bytesReturned;
            success = NativeMethods.GetTokenInformation(tokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS.TokenElevationType, safeBuffer, (uint) safeBuffer.Capacity, out bytesReturned);

            lastError = Marshal.GetLastWin32Error();

            if (!success)
               throw new Win32Exception(lastError, string.Format(CultureInfo.CurrentCulture, "{0}: GetTokenInformation failed with error: {1}", MethodBase.GetCurrentMethod().Name, lastError.ToString(CultureInfo.CurrentCulture)));


            return (NativeMethods.TOKEN_ELEVATION_TYPE) safeBuffer.ReadInt32();
         }
      }
   }
}
