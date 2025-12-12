using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.Native;

namespace winPEAS.Info.SystemInfo.NamedPipes
{
    internal static class NamedPipeSecurityAnalyzer
    {
        private const string DeviceNamedPipePrefix = @"\Device\NamedPipe\";
        private static readonly char[] CandidateSeparators = { '\\', '/', '-', ':', '(' };

        private static readonly HashSet<string> LowPrivSidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "S-1-1-0",      // Everyone
            "S-1-5-11",     // Authenticated Users
            "S-1-5-32-545", // Users
            "S-1-5-32-546", // Guests
            "S-1-5-32-547", // Power Users
            "S-1-5-32-554", // Pre-Windows 2000 Compatible Access
            "S-1-5-32-555", // Remote Desktop Users
            "S-1-5-32-558", // Performance Log Users
            "S-1-5-32-559", // Performance Monitor Users
            "S-1-5-32-562", // Distributed COM Users
            "S-1-5-32-569", // Remote Management Users
            "S-1-5-4",      // Interactive
            "S-1-5-2",      // Network
            "S-1-5-1",      // Dialup
            "S-1-5-7"       // Anonymous Logon
        };

        private static readonly HashSet<string> LowPrivPrincipalKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "everyone",
            "authenticated users",
            "users",
            "guests",
            "power users",
            "remote desktop users",
            "remote management users",
            "distributed com users",
            "anonymous logon",
            "interactive",
            "network",
            "local",
            "batch",
            "iis_iusrs"
        };

        private static readonly HashSet<string> PrivilegedSidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "S-1-5-18",      // SYSTEM
            "S-1-5-19",      // LOCAL SERVICE
            "S-1-5-20",      // NETWORK SERVICE
            "S-1-5-32-544"   // Administrators
        };

        private static readonly (string Label, FileSystemRights Right)[] DangerousRightsMap = new[]
        {
            ("FullControl", FileSystemRights.FullControl),
            ("Modify", FileSystemRights.Modify),
            ("Write", FileSystemRights.Write),
            ("WriteData", FileSystemRights.WriteData),
            ("AppendData", FileSystemRights.AppendData),
            ("CreateFiles", FileSystemRights.CreateFiles),
            ("CreateDirectories", FileSystemRights.CreateDirectories),
            ("WriteAttributes", FileSystemRights.WriteAttributes),
            ("WriteExtendedAttributes", FileSystemRights.WriteExtendedAttributes),
            ("Delete", FileSystemRights.Delete),
            ("ChangePermissions", FileSystemRights.ChangePermissions),
            ("TakeOwnership", FileSystemRights.TakeOwnership)
        };

        public static IEnumerable<NamedPipeSecurityIssue> GetNamedPipeAbuseCandidates()
        {
            var insecurePipes = DiscoverInsecurePipes();
            if (!insecurePipes.Any())
            {
                return Enumerable.Empty<NamedPipeSecurityIssue>();
            }

            AttachProcesses(insecurePipes);

            return insecurePipes.Values
                                .Where(issue => issue.LowPrivilegeAces.Any())
                                .OrderByDescending(issue => issue.HasPrivilegedServer)
                                .ThenBy(issue => issue.Name)
                                .ToList();
        }

        private static Dictionary<string, NamedPipeSecurityIssue> DiscoverInsecurePipes()
        {
            var result = new Dictionary<string, NamedPipeSecurityIssue>(StringComparer.OrdinalIgnoreCase);

            foreach (var pipe in NamedPipes.GetNamedPipeInfos())
            {
                if (string.IsNullOrWhiteSpace(pipe.Sddl) || pipe.Sddl.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var descriptor = new RawSecurityDescriptor(pipe.Sddl);
                    if (descriptor.DiscretionaryAcl == null)
                        continue;

                    foreach (GenericAce ace in descriptor.DiscretionaryAcl)
                    {
                        if (!(ace is CommonAce commonAce))
                            continue;

                        var sid = commonAce.SecurityIdentifier;
                        if (sid == null || !IsLowPrivilegePrincipal(sid))
                            continue;

                        if (!HasDangerousWriteRights(commonAce.AccessMask))
                            continue;

                        var rights = DescribeRights(commonAce.AccessMask).ToList();
                        if (!rights.Any())
                            continue;

                        if (!result.TryGetValue(pipe.Name, out var issue))
                        {
                            issue = new NamedPipeSecurityIssue(pipe.Name, pipe.Sddl, NormalizePipeName(pipe.Name));
                            result[pipe.Name] = issue;
                        }

                        var account = ResolveSidToName(sid);
                        issue.AddLowPrivPrincipal(account, sid.Value, rights);
                    }
                }
                catch
                {
                    // Ignore malformed SDDL strings
                }
            }

            return result;
        }

        private static void AttachProcesses(Dictionary<string, NamedPipeSecurityIssue> insecurePipes)
        {
            if (!insecurePipes.Any())
                return;

            var lookup = BuildLookup(insecurePipes.Values);
            if (!lookup.Any())
                return;

            List<HandlesHelper.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> handles;
            try
            {
                handles = HandlesHelper.GetAllHandlers();
            }
            catch
            {
                return;
            }

            var currentProcess = Kernel32.GetCurrentProcess();
            var processCache = new Dictionary<int, NamedPipeProcessInfo>();

            foreach (var handle in handles)
            {
                IntPtr processHandle = IntPtr.Zero;
                IntPtr duplicatedHandle = IntPtr.Zero;

                try
                {
                    int pid = GetPid(handle);
                    if (pid <= 0)
                        continue;

                    processHandle = Kernel32.OpenProcess(
                        HandlesHelper.ProcessAccessFlags.DupHandle | HandlesHelper.ProcessAccessFlags.QueryLimitedInformation,
                        false,
                        pid);

                    if (processHandle == IntPtr.Zero)
                        continue;

                    if (!Kernel32.DuplicateHandle(processHandle, handle.HandleValue, currentProcess, out duplicatedHandle, 0, false, HandlesHelper.DUPLICATE_SAME_ACCESS))
                        continue;

                    var typeName = HandlesHelper.GetObjectType(duplicatedHandle);
                    if (!string.Equals(typeName, "File", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var objectName = HandlesHelper.GetObjectName(duplicatedHandle);
                    if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith(DeviceNamedPipePrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var normalizedHandleName = NormalizePipeName(objectName.Substring(DeviceNamedPipePrefix.Length));
                    var candidates = GetCandidateKeys(normalizedHandleName);

                    bool matched = false;

                    foreach (var candidate in candidates)
                    {
                        if (!lookup.TryGetValue(candidate, out var matchedIssues))
                            continue;

                        if (!processCache.TryGetValue(pid, out var processInfo))
                        {
                            var raw = HandlesHelper.getProcInfoById(pid);
                            processInfo = new NamedPipeProcessInfo(raw.pid, raw.name, raw.userName, raw.userSid, IsHighPrivilegeAccount(raw.userSid, raw.userName));
                            processCache[pid] = processInfo;
                        }

                        foreach (var issue in matchedIssues)
                        {
                            issue.AddProcess(processInfo);
                        }

                        matched = true;
                        break;
                    }

                    if (!matched)
                        continue;
                }
                catch
                {
                    // Ignore per-handle failures
                }
                finally
                {
                    if (duplicatedHandle != IntPtr.Zero)
                    {
                        Kernel32.CloseHandle(duplicatedHandle);
                    }
                    if (processHandle != IntPtr.Zero)
                    {
                        Kernel32.CloseHandle(processHandle);
                    }
                }
            }
        }

        private static Dictionary<string, List<NamedPipeSecurityIssue>> BuildLookup(IEnumerable<NamedPipeSecurityIssue> issues)
        {
            var lookup = new Dictionary<string, List<NamedPipeSecurityIssue>>(StringComparer.OrdinalIgnoreCase);

            foreach (var issue in issues)
            {
                foreach (var key in GetCandidateKeys(issue.NormalizedName))
                {
                    if (!lookup.TryGetValue(key, out var list))
                    {
                        list = new List<NamedPipeSecurityIssue>();
                        lookup[key] = list;
                    }

                    if (!list.Contains(issue))
                    {
                        list.Add(issue);
                    }
                }
            }

            return lookup;
        }

        private static IEnumerable<string> GetCandidateKeys(string normalizedName)
        {
            if (string.IsNullOrEmpty(normalizedName))
                return Array.Empty<string>();

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                normalizedName
            };

            foreach (var separator in CandidateSeparators)
            {
                var idx = normalizedName.IndexOf(separator);
                if (idx > 0)
                {
                    candidates.Add(normalizedName.Substring(0, idx));
                }
            }

            return candidates;
        }

        private static string NormalizePipeName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return string.Empty;

            var normalized = rawName.Replace('/', '\\').Trim();
            while (normalized.StartsWith("\\", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(1);
            }

            return normalized.ToLowerInvariant();
        }

        private static bool HasDangerousWriteRights(int accessMask)
        {
            var rights = (FileSystemRights)accessMask;
            foreach (var entry in DangerousRightsMap)
            {
                if ((rights & entry.Right) == entry.Right)
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> DescribeRights(int accessMask)
        {
            var rights = (FileSystemRights)accessMask;
            var descriptions = new List<string>();

            foreach (var entry in DangerousRightsMap)
            {
                if ((rights & entry.Right) == entry.Right)
                {
                    descriptions.Add(entry.Label);
                    if (entry.Right == FileSystemRights.FullControl)
                        break;
                }
            }

            if (!descriptions.Any())
            {
                descriptions.Add($"0x{accessMask:x}");
            }

            return descriptions;
        }

        private static bool IsLowPrivilegePrincipal(SecurityIdentifier sid)
        {
            if (sid == null)
                return false;

            if (LowPrivSidSet.Contains(sid.Value))
                return true;

            var accountName = ResolveSidToName(sid);
            if (string.IsNullOrEmpty(accountName))
                return false;

            return LowPrivPrincipalKeywords.Any(keyword => accountName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string ResolveSidToName(SecurityIdentifier sid)
        {
            if (sid == null)
                return string.Empty;

            try
            {
                return sid.Translate(typeof(NTAccount)).Value;
            }
            catch
            {
                return sid.Value;
            }
        }

        private static bool IsHighPrivilegeAccount(string sid, string userName)
        {
            if (!string.IsNullOrEmpty(sid))
            {
                if (PrivilegedSidSet.Contains(sid))
                    return true;

                if (sid.StartsWith("S-1-5-80-", StringComparison.OrdinalIgnoreCase)) // Service SID
                    return true;

                if (sid.StartsWith("S-1-5-82-", StringComparison.OrdinalIgnoreCase)) // AppPool / service-like SIDs
                    return true;
            }

            if (!string.IsNullOrEmpty(userName))
            {
                if (string.Equals(userName, HandlesHelper.elevatedProcess, StringComparison.OrdinalIgnoreCase))
                    return true;

                var normalized = userName.ToUpperInvariant();
                if (normalized.Contains("SYSTEM") || normalized.Contains("LOCAL SERVICE") || normalized.Contains("NETWORK SERVICE"))
                    return true;

                if (normalized.StartsWith("NT SERVICE\\", StringComparison.Ordinal))
                    return true;

                if (normalized.EndsWith("$", StringComparison.Ordinal) && normalized.Contains("\\"))
                    return true;
            }

            return false;
        }

        private static int GetPid(HandlesHelper.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handle)
        {
            unchecked
            {
                if (IntPtr.Size == 4)
                {
                    return (int)handle.UniqueProcessId.ToUInt32();
                }

                return (int)handle.UniqueProcessId.ToUInt64();
            }
        }
    }

    internal class NamedPipeSecurityIssue
    {
        private readonly Dictionary<string, NamedPipePrincipalAccess> _principalAccess = new Dictionary<string, NamedPipePrincipalAccess>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, NamedPipeProcessInfo> _processes = new Dictionary<int, NamedPipeProcessInfo>();

        public NamedPipeSecurityIssue(string name, string sddl, string normalizedName)
        {
            Name = name;
            Sddl = sddl;
            NormalizedName = normalizedName;
        }

        public string Name { get; }
        public string Sddl { get; }
        public string NormalizedName { get; }

        public IReadOnlyCollection<NamedPipePrincipalAccess> LowPrivilegeAces => _principalAccess.Values;
        public IReadOnlyCollection<NamedPipeProcessInfo> Processes => _processes.Values;
        public bool HasPrivilegedServer => _processes.Values.Any(process => process.IsHighPrivilege);

        public void AddLowPrivPrincipal(string principal, string sid, IEnumerable<string> rights)
        {
            if (string.IsNullOrEmpty(sid))
                return;

            if (!_principalAccess.TryGetValue(sid, out var access))
            {
                access = new NamedPipePrincipalAccess(principal, sid);
                _principalAccess[sid] = access;
            }

            access.AddRights(rights);
        }

        public void AddProcess(NamedPipeProcessInfo process)
        {
            if (process == null)
                return;

            if (!_processes.ContainsKey(process.Pid))
            {
                _processes[process.Pid] = process;
            }
        }
    }

    internal class NamedPipePrincipalAccess
    {
        private readonly HashSet<string> _rights = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public NamedPipePrincipalAccess(string principal, string sid)
        {
            Principal = principal;
            Sid = sid;
        }

        public string Principal { get; }
        public string Sid { get; }
        public string RightsDescription => _rights.Count == 0 ? string.Empty : string.Join("|", _rights.OrderBy(r => r));
        public IEnumerable<string> Rights => _rights;

        public void AddRights(IEnumerable<string> rights)
        {
            if (rights == null)
                return;

            foreach (var right in rights)
            {
                if (!string.IsNullOrWhiteSpace(right))
                {
                    _rights.Add(right.Trim());
                }
            }
        }
    }

    internal class NamedPipeProcessInfo
    {
        public NamedPipeProcessInfo(int pid, string processName, string userName, string userSid, bool isHighPrivilege)
        {
            Pid = pid;
            ProcessName = processName;
            UserName = userName;
            UserSid = userSid;
            IsHighPrivilege = isHighPrivilege;
        }

        public int Pid { get; }
        public string ProcessName { get; }
        public string UserName { get; }
        public string UserSid { get; }
        public bool IsHighPrivilege { get; }
    }
}
