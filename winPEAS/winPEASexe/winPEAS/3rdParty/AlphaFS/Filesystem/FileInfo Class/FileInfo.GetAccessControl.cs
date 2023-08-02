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
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Filesystem
{
   partial class FileInfo
   {
      #region .NET

      /// <summary>Gets a <see cref="FileSecurity"/> object that encapsulates the access control list (ACL) entries for the file described by the current <see cref="FileInfo"/> object.</summary>
      /// <returns><see cref="FileSecurity"/>A FileSecurity object that encapsulates the access control rules for the current file.</returns>
      [SecurityCritical]
      public FileSecurity GetAccessControl()
      {
         return File.GetAccessControlCore<FileSecurity>(false, LongFullName, AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner, PathFormat.LongFullPath);
      }


      /// <summary>Gets a <see cref="FileSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the file described by the current FileInfo object.</summary>
      /// <returns><see cref="FileSecurity"/> object that encapsulates the specified type of access control list (ACL) entries for the file described by the current FileInfo object.</returns>
      /// <param name="includeSections">One of the <see cref="System.Security"/> values that specifies which group of access control entries to retrieve.</param>
      [SecurityCritical]
      public FileSecurity GetAccessControl(AccessControlSections includeSections)
      {
         return File.GetAccessControlCore<FileSecurity>(false, LongFullName, includeSections, PathFormat.LongFullPath);
      }

      #endregion // .NET
   }
}
