using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace winPEAS.Info.SystemInfo
{
    internal class WindowsVersionVulns
    {
        private const string DefinitionsFileName = "windows_version_exploits.json";
        private static readonly object _cacheLock = new object();
        private static WindowsVersionDefinitions _cachedDefinitions;

        private static readonly SortedDictionary<int, string> BuildNumbers = new SortedDictionary<int, string>
        {
            { 10240, "1507" },
            { 10586, "1511" },
            { 14393, "1607" },
            { 15063, "1703" },
            { 16299, "1709" },
            { 17134, "1803" },
            { 17763, "1809" },
            { 18362, "1903" },
            { 18363, "1909" },
            { 19041, "2004" },
            { 19042, "20H2" },
            { 19043, "21H1" },
            { 19044, "21H2" },
            { 19045, "22H2" },
            { 20348, "21H2" },
            { 22000, "21H2" },
            { 22621, "22H2" },
            { 22631, "23H2" },
            { 26100, "24H2" },
        };

        internal static WindowsVersionVulnReport GetVulnerabilityReport(Dictionary<string, string> basicInfo)
        {
            var report = new WindowsVersionVulnReport();
            var definitions = LoadDefinitions();
            if (definitions == null || definitions.products == null)
            {
                return report;
            }

            report.DefinitionsDate = definitions.generated ?? "";
            report.CandidateProducts = BuildCandidateProducts(basicInfo);
            var installedHotfixes = GetInstalledHotfixes(basicInfo);
            report.InstalledHotfixesCount = installedHotfixes.Count;

            var matchedProducts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var matchedEntries = new List<WindowsVersionVulnEntry>();

            foreach (var candidate in report.CandidateProducts)
            {
                AddProductMatches(definitions.products, candidate, matchedProducts, matchedEntries);
            }

            report.MatchedProducts = matchedProducts.OrderBy(p => p).ToList();
            report.TotalMatchedBeforeFiltering = matchedEntries.Count;

            var filteredVulns = FilterPatchedVulnerabilities(matchedEntries, installedHotfixes, definitions.kb_supersedes);
            var vulnById = new Dictionary<string, WindowsVersionVulnEntry>(StringComparer.OrdinalIgnoreCase);
            AddEntries(filteredVulns, vulnById);

            report.Vulnerabilities = vulnById.Values
                .OrderByDescending(v => GetSeverityPriority(v.severity))
                .ThenBy(v => string.IsNullOrEmpty(v.cve) ? v.kb : v.cve, StringComparer.OrdinalIgnoreCase)
                .ToList();
            report.FilteredByPatches = report.TotalMatchedBeforeFiltering - report.Vulnerabilities.Count;

            return report;
        }

        private static void AddProductMatches(
            Dictionary<string, List<WindowsVersionVulnEntry>> products,
            string candidate,
            HashSet<string> matchedProducts,
            List<WindowsVersionVulnEntry> matchedEntries)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            var candidateVariants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                candidate,
                candidate.Replace(" Systems", " systems"),
                candidate.Replace(" systems", " Systems")
            };

            foreach (var candidateVariant in candidateVariants)
            {
                if (products.TryGetValue(candidateVariant, out var exactEntries))
                {
                    matchedProducts.Add(candidateVariant);
                    matchedEntries.AddRange(exactEntries);
                }
            }

            if (!candidate.StartsWith("Windows Server ", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            foreach (var kv in products)
            {
                if (kv.Key.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    matchedProducts.Add(kv.Key);
                    matchedEntries.AddRange(kv.Value);
                }
            }
        }

        private static void AddEntries(IEnumerable<WindowsVersionVulnEntry> entries, Dictionary<string, WindowsVersionVulnEntry> vulnById)
        {
            foreach (var entry in entries ?? Enumerable.Empty<WindowsVersionVulnEntry>())
            {
                var key = !string.IsNullOrWhiteSpace(entry.cve) ? entry.cve : $"KB{entry.kb}";
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!vulnById.ContainsKey(key))
                {
                    vulnById[key] = entry;
                }
            }
        }

        private static int GetSeverityPriority(string severity)
        {
            switch ((severity ?? "").Trim().ToLowerInvariant())
            {
                case "critical":
                    return 4;
                case "important":
                    return 3;
                case "moderate":
                    return 2;
                case "low":
                    return 1;
                default:
                    return 0;
            }
        }

        private static HashSet<string> GetInstalledHotfixes(Dictionary<string, string> basicInfo)
        {
            var hotfixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string text = GetValue(basicInfo, "Hotfixes");
            if (string.IsNullOrWhiteSpace(text))
            {
                return hotfixes;
            }

            foreach (Match match in Regex.Matches(text, @"KB(\d+)", RegexOptions.IgnoreCase))
            {
                hotfixes.Add(match.Groups[1].Value);
            }

            return hotfixes;
        }

        private static List<WindowsVersionVulnEntry> FilterPatchedVulnerabilities(
            List<WindowsVersionVulnEntry> entries,
            HashSet<string> installedHotfixes,
            Dictionary<string, List<string>> kbSupersedes)
        {
            if (entries.Count == 0)
            {
                return entries;
            }

            var suppressed = ExpandSupersededHotfixes(installedHotfixes, kbSupersedes);
            return entries
                .Where(entry =>
                {
                    if (string.IsNullOrWhiteSpace(entry.kb))
                    {
                        return true;
                    }
                    return !suppressed.Contains(entry.kb);
                })
                .ToList();
        }

        private static HashSet<string> ExpandSupersededHotfixes(HashSet<string> installedHotfixes, Dictionary<string, List<string>> kbSupersedes)
        {
            var suppressed = new HashSet<string>(installedHotfixes, StringComparer.OrdinalIgnoreCase);
            if (kbSupersedes == null || kbSupersedes.Count == 0)
            {
                return suppressed;
            }

            var queue = new Queue<string>(installedHotfixes);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!kbSupersedes.TryGetValue(current, out var children) || children == null)
                {
                    continue;
                }

                foreach (var child in children)
                {
                    if (suppressed.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return suppressed;
        }

        private static List<string> BuildCandidateProducts(Dictionary<string, string> basicInfo)
        {
            var candidates = new List<string>();
            string osName = GetValue(basicInfo, "OS Name");
            string productName = GetValue(basicInfo, "ProductName");
            string osVersion = GetValue(basicInfo, "OS Version");
            string systemType = GetValue(basicInfo, "System Type");
            string text = $"{osName} {productName}";

            string arch = GetArchitectureLabel(systemType);
            string servicePack = GetServicePack(osVersion);
            int build = GetBuildNumber(osVersion);
            string clientVersion = GetVersionFromBuild(build);

            if (Contains(text, "Windows 11") && !string.IsNullOrEmpty(clientVersion))
            {
                candidates.Add($"Windows 11 Version {clientVersion} for {arch} Systems");
            }
            if (Contains(text, "Windows 10") && !string.IsNullOrEmpty(clientVersion))
            {
                candidates.Add($"Windows 10 Version {clientVersion} for {arch} Systems");
            }
            if (Contains(text, "Windows 8.1"))
            {
                candidates.Add($"Windows 8.1 for {arch} systems");
                candidates.Add($"Windows 8.1 for {arch} Systems");
            }
            if (Contains(text, "Windows 8") && !Contains(text, "Windows 8.1"))
            {
                candidates.Add($"Windows 8 for {arch} Systems");
            }
            if (Contains(text, "Windows 7"))
            {
                string win7 = $"Windows 7 for {arch} Systems";
                if (!string.IsNullOrEmpty(servicePack))
                {
                    win7 += $" Service Pack {servicePack}";
                }
                candidates.Add(win7);
            }

            AddServerCandidates(candidates, text, build, arch, servicePack);

            return candidates
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void AddServerCandidates(List<string> candidates, string productText, int build, string arch, string servicePack)
        {
            if (!Contains(productText, "Windows Server"))
            {
                return;
            }

            string serverName = "";
            if (Contains(productText, "Server 2025")) serverName = "2025";
            else if (Contains(productText, "Server 2022")) serverName = "2022";
            else if (Contains(productText, "Server 2019")) serverName = "2019";
            else if (Contains(productText, "Server 2016")) serverName = "2016";
            else if (Contains(productText, "Server 2012 R2")) serverName = "2012 R2";
            else if (Contains(productText, "Server 2012")) serverName = "2012";
            else if (Contains(productText, "Server 2008 R2")) serverName = "2008 R2";
            else if (Contains(productText, "Server 2008")) serverName = "2008";
            else if (Contains(productText, "Server 2003 R2")) serverName = "2003 R2";
            else if (Contains(productText, "Server 2003")) serverName = "2003";

            if (string.IsNullOrEmpty(serverName))
            {
                if (build >= 26100) serverName = "2025";
                else if (build >= 20348) serverName = "2022";
                else if (build >= 17763) serverName = "2019";
                else if (build >= 14393) serverName = "2016";
            }

            if (string.IsNullOrEmpty(serverName))
            {
                return;
            }

            if (serverName == "2008" || serverName == "2008 R2")
            {
                string item = $"Windows Server {serverName} for {arch} Systems";
                if (!string.IsNullOrEmpty(servicePack))
                {
                    item += $" Service Pack {servicePack}";
                }
                candidates.Add(item);
                candidates.Add(item + " (Server Core installation)");
                return;
            }

            candidates.Add($"Windows Server {serverName}");
            candidates.Add($"Windows Server {serverName} (Server Core installation)");
        }

        private static string GetArchitectureLabel(string systemType)
        {
            string value = (systemType ?? "").ToLowerInvariant();
            if (value.Contains("arm64"))
            {
                return "ARM64-based";
            }
            if (value.Contains("x64"))
            {
                return "x64-based";
            }
            if (value.Contains("x86") || value.Contains("32"))
            {
                return "32-bit";
            }
            return "x64-based";
        }

        private static int GetBuildNumber(string osVersion)
        {
            var match = Regex.Match(osVersion ?? "", @"Build\s+(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static string GetServicePack(string osVersion)
        {
            var match = Regex.Match(osVersion ?? "", @"Service Pack\s+(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "";
        }

        private static string GetVersionFromBuild(int build)
        {
            string version = "";
            foreach (var kv in BuildNumbers)
            {
                if (build == kv.Key)
                {
                    return kv.Value;
                }
                if (build > kv.Key)
                {
                    version = kv.Value;
                    continue;
                }
                break;
            }

            return version;
        }

        private static bool Contains(string input, string value)
        {
            return (input ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetValue(Dictionary<string, string> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value))
            {
                return "";
            }

            return value ?? "";
        }

        private static WindowsVersionDefinitions LoadDefinitions()
        {
            if (_cachedDefinitions != null)
            {
                return _cachedDefinitions;
            }

            lock (_cacheLock)
            {
                if (_cachedDefinitions != null)
                {
                    return _cachedDefinitions;
                }

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = $"{assembly.GetName().Name}.{DefinitionsFileName}";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();
                        var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
                        _cachedDefinitions = serializer.Deserialize<WindowsVersionDefinitions>(content);
                    }
                }
            }

            return _cachedDefinitions;
        }
    }

    internal class WindowsVersionVulnReport
    {
        public string DefinitionsDate { get; set; } = "";
        public List<string> CandidateProducts { get; set; } = new List<string>();
        public List<string> MatchedProducts { get; set; } = new List<string>();
        public List<WindowsVersionVulnEntry> Vulnerabilities { get; set; } = new List<WindowsVersionVulnEntry>();
        public int InstalledHotfixesCount { get; set; }
        public int TotalMatchedBeforeFiltering { get; set; }
        public int FilteredByPatches { get; set; }
    }

    internal class WindowsVersionDefinitions
    {
        public string generated { get; set; }
        public Dictionary<string, List<WindowsVersionVulnEntry>> products { get; set; }
        public Dictionary<string, List<string>> kb_supersedes { get; set; }
    }

    internal class WindowsVersionVulnEntry
    {
        public string cve { get; set; }
        public string kb { get; set; }
        public string severity { get; set; }
        public string impact { get; set; }
    }
}
