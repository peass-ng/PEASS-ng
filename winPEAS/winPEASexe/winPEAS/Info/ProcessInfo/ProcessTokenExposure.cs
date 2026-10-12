using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using winPEAS.Helpers;
using winPEAS.Info.UserInfo.Token;
using winPEAS.Native;
using winPEAS.Native.Classes;
using winPEAS.Native.Enums;
using winPEAS.Native.Structs;

namespace winPEAS.Info.ProcessInfo
{
    internal enum ProcessTokenAccountKind
    {
        Other,
        System,
        BuiltInService,
        DomainService
    }

    internal sealed class ProcessTokenService
    {
        internal string Name { get; set; }
        internal string Account { get; set; }
    }

    internal sealed class ProcessTokenExposureInfo
    {
        internal int ProcessId { get; set; }
        internal string ProcessName { get; set; }
        internal string ExecutablePath { get; set; }
        internal string TokenUser { get; set; }
        internal string TokenSid { get; set; }
        internal string LogonSession { get; set; }
        internal TokenIntegrityState Integrity { get; set; } = TokenIntegrityState.Unknown;
        internal ProcessTokenAccountKind AccountKind { get; set; }
        internal List<ProcessTokenService> Services { get; } = new List<ProcessTokenService>();
        internal bool ProcessQueryAccess { get; set; }
        internal bool TokenQueryAccess { get; set; }
        internal bool TokenDuplicateAccess { get; set; }
        internal bool TokenImpersonateAccess { get; set; }
        internal bool TokenAssignPrimaryAccess { get; set; }
        internal bool IsDifferentUser { get; set; }

        internal bool IsHigherValueIdentity =>
            AccountKind != ProcessTokenAccountKind.Other ||
            Integrity == TokenIntegrityState.High ||
            Integrity == TokenIntegrityState.System ||
            Integrity == TokenIntegrityState.Protected;

        // Opening a token with TOKEN_DUPLICATE is a useful lead, but exploitation still
        // depends on token type, privileges, session, process protections and target behavior.
        internal bool IsConditionalTokenAbuseCandidate =>
            IsDifferentUser && IsHigherValueIdentity && TokenDuplicateAccess;

        internal bool ShouldDisplay =>
            TokenQueryAccess && (Services.Count > 0 || IsHigherValueIdentity ||
                (IsDifferentUser && (TokenDuplicateAccess || TokenImpersonateAccess || TokenAssignPrimaryAccess)));

        internal string AccessSummary =>
            "process-query=" + YesNo(ProcessQueryAccess) +
            ", token-query=" + YesNo(TokenQueryAccess) +
            ", duplicate=" + YesNo(TokenDuplicateAccess) +
            ", impersonate=" + YesNo(TokenImpersonateAccess) +
            ", assign-primary=" + YesNo(TokenAssignPrimaryAccess);

        private static string YesNo(bool value) => value ? "yes" : "no";
    }

    internal sealed class ProcessTokenExposureReport
    {
        internal List<ProcessTokenExposureInfo> Processes { get; } = new List<ProcessTokenExposureInfo>();
        internal int Enumerated { get; set; }
        internal int ProcessQueryDenied { get; set; }
        internal int TokenQueryDenied { get; set; }
        internal bool ProcessLimitReached { get; set; }
        internal bool TimeLimitReached { get; set; }
        internal bool ServiceInventoryUnavailable { get; set; }
    }

    internal static class ProcessTokenExposure
    {
        internal const int MaxProcesses = 1024;
        internal const int MaxInspectionMilliseconds = 2500;

        private const string LocalSystemSid = "S-1-5-18";
        private const string LocalServiceSid = "S-1-5-19";
        private const string NetworkServiceSid = "S-1-5-20";

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenStatistics
        {
            internal winPEAS.Native.Structs.LUID TokenId;
            internal winPEAS.Native.Structs.LUID AuthenticationId;
            internal long ExpirationTime;
            internal int TokenType;
            internal int ImpersonationLevel;
            internal uint DynamicCharged;
            internal uint DynamicAvailable;
            internal uint GroupCount;
            internal uint PrivilegeCount;
            internal winPEAS.Native.Structs.LUID ModifiedId;
        }

        private sealed class ProcessDescriptor
        {
            internal int Id;
            internal string Name;
        }

