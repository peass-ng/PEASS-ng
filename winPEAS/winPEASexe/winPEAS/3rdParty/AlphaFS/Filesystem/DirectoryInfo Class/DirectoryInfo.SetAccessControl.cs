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
   public sealed partial class DirectoryInfo
   {
      #region .NET

      /// <summary>Applies access control list (ACL) entries described by a <see cref="DirectorySecurity"/> object to the directory described by the current DirectoryInfo object.</summary>
      /// <param name="directorySecurity">A <see cref="DirectorySecurity"/> object that describes an ACL entry to apply to the directory described by the path parameter.</param>
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public void SetAccessControl(DirectorySecurity directorySecurity)
      {
         File.SetAccessControlCore(LongFullName, null, directorySecurity, AccessControlSections.All, PathFormat.LongFullPath);
      }


      /// <summary>Applies access control list (ACL) entries described by a <see cref="DirectorySecurity"/> object to the directory described by the current DirectoryInfo object.</summary>
      /// <param name="directorySecurity">A <see cref="DirectorySecurity"/> object that describes an ACL entry to apply to the directory described by the path parameter.</param>
      /// <param name="includeSections">One or more of the <see cref="AccessControlSections"/> values that specifies the type of access control list (ACL) information to set.</param>
      [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
      [SecurityCritical]
      public void SetAccessControl(DirectorySecurity directorySecurity, AccessControlSections includeSections)
      {
         File.SetAccessControlCore(LongFullName, null, directorySecurity, includeSections, PathFormat.LongFullPath);
      }

      #endregion // .NET
   }
}
