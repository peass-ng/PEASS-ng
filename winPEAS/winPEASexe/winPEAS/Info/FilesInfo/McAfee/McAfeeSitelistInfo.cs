using System.Collections.Generic;

namespace winPEAS.Info.FilesInfo.McAfee
{
    internal class McAfeeSitelistInfo
    {
        public string Path { get; set; }
        public List<McAfeeSiteInfo> Sites { get; set; }

        public string ParseException { get; set; }

        public McAfeeSitelistInfo(string path, List<McAfeeSiteInfo> sites, string parseException = null)
        {
            Path = path;
            Sites = sites ?? new List<McAfeeSiteInfo>();
            ParseException = parseException;
        }
    }
}
