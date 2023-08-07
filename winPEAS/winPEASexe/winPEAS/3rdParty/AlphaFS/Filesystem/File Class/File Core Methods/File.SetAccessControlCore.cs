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
using System.Security.AccessControl;
using Alphaleonis.Win32.Security;
using Microsoft.Win32.SafeHandles;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>[AlphaFS] Applies access control list (ACL) entries described by a <see cref="FileSecurity"/>/<see cref="DirectorySecurity"/> object to the specified file or directory.</summary>
      /// <remarks>Use either <paramref name="path"/> or <paramref name="handle"/>, not both.</remarks>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">A file or directory to add or remove access control list (ACL) entries from. This parameter This parameter may be <c>null</c>.</param>
      /// <param name="handle">A <see cref="SafeFileHandle"/> to add or remove access control list (ACL) entries from. This parameter This parameter may be <c>null</c>.</param>
      /// <param name="objectSecurity">A <see cref="FileSecurity"/>/<see cref="DirectorySecurity"/> object that describes an ACL entry to apply to the file or directory described by the <paramref name="path"/>/<paramref name="handle"/> parameter.</param>
      /// <param name="includeSections">One or more of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to set.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
      [SecurityCritical]
      internal static void SetAccessControlCore(string path, SafeFileHandle handle, ObjectSecurity objectSecurity, AccessControlSections includeSections, PathFormat pathFormat)
      {
         if (pathFormat == PathFormat.RelativePath)
            Path.CheckSupportedPathFormat(path, true, true);

         if (objectSecurity == null)
            throw new ArgumentNullException("objectSecurity");


         var managedDescriptor = objectSecurity.GetSecurityDescriptorBinaryForm();

         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(managedDescriptor.Length))
         {
            var pathLp = Path.GetExtendedLengthPathCore(null, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.CheckInvalidPathChars);

            safeBuffer.CopyFrom(managedDescriptor, 0, managedDescriptor.Length);

            SECURITY_DESCRIPTOR_CONTROL control;
            uint revision;


            var success = Security.NativeMethods.GetSecurityDescriptorControl(safeBuffer, out control, out revision);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError, pathLp);


            PrivilegeEnabler privilegeEnabler = null;

            try
            {
               var securityInfo = SECURITY_INFORMATION.None;
               var pDacl = IntPtr.Zero;

               if ((includeSections & AccessControlSections.Access) != 0)
               {
                  bool daclDefaulted, daclPresent;


                  success = Security.NativeMethods.GetSecurityDescriptorDacl(safeBuffer, out daclPresent, out pDacl, out daclDefaulted);

                  lastError = Marshal.GetLastWin32Error();
                  if (!success)
                     NativeError.ThrowException(lastError, pathLp);


                  if (daclPresent)
                  {
                     securityInfo |= SECURITY_INFORMATION.DACL_SECURITY_INFORMATION;
                     securityInfo |= (control & SECURITY_DESCRIPTOR_CONTROL.SE_DACL_PROTECTED) != 0 ? SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION : SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION;
                  }
               }


               var pSacl = IntPtr.Zero;

               if ((includeSections & AccessControlSections.Audit) != 0)
               {
                  bool saclDefaulted, saclPresent;


                  success = Security.NativeMethods.GetSecurityDescriptorSacl(safeBuffer, out saclPresent, out pSacl, out saclDefaulted);

                  lastError = Marshal.GetLastWin32Error();
                  if (!success)
                     NativeError.ThrowException(lastError, pathLp);
                  
                  
                  if (saclPresent)
                  {
                     securityInfo |= SECURITY_INFORMATION.SACL_SECURITY_INFORMATION;
                     securityInfo |= (control & SECURITY_DESCRIPTOR_CONTROL.SE_SACL_PROTECTED) != 0 ? SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION : SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION;

                     privilegeEnabler = new PrivilegeEnabler(Privilege.Security);
                  }
               }


               var pOwner = IntPtr.Zero;

               if ((includeSections & AccessControlSections.Owner) != 0)
               {
                  bool ownerDefaulted;


                  success = Security.NativeMethods.GetSecurityDescriptorOwner(safeBuffer, out pOwner, out ownerDefaulted);

                  lastError = Marshal.GetLastWin32Error();
                  if (!success)
                     NativeError.ThrowException(lastError, pathLp);


                  if (pOwner != IntPtr.Zero)
                     securityInfo |= SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION;
               }


               var pGroup = IntPtr.Zero;

               if ((includeSections & AccessControlSections.Group) != 0)
               {
                  bool groupDefaulted;


                  success = Security.NativeMethods.GetSecurityDescriptorGroup(safeBuffer, out pGroup, out groupDefaulted);

                  lastError = Marshal.GetLastWin32Error();
                  if (!success)
                     NativeError.ThrowException(lastError, pathLp);


                  if (pGroup != IntPtr.Zero)
                     securityInfo |= SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION;
               }




               if (!Utils.IsNullOrWhiteSpace(pathLp))
               {
                  // SetNamedSecurityInfo()
                  // 2013-01-13: MSDN does not confirm LongPath usage but a Unicode version of this function exists.

                  lastError = (int) Security.NativeMethods.SetNamedSecurityInfo(pathLp, SE_OBJECT_TYPE.SE_FILE_OBJECT, securityInfo, pOwner, pGroup, pDacl, pSacl);

                  if (lastError != Win32Errors.ERROR_SUCCESS)
                     NativeError.ThrowException(lastError, pathLp);
               }

               else
               {
                  if (NativeMethods.IsValidHandle(handle))
                  {
                     lastError = (int) Security.NativeMethods.SetSecurityInfo(handle, SE_OBJECT_TYPE.SE_FILE_OBJECT, securityInfo, pOwner, pGroup, pDacl, pSacl);

                     if (lastError != Win32Errors.ERROR_SUCCESS)
                        NativeError.ThrowException(lastError);
                  }
               }
            }
            finally
            {
               if (null != privilegeEnabler)
                  privilegeEnabler.Dispose();
            }
         }
      }
   }
}
