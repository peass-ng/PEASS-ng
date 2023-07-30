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
using System.IO;
using System.Security;
using System.Security.AccessControl;
using Alphaleonis.Win32.Security;
using Microsoft.Win32.SafeHandles;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      #region .NET

      /// <summary>Gets a <see cref="DirectorySecurity"/> object that encapsulates the access control list (ACL) entries for the specified directory.</summary>
      /// <returns>A <see cref="DirectorySecurity"/> object that encapsulates the access control rules for the file described by the <paramref name="path"/> parameter.</returns>
      /// <exception cref="IOException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to a directory containing a <see cref="DirectorySecurity"/> object that describes the file's access control list (ACL) information.</param>
      [SecurityCritical]
      public static DirectorySecurity GetAccessControl(string path)
      {
         return File.GetAccessControlCore<DirectorySecurity>(true, path, AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner, PathFormat.RelativePath);
      }

      #endregion // .NET


      /// <summary>[AlphaFS] Gets a <see cref="DirectorySecurity"/> object that encapsulates the access control list (ACL) entries for the specified directory.</summary>
      /// <returns>A <see cref="DirectorySecurity"/> object that encapsulates the access control rules for the file described by the <paramref name="path"/> parameter.</returns>
      /// <exception cref="IOException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to a directory containing a <see cref="DirectorySecurity"/> object that describes the file's access control list (ACL) information.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static DirectorySecurity GetAccessControl(string path, PathFormat pathFormat)
      {
         return File.GetAccessControlCore<DirectorySecurity>(true, path, AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner, pathFormat);
      }


      /// <summary>Gets a <see cref="DirectorySecurity"/> object that encapsulates the specified type of access control list (ACL) entries for a particular directory.</summary>
      /// <returns>A <see cref="DirectorySecurity"/> object that encapsulates the access control rules for the directory described by the <paramref name="path"/> parameter. </returns>
      /// <exception cref="IOException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to a directory containing a <see cref="DirectorySecurity"/> object that describes the directory's access control list (ACL) information.</param>
      /// <param name="includeSections">One (or more) of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to receive.</param>
      [SecurityCritical]
      public static DirectorySecurity GetAccessControl(string path, AccessControlSections includeSections)
      {
         return File.GetAccessControlCore<DirectorySecurity>(true, path, includeSections, PathFormat.RelativePath);
      }
      

      /// <summary>[AlphaFS] Gets a <see cref="DirectorySecurity"/> object that encapsulates the specified type of access control list (ACL) entries for a particular directory.</summary>
      /// <returns>A <see cref="DirectorySecurity"/> object that encapsulates the access control rules for the directory described by the <paramref name="path"/> parameter. </returns>
      /// <exception cref="IOException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="path">The path to a directory containing a <see cref="DirectorySecurity"/> object that describes the directory's access control list (ACL) information.</param>
      /// <param name="includeSections">One (or more) of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to receive.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      public static DirectorySecurity GetAccessControl(string path, AccessControlSections includeSections, PathFormat pathFormat)
      {
         return File.GetAccessControlCore<DirectorySecurity>(true, path, includeSections, pathFormat);
      }


      /// <summary>[AlphaFS] Gets a <see cref="DirectorySecurity"/> object that encapsulates the access control list (ACL) entries for the specified directory handle.</summary>
      /// <returns>A <see cref="DirectorySecurity"/> object that encapsulates the access control rules for the file described by the <paramref name="handle"/> parameter.</returns>
      /// <exception cref="IOException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="handle">A <see cref="SafeFileHandle"/> to a directory containing a <see cref="DirectorySecurity"/> object that describes the directory's access control list (ACL) information.</param>
      [SecurityCritical]
      public static DirectorySecurity GetAccessControl(SafeFileHandle handle)
      {
         return File.GetAccessControlHandleCore<DirectorySecurity>(false, true, handle, AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner, SECURITY_INFORMATION.None);
      }


      /// <summary>[AlphaFS] Gets a <see cref="DirectorySecurity"/> object that encapsulates the specified type of access control list (ACL) entries for a particular directory handle.</summary>
      /// <returns>A <see cref="DirectorySecurity"/> object that encapsulates the access control rules for the directory described by the <paramref name="handle"/> parameter. </returns>
      /// <exception cref="IOException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="ArgumentNullException"/>
      /// <param name="handle">A <see cref="SafeFileHandle"/> to a directory containing a <see cref="DirectorySecurity"/> object that describes the directory's access control list (ACL) information.</param>
      /// <param name="includeSections">One (or more) of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to receive.</param>
      [SecurityCritical]
      public static DirectorySecurity GetAccessControl(SafeFileHandle handle, AccessControlSections includeSections)
      {
         return File.GetAccessControlHandleCore<DirectorySecurity>(false, true, handle, includeSections, SECURITY_INFORMATION.None);
      }
   }
}
