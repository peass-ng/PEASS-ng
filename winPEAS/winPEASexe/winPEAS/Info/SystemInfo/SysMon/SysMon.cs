using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;

namespace winPEAS.Info.SystemInfo.SysMon
{
    static class SysMon
    {
        public const string NotDefined = "Not Defined";

        public static IEnumerable<SysmonInfo> GetSysMonInfos()
        {
            var paramsKey = @"SYSTEM\CurrentControlSet\Services\SysmonDrv\Parameters";
            uint? regHashAlg = GetUintNullableFromString(RegistryHelper.GetRegValue("HKLM", paramsKey, "HashingAlgorithm"));
            uint? regOptions = GetUintNullableFromString(RegistryHelper.GetRegValue("HKLM", paramsKey, "Options"));
            byte[] regSysmonRules = GetBinaryValueFromRegistry(Registry.LocalMachine, paramsKey, "Rules");
            var installed = false;
            var hashingAlgorithm = (SysmonHashAlgorithm)0;
            var sysmonOptions = (SysmonOptions)0;
            string b64SysmonRules = null;

            if ((regHashAlg != null) || (regOptions != null) || (regSysmonRules != null))
            {
                installed = true;
            }

            if (regHashAlg != null && regHashAlg != 0)
            {
                regHashAlg = regHashAlg & 15; // we only care about the last 4 bits
                hashingAlgorithm = (SysmonHashAlgorithm)regHashAlg;
            }

            if (regOptions != null)
            {
                sysmonOptions = (SysmonOptions)regOptions;
            }

            if (regSysmonRules != null)
            {
                b64SysmonRules = Convert.ToBase64String(regSysmonRules);
            }

            yield return new SysmonInfo(
                installed,
                hashingAlgorithm,
                sysmonOptions,
                b64SysmonRules
            );
        }

        public static IEnumerable<SysmonEventInfo> GetSysMonEventInfos()
        {
            var query = "*[System/EventID=1]";
            EventLogReader logReader;
            try
            {
                var computerName = Environment.GetEnvironmentVariable("COMPUTERNAME");
                logReader = MyUtils.GetEventLogReader("Microsoft-Windows-Sysmon/Operational", query, computerName);
            }
            catch (Exception ex)
            {
                Beaprint.NoColorPrint("      Unable to query Sysmon event logs, Sysmon likely not installed.");
                yield break;
            }

            var i = 0;

            for (var eventDetail = logReader.ReadEvent(); eventDetail != null; eventDetail = logReader.ReadEvent())
            {
                ++i;
                var commandLine = eventDetail.Properties[10].Value.ToString().Trim();
                if (commandLine != "")
                {
                    var userName = eventDetail.Properties[12].Value.ToString().Trim();
                    yield return new SysmonEventInfo
                    {
                        TimeCreated = eventDetail.TimeCreated,
                        EventID = eventDetail.Id,
                        UserName = userName,
                        Match = commandLine
                    };
                }
            }
        }

        private static byte[] GetBinaryValueFromRegistry(RegistryKey registryKey, string paramsKey, string val)
        {
            try
            {
                var key = registryKey.OpenSubKey(paramsKey);

                if (key == null)
                {
                    return null;
                }

                byte[] result = (byte[])key.GetValue(val);

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static uint? GetUintNullableFromString(string str)
        {
            uint? result = null;
            if (uint.TryParse(str, out uint val))
            {
                result = val;
            }

            return result;
        }
    }
}
