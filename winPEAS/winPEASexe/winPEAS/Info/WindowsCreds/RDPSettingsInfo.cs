
namespace winPEAS.Info.WindowsCreds
{
    internal class RDPSettingsInfo
    {
        public RDPClientSettings ClientSettings { get; }
        public RDPServerSettings ServerSettings { get; }

        public RDPSettingsInfo(
            RDPClientSettings clientSettings,
            RDPServerSettings serverSettings)
        {
            ClientSettings = clientSettings;
            ServerSettings = serverSettings;
        }
    }
}
