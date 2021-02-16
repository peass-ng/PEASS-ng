using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo.Ntlm
{
    internal class Ntlm
    {
        public static NtlmSettingsInfo GetNtlmSettingsInfo()
        {
            return new NtlmSettingsInfo
            {
                LanmanCompatibilityLevel = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Control\Lsa", "LmCompatibilityLevel"),

                ClientRequireSigning = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "RequireSecuritySignature") == 1,
                ClientNegotiateSigning = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Services\LanmanWorkstation\Parameters", "EnableSecuritySignature") == 1,
                ServerRequireSigning = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Services\LanManServer\Parameters", "RequireSecuritySignature") == 1,
                ServerNegotiateSigning = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Services\LanManServer\Parameters", "EnableSecuritySignature") == 1,


                LdapSigning = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Services\LDAP", "LDAPClientIntegrity"),

                NTLMMinClientSec = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0", "NtlmMinClientSec"),
                NTLMMinServerSec = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0", "NtlmMinServerSec"),


                InboundRestrictions = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "RestrictReceivingNTLMTraffic"), // Network security: Restrict NTLM: Incoming NTLM traffic
                OutboundRestrictions = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "RestrictSendingNTLMTraffic"),  // Network security: Restrict NTLM: Outgoing NTLM traffic to remote servers
                InboundAuditing = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "AuditReceivingNTLMTraffic"),        // Network security: Restrict NTLM: Audit Incoming NTLM Traffic
                OutboundExceptions = RegistryHelper.GetRegValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "ClientAllowedNTLMServers"),      // Network security: Restrict NTLM: Add remote server exceptions for NTLM authentication

                //DCRestrictions = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\Netlogon\Parameters", "RestrictNTLMInDomain"),    // Network security: Restrict NTLM:  NTLM authentication in this domain
                //DCExceptions = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\Netlogon\Parameters", "DCAllowedNTLMServers"),      // Network security: Restrict NTLM: Add server exceptions in this domain
                //DCAuditing = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\Netlogon\Parameters", "AuditNTLMInDomain"),           // Network security: Restrict NTLM: Audit NTLM authentication in this domain
                //DCLdapSigning = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\NTDS\Parameters", "LDAPServerIntegrity"),
                //LdapChannelBinding = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\NTDS\Parameters", "LdapEnforceChannelBinding"),
                //ExtendedProtectionForAuthentication = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Control\LSA", "SuppressExtendedProtection"),
            };
        }
    }
}
