using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace winPEAS.Info.ApplicationInfo
{
    internal enum Pdf24RepairAssessment
    {
        UnknownVersion,
        FixedVersion,
        MissingRepairEvidence,
        ConditionalLead
    }

    internal sealed class Pdf24RepairEvidence
    {
        internal string RegistryVersion;
        internal string BinaryVersion;
        internal bool MsiRegistered;
        internal bool ReadablePackage;
        internal bool RepairUiHidden;
        internal bool InteractiveSession;
    }

    internal static class Pdf24RepairPrerequisites
    {
        private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string UserDataPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";
        private static readonly Version FixedVersion = new Version(11, 15, 2);

        internal static bool IsProduct(string name)
        {
            return !string.IsNullOrEmpty(name) && name.Length <= 128 &&
                Regex.IsMatch(name.Trim(), @"^PDF24 Creator(?: \([^)]+\))?$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        internal static Version ParseVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 32 ||
                !Regex.IsMatch(value.Trim(), @"^[0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?$"))
                return null;

            Version version;
            return Version.TryParse(value.Trim(), out version) ? version : null;
        }

        internal static Pdf24RepairAssessment Assess(Pdf24RepairEvidence evidence)
        {
            Version registry = ParseVersion(evidence.RegistryVersion);
            Version binary = ParseVersion(evidence.BinaryVersion);
            if (registry == null && binary == null)
                return Pdf24RepairAssessment.UnknownVersion;

            // Conflicting installed-file and uninstall versions need manual review.
            if (registry != null && binary != null &&
                Normalize(registry).CompareTo(Normalize(binary)) != 0)
                return Pdf24RepairAssessment.UnknownVersion;

            Version version = binary ?? registry;
            if (Normalize(version).CompareTo(Normalize(FixedVersion)) >= 0)
                return Pdf24RepairAssessment.FixedVersion;

            return evidence.MsiRegistered && evidence.ReadablePackage &&
                !evidence.RepairUiHidden && evidence.InteractiveSession
                ? Pdf24RepairAssessment.ConditionalLead
                : Pdf24RepairAssessment.MissingRepairEvidence;
        }

        private static Version Normalize(Version version)
        {
            return new Version(version.Major, version.Minor, version.Build,
                version.Revision < 0 ? 0 : version.Revision);
        }

        internal static IEnumerable<Pdf24RepairEvidence> Collect()
        {
            var results = new List<Pdf24RepairEvidence>();
            var locations = new[]
            {
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry64),
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry32),
                Tuple.Create(RegistryHive.CurrentUser, RegistryView.Default)
            };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var location in locations)
            {
                RegistryKey hive = null;
                RegistryKey uninstall = null;
                try
                {
                    hive = RegistryKey.OpenBaseKey(location.Item1, location.Item2);
                    uninstall = hive.OpenSubKey(UninstallPath);
                    if (uninstall == null) continue;
                    string[] names = uninstall.GetSubKeyNames();
                    for (int i = 0; i < names.Length && i < 4096; i++)
                    {
                        try
                        {
                            using (RegistryKey app = uninstall.OpenSubKey(names[i]))
                            {
                                if (app == null || !IsProduct(Convert.ToString(app.GetValue("DisplayName"))))
                                    continue;
                                string identity = location.Item1 + ":" + location.Item2 + ":" + names[i];
                                if (!seen.Add(identity)) continue;
                                results.Add(CollectEntry(app, names[i]));
                            }
                        }
                        catch (Exception)
                        {
                            // One unreadable entry must not stop the installed-app inventory.
                        }
                    }
                }
                catch (Exception)
                {
                    // Registry views can be unavailable or inaccessible.
                }
                finally
                {
                    if (uninstall != null) uninstall.Dispose();
                    if (hive != null) hive.Dispose();
                }
            }
            return results;
        }

        private static Pdf24RepairEvidence CollectEntry(RegistryKey app, string keyName)
        {
            var evidence = new Pdf24RepairEvidence
            {
                RegistryVersion = Convert.ToString(app.GetValue("DisplayVersion")),
                MsiRegistered = Convert.ToString(app.GetValue("WindowsInstaller")) == "1",
                RepairUiHidden = Convert.ToString(app.GetValue("NoRepair")) == "1",
                InteractiveSession = IsInteractiveSession()
            };

            string installLocation = CleanLocalPath(Convert.ToString(app.GetValue("InstallLocation")));
            if (IsLocalPath(installLocation))
            {
                string binaryPath = Path.Combine(installLocation, "pdf24.exe");
                try
                {
                    if (File.Exists(binaryPath))
                        evidence.BinaryVersion = FileVersionInfo.GetVersionInfo(binaryPath).FileVersion;
                }
                catch (Exception) { }
            }

            var packages = new List<string> { Convert.ToString(app.GetValue("LocalPackage")) };
            Guid productCode;
            if (Guid.TryParse(keyName, out productCode))
            {
                string packed = PackGuid(productCode);
                foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using (RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                        using (RegistryKey users = machine.OpenSubKey(UserDataPath))
                        {
                            if (users == null) continue;
                            evidence.MsiRegistered |= AddRegisteredPackage(machine, packages, "S-1-5-18", packed);
                            string[] sids = users.GetSubKeyNames();
                            for (int i = 0; i < sids.Length && i < 64; i++)
                                if (sids[i] != "S-1-5-18")
                                    evidence.MsiRegistered |= AddRegisteredPackage(machine, packages, sids[i], packed);
                        }
                    }
                    catch (Exception) { }
                }
            }
            string source = CleanLocalPath(Convert.ToString(app.GetValue("InstallSource")));
            string packageName = Convert.ToString(app.GetValue("PackageName"));
            if (IsLocalPath(source) && !string.IsNullOrEmpty(packageName) &&
                packageName.Length <= 128 && Path.GetFileName(packageName) == packageName)
                packages.Add(Path.Combine(source, packageName));

            foreach (string package in packages)
            {
                if (IsReadableMsi(package))
                {
                    evidence.ReadablePackage = true;
                    break;
                }
            }
            return evidence;
        }

        private static bool AddRegisteredPackage(RegistryKey machine, List<string> packages, string sid, string packed)
        {
            try
            {
                string productPath = UserDataPath + @"\" + sid + @"\Products\" + packed;
                using (RegistryKey key = machine.OpenSubKey(productPath + @"\InstallProperties"))
                {
                    if (key == null) return false;
                    packages.Add(Convert.ToString(key.GetValue("LocalPackage")));
                }
                using (RegistryKey source = machine.OpenSubKey(productPath + @"\SourceList"))
                {
                    if (source != null)
                    {
                        string fileName = Convert.ToString(source.GetValue("PackageName"));
                        string lastSource = Convert.ToString(source.GetValue("LastUsedSource"));
                        if (lastSource != null) lastSource = lastSource.Substring(lastSource.LastIndexOf(';') + 1);
                        lastSource = CleanLocalPath(lastSource);
                        if (IsLocalPath(lastSource) && !string.IsNullOrEmpty(fileName) &&
                            fileName.Length <= 128 && Path.GetFileName(fileName) == fileName)
                            packages.Add(Path.Combine(lastSource, fileName));
                    }
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        // Windows Installer stores the product GUID in its packed registry form.
        internal static string PackGuid(Guid guid)
        {
            string value = guid.ToString("N").ToUpperInvariant();
            string packed = Reverse(value.Substring(0, 8)) + Reverse(value.Substring(8, 4)) +
                Reverse(value.Substring(12, 4));
            for (int i = 16; i < 32; i += 2)
                packed += value[i + 1].ToString() + value[i];
            return packed;
        }

        private static string Reverse(string text)
        {
            char[] chars = text.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private static bool IsLocalPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length > 512) return false;
            path = CleanLocalPath(path);
            return Path.IsPathRooted(path) && !path.StartsWith(@"\\", StringComparison.Ordinal);
        }

        private static string CleanLocalPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? path :
                Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        }

        private static bool IsReadableMsi(string path)
        {
            path = CleanLocalPath(path);
            if (!IsLocalPath(path) || !string.Equals(Path.GetExtension(path), ".msi", StringComparison.OrdinalIgnoreCase))
                return false;
            try
            {
                // Read-only open verifies package availability without invoking Installer.
                using (var file = new FileStream(path,
                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    return file.Length > 0;
            }
            catch (Exception) { return false; }
        }

        private static bool IsInteractiveSession()
        {
            try
            {
                return Environment.UserInteractive && Process.GetCurrentProcess().SessionId > 0;
            }
            catch (Exception) { return false; }
        }
    }
}
