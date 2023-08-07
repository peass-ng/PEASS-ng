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
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Security
{
   internal static partial class NativeMethods
   {
      /// <summary>The GetTokenInformation function retrieves a specified type of information about an access token. The calling process must have appropriate access rights to obtain the information.</summary>
      /// <returns>
      /// If the function succeeds, the return value is nonzero.
      /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [SuppressUnmanagedCodeSecurity]
      [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetTokenInformation(SafeTokenHandle tokenHandle, [MarshalAs(UnmanagedType.U4)] TOKEN_INFORMATION_CLASS tokenInformationClass, SafeGlobalMemoryBufferHandle tokenInformation, [MarshalAs(UnmanagedType.U4)] uint tokenInformationLength, [MarshalAs(UnmanagedType.U4)] out uint returnLength);
   }
}
