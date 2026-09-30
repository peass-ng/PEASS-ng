using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Helpers;

namespace winPEAS.Info.SystemInfo
{
    internal static class WindowsInstallerCve27727
    {
        internal const string TempPackagesKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\TempPackages";
        internal const string InstallerFoldersKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders";

        private static readonly Dictionary<int, FixedBuild> FixedBuilds = new Dictionary<int, FixedBuild>
        {
            { 6003,  new FixedBuild(23220, "5055609/5055596") },
            { 7601,  new FixedBuild(27670, "5055561/5055570") },
            { 9200,  new FixedBuild(25423, "5055581") },
            { 9600,  new FixedBuild(22523, "5055557") },
            { 10240, new FixedBuild(20978, "5055547") },
            { 14393, new FixedBuild(7969,  "5055521") },
            { 17763, new FixedBuild(7136,  "5055519") },
            { 19044, new FixedBuild(5737,  "5055518") },
            { 19045, new FixedBuild(5737,  "5055518") },
            { 20348, new FixedBuild(3453,  "5055526") },
            { 22621, new FixedBuild(5189,  "5055528") },
            { 22631, new FixedBuild(5189,  "5055528") },
            { 25398, new FixedBuild(1551,  "5055527") },
            { 26100, new FixedBuild(3775,  "5055523") },
        };

        internal static WindowsInstallerReport GetReport(Dictionary<string, string> basicInfo)
        {
            var report = new WindowsInstallerReport
            {
                Architecture = GetValue(basicInfo, "Architecture"),
                ProductName = GetValue(basicInfo, "ProductName"),
                InstalledHotfixes = GetValue(basicInfo, "Hotfixes")
            };

            int build;
            int revision;
            if (int.TryParse(GetValue(basicInfo, "CurrentBuild"), out build) &&
                int.TryParse(GetValue(basicInfo, "UpdateBuildRevision"), out revision))
            {
                report.Build = build;
                report.Revision = revision;
                report.BuildVersion = build + "." + revision;

                FixedBuild fixedBuild;
                if (FixedBuilds.TryGetValue(build, out fixedBuild))
                {
                    report.FixedRevision = fixedBuild.Revision;
                    report.FixedVersion = build + "." + fixedBuild.Revision;
                    report.ApplicableKb = fixedBuild.Kb;
                    report.VersionStatus = revision < fixedBuild.Revision ?
                        WindowsInstallerVersionStatus.Susceptible :
                        WindowsInstallerVersionStatus.Patched;
                }
                else
                {
                    report.VersionStatus = WindowsInstallerVersionStatus.NotListed;
                }
            }
            else
            {
                report.VersionStatus = WindowsInstallerVersionStatus.Unknown;
            }

            CollectMsiDll(report);
            CollectRegistryArtifacts(report);
            CollectConfigMsiArtifacts(report);
            return report;
        }

        private static string GetValue(Dictionary<string, string> values, string key)
        {
            string value;
            return values != null && values.TryGetValue(key, out value) ? value ?? "" : "";
        }

        private static void CollectMsiDll(WindowsInstallerReport report)
        {
            try
            {
                string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                report.MsiDllPath = Path.Combine(windowsDirectory, "System32", "msi.dll");
                if (!File.Exists(report.MsiDllPath))
                {
                    return;
                }

                FileVersionInfo version = FileVersionInfo.GetVersionInfo(report.MsiDllPath);
                FileInfo file = new FileInfo(report.MsiDllPath);
                report.MsiDllFileVersion = version.FileVersion ?? "";
                report.MsiDllProductVersion = version.ProductVersion ?? "";
                report.MsiDllCompany = version.CompanyName ?? "";
                report.MsiDllSize = file.Length;
                report.MsiDllLastWriteUtc = file.LastWriteTimeUtc;
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("msi.dll metadata: " + ex.Message);
            }
        }

