using Microsoft.Win32;
using System;
using System.Security;

namespace winPEAS.Info.SystemInfo
{
    internal enum ClfsAuthenticationStatus
    {
        NotAvailable,
        Enforced,
        Learning,
        LearningWithoutAutoEnforcement,
        DisabledByAdministrator,
        DisabledBySystem,
        AccessDenied,
        Unknown
    }

    internal sealed class ClfsAuthenticationReport
    {
        internal ClfsAuthenticationStatus Status { get; set; }
        internal ulong? Mode { get; set; }
        internal ulong? EnforcementTransitionPeriod { get; set; }
    }

    internal static class ClfsAuthentication
    {
        internal const string RegistryPath = @"SYSTEM\CurrentControlSet\Services\CLFS\Authentication";

        internal static ClfsAuthenticationReport GetReport()
        {
            try
            {
                // HKLM\SYSTEM is shared between WOW64 registry views, so the process default
                // view reaches the same CLFS configuration on 32-bit and 64-bit executions.
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
                using (var key = baseKey.OpenSubKey(RegistryPath, false))
                {
                    if (key == null)
                    {
                        return new ClfsAuthenticationReport
                        {
                            Status = ClfsAuthenticationStatus.NotAvailable
                        };
                    }

                    object mode = key.GetValue("Mode", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    object transitionPeriod = key.GetValue(
                        "EnforcementTransitionPeriod",
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames);

                    return Evaluate(mode, transitionPeriod);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new ClfsAuthenticationReport
                {
                    Status = ClfsAuthenticationStatus.AccessDenied
                };
            }
            catch (SecurityException)
            {
                return new ClfsAuthenticationReport
                {
                    Status = ClfsAuthenticationStatus.AccessDenied
                };
            }
        }

        internal static ClfsAuthenticationReport Evaluate(object modeValue, object transitionPeriodValue)
        {
            ulong mode;
            ulong transitionPeriod;
            bool hasMode = TryConvertToUInt64(modeValue, out mode);
            bool hasTransitionPeriod = TryConvertToUInt64(transitionPeriodValue, out transitionPeriod);

            var report = new ClfsAuthenticationReport
            {
                Status = ClfsAuthenticationStatus.Unknown,
                Mode = hasMode ? (ulong?)mode : null,
                EnforcementTransitionPeriod = hasTransitionPeriod ? (ulong?)transitionPeriod : null
            };

            if (!hasMode)
            {
                return report;
            }

            switch (mode)
            {
                case 0:
                    report.Status = ClfsAuthenticationStatus.Enforced;
                    break;
                case 1:
                    report.Status = hasTransitionPeriod && transitionPeriod == 0
                        ? ClfsAuthenticationStatus.LearningWithoutAutoEnforcement
                        : ClfsAuthenticationStatus.Learning;
                    break;
                case 2:
                    report.Status = ClfsAuthenticationStatus.DisabledByAdministrator;
                    break;
                case 3:
                    report.Status = ClfsAuthenticationStatus.DisabledBySystem;
                    break;
            }

            return report;
        }

        private static bool TryConvertToUInt64(object value, out ulong result)
        {
            result = 0;
            if (value is int intValue && intValue >= 0)
            {
                result = (ulong)intValue;
                return true;
            }

            if (value is long longValue && longValue >= 0)
            {
                result = (ulong)longValue;
                return true;
            }

            if (value is uint uintValue)
            {
                result = uintValue;
                return true;
            }

            if (value is ulong ulongValue)
            {
                result = ulongValue;
                return true;
            }

            return false;
        }
    }
}
