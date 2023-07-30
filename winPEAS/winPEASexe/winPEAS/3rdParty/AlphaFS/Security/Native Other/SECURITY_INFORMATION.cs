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

namespace Alphaleonis.Win32.Security
{
   /// <summary>The SECURITY_INFORMATION data type identifies the object-related security information being set or queried.
   /// This security information includes:
   ///   The owner of an object;
   ///   The primary group of an object;
   ///   The discretionary access control list (DACL) of an object;
   ///   The system access control list (SACL) of an object;
   /// </summary>
   /// <remarks>
   /// An unsigned 32-bit integer specifies portions of a SECURITY_DESCRIPTOR by means of bit flags.
   /// Individual bit values (combinable with the bitwise OR operation) are as shown in the following table.
   /// </remarks>
   [Flags]
   internal enum SECURITY_INFORMATION : uint
   {
      /// <summary>None</summary>
      None = 0,

      /// <summary>OWNER_SECURITY_INFORMATION (0x00000001) - The owner identifier of the object is being referenced.</summary>
      OWNER_SECURITY_INFORMATION = 1,

      /// <summary>GROUP_SECURITY_INFORMATION (0x00000002) - The primary group identifier of the object is being referenced.</summary>
      GROUP_SECURITY_INFORMATION = 2,

      /// <summary>DACL_SECURITY_INFORMATION (0x00000004) - The DACL of the object is being referenced.</summary>
      DACL_SECURITY_INFORMATION = 4,

      /// <summary>SACL_SECURITY_INFORMATION (0x00000008) - The SACL of the object is being referenced.</summary>
      SACL_SECURITY_INFORMATION = 8,

      /// <summary>LABEL_SECURITY_INFORMATION (0x00000010) - The mandatory integrity label is being referenced. The mandatory integrity label is an ACE in the SACL of the object.</summary>
      /// <remarks>Windows Server 2003 and Windows XP: This bit flag is not available.</remarks>
      LABEL_SECURITY_INFORMATION = 16,

      /// <summary>ATTRIBUTE_SECURITY_INFORMATION (0x00000020) - The resource properties of the object being referenced.
      /// The resource properties are stored in SYSTEM_RESOURCE_ATTRIBUTE_ACE types in the SACL of the security descriptor.
      /// </summary>
      /// <remarks>Windows Server 2008 R2, Windows 7, Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This bit flag is not available.</remarks>
      ATTRIBUTE_SECURITY_INFORMATION = 32,

      /// <summary>SCOPE_SECURITY_INFORMATION (0x00000040) - The Central Access Policy (CAP) identifier applicable on the object that is being referenced.
      /// Each CAP identifier is stored in a SYSTEM_SCOPED_POLICY_ID_ACE type in the SACL of the SD.
      /// </summary>
      /// <remarks>Windows Server 2008 R2, Windows 7, Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This bit flag is not available.</remarks>
      SCOPE_SECURITY_INFORMATION = 64,

      /// <summary>BACKUP_SECURITY_INFORMATION (0x00010000) - All parts of the security descriptor. This is useful for backup and restore software that needs to preserve the entire security descriptor.</summary>
      /// <remarks>Windows Server 2008 R2, Windows 7, Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP: This bit flag is not available.</remarks>
      BACKUP_SECURITY_INFORMATION = 65536,

      /// <summary>UNPROTECTED_SACL_SECURITY_INFORMATION (0x10000000) - The SACL inherits ACEs from the parent object.</summary>
      UNPROTECTED_SACL_SECURITY_INFORMATION = 268435456,

      /// <summary>UNPROTECTED_DACL_SECURITY_INFORMATION (0x20000000) - The DACL inherits ACEs from the parent object.</summary>
      UNPROTECTED_DACL_SECURITY_INFORMATION = 536870912,

      /// <summary>PROTECTED_SACL_SECURITY_INFORMATION (0x40000000) - The SACL cannot inherit ACEs.</summary>
      PROTECTED_SACL_SECURITY_INFORMATION = 1073741824,

      /// <summary>PROTECTED_DACL_SECURITY_INFORMATION (0x80000000) - The DACL cannot inherit access control entries (ACEs).</summary>
      PROTECTED_DACL_SECURITY_INFORMATION = 2147483648
   }
}
