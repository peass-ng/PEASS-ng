using winPEAS.Helpers.Registry;

namespace winPEAS.Info.WindowsCreds
{
    internal class RemoteDesktop
    {
        public static RDPSettingsInfo GetRDPSettingsInfo()
        {
            // Client settings
            var credDelegKey = @"Software\Policies\Microsoft\Windows\CredentialsDelegation";
            var restrictedAdmin = RegistryHelper.GetDwordValue("HKLM", credDelegKey, "RestrictedRemoteAdministration");
            var restrictedAdminType = RegistryHelper.GetDwordValue("HKLM", credDelegKey, "RestrictedRemoteAdministrationType");
            var serverAuthLevel = RegistryHelper.GetDwordValue("HKLM", @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", "AuthenticationLevel");
            var termServKey = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services";
            var disablePwSaving = RegistryHelper.GetDwordValue("HKLM", termServKey, "DisablePasswordSaving");

            // Server settings
            var nla = RegistryHelper.GetDwordValue("HKLM", termServKey, "UserAuthentication");
            var blockClipboard = RegistryHelper.GetDwordValue("HKLM", termServKey, "fDisableClip");
            var blockComPort = RegistryHelper.GetDwordValue("HKLM", termServKey, "fDisableCcm");
            var blockDrives = RegistryHelper.GetDwordValue("HKLM", termServKey, "fDisableCdm");
            var blockLptPort = RegistryHelper.GetDwordValue("HKLM", termServKey, "fDisableLPT");
            var blockSmartCard = RegistryHelper.GetDwordValue("HKLM", termServKey, "fEnableSmartCard");
            var blockPnp = RegistryHelper.GetDwordValue("HKLM", termServKey, "fDisablePNPRedir");
            var blockPrinters = RegistryHelper.GetDwordValue("HKLM", termServKey, "fDisableCpm");

            return new RDPSettingsInfo(
                new RDPClientSettings(
                    restrictedAdmin != null && restrictedAdmin != 0,
                    restrictedAdminType,
                    serverAuthLevel,
                    disablePwSaving == null || disablePwSaving == 1),
                new RDPServerSettings(
                        nla,
                        blockClipboard,
                        blockComPort,
                        blockDrives,
                        blockLptPort,
                        blockSmartCard,
                        blockPnp,
                        blockPrinters
                    )
            );
        }
    }
}
