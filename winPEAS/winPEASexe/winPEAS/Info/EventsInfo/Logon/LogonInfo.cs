using System.Collections.Generic;

namespace winPEAS.Info.EventsInfo.Logon
{
    internal class LogonInfo
    {
        public HashSet<string> NTLMv1LoggedUsersSet { get; set; } = new HashSet<string>();
        public HashSet<string> NTLMv2LoggedUsersSet { get; set; } = new HashSet<string>();
        public HashSet<string> KerberosLoggedUsersSet { get; set; } = new HashSet<string>();

        public IEnumerable<LogonEventInfo> LogonEventInfos { get; set; } = new List<LogonEventInfo>();
    }
}
