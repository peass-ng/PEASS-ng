using System;

namespace winPEAS.Info.UserInfo.LogonSessions
{
    internal class LogonSessionsInfo
    {
        public string Method { get; }
        public string UserName { get; }
        public string Domain { get; }
        public string LogonId { get; }
        public string LogonType { get; }
        public string AuthenticationPackage { get; }
        public DateTime? StartTime { get; }
        public DateTime? LogonTime { get; }
        public string LogonServer { get; }
        public string LogonServerDnsDomain { get; }
        public string UserPrincipalName { get; }
        public string UserSID { get; }

        public LogonSessionsInfo(
            string method,
            string userName,
            string domain,
            string logonId,
            string logonType,
            string authenticationPackage,
            DateTime? startTime,
            DateTime? logonTime,
            string logonServer,
            string logonServerDnsDomain,
            string userPrincipalName,
            string userSid)
        {
            Method = method;
            UserName = userName;
            Domain = domain;
            LogonId = logonId;
            LogonType = logonType;
            AuthenticationPackage = authenticationPackage;
            StartTime = startTime;
            LogonTime = logonTime;
            LogonServer = logonServer;
            LogonServerDnsDomain = logonServerDnsDomain;
            UserPrincipalName = userPrincipalName;
            UserSID = userSid;
        }
    }
}
