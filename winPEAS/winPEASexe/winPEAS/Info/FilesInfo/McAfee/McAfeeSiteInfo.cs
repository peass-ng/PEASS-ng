namespace winPEAS.Info.FilesInfo.McAfee
{
    internal class McAfeeSiteInfo
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Server { get; set; }
        public string RelativePath { get; set; }
        public string ShareName { get; set; }
        public string UserName { get; set; }
        public string DomainName { get; set; }
        public string EncPassword { get; set; }
        public string DecPassword { get; set; }

        public McAfeeSiteInfo(
            string type,
            string name,
            string server,
            string relativePath,
            string shareName,
            string userName,
            string domainName,
            string encPassword,
            string decPassword)
        {
            Type = type;
            Name = name;
            Server = server;
            RelativePath = relativePath;
            ShareName = shareName;
            UserName = userName;
            DomainName = domainName;
            EncPassword = encPassword;
            DecPassword = decPassword;
        }
    }
}
