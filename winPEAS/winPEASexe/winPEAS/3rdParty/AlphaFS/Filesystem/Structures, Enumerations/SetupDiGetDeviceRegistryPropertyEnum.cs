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

namespace Alphaleonis.Win32.Filesystem
{
   internal static partial class NativeMethods
   {
      /// <summary>Flags for SetupDiGetDeviceRegistryProperty().</summary>
      internal enum SetupDiGetDeviceRegistryPropertyEnum
      {
         /// <summary>SPDRP_DEVICEDESC
         /// <para>Represents a description of a device instance.</para>
         /// </summary>
         DeviceDescription = 0,

         /// <summary>SPDRP_HARDWAREID
         /// <para>Represents the list of hardware identifiers for a device instance.</para>
         /// </summary>
         HardwareId = 1,

         /// <summary>SPDRP_COMPATIBLEIDS
         /// <para>Represents the list of compatible identifiers for a device instance.</para>
         /// </summary>
         CompatibleIds = 2,

         //SPDRP_UNUSED0 = 0x00000003,

         /// <summary>SPDRP_CLASS
         /// <para>Represents the name of the service that is installed for a device instance.</para>
         /// </summary>
         Service = 4,

         //SPDRP_UNUSED1 = 0x00000005,
         //SPDRP_UNUSED2 = 0x00000006,

         /// <summary>SPDRP_CLASS
         /// <para>Represents the name of the device setup class that a device instance belongs to.</para>
         /// </summary>
         Class = 7,

         /// <summary>SPDRP_CLASSGUID
         /// <para>Represents the <see cref="System.Guid"/> of the device setup class that a device instance belongs to.</para>
         /// </summary>
         ClassGuid = 8,

         /// <summary>SPDRP_DRIVER
         /// <para>Represents the registry entry name of the driver key for a device instance.</para>
         /// </summary>
         Driver = 9,

         ///// <summary>SPDRP_CONFIGFLAGS
         ///// Represents the configuration flags that are set for a device instance.
         ///// </summary>
         //ConfigurationFlags = 10,

         /// <summary>SPDRP_MFG
         /// <para>Represents the name of the manufacturer of a device instance.</para>
         /// </summary>
         Manufacturer = 11,

         /// <summary>SPDRP_FRIENDLYNAME
         /// <para>Represents the friendly name of a device instance.</para>
         /// </summary>
         FriendlyName = 12,

         /// <summary>SPDRP_LOCATION_INFORMATION
         /// <para>Represents the bus-specific physical location of a device instance.</para>
         /// </summary>
         LocationInformation = 13,

         /// <summary>SPDRP_PHYSICAL_DEVICE_LOCATION
         /// <para>Encapsulates the physical device location information provided by a device's firmware to Windows.</para>
         /// </summary>
         PhysicalDeviceObjectName = 14,

         ///// <summary>SPDRP_CAPABILITIES
         //// <para>Represents the capabilities of a device instance.</para>
         //// </summary>
         //Capabilities = 15,

         ///// <summary>SPDRP_UI_NUMBER - Represents a number for the device instance that can be displayed in a user interface item.</summary>
         //UiNumber = 16,

         ///// <summary>SPDRP_UPPERFILTERS - Represents a list of the service names of the upper-level filter drivers that are installed for a device instance.</summary>
         //UpperFilters = 17,

         ///// <summary>SPDRP_LOWERFILTERS - Represents a list of the service names of the lower-level filter drivers that are installed for a device instance.</summary>
         //LowerFilters = 18,

         ///// <summary>SPDRP_BUSTYPEGUID - Represents the <see cref="Guid"/> that identifies the bus type of a device instance.</summary>
         //BusTypeGuid = 19,

         ///// <summary>SPDRP_LEGACYBUSTYPE - Represents the legacy bus number of a device instance.</summary>
         //LegacyBusType = 20,

         ///// <summary>SPDRP_BUSNUMBER - Represents the number that identifies the bus instance that a device instance is attached to.</summary>
         //BusNumber = 21,

         /// <summary>SPDRP_ENUMERATOR_NAME
         /// <para>Represents the name of the enumerator for a device instance.</para>
         /// </summary>
         EnumeratorName = 22,

         ///// <summary>SPDRP_SECURITY - Represents a security descriptor structure for a device instance.</summary>
         //Security = 23,

         ///// <summary>SPDRP_SECURITY_SDS - Represents a security descriptor string for a device instance.</summary>
         //SecuritySds = 24,

         ///// <summary>SPDRP_DEVTYPE - Represents the device type of a device instance.</summary>
         //DeviceType = 25,

         ///// <summary>SPDRP_EXCLUSIVE - Represents a Boolean value that determines whether a device instance can be opened for exclusive use.</summary>
         //Exclusive = 26,

         ///// <summary>SPDRP_CHARACTERISTICS - Represents the characteristics of a device instance.</summary>
         //Characteristics = 27,

         ///// <summary>SPDRP_ADDRESS - Represents the bus-specific address of a device instance.</summary>
         //Address = 28,

         ///// <summary>SPDRP_UI_NUMBER_DESC_FORMAT - Represents a printf-compatible format string that you should use to display the value of the <see cref="UiNumber"/> device property for a device instance.</summary>
         //UiNumberDescriptionFormat = 29,

         ///// <summary>SPDRP_DEVICE_POWER_DATA - Represents power information about a device instance.</summary>
         //DevicePowerData = 30,

         ///// <summary>SPDRP_REMOVAL_POLICY - Represents the current removal policy for a device instance.</summary>
         //RemovalPolicy = 31,

         ///// <summary>SPDRP_REMOVAL_POLICY_HW_DEFAULT - Represents the default removal policy for a device instance.</summary>
         //RemovalPolicyDefault = 32,

         ///// <summary>SPDRP_REMOVAL_POLICY_OVERRIDE- Represents the removal policy override for a device instance.</summary>
         //RemovalPolicyOverride = 33,

         ///// <summary>SPDRP_INSTALL_STATE - Represents the installation state of a device instance.</summary>
         //InstallState = 34,

         /// <summary>SPDRP_LOCATION_PATHS
         /// <para>Represents the location of a device instance in the device tree.</para>
         /// </summary>
         LocationPaths = 35,

         /// <summary>SPDRP_BASE_CONTAINERID
         /// <para>Represents the <see cref="System.Guid"/> value of the base container identifier (ID) .The Windows Plug and Play (PnP) manager assigns this value to the device node (devnode).</para>
         /// </summary>
         BaseContainerId = 36
      }
   }
}