using System.ComponentModel;
using System.Diagnostics;

namespace winPEAS.Wifi.NativeWifiApi
{
    public static class Wlan
    {
        #region P/Invoke API

        public const uint WLAN_CLIENT_VERSION_XP_SP2 = 1;
        public const uint WLAN_CLIENT_VERSION_LONGHORN = 2;
        public const uint WLAN_MAX_NAME_LENGTH = 256;



        #endregion

        /// <summary>
        /// Helper method to wrap calls to Native WiFi API methods.
        /// If the method falls, throws an exception containing the error code.
        /// </summary>
        /// <param name="win32ErrorCode">The error code.</param>
        [DebuggerStepThrough]
        internal static void ThrowIfError(int win32ErrorCode)
        {
            if (win32ErrorCode != 0)
                throw new Win32Exception(win32ErrorCode);
        }
    }
}
