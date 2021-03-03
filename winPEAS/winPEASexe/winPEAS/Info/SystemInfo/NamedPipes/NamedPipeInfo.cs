namespace winPEAS.Info.SystemInfo.NamedPipes
{
    internal class NamedPipeInfo
    {
        public string Name { get; }
        public string Sddl { get; }

        public NamedPipeInfo(string name, string sddl)
        {
            Name = name;
            Sddl = sddl;
        }
    }
}
