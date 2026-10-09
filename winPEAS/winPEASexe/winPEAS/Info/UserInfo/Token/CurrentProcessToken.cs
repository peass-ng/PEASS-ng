using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using winPEAS.Native;
using winPEAS.Native.Classes;
using winPEAS.Native.Enums;
using winPEAS.Native.Structs;

namespace winPEAS.Info.UserInfo.Token
{
    // These values describe this process token, not the account's other logons or tokens.
    internal enum TokenGroupState { Unknown, Absent, Enabled, DenyOnly, Disabled }
    internal enum TokenElevationState { Unknown, Default, Full, Limited }
    internal enum TokenIntegrityState { Unknown, Untrusted, Low, Medium, High, System, Protected }

    internal sealed class CurrentProcessTokenSnapshot
    {
        internal TokenIntegrityState Integrity { get; set; } = TokenIntegrityState.Unknown;
        internal TokenElevationState Elevation { get; set; } = TokenElevationState.Unknown;
        internal TokenGroupState Administrators { get; set; } = TokenGroupState.Unknown;
        internal TokenGroupState LocalAdministratorAccount { get; set; } = TokenGroupState.Unknown;
        internal bool? EnableLUA { get; set; }
        internal int? ConsentPromptBehaviorAdmin { get; set; }

        internal bool IsFilteredLocalAdminCandidate =>
            Integrity == TokenIntegrityState.Medium &&
            Elevation == TokenElevationState.Limited &&
            Administrators == TokenGroupState.DenyOnly &&
            EnableLUA == true;

        internal string Summary =>
            "Integrity: " + Integrity + "; elevation type: " + Elevation +
            "; Administrators SID: " + Administrators +
            "; local administrator account SID: " + LocalAdministratorAccount +
            "; EnableLUA: " + (EnableLUA.HasValue ? (EnableLUA.Value ? "enabled" : "disabled") : "unknown") +
            "; ConsentPromptBehaviorAdmin: " + (ConsentPromptBehaviorAdmin.HasValue ? ConsentPromptBehaviorAdmin.Value.ToString() : "unknown");
    }

    internal static class CurrentProcessToken
    {
        private const string AdministratorsSid = "S-1-5-32-544";
        private const string LocalAdministratorSid = "S-1-5-114";
        private const uint GroupEnabled = 0x00000004;
        private const uint GroupDenyOnly = 0x00000010;
        private const string UacPolicyKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";

        [StructLayout(LayoutKind.Sequential)]
        private struct TokenGroupsHeader
        {
            public uint Count;
            public SID_AND_ATTRIBUTES FirstGroup;
        }

        internal static CurrentProcessTokenSnapshot Read()
        {
            var snapshot = new CurrentProcessTokenSnapshot();
            try
            {
                // FromCurrentProcess currently locks a null static handle. A fresh handle also
                // avoids sharing a disposable token between checks.
                using (SafeTokenHandle handle = SafeTokenHandle.FromProcess(Kernel32.GetCurrentProcess(), AccessTypes.TokenQuery))
                {
                    snapshot.Elevation = ReadElevation(handle);
                    snapshot.Integrity = ReadIntegrity(handle);
                    ReadGroups(handle, snapshot);
                }
            }
            catch
            {
                // Individual failed queries retain their unknown default values.
            }

            ReadUacPolicy(snapshot);
            return snapshot;
        }

        private static bool TryRead(SafeTokenHandle handle, TOKEN_INFORMATION_CLASS informationClass, out IntPtr buffer, out int length)
        {
            buffer = IntPtr.Zero;
            length = 0;
            try
            {
                Advapi32.GetTokenInformation(handle, informationClass, IntPtr.Zero, 0, out length);
                if (length <= 0 || length > 1024 * 1024)
                    return false;
                buffer = Marshal.AllocHGlobal(length);
                int returned;
                if (Advapi32.GetTokenInformation(handle, informationClass, buffer, length, out returned) && returned > 0 && returned <= length)
                {
                    length = returned;
                    return true;
                }
            }
            catch { }
            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;
            return false;
        }

