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
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {  
      /// <summary>Defines, redefines, or deletes MS-DOS device names.</summary>
      /// <returns>
      /// If the function succeeds, the return value is nonzero.
      /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "DefineDosDeviceW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool DefineDosDevice(DosDeviceAttributes dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string lpDeviceName, [MarshalAs(UnmanagedType.LPWStr)] string lpTargetPath);

      /// <summary>Deletes a drive letter or mounted folder.</summary>
      /// <returns>
      /// If the function succeeds, the return value is nonzero.
      /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "DeleteVolumeMountPointW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool DeleteVolumeMountPoint([MarshalAs(UnmanagedType.LPWStr)] string lpszVolumeMountPoint);

      /// <summary>Retrieves the name of a volume on a computer. FindFirstVolume is used to begin scanning the volumes of a computer.</summary>
      /// <returns>
      /// If the function succeeds, the return value is a search handle used in a subsequent call to the FindNextVolume and FindVolumeClose functions.
      /// If the function fails to find any volumes, the return value is the INVALID_HANDLE_VALUE error code. To get extended error information, call GetLastError.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindFirstVolumeW"), SuppressUnmanagedCodeSecurity]
      internal static extern SafeFindVolumeHandle FindFirstVolume(StringBuilder lpszVolumeName, [MarshalAs(UnmanagedType.U4)] uint cchBufferLength);

      /// <summary>Retrieves the name of a mounted folder on the specified volume. FindFirstVolumeMountPoint is used to begin scanning the mounted folders on a volume.</summary>
      /// <returns>
      /// If the function succeeds, the return value is a search handle used in a subsequent call to the FindNextVolumeMountPoint and FindVolumeMountPointClose functions.
      /// If the function fails to find a mounted folder on the volume, the return value is the INVALID_HANDLE_VALUE error code.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindFirstVolumeMountPointW"), SuppressUnmanagedCodeSecurity]
      internal static extern SafeFindVolumeMountPointHandle FindFirstVolumeMountPoint([MarshalAs(UnmanagedType.LPWStr)] string lpszRootPathName, StringBuilder lpszVolumeMountPoint, [MarshalAs(UnmanagedType.U4)] uint cchBufferLength);

      /// <summary>Continues a volume search started by a call to the FindFirstVolume function. FindNextVolume finds one volume per call.</summary>
      /// <returns>
      /// If the function succeeds, the return value is nonzero.
      /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindNextVolumeW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool FindNextVolume(SafeFindVolumeHandle hFindVolume, StringBuilder lpszVolumeName, [MarshalAs(UnmanagedType.U4)] uint cchBufferLength);

      /// <summary>Continues a mounted folder search started by a call to the FindFirstVolumeMountPoint function. FindNextVolumeMountPoint finds one mounted folder per call.</summary>
      /// <returns>
      /// If the function succeeds, the return value is nonzero.
      /// If the function fails, the return value is zero. To get extended error information, call GetLastError. If no more mounted folders can be found, the GetLastError function returns the ERROR_NO_MORE_FILES error code.
      /// In that case, close the search with the FindVolumeMountPointClose function.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FindNextVolumeMountPointW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool FindNextVolumeMountPoint(SafeFindVolumeMountPointHandle hFindVolume, StringBuilder lpszVolumeName, [MarshalAs(UnmanagedType.U4)] uint cchBufferLength);

      /// <summary>Closes the specified volume search handle.</summary>
      /// <remarks>
      ///   <para>SetLastError is set to <c>false</c>.</para>
      ///   Minimum supported client: Windows XP [desktop apps only]. Minimum supported server: Windows Server 2003 [desktop apps only].
      /// </remarks>
      /// <returns>
      ///   If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error
      ///   information, call GetLastError.
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool FindVolumeClose(IntPtr hFindVolume);

      /// <summary>Closes the specified mounted folder search handle.</summary>
      /// <remarks>
      ///   <para>SetLastError is set to <c>false</c>.</para>
      ///   <para>Minimum supported client: Windows XP</para>
      ///   <para>Minimum supported server: Windows Server 2003</para>
      /// </remarks>
      /// <returns>
      ///   If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error
      ///   information, call GetLastError.
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool FindVolumeMountPointClose(IntPtr hFindVolume);

      /// <summary>
      ///   Determines whether a disk drive is a removable, fixed, CD-ROM, RAM disk, or network drive.
      ///   <para>To determine whether a drive is a USB-type drive, call <see cref="SetupDiGetDeviceRegistryProperty"/> and specify the
      ///   SPDRP_REMOVAL_POLICY property.</para>
      /// </summary>
      /// <remarks>
      ///   <para>SMB does not support volume management functions.</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps only]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      /// <param name="lpRootPathName">Full pathname of the root file.</param>
      /// <returns>
      ///   <para>The return value specifies the type of drive, see <see cref="DriveType"/>.</para>
      ///   <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetDriveTypeW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.U4)]
      internal static extern DriveType GetDriveType([MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName);

      /// <summary>
      ///   Retrieves a bitmask representing the currently available disk drives.
      /// </summary>
      /// <remarks>
      ///   <para>SMB does not support volume management functions.</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps only]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      /// <returns>
      ///   <para>If the function succeeds, the return value is a bitmask representing the currently available disk drives.</para>
      ///   <para>Bit position 0 (the least-significant bit) is drive A, bit position 1 is drive B, bit position 2 is drive C, and so on.</para>
      ///   <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.U4)]
      internal static extern uint GetLogicalDrives();

      /// <summary>Retrieves information about the file system and volume associated with the specified root directory.</summary>
      /// <returns>
      /// If all the requested information is retrieved, the return value is nonzero.
      /// If not all the requested information is retrieved, the return value is zero.
      /// </returns>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      /// <remarks>"lpRootPathName" must end with a trailing backslash.</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetVolumeInformationW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetVolumeInformation([MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName, StringBuilder lpVolumeNameBuffer, [MarshalAs(UnmanagedType.U4)] uint nVolumeNameSize, [MarshalAs(UnmanagedType.U4)] out uint lpVolumeSerialNumber, [MarshalAs(UnmanagedType.U4)] out int lpMaximumComponentLength, [MarshalAs(UnmanagedType.U4)] out VOLUME_INFO_FLAGS lpFileSystemAttributes, StringBuilder lpFileSystemNameBuffer, [MarshalAs(UnmanagedType.U4)] uint nFileSystemNameSize);

      /// <summary>Retrieves information about the file system and volume associated with the specified file.</summary>
      /// <returns>
      /// If all the requested information is retrieved, the return value is nonzero.
      /// If not all the requested information is retrieved, the return value is zero. To get extended error information, call GetLastError.
      /// </returns>
      /// <remarks>To retrieve the current compression state of a file or directory, use FSCTL_GET_COMPRESSION.</remarks>
      /// <remarks>SMB does not support volume management functions.</remarks>
      /// <remarks>Minimum supported client: Windows Vista [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2008 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetVolumeInformationByHandleW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetVolumeInformationByHandle(SafeFileHandle hFile, StringBuilder lpVolumeNameBuffer, [MarshalAs(UnmanagedType.U4)] uint nVolumeNameSize, [MarshalAs(UnmanagedType.U4)] out uint lpVolumeSerialNumber, [MarshalAs(UnmanagedType.U4)] out int lpMaximumComponentLength, out VOLUME_INFO_FLAGS lpFileSystemAttributes, StringBuilder lpFileSystemNameBuffer, [MarshalAs(UnmanagedType.U4)] uint nFileSystemNameSize);

      /// <summary>Retrieves a volume GUID path for the volume that is associated with the specified volume mount point (drive letter, volume GUID path, or mounted folder).</summary>
      /// <returns>
      /// If the function succeeds, the return value is nonzero.
      /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
      /// </returns>
      /// <remarks>Use GetVolumeNameForVolumeMountPoint to obtain a volume GUID path for use with functions such as SetVolumeMountPoint and FindFirstVolumeMountPoint that require a volume GUID path as an input parameter.</remarks>
      /// <remarks>SMB does not support volume management functions.</remarks>
      /// <remarks>Mount points aren't supported by ReFS volumes.</remarks>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetVolumeNameForVolumeMountPointW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetVolumeNameForVolumeMountPoint([MarshalAs(UnmanagedType.LPWStr)] string lpszVolumeMountPoint, StringBuilder lpszVolumeName, [MarshalAs(UnmanagedType.U4)] uint cchBufferLength);

      /// <summary>Retrieves the volume mount point where the specified path is mounted.</summary>
      /// <remarks>
      ///   <para>If a specified path is passed, GetVolumePathName returns the path to the volume mount point, which means that it returns the
      ///   root of the volume where the end point of the specified path is located.</para>
      ///   <para>For example, assume that you have volume D mounted at C:\Mnt\Ddrive and volume E mounted at "C:\Mnt\Ddrive\Mnt\Edrive". Also
      ///   assume that you have a file with the path "E:\Dir\Subdir\MyFile".</para>
      ///   <para>If you pass "C:\Mnt\Ddrive\Mnt\Edrive\Dir\Subdir\MyFile" to GetVolumePathName, it returns the path "C:\Mnt\Ddrive\Mnt\Edrive\".</para>
      ///   <para>If a network share is specified, GetVolumePathName returns the shortest path for which GetDriveType returns DRIVE_REMOTE,
      ///   which means that the path is validated as a remote drive that exists, which the current user can access.</para>
      ///   <para>Minimum supported client: Windows XP [desktop apps only]</para>
      ///   <para>Minimum supported server: Windows Server 2003 [desktop apps only]</para>
      /// </remarks>
      /// <returns>
      ///   <para>If the function succeeds, the return value is nonzero.</para>
      ///   <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetVolumePathNameW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetVolumePathName([MarshalAs(UnmanagedType.LPWStr)] string lpszFileName, StringBuilder lpszVolumePathName, [MarshalAs(UnmanagedType.U4)] uint cchBufferLength);

      /// <summary>Retrieves a list of drive letters and mounted folder paths for the specified volume.</summary>
      /// <remarks>Minimum supported client: Windows XP.</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003.</remarks>
      /// <returns>
      ///   If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error
      ///   information, call GetLastError.
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetVolumePathNamesForVolumeNameW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool GetVolumePathNamesForVolumeName([MarshalAs(UnmanagedType.LPWStr)] string lpszVolumeName, char[] lpszVolumePathNames, [MarshalAs(UnmanagedType.U4)] uint cchBuferLength, [MarshalAs(UnmanagedType.U4)] out uint lpcchReturnLength);

      /// <summary>Sets the label of a file system volume.</summary>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only].</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only].</remarks>
      /// <remarks>"lpRootPathName" must end with a trailing backslash.</remarks>
      /// <returns>
      ///   If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error
      ///   information, call GetLastError.
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SetVolumeLabelW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool SetVolumeLabel([MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName, [MarshalAs(UnmanagedType.LPWStr)] string lpVolumeName);

      /// <summary>Associates a volume with a drive letter or a directory on another volume.</summary>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only].</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only].</remarks>
      /// <returns>
      ///   If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error
      ///   information, call GetLastError.
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "SetVolumeMountPointW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.Bool)]
      internal static extern bool SetVolumeMountPoint([MarshalAs(UnmanagedType.LPWStr)] string lpszVolumeMountPoint, [MarshalAs(UnmanagedType.LPWStr)] string lpszVolumeName);

      /// <summary>Retrieves information about MS-DOS device names.</summary>
      /// <remarks>Minimum supported client: Windows XP [desktop apps only].</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only].</remarks>
      /// <returns>
      ///   If the function succeeds, the return value is the number of TCHARs stored into the buffer pointed to by lpTargetPath. If the
      ///   function fails, the return value is zero. To get extended error information, call GetLastError. If the buffer is too small, the
      ///   function fails and the last error code is ERROR_INSUFFICIENT_BUFFER.
      /// </returns>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "QueryDosDeviceW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.U4)]
      internal static extern uint QueryDosDevice([MarshalAs(UnmanagedType.LPWStr)] string lpDeviceName, char[] lpTargetPath, [MarshalAs(UnmanagedType.U4)] uint ucchMax);
   }
}