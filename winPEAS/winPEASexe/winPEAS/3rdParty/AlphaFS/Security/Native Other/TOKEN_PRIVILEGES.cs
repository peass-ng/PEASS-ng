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

using System.Runtime.InteropServices;

namespace Alphaleonis.Win32.Security
{
   /// <summary>The TOKEN_PRIVILEGES structure contains information about a set of privileges for an access token.</summary>
   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
   internal struct TOKEN_PRIVILEGES
   {
      /// <summary>This must be set to the number of entries in the Privileges array.</summary>
      [MarshalAs(UnmanagedType.U4)] public uint PrivilegeCount;

      /// <summary>Specifies an array of LUID_AND_ATTRIBUTES structures. Each structure contains the LUID and attributes of a privilege.</summary>
      public LUID Luid;

      /// <summary>The attributes of a privilege can be a combination of the following values:
      /// SE_PRIVILEGE_ENABLED: The privilege is enabled.
      /// SE_PRIVILEGE_ENABLED_BY_DEFAULT: The privilege is enabled by default.
      /// SE_PRIVILEGE_REMOVED: Used to remove a privilege. For details, see AdjustTokenPrivileges.
      /// SE_PRIVILEGE_USED_FOR_ACCESS: The privilege was used to gain access to an object or service. This flag is used to identify the relevant privileges in a set passed by a client application that may contain unnecessary privileges.
      /// </summary>
      [MarshalAs(UnmanagedType.U4)] public uint Attributes;
   }
}
