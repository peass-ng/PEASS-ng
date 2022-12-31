namespace winPEAS.KnownFileCreds.SecurityPackages
{
    internal class NtlmHashInfo
    {
        public string Version { get; private set; }
        public string Hash { get; private set; }

        public NtlmHashInfo(string version, string hash)
        {
            Version = version;
            Hash = hash;
        }
    }
}
