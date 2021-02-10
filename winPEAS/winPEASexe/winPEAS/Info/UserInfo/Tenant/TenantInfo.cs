using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace winPEAS.Info.UserInfo.Tenant
{
    internal class TenantInfo
    {
        public JoinType JType { get; }
        public Guid DeviceId { get; }
        public string IdpDomain { get; }
        public Guid TenantId { get; }
        public string JoinUserEmail { get; }
        public string TenantDisplayName { get; }
        public string MdmEnrollmentUrl { get; }
        public string MdmTermsOfUseUrl { get; }
        public string MdmComplianceUrl { get; }
        public string UserSettingSyncUrl { get; }
        public List<X509Certificate2> CertInfo { get; }
        public string UserEmail { get; }
        public Guid? UserKeyId { get; }
        public string UserKeyname { get; }

        public TenantInfo(JoinType jType, Guid deviceId, string idpDomain, Guid tenantId, string joinUserEmail, string tenantDisplayName,
            string mdmEnrollmentUrl, string mdmTermsOfUseUrl, string mdmComplianceUrl, string userSettingSyncUrl,
            List<X509Certificate2> certInfo, string userEmail, Guid? userKeyId, string userKeyname)
        {
            JType = jType;
            DeviceId = deviceId;
            IdpDomain = idpDomain;
            TenantId = tenantId;
            JoinUserEmail = joinUserEmail;
            TenantDisplayName = tenantDisplayName;
            MdmEnrollmentUrl = mdmEnrollmentUrl;
            MdmTermsOfUseUrl = mdmTermsOfUseUrl;
            MdmComplianceUrl = mdmComplianceUrl;
            UserSettingSyncUrl = userSettingSyncUrl;
            CertInfo = certInfo;
            UserEmail = userEmail;
            UserKeyId = userKeyId;
            UserKeyname = userKeyname;
        }
    }
}
