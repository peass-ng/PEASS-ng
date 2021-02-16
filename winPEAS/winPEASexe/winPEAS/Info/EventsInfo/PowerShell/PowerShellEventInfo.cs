using System;

namespace winPEAS.Info.EventsInfo.PowerShell
{
    internal class PowerShellEventInfo
    {
        public DateTime? CreatedAt { get; }
        public int EventId { get; }
        public string UserId { get; }
        public string Match { get; }
        public string Context { get; }

        public PowerShellEventInfo(
            DateTime? createdAt,
            int eventId,
            string userId,
            string match,
            string context)
        {
            CreatedAt = createdAt;
            EventId = eventId;
            UserId = userId;
            Match = match;
            Context = context;
        }
    }
}
