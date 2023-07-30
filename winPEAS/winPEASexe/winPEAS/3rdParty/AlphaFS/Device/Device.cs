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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Text;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Provides static methods to retrieve device resource information from a local or remote host.</summary>
   public static class Device
   {
      #region Enumerate Devices

      /// <summary>[AlphaFS] Enumerates all available devices on the local host.</summary>
      /// <returns><see cref="IEnumerable{DeviceInfo}"/> instances of type <see cref="DeviceGuid"/> from the local host.</returns>
      /// <param name="deviceGuid">One of the <see cref="DeviceGuid"/> devices.</param>
      //[SecurityCritical]
      //public static IEnumerable<DeviceInfo> EnumerateDevices(DeviceGuid deviceGuid)
      //{
      //   return EnumerateDevicesCore(null, deviceGuid, true);
      //}


      ///// <summary>[AlphaFS] Enumerates all available devices of type <see cref="DeviceGuid"/> on the local or remote host.</summary>
      ///// <returns><see cref="IEnumerable{DeviceInfo}"/> instances of type <see cref="DeviceGuid"/> for the specified <paramref name="hostName"/>.</returns>
      ///// <param name="hostName">The name of the local or remote host on which the device resides. <c>null</c> refers to the local host.</param>
      ///// <param name="deviceGuid">One of the <see cref="DeviceGuid"/> devices.</param>
      //[SecurityCritical]
      //public static IEnumerable<DeviceInfo> EnumerateDevices(string hostName, DeviceGuid deviceGuid)
      //{
      //   return EnumerateDevicesCore(hostName, deviceGuid, true);
      //}




      /// <summary>[AlphaFS] Enumerates all available devices on the local or remote host.</summary>
      //[SecurityCritical]
      //internal static IEnumerable<DeviceInfo> EnumerateDevicesCore(string hostName, DeviceGuid deviceGuid, bool getAllProperties)
      //{
      //   if (Utils.IsNullOrWhiteSpace(hostName))
      //      hostName = Environment.MachineName;


      //   // CM_Connect_Machine()
      //   // MSDN Note: Beginning in Windows 8 and Windows Server 2012 functionality to access remote machines has been removed.
      //   // You cannot access remote machines when running on these versions of Windows. 
      //   // http://msdn.microsoft.com/en-us/library/windows/hardware/ff537948%28v=vs.85%29.aspx


      //   SafeCmConnectMachineHandle safeMachineHandle;

      //   var lastError = NativeMethods.CM_Connect_Machine(Host.GetUncName(hostName), out safeMachineHandle);

      //   NativeMethods.IsValidHandle(safeMachineHandle, lastError);


      //   var classGuid = new Guid(Utils.GetEnumDescription(deviceGuid));


      //   // Start at the "Root" of the device tree of the specified machine.

      //   using (safeMachineHandle)
      //   using (var safeHandle = NativeMethods.SetupDiGetClassDevsEx(ref classGuid, IntPtr.Zero, IntPtr.Zero, NativeMethods.SetupDiGetClassDevsExFlags.Present | NativeMethods.SetupDiGetClassDevsExFlags.DeviceInterface, IntPtr.Zero, hostName, IntPtr.Zero))
      //   {
      //      NativeMethods.IsValidHandle(safeHandle, Marshal.GetLastWin32Error());

      //      uint memberInterfaceIndex = 0;
      //      var interfaceStructSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SP_DEVICE_INTERFACE_DATA));
      //      var dataStructSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SP_DEVINFO_DATA));


      //      // Start enumerating device interfaces.

      //      while (true)
      //      {
      //         var interfaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA { cbSize = interfaceStructSize };

      //         var success = NativeMethods.SetupDiEnumDeviceInterfaces(safeHandle, IntPtr.Zero, ref classGuid, memberInterfaceIndex++, ref interfaceData);

      //         lastError = Marshal.GetLastWin32Error();

      //         if (!success)
      //         {
      //            if (lastError != Win32Errors.NO_ERROR && lastError != Win32Errors.ERROR_NO_MORE_ITEMS)
      //               NativeError.ThrowException(lastError, hostName);

      //            break;
      //         }


      //         // Create DeviceInfo instance.

      //         var diData = new NativeMethods.SP_DEVINFO_DATA {cbSize = dataStructSize};

      //         var deviceInfo = new DeviceInfo(hostName) {DevicePath = GetDeviceInterfaceDetail(safeHandle, ref interfaceData, ref diData).DevicePath};


      //         if (getAllProperties)
      //         {
      //            deviceInfo.InstanceId = GetDeviceInstanceId(safeMachineHandle, hostName, diData);

      //            SetDeviceProperties(safeHandle, deviceInfo, diData);
      //         }

      //         else
      //            SetMinimalDeviceProperties(safeHandle, deviceInfo, diData);


      //         yield return deviceInfo;
      //      }
      //   }
      //}


      #region Private Helpers

      [SecurityCritical]
      private static string GetDeviceInstanceId(SafeCmConnectMachineHandle safeMachineHandle, string hostName, NativeMethods.SP_DEVINFO_DATA diData)
      {
         uint ptrPrevious;

         var lastError = NativeMethods.CM_Get_Parent_Ex(out ptrPrevious, diData.DevInst, 0, safeMachineHandle);

         if (lastError != Win32Errors.CR_SUCCESS)
            NativeError.ThrowException(lastError, hostName);


         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(NativeMethods.DefaultFileBufferSize / 8)) // 512
         {
            lastError = NativeMethods.CM_Get_Device_ID_Ex(diData.DevInst, safeBuffer, (uint) safeBuffer.Capacity, 0, safeMachineHandle);

            if (lastError != Win32Errors.CR_SUCCESS)
               NativeError.ThrowException(lastError, hostName);


            // Device InstanceID, such as: "USB\VID_8087&PID_0A2B\5&2EDA7E1E&0&7", "SCSI\DISK&VEN_SANDISK&PROD_X400\4&288ED25&0&000200", ...

            return safeBuffer.PtrToStringUni();
         }
      }


      /// <summary>Builds a Device Interface Detail Data structure.</summary>
      /// <returns>An initialized NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA instance.</returns>
      [SecurityCritical]
      private static NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA GetDeviceInterfaceDetail(SafeHandle safeHandle, ref NativeMethods.SP_DEVICE_INTERFACE_DATA interfaceData, ref NativeMethods.SP_DEVINFO_DATA infoData)
      {
         var didd = new NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA {cbSize = (uint) (IntPtr.Size == 4 ? 6 : 8)};

         var success = NativeMethods.SetupDiGetDeviceInterfaceDetail(safeHandle, ref interfaceData, ref didd, (uint) Marshal.SizeOf(didd), IntPtr.Zero, ref infoData);

         var lastError = Marshal.GetLastWin32Error();

         if (!success)
            NativeError.ThrowException(lastError);

         return didd;
      }


      [SecurityCritical]
      private static string GetDeviceRegistryProperty(SafeHandle safeHandle, NativeMethods.SP_DEVINFO_DATA infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum property)
      {
         var bufferSize = NativeMethods.DefaultFileBufferSize / 8; // 512

         while (true)
            using (var safeBuffer = new SafeGlobalMemoryBufferHandle(bufferSize))
            {
               var success = NativeMethods.SetupDiGetDeviceRegistryProperty(safeHandle, ref infoData, property, IntPtr.Zero, safeBuffer, (uint) safeBuffer.Capacity, IntPtr.Zero);

               var lastError = Marshal.GetLastWin32Error();

               if (success)
               {
                  var value = safeBuffer.PtrToStringUni();

                  return !Utils.IsNullOrWhiteSpace(value) ? value.Trim() : null;
               }


               // MSDN: SetupDiGetDeviceRegistryProperty returns ERROR_INVALID_DATA error code if
               // the requested property does not exist for a device or if the property data is not valid.

               if (lastError == Win32Errors.ERROR_INVALID_DATA)
                  return null;


               bufferSize = GetDoubledBufferSizeOrThrowException(lastError, safeBuffer, bufferSize, property.ToString());
            }
      }



      [SecurityCritical]
      private static void SetDeviceProperties(SafeHandle safeHandle, DeviceInfo deviceInfo, NativeMethods.SP_DEVINFO_DATA infoData)
      {
         SetMinimalDeviceProperties(safeHandle, deviceInfo, infoData);


         deviceInfo.CompatibleIds = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.CompatibleIds);

         deviceInfo.Driver = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Driver);

         deviceInfo.EnumeratorName = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.EnumeratorName);

         deviceInfo.HardwareId = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.HardwareId);

         deviceInfo.LocationInformation = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.LocationInformation);

         deviceInfo.LocationPaths = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.LocationPaths);

         deviceInfo.Manufacturer = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Manufacturer);

         deviceInfo.Service = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Service);
      }


      [SecurityCritical]
      private static void SetMinimalDeviceProperties(SafeHandle safeHandle, DeviceInfo deviceInfo, NativeMethods.SP_DEVINFO_DATA infoData)
      {
         deviceInfo.BaseContainerId = new Guid(GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.BaseContainerId));

         deviceInfo.ClassGuid = new Guid(GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.ClassGuid));

         deviceInfo.DeviceClass = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.Class);

         deviceInfo.DeviceDescription = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.DeviceDescription);

         deviceInfo.FriendlyName = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.FriendlyName);

         deviceInfo.PhysicalDeviceObjectName = GetDeviceRegistryProperty(safeHandle, infoData, NativeMethods.SetupDiGetDeviceRegistryPropertyEnum.PhysicalDeviceObjectName);
      }


      [SecurityCritical]
      internal static int GetDoubledBufferSizeOrThrowException(int lastError, SafeHandle safeBuffer, int bufferSize, string pathForException)
      {
         if (null != safeBuffer && !safeBuffer.IsClosed)
            safeBuffer.Close();


         switch ((uint) lastError)
         {
            case Win32Errors.ERROR_MORE_DATA:
            case Win32Errors.ERROR_INSUFFICIENT_BUFFER:
               bufferSize *= 2;
               break;


            default:
               NativeMethods.IsValidHandle(safeBuffer, lastError, string.Format(CultureInfo.InvariantCulture, "Buffer size: {0}. Path: {1}", bufferSize.ToString(CultureInfo.InvariantCulture), pathForException));
               break;
         }


         return bufferSize;
      }
      

      /// <summary>Repeatedly invokes InvokeIoControl with the specified input until enough memory has been allocated.</summary>
      [SecurityCritical]
      private static void InvokeIoControlUnknownSize<T>(SafeFileHandle handle, uint controlCode, T input, uint increment = 128)
      {
         var inputSize = (uint) Marshal.SizeOf(input);
         var outputLength = increment;

         do
         {
            var output = new byte[outputLength];
            uint bytesReturned;

            var success = NativeMethods.DeviceIoControlUnknownSize(handle, controlCode, input, inputSize, output, outputLength, out bytesReturned, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
            {
               switch ((uint) lastError)
               {
                  case Win32Errors.ERROR_MORE_DATA:
                  case Win32Errors.ERROR_INSUFFICIENT_BUFFER:
                     outputLength += increment;
                     break;

                  default:
                     if (lastError != Win32Errors.ERROR_SUCCESS)
                        NativeError.ThrowException(lastError);
                     break;
               }
            }

            else
               break;

         } while (true);
      }

      #endregion // Private Helpers


      #endregion // Enumerate Devices


      #region Compression

      /// <summary>[AlphaFS] Sets the NTFS compression state of a file or directory on a volume whose file system supports per-file and per-directory compression.</summary>
      /// <param name="transaction">The transaction.</param>
      /// <param name="isFolder">Specifies that <paramref name="path"/> is a file or directory.</param>
      /// <param name="path">A path that describes a folder or file to compress or decompress.</param>
      /// <param name="compress"><c>true</c> = compress, <c>false</c> = decompress</param>
      /// <param name="pathFormat">Indicates the format of the path parameter(s).</param>
      [SecurityCritical]
      internal static void ToggleCompressionCore(KernelTransaction transaction, bool isFolder, string path, bool compress, PathFormat pathFormat)
      {
         using (var handle = File.CreateFileCore(transaction, isFolder, path, ExtendedFileAttributes.BackupSemantics, null, FileMode.Open, FileSystemRights.Modify, FileShare.None, true, false, pathFormat))

            InvokeIoControlUnknownSize(handle, NativeMethods.FSCTL_SET_COMPRESSION, compress ? 1 : 0);
      }

      #endregion // Compression


      #region Link

      /// <summary>[AlphaFS] Creates an NTFS directory junction (similar to CMD command: "MKLINK /J").</summary>
      internal static void CreateDirectoryJunction(SafeFileHandle safeHandle, string directoryPath)
      {
         var targetDirBytes = Encoding.Unicode.GetBytes(Path.NonInterpretedPathPrefix + Path.GetRegularPathCore(directoryPath, GetFullPathOptions.AddTrailingDirectorySeparator, false));

         var header = new NativeMethods.ReparseDataBufferHeader
         {
            ReparseTag = ReparsePointTag.MountPoint,
            ReparseDataLength = (ushort) (targetDirBytes.Length + 12)
         };

         var mountPoint = new NativeMethods.MountPointReparseBuffer
         {
            SubstituteNameOffset = 0,
            SubstituteNameLength = (ushort) targetDirBytes.Length,
            PrintNameOffset = (ushort) (targetDirBytes.Length + UnicodeEncoding.CharSize),
            PrintNameLength = 0
         };

         var reparseDataBuffer = new NativeMethods.REPARSE_DATA_BUFFER
         {
            ReparseTag = header.ReparseTag,
            ReparseDataLength = header.ReparseDataLength,

            SubstituteNameOffset = mountPoint.SubstituteNameOffset,
            SubstituteNameLength = mountPoint.SubstituteNameLength,
            PrintNameOffset = mountPoint.PrintNameOffset,
            PrintNameLength = mountPoint.PrintNameLength,

            PathBuffer = new byte[NativeMethods.MAXIMUM_REPARSE_DATA_BUFFER_SIZE - 16] // 16368
         };

         targetDirBytes.CopyTo(reparseDataBuffer.PathBuffer, 0);


         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(Marshal.SizeOf(reparseDataBuffer)))
         {
            safeBuffer.StructureToPtr(reparseDataBuffer, false);

            uint bytesReturned;
            var succes = NativeMethods.DeviceIoControl2(safeHandle, NativeMethods.FSCTL_SET_REPARSE_POINT, safeBuffer, (uint) (targetDirBytes.Length + 20), IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();
            if (!succes)
               NativeError.ThrowException(lastError, directoryPath);
         }
      }


      /// <summary>[AlphaFS] Deletes an NTFS directory junction.</summary>
      internal static void DeleteDirectoryJunction(SafeFileHandle safeHandle)
      {
         var reparseDataBuffer = new NativeMethods.REPARSE_DATA_BUFFER
         {
            ReparseTag = ReparsePointTag.MountPoint,
            ReparseDataLength = 0,
            PathBuffer = new byte[NativeMethods.MAXIMUM_REPARSE_DATA_BUFFER_SIZE - 16] // 16368
         };


         using (var safeBuffer = new SafeGlobalMemoryBufferHandle(Marshal.SizeOf(reparseDataBuffer)))
         {
            safeBuffer.StructureToPtr(reparseDataBuffer, false);

            uint bytesReturned;
            var success = NativeMethods.DeviceIoControl2(safeHandle, NativeMethods.FSCTL_DELETE_REPARSE_POINT, safeBuffer, NativeMethods.REPARSE_DATA_BUFFER_HEADER_SIZE, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
               NativeError.ThrowException(lastError);
         }
      }


      /// <summary>[AlphaFS] Get information about the target of a mount point or symbolic link on an NTFS file system.</summary>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="UnrecognizedReparsePointException"/>
      [SecurityCritical]
      internal static LinkTargetInfo GetLinkTargetInfo(SafeFileHandle safeHandle, string reparsePath)
      {
         using (var safeBuffer = GetLinkTargetData(safeHandle, reparsePath))
         {
            var header = safeBuffer.PtrToStructure<NativeMethods.ReparseDataBufferHeader>(0);

            var marshalReparseBuffer = (int) Marshal.OffsetOf(typeof(NativeMethods.ReparseDataBufferHeader), "data");

            var dataOffset = (int) (marshalReparseBuffer + (header.ReparseTag == ReparsePointTag.MountPoint
               ? Marshal.OffsetOf(typeof(NativeMethods.MountPointReparseBuffer), "data")
               : Marshal.OffsetOf(typeof(NativeMethods.SymbolicLinkReparseBuffer), "data")).ToInt64());

            var dataBuffer = new byte[NativeMethods.MAXIMUM_REPARSE_DATA_BUFFER_SIZE - dataOffset];


            switch (header.ReparseTag)
            {
               // MountPoint can be a junction or mounted drive (mounted drive starts with "\??\Volume").

               case ReparsePointTag.MountPoint:
                  var mountPoint = safeBuffer.PtrToStructure<NativeMethods.MountPointReparseBuffer>(marshalReparseBuffer);

                  safeBuffer.CopyTo(dataOffset, dataBuffer);

                  return new LinkTargetInfo(
                     Encoding.Unicode.GetString(dataBuffer, mountPoint.SubstituteNameOffset, mountPoint.SubstituteNameLength),
                     Encoding.Unicode.GetString(dataBuffer, mountPoint.PrintNameOffset, mountPoint.PrintNameLength));


               case ReparsePointTag.SymLink:
                  var symLink = safeBuffer.PtrToStructure<NativeMethods.SymbolicLinkReparseBuffer>(marshalReparseBuffer);

                  safeBuffer.CopyTo(dataOffset, dataBuffer);

                  return new SymbolicLinkTargetInfo(
                     Encoding.Unicode.GetString(dataBuffer, symLink.SubstituteNameOffset, symLink.SubstituteNameLength),
                     Encoding.Unicode.GetString(dataBuffer, symLink.PrintNameOffset, symLink.PrintNameLength), symLink.Flags);


               default:
                  throw new UnrecognizedReparsePointException(reparsePath);
            }
         }
      }


      /// <summary>[AlphaFS] Get information about the target of a mount point or symbolic link on an NTFS file system.</summary>
      /// <exception cref="NotAReparsePointException"/>
      /// <exception cref="UnrecognizedReparsePointException"/>
      [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposing is controlled.")]
      [SecurityCritical]
      private static SafeGlobalMemoryBufferHandle GetLinkTargetData(SafeFileHandle safeHandle, string reparsePath)
      {
         var safeBuffer = new SafeGlobalMemoryBufferHandle(NativeMethods.MAXIMUM_REPARSE_DATA_BUFFER_SIZE);

         while (true)
         {
            uint bytesReturned;
            var success = NativeMethods.DeviceIoControl(safeHandle, NativeMethods.FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, safeBuffer, (uint) safeBuffer.Capacity, out bytesReturned, IntPtr.Zero);

            var lastError = Marshal.GetLastWin32Error();
            if (!success)
            {
               switch ((uint) lastError)
               {
                  case Win32Errors.ERROR_MORE_DATA:
                  case Win32Errors.ERROR_INSUFFICIENT_BUFFER:

                     // Should not happen since we already use the maximum size.

                     if (safeBuffer.Capacity < bytesReturned)
                        safeBuffer.Close();
                     break;


                  default:
                     if (lastError != Win32Errors.ERROR_SUCCESS)
                        NativeError.ThrowException(lastError, reparsePath);
                     break;
               }
            }

            else
               break;
         }


         return safeBuffer;
      }

      #endregion // Link
   }
}
