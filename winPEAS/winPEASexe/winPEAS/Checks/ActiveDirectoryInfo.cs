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
    // Lightweight AD-oriented checks for common escalation paths (gMSA readable password, AD CS template control)
    internal class ActiveDirectoryInfo : ISystemCheck
    {
        internal enum MachineAccountQuotaStatus { Unavailable, Zero, Positive }

        internal static MachineAccountQuotaStatus AssessMachineAccountQuota(int? quota)
        {
            if (!quota.HasValue || quota.Value < 0) return MachineAccountQuotaStatus.Unavailable;
            return quota.Value == 0 ? MachineAccountQuotaStatus.Zero : MachineAccountQuotaStatus.Positive;
        }

        internal enum Esc11RegistryStatus
        {
            Unknown,
            Candidate,
            Protected
        }

        internal enum GmsaAccessStatus { Unknown, NoMatch, Denied, Candidate }
        internal enum Esc16RegistryStatus { Unknown, Present, Absent }
        internal enum Esc6RegistryStatus { Unknown, Candidate, Absent }

        internal static GmsaAccessStatus AssessGmsaMembership(byte[] descriptorBytes, ISet<string> currentSids)
        {
            if (descriptorBytes == null || descriptorBytes.Length == 0 || currentSids == null || currentSids.Count == 0)
                return GmsaAccessStatus.Unknown;

            try
            {
                var descriptor = new RawSecurityDescriptor(descriptorBytes, 0);
                if ((descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) == 0 || descriptor.DiscretionaryAcl == null)
                    return GmsaAccessStatus.Unknown;

                bool allowed = false;
                bool uncertain = false;
                foreach (GenericAce ace in descriptor.DiscretionaryAcl)
                {
                    var qualified = ace as QualifiedAce;
                    if (qualified == null || (ace.AceFlags & AceFlags.InheritOnly) != 0 ||
                        !currentSids.Contains(qualified.SecurityIdentifier.Value))
                        continue;

                    // AD checks RIGHT_DS_READ_PROPERTY against this descriptor.
                    const int readProperty = 0x10;
                    const int genericRead = unchecked((int)0x80000000);
                    const int genericAll = 0x10000000;
                    if ((qualified.AccessMask & (readProperty | genericRead | genericAll)) == 0)
                        continue;

                    if (qualified.AceQualifier == AceQualifier.AccessDenied)
                        return GmsaAccessStatus.Denied;

                    if (qualified.AceQualifier == AceQualifier.AccessAllowed)
                    {
                        // Conditional or object-specific ACEs need a full AD access check.
                        var common = qualified as CommonAce;
                        if (common != null && !common.IsCallback)
                            allowed = true;
                        else
                            uncertain = true;
                    }
                }
                return uncertain ? GmsaAccessStatus.Unknown : allowed ? GmsaAccessStatus.Candidate : GmsaAccessStatus.NoMatch;
            }
            catch (Exception)
            {
                return GmsaAccessStatus.Unknown;
            }
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
                PrintDmsaCreationRights,
                PrintKerberoastableServiceAccounts,
                PrintMachineAccountQuota,
                PrintAdObjectControlPaths,
                PrintAdcsMisconfigurations
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private const int SampleObjectLimit = 120;
        private const int MaxFindingsToPrint = 40;
        private const int DmsaOuSampleLimit = 120;
        private const int GmsaSampleLimit = 120;
        private static readonly TimeSpan GmsaSearchTimeout = TimeSpan.FromSeconds(5);
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

        // Highlight objects where the current principal already has useful write/control rights
        private void PrintAdObjectControlPaths()
        {
            try
            {
                Beaprint.MainPrint("AD object control surfaces", "T1484.001,T1087.002,T1018");
                Beaprint.LinkPrint(
                    "https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/index.html#acl-abuse",
                    "Look for objects where you have GenericAll/GenericWrite/attribute rights for ACL abuse (password reset, SPN/UAC/RBCD, sidHistory, delegation, DCSync).");

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
                    Beaprint.GrayPrint("  [-] Could not resolve defaultNamingContext.");
                    return;
                }

                var sidSet = GetCurrentSidSet();
                var processedDns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var findings = new List<AdObjectFinding>();

                foreach (var target in EnumerateHighValueTargets(defaultNC))
                {
                    var finding = AnalyzeDirectoryObject(target.DistinguishedName, target.Label, sidSet, schemaNC, configNC);
                    if (finding == null)
                    {
                        continue;
                    }

                    if (processedDns.Add(finding.DistinguishedName))
                    {
                        findings.Add(finding);
                    }
                }

                try
                {
                    using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                    using (var ds = new DirectorySearcher(baseDe))
                    {
                        ds.PageSize = 200;
                        ds.SizeLimit = SampleObjectLimit;
                        ds.SearchScope = SearchScope.Subtree;
                        ds.SecurityMasks = SecurityMasks.Dacl;
                        ds.Filter = "(|(objectClass=user)(objectClass=group)(objectClass=computer))";
                        ds.PropertiesToLoad.Add("distinguishedName");
                        ds.PropertiesToLoad.Add("sAMAccountName");
                        ds.PropertiesToLoad.Add("name");

                        using (var results = ds.FindAll())
                        {
                            foreach (SearchResult r in results)
                            {
                                var dn = GetProp(r, "distinguishedName");
                                if (string.IsNullOrEmpty(dn) || processedDns.Contains(dn))
                                {
                                    continue;
                                }

                                var label = GetProp(r, "sAMAccountName") ?? GetProp(r, "name") ?? dn;
                                var finding = AnalyzeDirectoryObject(dn, label, sidSet, schemaNC, configNC);
                                if (finding != null && processedDns.Add(finding.DistinguishedName))
                                {
                                    findings.Add(finding);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint("    [!] LDAP sampling failed: " + ex.Message);
                }

                if (findings.Count == 0)
                {
                    Beaprint.GrayPrint("  [-] No impactful ACLs detected for the current principal (sampled set).");
                    return;
                }

                var ordered = findings
                    .OrderByDescending(f => f.MaxScore)
                    .ThenBy(f => f.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var truncated = ordered.Count > MaxFindingsToPrint;
                if (truncated)
                {
                    ordered = ordered.Take(MaxFindingsToPrint).ToList();
                }

                Beaprint.GrayPrint($"  [+] Found {findings.Count} object(s) where your principal has abuse-friendly rights:");
                foreach (var finding in ordered)
                {
                    Beaprint.BadPrint($"    -> {finding.DisplayName} ({finding.ClassName})");
                    Beaprint.GrayPrint("       DN: " + finding.DistinguishedName);
                    foreach (var impact in finding.Impacts.OrderByDescending(i => i.Score))
                    {
                        Beaprint.GrayPrint($"       * {impact.Impact}: {impact.Detail}");
                    }
                }

                if (truncated)
                {
                    Beaprint.GrayPrint($"  [!] Additional {findings.Count - MaxFindingsToPrint} object(s) not shown (enable domain mode or run winPEAS with more time to enumerate all objects).");
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
                    return EvaluateSecurity(entry, label ?? dn, sidSet, schemaNC, configNC);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static AdObjectFinding EvaluateSecurity(DirectoryEntry entry, string label, HashSet<string> sidSet, string schemaNC, string configNC)
        {
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
                        Impact = "Object owner",
                        Detail = "You own this object and can rewrite its ACL to grant full control.",
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
                if (rule == null || rule.AccessControlType != AccessControlType.Allow)
                {
                    continue;
                }

                if (!(rule.IdentityReference is SecurityIdentifier sid))
                {
                    continue;
                }

                if (!sidSet.Contains(sid.Value))
                {
                    continue;
                }

                foreach (var impact in MapRuleToImpacts(rule, schemaNC, configNC))
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

            return finding.Impacts.Count > 0 ? finding : null;
        }

        private static IEnumerable<AdAccessImpact> MapRuleToImpacts(ActiveDirectoryAccessRule rule, string schemaNC, string configNC)
        {
            var impacts = new List<AdAccessImpact>();
            var rights = rule.ActiveDirectoryRights;

            if ((rights & ActiveDirectoryRights.GenericAll) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "GenericAll",
                    Detail = "Full control -> reset password, add group members, edit SPNs/UAC, change ACLs.",
                    Score = 5
                });
                return impacts;
            }

            if ((rights & ActiveDirectoryRights.GenericWrite) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "GenericWrite",
                    Detail = "Can modify most attributes (logon scripts, SPNs, UAC, etc.).",
                    Score = 4
                });
            }

            if ((rights & ActiveDirectoryRights.WriteDacl) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "WriteDACL",
                    Detail = "Can edit the ACL to grant yourself additional rights/persistence.",
                    Score = 4
                });
            }

            if ((rights & ActiveDirectoryRights.WriteOwner) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "WriteOwner",
                    Detail = "Can take ownership and then modify the DACL.",
                    Score = 3
                });
            }

            if ((rights & ActiveDirectoryRights.CreateChild) != 0)
            {
                impacts.Add(new AdAccessImpact
                {
                    Impact = "CreateChild",
                    Detail = "Can create new users/computers/groups under this container (great for planting attack principals).",
                    Score = 3
                });
            }

            if ((rights & ActiveDirectoryRights.ExtendedRight) != 0)
            {
                var extImpact = MapExtendedRightImpact(rule.ObjectType, schemaNC, configNC);
                if (extImpact != null)
                {
                    impacts.Add(extImpact);
                }
            }

            if ((rights & ActiveDirectoryRights.WriteProperty) != 0)
            {
                var attrImpact = MapAttributeWriteImpact(rule.ObjectType, schemaNC, configNC, false);
                if (attrImpact != null)
                {
                    impacts.Add(attrImpact);
                }
            }

            if ((rights & ActiveDirectoryRights.Self) != 0)
            {
                var validatedImpact = MapAttributeWriteImpact(rule.ObjectType, schemaNC, configNC, true);
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
                    Detail = "Can reset the target account password without knowing the current value.",
                    Score = 5
                };
            }

            if (name.Contains("replicating directory changes"))
            {
                return new AdAccessImpact
                {
                    Impact = "Replication (DCSync)",
                    Detail = "Has replication rights (part of DCSync privilege to dump NTDS hashes).",
                    Score = name.Contains("filtered") ? 5 : 4
                };
            }

            return null;
        }

        private static AdAccessImpact MapAttributeWriteImpact(Guid objectType, string schemaNC, string configNC, bool validatedWrite)
        {
            if (objectType == Guid.Empty)
            {
                return new AdAccessImpact
                {
                    Impact = validatedWrite ? "Validated write (broad)" : "WriteProperty (broad)",
                    Detail = "ACE applies to most attributes. Consider SPN/UAC/sidHistory abuse paths.",
                    Score = 3
                };
            }

            var attributeName = GetGuidFriendlyName(objectType, schemaNC, configNC);
            if (string.IsNullOrEmpty(attributeName))
            {
                return null;
            }

            var lower = attributeName.ToLowerInvariant();

            if (lower.Contains("member"))
            {
                return new AdAccessImpact
                {
                    Impact = "Group membership control",
                    Detail = "Can edit the 'member' attribute -> add principals to this group.",
                    Score = 5
                };
            }

            if (lower.Contains("serviceprincipalname") || lower.Contains("validated-spn"))
            {
                return new AdAccessImpact
                {
                    Impact = "SPN control",
                    Detail = "Can set servicePrincipalName -> Kerberoast or constrained delegation abuse.",
                    Score = 4
                };
            }

            if (lower.Contains("useraccountcontrol"))
            {
                return new AdAccessImpact
                {
                    Impact = "UAC control",
                    Detail = "Can toggle UserAccountControl bits (AS-REP roastable, delegation, unconstrained).",
                    Score = 4
                };
            }

            if (lower.Contains("msds-allowedtoactonbehalfofotheridentity"))
            {
                return new AdAccessImpact
                {
                    Impact = "RBCD control",
                    Detail = "Can edit msDS-AllowedToActOnBehalfOfOtherIdentity -> configure Resource-Based Constrained Delegation.",
                    Score = 5
                };
            }

            if (lower.Contains("msds-allowedtodelegateto"))
            {
                return new AdAccessImpact
                {
                    Impact = "Delegation target control",
                    Detail = "Can edit msDS-AllowedToDelegateTo -> establish constrained delegation paths.",
                    Score = 4
                };
            }

            if (lower.Contains("sidhistory"))
            {
                return new AdAccessImpact
                {
                    Impact = "sidHistory control",
                    Detail = "Can add privileged SIDs into sidHistory for stealth escalation/persistence.",
                    Score = 4
                };
            }

            if (lower.Contains("unicodepwd") || lower.Contains("userpassword"))
            {
                return new AdAccessImpact
                {
                    Impact = "Password write",
                    Detail = "Can directly set unicodePwd/userPassword -> immediate account takeover.",
                    Score = 5
                };
            }

            return null;
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
            public string DisplayName { get; set; }
            public string DistinguishedName { get; set; }
            public string ClassName { get; set; }
            public List<AdAccessImpact> Impacts { get; } = new List<AdAccessImpact>();
            public int MaxScore => Impacts.Count == 0 ? 0 : Impacts.Max(i => i.Score);
        }

        private class AdAccessImpact
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

        // Detect gMSA objects where the current principal (or one of its groups) can retrieve the managed password
        private void PrintGmsaReadableByCurrentPrincipal()
        {
            try
            {
                Beaprint.MainPrint("gMSA readable managed passwords", "T1003");
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
                {
                    Beaprint.GrayPrint("  [-] Current token SIDs unavailable; gMSA access status UNKNOWN.");
                    return;
                }
                int total = 0, candidates = 0, unknown = 0, denied = 0;
                bool sampleTruncated = false;

                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                using (var ds = new DirectorySearcher(baseDe))
                {
                    ds.PageSize = 0;
                    ds.SizeLimit = GmsaSampleLimit + 1;
                    ds.ClientTimeout = GmsaSearchTimeout;
                    ds.ServerTimeLimit = GmsaSearchTimeout;
                    ds.Filter = "(&(objectClass=msDS-GroupManagedServiceAccount))";
                    ds.PropertiesToLoad.Add("sAMAccountName");
                    ds.PropertiesToLoad.Add("distinguishedName");
                    ds.PropertiesToLoad.Add("msDS-GroupMSAMembership");

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
                            var status = AssessGmsaMembership(descriptorBytes, currentSidSet);

                            if (status == GmsaAccessStatus.Candidate)
                            {
                                candidates++;
                                if (candidates <= MaxFindingsToPrint)
                                    Beaprint.BadPrint($"  Managed password access candidate for gMSA: {name}  (DN: {dn})");
                            }
                            else if (status == GmsaAccessStatus.Unknown)
                                unknown++;
                            else if (status == GmsaAccessStatus.Denied)
                                denied++;
                        }
                    }
                }

                Beaprint.GrayPrint($"  [*] Checked {total} gMSA(s): {candidates} access candidate(s), {denied} matching deny(s), {unknown} unknown descriptor(s). Effective access requires verification.");
                if (candidates > MaxFindingsToPrint)
                    Beaprint.GrayPrint($"  [*] {candidates - MaxFindingsToPrint} additional candidate(s) omitted.");
                if (sampleTruncated)
                    Beaprint.GrayPrint($"  [*] gMSA sample limited to {GmsaSampleLimit}; other accounts were not checked.");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
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
                    uint? strongBinding = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Services\Kdc", "StrongCertificateBindingEnforcement");
                    switch (strongBinding)
                    {
                        case 0: 
                            Beaprint.BadPrint("  StrongCertificateBindingEnforcement: 0 — Weak mapping allowed, vulnerable to ESC9.");
                            break;
                        case 2: 
                            Beaprint.GoodPrint("  StrongCertificateBindingEnforcement: 2 — Prevents weak UPN/DNS mappings even if SID extension missing, not vulnerable to ESC9.");
                            break;
                        // 1 is default behavior now I think?
                        case 1:
                        default: 
                            Beaprint.NoColorPrint($"  StrongCertificateBindingEnforcement: {strongBinding} — Allow weak mapping if SID extension missing, may be vulnerable to ESC9.");
                            break;

                    }  

                    uint? certMapping = RegistryHelper.GetDwordValue("HKLM", @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL", "CertificateMappingMethods");
                    if (certMapping.HasValue && (certMapping & 0x4) != 0)
                        Beaprint.BadPrint($"  CertificateMappingMethods: {certMapping} — Allow UPN-based mapping, vulnerable to ESC10.");
                    else if(certMapping.HasValue && ((certMapping & 0x1) != 0 || (certMapping & 0x2) != 0))
                        Beaprint.NoColorPrint($"  CertificateMappingMethods: {certMapping} — Allow weak Subject/Issuer certificate mapping.");
                    // 0x18 (strong mapping) is default behavior if not the flags above I think?
                    else
                        Beaprint.GoodPrint($"  CertificateMappingMethods: {certMapping} — Strong Certificate mapping enabled.");

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

                // Detect AD CS certificate templates where current principal has dangerous control rights(ESC4 - style)
                Beaprint.InfoPrint("\nIf you can modify a template (WriteDacl/WriteOwner/GenericAll), you can abuse ESC4");
                var configNC = GetRootDseProp("configurationNamingContext");
                if (string.IsNullOrEmpty(configNC))
                {
                    Beaprint.GrayPrint("  [-] Could not resolve configurationNamingContext.");
                    return;
                }

                var currentSidSet = GetCurrentSidSet();
                int checkedTemplates = 0;
                int vulnerable = 0;

                var templatesDn = $"LDAP://CN=Certificate Templates,CN=Public Key Services,CN=Services,{configNC}";

                using (var deBase = new DirectoryEntry(templatesDn))
                using (var ds = new DirectorySearcher(deBase))
                {
                    ds.PageSize = 300;
                    ds.Filter = "(objectClass=pKICertificateTemplate)";
                    ds.PropertiesToLoad.Add("cn");

                    foreach (SearchResult r in ds.FindAll())
                    {
                        checkedTemplates++;
                        string templateCn = GetProp(r, "cn") ?? "<unknown>";

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
                            continue;
                        }

                        try
                        {
                            var sd = de.ObjectSecurity; // ActiveDirectorySecurity
                            var rules = sd.GetAccessRules(true, true, typeof(SecurityIdentifier));
                            bool hit = false;
                            var hitRights = new HashSet<string>();

                            foreach (ActiveDirectoryAccessRule rule in rules)
                            {
                                if (rule.AccessControlType != AccessControlType.Allow) continue;
                                var sid = (rule.IdentityReference as SecurityIdentifier)?.Value;
                                if (string.IsNullOrEmpty(sid)) continue;
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
                    }
                }

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
                    ds.PageSize = 100;
                    ds.SizeLimit = searchLimit;
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
