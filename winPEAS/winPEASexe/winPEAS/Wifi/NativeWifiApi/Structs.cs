using System;
using System.Runtime.InteropServices;

namespace winPEAS.Wifi.NativeWifiApi
{
    /// <summary>
    /// Contains information provided when registering for notifications.
    /// </summary>
    /// <remarks>
    /// Corresponds to the native <c>WLAN_NOTIFICATION_DATA</c> type.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct WlanNotificationData
    {
        /// <summary>
        /// Specifies where the notification comes from.
        /// </summary>
        /// <remarks>
        /// On Windows XP SP2, this field must be set to <see cref="WlanNotificationSource.None"/>, <see cref="WlanNotificationSource.All"/> or <see cref="WlanNotificationSource.ACM"/>.
        /// </remarks>
        public WlanNotificationSource notificationSource;
        /// <summary>
        /// Indicates the type of notification. The value of this field indicates what type of associated data will be present in <see cref="dataPtr"/>.
        /// </summary>
        public int notificationCode;
        /// <summary>
        /// Indicates which interface the notification is for.
        /// </summary>
        public Guid interfaceGuid;
        /// <summary>
        /// Specifies the size of <see cref="dataPtr"/>, in bytes.
        /// </summary>
        public int dataSize;
        /// <summary>
        /// Pointer to additional data needed for the notification, as indicated by <see cref="notificationCode"/>.
        /// </summary>
        public IntPtr dataPtr;
    }

