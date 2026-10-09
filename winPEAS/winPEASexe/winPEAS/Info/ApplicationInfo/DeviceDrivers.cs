using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
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

    internal static class DeviceDrivers
    {
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
