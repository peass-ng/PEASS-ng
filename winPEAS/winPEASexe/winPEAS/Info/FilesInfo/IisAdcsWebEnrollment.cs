using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace winPEAS.Info.FilesInfo
{
    internal sealed class IisAdcsWebEnrollmentFinding
    {
        internal string Site;
        internal string Path;
        internal bool HttpBinding;
        internal bool HttpsBinding;
        internal bool? WindowsAuthentication;
        internal bool NtlmDirect = true;
        internal bool NtlmViaNegotiate = true;
        internal bool NtlmProvidersUnknown;
        internal bool? NtlmProvider => NtlmProvidersUnknown ? (bool?)null :
            NtlmDirect || NtlmViaNegotiate;
        internal string EpaTokenChecking;
        internal bool? RequireSsl;
        internal bool FileLevelOverride;

        internal bool Candidate => WindowsAuthentication == true && NtlmProvider == true &&
            (EpaTokenChecking == "None" || EpaTokenChecking == "Allow") && !FileLevelOverride &&
            ((HttpBinding && RequireSsl == false) || HttpsBinding);
    }

    // Reuses the bounded, read-only applicationHost.config XML loaded by IisServedRootPermissions.
    // These are local configuration leads; web.config, shared configuration, proxies, template
    // enrollment rights, and runtime endpoint reachability are not established here.
    internal static class IisAdcsWebEnrollment
    {
        internal const int MaxSites = 64;
        internal const int MaxFindings = 8;

        internal static void Assess(IisServedRootReport report, XDocument config, Stopwatch timer)
        {
            if (report == null || config == null || config.Root == null || timer == null) return;
            XElement sites = config.Root.Element("system.applicationHost")?.Element("sites");
            if (sites == null) return;
            int visited = 0;
            foreach (XElement site in sites.Elements("site"))
            {
                if (++visited > MaxSites || timer.ElapsedMilliseconds >= IisServedRootPermissions.MaxMilliseconds)
                { report.LimitReached = true; return; }
                string siteName = (string)site.Attribute("name");
                if (string.IsNullOrWhiteSpace(siteName)) continue;
                bool http = site.Element("bindings")?.Elements("binding").Any(binding =>
                    string.Equals((string)binding.Attribute("protocol"), "http", StringComparison.OrdinalIgnoreCase)) == true;
                bool https = site.Element("bindings")?.Elements("binding").Any(binding =>
                    string.Equals((string)binding.Attribute("protocol"), "https", StringComparison.OrdinalIgnoreCase)) == true;
                foreach (XElement app in site.Elements("application"))
                {
                    if (timer.ElapsedMilliseconds >= IisServedRootPermissions.MaxMilliseconds)
                    { report.LimitReached = true; return; }
                    string appPath = (string)app.Attribute("path") ?? "/";
                    if (IsCertSrv(appPath)) Add(report, config, siteName, appPath, http, https);
                    foreach (XElement vdir in app.Elements("virtualDirectory"))
                    {
                        string path = JoinWebPath(appPath, (string)vdir.Attribute("path"));
                        if (IsCertSrv(path)) Add(report, config, siteName, path, http, https);
                        if (report.AdcsWebEnrollment.Count >= MaxFindings)
                        { report.LimitReached = true; return; }
                    }
                }
            }
        }

        private static bool IsCertSrv(string path)
        {
            return string.Equals(path?.TrimEnd('/'), "/certsrv", StringComparison.OrdinalIgnoreCase);
        }

        private static string JoinWebPath(string app, string vdir)
        {
            if (string.IsNullOrEmpty(vdir) || vdir == "/") return app;
            return (app ?? "/").TrimEnd('/') + (vdir.StartsWith("/", StringComparison.Ordinal) ? vdir : "/" + vdir);
        }

        private static void Add(IisServedRootReport report, XDocument config, string site, string path,
            bool http, bool https)
        {
            if (report.AdcsWebEnrollment.Count >= MaxFindings || report.AdcsWebEnrollment.Any(f =>
                string.Equals(f.Site, site, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase))) return;
            var finding = new IisAdcsWebEnrollmentFinding
            {
                Site = Clean(site), Path = "/certsrv", HttpBinding = http, HttpsBinding = https,
                EpaTokenChecking = "None", RequireSsl = false
            };
            // IIS configuration inherits from server to site to application. Apply each
            // level explicitly; XML document order need not match precedence.
            ReadSettings(config.Root.Element("system.webServer"), finding);
            foreach (XElement location in config.Root.Elements("location").Where(l =>
                string.IsNullOrEmpty(((string)l.Attribute("path"))?.Trim('/'))))
                ReadSettings(location.Element("system.webServer"), finding);
            string sitePath = site.Trim('/');
            string appPath = sitePath + "/certsrv";
            foreach (string level in new[] { sitePath, appPath })
                foreach (XElement location in config.Root.Elements("location").Where(l =>
                    string.Equals(((string)l.Attribute("path"))?.Trim('/'), level,
                        StringComparison.OrdinalIgnoreCase)))
                    ReadSettings(location.Element("system.webServer"), finding);
            finding.FileLevelOverride = config.Root.Elements("location").Any(l =>
                (((string)l.Attribute("path")) ?? "").Trim('/').StartsWith(appPath + "/",
                    StringComparison.OrdinalIgnoreCase));
            report.AdcsWebEnrollment.Add(finding);
        }

        private static void ReadSettings(XElement webServer, IisAdcsWebEnrollmentFinding finding)
        {
            XElement security = webServer?.Element("security");
            XElement authentication = security?.Element("authentication")?.Element("windowsAuthentication");
            if (authentication != null)
            {
                string enabled = (string)authentication.Attribute("enabled");
                if (bool.TryParse(enabled, out bool parsedEnabled)) finding.WindowsAuthentication = parsedEnabled;
                else if (enabled != null) finding.WindowsAuthentication = null;
                XElement protection = authentication.Element("extendedProtection");
                string checking = (string)protection?.Attribute("tokenChecking");
                if (checking != null)
                    finding.EpaTokenChecking = new[] { "None", "Allow", "Require" }.FirstOrDefault(v =>
                        string.Equals(v, checking, StringComparison.OrdinalIgnoreCase)) ?? "Unknown";
                XElement providers = authentication.Element("providers");
                if (providers != null)
                {
                    foreach (XElement operation in providers.Elements())
                    {
                        string name = operation.Name.LocalName;
                        string value = (string)operation.Attribute("value");
                        if (name == "clear")
                        {
                            finding.NtlmDirect = false;
                            finding.NtlmViaNegotiate = false;
                            finding.NtlmProvidersUnknown = false;
                        }
                        else if (name == "add" || name == "remove")
                        {
                            bool added = name == "add";
                            if (string.Equals(value, "NTLM", StringComparison.OrdinalIgnoreCase))
                                finding.NtlmDirect = added;
                            else if (string.Equals(value, "Negotiate", StringComparison.OrdinalIgnoreCase))
                                finding.NtlmViaNegotiate = added;
                            else if (!string.Equals(value, "Negotiate:Kerberos", StringComparison.OrdinalIgnoreCase))
                                finding.NtlmProvidersUnknown = true;
                        }
                        else finding.NtlmProvidersUnknown = true;
                    }
                }
            }
            XElement access = security?.Element("access");
            string flags = (string)access?.Attribute("sslFlags");
            if (flags != null)
            {
                string[] values = flags.Split(',').Select(value => value.Trim()).ToArray();
                finding.RequireSsl = values.Any(value => string.Equals(value, "Ssl", StringComparison.OrdinalIgnoreCase))
                    ? (bool?)true : values.All(value => value == "" ||
                        string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)) ? (bool?)false : null;
            }
        }

        private static string Clean(string text)
        {
            return new string(text.Take(80).Select(c => char.IsControl(c) ? ' ' : c).ToArray());
        }
    }
}
