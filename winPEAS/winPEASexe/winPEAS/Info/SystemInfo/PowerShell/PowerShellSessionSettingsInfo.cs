using System.Collections.Generic;

namespace winPEAS.Info.SystemInfo.PowerShell
{
    internal class PowerShellSessionSettingsInfo
    {
        public string Plugin { get; }
        public List<PluginAccessInfo> Permissions { get; }

        public PowerShellSessionSettingsInfo(string plugin, List<PluginAccessInfo> permissions)
        {
            Plugin = plugin;
            Permissions = permissions;
        }
    }
}
