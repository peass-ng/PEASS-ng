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

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>The BackupRead function can be used to back up a file or directory, including the security information.
      ///   <para>The function reads data associated with a specified file or directory into a buffer,</para>
      ///   <para>which can then be written to the backup medium using the WriteFile function.</para>
      /// </summary>
      /// <remarks>
      ///   <para>This function is not intended for use in backing up files encrypted under the Encrypted File System.</para>
      ///   <para>Use ReadEncryptedFileRaw for that purpose.</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps only]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      /// <param name="hFile">The file.</param>
      /// <param name="lpBuffer">The buffer.</param>
      /// <param name="nNumberOfBytesToRead">Number of bytes to reads.</param>
      /// <param name="lpNumberOfBytesRead">[out] Number of bytes reads.</param>
      /// <param name="bAbort">true to abort.</param>
      /// <param name="bProcessSecurity">true to process security.</param>
      /// <param name="lpContext">[out] The context.</param>
      /// <returns>
      ///   <para>If the function succeeds, the return value is nonzero.</para>
      ///   <para>If the function fails, the return value is zero, indicating that an I/O error occurred. To get extended error information,
      ///   call GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool BackupRead(SafeFileHandle hFile, SafeGlobalMemoryBufferHandle lpBuffer, [MarshalAs(UnmanagedType.U4)] uint nNumberOfBytesToRead, [MarshalAs(UnmanagedType.U4)] out uint lpNumberOfBytesRead, [MarshalAs(UnmanagedType.Bool)] bool bAbort, [MarshalAs(UnmanagedType.Bool)] bool bProcessSecurity, ref IntPtr lpContext);


      /// <summary>The BackupSeek function seeks forward in a data stream initially accessed by using the <see cref="BackupRead"/> or
      ///   <see cref="BackupWrite"/> function.
      ///   <para>The function reads data associated with a specified file or directory into a buffer, which can then be written to the backup
      ///   medium using the WriteFile function.</para>
      /// </summary>
      /// <remarks>
      ///   <para>Applications use the BackupSeek function to skip portions of a data stream that cause errors.</para>
      ///   <para>This function does not seek across stream headers. For example, this function cannot be used to skip the stream name.</para>
      ///   <para>If an application attempts to seek past the end of a substream, the function fails, the lpdwLowByteSeeked and
      ///   lpdwHighByteSeeked parameters</para>
      ///   <para>indicate the actual number of bytes the function seeks, and the file position is placed at the start of the next stream
      ///   header.</para>
      ///   <para>&#160;</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps only]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      /// <param name="hFile">The file.</param>
      /// <param name="dwLowBytesToSeek">The low bytes to seek.</param>
      /// <param name="dwHighBytesToSeek">The high bytes to seek.</param>
      /// <param name="lpdwLowBytesSeeked">[out] The lpdw low bytes seeked.</param>
      /// <param name="lpdwHighBytesSeeked">[out] The lpdw high bytes seeked.</param>
      /// <param name="lpContext">[out] The context.</param>
      /// <returns>
      ///   <para>If the function could seek the requested amount, the function returns a nonzero value.</para>
      ///   <para>If the function could not seek the requested amount, the function returns zero. To get extended error information, call
      ///   GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool BackupSeek(SafeFileHandle hFile, [MarshalAs(UnmanagedType.U4)] uint dwLowBytesToSeek, [MarshalAs(UnmanagedType.U4)] uint dwHighBytesToSeek, [MarshalAs(UnmanagedType.U4)] out uint lpdwLowBytesSeeked, [MarshalAs(UnmanagedType.U4)] out uint lpdwHighBytesSeeked, ref IntPtr lpContext);


      /// <summary>The BackupWrite function can be used to restore a file or directory that was backed up using <see cref="BackupRead"/>.
      ///   <para>Use the ReadFile function to get a stream of data from the backup medium, then use BackupWrite to write the data to the
      ///   specified file or directory.</para>
      ///   <para>&#160;</para>
      /// </summary>
      /// <remarks>
      ///   <para>This function is not intended for use in restoring files encrypted under the Encrypted File System. Use WriteEncryptedFileRaw
      ///   for that purpose.</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps only]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      /// <param name="hFile">The file.</param>
      /// <param name="lpBuffer">The buffer.</param>
      /// <param name="nNumberOfBytesToWrite">Number of bytes to writes.</param>
      /// <param name="lpNumberOfBytesWritten">[out] Number of bytes writtens.</param>
      /// <param name="bAbort">true to abort.</param>
      /// <param name="bProcessSecurity">true to process security.</param>
      /// <param name="lpContext">[out] The context.</param>
      /// <returns>
      ///   <para>If the function succeeds, the return value is nonzero.</para>
      ///   <para>If the function fails, the return value is zero, indicating that an I/O error occurred. To get extended error information,
      ///   call GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool BackupWrite(SafeFileHandle hFile, SafeGlobalMemoryBufferHandle lpBuffer, [MarshalAs(UnmanagedType.U4)] uint nNumberOfBytesToWrite, [MarshalAs(UnmanagedType.U4)] out uint lpNumberOfBytesWritten, [MarshalAs(UnmanagedType.Bool)] bool bAbort, [MarshalAs(UnmanagedType.Bool)] bool bProcessSecurity, ref IntPtr lpContext);
   }
}
