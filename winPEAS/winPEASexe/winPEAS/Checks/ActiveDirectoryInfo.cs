using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Reflection;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.Info.ActiveDirectoryInfo;

namespace winPEAS.Checks
{
    // Lightweight AD checks for access candidates and certificate template configuration.
    internal class ActiveDirectoryInfo : ISystemCheck
    {
        internal enum MachineAccountQuotaStatus { Unavailable, Zero, Positive }

        internal static MachineAccountQuotaStatus AssessMachineAccountQuota(int? quota)
        {
            if (!quota.HasValue || quota.Value < 0) return MachineAccountQuotaStatus.Unavailable;
            return quota.Value == 0 ? MachineAccountQuotaStatus.Zero : MachineAccountQuotaStatus.Positive;
        }

        internal enum StagedComputerStatus { Unknown, Excluded, Candidate }

        internal static StagedComputerStatus AssessStagedComputer(int? userAccountControl)
        {
            if (!userAccountControl.HasValue || userAccountControl.Value < 0)
                return StagedComputerStatus.Unknown;
            const int disabled = 0x2;
            const int passwordNotRequired = 0x20;
            const int workstationTrust = 0x1000;
            const int serverTrust = 0x2000;
            int flags = userAccountControl.Value;
            if ((flags & disabled) != 0 || (flags & serverTrust) != 0)
                return StagedComputerStatus.Excluded;
            return (flags & (passwordNotRequired | workstationTrust)) == (passwordNotRequired | workstationTrust)
                ? StagedComputerStatus.Candidate
                : StagedComputerStatus.Excluded;
        }

        internal enum Esc11RegistryStatus
        {
            Unknown,
            Candidate,
            Protected
        }

        internal enum GmsaAccessStatus { Unknown, NoMatch, Denied, Candidate }
        internal enum GmsaReaderListWriteStatus { Unknown, NoMatch, Denied, Candidate }
        internal sealed class GmsaReaderListWriteAssessment
        {
            internal GmsaReaderListWriteStatus Status { get; set; }
            internal List<string> MatchingWriterSids { get; } = new List<string>();
            internal List<string> MatchingDenySids { get; } = new List<string>();
            internal bool BroadWrite { get; set; }
            internal bool InheritedAce { get; set; }
        }
        internal sealed class GmsaMembershipAssessment
        {
            internal GmsaAccessStatus Status { get; set; }
            internal List<string> ReaderSids { get; } = new List<string>();
            internal List<string> MatchingReaderSids { get; } = new List<string>();
            internal List<string> MatchingDenySids { get; } = new List<string>();
        }
        internal enum Esc16RegistryStatus { Unknown, Present, Absent }
        internal enum Esc6RegistryStatus { Unknown, Candidate, Absent }
        internal enum SchannelUpnMappingStatus { Unknown, Enabled, Disabled }
        internal enum SpnWriteRight { None, WriteProperty, ValidatedSelf }
        internal enum MembershipWriteRight { None, OwnMembership, MemberAttribute }
        internal enum ExactAttributeWriteRight { None, Upn, KeyCredentialLink, AltSecurityIdentities, GmsaReaderList, ScriptPath }
        private static readonly Guid SelfMembershipGuid = new Guid("bf9679c0-0de6-11d0-a285-00aa003049e2");
        private static readonly Guid ValidatedSpnGuid = new Guid("f3a64788-5306-11d1-a9c5-0000f80367c1");
        private static readonly Guid OrganizationalUnitClassGuid = new Guid("bf967aa5-0de6-11d0-a285-00aa003049e2");
        private static readonly Guid UserPrincipalNameGuid = new Guid("28630ebb-41d5-11d1-a9c1-0000f80367c1");
        private static readonly Guid KeyCredentialLinkGuid = new Guid("5b47d60f-6090-40b2-9f37-2a4de88f3063");
        private static readonly Guid AltSecurityIdentitiesGuid = new Guid("00fbf30c-91fe-11d1-aebc-0000f80367c1");
        private static readonly Guid GmsaReaderListGuid = new Guid("888eedd6-ce04-df40-b462-b8a50e41ba38");
        private static readonly Guid ScriptPathGuid = new Guid("bf9679a8-0de6-11d0-a285-00aa003049e2");
        private static readonly Guid CertificateEnrollGuid = new Guid("0e10c968-78fb-11d2-90d4-00c04f79dc55");

        internal enum Esc1TemplateStatus { Unknown, NotCandidate, Candidate }
        internal enum Esc9TemplateStatus { Unknown, NotCandidate, Candidate }
        internal enum Esc13TemplateStatus { Unknown, NotCandidate, Candidate }

        private sealed class Esc13TemplateObservation
        {
            internal string Name;
            internal string[] PolicyOids;
            internal bool PolicyOverflow;
            internal int? EnrollmentFlags;
            internal int? RequiredSignatures;
            internal string[] ExtendedKeyUsages;
            internal bool DescriptorRead;
            internal bool CurrentEnrollAllow;
            internal bool CurrentEnrollDeny;
            internal bool ComputerEnrollAllow;
            internal bool ComputerEnrollDeny;
        }

        // Configuration only: publication, CA rights, effective ACLs and KDC mapping remain unverified.
        internal static Esc1TemplateStatus AssessEsc1Template(int? nameFlags, int? enrollmentFlags,
            int? requiredSignatures, IEnumerable<string> extendedKeyUsages)
        {
            if (!nameFlags.HasValue || !enrollmentFlags.HasValue || !requiredSignatures.HasValue ||
                extendedKeyUsages == null)
                return Esc1TemplateStatus.Unknown;
            if ((nameFlags.Value & 0x10001) == 0 || (enrollmentFlags.Value & 0x2) != 0 ||
                requiredSignatures.Value != 0)
                return Esc1TemplateStatus.NotCandidate;
            var eku = new HashSet<string>(extendedKeyUsages, StringComparer.Ordinal);
            return eku.Contains("1.3.6.1.5.5.7.3.2") || // Client Authentication
                   eku.Contains("1.3.6.1.5.2.3.4") || // PKINIT Client Authentication
                   eku.Contains("1.3.6.1.4.1.311.20.2.2") // Smart Card Logon
                ? Esc1TemplateStatus.Candidate : Esc1TemplateStatus.NotCandidate;
        }

        // Configuration only: publication, enrollment, writable account UPN and effective
        // certificate mapping on the authentication endpoint are separate prerequisites.
        internal static Esc9TemplateStatus AssessEsc9Template(int? enrollmentFlags,
            int? requiredSignatures, IEnumerable<string> extendedKeyUsages)
        {
            if (!enrollmentFlags.HasValue) return Esc9TemplateStatus.Unknown;
            if ((enrollmentFlags.Value & 0x80000) == 0) return Esc9TemplateStatus.NotCandidate;
            if (!requiredSignatures.HasValue || extendedKeyUsages == null)
                return Esc9TemplateStatus.Unknown;
            if ((enrollmentFlags.Value & 0x2) != 0 || requiredSignatures.Value != 0)
                return Esc9TemplateStatus.NotCandidate;
            var eku = new HashSet<string>(extendedKeyUsages, StringComparer.Ordinal);
            if (eku.Count == 0) return Esc9TemplateStatus.Unknown; // No EKU may mean Any Purpose.
            return eku.Contains("1.3.6.1.5.5.7.3.2") || // Client Authentication
                   eku.Contains("1.3.6.1.5.2.3.4") || // PKINIT Client Authentication
                   eku.Contains("1.3.6.1.4.1.311.20.2.2") || // Smart Card Logon
                   eku.Contains("2.5.29.37.0") // Any Purpose
                ? Esc9TemplateStatus.Candidate : Esc9TemplateStatus.NotCandidate;
        }

        // A linked issuance-policy OID is a configuration lead, not proof that a CA
        // publishes the template or that the principal can obtain and use a certificate.
        internal static Esc13TemplateStatus AssessEsc13Template(IEnumerable<string> policyOids,
            IDictionary<string, string> linkedGroups, bool oidLookupComplete, int? enrollmentFlags,
            int? requiredSignatures, IEnumerable<string> extendedKeyUsages, out string linkedGroup)
        {
            linkedGroup = null;
            if (policyOids == null) return Esc13TemplateStatus.Unknown;
            var policies = policyOids.Where(oid => !string.IsNullOrWhiteSpace(oid)).ToArray();
            if (policies.Length == 0) return Esc13TemplateStatus.NotCandidate;
            if (!oidLookupComplete || linkedGroups == null) return Esc13TemplateStatus.Unknown;

            bool conflictingLink = false;
            foreach (string policy in policies)
            {
                string group;
                if (!linkedGroups.TryGetValue(policy, out group)) continue;
                // Empty values represent duplicate/conflicting OID-to-group records.
                if (string.IsNullOrEmpty(group))
                {
                    conflictingLink = true;
                    continue;
                }
                linkedGroup = group;
                break;
            }
            if (linkedGroup == null)
                return conflictingLink
                    ? Esc13TemplateStatus.Unknown : Esc13TemplateStatus.NotCandidate;
            if (!enrollmentFlags.HasValue || !requiredSignatures.HasValue || extendedKeyUsages == null)
                return Esc13TemplateStatus.Unknown;
            if ((enrollmentFlags.Value & 0x2) != 0 || requiredSignatures.Value != 0)
                return Esc13TemplateStatus.NotCandidate;
            var eku = new HashSet<string>(extendedKeyUsages, StringComparer.Ordinal);
            if (eku.Count == 0) return Esc13TemplateStatus.Unknown;
            return eku.Contains("1.3.6.1.5.5.7.3.2") || // Client Authentication
                   eku.Contains("1.3.6.1.5.2.3.4") || // PKINIT Client Authentication
                   eku.Contains("1.3.6.1.4.1.311.20.2.2") || // Smart Card Logon
                   eku.Contains("2.5.29.37.0") // Any Purpose
                ? Esc13TemplateStatus.Candidate : Esc13TemplateStatus.NotCandidate;
        }

        private static Dictionary<string, string> ReadOidGroupLinks(string configurationNamingContext,
            out bool complete)
        {
            var links = new Dictionary<string, string>(StringComparer.Ordinal);
            complete = false;
            try
            {
                var oidDn = "LDAP://CN=OID,CN=Public Key Services,CN=Services," + configurationNamingContext;
                using (var root = new DirectoryEntry(oidDn))
                using (var search = new DirectorySearcher(root))
                {
                    search.SearchScope = SearchScope.OneLevel;
                    search.Filter = "(&(objectClass=msPKI-Enterprise-Oid)(msDS-OIDToGroupLink=*))";
                    search.PropertiesToLoad.Add("msPKI-Cert-Template-OID");
                    search.PropertiesToLoad.Add("msDS-OIDToGroupLink");
                    search.PageSize = 0; // Keep the server-side size cap effective.
                    search.SizeLimit = 121;
                    search.ReferralChasing = ReferralChasingOption.None;
                    search.CacheResults = false;
                    search.ServerTimeLimit = TimeSpan.FromSeconds(5);
                    search.ClientTimeout = TimeSpan.FromSeconds(5);
                    using (var results = search.FindAll())
                    {
                        int seen = 0;
                        foreach (SearchResult result in results)
                        {
                            if (++seen > 120) return links; // Incomplete: unmatched OIDs remain unknown.
                            var oid = GetProp(result, "msPKI-Cert-Template-OID")?.Trim();
                            var group = GetProp(result, "msDS-OIDToGroupLink")?.Trim();
                            if (string.IsNullOrEmpty(oid) || string.IsNullOrEmpty(group)) continue;
                            string oldGroup;
                            if (links.TryGetValue(oid, out oldGroup))
                            {
                                if (!string.Equals(oldGroup, group, StringComparison.OrdinalIgnoreCase))
                                    links[oid] = string.Empty; // Conflicting LDAP records are not a candidate.
                            }
                            else links.Add(oid, group);
                        }
                    }
                }
                complete = true;
            }
            catch (Exception)
            {
                // LDAP visibility and availability vary by domain; incomplete means unknown.
            }
            return links;
        }

        internal static bool IsCertificateEnrollAce(ActiveDirectoryRights rights, Guid objectType)
        {
            return ((rights & ActiveDirectoryRights.GenericAll) != 0 && objectType == Guid.Empty) ||
                   ((rights & ActiveDirectoryRights.ExtendedRight) != 0 &&
                    (objectType == CertificateEnrollGuid || objectType == Guid.Empty));
        }

        internal static string DescribeEsc1EnrollAceEvidence(bool descriptorRead, bool currentAllow,
            bool currentDeny, bool computerAllow, bool computerDeny, bool computerSidKnown)
        {
            if (!descriptorRead) return "enrollment ACL unavailable";
            var evidence = new List<string>();
            if (currentDeny) evidence.Add("current-token matching deny ACE");
            else if (currentAllow) evidence.Add("current-token Enroll allow ACE");
            if (!computerSidKnown) evidence.Add("Domain Computers SID unavailable from token");
            else if (computerDeny) evidence.Add("Domain Computers matching deny ACE");
            else if (computerAllow) evidence.Add("Domain Computers Enroll allow ACE");
            if (evidence.Count == 0) evidence.Add("matching Enroll allow ACE not found; indirect rights unknown");
            return string.Join("; ", evidence);
        }

        internal static string DescribeTemplateMinimumKeySize(int? minimumKey)
        {
            return minimumKey.HasValue && minimumKey.Value > 0
                ? "minimum key size " + minimumKey.Value : "minimum key size unknown";
        }

