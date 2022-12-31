using Microsoft.Win32;
using System.Collections.Generic;
using winPEAS.Helpers.Registry;
using winPEAS.Native.Enums;

namespace winPEAS.Info.SystemInfo.GroupPolicy
{
    internal class GroupPolicy
    {
        public static IEnumerable<LocalGroupPolicyInfo> GetLocalGroupPolicyInfos()
        {
            // reference - https://specopssoft.com/blog/things-work-group-policy-caching/

            // local machine GPOs
            var basePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\DataStore\Machine\0";
            var machineIDs = RegistryHelper.GetRegSubkeys("HKLM", basePath) ?? new string[] { };

            foreach (var id in machineIDs)
            {
                var settings = RegistryHelper.GetRegValues("HKLM", $"{basePath}\\{id}");

                yield return new LocalGroupPolicyInfo(
                    settings["GPOName"],
                    "machine",
                    settings["DisplayName"],
                    settings["Link"],
                    settings["FileSysPath"],
                    (GPOOptions)settings["Options"],
                    (GPOLink)settings["GPOLink"],
                    settings["Extensions"]
                );
            }

            // local user GPOs
            var userGpOs = new Dictionary<string, Dictionary<string, object>>();

            var sids = Registry.Users.GetSubKeyNames();
            foreach (var sid in sids)
            {
                if (!sid.StartsWith("S-1-5") || sid.EndsWith("_Classes"))
                {
                    continue;
                }

                var extensions = RegistryHelper.GetRegSubkeys("HKU", $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History");
                if ((extensions == null) || (extensions.Length == 0))
                {
                    continue;
                }

                foreach (var extension in extensions)
                {
                    var path = $"{sid}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Group Policy\\History\\{extension}";
                    var userIDs = RegistryHelper.GetRegSubkeys("HKU", path) ?? new string[] { };

                    foreach (var id in userIDs)
                    {
                        var settings = RegistryHelper.GetRegValues("HKU", $"{path}\\{id}");

                        if (userGpOs.ContainsKey($"{settings["GPOName"]}"))
                        {
                            continue;
                        }

                        userGpOs.Add($"{settings["GPOName"]}", settings);
                    }
                }
            }

            foreach (var userGPO in userGpOs)
            {
                yield return new LocalGroupPolicyInfo(
                    userGPO.Value["GPOName"],
                    "user",
                    userGPO.Value["DisplayName"],
                    userGPO.Value["Link"],
                    userGPO.Value["FileSysPath"],
                    (GPOOptions)userGPO.Value["Options"],
                    (GPOLink)userGPO.Value["GPOLink"],
                    userGPO.Value["Extensions"]
                );
            }
        }
    }
}
