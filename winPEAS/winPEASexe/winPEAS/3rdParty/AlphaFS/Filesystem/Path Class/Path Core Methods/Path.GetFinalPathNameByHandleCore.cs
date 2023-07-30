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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   public static partial class Path
   {
      /// <summary>Retrieves the final path for the specified file, formatted as <see cref="FinalPathFormats"/>.</summary>
      /// <returns>The final path as a string.</returns>
      /// <remarks>
      ///   A final path is the path that is returned when a path is fully resolved. For example, for a symbolic link named "C:\tmp\mydir" that
      ///   points to "D:\yourdir", the final path would be "D:\yourdir". The string that is returned by this function uses the
      ///   <see cref="LongPathPrefix"/> syntax.
      /// </remarks>
      /// <param name="handle">Then handle to a <see cref="SafeFileHandle"/> instance.</param>
      /// <param name="finalPath">The final path, formatted as <see cref="FinalPathFormats"/></param>
      [SecurityCritical]
      internal static string GetFinalPathNameByHandleCore(SafeFileHandle handle, FinalPathFormats finalPath)
      {
         NativeMethods.IsValidHandle(handle);

         var buffer = new StringBuilder(NativeMethods.MaxPathUnicode);

         using (new NativeMethods.ChangeErrorMode(NativeMethods.ErrorMode.FailCriticalErrors))
         {
            if (NativeMethods.IsAtLeastWindowsVista)
            {
               // MSDN: GetFinalPathNameByHandle(): If the function fails for any other reason, the return value is zero.

               var success = NativeMethods.GetFinalPathNameByHandle(handle, buffer, (uint) buffer.Capacity, finalPath) == Win32Errors.ERROR_SUCCESS;

               var lastError = Marshal.GetLastWin32Error();
               if (!success && lastError != Win32Errors.ERROR_SUCCESS)
                  NativeError.ThrowException(lastError);


               return buffer.ToString();
            }
         }

         
         // Older OperatingSystem

         // Obtaining a File Name From a File Handle
         // http://msdn.microsoft.com/en-us/library/aa366789%28VS.85%29.aspx

         // Be careful when using GetFileSizeEx to check the size of hFile handle of an unknown "File" type object.
         // This is more towards returning a filename from a file handle. If the handle is a named pipe handle it seems to hang the thread.
         // Check for: FileTypes.DiskFile

         // Can't map a 0 byte file.
         long fileSizeHi;
         if (!NativeMethods.GetFileSizeEx(handle, out fileSizeHi))
            if (fileSizeHi == 0)
               return string.Empty;


         // PAGE_READONLY
         // Allows views to be mapped for read-only or copy-on-write access. An attempt to write to a specific region results in an access violation.
         // The file handle that the hFile parameter specifies must be created with the GENERIC_READ access right.
         // PageReadOnly = 0x02,
         using (var handle2 = NativeMethods.CreateFileMapping(handle, null, 2, 0, 1, null))
         {
            NativeMethods.IsValidHandle(handle, Marshal.GetLastWin32Error());

            // FILE_MAP_READ
            // Read = 4
            using (var pMem = NativeMethods.MapViewOfFile(handle2, 4, 0, 0, (UIntPtr)1))
            {
               if (NativeMethods.IsValidHandle(pMem, Marshal.GetLastWin32Error()))
                  if (NativeMethods.GetMappedFileName(Process.GetCurrentProcess().Handle, pMem, buffer, (uint) buffer.Capacity))
                     NativeMethods.UnmapViewOfFile(pMem);
            }
         }


         // Default output from GetMappedFileName(): "\Device\HarddiskVolumeX\path\filename.ext"
         var dosDevice = buffer.Length > 0 ? buffer.ToString() : string.Empty;


         // Select output format.
         switch (finalPath)
         {
            // As-is: "\Device\HarddiskVolumeX\path\filename.ext"
            case FinalPathFormats.VolumeNameNT:
               return dosDevice;


            // To: "\path\filename.ext"
            case FinalPathFormats.VolumeNameNone:
               return DosDeviceToDosPath(dosDevice, string.Empty);


            // To: "\\?\Volume{GUID}\path\filename.ext"
            case FinalPathFormats.VolumeNameGuid:
               var dosPath = DosDeviceToDosPath(dosDevice, null);

               if (!Utils.IsNullOrWhiteSpace(dosPath))
               {
                  var driveLetter = RemoveTrailingDirectorySeparator(GetPathRoot(dosPath, false));
                  var file = GetFileName(dosPath, true);


                  if (!Utils.IsNullOrWhiteSpace(file))
                  { 
                     foreach (var drive in Directory.EnumerateLogicalDrivesCore(false, false)
                        
                        .Select(drv => drv.Name).Where(drv => driveLetter.Equals(RemoveTrailingDirectorySeparator(drv), StringComparison.OrdinalIgnoreCase)))

                        return CombineCore(false, Volume.GetUniqueVolumeNameForPath(drive), GetSuffixedDirectoryNameWithoutRootCore(null, dosPath, PathFormat.FullPath), file);
                  }
               }

               break;
         }


         // To: "\\?\C:\path\filename.ext"
         return !Utils.IsNullOrWhiteSpace(dosDevice) ? LongPathPrefix + DosDeviceToDosPath(dosDevice, null) : string.Empty;
      }


      /// <summary>Tranlates DosDevicePath, Volume GUID. For example: "\Device\HarddiskVolumeX\path\filename.ext" can translate to: "\path\filename.ext" or: "\\?\Volume{GUID}\path\filename.ext".</summary>
      /// <returns>A translated dos path.</returns>
      /// <param name="dosDevice">A DosDevicePath, for example: \Device\HarddiskVolumeX\path\filename.ext.</param>
      /// <param name="deviceReplacement">Alternate path/device text, usually <c>string.Empty</c> or <c>null</c>.</param>
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
      [SecurityCritical]
      private static string DosDeviceToDosPath(string dosDevice, string deviceReplacement)
      {
         if (Utils.IsNullOrWhiteSpace(dosDevice))
            return string.Empty;


         foreach (var drive in Directory.EnumerateLogicalDrivesCore(false, false).Select(drv => drv.Name))
         {
            try
            {
               var path = RemoveTrailingDirectorySeparator(drive);

               foreach (var devNt in Volume.QueryAllDosDevices().Where(device => device.StartsWith(path, StringComparison.OrdinalIgnoreCase)).ToArray())

                  return dosDevice.ReplaceIgnoreCase(devNt, deviceReplacement ?? path);
            }
            catch
            {
            }
         }

         return string.Empty;
      }
   }
}
