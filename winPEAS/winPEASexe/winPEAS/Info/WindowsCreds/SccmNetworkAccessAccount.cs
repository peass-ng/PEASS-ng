using System;
using System.Management;
using System.Runtime.InteropServices;

namespace winPEAS.Info.WindowsCreds
{
    internal enum SccmNetworkAccessAccountStatus
    {
        NotConfigured,
        Configured,
        AccessDenied,
        Unavailable,
    }

    internal sealed class SccmNetworkAccessAccountReport
    {
        public SccmNetworkAccessAccountStatus Status { get; set; }
        public int AccountCount { get; set; }
        public bool LimitReached { get; set; }
    }

    internal static class SccmNetworkAccessAccount
    {
        internal const int MaxAccounts = 10;
        internal const string PolicyNamespace = @"root\ccm\Policy\Machine\ActualConfig";
        internal const string MetadataOnlyQuery = "SELECT SiteSettingsKey FROM CCM_NetworkAccessAccount";

        private const int WbemAccessDenied = unchecked((int)0x80041003);
        private const int Win32AccessDenied = unchecked((int)0x80070005);

        public static SccmNetworkAccessAccountReport GetReport()
        {
            return GetReport(QueryAccountCount);
        }

        internal static SccmNetworkAccessAccountReport GetReport(Func<int> accountCountQuery)
        {
            try
            {
                int accountCount = accountCountQuery();
                if (accountCount <= 0)
                {
                    return new SccmNetworkAccessAccountReport
                    {
                        Status = SccmNetworkAccessAccountStatus.NotConfigured,
                    };
                }

                return new SccmNetworkAccessAccountReport
                {
                    Status = SccmNetworkAccessAccountStatus.Configured,
                    AccountCount = Math.Min(accountCount, MaxAccounts),
                    LimitReached = accountCount > MaxAccounts,
                };
            }
            catch (UnauthorizedAccessException)
            {
                return AccessDeniedReport();
            }
            catch (ManagementException ex) when (
                ex.ErrorCode == ManagementStatus.InvalidNamespace ||
                ex.ErrorCode == ManagementStatus.InvalidClass)
            {
                return new SccmNetworkAccessAccountReport
                {
                    Status = SccmNetworkAccessAccountStatus.NotConfigured,
                };
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.AccessDenied)
            {
                return AccessDeniedReport();
            }
            catch (COMException ex) when (
                ex.ErrorCode == WbemAccessDenied ||
                ex.ErrorCode == Win32AccessDenied)
            {
                return AccessDeniedReport();
            }
            catch
            {
                return new SccmNetworkAccessAccountReport
                {
                    Status = SccmNetworkAccessAccountStatus.Unavailable,
                };
            }
        }

        private static int QueryAccountCount()
        {
            var options = new EnumerationOptions
            {
                DirectRead = true,
                ReturnImmediately = true,
                Rewindable = false,
            };

            using (var searcher = new ManagementObjectSearcher(
                PolicyNamespace,
                MetadataOnlyQuery,
                options))
            using (ManagementObjectCollection accounts = searcher.Get())
            {
                int accountCount = 0;
                foreach (ManagementObject account in accounts)
                {
                    using (account)
                    {
                        accountCount++;
                    }

                    if (accountCount > MaxAccounts)
                    {
                        break;
                    }
                }

                return accountCount;
            }
        }

        private static SccmNetworkAccessAccountReport AccessDeniedReport()
        {
            return new SccmNetworkAccessAccountReport
            {
                Status = SccmNetworkAccessAccountStatus.AccessDenied,
            };
        }
    }
}
