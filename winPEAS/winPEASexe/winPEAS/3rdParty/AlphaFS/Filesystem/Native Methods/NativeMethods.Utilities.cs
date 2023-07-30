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

using Alphaleonis.Win32.Security;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using winPEAS._3rdParty.AlphaFS;

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      internal static uint GetHighOrderDword(long highPart)
      {
         return (uint) ((highPart >> 32) & 0xFFFFFFFF);
      }


      internal static uint GetLowOrderDword(long lowPart)
      {
         return (uint) (lowPart & 0xFFFFFFFF);
      }


      internal static long LuidToLong(LUID luid)
      {
         var high = (ulong) luid.HighPart << 32;
         var low = (ulong) luid.LowPart & 0x00000000FFFFFFFF;

         return unchecked((long) (high | low));
      }


      internal static LUID LongToLuid(long lluid)
      {
         return new LUID {HighPart = (uint) (lluid >> 32), LowPart = (uint) (lluid & 0xFFFFFFFF)};
      }


      internal static long ToLong(uint highPart, uint lowPart)
      {
         return ((long) highPart << 32) | ((long) lowPart & 0xFFFFFFFF);
      }


      /// <summary>Check is the current handle is not null, not closed and not invalid.</summary>
      /// <param name="handle">The current handle to check.</param>
      /// <param name="throwException"><c>true</c> will throw an <exception cref="Resources.Handle_Is_Invalid"/>, <c>false</c> will not raise this exception..</param>
      /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
      /// <exception cref="ArgumentException"/>
      internal static bool IsValidHandle(SafeHandle handle, bool throwException = true)
      {
         if (null == handle || handle.IsClosed || handle.IsInvalid)
         {
            CloseSafeHandle(handle);

            if (throwException)
               throw new ArgumentException(Resources.Handle_Is_Invalid, "handle");

            return false;
         }

         return true;
      }


      /// <summary>Check is the current handle is not null, not closed and not invalid.</summary>
      /// <param name="handle">The current handle to check.</param>
      /// <param name="lastError">The result of Marshal.GetLastWin32Error()</param>
      /// <param name="throwException"><c>true</c> will throw an <exception cref="Resources.Handle_Is_Invalid_Win32Error"/>, <c>false</c> will not raise this exception..</param>
      /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
      /// <exception cref="ArgumentException"/>
      internal static bool IsValidHandle(SafeHandle handle, int lastError, bool throwException = true)
      {
         if (null == handle || handle.IsClosed || handle.IsInvalid)
         {
            CloseSafeHandle(handle);

            if (throwException)
               throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Handle_Is_Invalid_Win32Error, lastError), "handle");

            return false;
         }

         return true;
      }


      /// <summary>Check is the current handle is not null, not closed and not invalid.</summary>
      /// <param name="handle">The current handle to check.</param>
      /// <param name="lastError">The result of Marshal.GetLastWin32Error()</param>
      /// <param name="path">The path on which the Exception occurred.</param>
      /// <param name="throwException"><c>true</c> will throw an <exception cref="Resources.Handle_Is_Invalid_Win32Error"/>, <c>false</c> will not raise this exception..</param>
      /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="Exception"/>
      internal static bool IsValidHandle(SafeHandle handle, int lastError, string path, bool throwException = true)
      {
         if (null == handle || handle.IsClosed || handle.IsInvalid)
         {
            CloseSafeHandle(handle);

            if (throwException)
               NativeError.ThrowException(lastError, path);

            return false;
         }

         return true;
      }


      /// <summary>Check is the current handle is not null, not closed and not invalid.</summary>
      /// <param name="handle">The current handle to check.</param>
      /// <param name="lastError">The result of Marshal.GetLastWin32Error()</param>
      /// <param name="isFolder">When <c>true</c> indicates the source is a directory, <c>false</c> indicates a file and <c>null</c> specifies a physical device.</param>
      /// <param name="path">The path on which the Exception occurred.</param>
      /// <param name="throwException"><c>true</c> will throw an <exception cref="Resources.Handle_Is_Invalid_Win32Error"/>, <c>false</c> will not raise this exception..</param>
      /// <returns><c>true</c> on success, <c>false</c> otherwise.</returns>
      /// <exception cref="ArgumentException"/>
      /// <exception cref="Exception"/>
      internal static bool CloseHandleAndPossiblyThrowException(SafeHandle handle, int lastError, bool? isFolder, string path, bool throwException = true)
      {
         if (null == handle || handle.IsClosed || handle.IsInvalid)
         {
            CloseSafeHandle(handle);

            if (throwException)
               NativeError.ThrowException(lastError, isFolder, path);

            return false;
         }

         return true;
      }


      internal static void CloseSafeHandle(SafeHandle handle)
      {
         if (null != handle && !handle.IsClosed)
            handle.Close();

         handle = null;
      }


      /// <summary>Controls whether the system will handle the specified types of serious errors or whether the process will handle them.</summary>
      /// <remarks>
      ///   Because the error mode is set for the entire process, you must ensure that multi-threaded applications do not set different error-
      ///   mode attributes. Doing so can lead to inconsistent error handling.
      /// </remarks>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only].</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only].</remarks>
      /// <param name="uMode">The mode.</param>
      /// <returns>The return value is the previous state of the error-mode bit attributes.</returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.U4)]
      private static extern ErrorMode SetErrorMode(ErrorMode uMode);


      /// <summary>Controls whether the system will handle the specified types of serious errors or whether the calling thread will handle them.</summary>
      /// <remarks>
      ///   Because the error mode is set for the entire process, you must ensure that multi-threaded applications do not set different error-
      ///   mode attributes. Doing so can lead to inconsistent error handling.
      /// </remarks>
      /// <remarks>Minimum supported client: Windows 7 [desktop apps only].</remarks>
      /// <remarks>Minimum supported server: Windows Server 2008 R2 [desktop apps only].</remarks>
      /// <param name="dwNewMode">The new mode.</param>
      /// <param name="lpOldMode">[out] The old mode.</param>
      /// <returns>The return value is the previous state of the error-mode bit attributes.</returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetThreadErrorMode(ErrorMode dwNewMode, [MarshalAs(UnmanagedType.U4)] out ErrorMode lpOldMode);
   }
}
