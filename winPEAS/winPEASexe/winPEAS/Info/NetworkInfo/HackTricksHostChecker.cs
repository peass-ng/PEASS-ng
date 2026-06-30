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

    public static class HackTricksHostChecker
    {
        private const int TimeoutSeconds = 15;
        private const string DefaultUrl = "https://tools.hacktricks.wiki/api/host-checker";
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

            return ParsePackageVulnerabilities(result.RawResponse, maxLines);
        }

        public static PackageVulnerabilitySummary ParsePackageVulnerabilities(string responseJson, int maxLines)
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

                    foreach (var package in packages.EnumerateArray())
                    {
                        if (summary.Lines.Count >= maxLines)
                        {
                            break;
                        }

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
                        summary.Lines.Add($"- {name} {version}{sourcePart}: {string.Join(", ", vulns)}".Trim());
                    }

                    summary.NotShown = Math.Max(0, summary.Affected - summary.Lines.Count);
                }
            }
            catch (Exception ex)
            {
                summary.Error = "Could not parse package vulnerability lookup response: " + ex.Message;
            }

            return summary;
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

        private static List<string> GetStringArray(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            return property
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
        }
    }
}
