namespace winPEAS.Info.WindowsCreds
{
    internal class RDPClientSettings
    {
        public bool RestrictedRemoteAdministration { get; }
        public uint? RestrictedRemoteAdministrationType { get; }
        public uint? ServerAuthLevel { get; }
        public bool DisablePasswordSaving { get; }

        public RDPClientSettings(
            bool restrictedRemoteAdministration,
            uint? restrictedRemoteAdministrationType,
            uint? serverAuthLevel,
            bool disablePasswordSaving)
        {
            RestrictedRemoteAdministration = restrictedRemoteAdministration;
            RestrictedRemoteAdministrationType = restrictedRemoteAdministrationType;
            ServerAuthLevel = serverAuthLevel;
            DisablePasswordSaving = disablePasswordSaving;
        }
    }
}
