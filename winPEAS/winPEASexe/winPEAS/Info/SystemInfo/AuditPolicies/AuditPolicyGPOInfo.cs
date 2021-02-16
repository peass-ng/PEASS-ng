using System.Collections.Generic;

namespace winPEAS.Info.SystemInfo.AuditPolicies
{
    internal class AuditPolicyGPOInfo
    {
        public string Path { get; }
        public string Domain { get; }
        public string GPO { get; }
        public string Type { get; }
        public List<AuditEntryInfo> Settings { get; }

        public AuditPolicyGPOInfo(
            string path,
            string domain,
            string gpo,
            string type,
            List<AuditEntryInfo> settings)
        {
            Path = path;
            Domain = domain;
            GPO = gpo;
            Type = type;
            Settings = settings ?? new List<AuditEntryInfo>();
        }
    }
}
