using winPEAS.Native.Enums;

namespace winPEAS.Info.SystemInfo.GroupPolicy
{
    class LocalGroupPolicyInfo
    {
        public object GPOName { get; }
        public object GPOType { get; }
        public object DisplayName { get; }
        public object Link { get; set; }
        public object FileSysPath { get; }
        public GPOOptions Options { get; }
        public GPOLink GPOLink { get; }
        public object Extensions { get; }

        public LocalGroupPolicyInfo(
            object gpoName,
            object gpoType,
            object displayName,
            object link,
            object fileSysPath,
            GPOOptions options,
            GPOLink gpoLink,
            object extensions)
        {
            GPOName = gpoName;
            GPOType = gpoType;
            DisplayName = displayName;
            Link = link;
            FileSysPath = fileSysPath;
            Options = options;
            GPOLink = gpoLink;
            Extensions = extensions;
        }
    }
}
