namespace winPEAS.Info.SystemInfo.WindowsDefender
{
    class WindowsDefenderSettingsInfo
    {
        public WindowsDefenderSettings LocalSettings { get; set; }
        public WindowsDefenderSettings GroupPolicySettings { get; set; }

        public WindowsDefenderSettingsInfo(WindowsDefenderSettings localSettings, WindowsDefenderSettings groupPolicySettings)
        {
            LocalSettings = localSettings;
            GroupPolicySettings = groupPolicySettings;
        }
    }
}
