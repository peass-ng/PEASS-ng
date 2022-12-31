namespace winPEAS.Info.WindowsCreds
{
    internal class RDPServerSettings
    {
        public uint? NetworkLevelAuthentication { get; }
        public uint? BlockClipboardRedirection { get; }
        public uint? BlockComPortRedirection { get; }
        public uint? BlockDriveRedirection { get; }
        public uint? BlockLptPortRedirection { get; }
        public uint? AllowSmartCardRedirection { get; }
        public uint? BlockPnPDeviceRedirection { get; }
        public uint? BlockPrinterRedirection { get; }

        public RDPServerSettings(
            uint? networkLevelAuthentication,
            uint? blockClipboardRedirection,
            uint? blockComPortRedirection,
            uint? blockDriveRedirection,
            uint? blockLptPortRedirection,
            uint? allowSmartCardRedirection,
            uint? blockPnPDeviceRedirection,
            uint? blockPrinterRedirection)
        {
            NetworkLevelAuthentication = networkLevelAuthentication;
            BlockClipboardRedirection = blockClipboardRedirection;
            BlockComPortRedirection = blockComPortRedirection;
            BlockDriveRedirection = blockDriveRedirection;
            BlockLptPortRedirection = blockLptPortRedirection;
            AllowSmartCardRedirection = allowSmartCardRedirection;
            BlockPnPDeviceRedirection = blockPnPDeviceRedirection;
            BlockPrinterRedirection = blockPrinterRedirection;
        }
    }
}
