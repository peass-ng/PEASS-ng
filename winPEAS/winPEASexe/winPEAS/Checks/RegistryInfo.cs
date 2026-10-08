using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.Checks
{
    internal class RegistryInfo : ISystemCheck
    {
        private const string TypingInsightsRelativePath = @"Software\Microsoft\Input\TypingInsights";

        private static readonly string[] KnownWritableSystemKeyCandidates = new[]
        {
            @"SOFTWARE\Microsoft\CoreShell",
            @"SOFTWARE\Microsoft\DRM",
            @"SOFTWARE\Microsoft\Input\Locales",
            @"SOFTWARE\Microsoft\Input\Settings",
            @"SOFTWARE\Microsoft\Shell\Oobe",
            @"SOFTWARE\Microsoft\Shell\Session",
            @"SOFTWARE\Microsoft\Tracing",
            @"SOFTWARE\Microsoft\Windows\UpdateApi",
            @"SOFTWARE\Microsoft\WindowsUpdate\UX",
            @"SOFTWARE\WOW6432Node\Microsoft\DRM",
            @"SOFTWARE\WOW6432Node\Microsoft\Tracing",
            @"SYSTEM\Software\Microsoft\TIP",
            @"SYSTEM\ControlSet001\Control\Cryptography\WebSignIn\Navigation",
            @"SYSTEM\ControlSet001\Control\MUI\StringCacheSettings",
            @"SYSTEM\ControlSet001\Control\USB\AutomaticSurpriseRemoval",
            @"SYSTEM\ControlSet001\Services\BTAGService\Parameters\Settings",
        };

        private static readonly string[] ScanBasePaths = new[]
        {
            @"SOFTWARE\Microsoft",
            @"SOFTWARE\WOW6432Node\Microsoft",
            @"SYSTEM\CurrentControlSet\Services",
            @"SYSTEM\CurrentControlSet\Control",
            @"SYSTEM\ControlSet001\Control",
        };

        private static readonly string[] ContextMenuClasses = new[]
        {
            "*", "Directory", "Directory\\Background", "Drive", "Folder",
        };

        private const int MaxHandlersPerClass = 64;
        private const int MaxWritableContextMenuServers = 25;

        public string[] MitreAttackIds { get; } = new[] { "T1012", "T1574.011", "T1056.001" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Registry permissions for hive exploitation", "T1012,T1574.011,T1056.001");

            new List<Action>
            {
                PrintTypingInsightsPermissions,
                PrintKnownSystemWritableKeys,
                PrintWritableContextMenuServers,
                PrintHeuristicWritableKeys,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private void PrintTypingInsightsPermissions()
        {
            Beaprint.MainPrint("Cross-user TypingInsights key (HKCU/HKU)", "T1056.001");

            var matches = new List<RegistryWritableKeyInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (RegistryAclScanner.TryGetWritableKey("HKCU", TypingInsightsRelativePath, out var currentUserKey))
            {
                if (seen.Add(currentUserKey.FullPath))
                {
                    matches.Add(currentUserKey);
                }
            }

            foreach (var sid in RegistryHelper.GetUserSIDs())
            {
                if (string.IsNullOrEmpty(sid) || sid.Equals(".DEFAULT", StringComparison.OrdinalIgnoreCase) || sid.EndsWith("_Classes", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relativePath = $"{sid}\\{TypingInsightsRelativePath}";
                if (RegistryAclScanner.TryGetWritableKey("HKU", relativePath, out var info) && seen.Add(info.FullPath))
                {
                    matches.Add(info);
                }
            }

            if (matches.Count == 0)
            {
                Beaprint.GrayPrint("  [-] TypingInsights key does not grant write access to low-privileged groups.");
                return;
            }

            PrintEntries(matches);
            Beaprint.LinkPrint("https://projectzero.google/2025/05/the-windows-registry-adventure-8-exploitation.html", "Writable TypingInsights enables cross-user hive tampering and DoS.");
        }

        private void PrintKnownSystemWritableKeys()
        {
            Beaprint.MainPrint("Known HKLM descendants writable by standard users", "T1574.011");

            var matches = new List<RegistryWritableKeyInfo>();
            foreach (var path in KnownWritableSystemKeyCandidates)
            {
                if (RegistryAclScanner.TryGetWritableKey("HKLM", path, out var info))
                {
                    matches.Add(info);
                }
            }

            if (matches.Count == 0)
            {
                Beaprint.GrayPrint("  [-] None of the tracked HKLM keys are writable by low-privileged groups.");
                return;
            }

            PrintEntries(matches);
        }

        private void PrintHeuristicWritableKeys()
        {
            Beaprint.MainPrint("Sample of additional writable HKLM keys (depth-limited scan)", "T1574.011");

            var matches = RegistryAclScanner.ScanWritableKeys("HKLM", ScanBasePaths, maxDepth: 3, maxResults: 25);
            if (matches.Count == 0)
            {
                Beaprint.GrayPrint("  [-] No additional writable HKLM keys were found within the sampled paths.");
                return;
            }

            PrintEntries(matches);
            Beaprint.GrayPrint("  [*] Showing up to 25 entries from the sampled paths to avoid noisy output.");
        }

        private static void PrintWritableContextMenuServers()
        {
            Beaprint.MainPrint("Potentially writable context menu COM servers (HKLM)", "T1574.011");
            Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/privilege-escalation-with-autorun-binaries.html", "Trace context menu CLSIDs to DLLs and confirm a privileged trigger.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int findings = 0;
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    {
                        foreach (var className in ContextMenuClasses)
                        {
                            try
                            {
                                string handlersPath = @"SOFTWARE\Classes\" + className + @"\shellex\ContextMenuHandlers";
                                using (var handlers = machine.OpenSubKey(handlersPath))
                                {
                                    if (handlers == null)
                                    {
                                        continue;
                                    }

                                    string[] handlerNames = handlers.GetSubKeyNames();
                                    foreach (var handlerName in handlerNames.Take(MaxHandlersPerClass))
                                    {
                                        try
                                        {
                                            using (var handler = handlers.OpenSubKey(handlerName))
                                            {
                                                string registration = handler?.GetValue("") as string;
                                                if (!Guid.TryParse(string.IsNullOrWhiteSpace(registration) ? handlerName : registration, out var clsid))
                                                {
                                                    continue;
                                                }

                                                string serverPath = @"SOFTWARE\Classes\CLSID\" + clsid.ToString("B").ToUpperInvariant() + @"\InprocServer32";
                                                string identity = view + ":" + serverPath;
                                                if (!seen.Add(identity))
                                                {
                                                    continue;
                                                }

                                                using (var server = machine.OpenSubKey(serverPath))
                                                {
                                                    string dll = server?.GetValue("") as string;
                                                    string principal;
                                                    if (string.IsNullOrWhiteSpace(dll) || !CurrentUserCanSetRegistryValue(server, out principal))
                                                    {
                                                        continue;
                                                    }

                                                    Beaprint.BadPrint($"  [!] HKLM\\{serverPath} ({view}) -> {dll}");
                                                    Beaprint.GrayPrint($"      Context menu: HKLM\\{handlersPath}\\{handlerName}; SetValue: {principal}");
                                                    findings++;
                                                    if (findings >= MaxWritableContextMenuServers)
                                                    {
                                                        Beaprint.GrayPrint("  [*] Showing up to 25 context menu COM server candidates.");
                                                        return;
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // A missing or inaccessible registration should not hide other handlers.
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // A missing or inaccessible class should not hide other classes.
                            }
                        }
                    }
                }
                catch
                {
                    // The 64-bit view is unavailable on 32-bit Windows.
                }
            }

            if (findings == 0)
            {
                Beaprint.GrayPrint("  [-] No matching COM server ACLs found in the sampled context menu registrations.");
            }
        }

        private static bool CurrentUserCanSetRegistryValue(RegistryKey key, out string principal)
        {
            principal = null;
            if (key == null || Checks.CurrentUserSiDs.Count == 0)
            {
                return false;
            }

            foreach (RegistryAccessRule rule in key.GetAccessControl(AccessControlSections.Access)
                .GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                if ((rule.RegistryRights & RegistryRights.SetValue) == 0 ||
                    (rule.PropagationFlags & PropagationFlags.InheritOnly) != 0 ||
                    !Checks.CurrentUserSiDs.TryGetValue(rule.IdentityReference.Value, out var name))
                {
                    continue;
                }

                if (rule.AccessControlType == AccessControlType.Deny)
                {
                    principal = null;
                    return false;
                }

                principal = string.IsNullOrEmpty(name) ? rule.IdentityReference.Value : name;
            }

            return principal != null;
        }

        private static void PrintEntries(IEnumerable<RegistryWritableKeyInfo> entries)
        {
            foreach (var entry in entries)
            {
                var principals = string.Join(", ", entry.Principals);
                var rights = entry.Rights.Count > 0 ? string.Join(", ", entry.Rights.Distinct(StringComparer.OrdinalIgnoreCase)) : "Write access";
                var displayPath = string.IsNullOrEmpty(entry.FullPath) ? $"{entry.Hive}\\{entry.RelativePath}" : entry.FullPath;
                Beaprint.BadPrint($"  [!] {displayPath} -> {principals} ({rights})");
            }
        }
    }
}
