using System;
using System.Collections.Generic;
using winPEAS.Helpers;

namespace winPEAS.Info.EventsInfo.Power
{
    internal class Power
    {
        public static IEnumerable<PowerEventInfo> GetPowerEventInfos(int lastDays)
        {
            var startTime = DateTime.Now.AddDays(-lastDays);
            var endTime = DateTime.Now;

            // eventID 1 == sleep
            var query = $@"((*[System[(EventID=12 or EventID=13) and Provider[@Name='Microsoft-Windows-Kernel-General']]] or *[System/EventID=42]) or (*[System/EventID=6008]) or (*[System/EventID=1] and *[System[Provider[@Name='Microsoft-Windows-Power-Troubleshooter']]])) and *[System[TimeCreated[@SystemTime >= '{startTime.ToUniversalTime():o}']]] and *[System[TimeCreated[@SystemTime <= '{endTime.ToUniversalTime():o}']]]";

            var logReader = MyUtils.GetEventLogReader("System", query);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                var action = eventDetail.Id switch
                {
                    1 => "Awake",
                    12 => "Startup",
                    13 => "Shutdown",
                    42 => "Sleep",
                    6008 => "Unexpected Shutdown",
                    _ => null
                };

                yield return new PowerEventInfo
                {
                    DateUtc = (DateTime)eventDetail.TimeCreated?.ToUniversalTime(),
                    Description = action
                };
            }
        }
    }
}
