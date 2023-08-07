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
using System.Security.AccessControl;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Directory
   {
      /// <summary>[AlphaFS] Checks if the directory has permission inheritance enabled.</summary>
      /// <param name="path">The full path to the directory to check.</param>
      /// <returns><c>true</c> if permission inheritance is enabled, <c>false</c> if permission inheritance is disabled.</returns>
      public static bool HasInheritedPermissions(string path)
      {
         return HasInheritedPermissions(path, PathFormat.RelativePath);
      }


      /// <summary>[AlphaFS] Checks if the directory has permission inheritance enabled.</summary>
      /// <returns><c>true</c> if permission inheritance is enabled, <c>false</c> if permission inheritance is disabled.</returns>
      /// <param name="path">The full path to the directory to check.</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      public static bool HasInheritedPermissions(string path, PathFormat pathFormat)
      {
         if (Utils.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException("path");


         var acl = File.GetAccessControlCore<DirectorySecurity>(true, path, AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner, pathFormat);

         var rawBytes = acl.GetSecurityDescriptorBinaryForm();

         var rsd = new RawSecurityDescriptor(rawBytes, 0);


         // "Include inheritable permissions from this object's parent" is unchecked.
         var inheritanceDisabled = (rsd.ControlFlags & ControlFlags.DiscretionaryAclProtected) == ControlFlags.DiscretionaryAclProtected;

         return !inheritanceDisabled;
      }
   }
}
