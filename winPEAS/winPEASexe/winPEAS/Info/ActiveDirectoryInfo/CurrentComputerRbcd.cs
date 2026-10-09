using System;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace winPEAS.Info.ActiveDirectoryInfo
{
    internal enum CurrentComputerRbcdStatus { Absent, Present, Inaccessible, Malformed, Oversized }

    internal sealed class CurrentComputerRbcdReport
    {
        internal CurrentComputerRbcdStatus Status { get; set; }
        internal IList<string> TrusteeSids { get; set; } = new List<string>();
        internal int AceCount { get; set; }
        internal bool Truncated { get; set; }
    }

    // This is a state indicator only. ACE trustees do not establish usable delegation.
    internal static class CurrentComputerRbcd
    {
        internal const int MaxDescriptorBytes = 16384;
        internal const int MaxAcesInspected = 128;
        internal const int MaxTrusteeSids = 16;

        internal static CurrentComputerRbcdReport Evaluate(byte[] bytes)
        {
            if (bytes == null)
                return new CurrentComputerRbcdReport { Status = CurrentComputerRbcdStatus.Absent };
            if (bytes.Length > MaxDescriptorBytes)
                return new CurrentComputerRbcdReport { Status = CurrentComputerRbcdStatus.Oversized };
            if (bytes.Length == 0)
                return new CurrentComputerRbcdReport { Status = CurrentComputerRbcdStatus.Malformed };

            try
            {
                var descriptor = new RawSecurityDescriptor(bytes, 0);
                if ((descriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) == 0 ||
                    descriptor.DiscretionaryAcl == null)
                    return new CurrentComputerRbcdReport { Status = CurrentComputerRbcdStatus.Malformed };

                var report = new CurrentComputerRbcdReport
                {
                    Status = CurrentComputerRbcdStatus.Present,
                    AceCount = descriptor.DiscretionaryAcl.Count
                };
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int inspected = Math.Min(report.AceCount, MaxAcesInspected);
                for (int i = 0; i < inspected; i++)
                {
                    var qualified = descriptor.DiscretionaryAcl[i] as QualifiedAce;
                    string sid = qualified?.SecurityIdentifier?.Value;
                    if (sid == null || !seen.Add(sid))
                        continue;
                    if (report.TrusteeSids.Count == MaxTrusteeSids)
                    {
                        report.Truncated = true;
                        break;
                    }
                    report.TrusteeSids.Add(sid);
                }
                report.Truncated |= report.AceCount > MaxAcesInspected;
                return report;
            }
            catch (Exception)
            {
                return new CurrentComputerRbcdReport { Status = CurrentComputerRbcdStatus.Malformed };
            }
        }
    }
}
