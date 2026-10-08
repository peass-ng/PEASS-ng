using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

namespace winPEAS.Info.FilesInfo
{
    internal enum IisCreateFileAcl { NoMatch, Denied, Indicated, ManualReview }

    internal sealed class IisServedRoot
    {
        internal string Path;
        internal string Site;
        internal string Application;
        internal string Pool;
        internal bool Configured;
        internal bool AutoStartConfigured;
        internal bool AspxHandlerConfigured;
        internal IisCreateFileAcl CreateFileAcl;
        internal string Trustee;
        internal string Reason;
    }

    internal sealed class IisServedRootReport
    {
        internal readonly List<IisServedRoot> Roots = new List<IisServedRoot>();
        internal bool ConfigReadable;
        internal bool LimitReached;
        internal string Note;
    }

    // Reads a small portion of applicationHost.config and directory ACLs. No content traversal or write probe.
    internal static class IisServedRootPermissions
    {
        internal const int MaxRoots = 32;
        internal const int MaxConfigBytes = 1024 * 1024;
        internal const int MaxMilliseconds = 2000;
        private const int FileAddFile = 0x2; // FILE_ADD_FILE, distinct from FILE_ADD_SUBDIRECTORY (0x4).

        internal static IisServedRootReport Scan()
        {
            var report = new IisServedRootReport();
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || !IisInstalled()) return report;
            var timer = Stopwatch.StartNew();
            string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"System32\inetsrv\config\applicationHost.config");
            try
            {
                using (var stream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (stream.Length > MaxConfigBytes) throw new InvalidDataException("IIS configuration exceeds size limit");
                    var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null, MaxCharactersInDocument = MaxConfigBytes };
                    using (var reader = XmlReader.Create(stream, settings))
                    {
                        XDocument config = XDocument.Load(reader);
                        report.ConfigReadable = true;
                        AddConfiguredRoots(report, config, timer);
                    }
                }
            }
            catch (Exception)
            {
                report.Note = "IIS configuration unavailable or over limit; the default path is only a candidate.";
            }
            if (!report.LimitReached && report.Roots.Count < MaxRoots)
            {
                string drive = Environment.GetEnvironmentVariable("SystemDrive");
                string candidate = NormalizeLocalPath((drive ?? "C:") + @"\inetpub\wwwroot");
                if (candidate != null && !report.Roots.Any(r => string.Equals(r.Path, candidate, StringComparison.OrdinalIgnoreCase)) &&
                    Directory.Exists(candidate))
                    report.Roots.Add(new IisServedRoot { Path = candidate, Configured = false });
            }
            foreach (IisServedRoot root in report.Roots)
            {
                if (timer.ElapsedMilliseconds >= MaxMilliseconds) { report.LimitReached = true; break; }
                AssessRoot(root);
            }
            return report;
        }

        private static bool IisInstalled()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\W3SVC"))
                    return key != null;
            }
            catch (Exception) { return false; }
        }

        internal static void AddConfiguredRoots(IisServedRootReport report, XDocument config, Stopwatch timer)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            XElement configuration = config.Root;
            XElement host = configuration == null ? null : configuration.Element("system.applicationHost");
            XElement sites = host == null ? null : host.Element("sites");
            if (sites == null) { report.Note = "No IIS site mapping was readable."; return; }
            bool aspx = HasGlobalAspxHandler(config);
            // Prefer auto-start sites under the path cap, but a site configured not to
            // auto-start may still be started manually and must remain visible.
            foreach (XElement site in sites.Elements("site").OrderBy(s =>
                string.Equals((string)s.Attribute("serverAutoStart"), "false", StringComparison.OrdinalIgnoreCase) ? 1 : 0))
            {
                if (timer.ElapsedMilliseconds >= MaxMilliseconds) { report.LimitReached = true; break; }
                if (site.Element("bindings") == null || !site.Element("bindings").Elements("binding").Any()) continue;
                bool autoStart = !string.Equals((string)site.Attribute("serverAutoStart"), "false", StringComparison.OrdinalIgnoreCase);
                foreach (XElement app in site.Elements("application"))
                {
                    XElement defaults = sites.Element("applicationDefaults");
                    string pool = (string)app.Attribute("applicationPool") ??
                        (defaults == null ? null : (string)defaults.Attribute("applicationPool"));
                    foreach (XElement virtualDirectory in app.Elements("virtualDirectory"))
                    {
                        if (timer.ElapsedMilliseconds >= MaxMilliseconds || report.Roots.Count >= MaxRoots)
                        { report.LimitReached = true; return; }
                        string path = NormalizeLocalPath((string)virtualDirectory.Attribute("physicalPath"));
                        if (path == null || !seen.Add(path)) continue;
                        report.Roots.Add(new IisServedRoot { Path = path, Site = (string)site.Attribute("name"),
                            Application = (string)app.Attribute("path"), Pool = pool, Configured = true,
                            AutoStartConfigured = autoStart,
                            AspxHandlerConfigured = aspx });
                    }
                }
            }
        }

        private static bool HasGlobalAspxHandler(XDocument document)
        {
            XElement root = document.Root;
            XElement webServer = root == null ? null : root.Element("system.webServer");
            XElement handlers = webServer == null ? null : webServer.Element("handlers");
            if (handlers == null || handlers.Elements().Any(e => e.Name.LocalName == "clear" || e.Name.LocalName == "remove") ||
                root.Elements("location").Any(e => e.Descendants("handlers").Any())) return false;
            string policy = (string)handlers.Attribute("accessPolicy") ?? "Read,Script";
            if (!policy.Split(',').Any(p => string.Equals(p.Trim(), "Script", StringComparison.OrdinalIgnoreCase))) return false;
            return handlers.Elements("add").Any(e =>
                string.Equals((string)e.Attribute("path"), "*.aspx", StringComparison.OrdinalIgnoreCase) &&
                string.Equals((string)e.Attribute("modules"), "ManagedPipelineHandler", StringComparison.OrdinalIgnoreCase) &&
                ((string)e.Attribute("type") ?? "").IndexOf("System.Web.UI.PageHandlerFactory", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !string.Equals((string)e.Attribute("verb"), "HEAD", StringComparison.OrdinalIgnoreCase));
        }

        internal static string NormalizeLocalPath(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string expanded = Environment.ExpandEnvironmentVariables(raw.Trim());
            if (expanded.IndexOf('%') >= 0 || expanded.StartsWith(@"\\", StringComparison.Ordinal) ||
                expanded.StartsWith(@"//", StringComparison.Ordinal) ||
                expanded.Length < 3 || !char.IsLetter(expanded[0]) || expanded[1] != ':' ||
                (expanded[2] != '\\' && expanded[2] != '/')) return null;
            try
            {
                string full = Path.GetFullPath(expanded);
                return full.Length == Path.GetPathRoot(full).Length ? full : full.TrimEnd('\\', '/');
            }
            catch (Exception) { return null; }
        }

        private static void AssessRoot(IisServedRoot root)
        {
            root.CreateFileAcl = IisCreateFileAcl.ManualReview;
            if (!root.Configured) { root.Reason = "No active site mapping evidence"; return; }
            try
            {
                if (new DriveInfo(Path.GetPathRoot(root.Path)).DriveType != DriveType.Fixed)
                { root.Reason = "Physical path is not on a fixed local drive"; return; }
                // Reject junctions/symlinks in every component, including the physical root itself.
                string current = Path.GetPathRoot(root.Path);
                string remainder = root.Path.Substring(current.Length);
                foreach (string part in remainder.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    current = Path.Combine(current, part);
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    { root.Reason = "Reparse point in physical path"; return; }
                }
                if (!Directory.Exists(root.Path)) { root.Reason = "Physical root inaccessible"; return; }
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var deny = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (identity.User == null) { root.Reason = "Current token SID unavailable"; return; }
                    enabled.Add(identity.User.Value);
                    deny.Add(identity.User.Value);
                    var principal = new WindowsPrincipal(identity);
                    if (identity.Groups != null)
                        foreach (IdentityReference reference in identity.Groups)
                        {
                            var sid = reference as SecurityIdentifier;
                            if (sid == null) continue;
                            deny.Add(sid.Value); // Deny-only groups still participate in Deny ACEs.
                            if (principal.IsInRole(sid)) enabled.Add(sid.Value);
                        }
                    DirectorySecurity security = Directory.GetAccessControl(root.Path, AccessControlSections.Access);
                    string trustee;
                    root.CreateFileAcl = EvaluateCreateFileAcl(
                        new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0), enabled, deny, out trustee);
                    root.Trustee = trustee;
                    // A DACL Allow cannot establish effective access without the mandatory label,
                    // restricted-token state, and other policy. Do not claim it as verified access.
                    if (root.CreateFileAcl == IisCreateFileAcl.Indicated)
                    {
                        root.CreateFileAcl = IisCreateFileAcl.ManualReview;
                        root.Reason = "Create-file Allow indicated by ACL; token integrity/restrictions unverified";
                    }
                    else if (root.CreateFileAcl == IisCreateFileAcl.ManualReview)
                    {
                        root.Reason = "ACL evaluation inconclusive; review ACE ordering and token policy";
                    }
                }
            }
            catch (Exception) { root.Reason = "Physical root or ACL inaccessible"; }
        }

        internal static IisCreateFileAcl EvaluateCreateFileAcl(RawSecurityDescriptor descriptor,
            ISet<string> enabledSids, ISet<string> denySids, out string trustee)
        {
            trustee = null;
            if (descriptor == null || enabledSids == null || enabledSids.Count == 0 ||
                (descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) == 0 || descriptor.DiscretionaryAcl == null)
                return IisCreateFileAcl.ManualReview;
            bool allow = false;
            bool deny = false;
            foreach (GenericAce rawAce in descriptor.DiscretionaryAcl)
            {
                var ace = rawAce as QualifiedAce;
                if (ace == null || (ace.AceFlags & AceFlags.InheritOnly) != 0 || ace.SecurityIdentifier == null) continue;
                // Generic write/all map to FILE_ADD_FILE for directories. Unknown object ACEs stay unresolved.
                int mask = ace.AccessMask;
                bool createsFile = (mask & (FileAddFile | unchecked((int)0x40000000) | unchecked((int)0x10000000))) != 0;
                if (!createsFile) continue;
                if (ace.AceQualifier == AceQualifier.AccessDenied &&
                    denySids != null && denySids.Contains(ace.SecurityIdentifier.Value)) deny = true;
                if (ace.AceQualifier == AceQualifier.AccessAllowed && enabledSids.Contains(ace.SecurityIdentifier.Value))
                { allow = true; trustee = ace.SecurityIdentifier.Value; }
            }
            // ACL ordering and generic-rights mapping can affect conflicting ACEs.
            // Avoid claiming denial when both an Allow and a Deny match.
            if (allow && deny) return IisCreateFileAcl.ManualReview;
            if (deny) return IisCreateFileAcl.Denied;
            return allow ? IisCreateFileAcl.Indicated : IisCreateFileAcl.NoMatch;
        }
    }
}
