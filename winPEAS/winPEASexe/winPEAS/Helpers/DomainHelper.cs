using System;
using winPEAS.Native;
using winPEAS.Native.Enums;

namespace winPEAS.Helpers
{
    //////////////////////
    /// IsDomainJoined ///
    //////////////////////
    /// The clases and functions here are dedicated to discover if the current host is joined in a domain or not, and get the domain name if so
    /// It can be done using .Net (default) and WMI (used if .Net fails)

    internal static class DomainHelper
    {
        internal class Win32
        {
            public const int ErrorSuccess = 0;


        }

        public static string IsDomainJoined()
        {
            // returns Compuer Domain if the system is inside an AD (an nothing if it is not)
            try
            {
                NetJoinStatus status = NetJoinStatus.NetSetupUnknownStatus;
                IntPtr pDomain = IntPtr.Zero;
                int result = Netapi32.NetGetJoinInformation(null, out pDomain, out status);
                if (pDomain != IntPtr.Zero)
                {
                    Netapi32.NetApiBufferFree(pDomain);
                }

                if (result == Win32.ErrorSuccess)
                {
                    // If in domain, return domain name, if not, return empty
                    return status == NetJoinStatus.NetSetupDomainName ? Environment.UserDomainName : "";
                }

            }

            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}\n Trying to check if domain is joined using WMI", ex.Message));
                return IsDomainJoinedWmi();
            }
            return "";
        }

        private static string IsDomainJoinedWmi()
        {
            // returns Compuer Domain if the system is inside an AD (an nothing if it is not)
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    using (var items = searcher.Get())
                    {
                        foreach (var item in items)
                        {
                            return (string)item["Domain"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            //By default local
            return "";
        }
    }
}