        internal static ProcessTokenExposureReport Inspect()
        {
            var report = new ProcessTokenExposureReport();
            Dictionary<int, List<ProcessTokenService>> services = GetRunningServices(report);
            List<ProcessDescriptor> processes = GetProcesses();
            string currentSid = GetCurrentSid();
            int currentProcessId;
            using (Process currentProcess = Process.GetCurrentProcess())
                currentProcessId = currentProcess.Id;
            var timer = Stopwatch.StartNew();
            var translatedAccounts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (ProcessDescriptor process in processes.OrderBy(p => p.Id))
            {
                if (report.Enumerated >= MaxProcesses)
                {
                    report.ProcessLimitReached = true;
                    break;
                }
                if (timer.ElapsedMilliseconds >= MaxInspectionMilliseconds)
                {
                    report.TimeLimitReached = true;
                    break;
                }

                ++report.Enumerated;
                var info = new ProcessTokenExposureInfo
                {
                    ProcessId = process.Id,
                    ProcessName = Clean(process.Name, 128),
                    ExecutablePath = "unavailable"
                };
                if (services.TryGetValue(process.Id, out List<ProcessTokenService> processServices))
                    info.Services.AddRange(processServices);

                InspectProcess(info, currentSid, currentProcessId, translatedAccounts);
                if (!info.ProcessQueryAccess)
                    ++report.ProcessQueryDenied;
                else if (!info.TokenQueryAccess)
                    ++report.TokenQueryDenied;
                report.Processes.Add(info);
            }

            return report;
        }

        private static List<ProcessDescriptor> GetProcesses()
        {
            var results = new List<ProcessDescriptor>();
            Process[] processes = Array.Empty<Process>();
            try
            {
                processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    try
                    {
                        results.Add(new ProcessDescriptor { Id = process.Id, Name = process.ProcessName });
                    }
                    catch { }
                }
            }
            catch { }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
            return results;
        }

