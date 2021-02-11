namespace winPEAS.Info.SystemInfo.PowerShell
{
    internal class PluginAccessInfo
    {
        public string Principal { get; }
        public string Sid { get; }
        public string Permission { get; }

        public PluginAccessInfo(
            string principal,
            string sid,
            string permission)
        {
            Principal = principal;
            Sid = sid;
            Permission = permission;
        }
    }
}
