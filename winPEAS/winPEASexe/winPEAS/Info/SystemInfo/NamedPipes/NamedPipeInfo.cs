namespace winPEAS.Info.SystemInfo.NamedPipes
{
    internal class NamedPipeInfo
    {
        public string Name { get; }
        public string Sddl { get; }
        public string CurrentUserPerms { get; }

        public NamedPipeInfo(string name, string sddl, string currentUserPerms)
        {
            Name = name;
            Sddl = sddl;
            CurrentUserPerms = currentUserPerms;
        }
    }
}
