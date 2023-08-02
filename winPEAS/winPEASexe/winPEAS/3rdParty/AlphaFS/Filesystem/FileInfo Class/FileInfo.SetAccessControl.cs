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
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Filesystem
{
   partial class FileInfo
   {
      #region .NET

      /// <summary>Applies access control list (ACL) entries described by a FileSecurity object to the file described by the current FileInfo object.</summary>
      /// <remarks>
      ///   The SetAccessControl method applies access control list (ACL) entries to the current file that represents the noninherited ACL
      ///   list. Use the SetAccessControl method whenever you need to add or remove ACL entries from a file.
      /// </remarks>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> object that describes an access control list (ACL) entry to apply to the current file.</param>      
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public void SetAccessControl(FileSecurity fileSecurity)
      {
         File.SetAccessControlCore(LongFullName, null, fileSecurity, AccessControlSections.All, PathFormat.LongFullPath);
      }


      /// <summary>Applies access control list (ACL) entries described by a FileSecurity object to the file described by the current FileInfo object.</summary>
      /// <remarks>
      ///   The SetAccessControl method applies access control list (ACL) entries to the current file that represents the noninherited ACL
      ///   list. Use the SetAccessControl method whenever you need to add or remove ACL entries from a file.
      /// </remarks>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> object that describes an access control list (ACL) entry to apply to the current file.</param>
      /// <param name="includeSections">One or more of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to set.</param>      
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public void SetAccessControl(FileSecurity fileSecurity, AccessControlSections includeSections)
      {
         File.SetAccessControlCore(LongFullName, null, fileSecurity, includeSections, PathFormat.LongFullPath);
      }

      #endregion // .NET
   }
}
