using System;
using System.Collections.Generic;
using System.Linq;
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

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Registry permissions for hive exploitation");

            new List<Action>
            {
                PrintTypingInsightsPermissions,
                PrintKnownSystemWritableKeys,
                PrintHeuristicWritableKeys,
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private void PrintTypingInsightsPermissions()
        {
            Beaprint.MainPrint("Cross-user TypingInsights key (HKCU/HKU)");

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
            Beaprint.MainPrint("Known HKLM descendants writable by standard users");

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
            Beaprint.MainPrint("Sample of additional writable HKLM keys (depth-limited scan)");

            var matches = RegistryAclScanner.ScanWritableKeys("HKLM", ScanBasePaths, maxDepth: 3, maxResults: 25);
            if (matches.Count == 0)
            {
                Beaprint.GrayPrint("  [-] No additional writable HKLM keys were found within the sampled paths.");
                return;
            }

            PrintEntries(matches);
            Beaprint.GrayPrint("  [*] Showing up to 25 entries from the sampled paths to avoid noisy output.");
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