        private static Dictionary<int, List<ProcessTokenService>> GetRunningServices(ProcessTokenExposureReport report)
        {
            var results = new Dictionary<int, List<ProcessTokenService>>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\cimv2",
                    "SELECT Name, ProcessId, StartName FROM Win32_Service WHERE ProcessId > 0"))
                using (ManagementObjectCollection collection = searcher.Get())
                {
                    foreach (ManagementObject service in collection)
                    {
                        try
                        {
                            int processId = Convert.ToInt32(service["ProcessId"]);
                            if (processId <= 0) continue;
                            if (!results.TryGetValue(processId, out List<ProcessTokenService> entries))
                            {
                                entries = new List<ProcessTokenService>();
                                results[processId] = entries;
                            }
                            entries.Add(new ProcessTokenService
                            {
                                Name = Clean(Convert.ToString(service["Name"]), 128),
                                Account = Clean(Convert.ToString(service["StartName"]), 128)
                            });
                        }
                        catch { }
                        finally { service.Dispose(); }
                    }
                }
            }
            catch
            {
                report.ServiceInventoryUnavailable = true;
            }
            return results;
        }

        private static void InspectProcess(ProcessTokenExposureInfo info, string currentSid,
            int currentProcessId, IDictionary<string, string> translatedAccounts)
        {
            IntPtr processHandle = Kernel32.OpenProcess(HandlesHelper.ProcessAccessFlags.QueryLimitedInformation,
                false, info.ProcessId);
            if (processHandle == IntPtr.Zero)
                processHandle = Kernel32.OpenProcess(HandlesHelper.ProcessAccessFlags.QueryInformation,
                    false, info.ProcessId);
            if (processHandle == IntPtr.Zero)
                return;

            info.ProcessQueryAccess = true;
            try
            {
                info.ExecutablePath = ReadExecutablePath(processHandle);
                info.TokenDuplicateAccess = CanOpenToken(processHandle, AccessTypes.TokenDuplicate);
                info.TokenImpersonateAccess = CanOpenToken(processHandle, AccessTypes.TokenImpersonate);
                info.TokenAssignPrimaryAccess = CanOpenToken(processHandle, AccessTypes.TokenAssignPrimary);

                SafeTokenHandle token;
                if (!Advapi32.OpenProcessToken(processHandle, AccessTypes.TokenQuery, out token))
                    return;

                using (token)
                {
                    info.TokenQueryAccess = true;
                    info.TokenSid = ReadTokenSid(token);
                    info.TokenUser = TranslateAccount(info.TokenSid, translatedAccounts);
                    info.LogonSession = ReadLogonSession(token);
                    info.Integrity = CurrentProcessToken.ReadIntegrity(token);
                }

                info.AccountKind = ClassifyAccount(info.TokenSid, info.Services);
                info.IsDifferentUser = info.ProcessId != currentProcessId &&
                    !string.IsNullOrEmpty(info.TokenSid) && !string.IsNullOrEmpty(currentSid) &&
                    !string.Equals(info.TokenSid, currentSid, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Kernel32.CloseHandle(processHandle);
            }
        }

        private static bool CanOpenToken(IntPtr processHandle, AccessTypes access)
        {
            SafeTokenHandle token;
            if (!Advapi32.OpenProcessToken(processHandle, access, out token))
                return false;
            token.Dispose();
            return true;
        }

        private static string ReadExecutablePath(IntPtr processHandle)
        {
            try
            {
                var path = new StringBuilder(32768);
                int length = path.Capacity;
                if (Kernel32.QueryFullProcessImageName(processHandle, 0, path, ref length) && length > 0)
                    return Clean(path.ToString(), 1024);
            }
            catch { }
            return "unavailable";
        }

        private static string ReadTokenSid(SafeTokenHandle token)
        {
            IntPtr buffer;
            int length;
            if (!CurrentProcessToken.TryRead(token, TOKEN_INFORMATION_CLASS.TokenUser, out buffer, out length))
                return string.Empty;
            try
            {
                if (length < Marshal.SizeOf(typeof(SID_AND_ATTRIBUTES))) return string.Empty;
                var user = (SID_AND_ATTRIBUTES)Marshal.PtrToStructure(buffer, typeof(SID_AND_ATTRIBUTES));
                return user.Sid == IntPtr.Zero ? string.Empty : new SecurityIdentifier(user.Sid).Value;
            }
            catch { return string.Empty; }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static string ReadLogonSession(SafeTokenHandle token)
        {
            IntPtr buffer;
            int length;
            if (!CurrentProcessToken.TryRead(token, TOKEN_INFORMATION_CLASS.TokenStatistics, out buffer, out length))
                return "unknown";
            try
            {
                if (length < Marshal.SizeOf(typeof(TokenStatistics))) return "unknown";
                var statistics = (TokenStatistics)Marshal.PtrToStructure(buffer, typeof(TokenStatistics));
                return string.Format("0x{0:X8}:0x{1:X8}",
                    unchecked((uint)statistics.AuthenticationId.HighPart), statistics.AuthenticationId.LowPart);
            }
            catch { return "unknown"; }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static string TranslateAccount(string sid, IDictionary<string, string> cache)
        {
            if (string.IsNullOrEmpty(sid)) return "unknown";
            if (cache.TryGetValue(sid, out string account)) return account;
            try
            {
                account = new SecurityIdentifier(sid).Translate(typeof(NTAccount)).Value;
            }
            catch
            {
                account = "unresolved";
            }
            cache[sid] = Clean(account, 256);
            return cache[sid];
        }

        internal static ProcessTokenAccountKind ClassifyAccount(string sid,
            IEnumerable<ProcessTokenService> services)
        {
            if (string.Equals(sid, LocalSystemSid, StringComparison.OrdinalIgnoreCase))
                return ProcessTokenAccountKind.System;
            if (string.Equals(sid, LocalServiceSid, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sid, NetworkServiceSid, StringComparison.OrdinalIgnoreCase))
                return ProcessTokenAccountKind.BuiltInService;
            if (services != null && services.Any(service => IsDomainServiceAccount(service.Account)))
                return ProcessTokenAccountKind.DomainService;
            return ProcessTokenAccountKind.Other;
        }

        internal static bool IsDomainServiceAccount(string account)
        {
            if (string.IsNullOrWhiteSpace(account)) return false;
            account = account.Trim();
            if (account.IndexOf('@') > 0) return true;
            int separator = account.IndexOf('\\');
            if (separator <= 0) return false;
            string domain = account.Substring(0, separator);
            return !string.Equals(domain, ".", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(domain, "localhost", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(domain, "NT AUTHORITY", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(domain, "NT SERVICE", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(domain, "BUILTIN", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCurrentSid()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    return identity.User == null ? string.Empty : identity.User.Value;
            }
            catch { return string.Empty; }
        }

        private static string Clean(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string cleaned = new string(value.Select(c => char.IsControl(c) ? '?' : c).ToArray()).Trim();
            return cleaned.Length <= maximumLength ? cleaned : cleaned.Substring(0, maximumLength) + "...";
        }
    }
}
