using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Info.NetworkInfo
{
    public sealed class HostCheckerResult
    {
        public string Hostname { get; set; }
        public string RawResponse { get; set; }
        public string HostOnlyResponse { get; set; }
        public string Error { get; set; }
    }

    public sealed class PackageVulnerabilitySummary
    {
        public int Checked { get; set; }
        public int Affected { get; set; }
        public int NotShown { get; set; }
        public string Error { get; set; }
        public List<string> Lines { get; } = new List<string>();
    }

    internal sealed class PackageVulnerabilityEntry
    {
        public int OriginalIndex { get; set; }
        public int SeverityPriority { get; set; }
        public double CvssScore { get; set; }
        public string Severity { get; set; }
        public string Line { get; set; }
        public List<string> VulnerabilityIds { get; } = new List<string>();
    }

    internal sealed class VulnerabilityCriticality
    {
        public int SeverityPriority { get; set; }
        public double CvssScore { get; set; }
        public string Severity { get; set; }
    }

    public static class HackTricksHostChecker
    {
        private const int TimeoutSeconds = 15;
        private const int OsvTimeoutSeconds = 12;
        private const int MaxOsvVulnerabilityLookups = 100;
        private const string DefaultUrl = "https://tools.hacktricks.wiki/api/host-checker";
        private const string OsvVulnerabilityUrl = "https://api.osv.dev/v1/vulns/";
        private static readonly object LockObj = new object();
        private static Task<HostCheckerResult> lookupTask;
        private static bool startedWithPackages;
        private static bool startedWithHostname;

        public static void Start(bool includeHostname, bool includePackages)
        {
            if (!includeHostname && !includePackages)
            {
                return;
            }

            lock (LockObj)
            {
                if (lookupTask != null)
                {
                    if ((includePackages && !startedWithPackages) || (includeHostname && !startedWithHostname))
                    {
                        lookupTask = Task.Run(() => ExecuteLookup(includeHostname || startedWithHostname, includePackages || startedWithPackages));
                        startedWithHostname = includeHostname || startedWithHostname;
                        startedWithPackages = includePackages || startedWithPackages;
                    }
                    return;
                }

                startedWithHostname = includeHostname;
                startedWithPackages = includePackages;
                lookupTask = Task.Run(() => ExecuteLookup(includeHostname, includePackages));
            }
        }

        public static HostCheckerResult GetResult()
        {
            EnsureStarted(Checks.Checks.CheckOnlineVulnPackages);
            try
            {
                return lookupTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return new HostCheckerResult { Error = $"Error during online HackTricks check: {ex.Message}" };
            }
        }

        public static PackageVulnerabilitySummary GetPackageVulnerabilities(int maxLines)
        {
            EnsureStarted(includePackages: true);
            var result = GetResult();
            if (!string.IsNullOrEmpty(result.Error))
            {
                return new PackageVulnerabilitySummary { Error = result.Error };
            }

            return ParsePackageVulnerabilities(
                result.RawResponse,
                maxLines,
                ResolveVulnerabilityCriticalitiesFromOsv);
        }

        public static PackageVulnerabilitySummary ParsePackageVulnerabilities(string responseJson, int maxLines)
        {
            return ParsePackageVulnerabilities(responseJson, maxLines, null);
        }

        private static PackageVulnerabilitySummary ParsePackageVulnerabilities(
            string responseJson,
            int maxLines,
            Func<IEnumerable<string>, Dictionary<string, VulnerabilityCriticality>> vulnerabilityCriticalityResolver)
        {
            var summary = new PackageVulnerabilitySummary();
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                summary.Error = "No response received from HackTricks online lookup.";
                return summary;
            }

            try
            {
                using (var doc = JsonDocument.Parse(responseJson))
                {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("package_vulnerabilities", out var packageVulns))
                    {
                        return summary;
                    }

                    summary.Checked = GetInt(packageVulns, "checked");
                    summary.Affected = GetInt(packageVulns, "affected");

                    if (packageVulns.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                    {
                        summary.Error = "Package vulnerability lookup failed: " + error.GetString();
                        return summary;
                    }

                    if (!packageVulns.TryGetProperty("vulnerable_packages", out var packages) ||
                        packages.ValueKind != JsonValueKind.Array)
                    {
                        return summary;
                    }

                    var entries = new List<PackageVulnerabilityEntry>();
                    var index = 0;
                    foreach (var package in packages.EnumerateArray())
                    {
                        var name = GetString(package, "name");
                        var version = GetString(package, "version");
                        var ecosystem = GetString(package, "ecosystem");
                        var manager = GetString(package, "manager");
                        var vulns = GetStringArray(package, "vulns");
                        if (string.IsNullOrWhiteSpace(name) || vulns.Count == 0)
                        {
                            continue;
                        }

                        var source = !string.IsNullOrWhiteSpace(ecosystem) ? ecosystem : manager;
                        var sourcePart = !string.IsNullOrWhiteSpace(source) ? $" [{source}]" : string.Empty;
                        var severity = GetPackageSeverity(package);
                        var severityPart = !string.IsNullOrWhiteSpace(severity) ? $" [{severity}]" : string.Empty;
                        var entry = new PackageVulnerabilityEntry
                        {
                            OriginalIndex = index,
                            Severity = severity,
                            SeverityPriority = GetSeverityPriority(severity),
                            CvssScore = GetPackageCvssScore(package),
                            Line = $"- {name} {version}{sourcePart}{severityPart}: {string.Join(", ", vulns)}".Trim()
                        };
                        entry.VulnerabilityIds.AddRange(vulns);
                        entries.Add(entry);
                        index++;
                    }

                    ApplyResolvedCriticalities(entries, vulnerabilityCriticalityResolver);

                    summary.Lines.AddRange(entries
                        .OrderByDescending(entry => entry.SeverityPriority)
                        .ThenByDescending(entry => entry.CvssScore)
                        .ThenBy(entry => entry.OriginalIndex)
                        .Take(Math.Max(0, maxLines))
                        .Select(entry => entry.Line));

                    summary.NotShown = Math.Max(0, summary.Affected - summary.Lines.Count);
                }
            }
            catch (Exception ex)
            {
                summary.Error = "Could not parse package vulnerability lookup response: " + ex.Message;
            }

            return summary;
        }

        private static void ApplyResolvedCriticalities(
            List<PackageVulnerabilityEntry> entries,
            Func<IEnumerable<string>, Dictionary<string, VulnerabilityCriticality>> vulnerabilityCriticalityResolver)
        {
            if (vulnerabilityCriticalityResolver == null || entries.Count == 0)
            {
                return;
            }

            var vulnerabilityIds = entries
                .SelectMany(entry => entry.VulnerabilityIds)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (vulnerabilityIds.Count == 0)
            {
                return;
            }

            Dictionary<string, VulnerabilityCriticality> criticalities;
            try
            {
                criticalities = vulnerabilityCriticalityResolver(vulnerabilityIds);
            }
            catch
            {
                return;
            }

            if (criticalities == null || criticalities.Count == 0)
            {
                return;
            }

            foreach (var entry in entries)
            {
                var resolved = entry.VulnerabilityIds
                    .Where(id => criticalities.ContainsKey(id))
                    .Select(id => criticalities[id])
                    .OrderByDescending(criticality => criticality.SeverityPriority)
                    .ThenByDescending(criticality => criticality.CvssScore)
                    .FirstOrDefault();
                if (resolved == null || resolved.SeverityPriority <= entry.SeverityPriority)
                {
                    continue;
                }

                entry.Severity = resolved.Severity;
                entry.SeverityPriority = resolved.SeverityPriority;
                entry.CvssScore = resolved.CvssScore;
                entry.Line = AddSeverityToLine(entry.Line, resolved.Severity);
            }
        }

        private static string AddSeverityToLine(string line, string severity)
        {
            severity = FormatSeverity(severity);
            if (string.IsNullOrWhiteSpace(severity) || string.IsNullOrWhiteSpace(line))
            {
                return line;
            }

            var separator = line.IndexOf(": ", StringComparison.Ordinal);
            if (separator < 0)
            {
                return line;
            }

            return line.Substring(0, separator) + $" [{severity}]" + line.Substring(separator);
        }

        private static Dictionary<string, VulnerabilityCriticality> ResolveVulnerabilityCriticalitiesFromOsv(IEnumerable<string> vulnerabilityIds)
        {
            var ids = vulnerabilityIds
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxOsvVulnerabilityLookups)
                .ToList();
            var results = new Dictionary<string, VulnerabilityCriticality>(StringComparer.OrdinalIgnoreCase);
            if (ids.Count == 0)
            {
                return results;
            }

            using (var httpClient = new HttpClient())
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(OsvTimeoutSeconds)))
            {
                httpClient.Timeout = TimeSpan.FromSeconds(OsvTimeoutSeconds);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("winpeas");
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var tasks = ids.Select(id => GetOsvCriticality(httpClient, cts.Token, id)).ToArray();
                Task.WaitAll(tasks, TimeSpan.FromSeconds(OsvTimeoutSeconds));
                foreach (var task in tasks)
                {
                    if (task.Status == TaskStatus.RanToCompletion && task.Result.Value != null)
                    {
                        results[task.Result.Key] = task.Result.Value;
                    }
                }
            }

            return results;
        }

        private static async Task<KeyValuePair<string, VulnerabilityCriticality>> GetOsvCriticality(
            HttpClient httpClient,
            CancellationToken cancellationToken,
            string vulnerabilityId)
        {
            try
            {
                var url = OsvVulnerabilityUrl + Uri.EscapeDataString(vulnerabilityId);
                var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new KeyValuePair<string, VulnerabilityCriticality>(vulnerabilityId, null);
                }

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using (var doc = JsonDocument.Parse(body))
                {
                    return new KeyValuePair<string, VulnerabilityCriticality>(
                        vulnerabilityId,
                        GetOsvCriticality(doc.RootElement));
                }
            }
            catch
            {
                return new KeyValuePair<string, VulnerabilityCriticality>(vulnerabilityId, null);
            }
        }

        private static VulnerabilityCriticality GetOsvCriticality(JsonElement root)
        {
            var best = new VulnerabilityCriticality();
            if (!root.TryGetProperty("severity", out var severities) || severities.ValueKind != JsonValueKind.Array)
            {
                return best;
            }

            foreach (var severity in severities.EnumerateArray())
            {
                var scoreText = GetString(severity, "score");
                var cvssScore = GetCvssScore(scoreText);
                var severityName = GetSeverityFromCvss(cvssScore);
                var priority = GetSeverityPriority(severityName);
                if (priority > best.SeverityPriority ||
                    (priority == best.SeverityPriority && cvssScore > best.CvssScore))
                {
                    best.Severity = severityName;
                    best.SeverityPriority = priority;
                    best.CvssScore = cvssScore;
                }
            }

            return best;
        }

        private static void EnsureStarted(bool includePackages)
        {
            Start(
                includeHostname: !Checks.Checks.DontCheckHostname,
                includePackages: includePackages);
        }

        private static HostCheckerResult ExecuteLookup(bool includeHostname, bool includePackages)
        {
            var result = new HostCheckerResult();
            try
            {
                var hostname = GetHostname();
                result.Hostname = hostname;

                var payload = new Dictionary<string, object>
                {
                    { "source", "winpeas" },
                    { "os", GetOsInfo() }
                };

                if (includeHostname && !string.IsNullOrWhiteSpace(hostname))
                {
                    payload["hostname"] = hostname;
                }

                if (includePackages)
                {
                    payload["packages"] = InstalledPackageInventory.GetInstalledPackages();
                }

                var url = Environment.GetEnvironmentVariable("HACKTRICKS_HOST_CHECKER_URL");
                if (string.IsNullOrWhiteSpace(url))
                {
                    url = DefaultUrl;
                }

                using (var httpClient = new HttpClient())
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds)))
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
                    var body = JsonSerializer.Serialize(payload);
                    var req = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                    req.Headers.UserAgent.ParseAdd("winpeas");
                    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var resp = httpClient.SendAsync(req, cts.Token).GetAwaiter().GetResult();
                    if (!resp.IsSuccessStatusCode)
                    {
                        result.Error = $"External check failed (HTTP {(int)resp.StatusCode})";
                        return result;
                    }

                    result.RawResponse = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    result.HostOnlyResponse = RemovePackageVulnerabilities(result.RawResponse);
                }
            }
            catch (Exception ex)
            {
                result.Error = $"Error during online HackTricks check: {ex.Message}";
            }

            return result;
        }

        private static string RemovePackageVulnerabilities(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return responseJson;
            }

            try
            {
                using (var doc = JsonDocument.Parse(responseJson))
                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        writer.WriteStartObject();
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            if (!string.Equals(property.Name, "package_vulnerabilities", StringComparison.OrdinalIgnoreCase))
                            {
                                property.WriteTo(writer);
                            }
                        }
                        writer.WriteEndObject();
                    }

                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch
            {
                return responseJson;
            }
        }

        private static Dictionary<string, object> GetOsInfo()
        {
            return new Dictionary<string, object>
            {
                { "id", "windows" },
                { "name", "Windows" },
                { "version_id", Environment.OSVersion.VersionString },
                { "kernel", new Dictionary<string, string>
                    {
                        { "release", Environment.OSVersion.Version.ToString() },
                        { "arch", Environment.Is64BitOperatingSystem ? "x64" : "x86" }
                    }
                }
            };
        }

        private static string GetHostname()
        {
            try
            {
                var hostname = Dns.GetHostName();
                return string.IsNullOrWhiteSpace(hostname) ? Environment.MachineName : hostname;
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        private static int GetInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
            {
                return value;
            }
            return 0;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
            return string.Empty;
        }

        private static string GetPackageSeverity(JsonElement package)
        {
            var severity = GetString(package, "severity");
            var priority = GetSeverityPriority(severity);

            foreach (var propertyName in new[] { "max_severity", "criticality", "risk", "level" })
            {
                var candidate = GetString(package, propertyName);
                var candidatePriority = GetSeverityPriority(candidate);
                if (candidatePriority > priority)
                {
                    severity = candidate;
                    priority = candidatePriority;
                }
            }

            if (package.TryGetProperty("severities", out var severities) && severities.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in severities.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var candidate = item.GetString();
                    var candidatePriority = GetSeverityPriority(candidate);
                    if (candidatePriority > priority)
                    {
                        severity = candidate;
                        priority = candidatePriority;
                    }
                }
            }

            if (package.TryGetProperty("vulns", out var vulns) && vulns.ValueKind == JsonValueKind.Array)
            {
                foreach (var vuln in vulns.EnumerateArray())
                {
                    if (vuln.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    foreach (var propertyName in new[] { "severity", "max_severity", "criticality", "risk", "level" })
                    {
                        var candidate = GetString(vuln, propertyName);
                        var candidatePriority = GetSeverityPriority(candidate);
                        if (candidatePriority > priority)
                        {
                            severity = candidate;
                            priority = candidatePriority;
                        }
                    }
                }
            }

            if (priority == 0)
            {
                var cvssScore = GetPackageCvssScore(package);
                severity = GetSeverityFromCvss(cvssScore);
            }

            return FormatSeverity(severity);
        }

        private static double GetPackageCvssScore(JsonElement package)
        {
            var score = 0.0;
            foreach (var propertyName in new[] { "cvss", "cvss_score", "cvssScore", "max_cvss", "max_cvss_score", "score" })
            {
                score = Math.Max(score, GetScore(package, propertyName));
            }

            if (package.TryGetProperty("vulns", out var vulns) && vulns.ValueKind == JsonValueKind.Array)
            {
                foreach (var vuln in vulns.EnumerateArray())
                {
                    if (vuln.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    foreach (var propertyName in new[] { "cvss", "cvss_score", "cvssScore", "max_cvss", "max_cvss_score", "score" })
                    {
                        score = Math.Max(score, GetScore(vuln, propertyName));
                    }
                }
            }

            return score;
        }

        private static int GetSeverityPriority(string severity)
        {
            switch ((severity ?? "").Trim().ToLowerInvariant())
            {
                case "critical":
                    return 5;
                case "high":
                case "important":
                    return 4;
                case "medium":
                case "moderate":
                    return 3;
                case "low":
                    return 2;
                case "none":
                case "informational":
                case "info":
                    return 1;
                default:
                    return 0;
            }
        }

        private static string GetSeverityFromCvss(double cvssScore)
        {
            if (cvssScore >= 9.0)
            {
                return "Critical";
            }
            if (cvssScore >= 7.0)
            {
                return "High";
            }
            if (cvssScore >= 4.0)
            {
                return "Medium";
            }
            if (cvssScore > 0)
            {
                return "Low";
            }

            return string.Empty;
        }

        private static string FormatSeverity(string severity)
        {
            severity = (severity ?? "").Trim();
            if (string.IsNullOrWhiteSpace(severity))
            {
                return string.Empty;
            }

            return char.ToUpperInvariant(severity[0]) + severity.Substring(1).ToLowerInvariant();
        }

        private static double GetScore(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return 0.0;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value))
            {
                return value;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return GetCvssScore(property.GetString());
            }

            return 0.0;
        }

        private static double GetCvssScore(string score)
        {
            score = (score ?? "").Trim();
            if (double.TryParse(
                score,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var numericScore))
            {
                return numericScore;
            }

            if (score.StartsWith("CVSS:3.", StringComparison.OrdinalIgnoreCase))
            {
                return GetCvssV3Score(score);
            }

            return 0.0;
        }

        private static double GetCvssV3Score(string vector)
        {
            var metrics = vector
                .Split('/')
                .Skip(1)
                .Select(part => part.Split(':'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

            if (!metrics.TryGetValue("AV", out var av) ||
                !metrics.TryGetValue("AC", out var ac) ||
                !metrics.TryGetValue("PR", out var pr) ||
                !metrics.TryGetValue("UI", out var ui) ||
                !metrics.TryGetValue("S", out var scope) ||
                !metrics.TryGetValue("C", out var c) ||
                !metrics.TryGetValue("I", out var i) ||
                !metrics.TryGetValue("A", out var a))
            {
                return 0.0;
            }

            var scopeChanged = string.Equals(scope, "C", StringComparison.OrdinalIgnoreCase);
            var impact = 1 - ((1 - GetCvssImpactValue(c)) * (1 - GetCvssImpactValue(i)) * (1 - GetCvssImpactValue(a)));
            var impactSubScore = scopeChanged
                ? 7.52 * (impact - 0.029) - 3.25 * Math.Pow(impact - 0.02, 15)
                : 6.42 * impact;
            if (impactSubScore <= 0)
            {
                return 0.0;
            }

            var exploitability = 8.22 *
                GetCvssAttackVectorValue(av) *
                GetCvssAttackComplexityValue(ac) *
                GetCvssPrivilegesRequiredValue(pr, scopeChanged) *
                GetCvssUserInteractionValue(ui);
            var rawScore = scopeChanged
                ? Math.Min(1.08 * (impactSubScore + exploitability), 10)
                : Math.Min(impactSubScore + exploitability, 10);
            return RoundUpCvss(rawScore);
        }

        private static double RoundUpCvss(double value)
        {
            return Math.Ceiling((value - 0.000001) * 10.0) / 10.0;
        }

        private static double GetCvssAttackVectorValue(string value)
        {
            switch ((value ?? "").ToUpperInvariant())
            {
                case "N":
                    return 0.85;
                case "A":
                    return 0.62;
                case "L":
                    return 0.55;
                case "P":
                    return 0.20;
                default:
                    return 0.0;
            }
        }

        private static double GetCvssAttackComplexityValue(string value)
        {
            switch ((value ?? "").ToUpperInvariant())
            {
                case "L":
                    return 0.77;
                case "H":
                    return 0.44;
                default:
                    return 0.0;
            }
        }

        private static double GetCvssPrivilegesRequiredValue(string value, bool scopeChanged)
        {
            switch ((value ?? "").ToUpperInvariant())
            {
                case "N":
                    return 0.85;
                case "L":
                    return scopeChanged ? 0.68 : 0.62;
                case "H":
                    return scopeChanged ? 0.50 : 0.27;
                default:
                    return 0.0;
            }
        }

        private static double GetCvssUserInteractionValue(string value)
        {
            switch ((value ?? "").ToUpperInvariant())
            {
                case "N":
                    return 0.85;
                case "R":
                    return 0.62;
                default:
                    return 0.0;
            }
        }

        private static double GetCvssImpactValue(string value)
        {
            switch ((value ?? "").ToUpperInvariant())
            {
                case "H":
                    return 0.56;
                case "L":
                    return 0.22;
                case "N":
                    return 0.0;
                default:
                    return 0.0;
            }
        }

        private static List<string> GetStringArray(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            return property
                .EnumerateArray()
                .Select(item =>
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        return item.GetString();
                    }

                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var propertyNameCandidate in new[] { "id", "cve", "name", "vulnerability_id" })
                        {
                            var value = GetString(item, propertyNameCandidate);
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }

                    return string.Empty;
                })
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
        }
    }
}
