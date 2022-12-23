using System.Collections.Generic;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.NetworkInfo.InternetSettings
{
    class InternetSettings
    {
        public static InternetSettingsInfo GetInternetSettingsInfo()
        {
            var result = new InternetSettingsInfo();

            // List user/system internet settings for zonemapkey (local, trusted, etc.) :
            // 1 = Intranet zone – sites on your local network.
            // 2 = Trusted Sites zone – sites that have been added to your trusted sites.
            // 3 = Internet zone – sites that are on the Internet.
            // 4 = Restricted Sites zone – sites that have been specifically added to your restricted sites.


            IDictionary<string, string> zoneMapKeys = new Dictionary<string, string>()
                                            {
                                                {"0", "My Computer" },
                                                {"1", "Local Intranet Zone"},
                                                {"2", "Trusted Sites Zone"},
                                                {"3", "Internet Zone"},
                                                {"4", "Restricted Sites Zone"}
                                            };

            // lists user/system internet settings, including default proxy info        
            string internetSettingsKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            AddSettings("HKCU", internetSettingsKey, result.GeneralSettings, zoneMapKeys: null);
            AddSettings("HKLM", internetSettingsKey, result.GeneralSettings, zoneMapKeys: null);

            string zoneMapKey = @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMapKey";
            AddSettings("HKCU", zoneMapKey, result.ZoneMaps, zoneMapKeys);
            AddSettings("HKLM", zoneMapKey, result.ZoneMaps, zoneMapKeys);

            // List Zones settings with automatic logons

            /**
             * HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\{0..4}\1A00
             * Logon setting (1A00) may have any one of the following values (hexadecimal):
             * Value    Setting
             *  ---------------------------------------------------------------
             * 0x00000000 Automatically logon with current username and password
             * 0x00010000 Prompt for user name and password
             * 0x00020000 Automatic logon only in the Intranet zone
             * 0x00030000 Anonymous logon
            **/

            IDictionary<uint, string> zoneAuthSettings = new Dictionary<uint, string>()
                                            {
                                                {0x00000000, "Automatically logon with current username and password"},
                                                {0x00010000, "Prompt for user name and password"},
                                                {0x00020000, "Automatic logon only in the Intranet zone"},
                                                {0x00030000, "Anonymous logon"}
                                            };

            for (int i = 0; i <= 4; i++)
            {
                var keyPath = @"Software\Policies\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\" + i;
                var isParsed = uint.TryParse(RegistryHelper.GetRegValue("HKLM", keyPath, "1A00"), out uint authSetting);

                if (isParsed)
                {
                    var zone = zoneMapKeys[i.ToString()];
                    var authSettingStr = zoneAuthSettings[authSetting];

                    result.ZoneAuthSettings.Add(new InternetSettingsKey(
                        "HKLM",
                        keyPath,
                        "1A00",
                        authSetting.ToString(),
                        $"{zone} : {authSettingStr}"
                    ));
                }
            }

            return result;
        }

        private static void AddSettings(string hive, string keyPath, IList<InternetSettingsKey> internetSettingsList, IDictionary<string, string> zoneMapKeys = null)
        {
            var proxySettings = (RegistryHelper.GetRegValues(hive, keyPath) ?? new Dictionary<string, object>());
            if (proxySettings != null)
            {
                foreach (var kvp in proxySettings)
                {
                    string interpretation = zoneMapKeys?[kvp.Value.ToString()];

                    internetSettingsList.Add(new InternetSettingsKey(
                        hive,
                        keyPath,
                        kvp.Key,
                        kvp.Value.ToString(),
                        interpretation));
                }
            }
        }
    }
}
