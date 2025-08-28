using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Helpers;

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
                PrintAdcsEsc4LikeTemplates
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

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

        // Detect AD CS certificate templates where current principal has dangerous control rights (ESC4-style)
        private void PrintAdcsEsc4LikeTemplates()
        {
            try
            {
                Beaprint.MainPrint("AD CS templates with dangerous ACEs (ESC4)");
                Beaprint.LinkPrint(
                    "https://book.hacktricks.wiki/en/windows-hardening/active-directory-methodology/ad-certificates.html#esc4",
                    "If you can modify a template (WriteDacl/WriteOwner/GenericAll), you can abuse ESC4");

                if (!Checks.IsPartOfDomain)
                {
                    Beaprint.GrayPrint("  [-] Host is not domain-joined. Skipping.");
                    return;
                }

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
    }
}
