using System;

namespace winPEAS.Info.SystemInfo.SysMon
{
    internal class SysmonEventInfo
    {
        public DateTime? TimeCreated { get; set; }
        public int EventID { get; set; }
        public string UserName { get; set; }
        public string Match { get; set; }

        public SysmonEventInfo()
        {
        }

        public SysmonEventInfo(DateTime? timeCreated, int eventID, string userName, string match)
        {
            this.TimeCreated = timeCreated;
            this.EventID = eventID;
            this.UserName = userName;
        }
    }
}
