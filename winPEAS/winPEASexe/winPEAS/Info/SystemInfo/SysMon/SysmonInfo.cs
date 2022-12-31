namespace winPEAS.Info.SystemInfo.SysMon
{
    internal class SysmonInfo
    {
        public bool Installed { get; }
        public SysmonHashAlgorithm HashingAlgorithm { get; }
        public SysmonOptions Options { get; }
        public string Rules { get; }

        public SysmonInfo(bool installed, SysmonHashAlgorithm hashingAlgorithm, SysmonOptions options, string rules)
        {
            Installed = installed;
            HashingAlgorithm = hashingAlgorithm;
            Options = options;
            Rules = rules;
        }
    }
}