    /// <summary>
    /// Specifies the parameters used when using the <see cref="Wlan.WlanConnect"/> function.
    /// </summary>
    /// <remarks>
    /// Corresponds to the native <c>WLAN_CONNECTION_PARAMETERS</c> type.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct WlanConnectionParameters
    {
        /// <summary>
        /// Specifies the mode of connection.
        /// </summary>
        public WlanConnectionMode wlanConnectionMode;
        /// <summary>
        /// Specifies the profile being used for the connection.
        /// The contents of the field depend on the <see cref="wlanConnectionMode"/>:
        /// <list type="table">
        /// <listheader>
        /// <term>Value of <see cref="wlanConnectionMode"/></term>
        /// <description>Contents of the profile string</description>
        /// </listheader>
        /// <item>
        /// <term><see cref="Wlan.WlanConnectionMode.Profile"/></term>
        /// <description>The name of the profile used for the connection.</description>
        /// </item>
        /// <item>
        /// <term><see cref="Wlan.WlanConnectionMode.TemporaryProfile"/></term>
        /// <description>The XML representation of the profile used for the connection.</description>
        /// </item>
        /// <item>
        /// <term><see cref="Wlan.WlanConnectionMode.DiscoverySecure"/>, <see cref="Wlan.WlanConnectionMode.DiscoveryUnsecure"/> or <see cref="Wlan.WlanConnectionMode.Auto"/></term>
        /// <description><c>null</c></description>
        /// </item>
        /// </list>
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string profile;
        /// <summary>
        /// Pointer to a <see cref="Wlan.Dot11Ssid"/> structure that specifies the SSID of the network to connect to.
        /// This field is optional. When set to <c>null</c>, all SSIDs in the profile will be tried.
        /// This field must not be <c>null</c> if <see cref="wlanConnectionMode"/> is set to <see cref="Wlan.WlanConnectionMode.DiscoverySecure"/> or <see cref="Wlan.WlanConnectionMode.DiscoveryUnsecure"/>.
        /// </summary>
        public IntPtr dot11SsidPtr;
        /// <summary>
        /// Pointer to a <see cref="Dot11BssidList"/> structure that contains the list of basic service set (BSS) identifiers desired for the connection.
        /// </summary>
        /// <remarks>
        /// On Windows XP SP2, must be set to <c>null</c>.
        /// </remarks>
        public IntPtr desiredBssidListPtr;
        /// <summary>
        /// A <see cref="Wlan.Dot11BssType"/> value that indicates the BSS type of the network. If a profile is provided, this BSS type must be the same as the one in the profile.
        /// </summary>
        public Dot11BssType dot11BssType;
        /// <summary>
        /// Specifies ocnnection parameters.
        /// </summary>
        /// <remarks>
        /// On Windows XP SP2, must be set to 0.
        /// </remarks>
        public WlanConnectionFlags flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WlanBssListHeader
    {
        internal uint totalSize;
        internal uint numberOfItems;
    }

    /// <summary>
    /// Contains information about a basic service set (BSS).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WlanBssEntry
    {
        /// <summary>
        /// Contains the SSID of the access point (AP) associated with the BSS.
        /// </summary>
        public Dot11Ssid dot11Ssid;
        /// <summary>
        /// The identifier of the PHY on which the AP is operating.
        /// </summary>
        public uint phyId;
        /// <summary>
        /// Contains the BSS identifier.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] dot11Bssid;
        /// <summary>
        /// Specifies whether the network is infrastructure or ad hoc.
        /// </summary>
        public Dot11BssType dot11BssType;
        public Dot11PhyType dot11BssPhyType;
        /// <summary>
        /// The received signal strength in dBm.
        /// </summary>
        public int rssi;
        /// <summary>
        /// The link quality reported by the driver. Ranges from 0-100.
        /// </summary>
        public uint linkQuality;
        /// <summary>
        /// If 802.11d is not implemented, the network interface card (NIC) must set this field to TRUE. If 802.11d is implemented (but not necessarily enabled), the NIC must set this field to TRUE if the BSS operation complies with the configured regulatory domain.
        /// </summary>
        public bool inRegDomain;
        /// <summary>
        /// Contains the beacon interval value from the beacon packet or probe response.
        /// </summary>
        public ushort beaconPeriod;
        /// <summary>
        /// The timestamp from the beacon packet or probe response.
        /// </summary>
        public ulong timestamp;
        /// <summary>
        /// The host timestamp value when the beacon or probe response is received.
        /// </summary>
        public ulong hostTimestamp;
        /// <summary>
        /// The capability value from the beacon packet or probe response.
        /// </summary>
        public ushort capabilityInformation;
        /// <summary>
        /// The frequency of the center channel, in kHz.
        /// </summary>
        public uint chCenterFrequency;
        /// <summary>
        /// Contains the set of data transfer rates supported by the BSS.
        /// </summary>
        public WlanRateSet wlanRateSet;
        /// <summary>
        /// Offset of the information element (IE) data blob.
        /// </summary>
        public uint ieOffset;
        /// <summary>
        /// Size of the IE data blob, in bytes.
        /// </summary>
        public uint ieSize;
    }

    /// <summary>
    /// Contains the set of supported data rates.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WlanRateSet
    {
        /// <summary>
        /// The length, in bytes, of <see cref="rateSet"/>.
        /// </summary>
        private uint rateSetLength;
        /// <summary>
        /// An array of supported data transfer rates.
        /// If the rate is a basic rate, the first bit of the rate value is set to 1.
        /// A basic rate is the data transfer rate that all stations in a basic service set (BSS) can use to receive frames from the wireless medium.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 126)]
        private ushort[] rateSet;
    }

    /// <summary>
    /// Contains the SSID of an interface.
    /// </summary>
    public struct Dot11Ssid
    {
        /// <summary>
        /// The length, in bytes, of the <see cref="SSID"/> array.
        /// </summary>
        public uint SSIDLength;
        /// <summary>
        /// The SSID.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] SSID;
    }

    /// <summary>
    /// Contains information about a LAN interface.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WlanInterfaceInfo
    {
        /// <summary>
        /// The GUID of the interface.
        /// </summary>
        public Guid interfaceGuid;
        /// <summary>
        /// The description of the interface.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Wlan.WLAN_MAX_NAME_LENGTH)]
        public string interfaceDescription;
        /// <summary>
        /// The current state of the interface.
        /// </summary>
        public WlanInterfaceState isState;
    }

    /// <summary>
    /// The header of the list returned by <see cref="Wlan.WlanEnumInterfaces"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WlanInterfaceInfoListHeader
    {
        public uint numberOfItems;
        public uint index;
    }

    /// <summary>
    /// The header of the list returned by <see cref="Wlan.WlanGetProfileList"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WlanProfileInfoListHeader
    {
        public uint numberOfItems;
        public uint index;
    }

    /// <summary>
    /// Contains basic information about a profile.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WlanProfileInfo
    {
        /// <summary>
        /// The name of the profile. This value may be the name of a domain if the profile is for provisioning. Profile names are case-sensitive.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Wlan.WLAN_MAX_NAME_LENGTH)]
        public string profileName;
        /// <summary>
        /// Profile flags.
        /// </summary>
        public WlanProfileFlags profileFlags;
    }
}
