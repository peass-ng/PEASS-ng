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

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>
      ///   Retrieves information about the specified disk, including the amount of free space on the disk.
      /// </summary>
      /// <remarks>
      ///   <para>Symbolic link behavior: If the path points to a symbolic link, the operation is performed on the target.</para>
      ///   <para>If this parameter is a UNC name, it must include a trailing backslash (for example, "\\MyServer\MyShare\").</para>
      ///   <para>Furthermore, a drive specification must have a trailing backslash (for example, "C:\").</para>
      ///   <para>The calling application must have FILE_LIST_DIRECTORY access rights for this directory.</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps only]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      /// <param name="lpRootPathName">Full pathname of the root file.</param>
      /// <param name="lpSectorsPerCluster">[out] The sectors per cluster.</param>
      /// <param name="lpBytesPerSector">[out] The bytes per sector.</param>
      /// <param name="lpNumberOfFreeClusters">[out] Number of free clusters.</param>
      /// <param name="lpTotalNumberOfClusters">[out] The total number of clusters.</param>
      /// <returns>
      ///   <para>If the function succeeds, the return value is nonzero.</para>
      ///   <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetDiskFreeSpaceW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetDiskFreeSpace([MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName, [MarshalAs(UnmanagedType.U4)] out int lpSectorsPerCluster, [MarshalAs(UnmanagedType.U4)] out int lpBytesPerSector, [MarshalAs(UnmanagedType.U4)] out int lpNumberOfFreeClusters, [MarshalAs(UnmanagedType.U4)] out uint lpTotalNumberOfClusters);

      /// <summary>
      ///   Retrieves information about the amount of space that is available on a disk volume, which is the total amount of space,
      ///   <para>the total amount of free space, and the total amount of free space available to the user that is associated with the calling
      ///   thread.</para>
      /// </summary>
      /// <remarks>
      ///   <para>Symbolic link behavior: If the path points to a symbolic link, the operation is performed on the target.</para>
      ///   <para>The GetDiskFreeSpaceEx function returns zero (0) for lpTotalNumberOfFreeBytes and lpFreeBytesAvailable
      ///   for all CD requests unless the disk is an unwritten CD in a CD-RW drive.</para>
      ///   <para>If this parameter is a UNC name, it must include a trailing backslash, for example, "\\MyServer\MyShare\".</para>
      ///   <para>This parameter does not have to specify the root directory on a disk.</para>
      ///   <para>The function accepts any directory on a disk.</para>
      ///   <para>The calling application must have FILE_LIST_DIRECTORY access rights for this directory.</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps | Windows Store apps]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps | Windows Store apps]</para>
      /// </remarks>
      /// <param name="lpDirectoryName">Pathname of the directory.</param>
      /// <param name="lpFreeBytesAvailable">[out] The free bytes available.</param>
      /// <param name="lpTotalNumberOfBytes">[out] The total number of in bytes.</param>
      /// <param name="lpTotalNumberOfFreeBytes">[out] The total number of free in bytes.</param>
      /// <returns>
      ///   <para>If the function succeeds, the return value is nonzero.</para>
      ///   <para>If the function fails, the return value is zero (0). To get extended error information, call GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetDiskFreeSpaceExW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetDiskFreeSpaceEx([MarshalAs(UnmanagedType.LPWStr)] string lpDirectoryName, [MarshalAs(UnmanagedType.U8)] out long lpFreeBytesAvailable, [MarshalAs(UnmanagedType.U8)] out long lpTotalNumberOfBytes, [MarshalAs(UnmanagedType.U8)] out long lpTotalNumberOfFreeBytes);
   }
}
