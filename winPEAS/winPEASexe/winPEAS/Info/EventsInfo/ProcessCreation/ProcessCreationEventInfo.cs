using System;

namespace winPEAS.Info.EventsInfo.ProcessCreation
{
    internal class ProcessCreationEventInfo
    {
        public DateTime? CreatedAtUtc { get; set; }
        public int EventId { get; set; }
        public string User { get; set; }
        public string Match { get; set; }

        public ProcessCreationEventInfo(
            DateTime? createdAtUtc,
            int eventId,
            string user,
            string match)
        {
            CreatedAtUtc = createdAtUtc;
            EventId = eventId;
            User = user;
            Match = match;
        }
    }
}
