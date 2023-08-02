using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace Alphaleonis.Win32.Filesystem
{
   internal partial class NativeMethods
   {
      /// <summary>Opens an encrypted file in order to backup (export) or restore (import) the file.</summary>
      /// <returns>If the function succeeds, it returns ERROR_SUCCESS.</returns>
      /// <returns>If the function fails, it returns a nonzero error code defined in WinError.h. You can use FormatMessage with the FORMAT_MESSAGE_FROM_SYSTEM flag to get a generic text description of the error.</returns>
      /// <remarks>Minimum supported client: Windows XP Professional [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      /// <param name="lpFileName">The name of the file to be opened.</param>
      /// <param name="ulFlags">The operation to be performed.</param>
      /// <param name="pvContext">[out] The address of a context block that must be presented in subsequent calls to
      /// ReadEncryptedFileRaw, WriteEncryptedFileRaw, or CloseEncryptedFileRaw.</param>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("Advapi32.dll", SetLastError = false, CharSet = CharSet.Unicode, EntryPoint = "OpenEncryptedFileRawW"), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.U4)]
      internal static extern uint OpenEncryptedFileRaw([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, EncryptedFileRawMode ulFlags, out SafeEncryptedFileRawHandle pvContext);


      /// <summary>Closes an encrypted file after a backup or restore operation, and frees associated system resources.</summary>
      /// <remarks>Minimum supported client: Windows XP Professional [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      /// <param name="pvContext">A pointer to a system-defined context block. The OpenEncryptedFileRaw function returns the context block.</param>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("Advapi32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      internal static extern void CloseEncryptedFileRaw(IntPtr pvContext);


      /// <summary>Backs up (export) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <returns>If the function succeeds, it returns ERROR_SUCCESS.</returns>
      /// <returns>If the function fails, it returns a nonzero error code defined in WinError.h. You can use FormatMessage with the FORMAT_MESSAGE_FROM_SYSTEM flag to get a generic text description of the error.</returns>
      /// <remarks>Minimum supported client: Windows XP Professional [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule"), SuppressUnmanagedCodeSecurity]
      [DllImport("Advapi32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.U4)]
      internal static extern uint ReadEncryptedFileRaw([MarshalAs(UnmanagedType.FunctionPtr)] EncryptedFileRawExportCallback pfExportCallback, IntPtr pvCallbackContext, SafeEncryptedFileRawHandle pvContext);


      /// <summary>Restores (import) encrypted files. This is one of a group of Encrypted File System (EFS) functions that is intended to implement backup and restore functionality, while maintaining files in their encrypted state.</summary>
      /// <returns>If the function succeeds, it returns ERROR_SUCCESS.</returns>
      /// <returns>If the function fails, it returns a nonzero error code defined in WinError.h. You can use FormatMessage with the FORMAT_MESSAGE_FROM_SYSTEM flag to get a generic text description of the error.</returns>
      /// <remarks>Minimum supported client: Windows XP Professional [desktop apps only]</remarks>
      /// <remarks>Minimum supported server: Windows Server 2003 [desktop apps only]</remarks>
      [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage"), SuppressMessage("Microsoft.Security", "CA5122:PInvokesShouldNotBeSafeCriticalFxCopRule")]
      [DllImport("Advapi32.dll", SetLastError = false, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
      [return: MarshalAs(UnmanagedType.U4)]
      internal static extern uint WriteEncryptedFileRaw([MarshalAs(UnmanagedType.FunctionPtr)] EncryptedFileRawImportCallback pfExportCallback, IntPtr pvCallbackContext, SafeEncryptedFileRawHandle pvContext);


      [SuppressUnmanagedCodeSecurity]
      internal delegate int EncryptedFileRawExportCallback(IntPtr pbData, IntPtr pvCallbackContext, uint ulLength);

      [SuppressUnmanagedCodeSecurity]
      internal delegate int EncryptedFileRawImportCallback(IntPtr pbData, IntPtr pvCallbackContext, ref uint ulLength);
   }
}
