namespace winPEAS.Info.FilesInfo.Office.OneDrive
{
    internal class CloudSyncProviderInfo
    {
        public CloudSyncProviderInfo(string sid, OneDriveSyncProviderInfo oneDriveSyncProviderInfo)
        {
            Sid = sid;
            OneDriveSyncProviderInfo = oneDriveSyncProviderInfo;
        }
        public string Sid { get; }
        public OneDriveSyncProviderInfo OneDriveSyncProviderInfo { get; }
    }
}
