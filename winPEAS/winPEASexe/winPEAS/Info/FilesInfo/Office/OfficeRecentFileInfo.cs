using System;

namespace winPEAS.Info.FilesInfo.Office
{
    internal class OfficeRecentFileInfo
    {
        public string Application { get; set; }
        public string User { get; set; }
        public string Target { get; set; }
        public DateTime LastAccessDate { get; set; }
    }
}