        internal static string DomainComputersSidFromToken(IEnumerable<string> tokenSids)
        {
            if (tokenSids == null) return null;
            foreach (string sid in tokenSids)
            {
                if (string.IsNullOrEmpty(sid) || !sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (sid.EndsWith("-515", StringComparison.Ordinal) || sid.EndsWith("-513", StringComparison.Ordinal))
                    return sid.Substring(0, sid.LastIndexOf('-') + 1) + "515";
            }
            return null;
        }

        internal static bool IsAclCandidateAce(AccessControlType accessType, bool sidMatches, bool inheritOnly,
            bool isInherited, Guid inheritedObjectType, string targetClass)
        {
            if (accessType != AccessControlType.Allow || !sidMatches || inheritOnly)
                return false;
            if (isInherited && string.Equals(targetClass, "organizationalUnit", StringComparison.OrdinalIgnoreCase) &&
                inheritedObjectType != Guid.Empty && inheritedObjectType != OrganizationalUnitClassGuid)
                return false;
            return true;
        }

        internal static MembershipWriteRight ClassifyMembershipWrite(Guid objectType, bool validatedWrite, string targetClass)
        {
            if (!string.Equals(targetClass, "group", StringComparison.OrdinalIgnoreCase))
                return MembershipWriteRight.None;
            if (validatedWrite)
                return objectType == SelfMembershipGuid ? MembershipWriteRight.OwnMembership : MembershipWriteRight.None;
            return objectType == SelfMembershipGuid ? MembershipWriteRight.MemberAttribute : MembershipWriteRight.None;
        }

        internal static string DescribeMembershipCandidate(MembershipWriteRight right)
        {
            if (right == MembershipWriteRight.OwnMembership)
                return "Candidate to add or remove only the current account from this group; effective access requires review.";
            if (right == MembershipWriteRight.MemberAttribute)
                return "Candidate to edit the exact member attribute and manage group membership; effective access requires review, and new membership requires a refreshed token/session.";
            return null;
        }

        internal static ExactAttributeWriteRight ClassifyExactAttributeWrite(Guid objectType, bool validatedWrite, string targetClass)
        {
            if (validatedWrite) return ExactAttributeWriteRight.None;
            if (objectType == UserPrincipalNameGuid && string.Equals(targetClass, "user", StringComparison.OrdinalIgnoreCase))
                return ExactAttributeWriteRight.Upn;
            if (objectType == KeyCredentialLinkGuid &&
                (string.Equals(targetClass, "user", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(targetClass, "computer", StringComparison.OrdinalIgnoreCase)))
                return ExactAttributeWriteRight.KeyCredentialLink;
            if (objectType == AltSecurityIdentitiesGuid &&
                (string.Equals(targetClass, "user", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(targetClass, "computer", StringComparison.OrdinalIgnoreCase)))
                return ExactAttributeWriteRight.AltSecurityIdentities;
            if (objectType == GmsaReaderListGuid &&
                string.Equals(targetClass, "msDS-GroupManagedServiceAccount", StringComparison.OrdinalIgnoreCase))
                return ExactAttributeWriteRight.GmsaReaderList;
            if (objectType == ScriptPathGuid && string.Equals(targetClass, "user", StringComparison.OrdinalIgnoreCase))
                return ExactAttributeWriteRight.ScriptPath;
            return ExactAttributeWriteRight.None;
        }

        internal sealed class BoundedSample<T>
        {
            internal List<T> Items { get; } = new List<T>();
            internal bool Truncated { get; set; }
        }

        internal static BoundedSample<T> SelectBoundedSample<T>(IEnumerable<T> source, int limit)
        {
            var sample = new BoundedSample<T>();
            if (source == null || limit < 1) return sample;
            foreach (var item in source)
            {
                if (sample.Items.Count == limit)
                {
                    sample.Truncated = true;
                    break;
                }
                sample.Items.Add(item);
            }
            return sample;
        }

        // Only inspect notes already returned by the bounded AD object sample. Never return note text.
        internal static string ClassifyAdCredentialNote(IEnumerable<string> objectClasses, string description, string info)
        {
            if (objectClasses == null) return null;
            bool isUser = false;
            foreach (var objectClass in objectClasses)
            {
                if (string.Equals(objectClass, "computer", StringComparison.OrdinalIgnoreCase)) return null;
                if (string.Equals(objectClass, "user", StringComparison.OrdinalIgnoreCase)) isUser = true;
            }
            if (!isUser) return null;
            if (HasCredentialAssignmentCue(description)) return "description password-like assignment (value redacted)";
            if (HasCredentialAssignmentCue(info)) return "info password-like assignment (value redacted)";
            return null;
        }

        private static bool HasCredentialAssignmentCue(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return false;
            // Bound both work and memory even if a directory contains unusually large notes.
            string text = note.Substring(0, Math.Min(note.Length, 512));
            foreach (var keyword in new[] { "password", "passwd", "passphrase", "pwd", "credential", "secret" })
            {
                int start = 0;
                while (start < text.Length)
                {
                    int at = text.IndexOf(keyword, start, StringComparison.OrdinalIgnoreCase);
                    if (at < 0) break;
                    start = at + keyword.Length;
                    if ((at > 0 && IsAdNoteWordChar(text[at - 1])) ||
                        (start < text.Length && IsAdNoteWordChar(text[start]))) continue;
                    int next = start;
                    while (next < text.Length && char.IsWhiteSpace(text[next])) next++;
                    if (next < text.Length && (text[next] == ':' || text[next] == '=')) next++;
                    else if (next + 2 < text.Length &&
                             string.Compare(text, next, "is ", 0, 3, StringComparison.OrdinalIgnoreCase) == 0) next += 3;
                    else continue;
                    while (next < text.Length && char.IsWhiteSpace(text[next])) next++;
                    int end = next;
                    while (end < text.Length && end - next < 96 && !char.IsWhiteSpace(text[end]) &&
                           text[end] != ',' && text[end] != ';') end++;
                    if (end - next < 4) continue;
                    string firstWord = text.Substring(next, end - next).TrimEnd('.').ToLowerInvariant();
                    if (new[] { "required", "expired", "changed", "change", "reset", "unknown", "stored",
                                "never", "unset", "blank", "empty", "none" }.Contains(firstWord)) continue;
                    return true;
                }
            }
            return false;
        }

        private static bool IsAdNoteWordChar(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private const int TrustedForDelegation = 0x80000;
        private const int ServerTrustAccount = 0x2000;

        internal sealed class DelegationComputer
        {
            internal string Name { get; set; }
            internal int? UserAccountControl { get; set; }
        }

        internal sealed class DelegationInventorySummary
        {
            internal List<string> DisplayNames { get; } = new List<string>();
            internal int Inspected { get; set; }
            internal int Candidates { get; set; }
            internal int UnknownFlags { get; set; }
            internal bool Truncated { get; set; }
            internal bool LdapError { get; set; }
        }

        internal static bool IsNonDcUnconstrainedDelegation(int? userAccountControl)
        {
            return userAccountControl.HasValue &&
                (userAccountControl.Value & TrustedForDelegation) != 0 &&
                (userAccountControl.Value & ServerTrustAccount) == 0;
        }

        internal static DelegationInventorySummary SummarizeDelegationInventory(
            IEnumerable<DelegationComputer> rows, bool ldapError)
        {
            var summary = new DelegationInventorySummary { LdapError = ldapError };
            var sample = SelectBoundedSample(rows, DelegationSampleLimit);
            summary.Truncated = sample.Truncated;
            foreach (var row in sample.Items)
            {
                summary.Inspected++;
                if (row == null || !row.UserAccountControl.HasValue)
                {
                    summary.UnknownFlags++;
                    continue;
                }
                if (!IsNonDcUnconstrainedDelegation(row.UserAccountControl)) continue;
                summary.Candidates++;
                if (summary.DisplayNames.Count < MaxFindingsToPrint)
                    summary.DisplayNames.Add(string.IsNullOrWhiteSpace(row.Name) ? "<unknown>" : row.Name);
            }
            return summary;
        }

        internal static bool ShouldInspectAclTarget(string dn, IEnumerable<string> inspectedDns)
        {
            return !string.IsNullOrWhiteSpace(dn) &&
                (inspectedDns == null || !inspectedDns.Any(inspected =>
                    string.Equals(inspected, dn, StringComparison.OrdinalIgnoreCase)));
        }

        internal sealed class OuSample
        {
            internal List<string> DistinguishedNames { get; } = new List<string>();
            internal bool Truncated { get; set; }
        }

        internal static OuSample SelectOuSample(IEnumerable<string> distinguishedNames, int limit)
        {
            var sample = new OuSample();
            if (distinguishedNames == null || limit < 1) return sample;
            foreach (var dn in distinguishedNames.Where(dn => !string.IsNullOrWhiteSpace(dn)))
            {
                if (sample.DistinguishedNames.Count == limit)
                {
                    sample.Truncated = true;
                    break;
                }
                sample.DistinguishedNames.Add(dn);
            }
            return sample;
        }

        internal static SchannelUpnMappingStatus AssessSchannelUpnMapping(uint? mappingMethods)
        {
            return !mappingMethods.HasValue ? SchannelUpnMappingStatus.Unknown
                : (mappingMethods.Value & 0x4) != 0 ? SchannelUpnMappingStatus.Enabled
                : SchannelUpnMappingStatus.Disabled;
        }

        internal static SpnWriteRight ClassifySpnWrite(string rightName, bool validatedWrite)
        {
            if (validatedWrite)
            {
                return string.Equals(rightName, "Validated-SPN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rightName, "Validated write to service principal name", StringComparison.OrdinalIgnoreCase)
                    ? SpnWriteRight.ValidatedSelf : SpnWriteRight.None;
            }

            return string.Equals(rightName, "servicePrincipalName", StringComparison.OrdinalIgnoreCase)
                ? SpnWriteRight.WriteProperty : SpnWriteRight.None;
        }

        internal static GmsaAccessStatus AssessGmsaMembership(byte[] descriptorBytes, ISet<string> currentSids)
        {
            return InspectGmsaMembership(descriptorBytes, currentSids).Status;
        }

        internal static GmsaMembershipAssessment InspectGmsaMembership(byte[] descriptorBytes, ISet<string> currentSids)
        {
            var result = new GmsaMembershipAssessment { Status = GmsaAccessStatus.Unknown };
            if (descriptorBytes == null || descriptorBytes.Length == 0 || descriptorBytes.Length > 16384)
                return result;

            try
            {
                var descriptor = new RawSecurityDescriptor(descriptorBytes, 0);
                if ((descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) == 0 || descriptor.DiscretionaryAcl == null)
                    return result;

                bool uncertain = false;
                foreach (GenericAce ace in descriptor.DiscretionaryAcl)
                {
                    var qualified = ace as QualifiedAce;
                    if (qualified == null || (ace.AceFlags & AceFlags.InheritOnly) != 0)
                        continue;

                    const int readProperty = 0x10;
                    const int genericRead = unchecked((int)0x80000000);
                    const int genericAll = 0x10000000;
                    if ((qualified.AccessMask & (readProperty | genericRead | genericAll)) == 0)
                        continue;

                    var sid = qualified.SecurityIdentifier.Value;
                    bool matchesToken = currentSids != null && currentSids.Contains(sid);
                    bool simpleAce = qualified is CommonAce && !((CommonAce)qualified).IsCallback;
                    if (qualified.AceQualifier == AceQualifier.AccessAllowed && simpleAce)
                    {
                        if (!result.ReaderSids.Contains(sid)) result.ReaderSids.Add(sid);
                        if (matchesToken && !result.MatchingReaderSids.Contains(sid))
                            result.MatchingReaderSids.Add(sid);
                    }

                    if (!matchesToken) continue;
                    if (qualified.AceQualifier == AceQualifier.AccessDenied)
                    {
                        if (simpleAce && !result.MatchingDenySids.Contains(sid))
                            result.MatchingDenySids.Add(sid);
                        else if (!simpleAce)
                            uncertain = true;
                    }
                    else if (qualified.AceQualifier != AceQualifier.AccessAllowed || !simpleAce)
                        uncertain = true;
                }
                result.Status = currentSids == null || currentSids.Count == 0 || uncertain
                    ? GmsaAccessStatus.Unknown
                    : result.MatchingDenySids.Count > 0 ? GmsaAccessStatus.Denied
                    : result.MatchingReaderSids.Count > 0 ? GmsaAccessStatus.Candidate
                    : GmsaAccessStatus.NoMatch;
            }
            catch (Exception)
            {
                result.Status = GmsaAccessStatus.Unknown;
                result.ReaderSids.Clear();
                result.MatchingReaderSids.Clear();
                result.MatchingDenySids.Clear();
            }
            return result;
        }

        // Interpret the *object* DACL, not the separate reader-list descriptor. A matching
        // ACE is only an indicator: effective access and possible deny/property-set rules
        // are not fully evaluated here.
        internal static GmsaReaderListWriteAssessment InspectGmsaReaderListWrite(byte[] descriptorBytes,
            ISet<string> currentSids)
        {
            var result = new GmsaReaderListWriteAssessment { Status = GmsaReaderListWriteStatus.Unknown };
            if (descriptorBytes == null || descriptorBytes.Length == 0 || descriptorBytes.Length > 65536 ||
                currentSids == null || currentSids.Count == 0)
                return result;

            try
            {
                var descriptor = new RawSecurityDescriptor(descriptorBytes, 0);
                if ((descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) == 0 ||
                    descriptor.DiscretionaryAcl == null)
                    return result;

                bool complexAce = false;
                foreach (GenericAce ace in descriptor.DiscretionaryAcl)
                {
                    var qualified = ace as QualifiedAce;
                    if (qualified == null || (ace.AceFlags & AceFlags.InheritOnly) != 0 ||
                        !currentSids.Contains(qualified.SecurityIdentifier.Value))
                        continue;

                    const int writeProperty = 0x20;
                    const int genericWrite = 0x40000000;
                    const int genericAll = 0x10000000;
                    if ((qualified.AccessMask & (writeProperty | genericWrite | genericAll)) == 0)
                        continue;

                    var objectAce = qualified as ObjectAce;
                    bool scoped = objectAce != null &&
                        (objectAce.ObjectAceFlags & ObjectAceFlags.ObjectAceTypePresent) != 0;
                    if (scoped && ClassifyExactAttributeWrite(objectAce.ObjectAceType, false,
                        "msDS-GroupManagedServiceAccount") != ExactAttributeWriteRight.GmsaReaderList)
                        continue;

                    // Callback/conditional ACEs and inherited-object scopes need a complete
                    // access check; never promote them to a positive candidate.
                    if (qualified.IsCallback || (objectAce != null &&
                        (objectAce.ObjectAceFlags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0))
                    {
                        complexAce = true;
                        continue;
                    }

                    string sid = qualified.SecurityIdentifier.Value;
                    if (qualified.AceQualifier == AceQualifier.AccessDenied)
                    {
                        if (!result.MatchingDenySids.Contains(sid)) result.MatchingDenySids.Add(sid);
                    }
                    else if (qualified.AceQualifier == AceQualifier.AccessAllowed)
                    {
                        if (!result.MatchingWriterSids.Contains(sid)) result.MatchingWriterSids.Add(sid);
                        if (!scoped) result.BroadWrite = true;
                        if ((ace.AceFlags & AceFlags.Inherited) != 0) result.InheritedAce = true;
                    }
                    else
                        complexAce = true;
                }

                result.Status = complexAce ? GmsaReaderListWriteStatus.Unknown
                    : result.MatchingDenySids.Count > 0 ? GmsaReaderListWriteStatus.Denied
                    : result.MatchingWriterSids.Count > 0 ? GmsaReaderListWriteStatus.Candidate
                    : GmsaReaderListWriteStatus.NoMatch;
            }
            catch (Exception)
            {
                result.Status = GmsaReaderListWriteStatus.Unknown;
                result.MatchingWriterSids.Clear();
                result.MatchingDenySids.Clear();
            }
            return result;
        }

        internal static Esc16RegistryStatus AssessEsc16(object disableExtensionList)
        {
            var values = disableExtensionList as string[];
            if (values == null)
                return Esc16RegistryStatus.Unknown;
            return values.Any(value => string.Equals(value, "1.3.6.1.4.1.311.25.2", StringComparison.Ordinal))
                ? Esc16RegistryStatus.Present : Esc16RegistryStatus.Absent;
        }

        internal static Esc6RegistryStatus AssessEsc6(uint? editFlags)
        {
            return !editFlags.HasValue ? Esc6RegistryStatus.Unknown
                : (editFlags.Value & 0x40000) != 0 ? Esc6RegistryStatus.Candidate : Esc6RegistryStatus.Absent;
        }

        internal sealed class AdcsLocalRegistryAssessment
        {
            internal bool CheckDcMappings { get; set; }
            internal bool CheckCaSettings { get; set; }
            internal Esc11RegistryStatus Esc11Status { get; set; }
        }

        internal static AdcsLocalRegistryAssessment AssessLocalAdcsRegistry(bool isDomainController, string caName, uint? interfaceFlags)
        {
            return new AdcsLocalRegistryAssessment
            {
                CheckDcMappings = isDomainController,
                CheckCaSettings = !string.IsNullOrWhiteSpace(caName),
                Esc11Status = !interfaceFlags.HasValue
                    ? Esc11RegistryStatus.Unknown
                    : (interfaceFlags.Value & 0x200) == 0
                        ? Esc11RegistryStatus.Candidate
                        : Esc11RegistryStatus.Protected
            };
        }

        public string[] MitreAttackIds { get; } = new[] { "T1018", "T1087.002", "T1558.003", "T1484.001", "T1649", "T1003" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Active Directory Quick Checks", "T1018,T1087.002,T1558.003,T1484.001,T1649,T1003");

            new List<Action>
            {
                PrintCurrentComputerLapsPasswordExposure,
                PrintGmsaReadableByCurrentPrincipal,
                PrintNonDcUnconstrainedDelegation,
                PrintDmsaCreationRights,
                PrintKerberoastableServiceAccounts,
                PrintMachineAccountQuota,
                PrintStagedComputerCandidates,
                PrintAdObjectControlPaths,
                PrintAdcsMisconfigurations
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private const int SampleObjectLimit = 120;
        private const int OuSampleLimit = 50;
        private static readonly TimeSpan OuSearchTimeout = TimeSpan.FromSeconds(5);
        private const int MaxFindingsToPrint = 40;
        private const int DmsaOuSampleLimit = 120;
        private const int GmsaSampleLimit = 120;
        private const int DelegationSampleLimit = 120;
        private static readonly TimeSpan GmsaSearchTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DelegationSearchTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DmsaSearchTimeout = TimeSpan.FromSeconds(5);
        private static readonly Dictionary<Guid, string> GuidNameCache = new Dictionary<Guid, string>();
        private static readonly object GuidCacheLock = new object();

        private static HashSet<string> GetCurrentSidSet()
        {
            var sids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var id = WindowsIdentity.GetCurrent();
                sids.Add(id.User.Value);
                foreach (var g in id.Groups)
                {
                    sids.Add(g.Value);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("    [!] Error obtaining current SIDs: " + ex.Message);
            }
            return sids;
        }

        private static string GetRootDseProp(string prop)
        {
            try
            {
                using (var root = new DirectoryEntry("LDAP://RootDSE"))
                {
                    return root.Properties[prop]?.Value as string;
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint($"    [!] Error accessing RootDSE ({prop}): {ex.Message}");
                return null;
            }
        }

        private static string GetProp(SearchResult r, string name)
        {
            return (r.Properties.Contains(name) && r.Properties[name].Count > 0)
                ? r.Properties[name][0]?.ToString()
                : null;
        }

        // BadSuccessor prerequisite: direct CreateChild rights for the dMSA class on an OU.
        // This is an ACL candidate check, not an exploit or a test of DC patch state.
        private void PrintDmsaCreationRights()
        {
            Beaprint.MainPrint("dMSA creation rights on OUs (BadSuccessor prerequisite)", "T1484.001");
            Beaprint.InfoPrint("  Check whether the current domain identity may create a delegated Managed Service Account in an OU.");

            if (!Checks.IsPartOfDomain)
            {
                Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                return;
            }

            try
            {
                var defaultNc = GetRootDseProp("defaultNamingContext");
                var schemaNc = GetRootDseProp("schemaNamingContext");
                if (string.IsNullOrEmpty(defaultNc) || string.IsNullOrEmpty(schemaNc))
                {
                    Beaprint.GrayPrint("  [-] Could not resolve AD naming contexts.");
                    return;
                }

                Guid dmsaClassGuid;
                using (var schema = new DirectoryEntry("LDAP://" + schemaNc))
                using (var searcher = new DirectorySearcher(schema))
                {
                    searcher.Filter = "(&(objectClass=classSchema)(lDAPDisplayName=msDS-DelegatedManagedServiceAccount))";
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.SizeLimit = 1;
                    searcher.ClientTimeout = DmsaSearchTimeout;
                    searcher.ServerTimeLimit = DmsaSearchTimeout;
                    searcher.PropertiesToLoad.Add("schemaIDGUID");
                    var classResult = searcher.FindOne();
                    var guidBytes = classResult != null && classResult.Properties["schemaIDGUID"].Count > 0
                        ? classResult.Properties["schemaIDGUID"][0] as byte[]
                        : null;
                    if (guidBytes == null || guidBytes.Length != 16)
                    {
                        Beaprint.GrayPrint("  [-] dMSA class is unavailable or its schema GUID cannot be read. Skipping OU ACLs.");
                        return;
                    }
                    dmsaClassGuid = new Guid(guidBytes);
                }

                var currentSids = GetCurrentSidSet();
                if (currentSids.Count == 0)
                {
                    Beaprint.GrayPrint("  [-] Could not identify the current security token.");
                    return;
                }

                int checkedOus = 0, candidateOus = 0, unreadableOus = 0;
                bool sampleTruncated = false;
                using (var domain = new DirectoryEntry("LDAP://" + defaultNc))
                using (var searcher = new DirectorySearcher(domain))
                {
                    searcher.Filter = "(objectClass=organizationalUnit)";
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.SizeLimit = DmsaOuSampleLimit + 1;
                    searcher.SecurityMasks = SecurityMasks.Dacl;
                    searcher.ClientTimeout = DmsaSearchTimeout;
                    searcher.ServerTimeLimit = DmsaSearchTimeout;
                    searcher.PropertiesToLoad.Add("distinguishedName");
                    searcher.PropertiesToLoad.Add("ntSecurityDescriptor");

                    using (var results = searcher.FindAll())
                    {
                        foreach (SearchResult result in results)
                        {
                            if (checkedOus == DmsaOuSampleLimit)
                            {
                                sampleTruncated = true;
                                break;
                            }
                            checkedOus++;

                            var descriptorBytes = result.Properties["ntSecurityDescriptor"]?.Count > 0
                                ? result.Properties["ntSecurityDescriptor"][0] as byte[]
                                : null;
                            if (descriptorBytes == null)
                            {
                                unreadableOus++;
                                continue;
                            }

                            try
                            {
                                var security = new ActiveDirectorySecurity();
                                security.SetSecurityDescriptorBinaryForm(descriptorBytes);
                                if (!HasDmsaCreateChildCandidate(security, currentSids, dmsaClassGuid))
                                {
                                    continue;
                                }
                            }
                            catch
                            {
                                unreadableOus++;
                                continue;
                            }

                            candidateOus++;
                            if (candidateOus <= MaxFindingsToPrint)
                            {
                                Beaprint.BadPrint("  [!] Candidate OU: " + (GetProp(result, "distinguishedName") ?? "<unknown>"));
                            }
                        }
                    }
                }

                if (candidateOus == 0)
                {
                    Beaprint.GrayPrint($"  [-] No direct dMSA CreateChild candidate in {checkedOus} sampled OUs.");
                }
                else
                {
                    Beaprint.GrayPrint($"  [*] {candidateOus} candidate OU(s) in {checkedOus} sampled. Effective rights and DC patch state need verification.");
                    if (candidateOus > MaxFindingsToPrint)
                    {
                        Beaprint.GrayPrint($"  [*] {candidateOus - MaxFindingsToPrint} additional candidate OU(s) omitted.");
                    }
                }

                if (sampleTruncated)
                {
                    Beaprint.GrayPrint($"  [*] OU sample limited to {DmsaOuSampleLimit}; other OUs were not checked.");
                }
                if (unreadableOus > 0)
                {
                    Beaprint.GrayPrint($"  [*] {unreadableOus} OU DACL(s) could not be evaluated.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("  [-] dMSA OU ACL check failed: " + ex.Message);
            }
        }

        private static bool HasDmsaCreateChildCandidate(ActiveDirectorySecurity security, HashSet<string> currentSids, Guid dmsaClassGuid)
        {
            bool allowed = false;
            bool denied = false;
            foreach (ActiveDirectoryAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                var sid = rule.IdentityReference as SecurityIdentifier;
                if (sid == null || !currentSids.Contains(sid.Value))
                {
                    continue;
                }
                if ((rule.PropagationFlags & PropagationFlags.InheritOnly) != 0)
                {
                    continue;
                }

                var rights = rule.ActiveDirectoryRights;
                if ((rights & ActiveDirectoryRights.CreateChild) != ActiveDirectoryRights.CreateChild &&
                    (rights & ActiveDirectoryRights.GenericAll) != ActiveDirectoryRights.GenericAll)
                {
                    continue;
                }

                if (rule.ObjectType != Guid.Empty && rule.ObjectType != dmsaClassGuid)
                {
                    continue;
                }

                if (rule.AccessControlType == AccessControlType.Deny)
                {
                    denied = true;
                }
                else if (rule.AccessControlType == AccessControlType.Allow)
                {
                    allowed = true;
                }
            }

            // A matching deny makes the result uncertain; omit it instead of asserting access.
            return allowed && !denied;
        }

        private void PrintMachineAccountQuota()
        {
            Beaprint.MainPrint("Domain machine-account quota", "T1136.002");
            if (!Checks.IsPartOfDomain)
            {
                Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                return;
            }

            var defaultNc = GetRootDseProp("defaultNamingContext");
            if (string.IsNullOrEmpty(defaultNc))
            {
                Beaprint.InfoPrint("  ms-DS-MachineAccountQuota: unavailable (domain root not resolved).");
                return;
            }

            try
            {
                using (var domain = new DirectoryEntry("LDAP://" + defaultNc))
                {
                    domain.RefreshCache(new[] { "ms-DS-MachineAccountQuota" });
                    var quota = GetDirectoryEntryInt(domain, "ms-DS-MachineAccountQuota");
                    switch (AssessMachineAccountQuota(quota))
                    {
                        case MachineAccountQuotaStatus.Zero:
                            Beaprint.InfoPrint("  ms-DS-MachineAccountQuota: 0 (quota path unavailable; delegated rights or an existing account may still apply).");
                            break;
                        case MachineAccountQuotaStatus.Positive:
                            Beaprint.InfoPrint("  ms-DS-MachineAccountQuota: " + quota.Value + " (domain setting only; current caller's remaining quota and creation rights are unverified).");
                            break;
                        default:
                            Beaprint.InfoPrint("  ms-DS-MachineAccountQuota: unavailable (missing or invalid value).");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.InfoPrint("  ms-DS-MachineAccountQuota: unavailable (LDAP read failed: " + ex.Message + ").");
            }
        }

        private void PrintStagedComputerCandidates()
        {
            Beaprint.MainPrint("Staged computer-account candidates", "T1087.002");
            if (!Checks.IsPartOfDomain)
            {
                Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                return;
            }

            string defaultNc = GetRootDseProp("defaultNamingContext");
            if (string.IsNullOrEmpty(defaultNc))
            {
                Beaprint.GrayPrint("  [?] Candidate visibility unknown: domain root not resolved.");
                return;
            }

            const int sampleLimit = 120;
            const int displayLimit = 20;
            int inspected = 0;
            int candidates = 0;
            int unknown = 0;
            bool truncated = false;
            bool elapsedLimit = false;
            var displayed = new List<string>();
            try
            {
                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNc))
                using (var searcher = new DirectorySearcher(baseDe))
                {
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.ReferralChasing = ReferralChasingOption.None;
                    searcher.PageSize = 0;
                    searcher.SizeLimit = sampleLimit + 1;
                    searcher.ClientTimeout = TimeSpan.FromSeconds(5);
                    searcher.ServerTimeLimit = TimeSpan.FromSeconds(5);
                    searcher.Filter = "(&(objectCategory=computer)(userAccountControl:1.2.840.113556.1.4.803:=32)"
                        + "(userAccountControl:1.2.840.113556.1.4.803:=4096)"
                        + "(!(userAccountControl:1.2.840.113556.1.4.803:=2))"
                        + "(!(userAccountControl:1.2.840.113556.1.4.803:=8192)))";
                    searcher.PropertiesToLoad.Add("sAMAccountName");
                    searcher.PropertiesToLoad.Add("userAccountControl");
                    searcher.PropertiesToLoad.Add("pwdLastSet");

                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    using (var results = searcher.FindAll())
                    foreach (SearchResult result in results)
                    {
                        if (watch.Elapsed >= TimeSpan.FromSeconds(5))
                        {
                            elapsedLimit = true;
                            break;
                        }
                        if (inspected == sampleLimit)
                        {
                            truncated = true;
                            break;
                        }
                        inspected++;
                        var status = AssessStagedComputer(GetIntProp(result, "userAccountControl"));
                        if (status == StagedComputerStatus.Unknown)
                        {
                            unknown++;
                            continue;
                        }
                        if (status != StagedComputerStatus.Candidate)
                            continue;

                        candidates++;
                        if (displayed.Count >= displayLimit)
                            continue;
                        string name = GetProp(result, "sAMAccountName");
                        if (string.IsNullOrWhiteSpace(name)) name = "<unnamed computer>";
                        name = new string(name.Select(c => char.IsControl(c) ? '?' : c).ToArray());
                        displayed.Add(name + (GetFileTimeProp(result, "pwdLastSet").HasValue
                            ? " (password timestamp present)" : " (password timestamp absent or zero)"));
                    }
                }

                Beaprint.InfoPrint("  Inspected " + inspected + " staged-computer candidate object(s); "
                    + candidates + " enabled workstation-trust candidate(s); account-control unknown: " + unknown + ".");
                foreach (string label in displayed) Beaprint.GrayPrint("    - " + label);
                if (candidates > displayed.Count)
                    Beaprint.GrayPrint("  [*] " + (candidates - displayed.Count) + " additional candidate(s) omitted from display.");
                if (truncated) Beaprint.GrayPrint("  [?] LDAP sample capped at 120 objects; further candidates are unknown.");
                if (elapsedLimit) Beaprint.GrayPrint("  [?] LDAP elapsed limit reached; further candidates are unknown.");
                Beaprint.GrayPrint("  [*] Account flags identify review candidates only; they do not prove a predictable password or usable logon. Review machine-principal object rights separately.");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("  [?] Candidate visibility unknown: LDAP search failed or timed out: " + ex.Message);
            }
        }

        // Show matching ACL leads for the current token.
        private void PrintAdObjectControlPaths()
        {
            try
            {
                Beaprint.MainPrint("AD object control surfaces", "T1484.001,T1087.002,T1018");
                Beaprint.LinkPrint(
                    "https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/index.html#acl-abuse",
                    "Review GenericAll, GenericWrite, and attribute-right ACL candidates for password reset, SPN, UAC, RBCD, sidHistory, delegation, and replication paths.");

                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                    return;
                }

                var defaultNC = GetRootDseProp("defaultNamingContext");
                var schemaNC = GetRootDseProp("schemaNamingContext");
                var configNC = GetRootDseProp("configurationNamingContext");

                if (string.IsNullOrEmpty(defaultNC))
                {
                    Beaprint.GrayPrint("  [!] Current computer object ACL coverage unknown: could not resolve defaultNamingContext.");
                    return;
                }

                var sidSet = GetCurrentSidSet();
                var processedDns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var findings = new List<AdObjectFinding>();
                bool samplingIncomplete = false;
                bool credentialNotesSampled = false;
                int sampledUserNotes = 0;
                int credentialNoteCues = 0;
                int credentialNoteRows = 0;

                foreach (var target in EnumerateHighValueTargets(defaultNC))
                {
                    var finding = AnalyzeDirectoryObject(target.DistinguishedName, target.Label, sidSet, schemaNC, configNC);
                    if (finding == null)
                    {
                        continue;
                    }

                    if (processedDns.Add(finding.DistinguishedName))
                    {
                        finding.SamplePriority = 0;
                        findings.Add(finding);
                    }
                }

                // Inspect the exact local computer before the capped ordinary-object sample.
                // A DC computer object can otherwise fall beyond the first 120 results.
                try
                {
                    string computerAccount = LapsPasswordExposure.EscapeLdapFilterValue(Environment.MachineName + "$");
                    using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                    using (var searcher = new DirectorySearcher(baseDe))
                    {
                        searcher.SearchScope = SearchScope.Subtree;
                        searcher.SizeLimit = 1;
                        searcher.ClientTimeout = TimeSpan.FromSeconds(5);
                        searcher.ServerTimeLimit = TimeSpan.FromSeconds(5);
                        searcher.Filter = "(&(objectCategory=computer)(sAMAccountName=" + computerAccount + "))";
                        searcher.PropertiesToLoad.Add("distinguishedName");

                        var result = searcher.FindOne();
                        var dn = result == null ? null : GetProp(result, "distinguishedName");
                        if (string.IsNullOrWhiteSpace(dn))
                        {
                            samplingIncomplete = true;
                            Beaprint.GrayPrint("  [!] Current computer object ACL coverage unknown: object lookup returned no DN.");
                        }
                        else if (ShouldInspectAclTarget(dn, processedDns))
                        {
                            bool aclReadSucceeded;
                            var finding = AnalyzeDirectoryObject(dn, Environment.MachineName + "$", sidSet,
                                schemaNC, configNC, out aclReadSucceeded);
                            if (!aclReadSucceeded)
                            {
                                samplingIncomplete = true;
                                Beaprint.GrayPrint("  [!] Current computer object ACL coverage unknown: DACL read failed for " + dn + ".");
                                // An owner match may still be known even when ACE enumeration fails.
                                if (finding != null)
                                {
                                    finding.SamplePriority = 0;
                                    findings.Add(finding);
                                }
                            }
                            else
                            {
                                processedDns.Add(dn);
                                if (finding != null)
                                {
                                    finding.SamplePriority = 0;
                                    findings.Add(finding);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    samplingIncomplete = true;
                    Beaprint.GrayPrint("  [!] Current computer object ACL coverage unknown: lookup failed: " + ex.Message);
                }

                try
                {
                    using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                    using (var ds = new DirectorySearcher(baseDe))
                    {
                        ds.PageSize = 0;
                        ds.SizeLimit = OuSampleLimit + 1;
                        ds.ReferralChasing = ReferralChasingOption.None;
                        ds.SearchScope = SearchScope.Subtree;
                        ds.ClientTimeout = OuSearchTimeout;
                        ds.ServerTimeLimit = OuSearchTimeout;
                        ds.Filter = "(objectClass=organizationalUnit)";
                        ds.PropertiesToLoad.Add("distinguishedName");

                        using (var results = ds.FindAll())
                        {
                            var sample = SelectOuSample(results.Cast<SearchResult>().Select(r => GetProp(r, "distinguishedName")), OuSampleLimit);
                            foreach (var dn in sample.DistinguishedNames)
                            {
                                if (processedDns.Contains(dn)) continue;
                                var finding = AnalyzeDirectoryObject(dn, dn, sidSet, schemaNC, configNC);
                                if (finding != null && processedDns.Add(finding.DistinguishedName))
                                {
                                    finding.SamplePriority = 1;
                                    findings.Add(finding);
                                }
                            }
                            Beaprint.GrayPrint($"  [*] Sampled {sample.DistinguishedNames.Count} ordinary OU(s) for ACL candidates.");
                            if (sample.Truncated)
                                Beaprint.GrayPrint($"  [*] OU sample limited to {OuSampleLimit}; other OUs were not checked.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    samplingIncomplete = true;
                    Beaprint.GrayPrint("    [!] OU ACL sampling failed: " + ex.Message);
                }

                try
                {
                    using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                    using (var ds = new DirectorySearcher(baseDe))
                    {
                        ds.PageSize = 0;
                        ds.SizeLimit = SampleObjectLimit + 1;
                        ds.ReferralChasing = ReferralChasingOption.None;
                        ds.ClientTimeout = OuSearchTimeout;
                        ds.ServerTimeLimit = OuSearchTimeout;
                        ds.SearchScope = SearchScope.Subtree;
                        ds.SecurityMasks = SecurityMasks.Dacl;
                        ds.Filter = "(|(objectClass=user)(objectClass=group)(objectClass=computer))";
                        ds.PropertiesToLoad.Add("distinguishedName");
                        ds.PropertiesToLoad.Add("sAMAccountName");
                        ds.PropertiesToLoad.Add("name");
                        ds.PropertiesToLoad.Add("objectClass");
                        ds.PropertiesToLoad.Add("description");
                        ds.PropertiesToLoad.Add("info");

                        using (var results = ds.FindAll())
                        {
                            var sample = SelectBoundedSample(results.Cast<SearchResult>(), SampleObjectLimit);
                            credentialNotesSampled = true;
                            foreach (SearchResult r in sample.Items)
                            {
                                var classes = r.Properties.Contains("objectClass")
                                    ? r.Properties["objectClass"].Cast<object>().Select(value => value?.ToString())
                                    : Enumerable.Empty<string>();
                                var classList = classes.ToList();
                                if (classList.Any(value => string.Equals(value, "user", StringComparison.OrdinalIgnoreCase)) &&
                                    !classList.Any(value => string.Equals(value, "computer", StringComparison.OrdinalIgnoreCase)))
                                {
                                    sampledUserNotes++;
                                    var cue = ClassifyAdCredentialNote(classList, GetProp(r, "description"), GetProp(r, "info"));
                                    if (cue != null)
                                    {
                                        credentialNoteCues++;
                                        var account = GetProp(r, "sAMAccountName");
                                        if (!string.IsNullOrWhiteSpace(account) && credentialNoteRows < 12)
                                        {
                                            account = new string(account.Take(128).Select(ch => char.IsControl(ch) ? '?' : ch).ToArray());
                                            Beaprint.BadPrint("    -> AD account note cue: " + account + " — " + cue);
                                            credentialNoteRows++;
                                        }
                                    }
                                }
                                var dn = GetProp(r, "distinguishedName");
                                if (!ShouldInspectAclTarget(dn, processedDns))
                                {
                                    continue;
                                }

                                var label = GetProp(r, "sAMAccountName") ?? GetProp(r, "name") ?? dn;
                                var finding = AnalyzeDirectoryObject(dn, label, sidSet, schemaNC, configNC);
                                if (finding != null && processedDns.Add(finding.DistinguishedName))
                                {
                                    finding.SamplePriority = 2;
                                    findings.Add(finding);
                                }
                            }
                            if (sample.Truncated)
                                Beaprint.GrayPrint($"  [*] LDAP sample capped at {SampleObjectLimit} objects; other user/group/computer objects were not inspected.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    samplingIncomplete = true;
                    Beaprint.GrayPrint("    [!] LDAP sampling failed: " + ex.Message);
                }

                if (credentialNotesSampled)
                    Beaprint.GrayPrint($"  [*] AD account-note review: {sampledUserNotes} user(s) in the capped {SampleObjectLimit}-object mixed sample; {credentialNoteCues} redacted cue(s)" +
                        (credentialNoteCues > credentialNoteRows ? " (first " + credentialNoteRows + " account(s) shown)" : "") +
                        ". This is partial coverage; no matching cue does not clear the domain." +
                        (samplingIncomplete ? " LDAP/ACL sampling was incomplete." : ""));
                else
                    Beaprint.GrayPrint("  [!] AD account-note review incomplete: capped LDAP sample unavailable.");

                if (findings.Count == 0)
                {
                    Beaprint.GrayPrint("  [-] No matching ACL candidates observed in the sampled set; effective access was not evaluated." +
                        (samplingIncomplete ? " LDAP sampling was incomplete." : ""));
                    return;
                }

                var ordered = findings
                    .OrderBy(f => f.SamplePriority == 0 ? 0 : 1)
                    .ThenByDescending(f => f.MaxScore)
                    .ThenBy(f => f.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var truncated = ordered.Count > MaxFindingsToPrint;
                if (truncated)
                {
                    ordered = ordered.Take(MaxFindingsToPrint).ToList();
                }

                Beaprint.GrayPrint($"  [+] Found {findings.Count} object(s) with matching ACL candidate rights. Deny ACEs, inheritance, object-class applicability, and effective access need verification:");
                foreach (var finding in ordered)
                {
                    Beaprint.BadPrint($"    -> ACL candidate: {finding.DisplayName} ({finding.ClassName})");
                    Beaprint.GrayPrint("       DN: " + finding.DistinguishedName);
                    foreach (var impact in finding.Impacts.OrderByDescending(i => i.Score))
                    {
                        Beaprint.GrayPrint($"       * {impact.Impact}: {impact.Detail}");
                    }
                }

                if (truncated)
                {
                    Beaprint.GrayPrint($"  [!] Additional {findings.Count - MaxFindingsToPrint} sampled object(s) not shown.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static IEnumerable<(string Label, string DistinguishedName)> EnumerateHighValueTargets(string defaultNC)
        {
            return new List<(string, string)>
            {
                ("Domain Root", defaultNC),
                ("AdminSDHolder", $"CN=AdminSDHolder,CN=System,{defaultNC}"),
                ("Domain Controllers OU", $"OU=Domain Controllers,{defaultNC}"),
                ("Domain Controllers group", $"CN=Domain Controllers,CN=Users,{defaultNC}"),
                ("Domain Admins", $"CN=Domain Admins,CN=Users,{defaultNC}"),
                ("Enterprise Admins", $"CN=Enterprise Admins,CN=Users,{defaultNC}"),
                ("Schema Admins", $"CN=Schema Admins,CN=Users,{defaultNC}"),
                ("Administrators", $"CN=Administrators,CN=Builtin,{defaultNC}"),
                ("Account Operators", $"CN=Account Operators,CN=Builtin,{defaultNC}"),
                ("Backup Operators", $"CN=Backup Operators,CN=Builtin,{defaultNC}"),
                ("Group Policy Creator Owners", $"CN=Group Policy Creator Owners,CN=Users,{defaultNC}"),
                ("krbtgt", $"CN=krbtgt,CN=Users,{defaultNC}")
            };
        }

        private static AdObjectFinding AnalyzeDirectoryObject(string dn, string label, HashSet<string> sidSet, string schemaNC, string configNC)
        {
            bool aclReadSucceeded;
            return AnalyzeDirectoryObject(dn, label, sidSet, schemaNC, configNC, out aclReadSucceeded);
        }

        private static AdObjectFinding AnalyzeDirectoryObject(string dn, string label, HashSet<string> sidSet, string schemaNC, string configNC,
            out bool aclReadSucceeded)
        {
            aclReadSucceeded = false;
            if (string.IsNullOrEmpty(dn))
            {
                return null;
            }

            try
            {
                using (var entry = new DirectoryEntry("LDAP://" + dn))
                {
                    entry.Options.SecurityMasks = SecurityMasks.Owner | SecurityMasks.Dacl;
                    entry.RefreshCache();
                    return EvaluateSecurity(entry, label ?? dn, sidSet, schemaNC, configNC, out aclReadSucceeded);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static AdObjectFinding EvaluateSecurity(DirectoryEntry entry, string label, HashSet<string> sidSet, string schemaNC, string configNC,
            out bool aclReadSucceeded)
        {
            aclReadSucceeded = false;
            ActiveDirectorySecurity security;
            try
            {
                security = entry.ObjectSecurity;
            }
            catch
            {
                return null;
            }

            if (security == null)
            {
                return null;
            }

            var finding = new AdObjectFinding
            {
                DisplayName = label ?? entry.Name,
                DistinguishedName = entry.Properties?["distinguishedName"]?.Value as string ?? entry.Path,
                ClassName = entry.SchemaClassName ?? "object"
            };

            var seenImpacts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var ownerSid = security.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
                if (ownerSid != null && sidSet.Contains(ownerSid.Value))
                {
                    var impact = new AdAccessImpact
                    {
                        Impact = "Object owner candidate",
                        Detail = "Current token SID matches the owner; review owner rights and the effective DACL.",
                        Score = 3
                    };
                    finding.Impacts.Add(impact);
                    seenImpacts.Add(impact.Impact);
                }
            }
            catch
            {
                // ignore owner lookup issues
            }

            AuthorizationRuleCollection rules;
            try
            {
                rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
            }
            catch
            {
                return finding.Impacts.Count > 0 ? finding : null;
            }

            foreach (ActiveDirectoryAccessRule rule in rules)
            {
                if (rule == null || !(rule.IdentityReference is SecurityIdentifier sid) ||
                    !IsAclCandidateAce(rule.AccessControlType, sidSet.Contains(sid.Value),
                        (rule.PropagationFlags & PropagationFlags.InheritOnly) != 0,
                        rule.IsInherited, rule.InheritedObjectType, finding.ClassName))
                    continue;

                foreach (var impact in MapRuleToImpacts(rule, finding.ClassName, schemaNC, configNC))
                {
                    if (impact == null)
                    {
                        continue;
                    }

                    var key = impact.Impact + "|" + impact.Detail;
                    if (seenImpacts.Add(key))
                    {
                        finding.Impacts.Add(impact);
                    }
                }
            }

            aclReadSucceeded = true;
            return finding.Impacts.Count > 0 ? finding : null;
        }

        private static IEnumerable<AdAccessImpact> MapRuleToImpacts(ActiveDirectoryAccessRule rule, string targetClass, string schemaNC, string configNC)
        {
            return MapRuleToImpacts(rule.ActiveDirectoryRights, rule.ObjectType, targetClass, schemaNC, configNC);
        }

        internal static IEnumerable<AdAccessImpact> MapRuleToImpacts(ActiveDirectoryRights rights, Guid objectType,
            string targetClass, string schemaNC, string configNC)
        {
            var impacts = new List<AdAccessImpact>();

            // AD generic rights are composite masks; a shared read bit is not full control.
            if ((rights & ActiveDirectoryRights.GenericAll) == ActiveDirectoryRights.GenericAll)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "GenericAll",
                    Detail = "Matching full-control allow ACE; target class, deny ACEs, and inheritance require review.",
                    Score = 5
                });
                return impacts;
            }

            if ((rights & ActiveDirectoryRights.GenericWrite) == ActiveDirectoryRights.GenericWrite)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "GenericWrite",
                    Detail = "Matching broad attribute-write ACE; allowed attributes depend on the target class.",
                    Score = 4
                });
            }

            if ((rights & ActiveDirectoryRights.WriteDacl) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "WriteDACL",
                    Detail = "Matching ACL-write ACE; effective control requires review.",
                    Score = 4
                });
            }

            if ((rights & ActiveDirectoryRights.WriteOwner) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "WriteOwner",
                    Detail = "Matching owner-write ACE; effective control requires review.",
                    Score = 3
                });
            }

            if ((rights & ActiveDirectoryRights.CreateChild) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "CreateChild",
                    Detail = "Matching child-create ACE; permitted child classes and effective access require review.",
                    Score = 3
                });
            }

            if ((rights & ActiveDirectoryRights.ExtendedRight) != 0)
            {
                var extImpact = MapExtendedRightImpact(objectType, schemaNC, configNC);
                if (extImpact != null)
                {
                    impacts.Add(extImpact);
                }
            }

            if ((rights & ActiveDirectoryRights.WriteProperty) != 0)
            {
                var attrImpact = MapAttributeWriteImpact(objectType, targetClass, schemaNC, configNC, false);
                if (attrImpact != null)
                {
                    impacts.Add(attrImpact);
                }
            }

            if ((rights & ActiveDirectoryRights.Self) != 0)
            {
                var validatedImpact = MapAttributeWriteImpact(objectType, targetClass, schemaNC, configNC, true);
                if (validatedImpact != null)
                {
                    impacts.Add(validatedImpact);
                }
            }

            return impacts;
        }

        private static AdAccessImpact MapExtendedRightImpact(Guid objectType, string schemaNC, string configNC)
        {
            if (objectType == Guid.Empty)
            {
                return null;
            }

            var name = GetGuidFriendlyName(objectType, schemaNC, configNC)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (name.Contains("reset password") || name.Contains("user-force-change-password"))
            {
                return new AdAccessImpact
                {
                    Impact = "ResetPassword right",
                    Detail = "Matching password-reset ACE; effective access and target applicability require review.",
                    Score = 5
                };
            }

            if (name.Contains("replicating directory changes"))
            {
                return new AdAccessImpact
                {
                    Impact = "Replication (DCSync)",
                    Detail = "Matching replication ACE; additional rights and effective access require review.",
                    Score = name.Contains("filtered") ? 5 : 4
                };
            }

            return null;
        }

        internal static AdAccessImpact MapAttributeWriteImpact(Guid objectType, string targetClass, string schemaNC, string configNC, bool validatedWrite)
        {
            if (objectType == Guid.Empty)
            {
                return new AdAccessImpact
                {
                    Impact = validatedWrite ? "Validated write (broad)" : "WriteProperty (broad)",
                    Detail = validatedWrite
                        ? "Matching ACE covers validated writes supported by the target class; permitted values are constrained."
                        : "Matching ACE covers broad attribute writes; effective access requires review.",
                    Score = 3
                };
            }

            var membershipRight = ClassifyMembershipWrite(objectType, validatedWrite, targetClass);
            if (membershipRight != MembershipWriteRight.None)
            {
                return new AdAccessImpact
                {
                    Impact = membershipRight == MembershipWriteRight.OwnMembership ? "Self-membership validated write" : "member WriteProperty",
                    Detail = DescribeMembershipCandidate(membershipRight),
                    Score = membershipRight == MembershipWriteRight.OwnMembership ? 3 : 5
                };
            }

            var exactRight = ClassifyExactAttributeWrite(objectType, validatedWrite, targetClass);
            if (exactRight != ExactAttributeWriteRight.None)
            {
                if (exactRight == ExactAttributeWriteRight.GmsaReaderList)
                {
                    return new AdAccessImpact
                    {
                        Impact = "gMSA reader-list WriteProperty candidate",
                        Detail = "Exact attribute-write ACE on msDS-GroupMSAMembership; deny ACEs, inheritance, effective access, and any subsequent password read remain unverified.",
                        Score = 5
                    };
                }
                if (exactRight == ExactAttributeWriteRight.AltSecurityIdentities)
                {
                    return new AdAccessImpact
                    {
                        Impact = "altSecurityIdentities WriteProperty candidate",
                        Detail = "Matching certificate-mapping attribute write ACE; effective access, certificate possession/enrollment, CA trust, and strong binding require review.",
                        Score = 5
                    };
                }
                if (exactRight == ExactAttributeWriteRight.ScriptPath)
                {
                    return new AdAccessImpact
                    {
                        Impact = "scriptPath WriteProperty candidate",
                        Detail = "Exact user logon-script attribute write ACE; effective access, a writable and reachable script path, and execution at the target user's next logon remain unverified.",
                        Score = 4
                    };
                }
                return new AdAccessImpact
                {
                    Impact = exactRight == ExactAttributeWriteRight.Upn
                        ? "UPN WriteProperty candidate" : "KeyCredentialLink WriteProperty candidate",
                    Detail = "Matching allow ACE is a lead only; deny ACEs, inheritance, property sets, applicable class, effective access, refreshed token/group context, server support, and certificate path remain unverified.",
                    Score = 4
                };
            }

            // The well-known validated-SPN right needs no schema lookup.
            var attributeName = validatedWrite && objectType == ValidatedSpnGuid
                ? "Validated-SPN" : GetGuidFriendlyName(objectType, schemaNC, configNC);
            if (string.IsNullOrEmpty(attributeName))
                return null;

            var spnRight = validatedWrite && objectType == ValidatedSpnGuid
                ? SpnWriteRight.ValidatedSelf : ClassifySpnWrite(attributeName, validatedWrite);
            if (spnRight != SpnWriteRight.None)
            {
                if (spnRight == SpnWriteRight.ValidatedSelf && !IsValidatedSpnTargetClass(targetClass))
                    return null;
                return new AdAccessImpact
                {
                    Impact = spnRight == SpnWriteRight.WriteProperty ? "SPN WriteProperty" : "SPN validated SELF",
                    Detail = spnRight == SpnWriteRight.WriteProperty
                        ? "Attribute write candidate on servicePrincipalName; effective access still depends on the full ACL."
                        : "Validated SPN write candidate on a computer or service account; values must comply with the account's DNS host name.",
                    Score = spnRight == SpnWriteRight.WriteProperty ? 4 : 2
                };
            }

            if (validatedWrite) return null;

            if (string.Equals(attributeName, "userAccountControl", StringComparison.OrdinalIgnoreCase))
            {
                return new AdAccessImpact
                {
                    Impact = "UAC control",
                    Detail = "Candidate to edit userAccountControl; permitted values and effective access require review.",
                    Score = 4
                };
            }

            if (string.Equals(attributeName, "msDS-AllowedToActOnBehalfOfOtherIdentity", StringComparison.OrdinalIgnoreCase))
            {
                return new AdAccessImpact
                {
                    Impact = "RBCD control",
                    Detail = "Candidate to edit msDS-AllowedToActOnBehalfOfOtherIdentity; effective access requires review.",
                    Score = 5
                };
            }

            if (string.Equals(attributeName, "msDS-AllowedToDelegateTo", StringComparison.OrdinalIgnoreCase))
            {
                return new AdAccessImpact
                {
                    Impact = "Delegation target control",
                    Detail = "Candidate to edit msDS-AllowedToDelegateTo; effective access requires review.",
                    Score = 4
                };
            }

            if (string.Equals(attributeName, "sIDHistory", StringComparison.OrdinalIgnoreCase))
            {
                return new AdAccessImpact
                {
                    Impact = "sidHistory control",
                    Detail = "Candidate to edit sIDHistory; directory restrictions and effective access require review.",
                    Score = 4
                };
            }

            if (string.Equals(attributeName, "unicodePwd", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(attributeName, "userPassword", StringComparison.OrdinalIgnoreCase))
            {
                return new AdAccessImpact
                {
                    Impact = "Password write",
                    Detail = "Candidate password-attribute write; protocol restrictions and effective access require review.",
                    Score = 5
                };
            }

            return null;
        }

        internal static bool IsValidatedSpnTargetClass(string targetClass)
        {
            return string.Equals(targetClass, "computer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(targetClass, "msDS-ManagedServiceAccount", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetGuidFriendlyName(Guid guid, string schemaNC, string configNC)
        {
            if (guid == Guid.Empty)
            {
                return null;
            }

            lock (GuidCacheLock)
            {
                if (GuidNameCache.TryGetValue(guid, out var cached))
                {
                    return cached;
                }
            }

            string resolved = null;

            if (!string.IsNullOrEmpty(schemaNC))
            {
                resolved = LookupGuidInSchema(guid, schemaNC);
            }

            if (resolved == null && !string.IsNullOrEmpty(configNC))
            {
                resolved = LookupGuidInExtendedRights(guid, configNC);
            }

            if (string.IsNullOrEmpty(resolved))
            {
                resolved = guid.ToString();
            }

            lock (GuidCacheLock)
            {
                if (!GuidNameCache.ContainsKey(guid))
                {
                    GuidNameCache[guid] = resolved;
                }
                return GuidNameCache[guid];
            }
        }

        private static string LookupGuidInSchema(Guid guid, string schemaNC)
        {
            try
            {
                using (var schema = new DirectoryEntry("LDAP://" + schemaNC))
                using (var searcher = new DirectorySearcher(schema))
                {
                    searcher.Filter = $"(schemaIDGUID={GuidToLdapFilter(guid)})";
                    searcher.PropertiesToLoad.Add("lDAPDisplayName");
                    searcher.PropertiesToLoad.Add("name");
                    var result = searcher.FindOne();
                    if (result != null)
                    {
                        return GetProp(result, "lDAPDisplayName") ?? GetProp(result, "name");
                    }
                }
            }
            catch
            {
                // ignore schema lookup errors
            }

            return null;
        }

        private static string LookupGuidInExtendedRights(Guid guid, string configNC)
        {
            try
            {
                var extendedRightsDn = $"CN=Extended-Rights,{configNC}";
                using (var rights = new DirectoryEntry("LDAP://" + extendedRightsDn))
                using (var searcher = new DirectorySearcher(rights))
                {
                    searcher.Filter = $"(rightsGuid={guid})";
                    searcher.PropertiesToLoad.Add("displayName");
                    searcher.PropertiesToLoad.Add("name");
                    var result = searcher.FindOne();
                    if (result != null)
                    {
                        return GetProp(result, "displayName") ?? GetProp(result, "name");
                    }
                }
            }
            catch
            {
                // ignore extended rights lookup issues
            }

            return null;
        }

        private static string GuidToLdapFilter(Guid guid)
        {
            var bytes = guid.ToByteArray();
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append($"\\{b:X2}");
            }

            return sb.ToString();
        }

        private class AdObjectFinding
        {
            public int SamplePriority { get; set; }
            public string DisplayName { get; set; }
            public string DistinguishedName { get; set; }
            public string ClassName { get; set; }
            public List<AdAccessImpact> Impacts { get; } = new List<AdAccessImpact>();
            public int MaxScore => Impacts.Count == 0 ? 0 : Impacts.Max(i => i.Score);
        }

        internal class AdAccessImpact
        {
            public string Impact { get; set; }
            public string Detail { get; set; }
            public int Score { get; set; }
        }

        private void PrintCurrentComputerLapsPasswordExposure()
        {
            bool rbcdPrinted = false;
            try
            {
                Beaprint.MainPrint("Current computer LAPS password readable from AD", "T1003");
                Beaprint.LinkPrint(
                    "https://learn.microsoft.com/en-us/windows-server/identity/laps/laps-scenarios-windows-server-active-directory#query-extended-rights-permissions",
                    "A readable cleartext LAPS attribute exposes this computer's managed local administrator credential. Password values are redacted.");

                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                    return;
                }

                string defaultNC = GetRootDseProp("defaultNamingContext");
                if (string.IsNullOrEmpty(defaultNC))
                {
                    Beaprint.GrayPrint("  [-] Could not resolve defaultNamingContext.");
                    PrintCurrentComputerRbcd(null);
                    rbcdPrinted = true;
                    return;
                }

                string computerAccount = LapsPasswordExposure.EscapeLdapFilterValue(Environment.MachineName + "$");
                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                using (var searcher = new DirectorySearcher(baseDe))
                {
                    searcher.SearchScope = SearchScope.Subtree;
                    searcher.SizeLimit = 1;
                    searcher.ClientTimeout = TimeSpan.FromSeconds(5);
                    searcher.ServerTimeLimit = TimeSpan.FromSeconds(5);
                    searcher.Filter = "(&(objectCategory=computer)(sAMAccountName=" + computerAccount + "))";
                    searcher.PropertiesToLoad.Add("distinguishedName");
                    searcher.PropertiesToLoad.Add("ms-Mcs-AdmPwd");
                    searcher.PropertiesToLoad.Add("msLAPS-Password");
                    searcher.PropertiesToLoad.Add("msDS-AllowedToActOnBehalfOfOtherIdentity");

                    SearchResult result = searcher.FindOne();
                    if (result == null)
                    {
                        Beaprint.GrayPrint("  [-] Could not find this computer's Active Directory object.");
                        PrintCurrentComputerRbcd(null);
                        rbcdPrinted = true;
                        return;
                    }

                    PrintCurrentComputerRbcd(result);
                    rbcdPrinted = true;

                    // Presence in the LDAP response proves the current identity can
                    // read a populated value. Never materialize or print the value.
                    bool legacyPasswordReturned = result.Properties.Contains("ms-Mcs-AdmPwd")
                        && result.Properties["ms-Mcs-AdmPwd"].Count > 0;
                    bool windowsLapsPasswordReturned = result.Properties.Contains("msLAPS-Password")
                        && result.Properties["msLAPS-Password"].Count > 0;
                    LapsPasswordExposureReport report = LapsPasswordExposure.Evaluate(
                        legacyPasswordReturned,
                        windowsLapsPasswordReturned);

                    if (!report.IsExposed)
                    {
                        Beaprint.GrayPrint("  [-] No populated cleartext LAPS password attribute was readable for this computer by the current domain identity.");
                        return;
                    }

                    Beaprint.BadPrint("  [!] The current domain identity can read this computer's managed local administrator password (value redacted).");
                    Beaprint.GrayPrint("      Computer: " + Environment.MachineName);
                    Beaprint.GrayPrint("      AD object: " + (GetProp(result, "distinguishedName") ?? "<unknown>"));

                    if (report.WindowsLapsPasswordReadable)
                    {
                        Beaprint.BadPrint("      Readable attribute: msLAPS-Password (Windows LAPS cleartext password)");
                    }

                    if (report.LegacyPasswordReadable)
                    {
                        Beaprint.BadPrint("      Readable attribute: ms-Mcs-AdmPwd (legacy Microsoft LAPS cleartext password)");
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("  [-] LAPS password exposure check failed: " + ex.Message);
                if (!rbcdPrinted)
                    PrintCurrentComputerRbcd(null);
            }
        }

        private static void PrintCurrentComputerRbcd(SearchResult result)
        {
            Beaprint.MainPrint("Current computer resource-based constrained delegation state", "T1484.001");
            if (result == null)
            {
                Beaprint.GrayPrint("  [-] Inaccessible: current computer AD object could not be read.");
                return;
            }

            const string attribute = "msDS-AllowedToActOnBehalfOfOtherIdentity";
            try
            {
                CurrentComputerRbcdReport report;
                if (!result.Properties.Contains(attribute) || result.Properties[attribute].Count == 0)
                    report = CurrentComputerRbcd.Evaluate(null);
                else
                {
                    var bytes = result.Properties[attribute][0] as byte[];
                    report = bytes == null
                        ? new CurrentComputerRbcdReport { Status = CurrentComputerRbcdStatus.Malformed }
                        : CurrentComputerRbcd.Evaluate(bytes);
                }

                switch (report.Status)
                {
                    case CurrentComputerRbcdStatus.Absent:
                        Beaprint.GrayPrint("  [-] Attribute not returned for this computer (absent or not readable by the current identity).");
                        break;
                    case CurrentComputerRbcdStatus.Oversized:
                        Beaprint.GrayPrint("  [?] Descriptor exceeds 16384 bytes; skipped parsing.");
                        break;
                    case CurrentComputerRbcdStatus.Malformed:
                        Beaprint.GrayPrint("  [?] Descriptor is malformed or has no readable DACL; delegation state unknown.");
                        break;
                    case CurrentComputerRbcdStatus.Present:
                        Beaprint.GrayPrint("  [i] Delegation descriptor present (" + report.AceCount + " ACEs). Presence alone does not establish exploitability.");
                        foreach (string sid in report.TrusteeSids)
                            Beaprint.GrayPrint("      DACL trustee SID: " + sid);
                        if (report.TrusteeSids.Count == 0)
                            Beaprint.GrayPrint("      No DACL trustee SIDs in the inspected ACEs.");
                        if (report.Truncated)
                            Beaprint.GrayPrint("      Trustee output truncated (first 128 ACEs, at most 16 distinct SIDs).");
                        break;
                }
            }
            catch (Exception)
            {
                Beaprint.GrayPrint("  [-] Inaccessible: could not read this computer's delegation attribute.");
            }
        }

        // Inventory non-DC computer objects configured for unconstrained delegation.
        private void PrintNonDcUnconstrainedDelegation()
        {
            Beaprint.MainPrint("Non-DC unconstrained delegation configuration", "T1018");
            if (!Checks.IsPartOfDomain)
            {
                Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                return;
            }

            var rows = new List<DelegationComputer>();
            bool ldapError = false;
            try
            {
                var defaultNC = GetRootDseProp("defaultNamingContext");
                if (string.IsNullOrEmpty(defaultNC))
                {
                    Beaprint.GrayPrint("  [?] LDAP naming context unavailable; delegation inventory unknown.");
                    return;
                }

                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                using (var ds = new DirectorySearcher(baseDe))
                {
                    ds.SearchScope = SearchScope.Subtree;
                    ds.PageSize = 0;
                    ds.SizeLimit = DelegationSampleLimit + 1;
                    ds.ClientTimeout = DelegationSearchTimeout;
                    ds.ServerTimeLimit = DelegationSearchTimeout;
                    ds.Filter = "(&(objectCategory=computer)" +
                        "(userAccountControl:1.2.840.113556.1.4.803:=524288)" +
                        "(!(userAccountControl:1.2.840.113556.1.4.803:=8192)))";
                    ds.PropertiesToLoad.Add("sAMAccountName");
                    ds.PropertiesToLoad.Add("distinguishedName");
                    ds.PropertiesToLoad.Add("userAccountControl");

                    using (var results = ds.FindAll())
                    {
                        foreach (SearchResult result in results)
                        {
                            if (rows.Count > DelegationSampleLimit) break;
                            int flags;
                            var rawFlags = GetProp(result, "userAccountControl");
                            int? uac = int.TryParse(rawFlags, out flags) ? (int?)flags : null;
                            rows.Add(new DelegationComputer
                            {
                                Name = GetProp(result, "sAMAccountName") ??
                                    GetProp(result, "distinguishedName"),
                                UserAccountControl = uac
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                ldapError = true;
            }

            var summary = SummarizeDelegationInventory(rows, ldapError);
            foreach (var name in summary.DisplayNames)
                Beaprint.GrayPrint("  [i] Candidate computer: " + name + " (TRUSTED_FOR_DELEGATION set; non-DC).");
            Beaprint.GrayPrint($"  [*] Inspected {summary.Inspected} matching computer object(s): {summary.Candidates} configuration candidate(s), {summary.UnknownFlags} missing or unreadable UAC value(s). This flag alone does not provide a TGT; ticket access requires separate privileges and conditions.");
            if (summary.Candidates > summary.DisplayNames.Count)
                Beaprint.GrayPrint($"  [*] {summary.Candidates - summary.DisplayNames.Count} additional candidate(s) omitted from display.");
            if (summary.Truncated)
                Beaprint.GrayPrint($"  [?] Sample limited to {DelegationSampleLimit} objects; additional matching computers were not checked.");
            if (summary.UnknownFlags > 0)
                Beaprint.GrayPrint("  [?] Some returned objects had unknown UAC flags; their delegation status could not be verified.");
            if (summary.LdapError)
                Beaprint.GrayPrint("  [?] LDAP query failed or ended early; this inventory is partial and domain-wide status is unknown.");
        }

        // Inspect gMSA membership descriptors for possible read-property trustees.
        private void PrintGmsaReadableByCurrentPrincipal()
        {
            try
            {
                Beaprint.MainPrint("gMSA managed password reader candidates", "T1003");
                Beaprint.LinkPrint(
                    "https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/golden-dmsa-gmsa.html",
                    "Look for Group Managed Service Account password access candidates");

                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                    return;
                }

                var defaultNC = GetRootDseProp("defaultNamingContext");
                if (string.IsNullOrEmpty(defaultNC))
                {
                    Beaprint.GrayPrint("  [-] Could not resolve defaultNamingContext.");
                    return;
                }

                var currentSidSet = GetCurrentSidSet();
                if (currentSidSet.Count == 0)
                    Beaprint.GrayPrint("  [-] Current token SIDs unavailable; gMSA access status UNKNOWN.");
                int total = 0, candidates = 0, unknown = 0, denied = 0;
                int writerCandidates = 0, writerDenied = 0, writerUnknown = 0;
                int readerRows = 0, writerRows = 0;
                bool sampleTruncated = false;

                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                using (var ds = new DirectorySearcher(baseDe))
                {
                    ds.PageSize = 0;
                    ds.SizeLimit = GmsaSampleLimit + 1;
                    ds.ClientTimeout = GmsaSearchTimeout;
                    ds.ServerTimeLimit = GmsaSearchTimeout;
                    ds.SecurityMasks = SecurityMasks.Dacl;
                    ds.Filter = "(&(objectClass=msDS-GroupManagedServiceAccount))";
                    ds.PropertiesToLoad.Add("sAMAccountName");
                    ds.PropertiesToLoad.Add("distinguishedName");
                    ds.PropertiesToLoad.Add("msDS-GroupMSAMembership");
                    ds.PropertiesToLoad.Add("ntSecurityDescriptor");

                    using (var results = ds.FindAll())
                    {
                        foreach (SearchResult r in results)
                        {
                            if (total == GmsaSampleLimit)
                            {
                                sampleTruncated = true;
                                break;
                            }
                            total++;
                            var name = GetProp(r, "sAMAccountName") ?? GetProp(r, "distinguishedName") ?? "<unknown>";
                            var dn = GetProp(r, "distinguishedName") ?? "";
                            var descriptorBytes = r.Properties.Contains("msDS-GroupMSAMembership") &&
                                r.Properties["msDS-GroupMSAMembership"].Count > 0
                                ? r.Properties["msDS-GroupMSAMembership"][0] as byte[] : null;
                            var assessment = InspectGmsaMembership(descriptorBytes, currentSidSet);
                            var status = assessment.Status;
                            var objectDescriptor = r.Properties.Contains("ntSecurityDescriptor") &&
                                r.Properties["ntSecurityDescriptor"].Count > 0
                                ? r.Properties["ntSecurityDescriptor"][0] as byte[] : null;
                            var writer = InspectGmsaReaderListWrite(objectDescriptor, currentSidSet);

                            if (assessment.ReaderSids.Count > 0 && readerRows < MaxFindingsToPrint)
                            {
                                readerRows++;
                                Beaprint.GrayPrint($"  Descriptor read-grant trustees for {name}: " +
                                    string.Join(", ", assessment.ReaderSids.Take(8)) +
                                    (assessment.ReaderSids.Count > 8 ? " (more omitted)" : ""));
                            }

                            if (status == GmsaAccessStatus.Candidate)
                            {
                                candidates++;
                                if (candidates <= MaxFindingsToPrint)
                                    Beaprint.BadPrint($"  Current-token SID matches a read-grant candidate for gMSA: {name} (DN: {dn}); " +
                                        string.Join(", ", assessment.MatchingReaderSids.Take(8)));
                            }
                            else if (status == GmsaAccessStatus.Unknown)
                            {
                                unknown++;
                                if (unknown <= MaxFindingsToPrint)
                                    Beaprint.GrayPrint($"  [?] Reader status unknown for gMSA: {name} (missing, malformed, or complex descriptor/token).");
                            }
                            else if (status == GmsaAccessStatus.Denied)
                            {
                                denied++;
                                if (denied <= MaxFindingsToPrint)
                                    Beaprint.GrayPrint($"  [?] Matching deny ACE for gMSA: {name}; effective access unknown. SID(s): " +
                                        string.Join(", ", assessment.MatchingDenySids.Take(8)));
                            }

                            if (writer.Status == GmsaReaderListWriteStatus.Candidate)
                            {
                                writerCandidates++;
                                if (writerRows++ < MaxFindingsToPrint)
                                    Beaprint.BadPrint($"  gMSA reader-list write candidate: {name} (DN: {dn}); " +
                                        (writer.BroadWrite ? "broad attribute-write ACE" : "exact msDS-GroupMSAMembership WriteProperty ACE") +
                                        "; matching token SID(s): " + string.Join(", ", writer.MatchingWriterSids.Take(8)) +
                                        (writer.InheritedAce ? "; inherited ACE" : "") +
                                        ". Effective rights and subsequent password access unverified.");
                            }
                            else if (writer.Status == GmsaReaderListWriteStatus.Denied)
                                writerDenied++;
                            else if (writer.Status == GmsaReaderListWriteStatus.Unknown)
                                writerUnknown++;
                        }
                    }
                }

                Beaprint.GrayPrint($"  [*] Checked {total} gMSA(s): {candidates} token-match candidate(s), {denied} matching deny(s), {unknown} unknown status(es). Descriptor trustees and token matches do not prove effective access; group membership changes require a refreshed token/session.");
                Beaprint.GrayPrint($"  [*] gMSA reader-list object ACLs: {writerCandidates} write candidate(s), {writerDenied} matching deny(s), {writerUnknown} unknown status(es). Exact attribute scope, deny/inheritance ordering, and effective access require review; LDAP DACL visibility may be incomplete.");
                if (candidates > MaxFindingsToPrint)
                    Beaprint.GrayPrint($"  [*] {candidates - MaxFindingsToPrint} additional candidate(s) omitted.");
                if (sampleTruncated)
                    Beaprint.GrayPrint($"  [*] gMSA sample limited to {GmsaSampleLimit}; other accounts were not checked.");
                if (readerRows == MaxFindingsToPrint)
                    Beaprint.GrayPrint("  [*] Trustee output limited to the first 40 gMSAs with read grants.");
                if (writerCandidates > MaxFindingsToPrint)
                    Beaprint.GrayPrint($"  [*] {writerCandidates - MaxFindingsToPrint} additional writer candidate(s) omitted.");
            }
            catch (Exception)
            {
                Beaprint.GrayPrint("  [?] gMSA LDAP enumeration unavailable; reader status unknown.");
            }
        }

        private void PrintKerberoastableServiceAccounts()
        {
            try
            {
                Beaprint.MainPrint("Kerberoasting / service ticket risks", "T1558.003");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/kerberoast.html",
                    "Review service-user SPNs and Kerberos encryption hints");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/silver-ticket.html",
                    "A service key has separate implications; an SPN does not show key possession");

                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                    return;
                }

                var defaultNC = GetRootDseProp("defaultNamingContext");
                if (string.IsNullOrEmpty(defaultNC))
                {
                    Beaprint.GrayPrint("  [-] Could not resolve defaultNamingContext.");
                    return;
                }

                PrintDomainKerberosDefaults(defaultNC);
                EnumerateKerberoastCandidates(defaultNC);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("  [-] Kerberoasting check failed: " + ex.Message);
            }
        }


        // Detect AD CS misconfigurations
        private void PrintAdcsMisconfigurations()
        {
            try
            {
                Beaprint.MainPrint("AD CS misconfigurations for ESC", "T1649");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/ad-certificates.html");
    
                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                    return;
                }

                Beaprint.InfoPrint("Check local AD CS and domain controller registry settings");
                bool IsDomainController = false;
                bool dcRegistryReadable = true;
                try
                {
                    using (var ntdsKey = RegistryHelper.GetReg("HKLM", @"SYSTEM\CurrentControlSet\Services\NTDS"))
                    {
                        IsDomainController = ntdsKey?.ValueCount > 0;
                    }
                }
                catch
                {
                    dcRegistryReadable = false;
                    Beaprint.GrayPrint("  [-] DC registry status unreadable. Skipping DC certificate mapping checks.");
                }
                // We take the active local CA configuration when one is installed.
                string caName = null;
                bool caRegistryReadable = true;
                try
                {
                    caName = RegistryHelper.GetRegValue("HKLM", @"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration", "Active");
                }
                catch
                {
                    caRegistryReadable = false;
                    Beaprint.GrayPrint("  [-] Local CA configuration unreadable.");
                }
                var localAssessment = AssessLocalAdcsRegistry(IsDomainController, caName, null);
                if (localAssessment.CheckDcMappings)
                {
                    // For StrongBinding and CertificateMapping, More details in KB014754 - Registry key information:
                    // https://support.microsoft.com/en-us/topic/kb5014754-certificate-based-authentication-changes-on-windows-domain-controllers-ad2c23b0-15d8-4340-a468-4d4f3b188f16
                    uint? strongBinding = null;
                    try
                    {
                        strongBinding = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Services\Kdc", "StrongCertificateBindingEnforcement");
                    }
                    catch
                    {
                        // Continue to the independent Schannel setting below.
                    }
                    switch (strongBinding)
                    {
                        case 0: 
                            Beaprint.NoColorPrint("  StrongCertificateBindingEnforcement: 0 — legacy weak-mapping setting; verify effective KDC behavior for this Windows version.");
                            break;
                        case 2: 
                            Beaprint.NoColorPrint("  StrongCertificateBindingEnforcement: 2 — configured KDC strong-binding enforcement; other certificate prerequisites are not checked here.");
                            break;
                        case 1:
                            Beaprint.NoColorPrint("  StrongCertificateBindingEnforcement: 1 — legacy compatibility setting; verify current KDC behavior for this Windows version.");
                            break;
                        default:
                            Beaprint.GrayPrint($"  StrongCertificateBindingEnforcement: {(strongBinding.HasValue ? strongBinding.Value.ToString() : "unavailable")} — KDC mapping behavior unknown from this read.");
                            break;

                    }  

                    uint? certMapping = null;
                    try
                    {
                        certMapping = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL", "CertificateMappingMethods");
                    }
                    catch
                    {
                        // A registry read failure leaves this local prerequisite unknown.
                    }
                    var mappingStatus = AssessSchannelUpnMapping(certMapping);
                    if (mappingStatus == SchannelUpnMappingStatus.Unknown)
                        Beaprint.GrayPrint("  CertificateMappingMethods: unavailable — local Schannel UPN mapping status unknown.");
                    else if (mappingStatus == SchannelUpnMappingStatus.Enabled)
                        Beaprint.BadPrint($"  CertificateMappingMethods: 0x{certMapping.Value:X} — UPN mapping enabled on this host. ESC10 is only a candidate if a victim UPN is writable and a suitable client-authentication template is enrollable.");
                    else if ((certMapping.Value & 0x3) != 0)
                        Beaprint.NoColorPrint($"  CertificateMappingMethods: 0x{certMapping.Value:X} — UPN mapping flag absent; weak Subject/Issuer mapping flag present.");
                    else
                        Beaprint.NoColorPrint($"  CertificateMappingMethods: 0x{certMapping.Value:X} — UPN mapping flag absent on this host.");

                }
                else if (dcRegistryReadable)
                {
                    Beaprint.GrayPrint("  [-] Host is not a domain controller. Skipping DC certificate mapping checks.");
                }

                // CA configuration is local to the CA host, which may be a member server.
                if (localAssessment.CheckCaSettings)
                {
                    string caPath = $@"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration\{caName}";
                    uint? interfaceFlags;
                    try
                    {
                        interfaceFlags = RegistryHelper.GetDwordValue("HKLM", caPath, "InterfaceFlags");
                    }
                    catch
                    {
                        interfaceFlags = null;
                    }

                    var assessment = AssessLocalAdcsRegistry(IsDomainController, caName, interfaceFlags);
                    switch (assessment.Esc11Status)
                    {
                        case Esc11RegistryStatus.Candidate:
                            Beaprint.BadPrint("  IF_ENFORCEENCRYPTICERTREQUEST (0x200) clear in InterfaceFlags — ESC11 candidate; requires reachable RPC enrollment, coercion, and a usable certificate template.");
                            break;
                        case Esc11RegistryStatus.Protected:
                            Beaprint.GoodPrint("  IF_ENFORCEENCRYPTICERTREQUEST (0x200) set in InterfaceFlags — RPC enrollment packet privacy enforced; protected from ESC11 relay via this interface.");
                            break;
                        default:
                            Beaprint.GrayPrint("  [-] InterfaceFlags missing or unreadable — ESC11 status UNKNOWN; effective behavior may depend on defaults and backports.");
                            break;
                    }

                    string policyModule = null;
                    try
                    {
                        policyModule = RegistryHelper.GetRegValue("HKLM", $@"{caPath}\PolicyModules", "Active");
                    }
                    catch { /* report unknown below */ }
                    if (!string.IsNullOrWhiteSpace(policyModule))
                    {
                        string policyPath = $@"{caPath}\PolicyModules\{policyModule}";
                        object disableExtensionList = null;
                        uint? editFlags = null;
                        try
                        {
                            using (var policyKey = RegistryHelper.GetReg("HKLM", policyPath))
                            {
                                if (policyKey != null)
                                {
                                    disableExtensionList = policyKey.GetValue("DisableExtensionList");
                                    var editFlagsValue = policyKey.GetValue("EditFlags");
                                    if (editFlagsValue is int)
                                        editFlags = unchecked((uint)(int)editFlagsValue);
                                }
                            }
                        }
                        catch { /* missing or unreadable registry data stays unknown */ }

                        switch (AssessEsc16(disableExtensionList))
                        {
                            case Esc16RegistryStatus.Present:
                                Beaprint.BadPrint("  szOID_NTDS_CA_SECURITY_EXT disabled for the entire CA — ESC16 candidate.");
                                break;
                            case Esc16RegistryStatus.Absent:
                                Beaprint.GoodPrint("  szOID_NTDS_CA_SECURITY_EXT absent from DisableExtensionList — no local ESC16 indicator.");
                                break;
                            default:
                                Beaprint.GrayPrint("  [-] DisableExtensionList missing or unreadable — ESC16 status UNKNOWN.");
                                break;
                        }

                        switch (AssessEsc6(editFlags))
                        {
                            case Esc6RegistryStatus.Candidate:
                                Beaprint.BadPrint("  EDITF_ATTRIBUTESUBJECTALTNAME2 (0x40000) set in EditFlags — ESC6 candidate.");
                                break;
                            case Esc6RegistryStatus.Absent:
                                Beaprint.GoodPrint("  EDITF_ATTRIBUTESUBJECTALTNAME2 (0x40000) clear in EditFlags — no local ESC6 indicator.");
                                break;
                            default:
                                Beaprint.GrayPrint("  [-] EditFlags missing or unreadable — ESC6 status UNKNOWN.");
                                break;
                        }
                    }
                    else
                    {
                        Beaprint.GrayPrint("  [-] Policy Module missing or unreadable — ESC16 and ESC6 status UNKNOWN.");
                    }
                }
                else if (caRegistryReadable)
                {
                    Beaprint.GrayPrint("  [-] Certificate Authority not found. Skipping.");
                }

                // Reuse one template scan for ESC4 rights and passive ESC1/ESC9 configuration candidates.
                Beaprint.InfoPrint("\nIf you can modify a template (WriteDacl/WriteOwner/GenericAll), you can abuse ESC4");
                var configNC = GetRootDseProp("configurationNamingContext");
                if (string.IsNullOrEmpty(configNC))
                {
                    Beaprint.GrayPrint("  [-] Could not resolve configurationNamingContext.");
                    Beaprint.GrayPrint("  [?] ESC1/ESC9 template visibility unknown without the configuration naming context.");
                    return;
                }

                var currentSidSet = GetCurrentSidSet();
                var domainComputersSid = DomainComputersSidFromToken(currentSidSet);
                int checkedTemplates = 0;
                int vulnerable = 0;
                int esc1Checked = 0;
                int esc1Unknown = 0;
                int esc1Candidates = 0;
                bool esc1Capped = false;
                int esc9Unknown = 0;
                int esc9Candidates = 0;
                var esc13Observations = new List<Esc13TemplateObservation>();

                var templatesDn = $"LDAP://CN=Certificate Templates,CN=Public Key Services,CN=Services,{configNC}";

                using (var deBase = new DirectoryEntry(templatesDn))
                using (var ds = new DirectorySearcher(deBase))
                {
                    ds.PageSize = 300;
                    ds.Filter = "(objectClass=pKICertificateTemplate)";
                    ds.PropertiesToLoad.Add("cn");
                    ds.PropertiesToLoad.Add("msPKI-Certificate-Name-Flag");
                    ds.PropertiesToLoad.Add("msPKI-Enrollment-Flag");
                    ds.PropertiesToLoad.Add("msPKI-RA-Signature");
                    ds.PropertiesToLoad.Add("pKIExtendedKeyUsage");
                    ds.PropertiesToLoad.Add("msPKI-Minimal-Key-Size");
                    ds.PropertiesToLoad.Add("msPKI-Certificate-Policy");

                    using (var results = ds.FindAll())
                    {
                        var esc1Watch = System.Diagnostics.Stopwatch.StartNew();
                        foreach (SearchResult r in results)
                        {
                            checkedTemplates++;
                            string templateCn = GetProp(r, "cn") ?? "<unknown>";
                            Esc1TemplateStatus esc1Status = Esc1TemplateStatus.Unknown;
                            Esc9TemplateStatus esc9Status = Esc9TemplateStatus.Unknown;
                            Esc13TemplateObservation esc13Observation = null;
                            bool currentEnrollAllow = false, currentEnrollDeny = false;
                            bool computerEnrollAllow = false, computerEnrollDeny = false;
                            bool descriptorRead = false;
                            if (esc1Checked < 120 && esc1Watch.Elapsed < TimeSpan.FromSeconds(5))
                            {
                                esc1Checked++;
                                IEnumerable<string> ekus = r.Properties.Contains("pKIExtendedKeyUsage")
                                    ? r.Properties["pKIExtendedKeyUsage"].Cast<object>()
                                        .Where(value => value != null).Select(value => value.ToString()).ToArray()
                                    : null;
                                int? enrollmentFlags = GetIntProp(r, "msPKI-Enrollment-Flag");
                                int? requiredSignatures = GetIntProp(r, "msPKI-RA-Signature");
                                esc1Status = AssessEsc1Template(GetIntProp(r, "msPKI-Certificate-Name-Flag"),
                                    enrollmentFlags, requiredSignatures, ekus);
                                esc9Status = AssessEsc9Template(enrollmentFlags, requiredSignatures, ekus);
                                if (esc1Status == Esc1TemplateStatus.Unknown) esc1Unknown++;
                                if (esc9Status == Esc9TemplateStatus.Unknown) esc9Unknown++;
                                if (r.Properties.Contains("msPKI-Certificate-Policy"))
                                {
                                    var policies = r.Properties["msPKI-Certificate-Policy"].Cast<object>()
                                        .Where(value => value != null).Select(value => value.ToString().Trim())
                                        .Where(value => value.Length > 0).Take(33).ToArray();
                                    if (policies.Length > 0)
                                    {
                                        esc13Observation = new Esc13TemplateObservation
                                        {
                                            Name = templateCn,
                                            PolicyOids = policies.Take(32).ToArray(),
                                            PolicyOverflow = policies.Length > 32,
                                            EnrollmentFlags = enrollmentFlags,
                                            RequiredSignatures = requiredSignatures,
                                            ExtendedKeyUsages = ekus?.ToArray()
                                        };
                                        esc13Observations.Add(esc13Observation);
                                    }
                                }
                            }
                            else
                            {
                                esc1Capped = true;
                            }

                            // Fetch security descriptor (DACL)
                            DirectoryEntry de = null;
                            try
                            {
                                de = r.GetDirectoryEntry();
                                de.Options.SecurityMasks = SecurityMasks.Dacl;
                                de.RefreshCache(new[] { "ntSecurityDescriptor" });
                            }
                            catch (Exception)
                            {
                                de?.Dispose();
                                de = null;
                            }

                            if (de != null) try
                            {
                                var sd = de.ObjectSecurity; // ActiveDirectorySecurity
                                var rules = sd.GetAccessRules(true, true, typeof(SecurityIdentifier));
                                descriptorRead = true;
                                bool hit = false;
                                var hitRights = new HashSet<string>();

                                foreach (ActiveDirectoryAccessRule rule in rules)
                                {
                                    var sid = (rule.IdentityReference as SecurityIdentifier)?.Value;
                                    if (string.IsNullOrEmpty(sid)) continue;
                                    if ((esc1Status == Esc1TemplateStatus.Candidate ||
                                        esc9Status == Esc9TemplateStatus.Candidate ||
                                        esc13Observation != null) &&
                                        IsCertificateEnrollAce(rule.ActiveDirectoryRights, rule.ObjectType))
                                    {
                                        bool allowed = rule.AccessControlType == AccessControlType.Allow;
                                        if (currentSidSet.Contains(sid))
                                        {
                                            if (allowed) currentEnrollAllow = true;
                                            else currentEnrollDeny = true;
                                        }
                                        if (domainComputersSid != null &&
                                            string.Equals(domainComputersSid, sid, StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (allowed) computerEnrollAllow = true;
                                            else computerEnrollDeny = true;
                                        }
                                    }
                                    if (rule.AccessControlType != AccessControlType.Allow) continue;
                                    if (!currentSidSet.Contains(sid)) continue;

                                    var rights = rule.ActiveDirectoryRights;
                                    bool dangerous =
                                        rights.HasFlag(ActiveDirectoryRights.GenericAll) ||
                                        rights.HasFlag(ActiveDirectoryRights.WriteDacl) ||
                                        rights.HasFlag(ActiveDirectoryRights.WriteOwner) ||
                                        rights.HasFlag(ActiveDirectoryRights.WriteProperty) ||
                                        rights.HasFlag(ActiveDirectoryRights.ExtendedRight);

                                    if (dangerous)
                                    {
                                        hit = true;
                                        if (rights.HasFlag(ActiveDirectoryRights.GenericAll)) hitRights.Add("GenericAll");
                                        if (rights.HasFlag(ActiveDirectoryRights.WriteDacl)) hitRights.Add("WriteDacl");
                                        if (rights.HasFlag(ActiveDirectoryRights.WriteOwner)) hitRights.Add("WriteOwner");
                                        if (rights.HasFlag(ActiveDirectoryRights.WriteProperty)) hitRights.Add("WriteProperty");
                                        if (rights.HasFlag(ActiveDirectoryRights.ExtendedRight)) hitRights.Add("ExtendedRight");
                                    }
                                }

                                if (hit)
                                {
                                    vulnerable++;
                                    Beaprint.BadPrint($"  Dangerous rights over template: {templateCn}  (Rights: {string.Join(",", hitRights)})");
                                }
                            }
                            catch (Exception)
                            {
                                // ignore templates we couldn't read
                            }
                            finally
                            {
                                de?.Dispose();
                            }
                            if (esc13Observation != null)
                            {
                                esc13Observation.DescriptorRead = descriptorRead;
                                esc13Observation.CurrentEnrollAllow = currentEnrollAllow;
                                esc13Observation.CurrentEnrollDeny = currentEnrollDeny;
                                esc13Observation.ComputerEnrollAllow = computerEnrollAllow;
                                esc13Observation.ComputerEnrollDeny = computerEnrollDeny;
                            }
                            if (esc1Status == Esc1TemplateStatus.Candidate)
                            {
                                esc1Candidates++;
                                if (esc1Candidates <= 20)
                                {
                                    string enrollment = DescribeEsc1EnrollAceEvidence(descriptorRead,
                                        currentEnrollAllow, currentEnrollDeny, computerEnrollAllow,
                                        computerEnrollDeny, domainComputersSid != null);
                                    int? minimumKey = GetIntProp(r, "msPKI-Minimal-Key-Size");
                                    string message = "  ESC1 configuration candidate: " + templateCn + " (" + enrollment
                                        + "; " + DescribeTemplateMinimumKeySize(minimumKey) + ").";
                                    if ((currentEnrollAllow && !currentEnrollDeny) ||
                                        (computerEnrollAllow && !computerEnrollDeny)) Beaprint.BadPrint(message);
                                    else Beaprint.GrayPrint(message);
                                }
                            }
                            if (esc9Status == Esc9TemplateStatus.Candidate)
                            {
                                esc9Candidates++;
                                if (esc9Candidates <= 20)
                                {
                                    string enrollment = DescribeEsc1EnrollAceEvidence(descriptorRead,
                                        currentEnrollAllow, currentEnrollDeny, computerEnrollAllow,
                                        computerEnrollDeny, domainComputersSid != null);
                                    string message = "  ESC9 no-SID-extension configuration candidate: " + templateCn
                                        + " (" + enrollment + ").";
                                    if ((currentEnrollAllow && !currentEnrollDeny) ||
                                        (computerEnrollAllow && !computerEnrollDeny)) Beaprint.BadPrint(message);
                                    else Beaprint.GrayPrint(message);
                                }
                            }
                        }
                    }
                }

                if (esc13Observations.Count > 0)
                {
                    bool oidLookupComplete;
                    var linkedGroups = ReadOidGroupLinks(configNC, out oidLookupComplete);
                    int esc13Candidates = 0, esc13Unknown = 0;
                    foreach (var observation in esc13Observations)
                    {
                        string linkedGroup = null;
                        var status = observation.PolicyOverflow ? Esc13TemplateStatus.Unknown :
                            AssessEsc13Template(observation.PolicyOids, linkedGroups, oidLookupComplete,
                                observation.EnrollmentFlags, observation.RequiredSignatures,
                                observation.ExtendedKeyUsages, out linkedGroup);
                        if (status == Esc13TemplateStatus.Unknown) esc13Unknown++;
                        if (status != Esc13TemplateStatus.Candidate) continue;
                        esc13Candidates++;
                        if (esc13Candidates > 20) continue;
                        string enrollment = DescribeEsc1EnrollAceEvidence(observation.DescriptorRead,
                            observation.CurrentEnrollAllow, observation.CurrentEnrollDeny,
                            observation.ComputerEnrollAllow, observation.ComputerEnrollDeny,
                            domainComputersSid != null);
                        string message = "  ESC13 linked issuance-policy candidate: " + observation.Name
                            + " -> " + linkedGroup + " (" + enrollment + ").";
                        if ((observation.CurrentEnrollAllow && !observation.CurrentEnrollDeny) ||
                            (observation.ComputerEnrollAllow && !observation.ComputerEnrollDeny))
                            Beaprint.BadPrint(message);
                        else Beaprint.GrayPrint(message);
                    }
                    Beaprint.GrayPrint("  [*] ESC13 issuance-policy review: " + esc13Observations.Count
                        + " policy-bearing template(s) assessed, " + esc13Candidates + " candidate(s), "
                        + esc13Unknown + " with incomplete evidence.");
                    if (esc13Candidates > 20)
                        Beaprint.GrayPrint("  [*] " + (esc13Candidates - 20)
                            + " additional ESC13 candidate(s) omitted from display.");
                    if (!oidLookupComplete)
                        Beaprint.GrayPrint("  [?] Linked issuance-policy OID query incomplete or capped at 120; all policy assessments are unknown.");
                    Beaprint.GrayPrint("  [*] ESC13 still requires a published template, CA enrollment rights, effective ACLs, and a working certificate-authentication path; no certificate was requested.");
                }

                Beaprint.GrayPrint("  [*] ESC1 configuration review: " + esc1Checked + " template(s) assessed, "
                    + esc1Candidates + " candidate(s), " + esc1Unknown + " with incomplete attributes.");
                Beaprint.GrayPrint("  [*] ESC9 no-SID-extension review: " + esc1Checked + " template(s) assessed, "
                    + esc9Candidates + " configuration candidate(s), " + esc9Unknown + " with incomplete attributes.");
                if (esc1Candidates > 20)
                    Beaprint.GrayPrint("  [*] " + (esc1Candidates - 20) + " additional ESC1 candidate(s) omitted from display.");
                if (esc9Candidates > 20)
                    Beaprint.GrayPrint("  [*] " + (esc9Candidates - 20) + " additional ESC9 candidate(s) omitted from display.");
                if (esc1Capped)
                    Beaprint.GrayPrint("  [?] ESC1/ESC9/ESC13 assessment capped at 120 templates or 5 seconds; remaining templates are unknown. ESC4 scan continued.");
                Beaprint.GrayPrint("  [*] Template flags and allow ACEs are leads only; publication, CA rights, effective ACLs and KDC strong SID mapping remain unverified.");
                if (esc9Candidates > 0)
                    Beaprint.GrayPrint("  [*] ESC9 mapping caveat: current patched KDCs require a strong certificate mapping; the historical StrongCertificateBindingEnforcement compatibility override ended in September 2025. Schannel UPN mapping is a separate endpoint setting. Local registry values do not prove the effective authentication path or a writable target UPN.");

                if (vulnerable == 0)
                {
                    Beaprint.GrayPrint($"  [-] No templates with dangerous rights found (checked {checkedTemplates}).");
                }
                else
                {
                    Beaprint.GrayPrint("  [*] Tip: Abuse with tools like Certipy (template write -> ESC1 -> enroll).");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
                Beaprint.GrayPrint("  [?] ESC1/ESC9 template visibility unknown because the AD CS enumeration did not complete.");
            }
        }
        private void PrintDomainKerberosDefaults(string defaultNc)
        {
            try
            {
                using (var domainEntry = new DirectoryEntry("LDAP://" + defaultNc))
                {
                    var encValue = GetDirectoryEntryInt(domainEntry, "msDS-DefaultSupportedEncryptionTypes");
                    if (encValue.HasValue)
                    {
                        var desc = DescribeEncTypes(encValue);
                        if (IsRc4Allowed(encValue))
                            Beaprint.BadPrint($"  Domain default supported encryption types: {desc} — RC4/NT hash tickets allowed.");
                        else
                            Beaprint.GoodPrint($"  Domain default supported encryption types: {desc} — RC4 disabled.");
                    }
                    else
                    {
                        Beaprint.GrayPrint("  [-] Domain default supported encryption types not set (legacy compatibility defaults to RC4).");
                    }
                }

                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNc))
                using (var ds = new DirectorySearcher(baseDe))
                {
                    ds.Filter = "(&(objectClass=user)(sAMAccountName=krbtgt))";
                    ds.PropertiesToLoad.Add("msDS-SupportedEncryptionTypes");
                    var result = ds.FindOne();
                    if (result != null)
                    {
                        var encValue = GetIntProp(result, "msDS-SupportedEncryptionTypes");
                        if (encValue.HasValue)
                        {
                            var desc = DescribeEncTypes(encValue);
                            if (IsRc4Allowed(encValue))
                                Beaprint.BadPrint($"  krbtgt supports: {desc} — RC4 TGTs can still be issued.");
                            else
                                Beaprint.GoodPrint($"  krbtgt supports: {desc}.");
                        }
                        else
                        {
                            Beaprint.GrayPrint("  [-] krbtgt enc types inherit domain defaults (unspecified).");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("  [-] Unable to query Kerberos defaults: " + ex.Message);
            }
        }

        private void EnumerateKerberoastCandidates(string defaultNc)
        {
            const int searchLimit = 201;
            const int displayLimit = 20;
            int checkedAccounts = 0;
            int highTotal = 0;
            int mediumTotal = 0;
            int otherTotal = 0;
            int unknownEligibility = 0;
            bool truncated = false;
            bool elapsedLimit = false;
            var high = new List<KerberoastCandidate>();
            var medium = new List<KerberoastCandidate>();
            var other = new List<KerberoastCandidate>();

            try
            {
                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNc))
                using (var ds = new DirectorySearcher(baseDe))
                {
                    ds.PageSize = 0;
                    ds.SizeLimit = searchLimit;
                    ds.ReferralChasing = ReferralChasingOption.None;
                    ds.ClientTimeout = TimeSpan.FromSeconds(5);
                    ds.ServerTimeLimit = TimeSpan.FromSeconds(5);
                    ds.Filter = "(&(objectClass=user)(servicePrincipalName=*))";
                    ds.PropertiesToLoad.Add("sAMAccountName");
                    ds.PropertiesToLoad.Add("distinguishedName");
                    ds.PropertiesToLoad.Add("servicePrincipalName");
                    ds.PropertiesToLoad.Add("msDS-SupportedEncryptionTypes");
                    ds.PropertiesToLoad.Add("userAccountControl");
                    ds.PropertiesToLoad.Add("pwdLastSet");
                    ds.PropertiesToLoad.Add("memberOf");
                    ds.PropertiesToLoad.Add("objectClass");

                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    using (var results = ds.FindAll())
                    foreach (SearchResult r in results)
                    {
                        if (watch.Elapsed > TimeSpan.FromSeconds(5))
                        {
                            elapsedLimit = true;
                            break;
                        }
                        checkedAccounts++;
                        if (checkedAccounts == searchLimit)
                        {
                            truncated = true;
                            break;
                        }
                        var candidate = BuildKerberoastCandidate(r);
                        if (candidate == null)
                        {
                            if (!GetIntProp(r, "userAccountControl").HasValue &&
                                !IsComputerObject(r) && !IsManagedServiceAccount(r)) unknownEligibility++;
                            continue;
                        }

                        if (candidate.Priority == 2)
                        {
                            highTotal++;
                            high.Add(candidate);
                        }
                        else if (candidate.Priority == 1)
                        {
                            mediumTotal++;
                            medium.Add(candidate);
                        }
                        else { otherTotal++; other.Add(candidate); }
                    }
                }

                int processed = Math.Min(checkedAccounts, searchLimit - 1);
                Beaprint.InfoPrint($"Inspected {processed} SPN-bearing objects; enabled service-user SPNs: {highTotal + mediumTotal + otherTotal} (priority: {highTotal} high, {mediumTotal} review, {otherTotal} other); eligibility unknown: {unknownEligibility}.");
                if (truncated)
                    Beaprint.GrayPrint("  [*] LDAP sample capped at 200 objects; further account counts and priority are unknown.");
                if (elapsedLimit)
                    Beaprint.GrayPrint("  [?] LDAP elapsed limit reached; further account counts and priority are unknown.");
                if (unknownEligibility > 0)
                    Beaprint.GrayPrint("  [*] Missing account-control data leaves some enabled states unknown.");

                if (highTotal + mediumTotal + otherTotal == 0)
                {
                    Beaprint.GrayPrint("  No enabled service-user SPNs observed in this bounded sample.");
                    return;
                }

                var shown = high.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase).Take(8)
                    .Concat(medium.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase).Take(4))
                    .Concat(other.OrderByDescending(c => c.HasSqlSpn).ThenBy(c => c.Label, StringComparer.OrdinalIgnoreCase).Take(8))
                    .Take(displayLimit).ToList();
                foreach (var c in shown)
                    Beaprint.ColorPrint($"      - [{c.PriorityLabel}] {c.Label} | SPNs: {c.SpnSummary} | Enc flags: {c.Encryption} | {c.Reason}", c.Priority == 2 ? Beaprint.LRED : Beaprint.YELLOW);
                int omitted = highTotal + mediumTotal + otherTotal - shown.Count;
                if (omitted > 0) Beaprint.GrayPrint($"  [*] {omitted} additional enabled service-user SPN account(s) omitted from display.");
                Beaprint.GrayPrint("  [*] SPN/encryption flags do not prove a weak password, issued ticket type, or service-key possession.");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("  [?] SPN visibility unknown: LDAP search failed or timed out: " + ex.Message);
            }
        }

        private KerberoastCandidate BuildKerberoastCandidate(SearchResult r)
        {
            var sam = GetProp(r, "sAMAccountName");
            var dn = GetProp(r, "distinguishedName");
            var uac = GetIntProp(r, "userAccountControl");
            if (IsComputerObject(r) || IsManagedServiceAccount(r) || !uac.HasValue || (uac.Value & 0x2) != 0)
                return null;

            var encValue = GetIntProp(r, "msDS-SupportedEncryptionTypes");
            bool rc4Flag = encValue.HasValue && encValue.Value != 0 && (encValue.Value & EncFlagRc4) != 0;
            bool passwordNeverExpires = uac.HasValue && (uac.Value & 0x10000) != 0;
            DateTime? pwdLastSet = GetFileTimeProp(r, "pwdLastSet");
            bool stalePassword = pwdLastSet.HasValue && pwdLastSet.Value < DateTime.UtcNow.AddDays(-365);
            var privilegeHits = GetPrivilegedGroups(r);
            var reasons = new List<string>();

            if (rc4Flag)
                reasons.Add("RC4 flag set");
            if (passwordNeverExpires)
                reasons.Add("PasswordNeverExpires");
            if (stalePassword)
                reasons.Add("PwdLastSet " + pwdLastSet.Value.ToString("yyyy-MM-dd"));
            if (privilegeHits.Count > 0)
                reasons.Add("Privileged: " + string.Join("/", privilegeHits));

            int priority = AssessSpnPriority(uac, IsComputerObject(r), IsManagedServiceAccount(r), encValue,
                passwordNeverExpires, stalePassword, privilegeHits.Count > 0);

            var label = !string.IsNullOrEmpty(sam) ? sam : dn;
            return new KerberoastCandidate
            {
                Label = label ?? "<unknown>",
                SpnSummary = BuildSpnSummary(r),
                Encryption = DescribeEncTypes(encValue),
                Reason = reasons.Count > 0 ? string.Join("; ", reasons) : "No priority flag observed",
                Priority = priority,
                HasSqlSpn = HasSpnPrefix(r, "MSSQLSvc/")
            };
        }

        internal static int AssessSpnPriority(int? uac, bool isComputer, bool isManagedServiceAccount,
            int? encryptionFlags, bool passwordNeverExpires, bool stalePassword, bool privileged)
        {
            if (isComputer || isManagedServiceAccount || !uac.HasValue || (uac.Value & 0x2) != 0)
                return -1;
            if ((encryptionFlags.HasValue && encryptionFlags.Value != 0 && (encryptionFlags.Value & EncFlagRc4) != 0) || privileged)
                return 2;
            return passwordNeverExpires || stalePassword ? 1 : 0;
        }

        private static bool HasSpnPrefix(SearchResult r, string prefix)
        {
            if (!r.Properties.Contains("servicePrincipalName")) return false;
            foreach (var value in r.Properties["servicePrincipalName"])
                if (value != null && value.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string BuildSpnSummary(SearchResult r)
        {
            if (!r.Properties.Contains("servicePrincipalName") || r.Properties["servicePrincipalName"].Count == 0)
                return "<none>";

            var values = r.Properties["servicePrincipalName"];
            var all = new List<string>();
            foreach (var value in values)
                if (value != null && !string.IsNullOrEmpty(value.ToString())) all.Add(value.ToString());
            var list = PrioritizeSpns(all).Take(3).ToList();

            string summary = list.Count > 0 ? string.Join(", ", list) : "<none>";
            if (all.Count > list.Count)
                summary += $" (+{all.Count - list.Count} more)";
            return summary;
        }

        internal static IEnumerable<string> PrioritizeSpns(IEnumerable<string> spns)
        {
            return spns.OrderByDescending(spn => spn.StartsWith("MSSQLSvc/", StringComparison.OrdinalIgnoreCase))
                .ThenBy(spn => spn, StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> GetPrivilegedGroups(SearchResult r)
        {
            var hits = new List<string>();
            if (!r.Properties.Contains("memberOf"))
                return hits;

            var memberships = r.Properties["memberOf"];
            foreach (var membership in memberships)
            {
                var cn = ExtractCn(membership?.ToString());
                if (string.IsNullOrEmpty(cn))
                    continue;

                foreach (var keyword in PrivilegedGroupKeywords)
                {
                    if (cn.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!hits.Contains(cn))
                            hits.Add(cn);
                        break;
                    }
                }
            }
            return hits;
        }

        private static string ExtractCn(string dn)
        {
            if (string.IsNullOrEmpty(dn))
                return null;

            var parts = dn.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                    return trimmed.Substring(3);
            }
            return dn;
        }

        private static bool IsComputerObject(SearchResult r)
        {
            return HasObjectClass(r, "computer");
        }

        private static bool IsManagedServiceAccount(SearchResult r)
        {
            return HasObjectClass(r, "msDS-ManagedServiceAccount") || HasObjectClass(r, "msDS-GroupManagedServiceAccount");
        }

        private static bool HasObjectClass(SearchResult r, string className)
        {
            if (!r.Properties.Contains("objectClass"))
                return false;

            foreach (var val in r.Properties["objectClass"])
            {
                if (string.Equals(val?.ToString(), className, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static DateTime? GetFileTimeProp(SearchResult r, string propName)
        {
            if (!r.Properties.Contains(propName) || r.Properties[propName].Count == 0)
                return null;
            return ConvertFileTime(r.Properties[propName][0]);
        }

        private static DateTime? ConvertFileTime(object value)
        {
            if (value == null)
                return null;
            try
            {
                if (value is long longVal)
                {
                    if (longVal <= 0) return null;
                    return DateTime.FromFileTimeUtc(longVal);
                }

                if (value is IConvertible convertible)
                {
                    long converted = convertible.ToInt64(null);
                    if (converted > 0)
                        return DateTime.FromFileTimeUtc(converted);
                }

                var type = value.GetType();
                var highProp = type.GetProperty("HighPart", BindingFlags.Public | BindingFlags.Instance);
                var lowProp = type.GetProperty("LowPart", BindingFlags.Public | BindingFlags.Instance);
                if (highProp != null && lowProp != null)
                {
                    int high = Convert.ToInt32(highProp.GetValue(value, null));
                    int low = Convert.ToInt32(lowProp.GetValue(value, null));
                    long fileTime = ((long)high << 32) | (uint)low;
                    if (fileTime > 0)
                        return DateTime.FromFileTimeUtc(fileTime);
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        private static int? GetIntProp(SearchResult r, string name)
        {
            if (!r.Properties.Contains(name) || r.Properties[name].Count == 0)
                return null;
            return ConvertToNullableInt(r.Properties[name][0]);
        }

        private static int? GetDirectoryEntryInt(DirectoryEntry entry, string name)
        {
            try
            {
                return ConvertToNullableInt(entry.Properties[name]?.Value);
            }
            catch
            {
                return null;
            }
        }

        private static int? ConvertToNullableInt(object value)
        {
            if (value == null)
                return null;
            if (value is int intValue)
                return intValue;
            if (value is long longValue)
                return unchecked((int)longValue);
            if (int.TryParse(value.ToString(), out var parsed))
                return parsed;
            return null;
        }

        private static bool IsRc4Allowed(int? encValue)
        {
            if (!encValue.HasValue || encValue.Value == 0)
                return true;
            return (encValue.Value & EncFlagRc4) != 0;
        }

        private static bool HasAes(int? encValue)
        {
            if (!encValue.HasValue)
                return false;
            return (encValue.Value & (EncFlagAes128 | EncFlagAes256)) != 0;
        }

        private static string DescribeEncTypes(int? encValue)
        {
            if (!encValue.HasValue || encValue.Value == 0)
                return "Unspecified (effective ticket type unknown)";

            var parts = new List<string>();
            if ((encValue.Value & EncFlagDesCrc) != 0) parts.Add("DES-CBC-CRC");
            if ((encValue.Value & EncFlagDesMd5) != 0) parts.Add("DES-CBC-MD5");
            if ((encValue.Value & EncFlagRc4) != 0) parts.Add("RC4-HMAC");
            if ((encValue.Value & EncFlagAes128) != 0) parts.Add("AES128");
            if ((encValue.Value & EncFlagAes256) != 0) parts.Add("AES256");
            if ((encValue.Value & 0x20) != 0) parts.Add("FAST");
            if (parts.Count == 0) parts.Add($"0x{encValue.Value:X}");
            return string.Join(", ", parts);
        }

        private class KerberoastCandidate
        {
            public string Label { get; set; }
            public string SpnSummary { get; set; }
            public string Encryption { get; set; }
            public string Reason { get; set; }
            public int Priority { get; set; }
            public string PriorityLabel { get { return Priority == 2 ? "higher priority" : Priority == 1 ? "review" : "other service account"; } }
            public bool HasSqlSpn { get; set; }
        }

        private static readonly string[] PrivilegedGroupKeywords = new[]
        {
            "Domain Admin",
            "Enterprise Admin",
            "Administrators",
            "Exchange",
            "Schema Admin",
            "Account Operator",
            "Server Operator",
            "Backup Operator",
            "DnsAdmin"
        };

        private const int EncFlagDesCrc = 0x1;
        private const int EncFlagDesMd5 = 0x2;
        private const int EncFlagRc4 = 0x4;
        private const int EncFlagAes128 = 0x8;
        private const int EncFlagAes256 = 0x10;


    }
}
