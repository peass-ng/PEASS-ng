using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using winPEAS.Native;

namespace winPEAS.Wifi.NativeWifiApi
{
    internal class WlanClient
    {
        // c# net api: https://github.com/jorgebv/windows-wifi-api
        // powershell: https://github.com/jcwalker/WiFiProfileManagement

        public class WlanInterface
        {
            private WlanClient client;
            private WlanInterfaceInfo info;

            internal WlanInterface(WlanClient client, WlanInterfaceInfo info)
            {
                this.client = client;
                this.info = info;
            }

            /// <summary>
            /// Gets the profile's XML specification.
            /// </summary>
            /// <param name="profileName">The name of the profile.</param>
            /// <param name="unencryptedPassword">Whether the password should be unencrypted in the returned XML. By default this is false and the password is left encrypted.</param>
            /// <returns>The XML document.</returns>
            public string GetProfileXml(string profileName, bool unencryptedPassword = true)
            {
                var flags = unencryptedPassword ? WlanProfileFlags.GetPlaintextKey : WlanProfileFlags.None;
                Wlan.ThrowIfError(
                    WlanApi.WlanGetProfile(
                        client.clientHandle, info.interfaceGuid, profileName, IntPtr.Zero, out var profileXmlPtr, out flags, out _));

                try
                {
                    return Marshal.PtrToStringUni(profileXmlPtr);
                }
                finally
                {
                    WlanApi.WlanFreeMemory(profileXmlPtr);
                }
            }

            /// <summary>
            /// Gets the information of all profiles on this interface.
            /// </summary>
            /// <returns>The profiles information.</returns>
            public WlanProfileInfo[] GetProfiles()
            {
                Wlan.ThrowIfError(
                    WlanApi.WlanGetProfileList(client.clientHandle, info.interfaceGuid, IntPtr.Zero, out var profileListPtr));
                try
                {
                    var header =
                        (WlanProfileInfoListHeader)Marshal.PtrToStructure(profileListPtr, typeof(WlanProfileInfoListHeader));
                    WlanProfileInfo[] profileInfos = new WlanProfileInfo[header.numberOfItems];
                    long profileListIterator = profileListPtr.ToInt64() + Marshal.SizeOf(header);

                    for (int i = 0; i < header.numberOfItems; ++i)
                    {
                        WlanProfileInfo profileInfo =
                            (WlanProfileInfo)Marshal.PtrToStructure(new IntPtr(profileListIterator), typeof(WlanProfileInfo));
                        profileInfos[i] = profileInfo;
                        profileListIterator += Marshal.SizeOf(profileInfo);
                    }

                    return profileInfos;
                }
                finally
                {
                    WlanApi.WlanFreeMemory(profileListPtr);
                }
            }
        }

        private IntPtr clientHandle;
        private uint negotiatedVersion;

        private Dictionary<Guid, WlanInterface> ifaces = new Dictionary<Guid, WlanInterface>();

        public WlanClient()
        {
            Wlan.ThrowIfError(
                WlanApi.WlanOpenHandle(
                    Wlan.WLAN_CLIENT_VERSION_XP_SP2, IntPtr.Zero, out negotiatedVersion, out clientHandle));
        }

        ~WlanClient()
        {
            if (clientHandle != IntPtr.Zero)
            {
                WlanApi.WlanCloseHandle(clientHandle, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Gets the WLAN interfaces.
        /// </summary>
        /// <value>The WLAN interfaces.</value>
        public WlanInterface[] Interfaces
        {
            get
            {
                Wlan.ThrowIfError(
                    WlanApi.WlanEnumInterfaces(
                        clientHandle, IntPtr.Zero, out var ifaceList));

                try
                {
                    var header = (WlanInterfaceInfoListHeader)Marshal.PtrToStructure(
                        ifaceList,
                        typeof(WlanInterfaceInfoListHeader));

                    Int64 listIterator = ifaceList.ToInt64() + Marshal.SizeOf(header);
                    WlanInterface[] interfaces = new WlanInterface[header.numberOfItems];
                    List<Guid> currentIfaceGuids = new List<Guid>();

                    for (int i = 0; i < header.numberOfItems; ++i)
                    {
                        var info =
                            (WlanInterfaceInfo)Marshal.PtrToStructure(
                                new IntPtr(listIterator),
                                typeof(WlanInterfaceInfo));

                        listIterator += Marshal.SizeOf(info);
                        currentIfaceGuids.Add(info.interfaceGuid);

                        var wlanIface = ifaces.ContainsKey(info.interfaceGuid) ?
                            ifaces[info.interfaceGuid] :
                            new WlanInterface(this, info);

                        interfaces[i] = wlanIface;
                        ifaces[info.interfaceGuid] = wlanIface;
                    }

                    // Remove stale interfaces
                    var deadIfacesGuids = new Queue<Guid>();

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
                    WlanApi.WlanFreeMemory(ifaceList);
                }
            }
        }
    }
}
