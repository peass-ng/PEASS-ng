using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace winPEAS.NativeWifiApi
{
    class WlanClient
    {
        public class WlanInterface
        {
            private WlanClient client;
            private Wlan.WlanInterfaceInfo info;
    
            #region Event queue
            
            private bool queueEvents;
            private AutoResetEvent eventQueueFilled = new AutoResetEvent(false);
            private Queue<object> eventQueue = new Queue<object>();
        
            #endregion

            internal WlanInterface(WlanClient client, Wlan.WlanInterfaceInfo info)
            {
                this.client = client;
                this.info = info;               
            }


            /// <summary>
            /// Converts a pointer to a BSS list (header + entries) to an array of BSS entries.
            /// </summary>
            /// <param name="bssListPtr">A pointer to a BSS list's header.</param>
            /// <returns>An array of BSS entries.</returns>
            private Wlan.WlanBssEntry[] ConvertBssListPtr(IntPtr bssListPtr)
            {
                Wlan.WlanBssListHeader bssListHeader = (Wlan.WlanBssListHeader)Marshal.PtrToStructure(bssListPtr, typeof(Wlan.WlanBssListHeader));
                long bssListIt = bssListPtr.ToInt64() + Marshal.SizeOf(typeof(Wlan.WlanBssListHeader));
                Wlan.WlanBssEntry[] bssEntries = new Wlan.WlanBssEntry[bssListHeader.numberOfItems];
                for (int i = 0; i < bssListHeader.numberOfItems; ++i)
                {
                    bssEntries[i] = (Wlan.WlanBssEntry)Marshal.PtrToStructure(new IntPtr(bssListIt), typeof(Wlan.WlanBssEntry));
                    bssListIt += Marshal.SizeOf(typeof(Wlan.WlanBssEntry));
                }
                return bssEntries;
            }

            /// <summary>
            /// Retrieves the basic service sets (BSS) list of all available networks.
            /// </summary>
            public Wlan.WlanBssEntry[] GetNetworkBssList()
            {
                IntPtr bssListPtr;
                Wlan.ThrowIfError(
                    Wlan.WlanGetNetworkBssList(client.clientHandle, info.interfaceGuid, IntPtr.Zero, Wlan.Dot11BssType.Any, false, IntPtr.Zero, out bssListPtr));
                try
                {
                    return ConvertBssListPtr(bssListPtr);
                }
                finally
                {
                    Wlan.WlanFreeMemory(bssListPtr);
                }
            }

            /// <summary>
            /// Retrieves the basic service sets (BSS) list of the specified network.
            /// </summary>
            /// <param name="ssid">Specifies the SSID of the network from which the BSS list is requested.</param>
            /// <param name="bssType">Indicates the BSS type of the network.</param>
            /// <param name="securityEnabled">Indicates whether security is enabled on the network.</param>
            public Wlan.WlanBssEntry[] GetNetworkBssList(Wlan.Dot11Ssid ssid, Wlan.Dot11BssType bssType, bool securityEnabled)
            {
                IntPtr ssidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ssid));
                Marshal.StructureToPtr(ssid, ssidPtr, false);
                try
                {
                    IntPtr bssListPtr;
                    Wlan.ThrowIfError(
                        Wlan.WlanGetNetworkBssList(client.clientHandle, info.interfaceGuid, ssidPtr, bssType, securityEnabled, IntPtr.Zero, out bssListPtr));
                    try
                    {
                        return ConvertBssListPtr(bssListPtr);
                    }
                    finally
                    {
                        Wlan.WlanFreeMemory(bssListPtr);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ssidPtr);
                }
            }

            /// <summary>
            /// Gets the profile's XML specification.
            /// </summary>
            /// <param name="profileName">The name of the profile.</param>
            /// <param name="unencryptedPassword">Whether the password should be unencrypted in the returned XML. By default this is false and the password is left encrypted.</param>
            /// <returns>The XML document.</returns>
            public string GetProfileXml(string profileName, bool unencryptedPassword = true)
            {
                IntPtr profileXmlPtr;
                Wlan.WlanProfileFlags flags = unencryptedPassword ? Wlan.WlanProfileFlags.GetPlaintextKey : Wlan.WlanProfileFlags.None;
                Wlan.WlanAccess access;
                Wlan.ThrowIfError(
                    Wlan.WlanGetProfile(client.clientHandle, info.interfaceGuid, profileName, IntPtr.Zero, out profileXmlPtr, out flags,
                                   out access));
                try
                {
                    return Marshal.PtrToStringUni(profileXmlPtr);
                }
                finally
                {
                    Wlan.WlanFreeMemory(profileXmlPtr);
                }
            }

            /// <summary>
            /// Gets the information of all profiles on this interface.
            /// </summary>
            /// <returns>The profiles information.</returns>
            public Wlan.WlanProfileInfo[] GetProfiles()
            {
                IntPtr profileListPtr;
                Wlan.ThrowIfError(
                    Wlan.WlanGetProfileList(client.clientHandle, info.interfaceGuid, IntPtr.Zero, out profileListPtr));
                try
                {
                    Wlan.WlanProfileInfoListHeader header = (Wlan.WlanProfileInfoListHeader)Marshal.PtrToStructure(profileListPtr, typeof(Wlan.WlanProfileInfoListHeader));
                    Wlan.WlanProfileInfo[] profileInfos = new Wlan.WlanProfileInfo[header.numberOfItems];
                    long profileListIterator = profileListPtr.ToInt64() + Marshal.SizeOf(header);
                    for (int i = 0; i < header.numberOfItems; ++i)
                    {
                        Wlan.WlanProfileInfo profileInfo = (Wlan.WlanProfileInfo)Marshal.PtrToStructure(new IntPtr(profileListIterator), typeof(Wlan.WlanProfileInfo));
                        profileInfos[i] = profileInfo;
                        profileListIterator += Marshal.SizeOf(profileInfo);
                    }
                    return profileInfos;
                }
                finally
                {
                    Wlan.WlanFreeMemory(profileListPtr);
                }
            }

            /// <summary>
            /// Enqueues a notification event to be processed serially.
            /// </summary>
            private void EnqueueEvent(object queuedEvent)
            {
                lock (eventQueue)
                    eventQueue.Enqueue(queuedEvent);
                eventQueueFilled.Set();
            }       
        }

        private IntPtr clientHandle;
        private uint negotiatedVersion;
        private Wlan.WlanNotificationCallbackDelegate wlanNotificationCallback;

        private Dictionary<Guid, WlanInterface> ifaces = new Dictionary<Guid, WlanInterface>();

        public WlanClient()
        {
            Wlan.ThrowIfError(
                Wlan.WlanOpenHandle(Wlan.WLAN_CLIENT_VERSION_XP_SP2, IntPtr.Zero, out negotiatedVersion, out clientHandle));          
        }

        ~WlanClient()
        {
            Wlan.WlanCloseHandle(clientHandle, IntPtr.Zero);
        }
            
        /// <summary>
        /// Gets the WLAN interfaces.
        /// </summary>
        /// <value>The WLAN interfaces.</value>
        public WlanInterface[] Interfaces
        {
            get
            {
                IntPtr ifaceList;
                Wlan.ThrowIfError(
                    Wlan.WlanEnumInterfaces(clientHandle, IntPtr.Zero, out ifaceList));
                try
                {
                    Wlan.WlanInterfaceInfoListHeader header =
                        (Wlan.WlanInterfaceInfoListHeader)Marshal.PtrToStructure(ifaceList, typeof(Wlan.WlanInterfaceInfoListHeader));
                    Int64 listIterator = ifaceList.ToInt64() + Marshal.SizeOf(header);
                    WlanInterface[] interfaces = new WlanInterface[header.numberOfItems];
                    List<Guid> currentIfaceGuids = new List<Guid>();
                    for (int i = 0; i < header.numberOfItems; ++i)
                    {
                        Wlan.WlanInterfaceInfo info =
                            (Wlan.WlanInterfaceInfo)Marshal.PtrToStructure(new IntPtr(listIterator), typeof(Wlan.WlanInterfaceInfo));
                        listIterator += Marshal.SizeOf(info);
                        WlanInterface wlanIface;
                        currentIfaceGuids.Add(info.interfaceGuid);
                        if (ifaces.ContainsKey(info.interfaceGuid))
                            wlanIface = ifaces[info.interfaceGuid];
                        else
                            wlanIface = new WlanInterface(this, info);
                        interfaces[i] = wlanIface;
                        ifaces[info.interfaceGuid] = wlanIface;
                    }

                    // Remove stale interfaces
                    Queue<Guid> deadIfacesGuids = new Queue<Guid>();
                    foreach (Guid ifaceGuid in ifaces.Keys)
                    {
                        if (!currentIfaceGuids.Contains(ifaceGuid))
                            deadIfacesGuids.Enqueue(ifaceGuid);
                    }
                    while (deadIfacesGuids.Count != 0)
                    {
                        Guid deadIfaceGuid = deadIfacesGuids.Dequeue();
                        ifaces.Remove(deadIfaceGuid);
                    }

                    return interfaces;
                }
                finally
                {
                    Wlan.WlanFreeMemory(ifaceList);
                }
            }
        }
    }
}
