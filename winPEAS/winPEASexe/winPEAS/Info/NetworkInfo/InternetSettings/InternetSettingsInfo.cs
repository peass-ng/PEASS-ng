using System.Collections.Generic;

namespace winPEAS.Info.NetworkInfo.InternetSettings
{
    class InternetSettingsInfo
    {
        public IList<InternetSettingsKey> GeneralSettings { get; set; } = new List<InternetSettingsKey>();
        public IList<InternetSettingsKey> ZoneMaps { get; set; } = new List<InternetSettingsKey>();
        public IList<InternetSettingsKey> ZoneAuthSettings { get; set; } = new List<InternetSettingsKey>();
    }
}
