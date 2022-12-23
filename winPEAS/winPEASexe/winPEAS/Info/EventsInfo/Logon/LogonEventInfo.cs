using System;

namespace winPEAS.Info.EventsInfo.Logon
{
    internal class LogonEventInfo
    {
        public DateTime? CreatedAtUtc { get; set; }
        public string TargetUserName { get; set; }
        public string TargetDomainName { get; set; }
        public string LogonType { get; set; }
        public string IpAddress { get; set; }
        public string SubjectUserName { get; set; }
        public string SubjectDomainName { get; set; }
        public string AuthenticationPackage { get; set; }
        public string LmPackage { get; set; }
        public string TargetOutboundUserName { get; set; }
        public string TargetOutboundDomainName { get; set; }

        public LogonEventInfo(
            DateTime? createdAtUtc,
            string targetUserName,
            string targetDomainName,
            string logonType,
            string ipAddress,
            string subjectUserName,
            string subjectDomainName,
            string authenticationPackage,
            string lmPackage,
            string targetOutboundUserName,
            string targetOutboundDomainName)
        {
            CreatedAtUtc = createdAtUtc;
            TargetUserName = targetUserName;
            TargetDomainName = targetDomainName;
            LogonType = logonType;
            IpAddress = ipAddress;
            SubjectUserName = subjectUserName;
            SubjectDomainName = subjectDomainName;
            AuthenticationPackage = authenticationPackage;
            LmPackage = lmPackage;
            TargetOutboundUserName = targetOutboundUserName;
            TargetOutboundDomainName = targetOutboundDomainName;
        }
    }
}
