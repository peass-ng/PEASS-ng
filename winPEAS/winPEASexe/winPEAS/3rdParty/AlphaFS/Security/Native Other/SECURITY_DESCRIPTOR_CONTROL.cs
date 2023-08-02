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
   /// <summary>The SECURITY_DESCRIPTOR_CONTROL data type is a set of bit flags that qualify the meaning of a security descriptor or its components.
   /// Each security descriptor has a Control member that stores the SECURITY_DESCRIPTOR_CONTROL bits.
   /// </summary>
   /// <remarks>
   /// <para>Minimum supported client: Windows XP [desktop apps only]</para>
   /// <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
   /// </remarks>
   [Flags]
   internal enum SECURITY_DESCRIPTOR_CONTROL
   {
      /// <summary>None</summary>
      None = 0,

      /// <summary>SE_OWNER_DEFAULTED (0x0001) - Indicates an SD with a default owner security identifier (SID). You can use this bit to find all of the objects that have default owner permissions set.</summary>
      SE_OWNER_DEFAULTED = 1,

      /// <summary>SE_GROUP_DEFAULTED (0x0002) - Indicates an SD with a default group SID. You can use this bit to find all of the objects that have default group permissions set.</summary>
      SE_GROUP_DEFAULTED = 2,

      /// <summary>SE_DACL_PRESENT (0x0004) - Indicates an SD that has a discretionary access control list (DACL). If this flag is not set, or if this flag is set and the DACL is NULL, the SD allows full access to everyone.</summary>
      SE_DACL_PRESENT = 4,

      /// <summary>SE_DACL_DEFAULTED (0x0008) - Indicates an SD with a default DACL. For example, if an object creator does not specify a DACL, the object receives the default DACL from the access token of the creator. This flag can affect how the system treats the DACL, with respect to access control entry (ACE) inheritance. The system ignores this flag if the SE_DACL_PRESENT flag is not set.</summary>
      SE_DACL_DEFAULTED = 8,

      /// <summary>SE_SACL_PRESENT (0x0010) - Indicates an SD that has a system access control list (SACL).</summary>
      SE_SACL_PRESENT = 16,

      /// <summary>SE_SACL_DEFAULTED (0x0020) - Indicates an SD with a default SACL. For example, if an object creator does not specify an SACL, the object receives the default SACL from the access token of the creator. This flag can affect how the system treats the SACL, with respect to ACE inheritance. The system ignores this flag if the SE_SACL_PRESENT flag is not set.</summary>
      SE_SACL_DEFAULTED = 32,

      /// <summary>SE_DACL_AUTO_INHERIT_REQ (0x0100) - Requests that the provider for the object protected by the SD automatically propagate the DACL to existing child objects. If the provider supports automatic inheritance, it propagates the DACL to any existing child objects, and sets the SE_DACL_AUTO_INHERITED bit in the security descriptors of the object and its child objects.</summary>
      SE_DACL_AUTO_INHERIT_REQ = 256,

      /// <summary>SE_SACL_AUTO_INHERIT_REQ (0x0200) - Requests that the provider for the object protected by the SD automatically propagate the SACL to existing child objects. If the provider supports automatic inheritance, it propagates the SACL to any existing child objects, and sets the SE_SACL_AUTO_INHERITED bit in the SDs of the object and its child objects.</summary>
      SE_SACL_AUTO_INHERIT_REQ = 512,

      /// <summary>SE_DACL_AUTO_INHERITED (0x0400) - Windows 2000 only. Indicates an SD in which the DACL is set up to support automatic propagation of inheritable ACEs to existing child objects. The system sets this bit when it performs the automatic inheritance algorithm for the object and its existing child objects. This bit is not set in SDs for Windows NT versions 4.0 and earlier, which do not support automatic propagation of inheritable ACEs.</summary>
      SE_DACL_AUTO_INHERITED = 1024,

      /// <summary>SE_SACL_AUTO_INHERITED (0x0800) - Windows 2000: Indicates an SD in which the SACL is set up to support automatic propagation of inheritable ACEs to existing child objects. The system sets this bit when it performs the automatic inheritance algorithm for the object and its existing child objects. This bit is not set in SDs for Windows NT versions 4.0 and earlier, which do not support automatic propagation of inheritable ACEs.</summary>
      SE_SACL_AUTO_INHERITED = 2048,

      /// <summary>SE_DACL_PROTECTED (0x1000) - Windows 2000: Prevents the DACL of the SD from being modified by inheritable ACEs.</summary>
      SE_DACL_PROTECTED = 4096,

      /// <summary>SE_SACL_PROTECTED (0x2000) - Windows 2000: Prevents the SACL of the SD from being modified by inheritable ACEs.</summary>
      SE_SACL_PROTECTED = 8192,

      /// <summary>SE_RM_CONTROL_VALID (0x4000) - Indicates that the resource manager control is valid.</summary>
      SE_RM_CONTROL_VALID = 16384,

      /// <summary>SE_SELF_RELATIVE (0x8000) - Indicates an SD in self-relative format with all of the security information in a contiguous block of memory. If this flag is not set, the SD is in absolute format. For more information, see Absolute and Self-Relative Security Descriptors.</summary>
      SE_SELF_RELATIVE = 32768
   }
}