        private static void CollectRegistryArtifacts(WindowsInstallerReport report)
        {
            try
            {
                using (RegistryKey baseKey = OpenLocalMachine())
                using (RegistryKey key = baseKey == null ? null : baseKey.OpenSubKey(TempPackagesKey))
                {
                    if (key != null)
                    {
                        string[] names = key.GetValueNames();
                        report.TempPackagesTotal = names.Length;
                        foreach (string name in names.Take(100))
                        {
                            object data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                            RegistryValueKind kind = key.GetValueKind(name);
                            long numericData;
                            bool isNumeric = TryGetInteger(data, out numericData);
                            var entry = new TempPackageEntry
                            {
                                Path = name,
                                Kind = kind.ToString(),
                                Data = data == null ? "(null)" : data.ToString(),
                                IsFolder = isNumeric && (numericData & 2) == 2,
                                IsConfigMsi = IsConfigMsiPath(name),
                                IsOutsideCommonInstallerLocations = !IsCommonInstallerLocation(name)
                            };
                            report.TempPackages.Add(entry);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("TempPackages registry: " + ex.Message);
            }

            try
            {
                using (RegistryKey baseKey = OpenLocalMachine())
                using (RegistryKey key = baseKey == null ? null : baseKey.OpenSubKey(InstallerFoldersKey))
                {
                    if (key != null)
                    {
                        report.InstallerFoldersHasConfigMsi = key.GetValueNames().Any(IsConfigMsiPath);
                    }
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Installer\\Folders registry: " + ex.Message);
            }
        }

        private static RegistryKey OpenLocalMachine()
        {
            try
            {
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            catch
            {
                try
                {
                    return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                }
                catch
                {
                    return null;
                }
            }
        }

        private static bool TryGetInteger(object value, out long result)
        {
            try
            {
                if (value is int || value is uint || value is long || value is ulong ||
                    value is short || value is ushort || value is byte || value is sbyte)
                {
                    result = Convert.ToInt64(value);
                    return true;
                }
            }
            catch
            {
            }

            result = 0;
            return false;
        }

        private static bool IsConfigMsiPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string root = Path.GetPathRoot(Environment.SystemDirectory);
                string expected = Path.Combine(root, "Config.Msi").TrimEnd('\\');
                string actual = Environment.ExpandEnvironmentVariables(path).TrimEnd('\\');
                return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCommonInstallerLocation(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string expanded = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
                string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var expectedRoots = new[]
                {
                    Path.Combine(windowsDirectory, "Installer"),
                    Path.Combine(windowsDirectory, "Temp"),
                    Path.GetTempPath(),
                    Path.Combine(programData, "Package Cache")
                };

                return IsConfigMsiPath(expanded) || expectedRoots.Any(root => IsPathUnder(expanded, root));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPathUnder(string path, string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            string normalizedPath = Path.GetFullPath(path).TrimEnd('\\') + "\\";
            string normalizedRoot = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static void CollectConfigMsiArtifacts(WindowsInstallerReport report)
        {
            try
            {
                string systemDirectory = Environment.SystemDirectory;
                string driveRoot = string.IsNullOrWhiteSpace(systemDirectory) ? "" : Path.GetPathRoot(systemDirectory);
                if (string.IsNullOrWhiteSpace(driveRoot))
                {
                    report.CollectionErrors.Add("Config.Msi path: unable to determine the Windows system drive.");
                    return;
                }

                report.ConfigMsiPath = Path.Combine(driveRoot, "Config.Msi");
                report.ConfigMsiExists = Directory.Exists(report.ConfigMsiPath);
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Config.Msi path: " + ex.Message);
                return;
            }

            if (!report.ConfigMsiExists)
            {
                return;
            }

            try
            {
                DirectoryInfo directory = new DirectoryInfo(report.ConfigMsiPath);
                report.ConfigMsiCreationUtc = directory.CreationTimeUtc;
                report.ConfigMsiLastWriteUtc = directory.LastWriteTimeUtc;
                report.ConfigMsiRecentlyChanged = directory.LastWriteTimeUtc >= DateTime.UtcNow.AddDays(-7);

                DirectorySecurity security = Directory.GetAccessControl(
                    report.ConfigMsiPath,
                    AccessControlSections.Owner | AccessControlSections.Access);
                SecurityIdentifier ownerSid = security.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
                if (ownerSid != null)
                {
                    report.ConfigMsiOwner = TranslateSid(ownerSid);
                    report.ConfigMsiUnusualOwner = !IsExpectedOwner(ownerSid.Value);
                }

                byte[] descriptor = security.GetSecurityDescriptorBinaryForm();
                var rawDescriptor = new RawSecurityDescriptor(descriptor, 0);
                report.ConfigMsiNullDacl = rawDescriptor.DiscretionaryAcl == null;
                report.ConfigMsiDangerousAcls.AddRange(GetDangerousAclEntries(security));
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Config.Msi security: " + ex.Message);
            }

            try
            {
                IEnumerable<string> rollbackFiles = Directory.EnumerateFiles(report.ConfigMsiPath, "*.rbs", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.EnumerateFiles(report.ConfigMsiPath, "*.rbf", SearchOption.TopDirectoryOnly))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(20);
                foreach (string rollbackFile in rollbackFiles)
                {
                    FileInfo file = new FileInfo(rollbackFile);
                    report.RollbackFiles.Add(file.Name + " (last write UTC: " + file.LastWriteTimeUtc.ToString("u") + ")");
                }
            }
            catch (Exception ex)
            {
                report.CollectionErrors.Add("Config.Msi rollback files: " + ex.Message);
            }
        }

        private static IEnumerable<string> GetDangerousAclEntries(DirectorySecurity security)
        {
            HashSet<string> unprivilegedSids = PermissionsHelper.GetUnprivilegedTokenSids();
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                var sid = rule.IdentityReference as SecurityIdentifier;
                if (sid == null || !unprivilegedSids.Contains(sid.Value) || rule.AccessControlType != AccessControlType.Allow)
                {
                    continue;
                }

                List<string> rights = GetDangerousRights(rule.FileSystemRights);
                if (rights.Count > 0)
                {
                    yield return TranslateSid(sid) + ": " + string.Join(", ", rights) +
                        (rule.IsInherited ? " (inherited)" : " (explicit)");
                }
            }
        }

        private static List<string> GetDangerousRights(FileSystemRights granted)
        {
            var rights = new List<string>();
            AddRight(rights, granted, FileSystemRights.FullControl, "FullControl");
            if (rights.Count > 0)
            {
                return rights;
            }

            AddRight(rights, granted, FileSystemRights.ChangePermissions, "WRITE_DAC");
            AddRight(rights, granted, FileSystemRights.TakeOwnership, "WRITE_OWNER");
            AddRight(rights, granted, FileSystemRights.DeleteSubdirectoriesAndFiles, "FILE_DELETE_CHILD");
            AddRight(rights, granted, FileSystemRights.Modify, "Modify");
            AddRight(rights, granted, FileSystemRights.WriteData, "CreateFiles/WriteData");
            AddRight(rights, granted, FileSystemRights.AppendData, "CreateDirectories/AppendData");
            AddRight(rights, granted, FileSystemRights.Delete, "Delete");
            return rights.Distinct().ToList();
        }

        private static void AddRight(List<string> output, FileSystemRights granted, FileSystemRights right, string name)
        {
            if ((granted & right) == right)
            {
                output.Add(name);
            }
        }

        private static string TranslateSid(SecurityIdentifier sid)
        {
            try
            {
                return sid.Translate(typeof(NTAccount)).Value + " (" + sid.Value + ")";
            }
            catch
            {
                return sid.Value;
            }
        }

        private static bool IsExpectedOwner(string sid)
        {
            return string.Equals(sid, "S-1-5-18", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sid, "S-1-5-32-544", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sid, "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class FixedBuild
        {
            internal FixedBuild(int revision, string kb)
            {
                Revision = revision;
                Kb = kb;
            }

            internal int Revision { get; private set; }
            internal string Kb { get; private set; }
        }
    }

    internal enum WindowsInstallerVersionStatus
    {
        Unknown,
        Susceptible,
        Patched,
        NotListed
    }

    internal sealed class TempPackageEntry
    {
        internal string Path { get; set; }
        internal string Kind { get; set; }
        internal string Data { get; set; }
        internal bool IsFolder { get; set; }
        internal bool IsConfigMsi { get; set; }
        internal bool IsOutsideCommonInstallerLocations { get; set; }
    }

    internal sealed class WindowsInstallerReport
    {
        internal WindowsInstallerReport()
        {
            TempPackages = new List<TempPackageEntry>();
            ConfigMsiDangerousAcls = new List<string>();
            RollbackFiles = new List<string>();
            CollectionErrors = new List<string>();
        }

        internal string ProductName { get; set; }
        internal string Architecture { get; set; }
        internal string InstalledHotfixes { get; set; }
        internal int Build { get; set; }
        internal int Revision { get; set; }
        internal int FixedRevision { get; set; }
        internal string BuildVersion { get; set; }
        internal string FixedVersion { get; set; }
        internal string ApplicableKb { get; set; }
        internal WindowsInstallerVersionStatus VersionStatus { get; set; }
        internal string MsiDllPath { get; set; }
        internal string MsiDllFileVersion { get; set; }
        internal string MsiDllProductVersion { get; set; }
        internal string MsiDllCompany { get; set; }
        internal long MsiDllSize { get; set; }
        internal DateTime MsiDllLastWriteUtc { get; set; }
        internal int TempPackagesTotal { get; set; }
        internal List<TempPackageEntry> TempPackages { get; private set; }
        internal bool InstallerFoldersHasConfigMsi { get; set; }
        internal string ConfigMsiPath { get; set; }
        internal bool ConfigMsiExists { get; set; }
        internal string ConfigMsiOwner { get; set; }
        internal bool ConfigMsiUnusualOwner { get; set; }
        internal bool ConfigMsiNullDacl { get; set; }
        internal DateTime ConfigMsiCreationUtc { get; set; }
        internal DateTime ConfigMsiLastWriteUtc { get; set; }
        internal bool ConfigMsiRecentlyChanged { get; set; }
        internal List<string> ConfigMsiDangerousAcls { get; private set; }
        internal List<string> RollbackFiles { get; private set; }
        internal List<string> CollectionErrors { get; private set; }
    }
}
