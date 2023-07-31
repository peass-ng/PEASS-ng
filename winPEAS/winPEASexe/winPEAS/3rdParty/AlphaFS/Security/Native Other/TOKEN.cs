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
   internal static partial class NativeMethods
   {
      /// <summary>[AlphaFS] Access rights for access-token objects.</summary>
      [Flags]
      internal enum TOKEN : uint
      {
         /// <summary>Required to attach a primary token to a process. The SE_ASSIGNPRIMARYTOKEN_NAME privilege is also required to accomplish this task.</summary>
         TOKEN_ASSIGN_PRIMARY = 1,

         /// <summary>Required to duplicate an access token.</summary>
         TOKEN_DUPLICATE = 2,

         /// <summary>Required to attach an impersonation access token to a process.</summary>
         TOKEN_IMPERSONATE = 4,

         /// <summary>Required to query an access token.</summary>
         TOKEN_QUERY = 8,

         /// <summary>Required to query the source of an access token.</summary>
         TOKEN_QUERY_SOURCE = 16,

         /// <summary>Required to enable or disable the privileges in an access token.</summary>
         TOKEN_ADJUST_PRIVILEGES = 32,

         /// <summary>Required to adjust the attributes of the groups in an access token.</summary>
         TOKEN_ADJUST_GROUPS = 64,

         /// <summary>Required to change the default owner, primary group, or DACL of an access token.</summary>
         TOKEN_ADJUST_DEFAULT = 128,

         /// <summary>Required to adjust the session ID of an access token. The SE_TCB_NAME privilege is required.</summary>
         TOKEN_ADJUST_SESSIONID = 256,

         /// <summary>Combines STANDARD_RIGHTS_READ and TOKEN_QUERY.</summary>
         TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY,

         /// <summary>Combines all possible access rights for a token.</summary>
         TOKEN_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                            TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID
      }
   }
}
