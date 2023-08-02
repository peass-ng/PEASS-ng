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
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;

namespace Alphaleonis.Win32.Filesystem
{
   /// <summary>Provides access to information of a device, on a local or remote host.</summary>
   [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
   [Serializable]
   [SecurityCritical]
   public sealed class DeviceInfo
   {
      #region Constructors
      
      /// <summary>Initializes a DeviceInfo class.</summary>
      //[SecurityCritical]
      //public DeviceInfo()
      //{
      //   HostName = Host.GetUncName();
      //}

      /// <summary>Initializes a DeviceInfo class.</summary>
      /// <param name="host">The DNS or NetBIOS name of the remote server. <c>null</c> refers to the local host.</param>
      //[SecurityCritical]
      //public DeviceInfo(string host)
      //{
      //   HostName = Host.GetUncName(host).Replace(Path.UncPrefix, string.Empty);
      //}

      #endregion // Constructors


      #region Methods

      /// <summary>Enumerates all available devices on the local host.</summary>
      /// <param name="deviceGuid">One of the <see cref="Filesystem.DeviceGuid"/> devices.</param>
      /// <returns><see cref="IEnumerable{DeviceInfo}"/> instances of type <see cref="Filesystem.DeviceGuid"/> from the local host.</returns>
      //[SecurityCritical]
      //public IEnumerable<DeviceInfo> EnumerateDevices(DeviceGuid deviceGuid)
      //{
      //   return Device.EnumerateDevicesCore(HostName, deviceGuid, true);
      //}
      
      #endregion // Methods


      #region Properties

      /// <summary>Represents the <see cref="Guid"/> value of the base container identifier (ID) .The Windows Plug and Play (PnP) manager assigns this value to the device node (devnode).</summary>
      public Guid BaseContainerId { get; internal set; }


      /// <summary>Represents the name of the device setup class that a device instance belongs to.</summary>
      public string DeviceClass { get; internal set; }


      /// <summary>Represents the <see cref="Guid"/> of the device setup class that a device instance belongs to.</summary>
      public Guid ClassGuid { get; internal set; }


      /// <summary>Represents the list of compatible identifiers for a device instance.</summary>
      public string CompatibleIds { get; internal set; }


      /// <summary>Represents a description of a device instance.</summary>
      public string DeviceDescription { get; internal set; }


      /// <summary>The device interface path.</summary>
      public string DevicePath { get; internal set; }


      /// <summary>Represents the registry entry name of the driver key for a device instance.</summary>
      public string Driver { get; internal set; }


      /// <summary>Represents the name of the enumerator for a device instance.</summary>
      public string EnumeratorName { get; internal set; }


      /// <summary>Represents the friendly name of a device instance.</summary>
      public string FriendlyName { get; internal set; }


      /// <summary>Represents the list of hardware identifiers for a device instance.</summary>
      public string HardwareId { get; internal set; }


      /// <summary>The host name that was passed to the class constructor.</summary>
      public string HostName { get; internal set; }


      /// <summary>Gets the instance Id of the device.</summary>
      public string InstanceId { get; internal set; }


      /// <summary>Represents the bus-specific physical location of a device instance.</summary>
      public string LocationInformation { get; internal set; }


      /// <summary>Represents the location of a device instance in the device tree.</summary>
      public string LocationPaths { get; internal set; }


      /// <summary>Represents the name of the manufacturer of a device instance.</summary>
      public string Manufacturer { get; internal set; }


      /// <summary>Encapsulates the physical device location information provided by a device's firmware to Windows.</summary>
      public string PhysicalDeviceObjectName { get; internal set; }


      /// <summary>Represents the name of the service that is installed for a device instance.</summary>
      public string Service { get; internal set; }

      #endregion // Properties
   }
}
