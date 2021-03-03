using System;

namespace winPEAS.Info.EventsInfo.Logon
{
    internal class ExplicitLogonEventInfo
    {
        public string SubjectUser { get; set; }
        public string SubjectDomain { get; set; }
        public string TargetUser { get; set; }
        public string TargetDomain { get; set; }
        public string Process { get; set; }
        public string IpAddress { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
    }
}
