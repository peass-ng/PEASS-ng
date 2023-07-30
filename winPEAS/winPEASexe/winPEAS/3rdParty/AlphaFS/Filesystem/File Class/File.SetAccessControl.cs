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
using System.Security;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class File
   {
      #region .NET

      /// <summary>Applies access control list (ACL) entries described by a <see cref="FileSecurity"/> FileSecurity object to the specified file.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">A file to add or remove access control list (ACL) entries from.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> object that describes an ACL entry to apply to the file described by the <paramref name="path"/> parameter.</param>      
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public static void SetAccessControl(string path, FileSecurity fileSecurity)
      {
         SetAccessControlCore(path, null, fileSecurity, AccessControlSections.All, PathFormat.RelativePath);
      }

      #endregion // .NET


      /// <summary>Applies access control list (ACL) entries described by a <see cref="DirectorySecurity"/> object to the specified directory.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">A directory to add or remove access control list (ACL) entries from.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity "/> object that describes an ACL entry to apply to the directory described by the path parameter.</param>
      /// <param name="includeSections">One or more of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to set.</param>      
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public static void SetAccessControl(string path, FileSecurity fileSecurity, AccessControlSections includeSections)
      {
         SetAccessControlCore(path, null, fileSecurity, includeSections, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Applies access control list (ACL) entries described by a <see cref="FileSecurity"/> FileSecurity object to the specified file.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">A file to add or remove access control list (ACL) entries from.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> object that describes an ACL entry to apply to the file described by the <paramref name="path"/> parameter.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>      
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public static void SetAccessControl(string path, FileSecurity fileSecurity, PathFormat pathFormat)
      {
         SetAccessControlCore(path, null, fileSecurity, AccessControlSections.All, pathFormat);
      }


      /// <summary>[AlphaFS] Applies access control list (ACL) entries described by a <see cref="DirectorySecurity"/> object to the specified directory.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="path">A directory to add or remove access control list (ACL) entries from.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity "/> object that describes an ACL entry to apply to the directory described by the path parameter.</param>
      /// <param name="includeSections">One or more of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to set.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>      
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public static void SetAccessControl(string path, FileSecurity fileSecurity, AccessControlSections includeSections, PathFormat pathFormat)
      {
         SetAccessControlCore(path, null, fileSecurity, includeSections, pathFormat);
      }


      /// <summary>Applies access control list (ACL) entries described by a <see cref="FileSecurity"/> FileSecurity object to the specified file.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="handle">A <see cref="SafeFileHandle"/> to a file to add or remove access control list (ACL) entries from.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> object that describes an ACL entry to apply to the file described by the <paramref name="handle"/> parameter.</param>      
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public static void SetAccessControl(SafeFileHandle handle, FileSecurity fileSecurity)
      {
         SetAccessControlCore(null, handle, fileSecurity, AccessControlSections.All, PathFormat.LongFullPath);
      }


      /// <summary>Applies access control list (ACL) entries described by a <see cref="FileSecurity"/> FileSecurity object to the specified file.</summary>
      /// <exception cref="ArgumentNullException"/>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="NotSupportedException"/>
      /// <param name="handle">A <see cref="SafeFileHandle"/> to a file to add or remove access control list (ACL) entries from.</param>
      /// <param name="fileSecurity">A <see cref="FileSecurity"/> object that describes an ACL entry to apply to the file described by the <paramref name="handle"/> parameter.</param>      
      /// <param name="includeSections">One or more of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to set.</param>
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public static void SetAccessControl(SafeFileHandle handle, FileSecurity fileSecurity, AccessControlSections includeSections)
      {
         SetAccessControlCore(null, handle, fileSecurity, includeSections, PathFormat.LongFullPath);
      }
   }
}
