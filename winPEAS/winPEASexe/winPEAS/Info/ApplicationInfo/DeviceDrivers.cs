using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32;
using System.ServiceProcess;
using winPEAS.Helpers;
using winPEAS.Native;

namespace winPEAS.Info.ApplicationInfo
{
    internal sealed class DeviceDriverRecord
    {
        public string Name;
        public string Path;
        public string State;
        public string StartMode;
        public string Company;
        public string Product;
        public string Version;
    }

    internal sealed class DeviceDriverInventory
    {
        public List<DeviceDriverRecord> Drivers = new List<DeviceDriverRecord>();
        public bool UnknownOrTruncated;
        public string Detail;
    }

    internal sealed class VulnerableDriverCandidate
    {
        public string ServiceName;
        public string Path;
        public bool ImagePresent;
        public string State = "unknown";
        public int Start = -1;
        public string Sha256 = "unavailable";
        public bool KnownHash;
        public string Version = "unavailable";
        public string Signer = "unavailable";
        public string SignatureStatus = "not checked";
    }

    internal static class DeviceDrivers
    {
        // SHA-256 of msft.sys in nbs32k/LocalStranger, commit 41fbecee2c9f928abb6306ccb481e715c80d8e8e.
        private const string LocalStrangerSha256 = "4dc3a03a046c210a7c6cd37db8d6bd34bf3760a612232cc82acf941a9e508e0f";
        private const int MaxDriverServiceKeys = 4096;
        private const int MaxDriverCandidates = 4;
        private const long MaxHashBytes = 8 * 1024 * 1024;
        internal const int MaxPsapiDrivers = 1024;
        internal const int MaxWmiDrivers = 1024;
        internal const int MaxOutputDrivers = 256;
        private static readonly TimeSpan WmiTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan WmiBudget = TimeSpan.FromSeconds(4);

        public static DeviceDriverInventory GetDriverInventory()
        {
            var psapi = new List<DeviceDriverRecord>();
            var wmi = new List<DeviceDriverRecord>();
            var details = new List<string>();
            ReadPsapi(psapi, details);
            ReadRunningSystemDrivers(wmi, details);
            return Merge(psapi, wmi, MaxOutputDrivers, string.Join("; ", details));
        }

