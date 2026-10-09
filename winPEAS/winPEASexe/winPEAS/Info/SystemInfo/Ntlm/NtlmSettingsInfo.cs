namespace winPEAS.Info.SystemInfo.Ntlm
{
    internal enum LdapPolicyReadState { Missing, Present, Error }
    internal enum LocalDomainRole { Unknown, Member, DomainController }
    internal enum DomainControllerGeneration { Unknown, Before2025, Server2025OrLater }
    internal enum LdapSigningStatus { Unknown, None, RequireSigning, LegacyDefault }
    internal enum LdapsChannelBindingStatus { Unknown, Never, WhenSupported, Always }

    internal sealed class DcLdapPolicyInfo
    {
        internal LocalDomainRole Role { get; set; }
        internal DomainControllerGeneration Generation { get; set; }
        internal LdapPolicyReadState SigningReadState { get; set; }
        internal uint? SigningValue { get; set; }
        internal LdapPolicyReadState ChannelBindingReadState { get; set; }
        internal uint? ChannelBindingValue { get; set; }

        internal LdapSigningStatus SigningStatus
        {
            get
            {
                if (Role != LocalDomainRole.DomainController) return LdapSigningStatus.Unknown;
                if (SigningReadState == LdapPolicyReadState.Missing && Generation == DomainControllerGeneration.Before2025)
                    return LdapSigningStatus.LegacyDefault;
                if (SigningReadState != LdapPolicyReadState.Present) return LdapSigningStatus.Unknown;
                if (SigningValue == 1) return LdapSigningStatus.None;
                if (SigningValue == 2) return LdapSigningStatus.RequireSigning;
                return LdapSigningStatus.Unknown;
            }
        }

        internal LdapsChannelBindingStatus ChannelBindingStatus
        {
            get
            {
                if (Role != LocalDomainRole.DomainController || ChannelBindingReadState != LdapPolicyReadState.Present)
                    return LdapsChannelBindingStatus.Unknown;
                if (ChannelBindingValue == 0) return LdapsChannelBindingStatus.Never;
                if (ChannelBindingValue == 1) return LdapsChannelBindingStatus.WhenSupported;
                if (ChannelBindingValue == 2) return LdapsChannelBindingStatus.Always;
                return LdapsChannelBindingStatus.Unknown;
            }
        }

        internal bool HasObservedRelayCondition =>
            SigningStatus == LdapSigningStatus.None ||
            SigningStatus == LdapSigningStatus.LegacyDefault ||
            ChannelBindingStatus == LdapsChannelBindingStatus.Never;
    }

    internal class NtlmSettingsInfo
    {
        public uint? LanmanCompatibilityLevel { get; set; }

        public string LanmanCompatibilityLevelString
        {
            get
            {
                switch (LanmanCompatibilityLevel)
                {
                    case 0: return "Send LM & NTLM responses";
                    case 1: return "Send LM & NTLM - Use NTLMv2 session security if negotiated";
                    case 2: return "Send NTLM response only";
                    case null:
                    case 3: return "Send NTLMv2 response only - Win7+ default";
                    case 4: return "Send NTLMv2 response only. DC: Refuse LM";
                    case 5: return "Send NTLMv2 response only. DC: Refuse LM & NTLM";
                    default: return "Unknown";
                }
            }
        }

        public bool ClientRequireSigning { get; set; }
        public bool ClientNegotiateSigning { get; set; }
        public bool ServerRequireSigning { get; set; }
        public bool ServerNegotiateSigning { get; set; }
        public uint? LdapSigning { get; set; }
        internal DcLdapPolicyInfo DcLdapPolicy { get; set; }

        public string LdapSigningString
        {
            get
            {
                switch (LdapSigning)
                {
                    case 0: return "No signing";
                    case 1: return "Negotiate signing";
                    case null: return "Unknown";
                    case 2: return "Require Signing";
                    default: return "Unknown";
                }
            }
        }

        public uint? NTLMMinClientSec { get; set; }
        public uint? NTLMMinServerSec { get; set; }
        public uint? InboundRestrictions { get; internal set; }

        public string InboundRestrictionsString
        {
            get
            {
                string inboundRestrictStr = InboundRestrictions switch
                {
                    0 => "Allow all",
                    1 => "Deny all domain accounts",
                    2 => "Deny all accounts",
                    _ => "Not defined",
                };

                return inboundRestrictStr;
            }
        }

        public uint? OutboundRestrictions { get; internal set; }

        public string OutboundRestrictionsString
        {
            get
            {
                string outboundRestrictStr = OutboundRestrictions switch
                {
                    0 => "Allow all",
                    1 => "Audit all",
                    2 => "Deny all",
                    _ => "Not defined",
                };

                return outboundRestrictStr;
            }
        }

        public uint? InboundAuditing { get; internal set; }

        public string InboundAuditingString
        {
            get
            {
                string inboundAuditStr = InboundAuditing switch
                {
                    0 => "Disable",
                    1 => "Enable auditing for domain accounts",
                    2 => "Enable auditing for all accounts",
                    _ => "Not defined",
                };
                return inboundAuditStr;
            }
        }

        public string OutboundExceptions { get; internal set; }

        //public string DCRestrictions { get; internal set; }
        //public string DCExceptions { get; internal set; }
        //public string DCAuditing { get; internal set; }
        //public string LdapChannelBinding { get; set; }
        //public string ExtendedProtectionForAuthentication { get; set; }
    }
}
