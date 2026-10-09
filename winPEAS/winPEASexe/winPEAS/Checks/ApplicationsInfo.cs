using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Info.ApplicationInfo;
using winPEAS.Info.NetworkInfo;

namespace winPEAS.Checks
{
    internal class ApplicationsInfo : ISystemCheck
    {
        public string[] MitreAttackIds { get; } = new[] { "T1518", "T1547.001", "T1053.005", "T1546.003", "T1068", "T1010", "T1014" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Applications Information", "T1518,T1547.001,T1053.005,T1546.003,T1068,T1010,T1014");

            new List<Action>
            {
                PrintActiveWindow,
                PrintInstalledApps,
                PrintOnlinePackageVulnerabilities,
                PrintAutoRuns,
                PrintScheduled,
                PrintRecallPolicyConfigurationExposure,
                PrintControllableSystemTasks,
                PrintPrivilegedWmiEventConsumers,
                PrintDeviceDrivers,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        void PrintOnlinePackageVulnerabilities()
        {
            if (!Checks.CheckOnlineVulnPackages)
            {
                return;
            }

            try
            {
                Beaprint.MainPrint("Package Vulnerabilities", "T1518");
                Beaprint.LinkPrint("", "Optional HackTricks online lookup enabled with -vulnpackages or all. Output is capped at 50 vulnerable packages.");

                var summary = HackTricksHostChecker.GetPackageVulnerabilities(50);
                if (!string.IsNullOrEmpty(summary.Error))
                {
                    Beaprint.PrintException("    " + summary.Error);
                    return;
                }

                Beaprint.NoColorPrint($"    Online package vulnerabilities found: {summary.Affected} vulnerable package(s), checked {summary.Checked}.");
                if (summary.Lines.Count == 0)
                {
                    Beaprint.GoodPrint("    No vulnerable packages found by the online lookup.");
                    return;
                }

                foreach (var line in summary.Lines)
                {
                    Beaprint.BadPrint("    " + line);
                }

                if (summary.NotShown > 0)
                {
                    Beaprint.NoColorPrint($"    ... {summary.NotShown} more vulnerable package(s) not shown.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintActiveWindow()
        {
            try
            {
                Beaprint.MainPrint("Current Active Window Application", "T1010");
                string title = ApplicationInfoHelper.GetActiveWindowTitle();
                List<string> permsFile = PermissionsHelper.GetPermissionsFile(title, Checks.CurrentUserSiDs);
                List<string> permsFolder = PermissionsHelper.GetPermissionsFolder(title, Checks.CurrentUserSiDs);
                if (permsFile.Count > 0)
                {
                    Beaprint.BadPrint("    " + title);
                    Beaprint.BadPrint("    File Permissions: " + string.Join(",", permsFile));
                }
                else
                {
                    Beaprint.GoodPrint("    " + title);
                }

                if (permsFolder.Count > 0)
                {
                    Beaprint.BadPrint("    Possible DLL Hijacking, folder is writable: " + PermissionsHelper.GetFolderFromString(title));
                    Beaprint.BadPrint("    Folder Permissions: " + string.Join(",", permsFile));
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintInstalledApps()
        {
            try
            {
                Beaprint.MainPrint("Installed Applications --Via Program Files/Uninstall registry--", "T1518");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#applications", "Check if you can modify installed software");
                PrintDockerDesktopVersionRisk();
                PrintCheckmkAgentVersionRisk();
                PrintVeeamBackupVersionRisk();
                PrintPdf24RepairPrerequisites();
                PrintLansweeperConfigMetadata();
                SortedDictionary<string, Dictionary<string, string>> installedAppsPerms = InstalledApps.GetInstalledAppsPerms();
                string format = "    ==>  {0} ({1})";

                foreach (KeyValuePair<string, Dictionary<string, string>> app in installedAppsPerms)
                {
                    if (string.IsNullOrEmpty(app.Value.ToString())) //If empty, nothing found, is good
                    {
                        Beaprint.GoodPrint(app.Key);
                    }
                    else //Then, we need to look deeper
                    {
                        //Checkeamos si la carpeta (que va a existir como subvalor dentro de si misma) debe ser good
                        if (string.IsNullOrEmpty(app.Value[app.Key]))
                        {
                            Beaprint.GoodPrint("    " + app.Key);
                        }
                        else
                        {
                            Beaprint.BadPrint(string.Format("    {0}({1})", app.Key, app.Value[app.Key]));
                            app.Value[app.Key] = ""; //So no reprinted later
                        }

                        //Check the rest of the values to see if we have something to print in red (permissions)
                        foreach (KeyValuePair<string, string> subfolder in app.Value)
                        {
                            if (!string.IsNullOrEmpty(subfolder.Value))
                            {
                                Beaprint.BadPrint(string.Format(format, subfolder.Key, subfolder.Value));
                            }
                        }
                    }
                }
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Beaprint.PrintException(e.Message);
            }
        }

        private static void PrintLansweeperConfigMetadata()
        {
            foreach (LansweeperConfigResult result in LansweeperConfigMetadata.Collect())
            {
                Beaprint.MainPrint("Lansweeper configuration metadata", "T1552.001");
                Beaprint.NoColorPrint("    Website\\web.config: " + result.ConfigState);
                Beaprint.NoColorPrint("    Key\\Encryption.txt: " + result.KeyState);
                Beaprint.NoColorPrint("    Protected connectionStrings: " +
                    (result.ConfigState == ConfigFileState.Readable
                        ? (result.ProtectedConnectionStrings ? "present" : "not detected") : "unknown"));
                if (result.Candidate)
                    Beaprint.BadPrint("    Candidate: application-stored scanning credentials may be recoverable with the required context.");
                Beaprint.GrayPrint("    Database connectivity and decryption context are unknown; no recovery was attempted.");
                if (result.Partial || result.ConfigState == ConfigFileState.TooLarge)
                    Beaprint.GrayPrint("    Partial visibility: file access, size, or time limit prevented complete inspection.");
            }
        }

        private static void PrintPdf24RepairPrerequisites()
        {
            foreach (Pdf24RepairEvidence evidence in Pdf24RepairPrerequisites.Collect())
            {
                Version registry = Pdf24RepairPrerequisites.ParseVersion(evidence.RegistryVersion);
                Version binary = Pdf24RepairPrerequisites.ParseVersion(evidence.BinaryVersion);
                string versions = "registry version " + (registry == null ? "unavailable" : registry.ToString()) +
                    ", binary version " + (binary == null ? "unavailable" : binary.ToString());
                switch (Pdf24RepairPrerequisites.Assess(evidence))
                {
                    case Pdf24RepairAssessment.FixedVersion:
                        Beaprint.GoodPrint("    PDF24 Creator " + versions + ": installed version at or above the 11.15.2 fix boundary; cached package version unverified.");
                        break;
                    case Pdf24RepairAssessment.ConditionalLead:
                        Beaprint.BadPrint("    PDF24 Creator " + versions + ": conditional MSI repair lead; registered installer, readable package, and nonzero interactive-session indicator found.");
                        Beaprint.InfoPrint("    Repair permission, desktop/UI visibility, vulnerable console action, and log-file delay/access remain unverified. No repair or log access was attempted.");
                        break;
                    case Pdf24RepairAssessment.MissingRepairEvidence:
                        Beaprint.InfoPrint("    PDF24 Creator " + versions + ": older-version clue only; MSI registered=" + evidence.MsiRegistered +
                            ", readable package=" + evidence.ReadablePackage + ", current session interactive=" + evidence.InteractiveSession +
                            ", repair UI hidden=" + evidence.RepairUiHidden + ". Repair permission, desktop/UI visibility, vulnerable console action, and log-file delay/access remain unverified.");
                        break;
                    default:
                        Beaprint.InfoPrint("    PDF24 Creator " + versions + ": installer version status unknown or conflicting; no repair conclusion.");
                        break;
                }
            }
        }

        private static void PrintDockerDesktopVersionRisk()
        {
            // CVE-2025-9074 was fixed in Docker Desktop 4.44.3. The uninstall
            // entries are a fast host-side clue; only a container can confirm
            // whether it can reach the Docker Engine API on the Desktop subnet.
            var locations = new[]
            {
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry64),
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry32),
                Tuple.Create(RegistryHive.CurrentUser, RegistryView.Default)
            };

            var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var location in locations)
            {
                try
                {
                    using (var hive = RegistryKey.OpenBaseKey(location.Item1, location.Item2))
                    using (var app = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Docker Desktop"))
                    {
                        if (app == null)
                        {
                            continue;
                        }

                        var versionText = Convert.ToString(app.GetValue("DisplayVersion"))?.Trim();
                        if (string.IsNullOrEmpty(versionText))
                        {
                            if (versions.Add("unknown"))
                            {
                                Beaprint.InfoPrint("    Docker Desktop is installed, but its registry version is missing; check CVE-2025-9074.");
                            }
                            continue;
                        }

                        if (!versions.Add(versionText))
                        {
                            continue;
                        }

                        Version version;
                        if (!Version.TryParse(versionText, out version))
                        {
                            Beaprint.InfoPrint("    Docker Desktop version " + versionText + ": check CVE-2025-9074; registry version could not be compared.");
                        }
                        else if (version.CompareTo(new Version(4, 44, 3)) < 0)
                        {
                            Beaprint.BadPrint("    Docker Desktop " + versionText + " predates the CVE-2025-9074 fix (4.44.3). Running Linux containers may reach the Engine API and access host files; confirm exposure from a container.");
                            Beaprint.LinkPrint("https://book.hacktricks.wiki/en/network-services-pentesting/2375-pentesting-docker.html", "Docker Engine API exposure and verification");
                        }
                        else
                        {
                            Beaprint.GoodPrint("    Docker Desktop " + versionText + " includes the CVE-2025-9074 fix.");
                        }
                    }
                }
                catch (Exception)
                {
                    // Missing or unreadable registry views should not interrupt enumeration.
                }
            }
        }

        internal enum CheckmkVersionStatus
        {
            Unknown,
            Candidate,
            Fixed
        }

        internal static bool IsCheckmkAgentProduct(string displayName)
        {
            // MSI product names include a branch on some releases (for example,
            // "Check MK Agent 2.1"). Keep this anchored to the agent product.
            return !string.IsNullOrWhiteSpace(displayName) && displayName.Length <= 128 && Regex.IsMatch(
                displayName.Trim(),
                @"^(?:Check[_ ]MK|Checkmk) Agent(?: 2\.[0-9]+(?:\.[0-9]+(?:[pb][0-9]+)?)?)?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        internal static CheckmkVersionStatus ClassifyCheckmkAgentVersion(string versionText)
        {
            // A bare branch such as 2.1 or 2.1.0 cannot establish the patch level.
            if (versionText == null || versionText.Length > 64)
            {
                return CheckmkVersionStatus.Unknown;
            }
            Match match = Regex.Match(versionText.Trim(),
                @"^2\.([0-9]+)\.0([pb])([0-9]+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int branch;
            int patch;
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out branch) ||
                !int.TryParse(match.Groups[3].Value, out patch))
            {
                return CheckmkVersionStatus.Unknown;
            }

            bool isPatch = string.Equals(match.Groups[2].Value, "p", StringComparison.OrdinalIgnoreCase);
            if (branch == 0)
            {
                // The vendor lists the 2.0 branch as affected and publishes no fix for it.
                return CheckmkVersionStatus.Candidate;
            }
            if (branch == 1 || branch == 2)
            {
                int fixedPatch = branch == 1 ? 40 : 23;
                return isPatch && patch >= fixedPatch ? CheckmkVersionStatus.Fixed : CheckmkVersionStatus.Candidate;
            }
            if (branch == 3 || branch == 4)
            {
                // The fix was already present in 2.3.0b1 and 2.4.0b1.
                return patch >= 1 || isPatch ? CheckmkVersionStatus.Fixed : CheckmkVersionStatus.Unknown;
            }
            return CheckmkVersionStatus.Unknown;
        }

        private static void PrintCheckmkAgentVersionRisk()
        {
            const string uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            var locations = new[]
            {
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry64),
                Tuple.Create(RegistryHive.LocalMachine, RegistryView.Registry32),
                Tuple.Create(RegistryHive.CurrentUser, RegistryView.Registry64),
                Tuple.Create(RegistryHive.CurrentUser, RegistryView.Registry32)
            };
            var reportedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var location in locations)
            {
                try
                {
                    using (var hive = RegistryKey.OpenBaseKey(location.Item1, location.Item2))
                    using (var uninstall = hive.OpenSubKey(uninstallPath))
                    {
                        if (uninstall == null)
                        {
                            continue;
                        }

                        string[] subkeys = uninstall.GetSubKeyNames();
                        for (int i = 0; i < subkeys.Length && i < 4096; i++)
                        {
                            try
                            {
                                using (var app = uninstall.OpenSubKey(subkeys[i]))
                                {
                                    if (app == null || !IsCheckmkAgentProduct(Convert.ToString(app.GetValue("DisplayName"))))
                                    {
                                        continue;
                                    }

                                    string version = Convert.ToString(app.GetValue("DisplayVersion"));
                                    version = version == null ? "" : version.Trim();
                                    if (!reportedVersions.Add(version))
                                    {
                                        continue;
                                    }

                                    switch (ClassifyCheckmkAgentVersion(version))
                                    {
                                        case CheckmkVersionStatus.Candidate:
                                            Beaprint.BadPrint("    Checkmk Windows agent registry version " + version + " predates the CVE-2024-0670 fix; candidate only. Confirm the installed agent and conditions before treating this as exploitable.");
                                            break;
                                        case CheckmkVersionStatus.Fixed:
                                            Beaprint.GoodPrint("    Checkmk Windows agent registry version " + version + " is at or above the CVE-2024-0670 fixed floor; verify the installed agent version.");
                                            break;
                                        default:
                                            Beaprint.InfoPrint("    Checkmk Windows agent registry version " + (version.Length == 0 ? "missing" : version.Length > 64 ? "unparsable" : version) + ": CVE-2024-0670 status unknown; verify the full agent patch level.");
                                            break;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                // One unreadable uninstall entry must not hide other entries.
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Unsupported or unreadable registry views are optional.
                }
            }
        }

        internal enum VeeamBackupVersionStatus
        {
            Unknown,
            Candidate,
            Fixed
        }

        internal static bool IsVeeamBackupProduct(string displayName)
        {
            return !string.IsNullOrWhiteSpace(displayName) && displayName.Length <= 128 && Regex.IsMatch(
                displayName.Trim(),
                @"^Veeam Backup (?:&|and) Replication(?: (?:Server|[0-9]+(?:a)?(?: P[0-9]{8})?))?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        internal static VeeamBackupVersionStatus ClassifyVeeamBackupVersion(string versionText)
        {
            // KB4424 fixes require the patch token. KB2680 lists the same four-part
            // 11a and 12 builds both before and after those patches.
            if (string.IsNullOrWhiteSpace(versionText) || versionText.Length > 64)
            {
                return VeeamBackupVersionStatus.Unknown;
            }

            Match match = Regex.Match(versionText.Trim(),
                @"^([0-9]{1,2})\.([0-9]{1,2})\.([0-9]{1,2})\.([0-9]{1,5})(?: +P([0-9]{8}))?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                return VeeamBackupVersionStatus.Unknown;
            }

            int major, minor, update, build;
            if (!int.TryParse(match.Groups[1].Value, out major) ||
                !int.TryParse(match.Groups[2].Value, out minor) ||
                !int.TryParse(match.Groups[3].Value, out update) ||
                !int.TryParse(match.Groups[4].Value, out build))
            {
                return VeeamBackupVersionStatus.Unknown;
            }

            string patch = match.Groups[5].Value;
            if (patch.Length != 0)
            {
                DateTime patchDate;
                if (!DateTime.TryParseExact(patch, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out patchDate))
                {
                    return VeeamBackupVersionStatus.Unknown;
                }
            }

            if (major < 11)
            {
                return major >= 5 ? VeeamBackupVersionStatus.Candidate : VeeamBackupVersionStatus.Unknown;
            }
            if (major == 11)
            {
                if (minor == 0 && update == 0)
                {
                    return VeeamBackupVersionStatus.Candidate;
                }
                if (minor != 0 || update != 1)
                {
                    return VeeamBackupVersionStatus.Unknown;
                }
                if (build < 1261)
                {
                    return VeeamBackupVersionStatus.Candidate;
                }
                if (build != 1261)
                {
                    return VeeamBackupVersionStatus.Unknown;
                }
                return patch.Length == 0 ? VeeamBackupVersionStatus.Unknown :
                    string.CompareOrdinal(patch, "20230227") >= 0 ? VeeamBackupVersionStatus.Fixed : VeeamBackupVersionStatus.Candidate;
            }
            if (major == 12)
            {
                if (minor > 0)
                {
                    return VeeamBackupVersionStatus.Fixed;
                }
                if (update != 0)
                {
                    return VeeamBackupVersionStatus.Unknown;
                }
                if (build < 1420)
                {
                    return VeeamBackupVersionStatus.Candidate;
                }
                if (build != 1420 || patch.Length == 0)
                {
                    return VeeamBackupVersionStatus.Unknown;
                }
                return string.CompareOrdinal(patch, "20230223") >= 0 ? VeeamBackupVersionStatus.Fixed : VeeamBackupVersionStatus.Candidate;
            }
            return major >= 13 ? VeeamBackupVersionStatus.Fixed : VeeamBackupVersionStatus.Unknown;
        }

        private static void PrintVeeamBackupVersionRisk()
        {
            const string uninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var uninstall = hive.OpenSubKey(uninstallPath))
                    {
                        if (uninstall == null) continue;
                        string[] subkeys = uninstall.GetSubKeyNames();
                        for (int i = 0; i < subkeys.Length && i < 4096; i++)
                        {
                            try
                            {
                                using (var app = uninstall.OpenSubKey(subkeys[i]))
                                {
                                    if (app == null || !IsVeeamBackupProduct(Convert.ToString(app.GetValue("DisplayName")))) continue;
                                    string version = (Convert.ToString(app.GetValue("DisplayVersion")) ?? "").Trim();
                                    if (!versions.Add(version)) continue;
                                    switch (ClassifyVeeamBackupVersion(version))
                                    {
                                        case VeeamBackupVersionStatus.Candidate:
                                            Beaprint.BadPrint("    Veeam Backup & Replication " + version + ": CVE-2023-27532 version candidate. Confirm the installed patch and Veeam.Backup.Service.exe on TCP 9401.");
                                            break;
                                        case VeeamBackupVersionStatus.Fixed:
                                            Beaprint.GoodPrint("    Veeam Backup & Replication " + version + ": registry reports a release at or beyond the CVE-2023-27532 fixed floor; verify the installed patch on the backup server.");
                                            break;
                                        default:
                                            Beaprint.InfoPrint("    Veeam Backup & Replication registry version " + (version.Length == 0 ? "missing" : version.Length > 64 ? "unparsable" : version.Replace('\r', ' ').Replace('\n', ' ')) + ": CVE-2023-27532 patch status unknown; verify the full build and patch on the backup server.");
                                            break;
                                    }
                                    Beaprint.LinkPrint("https://www.veeam.com/kb4424", "Veeam CVE-2023-27532 patch advisory");
                                    Beaprint.LinkPrint("https://www.veeam.com/kb2680", "Veeam Backup & Replication build and patch history");
                                }
                            }
                            catch (Exception)
                            {
                                // One inaccessible uninstall entry must not hide the others.
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Missing or unreadable registry views are optional.
                }
            }
        }

        private static void PrintAutoRuns()
        {
            try
            {
                Beaprint.MainPrint("Autorun Applications", "T1547.001");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/privilege-escalation-with-autorun-binaries.html", "Check if you can modify other users AutoRuns binaries (Note that is normal that you can modify HKCU registry and binaries indicated there)");
                List<Dictionary<string, string>> apps = AutoRuns.GetAutoRuns(Checks.CurrentUserSiDs);

                foreach (Dictionary<string, string> app in apps)
                {
                    var colorsA = new Dictionary<string, string>
                        {
                            { "FolderPerms:.*", Beaprint.ansi_color_bad },
                            { "FilePerms:.*", Beaprint.ansi_color_bad },
                            { "(Unquoted and Space detected)", Beaprint.ansi_color_bad },
                            { "(PATH Injection)", Beaprint.ansi_color_bad },
                            { "RegPerms: .*", Beaprint.ansi_color_bad },
                            { (app["Folder"].Length > 0) ? app["Folder"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "ouigyevb2uivydi2u3id2ddf3", !string.IsNullOrEmpty(app["interestingFolderRights"]) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { (app["File"].Length > 0) ? app["File"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "adu8v298hfubibuidiy2422r", !string.IsNullOrEmpty(app["interestingFileRights"]) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { (app["Reg"].Length > 0) ? app["Reg"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "o8a7eduia37ibduaunbf7a4g7ukdhk4ua", (app["RegPermissions"].Length > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { "Potentially sensitive file content:", Beaprint.ansi_color_bad },
                        };
                    string line = "";

                    if (!string.IsNullOrEmpty(app["Reg"]))
                    {
                        line += "\n    RegPath: " + app["Reg"];
                    }

                    if (app["RegPermissions"].Length > 0)
                    {
                        line += "\n    RegPerms: " + app["RegPermissions"];
                    }

                    if (!string.IsNullOrEmpty(app["RegKey"]))
                    {
                        line += "\n    Key: " + app["RegKey"];
                    }

                    if (!string.IsNullOrEmpty(app["Folder"]))
                    {
                        line += "\n    Folder: " + app["Folder"];
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(app["Reg"]))
                        {
                            line += "\n    Folder: None (PATH Injection)";
                        }
                    }

                    if (!string.IsNullOrEmpty(app["interestingFolderRights"]))
                    {
                        line += "\n    FolderPerms: " + app["interestingFolderRights"];
                    }

                    string filepath_mod = app["File"].Replace("\"", "").Replace("'", "");
                    if (!string.IsNullOrEmpty(app["File"]))
                    {
                        line += "\n    File: " + filepath_mod;
                    }

                    if (app["isUnquotedSpaced"].ToLower() != "false")
                    {
                        line += $" (Unquoted and Space detected) - {app["isUnquotedSpaced"]}";
                    }

                    if (!string.IsNullOrEmpty(app["interestingFileRights"]))
                    {
                        line += "\n    FilePerms: " + app["interestingFileRights"];
                    }

                    if (app.ContainsKey("sensitiveInfoList") && !string.IsNullOrEmpty(app["sensitiveInfoList"]))
                    {
                        line += "\n    Potentially sensitive file content: " + app["sensitiveInfoList"];
                    }

                    Beaprint.AnsiPrint(line, colorsA);
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintScheduled()
        {
            try
            {
                Beaprint.MainPrint("Scheduled Applications --Non Microsoft--", "T1053.005");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/privilege-escalation-with-autorun-binaries.html", "Check if you can modify other users scheduled binaries");
                ScheduledAppsResult scheduled_apps = ApplicationInfoHelper.GetScheduledAppsNoMicrosoft();
                int snortConfigsInspected = 0;
                bool snortLimitReached = false;
                HashSet<string> snortTokenSids = null;

                foreach (Dictionary<string, string> sapp in scheduled_apps.Apps)
                {
                    var fileRights = new List<string>();
                    var dirRights = new List<string>();
                    foreach (string actionPath in sapp["ActionPath"].Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            string path = actionPath.Trim().Trim('"');
                            foreach (string right in PermissionsHelper.GetPermissionsFile(path, Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT))
                                fileRights.Add(path + ": " + right);
                            string parent = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(parent))
                            {
                                foreach (string right in PermissionsHelper.GetPermissionsFolder(parent, Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT))
                                    dirRights.Add(parent + ": " + right);
                            }
                        }
                        catch (Exception ex)
                        {
                            Beaprint.PrintException($"failed to check scheduled action path: '{actionPath}': {ex.Message}");
                        }
                    }

                    Dictionary<string, string> colorsS = new Dictionary<string, string>()
                    {
                        { "Permissions.*", Beaprint.ansi_color_bad },
                        { "Possible LSA-secret lead.*", Beaprint.ansi_color_bad },
                    };
                    Beaprint.AnsiPrint(FormatScheduledApp(sapp, fileRights, dirRights), colorsS);
                    if (sapp.ContainsKey("SnortExecutable") && sapp["LogonType"] != "Group")
                    {
                        if (snortConfigsInspected >= 3)
                            snortLimitReached = true;
                        else
                        {
                            snortConfigsInspected++;
                            if (snortTokenSids == null)
                                snortTokenSids = PermissionsHelper.GetUnprivilegedTokenSids();
                            string lead = ApplicationInfoHelper.GetSnortModuleDirectoryLead(
                                sapp["SnortExecutable"], sapp["SnortArguments"], sapp["Principal"], snortTokenSids);
                            if (!string.IsNullOrEmpty(lead))
                            {
                                Beaprint.InfoPrint("    Snort dynamic preprocessor directory review: " + lead);
                                Beaprint.GrayPrint("    Configured task principal identifier differs from this token; account equivalence and effective privilege are unverified. Confirm Snort module loading and ACL/share restrictions before treating this as escalation.");
                            }
                        }
                    }
                    Beaprint.PrintLineSeparator();
                }
                if (snortLimitReached)
                    Beaprint.GrayPrint("    Snort configuration review stopped after 3 scheduled actions; remaining Snort actions unknown.");
                if (snortConfigsInspected > 0)
                    Beaprint.GrayPrint("    Snort review reads at most 64 KiB and the first 1024 lines of each config, and checks up to 3 literal module directories; other forms remain unknown.");
                if (scheduled_apps.LimitReached)
                    Beaprint.NoColorPrint($"    Scheduled task listing stopped at its safety limit ({ApplicationInfoHelper.MaxScheduledTasksInspected} tasks, {ApplicationInfoHelper.MaxScheduledFoldersInspected} folders, or {ApplicationInfoHelper.MaxScheduledAppsDisplayed} displayed).");
                if (scheduled_apps.WithoutAuthorLimitReached)
                    Beaprint.NoColorPrint($"    Tasks with unknown author were capped at {ApplicationInfoHelper.MaxScheduledAppsWithoutAuthor}.");
                if (scheduled_apps.BatchFileLimitReached)
                    Beaprint.NoColorPrint($"    Literal batch-to-PowerShell review stopped after {ApplicationInfoHelper.MaxScheduledBatchFilesInspected} local batch files; later batch files remain unknown.");
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        internal static string FormatScheduledApp(Dictionary<string, string> app, IEnumerable<string> fileRights, IEnumerable<string> dirRights)
        {
            var lines = new List<string> { $"    ({app["Author"]}) {app["Name"]}: {app["Action"]}" };
            lines.Add($"    Principal: {app["Principal"]}; Logon type: {app["LogonType"]}; Run level: {app["RunLevel"]}");
            lines.Add("    Action path: " + (string.IsNullOrEmpty(app["ActionPath"]) ? "unknown" : app["ActionPath"].Replace("\n", "\n                 ")));
            string file = string.Join(", ", fileRights ?? new string[0]);
            string directory = string.Join(", ", dirRights ?? new string[0]);
            if (!string.IsNullOrEmpty(file))
                lines.Add("    Permissions file (current user): " + file);
            if (!string.IsNullOrEmpty(directory))
                lines.Add("    Permissions parent directory (current user): " + directory);
            if (!string.IsNullOrEmpty(app["Trigger"]))
                lines.Add("    Trigger: " + app["Trigger"]);
            if (!string.IsNullOrEmpty(app["Description"]))
                lines.Add("    " + app["Description"]);
            if (!string.IsNullOrEmpty(app["CredentialLead"]))
                lines.Add("    " + app["CredentialLead"]);
            return string.Join("\n", lines);
        }

        void PrintControllableSystemTasks()
        {
            try
            {
                Beaprint.MainPrint("Low-privilege control and demand-start review of enabled SYSTEM scheduled tasks", "T1053.005");
                Beaprint.LinkPrint("https://learn.microsoft.com/en-us/windows/win32/taskschd/security-contexts-for-running-tasks", "A writable action target or task DACL can let a low-privilege principal replace what the task runs as SYSTEM.");

                PrivilegedScheduledTaskReport report = PrivilegedScheduledTasks.GetReport();
                if (report.ControlFindings.Count == 0 && report.Findings.Count == 0 && report.DemandStartFindings.Count == 0)
                {
                    Beaprint.GoodPrint($"    No controllable task definitions or writable targets found within {report.TasksInspected} inspected task(s).");
                }

                foreach (PrivilegedScheduledTaskControlFinding finding in report.ControlFindings)
                {
                    Beaprint.BadPrint($"    Task: {finding.TaskPath} ({finding.Principal})");
                    Beaprint.BadPrint($"    Task control: {finding.AccessReason}");
                    Beaprint.PrintLineSeparator();
                }

                foreach (PrivilegedScheduledTaskFinding finding in report.Findings)
                {
                    Beaprint.BadPrint($"    Task: {finding.TaskPath} ({finding.Principal})");
                    Beaprint.NoColorPrint($"    Action: {finding.Executable}");
                    Beaprint.BadPrint($"    Writable target: {finding.TargetPath}");
                    Beaprint.BadPrint($"    Access: {finding.AccessReason}");
                    Beaprint.PrintLineSeparator();
                }

                foreach (PrivilegedScheduledTaskDemandStartFinding finding in report.DemandStartFindings)
                {
                    Beaprint.InfoPrint($"    Review on-demand SYSTEM task: {finding.TaskPath} ({finding.Principal})");
                    Beaprint.NoColorPrint($"    Full action: {finding.Action}");
                    Beaprint.NoColorPrint($"    Script path: {finding.ScriptPath}");
                    Beaprint.NoColorPrint($"    Task execute access: {finding.Trustee}; verify whether the script trusts caller-controlled input before treating this as an escalation path.");
                    Beaprint.PrintLineSeparator();
                }

                if (report.TaskLimitReached)
                {
                    Beaprint.NoColorPrint($"    Task inspection stopped at the safety limit of {PrivilegedScheduledTasks.MaxTasks} tasks.");
                }

                if (report.FolderLimitReached)
                {
                    Beaprint.NoColorPrint($"    Folder inspection stopped at the safety limit of {PrivilegedScheduledTasks.MaxFolders} folders.");
                }

                if (report.FindingLimitReached)
                {
                    Beaprint.NoColorPrint($"    Findings were capped at {PrivilegedScheduledTasks.MaxFindings}.");
                }

                if (report.TargetLimitReached)
                {
                    Beaprint.NoColorPrint($"    Filesystem probes stopped at the safety limit of {PrivilegedScheduledTasks.MaxTargets} targets.");
                }

                if (report.TimeLimitReached)
                {
                    Beaprint.NoColorPrint($"    Inspection stopped at the safety limit of {PrivilegedScheduledTasks.MaxInspectionMilliseconds / 1000} seconds.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintPrivilegedWmiEventConsumers()
        {
            try
            {
                Beaprint.MainPrint("Writable payloads in privileged WMI event consumers", "T1546.003");
                Beaprint.LinkPrint(
                    "https://learn.microsoft.com/en-us/windows/win32/wmisdk/commandlineeventconsumer",
                    "Microsoft warns that CommandLineEventConsumer runs as LocalSystem and that unsecured payloads can be replaced. File-backed ActiveScript consumers are checked too.");

                PrivilegedWmiEventConsumerReport report = PrivilegedWmiEventConsumers.GetReport();
                if (!string.IsNullOrEmpty(report.Error))
                {
                    Beaprint.InfoPrint("    Inspection incomplete: " + report.Error);
                }

                if (report.Findings.Count == 0 && string.IsNullOrEmpty(report.Error))
                {
                    Beaprint.GoodPrint($"    No writable payloads found in {report.BoundConsumersInspected} bound privileged consumer(s).");
                }

                foreach (PrivilegedWmiEventConsumerFinding finding in report.Findings)
                {
                    Beaprint.BadPrint($"    Consumer: {finding.ConsumerName} ({finding.ConsumerType})");
                    Beaprint.BadPrint($"    Writable target: {finding.TargetPath}");
                    Beaprint.BadPrint($"    Access: {finding.AccessReason}");
                    Beaprint.PrintLineSeparator();
                }

                if (report.BindingLimitReached)
                {
                    Beaprint.NoColorPrint($"    Binding inspection was capped at {PrivilegedWmiEventConsumers.MaxBindings}.");
                }

                if (report.ConsumerLimitReached)
                {
                    Beaprint.NoColorPrint($"    Bound-consumer inspection was capped at {PrivilegedWmiEventConsumers.MaxConsumers}.");
                }

                if (report.TargetLimitReached)
                {
                    Beaprint.NoColorPrint($"    Filesystem probes were capped at {PrivilegedWmiEventConsumers.MaxTargets} targets.");
                }

                if (report.FindingLimitReached)
                {
                    Beaprint.NoColorPrint($"    Findings were capped at {PrivilegedWmiEventConsumers.MaxFindings}.");
                }

                if (report.TimeLimitReached)
                {
                    Beaprint.NoColorPrint($"    Inspection stopped at the safety limit of {PrivilegedWmiEventConsumers.MaxInspectionMilliseconds / 1000} seconds.");
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintRecallPolicyConfigurationExposure()
        {
            try
            {
                Beaprint.MainPrint("Microsoft Recall PolicyConfiguration task exposure", "T1068,T1053.005");
                Beaprint.LinkPrint(
                    "https://msrc.microsoft.com/update-guide/vulnerability/CVE-2026-20941",
                    "CVE-2025-60710 and CVE-2026-20941 abused a WNF-triggered SYSTEM task and unsafe privileged directory cleanup. This check is read-only and does not trigger the task.");

                RecallPolicyConfigurationReport report = RecallPolicyConfiguration.GetReport();
                if (!report.TaskPresent)
                {
                    if (report.Status == RecallPolicyConfigurationStatus.InspectionFailed)
                    {
                        Beaprint.InfoPrint("    Unable to query the Recall PolicyConfiguration task; verify it manually.");
                        if (!string.IsNullOrEmpty(report.Error))
                        {
                            Beaprint.InfoPrint("    Inspection detail: " + report.Error);
                        }
                    }
                    else
                    {
                        Beaprint.GoodPrint("    Recall PolicyConfiguration task not present.");
                    }
                    return;
                }

                Beaprint.NoColorPrint("    Task: " + RecallPolicyConfiguration.TaskPath);
                Beaprint.NoColorPrint("    Enabled: " + (report.TaskEnabled.HasValue ? report.TaskEnabled.Value.ToString() : "unknown"));
                Beaprint.NoColorPrint("    OS: " + (string.IsNullOrEmpty(report.ProductName) ? "unknown" : report.ProductName));
                Beaprint.NoColorPrint("    Build: " + report.BuildString);
                Beaprint.NoColorPrint("    SYSTEM principal: " + report.HasSystemPrincipal);
                Beaprint.NoColorPrint("    Expected COM handler: " + report.HasExpectedComHandler);
                Beaprint.NoColorPrint("    Recall WNF trigger: " + report.HasRecallWnfTrigger);
                Beaprint.NoColorPrint("    Expected WNF states: " + report.ExpectedWnfStateCount + "/" + RecallPolicyConfiguration.ExpectedWnfStates.Count);
                Beaprint.NoColorPrint("    WNF states: " + (report.WnfStateNames.Count == 0 ? "none found" : string.Join(", ", report.WnfStateNames)));
                Beaprint.NoColorPrint("    SessionUnlock trigger: " + report.HasSessionUnlockTrigger);
                Beaprint.NoColorPrint("    AllowStartOnDemand: " + (report.AllowStartOnDemand.HasValue ? report.AllowStartOnDemand.Value.ToString() : "unknown"));

                switch (report.Status)
                {
                    case RecallPolicyConfigurationStatus.PotentiallyVulnerable:
                        Beaprint.BadPrint("    Potentially vulnerable to CVE-2025-60710 / CVE-2026-20941: the task markers match and this build predates the final fix.");
                        Beaprint.BadPrint("    Required update: " + report.RequiredUpdate + " (build " + report.RequiredBuild + " or later).");
                        Beaprint.NoColorPrint("    Recall being disabled does not necessarily remove or disable this scheduled task.");
                        break;
                    case RecallPolicyConfigurationStatus.Patched:
                        Beaprint.GoodPrint("    Task markers match, but the OS build includes the final CVE-2026-20941 fix.");
                        break;
                    case RecallPolicyConfigurationStatus.Disabled:
                        Beaprint.GoodPrint("    Task markers match, but the scheduled task is disabled (Microsoft's documented workaround). Verify the security update before re-enabling it.");
                        break;
                    case RecallPolicyConfigurationStatus.NotAffected:
                        Beaprint.GoodPrint("    Task is present, but this OS build is not in the affected Windows 11 24H2/25H2 or Windows Server 2025 range.");
                        break;
                    case RecallPolicyConfigurationStatus.UnexpectedConfiguration:
                        Beaprint.InfoPrint("    The task is present but its SYSTEM/COM/WNF markers do not match the vulnerable configuration.");
                        break;
                    case RecallPolicyConfigurationStatus.UnknownBuild:
                        Beaprint.InfoPrint("    The vulnerable task markers match, but this OS build could not be mapped confidently. Verify patch status manually.");
                        break;
                    case RecallPolicyConfigurationStatus.InspectionFailed:
                        Beaprint.InfoPrint("    The task is present but could not be inspected completely. Verify its XML and patch status manually.");
                        break;
                }

                if (!string.IsNullOrEmpty(report.Error))
                {
                    Beaprint.InfoPrint("    Inspection detail: " + report.Error);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintDeviceDrivers()
        {
            try
            {
                Beaprint.MainPrint("Device Drivers -- candidates for manual review --", "T1014");
                // this link is not very specific, but its the best on hacktricks
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#drivers", "Check 3rd party drivers for known vulnerabilities/rootkits.");

                DeviceDriverInventory inventory = DeviceDrivers.GetDriverInventory();
                Beaprint.InfoPrint("    Driver presence and file permissions do not establish device access or unsafe IOCTL behavior.");
                foreach (DeviceDriverRecord driver in inventory.Drivers)
                {
                    string pathDriver = driver.Path;
                    bool localFile = DeviceDrivers.IsLocalFilePath(pathDriver) && File.Exists(pathDriver);
                    List<string> fileRights = localFile ? PermissionsHelper.GetPermissionsFile(pathDriver, Checks.CurrentUserSiDs) : new List<string>();
                    List<string> dirRights = localFile ? PermissionsHelper.GetPermissionsFolder(Path.GetDirectoryName(pathDriver), Checks.CurrentUserSiDs) : new List<string>();

                    Dictionary<string, string> colorsD = new Dictionary<string, string>()
                        {
                            { "Permissions.*", Beaprint.ansi_color_bad },
                            { "Capcom.sys", Beaprint.ansi_color_bad },
                            { pathDriver.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (fileRights.Count > 0 || dirRights.Count > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                        };


                    string formString = "    {0} - {1} [{2}]: {3}\n    Service: {6}; State: {7}; Start: {8}";
                    if (fileRights.Count > 0)
                    {
                        formString += "\n    Permissions file: {4}";
                    }

                    if (dirRights.Count > 0)
                    {
                        formString += "\n    Permissions folder(DLL Hijacking): {5}";
                    }

                    Beaprint.AnsiPrint(string.Format(formString,
                        string.IsNullOrEmpty(driver.Product) ? "product unavailable" : driver.Product,
                        string.IsNullOrEmpty(driver.Version) ? "version unavailable" : driver.Version,
                        string.IsNullOrEmpty(driver.Company) ? "publisher unavailable" : driver.Company,
                        pathDriver, string.Join(", ", fileRights), string.Join(", ", dirRights),
                        string.IsNullOrEmpty(driver.Name) ? "unknown" : driver.Name,
                        string.IsNullOrEmpty(driver.State) ? "unknown" : driver.State,
                        string.IsNullOrEmpty(driver.StartMode) ? "unknown" : driver.StartMode), colorsD);

                    //If vuln, end with separator
                    if ((fileRights.Count > 0) || (dirRights.Count > 0))
                    {
                        Beaprint.PrintLineSeparator();
                    }
                }
                if (inventory.UnknownOrTruncated)
                    Beaprint.InfoPrint("    Driver inventory partial/unknown: " + inventory.Detail);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }
    }
}
