using System.Collections.Generic;
using winPEAS.Helpers;

namespace winPEAS.Info.EventsInfo.PowerShell
{
    internal class PowerShell
    {
        public static IEnumerable<PowerShellEventInfo> GetPowerShellEventInfos()
        {
            // adapted from @djhohnstein's EventLogParser project
            //  https://github.com/djhohnstein/EventLogParser/blob/master/EventLogParser/EventLogHelpers.cs
            // combined with scraping from https://docs.microsoft.com/en-us/windows-server/administration/windows-commands/windows-commands            

            var context = 3; // number of lines around the match to display

            string[] powerShellLogs = { "Microsoft-Windows-PowerShell/Operational", "Windows PowerShell" };

            // Get our "sensitive" cmdline regexes from a common helper function.
            var powerShellRegex = Common.GetInterestingProcessArgsRegex();

            foreach (var logName in powerShellLogs)
            {
                var query = "*[System/EventID=4104]";

                var logReader = MyUtils.GetEventLogReader(logName, query);

                for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
                {
                    var scriptBlock = eventDetail.Properties[2].Value.ToString();

                    foreach (var reg in powerShellRegex)
                    {
                        var m = reg.Match(scriptBlock);
                        if (!m.Success)
                        {
                            continue;
                        }

                        var contextLines = new List<string>();

                        var scriptBlockParts = scriptBlock.Split('\n');
                        for (var i = 0; i < scriptBlockParts.Length; i++)
                        {
                            if (!scriptBlockParts[i].Contains(m.Value))
                            {
                                continue;
                            }

                            var printed = 0;
                            for (var j = 1; i - j > 0 && printed < context; j++)
                            {
                                if (scriptBlockParts[i - j].Trim() == "")
                                {
                                    continue;
                                }

                                contextLines.Add(scriptBlockParts[i - j].Trim());
                                printed++;
                            }
                            printed = 0;
                            contextLines.Add(m.Value.Trim());
                            for (var j = 1; printed < context && i + j < scriptBlockParts.Length; j++)
                            {
                                if (scriptBlockParts[i + j].Trim() == "")
                                {
                                    continue;
                                }

                                contextLines.Add(scriptBlockParts[i + j].Trim());
                                printed++;
                            }
                            break;
                        }

                        var contextJoined = string.Join("\n", contextLines.ToArray());

                        yield return new PowerShellEventInfo(
                            eventDetail.TimeCreated,
                            eventDetail.Id,
                            $"{eventDetail.UserId}",
                            m.Value,
                            contextJoined
                        );
                    }
                }
            }
        }
    }
}