        // Inspect only registered kernel-driver images with relevant names. Never contact a driver device.
        public static List<VulnerableDriverCandidate> GetVulnerableDriverCandidates(out bool incomplete)
        {
            var candidates = new List<VulnerableDriverCandidate>();
            incomplete = false;
            try
            {
                using (RegistryKey services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                {
                    if (services == null) { incomplete = true; return candidates; }
                    string[] names = services.GetSubKeyNames();
                    int inspected = 0;
                    var timer = Stopwatch.StartNew();
                    foreach (string name in names)
                    {
                        if (++inspected > MaxDriverServiceKeys || timer.ElapsedMilliseconds >= 2000 ||
                            candidates.Count >= MaxDriverCandidates)
                        {
                            incomplete = true;
                            break;
                        }
                        try
                        {
                            using (RegistryKey key = services.OpenSubKey(name))
                            {
                                if (key == null) continue;
                                int type = Convert.ToInt32(key.GetValue("Type", 0));
                                if (type != 1 && type != 2) continue; // kernel and file-system driver services
                                string rawPath = key.GetValue("ImagePath") as string;
                                if (string.IsNullOrWhiteSpace(rawPath))
                                    rawPath = @"System32\drivers\" + name + ".sys";
                                string path = NormalizePath(rawPath);
                                string fileName = Path.GetFileName(path);
                                if (!string.Equals(fileName, "WinNotify.sys", StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(fileName, "msft.sys", StringComparison.OrdinalIgnoreCase)) continue;

                                var candidate = new VulnerableDriverCandidate { ServiceName = name, Path = path };
                                try { candidate.Start = Convert.ToInt32(key.GetValue("Start", -1)); }
                                catch { /* Keep start mode unknown. */ }
                                try
                                {
                                    using (var controller = new ServiceController(name))
                                        candidate.State = controller.Status.ToString();
                                }
                                catch { /* Loaded state remains unknown. */ }
                                ReadCandidateFile(candidate);
                                candidates.Add(candidate);
                            }
                        }
                        catch { incomplete = true; /* One unreadable service must not stop the audit. */ }
                    }
                }
            }
            catch { incomplete = true; }
            return candidates;
        }

        private static void ReadCandidateFile(VulnerableDriverCandidate candidate)
        {
            try
            {
                if (!IsLocalFilePath(candidate.Path) || !File.Exists(candidate.Path)) return;
                candidate.ImagePresent = true;
                var file = new FileInfo(candidate.Path);
                if (file.Length > MaxHashBytes)
                {
                    candidate.Sha256 = "not checked (image exceeds 8 MiB limit)";
                    candidate.Version = "not checked (image exceeds 8 MiB limit)";
                    candidate.SignatureStatus = "not checked (image exceeds 8 MiB limit)";
                    return;
                }
                try
                {
                    using (var stream = new FileStream(candidate.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sha = SHA256.Create())
                        candidate.Sha256 = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    candidate.KnownHash = string.Equals(candidate.Sha256, LocalStrangerSha256, StringComparison.OrdinalIgnoreCase);
                }
                catch { candidate.Sha256 = "unavailable (image could not be read)"; }
                try { candidate.Version = FileVersionInfo.GetVersionInfo(candidate.Path).FileVersion ?? "unavailable"; }
                catch { /* Signature verification can still succeed without version metadata. */ }
                AuthenticodeHelper.Verify(candidate.Path, out string status);
                candidate.SignatureStatus = status;
                try
                {
                    using (var baseCert = X509Certificate.CreateFromSignedFile(candidate.Path))
                    using (var cert = new X509Certificate2(baseCert))
                    {
                        candidate.Signer = cert.Subject;
                    }
                }
                catch { /* A catalog signature may have no embedded signer. */ }
            }
            catch { /* Hash and signature remain unknown if the image cannot be read. */ }
        }

        // Pure merge/filter step: fixtures need no Windows APIs or device access.
        internal static DeviceDriverInventory Merge(IEnumerable<DeviceDriverRecord> psapi,
            IEnumerable<DeviceDriverRecord> systemDrivers, int maxOutput, string providerIssue = null)
        {
            var result = new DeviceDriverInventory();
            if (!string.IsNullOrEmpty(providerIssue))
            {
                result.UnknownOrTruncated = true;
                result.Detail = providerIssue;
            }
            var byPath = new Dictionary<string, DeviceDriverRecord>(StringComparer.OrdinalIgnoreCase);
            if (maxOutput < 1)
            {
                result.UnknownOrTruncated = true;
                result.Detail = "driver output cap reached";
                return result;
            }
            foreach (IEnumerable<DeviceDriverRecord> source in new[] { psapi, systemDrivers })
            {
                if (source == null) continue;
                foreach (DeviceDriverRecord driver in source)
                {
                    if (driver == null || !IsCandidate(driver)) continue;
                    string key = NormalizePath(driver.Path);
                    if (key.Length == 0) continue;
                    DeviceDriverRecord existing;
                    if (byPath.TryGetValue(key, out existing))
                    {
                        // Service metadata enriches a loaded PSAPI entry.
                        if (string.IsNullOrEmpty(existing.Name)) existing.Name = driver.Name;
                        if (string.IsNullOrEmpty(existing.State)) existing.State = driver.State;
                        if (string.IsNullOrEmpty(existing.StartMode)) existing.StartMode = driver.StartMode;
                        if (string.IsNullOrEmpty(existing.Company)) existing.Company = driver.Company;
                        if (string.IsNullOrEmpty(existing.Product)) existing.Product = driver.Product;
                        if (string.IsNullOrEmpty(existing.Version)) existing.Version = driver.Version;
                        continue;
                    }
                    if (result.Drivers.Count == maxOutput)
                    {
                        result.UnknownOrTruncated = true;
                        result.Detail = string.IsNullOrEmpty(result.Detail)
                            ? "driver output cap reached" : result.Detail + "; driver output cap reached";
                        return result;
                    }
                    var copy = new DeviceDriverRecord {
                        Name = driver.Name, Path = driver.Path, State = driver.State,
                        StartMode = driver.StartMode, Company = driver.Company,
                        Product = driver.Product, Version = driver.Version
                    };
                    byPath.Add(key, copy);
                    result.Drivers.Add(copy);
                }
            }
            return result;
        }

        internal static bool IsCandidate(DeviceDriverRecord driver)
        {
            return driver != null && !string.IsNullOrWhiteSpace(driver.Path)
                && (string.IsNullOrEmpty(driver.State) ||
                    string.Equals(driver.State, "Running", StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(driver.Company) ||
                    !driver.Company.TrimStart().StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(driver.Name) ||
                    !driver.Name.StartsWith("dump_", StringComparison.OrdinalIgnoreCase));
        }

        internal static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            string value = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            if (value.StartsWith(@"\??\", StringComparison.OrdinalIgnoreCase)) value = value.Substring(4);
            if (value.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
                value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), value.Substring(12));
            else if (value.StartsWith(@"System32\", StringComparison.OrdinalIgnoreCase))
                value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), value);
            else if (value.StartsWith(@"\Windows\", StringComparison.OrdinalIgnoreCase))
                value = Path.GetPathRoot(Environment.SystemDirectory) + value.TrimStart('\\');
            else if (value.IndexOf('\\') < 0 && value.EndsWith(".sys", StringComparison.OrdinalIgnoreCase))
                value = Path.Combine(Environment.SystemDirectory, "drivers", value);
            return value.TrimEnd('\\');
        }

        internal static bool IsLocalFilePath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.Length > 3 && char.IsLetter(path[0])
                && path[1] == ':' && path[2] == '\\';
        }

        private static void ReadPsapi(List<DeviceDriverRecord> drivers, List<string> details)
        {
            try
            {
                uint needed;
                if (!Psapi.EnumDeviceDrivers(null, 0, out needed) || needed == 0)
                {
                    details.Add("PSAPI driver enumeration unavailable");
                    return;
                }
                int total = checked((int)(needed / UIntPtr.Size));
                int count = Math.Min(total, MaxPsapiDrivers);
                if (total > count) details.Add("PSAPI driver list truncated");
                var addresses = new UIntPtr[count];
                if (!Psapi.EnumDeviceDrivers(addresses, (uint)(count * UIntPtr.Size), out needed))
                {
                    details.Add("PSAPI driver enumeration failed");
                    return;
                }
                if (needed > (uint)(count * UIntPtr.Size)) details.Add("PSAPI driver list changed or truncated");
                int returned = Math.Min(count, (int)(needed / UIntPtr.Size));
                bool nullAddress = false;
                for (int i = 0; i < returned; i++)
                {
                    if (addresses[i] == UIntPtr.Zero) { nullAddress = true; continue; }
                    var name = new StringBuilder(1024);
                    Psapi.GetDeviceDriverBaseName(addresses[i], name, (uint)name.Capacity);
                    if (name.ToString().StartsWith("dump_", StringComparison.OrdinalIgnoreCase)) continue;
                    var path = new StringBuilder(1024);
                    if (Psapi.GetDeviceDriverFileName(addresses[i], path, (uint)path.Capacity) == 0) continue;
                    string resolved = NormalizePath(path.ToString());
                    if (resolved.Length == 0) continue;
                    var record = new DeviceDriverRecord { Name = name.ToString(), Path = resolved, State = "Running" };
                    ReadVersion(record);
                    drivers.Add(record);
                }
                if (nullAddress) details.Add("PSAPI returned null driver base addresses; running service metadata used where available");
            }
            catch (Exception ex)
            {
                details.Add("PSAPI driver inventory unavailable (" + ex.GetType().Name + ")");
            }
        }

        private static void ReadRunningSystemDrivers(List<DeviceDriverRecord> drivers, List<string> details)
        {
            var options = new EnumerationOptions { ReturnImmediately = true, Rewindable = false, Timeout = WmiTimeout };
            var timer = Stopwatch.StartNew();
            try
            {
                using (var searcher = new ManagementObjectSearcher(new ManagementScope(@"\\.\root\cimv2"),
                    new ObjectQuery("SELECT Name,PathName,StartMode,State,ServiceType FROM Win32_SystemDriver WHERE State='Running'"), options))
                using (ManagementObjectCollection rows = searcher.Get())
                {
                    int examined = 0;
                    foreach (ManagementObject row in rows)
                    {
                        using (row)
                        {
                            if (timer.Elapsed >= WmiBudget || examined == MaxWmiDrivers)
                            {
                                details.Add("running driver metadata truncated (time or row cap)");
                                break;
                            }
                            examined++;
                            string type = ReadProperty(row, "ServiceType");
                            if (!string.Equals(type, "Kernel Driver", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(type, "File System Driver", StringComparison.OrdinalIgnoreCase)) continue;
                            string path = NormalizePath(ReadProperty(row, "PathName"));
                            if (path.Length == 0)
                            {
                                details.Add("running driver has no resolvable image path");
                                continue;
                            }
                            var record = new DeviceDriverRecord {
                                Name = ReadProperty(row, "Name"), Path = path,
                                State = ReadProperty(row, "State"), StartMode = ReadProperty(row, "StartMode")
                            };
                            ReadVersion(record);
                            drivers.Add(record);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                details.Add("running driver metadata unavailable or timed out (" + ex.GetType().Name + ")");
            }
        }

        private static string ReadProperty(ManagementBaseObject row, string property)
        {
            object value = row[property];
            return value == null ? string.Empty : value.ToString();
        }

        private static void ReadVersion(DeviceDriverRecord record)
        {
            try
            {
                if (!IsLocalFilePath(record.Path) || !File.Exists(record.Path)) return;
                FileVersionInfo version = FileVersionInfo.GetVersionInfo(record.Path);
                record.Company = version.CompanyName;
                record.Product = version.ProductName;
                record.Version = version.ProductVersion;
            }
            catch { /* Metadata is optional; keep the running driver in the inventory. */ }
        }
    }
}
