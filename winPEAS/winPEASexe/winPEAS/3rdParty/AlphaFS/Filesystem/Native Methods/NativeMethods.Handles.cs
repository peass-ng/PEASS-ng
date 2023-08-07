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
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>Closes an open object handle.</summary>
      /// <remarks>
      ///   <para>The CloseHandle function closes handles to the following objects:</para>
      ///   <para>Access token, Communications device, Console input, Console screen buffer, Event, File, File mapping, I/O completion port,
      ///   Job, Mailslot, Memory resource notification, Mutex, Named pipe, Pipe, Process, Semaphore, Thread, Transaction, Waitable
      ///   timer.</para>
      ///   <para>SetLastError is set to <c>false</c>.</para>
      ///   <para>Minimum supported client: Windows 2000 Professional [desktop apps | Windows Store apps]</para>
      ///   <para>Minimum supported server: Windows 2000 Server [desktop apps | Windows Store apps]</para>
      /// </remarks>
      /// <returns>
      ///   <para>If the function succeeds, the return value is nonzero.</para>
      ///   <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
      ///   <para>If the application is running under a debugger, the function will throw an exception if it receives either a handle value
      ///   that is not valid or a pseudo-handle value.This can happen if you close a handle twice, or if you call CloseHandle on a handle
      ///   returned by the FindFirstFile function instead of calling the FindClose function.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool CloseHandle(IntPtr hObject);
   }
}
