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
using System.IO;
using System.Security;
using System.Security.AccessControl;
using Alphaleonis.Win32.Security;
using Microsoft.Win32.SafeHandles;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      /// <summary>Gets an <see cref="ObjectSecurity"/> object for a particular file or directory.</summary>
      /// <returns>An <see cref="ObjectSecurity"/> object that encapsulates the access control rules for the file or directory described by the <paramref name="path"/> parameter.</returns>
      /// <exception cref="IOException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <typeparam name="T">Generic type parameter.</typeparam>
      /// <param name="isFolder">Specifies that <paramref name="path"/> is a file or directory.</param>
      /// <param name="path">The path to a file or directory containing a <see cref="FileSecurity"/>/<see cref="DirectorySecurity"/> object that describes the file's/directory's access control list (ACL) information.</param>
      /// <param name="includeSections">One (or more) of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to receive.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Disposing is controlled.")]
      [SecurityCritical]
      internal static T GetAccessControlCore<T>(bool isFolder, string path, AccessControlSections includeSections, PathFormat pathFormat)
      {
         var securityInfo = CreateSecurityInformation(includeSections);


         // We need the SE_SECURITY_NAME privilege enabled to be able to get the SACL descriptor.
         // So we enable it here for the remainder of this function.

         PrivilegeEnabler privilege = null;

         if ((includeSections & AccessControlSections.Audit) != 0)
            privilege = new PrivilegeEnabler(Privilege.Security);

         using (privilege)
         {
            IntPtr pSidOwner, pSidGroup, pDacl, pSacl;
            SafeGlobalMemoryBufferHandle pSecurityDescriptor;

            var pathLp = Path.GetExtendedLengthPathCore(null, path, pathFormat, GetFullPathOptions.RemoveTrailingDirectorySeparator | GetFullPathOptions.FullCheck);


            // Get/SetNamedSecurityInfo does not work with a handle but with a path, hence does not honor the privileges.
            // It magically does since Windows Server 2012 / 8 but not in previous OS versions.

            var lastError = Security.NativeMethods.GetNamedSecurityInfo(pathLp, SE_OBJECT_TYPE.SE_FILE_OBJECT, securityInfo, out pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);


            // When GetNamedSecurityInfo() fails with ACCESS_DENIED, try again using GetSecurityInfo().

            if (lastError == Win32Errors.ERROR_ACCESS_DENIED)
            {
               using (var handle = CreateFileCore(null, false, pathLp, ExtendedFileAttributes.BackupSemantics, null, FileMode.Open, FileSystemRights.Read, FileShare.Read, false, false, PathFormat.LongFullPath))

                  return GetAccessControlHandleCore<T>(true, isFolder, handle, includeSections, securityInfo);
            }

            return GetSecurityDescriptor<T>(lastError, isFolder, pathLp, pSecurityDescriptor);
         }
      }


      internal static T GetAccessControlHandleCore<T>(bool internalCall, bool isFolder, SafeFileHandle handle, AccessControlSections includeSections, SECURITY_INFORMATION securityInfo)
      {
         if (!internalCall)
            securityInfo = CreateSecurityInformation(includeSections);


         // We need the SE_SECURITY_NAME privilege enabled to be able to get the SACL descriptor.
         // So we enable it here for the remainder of this function.
         
         PrivilegeEnabler privilege = null;

         if (!internalCall && (includeSections & AccessControlSections.Audit) != 0)
            privilege = new PrivilegeEnabler(Privilege.Security);
         
         using (privilege)
         {
            IntPtr pSidOwner, pSidGroup, pDacl, pSacl;
            SafeGlobalMemoryBufferHandle pSecurityDescriptor;

            var lastError = Security.NativeMethods.GetSecurityInfo(handle, SE_OBJECT_TYPE.SE_FILE_OBJECT, securityInfo, out pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);

            return GetSecurityDescriptor<T>(lastError, isFolder, null, pSecurityDescriptor);
         }
      }


      private static SECURITY_INFORMATION CreateSecurityInformation(AccessControlSections includeSections)
      {
         var securityInfo = SECURITY_INFORMATION.None;


         if ((includeSections & AccessControlSections.Access) != 0)
            securityInfo |= SECURITY_INFORMATION.DACL_SECURITY_INFORMATION;

         if ((includeSections & AccessControlSections.Audit) != 0)
            securityInfo |= SECURITY_INFORMATION.SACL_SECURITY_INFORMATION;

         if ((includeSections & AccessControlSections.Group) != 0)
            securityInfo |= SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION;

         if ((includeSections & AccessControlSections.Owner) != 0)
            securityInfo |= SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION;
         

         return securityInfo;
      }


      private static T GetSecurityDescriptor<T>(uint lastError, bool isFolder, string path, SafeGlobalMemoryBufferHandle securityDescriptor)
      {
         ObjectSecurity objectSecurity;

         using (securityDescriptor)
         {
            if (lastError == Win32Errors.ERROR_FILE_NOT_FOUND || lastError == Win32Errors.ERROR_PATH_NOT_FOUND)
               lastError = isFolder ? Win32Errors.ERROR_PATH_NOT_FOUND : Win32Errors.ERROR_FILE_NOT_FOUND;


            // MSDN: GetNamedSecurityInfo() / GetSecurityInfo(): If the function fails, the return value is zero.
            if (lastError != Win32Errors.ERROR_SUCCESS)
               NativeError.ThrowException(lastError, !Utils.IsNullOrWhiteSpace(path) ? path : null);

            if (!NativeMethods.IsValidHandle(securityDescriptor, false))
               throw new IOException(Resources.Returned_Invalid_Security_Descriptor);


            var length = Security.NativeMethods.GetSecurityDescriptorLength(securityDescriptor);

            // Seems not to work: Method .CopyTo: length > Capacity, so an Exception is thrown.
            //byte[] managedBuffer = new byte[length];
            //pSecurityDescriptor.CopyTo(managedBuffer, 0, (int) length);

            var managedBuffer = securityDescriptor.ToByteArray(0, (int) length);

            objectSecurity = isFolder ? (ObjectSecurity) new DirectorySecurity() : new FileSecurity();
            objectSecurity.SetSecurityDescriptorBinaryForm(managedBuffer);
         }

         return (T) (object) objectSecurity;
      }
   }
}
