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
using System.Security.Principal;

namespace Alphaleonis.Win32.Security
{
   /// <summary>
   /// This object is used to enable a specific privilege for the currently running process during its lifetime. 
   /// It should be disposed as soon as the elevated privilege is no longer needed.
   /// For more information see the documentation on AdjustTokenPrivileges on MSDN.
   /// </summary>
   internal sealed class InternalPrivilegeEnabler : IDisposable
   {
      /// <summary>Initializes a new instance of the <see cref="PrivilegeEnabler"/> class and enabling the specified privilege for the currently running process.</summary>
      /// <param name="privilegeName">The name of the privilege.</param>
      [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
      [SecurityCritical]
      public InternalPrivilegeEnabler(Privilege privilegeName)
      {
         if (null == privilegeName)
            throw new ArgumentNullException("privilegeName");

         EnabledPrivilege = privilegeName;
         AdjustPrivilege(true);
      }


      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// In this case the privilege previously enabled will be disabled.
      /// </summary>            
      public void Dispose()
      {
         try
         {
            if (null != EnabledPrivilege)
               AdjustPrivilege(false);
         }
         finally
         {
            EnabledPrivilege = null;
         }
      }


      public Privilege EnabledPrivilege { get; private set; }


      /// <summary>Adjusts the privilege.</summary>
      /// <param name="enable"><c>true</c> the privilege will be enabled, otherwise disabled.</param>
      [SecurityCritical]
      private void AdjustPrivilege(bool enable)
      {
         using (var currentIdentity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges))
         {
            uint length;
            var hToken = currentIdentity.Token;
            var mOldPrivilege = new TOKEN_PRIVILEGES();

            var newPrivilege = new TOKEN_PRIVILEGES
            {
               PrivilegeCount = 1,
               Luid = Filesystem.NativeMethods.LongToLuid(EnabledPrivilege.LookupLuid()),

               // 2 = SePrivilegeEnabled;
               Attributes = (uint) (enable ? 2 : 0)
            };


            var success = NativeMethods.AdjustTokenPrivileges(hToken, false, ref newPrivilege, (uint) Marshal.SizeOf(mOldPrivilege), out mOldPrivilege, out length);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError);


            // If no privilege was changed, we don't want to reset it.
            if (mOldPrivilege.PrivilegeCount == 0)
               EnabledPrivilege = null;
         }
      }
   }
}
