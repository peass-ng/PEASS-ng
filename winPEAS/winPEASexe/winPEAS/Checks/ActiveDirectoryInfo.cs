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

namespace winPEAS.Checks
{
    // Lightweight AD-oriented checks for common escalation paths (gMSA readable password, AD CS template control)
    internal class ActiveDirectoryInfo : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Active Directory Quick Checks");

            new List<Action>
            {
                PrintGmsaReadableByCurrentPrincipal,
                PrintKerberoastableServiceAccounts,
                PrintAdObjectControlPaths,
                PrintAdcsMisconfigurations
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private const int SampleObjectLimit = 120;
        private const int MaxFindingsToPrint = 40;
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

        // Highlight objects where the current principal already has useful write/control rights
        private void PrintAdObjectControlPaths()
        {
            try
            {
                Beaprint.MainPrint("AD object control surfaces");
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

        // Detect gMSA objects where the current principal (or one of its groups) can retrieve the managed password
        private void PrintGmsaReadableByCurrentPrincipal()
        {
            try
            {
                Beaprint.MainPrint("gMSA readable managed passwords");
                Beaprint.LinkPrint(
                    "https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/gmsa.html",
                    "Look for Group Managed Service Accounts you can read (msDS-ManagedPassword)");

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
                int total = 0, readable = 0;

                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNC))
                using (var ds = new DirectorySearcher(baseDe))
                {
                    ds.PageSize = 300;
                    ds.Filter = "(&(objectClass=msDS-GroupManagedServiceAccount))";
                    ds.PropertiesToLoad.Add("sAMAccountName");
                    ds.PropertiesToLoad.Add("distinguishedName");
                    // Who can read the managed password
                    ds.PropertiesToLoad.Add("PrincipalsAllowedToRetrieveManagedPassword");

                    foreach (SearchResult r in ds.FindAll())
                    {
                        total++;
                        var name = GetProp(r, "sAMAccountName") ?? GetProp(r, "distinguishedName") ?? "<unknown>";
                        var dn = GetProp(r, "distinguishedName") ?? "";

                        bool canRead = false;
                        // Attribute may be absent or empty
                        var allowedDns = r.Properties["principalsallowedtoretrievemanagedpassword"];
                        if (allowedDns != null)
                        {
                            foreach (var val in allowedDns)
                            {
                                try
                                {
                                    using (var de = new DirectoryEntry("LDAP://" + val.ToString()))
                                    {
                                        var sidObj = de.Properties["objectSid"]?.Value as byte[];
                                        if (sidObj == null) continue;
                                        var sid = new SecurityIdentifier(sidObj, 0).Value;
                                        if (currentSidSet.Contains(sid))
                                        {
                                            canRead = true;
                                        }
                                    }
                                }
                                catch { /* ignore DN resolution issues */ }
                            }
                        }

                        if (canRead)
                        {
                            readable++;
                            Beaprint.BadPrint($"  You can retrieve managed password for gMSA: {name}  (DN: {dn})");
                        }
                    }
                }

                if (readable == 0)
                {
                    Beaprint.GrayPrint($"  [-] No gMSA with readable managed password found (checked {total}).");
                }
                else
                {
                    Beaprint.GrayPrint($"  [*] Hint: If such gMSA is member of Builtin\\Remote Management Users on a target, WinRM may be allowed.");
                }
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
                Beaprint.MainPrint("Kerberoasting / service ticket risks");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/kerberoast.html",
                    "Enumerate weak SPN accounts and legacy Kerberos crypto");

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
                Beaprint.MainPrint("AD CS misconfigurations for ESC");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/ad-certificates.html");
    
                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                    return;
                }

                Beaprint.InfoPrint("Check for ADCS misconfigurations in the local DC registry");
                bool IsDomainController = RegistryHelper.GetReg("HKLM", @"SYSTEM\CurrentControlSet\Services\NTDS")?.ValueCount > 0;
                if (IsDomainController)
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

                    // We take the Active CA, can they be several?
                    string caName = RegistryHelper.GetRegValue("HKLM", $@"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration", "Active");
                    if (!string.IsNullOrWhiteSpace(caName))
                    {
                        // Obscure Source for InterfaceFlag Enum:
                        // https://www.sysadmins.lv/apidocs/pki/html/T_PKI_CertificateServices_Flags_InterfaceFlagEnum.htm
                        uint? interfaceFlags = RegistryHelper.GetDwordValue("HKLM", $@"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration\{caName}", "InterfaceFlags");
                        if (!interfaceFlags.HasValue || (interfaceFlags & 512) == 0)
                            Beaprint.BadPrint("  IF_ENFORCEENCRYPTICERTREQUEST not set in InterfaceFlags — vulnerable to ESC11.");
                        else
                            Beaprint.GoodPrint("  IF_ENFORCEENCRYPTICERTREQUEST set in InterfaceFlags — not vulnerable to ESC11.");

                        string policyModule = RegistryHelper.GetRegValue("HKLM", $@"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration\{caName}\PolicyModules", "Active");
                        if (!string.IsNullOrWhiteSpace(policyModule))
                        {
                            string disableExtensionList = RegistryHelper.GetRegValue("HKLM", $@"SYSTEM\CurrentControlSet\Services\CertSvc\Configuration\{caName}\PolicyModules\{policyModule}", "DisableExtensionList");
                            // zOID_NTDS_CA_SECURITY_EXT (OID 1.3.6.1.4.1.311.25.2) 
                            if (disableExtensionList?.Contains("1.3.6.1.4.1.311.25.2") == true)
                                Beaprint.BadPrint("  szOID_NTDS_CA_SECURITY_EXT disabled for the entire CA — vulnerable to ESC16.");
                            else
                                Beaprint.GoodPrint("  szOID_NTDS_CA_SECURITY_EXT not disabled for the CA — not vulnerable to ESC16.");
                        }
                        else
                        {
                            Beaprint.GrayPrint("  [-] Policy Module not found. Skipping.");
                        }
                    }
                    else
                    {
                        Beaprint.GrayPrint("  [-] Certificate Authority not found. Skipping.");
                    }
                }
                else
                {
                    Beaprint.GrayPrint("  [-] Host is not a domain controller. Skipping ADCS Registry check");
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
            int checkedAccounts = 0;
            int highTotal = 0;
            int mediumTotal = 0;
            var high = new List<KerberoastCandidate>();
            var medium = new List<KerberoastCandidate>();

            try
            {
                using (var baseDe = new DirectoryEntry("LDAP://" + defaultNc))
                using (var ds = new DirectorySearcher(baseDe))
                {
                    ds.PageSize = 500;
                    ds.Filter = "(servicePrincipalName=*)";
                    ds.PropertiesToLoad.Add("sAMAccountName");
                    ds.PropertiesToLoad.Add("displayName");
                    ds.PropertiesToLoad.Add("distinguishedName");
                    ds.PropertiesToLoad.Add("servicePrincipalName");
                    ds.PropertiesToLoad.Add("msDS-SupportedEncryptionTypes");
                    ds.PropertiesToLoad.Add("userAccountControl");
                    ds.PropertiesToLoad.Add("pwdLastSet");
                    ds.PropertiesToLoad.Add("memberOf");
                    ds.PropertiesToLoad.Add("objectClass");

                    foreach (SearchResult r in ds.FindAll())
                    {
                        checkedAccounts++;
                        var candidate = BuildKerberoastCandidate(r);
                        if (candidate == null)
                        {
                            continue;
                        }

                        if (candidate.IsHighRisk)
                        {
                            highTotal++;
                            if (high.Count < 15) high.Add(candidate);
                        }
                        else
                        {
                            mediumTotal++;
                            if (medium.Count < 12) medium.Add(candidate);
                        }
                    }
                }

                Beaprint.InfoPrint($"Checked {checkedAccounts} SPN-bearing accounts. High-risk RC4/privileged targets: {highTotal}, long-lived AES-only targets: {mediumTotal}.");

                if (highTotal == 0 && mediumTotal == 0)
                {
                    Beaprint.GoodPrint("  No obvious Kerberoastable service accounts detected with current visibility.");
                    return;
                }

                if (high.Count > 0)
                {
                    Beaprint.BadPrint("  [!] RC4-enabled or privileged SPN accounts:");
                    foreach (var c in high)
                    {
                        Beaprint.ColorPrint($"      - {c.Label} | SPNs: {c.SpnSummary} | Enc: {c.Encryption} | {c.Reason}", Beaprint.LRED);
                    }
                    if (highTotal > high.Count)
                    {
                        Beaprint.GrayPrint($"      ... {highTotal - high.Count} additional high-risk accounts omitted.");
                    }
                }

                if (medium.Count > 0)
                {
                    Beaprint.ColorPrint("  [~] Long-lived SPN accounts (still Kerberoastable via AES tickets):", Beaprint.YELLOW);
                    foreach (var c in medium)
                    {
                        Beaprint.ColorPrint($"      - {c.Label} | SPNs: {c.SpnSummary} | Enc: {c.Encryption} | {c.Reason}", Beaprint.YELLOW);
                    }
                    if (mediumTotal > medium.Count)
                    {
                        Beaprint.GrayPrint($"      ... {mediumTotal - medium.Count} additional medium-risk accounts omitted.");
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("  [-] LDAP error while enumerating SPNs: " + ex.Message);
            }
        }

        private KerberoastCandidate BuildKerberoastCandidate(SearchResult r)
        {
            var sam = GetProp(r, "sAMAccountName");
            var displayName = GetProp(r, "displayName");
            var dn = GetProp(r, "distinguishedName");

            if (IsComputerObject(r) || IsManagedServiceAccount(r))
                return null;

            var uac = GetIntProp(r, "userAccountControl");
            if (uac.HasValue && (uac.Value & 0x2) != 0)
                return null;

            var encValue = GetIntProp(r, "msDS-SupportedEncryptionTypes");
            bool rc4Allowed = IsRc4Allowed(encValue);
            bool aesPresent = HasAes(encValue);
            bool passwordNeverExpires = uac.HasValue && (uac.Value & 0x10000) != 0;
            DateTime? pwdLastSet = GetFileTimeProp(r, "pwdLastSet");
            bool stalePassword = pwdLastSet.HasValue && pwdLastSet.Value < DateTime.UtcNow.AddDays(-365);
            var privilegeHits = GetPrivilegedGroups(r);
            var reasons = new List<string>();

            if (rc4Allowed)
                reasons.Add("RC4 allowed");
            else if (!aesPresent)
                reasons.Add("No AES flag");
            if (passwordNeverExpires)
                reasons.Add("PasswordNeverExpires");
            if (stalePassword)
                reasons.Add("PwdLastSet " + pwdLastSet.Value.ToString("yyyy-MM-dd"));
            if (privilegeHits.Count > 0)
                reasons.Add("Privileged: " + string.Join("/", privilegeHits));

            if (reasons.Count == 0)
                return null;

            bool isHigh = rc4Allowed || privilegeHits.Count > 0;
            if (!isHigh && !(passwordNeverExpires || stalePassword))
                return null;

            var label = !string.IsNullOrEmpty(sam) ? sam : dn;
            if (!string.IsNullOrEmpty(displayName) && !string.Equals(displayName, sam, StringComparison.OrdinalIgnoreCase))
            {
                label = string.IsNullOrEmpty(sam) ? displayName : $"{sam} ({displayName})";
            }

            return new KerberoastCandidate
            {
                Label = label ?? "<unknown>",
                SpnSummary = BuildSpnSummary(r),
                Encryption = DescribeEncTypes(encValue),
                Reason = string.Join("; ", reasons),
                IsHighRisk = isHigh
            };
        }

        private static string BuildSpnSummary(SearchResult r)
        {
            if (!r.Properties.Contains("servicePrincipalName") || r.Properties["servicePrincipalName"].Count == 0)
                return "<none>";

            var values = r.Properties["servicePrincipalName"];
            var list = new List<string>();
            int limit = values.Count < 3 ? values.Count : 3;
            for (int i = 0; i < limit; i++)
            {
                var spn = values[i]?.ToString();
                if (!string.IsNullOrEmpty(spn))
                    list.Add(spn);
            }

            string summary = list.Count > 0 ? string.Join(", ", list) : "<none>";
            if (values.Count > limit)
                summary += $" (+{values.Count - limit} more)";
            return summary;
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
                return "Unspecified (inherits defaults / RC4 compatible)";

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
            public bool IsHighRisk { get; set; }
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
