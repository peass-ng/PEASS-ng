using System;
using System.Collections.Generic;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo.WindowsDefender
{
    internal class WindowsDefenderSettings
    {
        public IList<string> PathExclusions { get; }
        public IList<string> PolicyManagerPathExclusions { get; }
        public IList<string> ProcessExclusions { get; }
        public IList<string> ExtensionExclusions { get; }
        public AsrSettings AsrSettings { get; }

        public WindowsDefenderSettings(string defenderBaseKeyPath)
        {
            PathExclusions = new List<string>();
            var pathExclusionData = RegistryHelper.GetRegValues("HKLM", $"{defenderBaseKeyPath}\\Exclusions\\Paths");
            if (pathExclusionData != null)
            {
                foreach (var kvp in pathExclusionData)
                {
                    PathExclusions.Add(kvp.Key);
                }
            }

            PolicyManagerPathExclusions = new List<string>();
            var excludedPaths = RegistryHelper.GetRegValue("HKLM", $"{defenderBaseKeyPath}\\Policy Manager", "ExcludedPaths");
            if (excludedPaths != null)
            {
                foreach (var s in excludedPaths.Split('|'))
                {
                    PolicyManagerPathExclusions.Add(s);
                }
            }

            ProcessExclusions = new List<string>();
            var processExclusionData = RegistryHelper.GetRegValues("HKLM", $"{defenderBaseKeyPath}\\Exclusions\\Processes");
            if (processExclusionData != null)
            {
                foreach (var kvp in processExclusionData)
                {
                    ProcessExclusions.Add(kvp.Key);
                }
            }

            ExtensionExclusions = new List<string>();
            var extensionExclusionData = RegistryHelper.GetRegValues("HKLM", $"{defenderBaseKeyPath}\\Exclusions\\Extensions");
            if (extensionExclusionData != null)
            {
                foreach (var kvp in extensionExclusionData)
                {
                    ExtensionExclusions.Add(kvp.Key);
                }
            }

            var asrKeyPath = $"{defenderBaseKeyPath}\\Windows Defender Exploit Guard\\ASR";
            var asrEnabled = RegistryHelper.GetRegValue("HKLM", asrKeyPath, "ExploitGuard_ASR_Rules");

            AsrSettings = new AsrSettings(
                !string.IsNullOrEmpty(asrEnabled) && (asrEnabled != "0")
                );

            var rules = RegistryHelper.GetRegValues("HKLM", $"{asrKeyPath}\\Rules");
            if (rules != null)
            {
                foreach (var value in rules)
                {
                    AsrSettings.Rules.Add(new AsrRule(
                        new Guid(value.Key),
                        int.Parse((string)value.Value)
                    ));
                }
            }

            var exclusions = RegistryHelper.GetRegValues("HKLM", $"{asrKeyPath}\\ASROnlyExclusions");
            if (exclusions != null)
            {
                foreach (var value in exclusions)
                {
                    AsrSettings.Exclusions.Add(value.Key);
                }
            }
        }
    }
}
