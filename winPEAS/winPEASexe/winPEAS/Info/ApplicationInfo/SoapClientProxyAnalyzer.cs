using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using winPEAS.Helpers;
using winPEAS.Info.ProcessInfo;

namespace winPEAS.Info.ApplicationInfo
{
    internal class SoapClientProxyInstance
    {
        public string SourceType { get; set; }
        public string Name { get; set; }
        public string Account { get; set; }
        public string Extra { get; set; }
    }

    internal class SoapClientProxyFinding
    {
        public string BinaryPath { get; set; }
        public List<SoapClientProxyInstance> Instances { get; } = new List<SoapClientProxyInstance>();
        public HashSet<string> BinaryIndicators { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ConfigIndicators { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public string ConfigPath { get; set; }
        public bool BinaryScanFailed { get; set; }
        public bool ConfigScanFailed { get; set; }
    }

    internal static class SoapClientProxyAnalyzer
    {
        private class SoapClientProxyCandidate
        {
            public string BinaryPath { get; set; }
            public string SourceType { get; set; }
            public string Name { get; set; }
            public string Account { get; set; }
            public string Extra { get; set; }
        }

        private static readonly string[] BinaryIndicatorStrings = new[]
        {
            "SoapHttpClientProtocol",
            "HttpWebClientProtocol",
            "DiscoveryClientProtocol",
            "HttpSimpleClientProtocol",
            "HttpGetClientProtocol",
            "HttpPostClientProtocol",
            "ServiceDescriptionImporter",
            "System.Web.Services.Description.ServiceDescriptionImporter",
        };

        private static readonly Dictionary<string, string> ConfigIndicatorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "soap:address", "soap:address element present" },
            { "soap12:address", "soap12:address element present" },
            { "?wsdl", "?wsdl reference" },
            { "<wsdl:", "WSDL schema embedded in config" },
            { "servicedescriptionimporter", "ServiceDescriptionImporter referenced in config" },
            { "system.web.services.description", "System.Web.Services.Description namespace referenced" },
            { "new-webserviceproxy", "PowerShell New-WebServiceProxy referenced" },
            { "file://", "file:// scheme referenced" },
        };