        private static TokenElevationState ReadElevation(SafeTokenHandle handle)
        {
            IntPtr buffer;
            int length;
            if (!TryRead(handle, TOKEN_INFORMATION_CLASS.TokenElevationType, out buffer, out length))
                return TokenElevationState.Unknown;
            try
            {
                if (length < sizeof(int)) return TokenElevationState.Unknown;
                switch (Marshal.ReadInt32(buffer))
                {
                    case 1: return TokenElevationState.Default;
                    case 2: return TokenElevationState.Full;
                    case 3: return TokenElevationState.Limited;
                    default: return TokenElevationState.Unknown;
                }
            }
            catch { return TokenElevationState.Unknown; }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static TokenIntegrityState ReadIntegrity(SafeTokenHandle handle)
        {
            IntPtr buffer;
            int length;
            if (!TryRead(handle, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, out buffer, out length))
                return TokenIntegrityState.Unknown;
            try
            {
                if (length < Marshal.SizeOf(typeof(SID_AND_ATTRIBUTES))) return TokenIntegrityState.Unknown;
                var label = (SID_AND_ATTRIBUTES)Marshal.PtrToStructure(buffer, typeof(SID_AND_ATTRIBUTES));
                if (label.Sid == IntPtr.Zero) return TokenIntegrityState.Unknown;
                string sid = new SecurityIdentifier(label.Sid).Value;
                int separator = sid.LastIndexOf('-');
                int rid;
                if (!sid.StartsWith("S-1-16-", StringComparison.Ordinal) ||
                    !int.TryParse(sid.Substring(separator + 1), out rid))
                    return TokenIntegrityState.Unknown;
                if (rid < 0x1000) return TokenIntegrityState.Untrusted;
                if (rid < 0x2000) return TokenIntegrityState.Low;
                if (rid < 0x3000) return TokenIntegrityState.Medium;
                if (rid < 0x4000) return TokenIntegrityState.High;
                if (rid < 0x5000) return TokenIntegrityState.System;
                return TokenIntegrityState.Protected;
            }
            catch { return TokenIntegrityState.Unknown; }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static void ReadGroups(SafeTokenHandle handle, CurrentProcessTokenSnapshot snapshot)
        {
            IntPtr buffer;
            int length;
            if (!TryRead(handle, TOKEN_INFORMATION_CLASS.TokenGroups, out buffer, out length))
                return;
            try
            {
                int offset = (int)Marshal.OffsetOf(typeof(TokenGroupsHeader), "FirstGroup");
                int stride = Marshal.SizeOf(typeof(SID_AND_ATTRIBUTES));
                if (length < offset) return;
                int count = Marshal.ReadInt32(buffer);
                if (count < 0 || count > (length - offset) / stride) return;
                snapshot.Administrators = TokenGroupState.Absent;
                snapshot.LocalAdministratorAccount = TokenGroupState.Absent;
                for (int i = 0; i < count; i++)
                {
                    var group = (SID_AND_ATTRIBUTES)Marshal.PtrToStructure(IntPtr.Add(buffer, offset + i * stride), typeof(SID_AND_ATTRIBUTES));
                    if (group.Sid == IntPtr.Zero) continue;
                    string sid = new SecurityIdentifier(group.Sid).Value;
                    TokenGroupState state = (group.Attributes & GroupDenyOnly) != 0 ? TokenGroupState.DenyOnly :
                        (group.Attributes & GroupEnabled) != 0 ? TokenGroupState.Enabled : TokenGroupState.Disabled;
                    if (sid == AdministratorsSid) snapshot.Administrators = state;
                    if (sid == LocalAdministratorSid) snapshot.LocalAdministratorAccount = state;
                }
            }
            catch
            {
                snapshot.Administrators = TokenGroupState.Unknown;
                snapshot.LocalAdministratorAccount = TokenGroupState.Unknown;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        private static void ReadUacPolicy(CurrentProcessTokenSnapshot snapshot)
        {
            try
            {
                RegistryView view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
                using (RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (RegistryKey key = machine.OpenSubKey(UacPolicyKey))
                {
                    if (key == null) return;
                    object enabled = key.GetValue("EnableLUA");
                    if (enabled is int && ((int)enabled == 0 || (int)enabled == 1))
                        snapshot.EnableLUA = (int)enabled == 1;
                    object prompt = key.GetValue("ConsentPromptBehaviorAdmin");
                    if (prompt is int && (int)prompt >= 0 && (int)prompt <= 5)
                        snapshot.ConsentPromptBehaviorAdmin = (int)prompt;
                }
            }
            catch { /* Missing or inaccessible policy is unknown. */ }
        }
    }
}
