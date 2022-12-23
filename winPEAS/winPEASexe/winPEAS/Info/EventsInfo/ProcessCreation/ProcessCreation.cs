using System.Collections.Generic;
using winPEAS.Helpers;

namespace winPEAS.Info.EventsInfo.ProcessCreation
{
    internal class ProcessCreation
    {
        public static IEnumerable<ProcessCreationEventInfo> GetProcessCreationEventInfos()
        {
            // Get our "sensitive" cmdline regexes from a common helper function.
            var processCmdLineRegex = Common.GetInterestingProcessArgsRegex();

            var query = $"*[System/EventID=4688]";
            var logReader = MyUtils.GetEventLogReader("Security", query);

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                var user = eventDetail.Properties[1].Value.ToString().Trim();
                var commandLine = eventDetail.Properties[8].Value.ToString().Trim();

                foreach (var reg in processCmdLineRegex)
                {
                    var m = reg.Match(commandLine);
                    if (m.Success)
                    {
                        yield return new ProcessCreationEventInfo(
                            eventDetail.TimeCreated?.ToUniversalTime(),
                            eventDetail.Id,
                            user,
                            commandLine
                        );
                    }
                }
            }
        }
    }
}
