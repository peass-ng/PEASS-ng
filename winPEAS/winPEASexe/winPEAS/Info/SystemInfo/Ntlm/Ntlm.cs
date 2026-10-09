using System;
using System.Management;
using Microsoft.Win32;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo.Ntlm
{
    internal class Ntlm
    {
        private const string DcPolicyKey = @"SYSTEM\CurrentControlSet\Services\NTDS\Parameters";
        private static readonly Lazy<DcLdapPolicyInfo> CachedDcLdapPolicy =
            new Lazy<DcLdapPolicyInfo>(ReadLocalDcLdapPolicy);

        internal static DcLdapPolicyInfo GetLocalDcLdapPolicy() => CachedDcLdapPolicy.Value;

        private static DcLdapPolicyInfo ReadLocalDcLdapPolicy()
        {
            var info = new DcLdapPolicyInfo
            {
                Role = LocalDomainRole.Unknown,
                Generation = DomainControllerGeneration.Unknown,
                SigningReadState = LdapPolicyReadState.Error,
                ChannelBindingReadState = LdapPolicyReadState.Error
            };

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT DomainRole FROM Win32_ComputerSystem"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject computer in results)
                    {
                        if (computer["DomainRole"] == null) break;
                        var role = Convert.ToInt32(computer["DomainRole"]);
                        if (role >= 0 && role <= 5)
                            info.Role = role >= 4 ? LocalDomainRole.DomainController : LocalDomainRole.Member;
                        break;
                    }
                }
            }
            catch { return info; }

            if (info.Role != LocalDomainRole.DomainController) return info;

            try
            {
                using (var version = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (version != null && int.TryParse(Convert.ToString(version.GetValue("CurrentBuild")), out int build) && build >= 7600)
                        info.Generation = build >= 26100 ? DomainControllerGeneration.Server2025OrLater : DomainControllerGeneration.Before2025;
                }
            }
            catch { }

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(DcPolicyKey))
                {
                    if (key == null)
                    {
                        info.SigningReadState = LdapPolicyReadState.Missing;
                        info.ChannelBindingReadState = LdapPolicyReadState.Missing;
                        return info;
                    }
                    ReadDword(key, "LDAPServerIntegrity", out var signingState, out var signingValue);
                    info.SigningReadState = signingState;
                    info.SigningValue = signingValue;
                    ReadDword(key, "LdapEnforceChannelBinding", out var bindingState, out var bindingValue);
                    info.ChannelBindingReadState = bindingState;
                    info.ChannelBindingValue = bindingValue;
                }
            }
            catch { }
            return info;
        }

        private static void ReadDword(RegistryKey key, string name, out LdapPolicyReadState state, out uint? value)
        {
            value = null;
            state = LdapPolicyReadState.Error;
            try
            {
                if (!Array.Exists(key.GetValueNames(), item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase)))
                {
                    state = LdapPolicyReadState.Missing;
                    return;
                }
                if (key.GetValueKind(name) != RegistryValueKind.DWord) return;
                var raw = key.GetValue(name);
                value = raw is int signed ? unchecked((uint)signed) : Convert.ToUInt32(raw);
                state = LdapPolicyReadState.Present;
            }
            catch { }
        }

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
                DcLdapPolicy = GetLocalDcLdapPolicy(),

                NTLMMinClientSec = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0", "NtlmMinClientSec"),
                NTLMMinServerSec = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Control\Lsa\MSV1_0", "NtlmMinServerSec"),


                InboundRestrictions = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "RestrictReceivingNTLMTraffic"), // Network security: Restrict NTLM: Incoming NTLM traffic
                OutboundRestrictions = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "RestrictSendingNTLMTraffic"),  // Network security: Restrict NTLM: Outgoing NTLM traffic to remote servers
                InboundAuditing = RegistryHelper.GetDwordValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "AuditReceivingNTLMTraffic"),        // Network security: Restrict NTLM: Audit Incoming NTLM Traffic
                OutboundExceptions = RegistryHelper.GetRegValue("HKLM", @"System\CurrentControlSet\Control\Lsa\MSV1_0", "ClientAllowedNTLMServers"),      // Network security: Restrict NTLM: Add remote server exceptions for NTLM authentication

                //DCRestrictions = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\Netlogon\Parameters", "RestrictNTLMInDomain"),    // Network security: Restrict NTLM:  NTLM authentication in this domain
                //DCExceptions = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\Netlogon\Parameters", "DCAllowedNTLMServers"),      // Network security: Restrict NTLM: Add server exceptions in this domain
                //DCAuditing = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Services\Netlogon\Parameters", "AuditNTLMInDomain"),           // Network security: Restrict NTLM: Audit NTLM authentication in this domain
                //ExtendedProtectionForAuthentication = RegistryUtil.GetValue("HKLM", @"System\CurrentControlSet\Control\LSA", "SuppressExtendedProtection"),
            };
        }
    }
}
