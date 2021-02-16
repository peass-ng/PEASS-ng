using System.Collections.Generic;

namespace winPEAS.Info.FilesInfo.Office.OneDrive
{
    internal class OneDriveSyncProviderInfo
    {
        // Stores the mapping between a sync ID and mount point
        public Dictionary<string, Dictionary<string, string>> MpList { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        // Stores the list of OneDrive accounts configured in the registry
        public Dictionary<string, Dictionary<string, string>> OneDriveList { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        // Stores the mapping between the account and the mountpoint IDs
        public Dictionary<string, List<string>> AccountToMountpointDict { get; set; } = new Dictionary<string, List<string>>();
        // Stores the 'used' scopeIDs (to identify orphans)
        public List<string> UsedScopeIDs { get; set; } = new List<string>();
    }
}