        private const long MaxBinaryScanSize = 200 * 1024 * 1024; // 200MB
        private static readonly object DotNetCacheLock = new object();
        private static readonly Dictionary<string, bool> DotNetCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static List<SoapClientProxyFinding> CollectFindings()
        {
            var findings = new Dictionary<string, SoapClientProxyFinding>(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in EnumerateServiceCandidates().Concat(EnumerateProcessCandidates()))
            {
                if (string.IsNullOrEmpty(candidate.BinaryPath) || !File.Exists(candidate.BinaryPath))
                {
                    continue;
                }

                if (!findings.TryGetValue(candidate.BinaryPath, out var finding))
                {
                    finding = new SoapClientProxyFinding
                    {
                        BinaryPath = candidate.BinaryPath,
                    };

                    findings.Add(candidate.BinaryPath, finding);
                }

                finding.Instances.Add(new SoapClientProxyInstance
                {
                    SourceType = candidate.SourceType,
                    Name = candidate.Name,
                    Account = string.IsNullOrEmpty(candidate.Account) ? "Unknown" : candidate.Account,
                    Extra = candidate.Extra ?? string.Empty,
                });
            }

            foreach (var finding in findings.Values)
            {
                ScanBinaryIndicators(finding);
                ScanConfigIndicators(finding);
            }

            return findings.Values
                .Where(f => f.BinaryIndicators.Count > 0 || f.ConfigIndicators.Count > 0)
                .OrderByDescending(f => f.BinaryIndicators.Contains("ServiceDescriptionImporter"))
                .ThenBy(f => f.BinaryPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<SoapClientProxyCandidate> EnumerateServiceCandidates()
        {
            var results = new List<SoapClientProxyCandidate>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\\cimv2", "SELECT Name, DisplayName, PathName, StartName FROM Win32_Service"))
                using (var services = searcher.Get())
                {
                    foreach (ManagementObject service in services)
                    {
                        string pathName = service["PathName"]?.ToString();
                        string binaryPath = MyUtils.GetExecutableFromPath(pathName ?? string.Empty);
                        if (string.IsNullOrEmpty(binaryPath) || !File.Exists(binaryPath))
                            continue;

                        if (!IsDotNetBinary(binaryPath))
                            continue;

                        results.Add(new SoapClientProxyCandidate
                        {
                            BinaryPath = binaryPath,
                            SourceType = "Service",
                            Name = service["Name"]?.ToString() ?? string.Empty,
                            Account = service["StartName"]?.ToString() ?? string.Empty,
                            Extra = service["DisplayName"]?.ToString() ?? string.Empty,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while enumerating services for SOAP client analysis: " + ex.Message);
            }

            return results;
        }

        private static IEnumerable<SoapClientProxyCandidate> EnumerateProcessCandidates()
        {
            var results = new List<SoapClientProxyCandidate>();
            try
            {
                List<Dictionary<string, string>> processes = ProcessesInfo.GetProcInfo();
                foreach (var proc in processes)
                {
                    string path = proc.ContainsKey("ExecutablePath") ? proc["ExecutablePath"] : string.Empty;
                    if (string.IsNullOrEmpty(path) || !File.Exists(path))
                        continue;

                    if (!IsDotNetBinary(path))
                        continue;

                    string owner = proc.ContainsKey("Owner") ? proc["Owner"] : string.Empty;
                    if (!IsInterestingProcessOwner(owner))
                        continue;

                    results.Add(new SoapClientProxyCandidate
                    {
                        BinaryPath = path,
                        SourceType = "Process",
                        Name = proc.ContainsKey("Name") ? proc["Name"] : string.Empty,
                        Account = owner,
                        Extra = proc.ContainsKey("ProcessID") ? $"PID {proc["ProcessID"]}" : string.Empty,
                    });
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while enumerating processes for SOAP client analysis: " + ex.Message);
            }

            return results;
        }

        private static bool IsInterestingProcessOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner))
                return true;

            string normalizedOwner = owner;
            if (owner.Contains("\\"))
            {
                normalizedOwner = owner.Split('\\').Last();
            }

            return !normalizedOwner.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDotNetBinary(string path)
        {
            lock (DotNetCacheLock)
            {
                if (DotNetCache.TryGetValue(path, out bool cached))
                {
                    return cached;
                }

                bool result = false;
                try
                {
                    result = MyUtils.CheckIfDotNet(path, true);
                }
                catch
                {
                }

                DotNetCache[path] = result;
                return result;
            }
        }

        private static void ScanBinaryIndicators(SoapClientProxyFinding finding)
        {
            try
            {
                FileInfo fi = new FileInfo(finding.BinaryPath);
                if (!fi.Exists || fi.Length == 0)
                    return;

                if (fi.Length > MaxBinaryScanSize)
                {
                    finding.BinaryScanFailed = true;
                    return;
                }

                foreach (var indicator in BinaryIndicatorStrings)
                {
                    if (FileContainsString(finding.BinaryPath, indicator))
                    {
                        finding.BinaryIndicators.Add(indicator);
                    }
                }
            }
            catch
            {
                finding.BinaryScanFailed = true;
            }
        }

        private static void ScanConfigIndicators(SoapClientProxyFinding finding)
        {
            string configPath = GetConfigPath(finding.BinaryPath);
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                finding.ConfigPath = configPath;
                try
                {
                    string content = File.ReadAllText(configPath);
                    foreach (var kvp in ConfigIndicatorMap)
                    {
                        if (content.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            finding.ConfigIndicators.Add(kvp.Value);
                        }
                    }
                }
                catch
                {
                    finding.ConfigScanFailed = true;
                }
            }

            string directory = Path.GetDirectoryName(finding.BinaryPath);
            if (!string.IsNullOrEmpty(directory))
            {
                try
                {
                    var wsdlFiles = Directory.GetFiles(directory, "*.wsdl", SearchOption.TopDirectoryOnly);
                    if (wsdlFiles.Length > 0)
                    {
                        finding.ConfigIndicators.Add($"Found {wsdlFiles.Length} WSDL file(s) next to binary");
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private static string GetConfigPath(string binaryPath)
        {
            if (string.IsNullOrEmpty(binaryPath))
                return string.Empty;

            string candidate = binaryPath + ".config";
            return File.Exists(candidate) ? candidate : string.Empty;
        }

        private static bool FileContainsString(string path, string value)
        {
            const int bufferSize = 64 * 1024;
            byte[] pattern = Encoding.UTF8.GetBytes(value);
            if (pattern.Length == 0)
                return false;

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    byte[] buffer = new byte[bufferSize + pattern.Length];
                    int bufferLen = 0;
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, bufferLen, bufferSize)) > 0)
                    {
                        int total = bufferLen + bytesRead;
                        if (IndexOf(buffer, total, pattern) >= 0)
                        {
                            return true;
                        }

                        if (pattern.Length > 1)
                        {
                            bufferLen = Math.Min(pattern.Length - 1, total);
                            Buffer.BlockCopy(buffer, total - bufferLen, buffer, 0, bufferLen);
                        }
                        else
                        {
                            bufferLen = 0;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static int IndexOf(byte[] buffer, int bufferLength, byte[] pattern)
        {
            int limit = bufferLength - pattern.Length;
            if (limit < 0)
                return -1;

            for (int i = 0; i <= limit; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return i;
            }

            return -1;
        }
    }
}
