namespace winPEAS.Info.SystemInfo.WindowsDefender
{
    internal class WindowsDefender
    {
        public static WindowsDefenderSettingsInfo GetDefenderSettingsInfo()
        {
            return new WindowsDefenderSettingsInfo(
                new WindowsDefenderSettings(@"SOFTWARE\Microsoft\Windows Defender\"),
                new WindowsDefenderSettings(@"SOFTWARE\Policies\Microsoft\Windows Defender\")
            );
        }
    }
}
