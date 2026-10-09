using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using winPEAS.Helpers;
using winPEAS.Info.UserInfo.SAM;
using winPEAS.Native;
using winPEAS.Native.Enums;

//Configuring Fody: https://tech.trailmax.info/2014/01/bundling-all-your-assemblies-into-one-or-alternative-to-ilmerge/
//I have also created the folder Costura32 and Costura64 with the respective Dlls of Colorful.Console

namespace winPEAS.Info.UserInfo
{
    internal enum RdpSessionVisibility { Unknown, Empty, Observed }

    internal sealed class RdpSessionEnumeration
    {
        internal RdpSessionVisibility Visibility { get; set; } = RdpSessionVisibility.Unknown;
        internal List<Dictionary<string, string>> Sessions { get; } = new List<Dictionary<string, string>>();
        internal string FailureReason { get; set; }
    }

    internal enum AutoLogonFinding
    {
        None,
        EnabledWithoutPlaintextPassword,
        PlaintextPassword
    }

    class UserInfoHelper
    {
        private const string WinlogonKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
        private static readonly string[] AutoLogonValueNames =
        {
            "AutoAdminLogon",
            "DefaultDomainName",
            "DefaultUserName",
            "DefaultPassword",
            "AltDefaultDomainName",
            "AltDefaultUserName",
            "AltDefaultPassword"
        };
        private const int ClipboardReadTimeoutMs = 1500;
        private static readonly Dictionary<string, bool> _highPrivAccountCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] _highPrivGroupIndicators = new string[]
        {
            "administrators",
            "domain admins",
            "enterprise admins",
            "schema admins",
            "server operators",
            "account operators",
            "backup operators",
            "dnsadmins",
            "hyper-v administrators"
        };

        // https://stackoverflow.com/questions/5247798/get-list-of-local-computer-usernames-in-windows


        public static string SID2GroupName(string SID)
        {
            //Frist, look in well-known SIDs
            string groupName = SID2GroupNameHelper.StaticSID2GroupName(SID);
            if (!string.IsNullOrEmpty(groupName))
            {
                return groupName;
            }

            //If not well known, search in local or domain (depending on the nature of the user)
            ContextType ct = ContextType.Domain;
            if (Checks.Checks.IsCurrentUserLocal)
            {
                ct = ContextType.Machine;
            }

            try
            {
                groupName = GetSIDGroupName(SID, ct);
            }
            catch (Exception ex)
            {
                //If error, check inside the other one
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}\n    Checking using the other Principal Context", ex.Message));
                try
                {
                    groupName = GetSIDGroupName(SID, ct == ContextType.Machine ? ContextType.Domain : ContextType.Machine);
                    return groupName;
                }
                catch
                {
                    Beaprint.PrintException(ex.Message);
                }
            }

            //If nothing, check inside the other one
            if (string.IsNullOrEmpty(groupName))
            {
                try
                {
                    groupName = GetSIDGroupName(SID, ct == ContextType.Machine ? ContextType.Domain : ContextType.Machine);
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
            }
            return groupName;
        }

        public static string GetSIDGroupName(string SID, ContextType ct)
        {
            string groupName = "";
            try
            {
                var ctx = new PrincipalContext(ct);
                var group = GroupPrincipal.FindByIdentity(ctx, IdentityType.Sid, SID);
                return group.SamAccountName.ToString();
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return groupName;
        }

        public static PrincipalContext GetPrincipalContext()
        {
            PrincipalContext oPrincipalContext = new PrincipalContext(ContextType.Machine);
            return oPrincipalContext;
        }

        public static bool IsHighPrivilegeAccount(string userName, string domain)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            string cacheKey = ($"{domain}\\{userName}").Trim('\\');
            if (_highPrivAccountCache.TryGetValue(cacheKey, out bool cached))
            {
                return cached;
            }

            bool isHighPriv = false;
            try
            {
                string resolvedDomain = string.IsNullOrWhiteSpace(domain) ? Checks.Checks.CurrentUserDomainName : domain;
                List<string> groups = User.GetUserGroups(userName, resolvedDomain);
                foreach (string group in groups)
                {
                    if (IsHighPrivilegeGroup(group))
                    {
                        isHighPriv = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [-] Unable to resolve groups for {0}\\{1}: {2}", domain, userName, ex.Message));
            }

            if (!isHighPriv)
            {
                isHighPriv = string.Equals(userName, "administrator", StringComparison.OrdinalIgnoreCase) || userName.StartsWith("admin", StringComparison.OrdinalIgnoreCase);
            }

            _highPrivAccountCache[cacheKey] = isHighPriv;
            return isHighPriv;
        }

        private static bool IsHighPrivilegeGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                return false;
            }

            foreach (string indicator in _highPrivGroupIndicators)
            {
                if (groupName.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        //From Seatbelt
        public enum WTS_CONNECTSTATE_CLASS
        {
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        public static void CloseServer(IntPtr ServerHandle)
        {
            Wtsapi32.WTSCloseServer(ServerHandle);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_ADDRESS
        {
            public uint AddressFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO_1
        {
            public Int32 ExecEnvId;

            public WTS_CONNECTSTATE_CLASS State;

            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pSessionName;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pHostName;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pUserName;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pDomainName;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pFarmName;
        }

        public static IntPtr OpenServer(String Name)
        {
            IntPtr server = Wtsapi32.WTSOpenServer(Name);
            return server;
        }

        internal static RdpSessionVisibility AssessRdpSessionVisibility(bool enumerationSucceeded, int visibleSessionCount)
        {
            return !enumerationSucceeded ? RdpSessionVisibility.Unknown
                : visibleSessionCount > 0 ? RdpSessionVisibility.Observed : RdpSessionVisibility.Empty;
        }

        internal static RdpSessionEnumeration GetRDPSessions()
        {
            var result = new RdpSessionEnumeration();
            // adapted from http://www.pinvoke.net/default.aspx/wtsapi32.wtsenumeratesessions
            IntPtr server = IntPtr.Zero;
            IntPtr ppSessionInfo = IntPtr.Zero;
            int count = 0;

            try
            {
                server = OpenServer("localhost");
                if (server == IntPtr.Zero)
                {
                    result.FailureReason = "WTSOpenServer error " + Marshal.GetLastWin32Error();
                    return result;
                }

                Int32 level = 1;
                Int32 retval = Wtsapi32.WTSEnumerateSessionsEx(server, ref level, 0, ref ppSessionInfo, ref count);
                if (retval == 0)
                {
                    result.FailureReason = "WTSEnumerateSessionsEx error " + Marshal.GetLastWin32Error();
                    return result;
                }
                if (count > 0 && ppSessionInfo == IntPtr.Zero)
                {
                    result.FailureReason = "WTSEnumerateSessionsEx returned no buffer";
                    return result;
                }

                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO_1));
                Int64 current = (Int64)ppSessionInfo;

                for (int i = 0; i < count; i++)
                {
                    Dictionary<string, string> rdp_session = new Dictionary<string, string>();
                    WTS_SESSION_INFO_1 si = (WTS_SESSION_INFO_1)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO_1));
                    current += dataSize;
                    if (string.IsNullOrEmpty(si.pUserName))
                        continue;

                    rdp_session["SessionID"] = si.SessionID.ToString();
                    rdp_session["pSessionName"] = si.pSessionName ?? "";
                    rdp_session["pUserName"] = si.pUserName;
                    rdp_session["pDomainName"] = si.pDomainName ?? "";
                    rdp_session["State"] = si.State.ToString();
                    rdp_session["SourceIP"] = "";

                    IntPtr addressPtr = IntPtr.Zero;
                    uint bytes = 0;
                    try
                    {
                        if (Wtsapi32.WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientAddress, out addressPtr, out bytes) &&
                            addressPtr != IntPtr.Zero && bytes >= Marshal.SizeOf(typeof(WTS_CLIENT_ADDRESS)))
                        {
                            var address = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure(addressPtr, typeof(WTS_CLIENT_ADDRESS));
                            if (address.AddressFamily == 2 && address.Address != null && address.Address[2] != 0)
                                rdp_session["SourceIP"] = string.Format("{0}.{1}.{2}.{3}", address.Address[2], address.Address[3], address.Address[4], address.Address[5]);
                        }
                    }
                    finally
                    {
                        if (addressPtr != IntPtr.Zero)
                            Wtsapi32.WTSFreeMemory(addressPtr);
                    }
                    result.Sessions.Add(rdp_session);
                }
                result.Visibility = AssessRdpSessionVisibility(true, result.Sessions.Count);
            }
            catch (Exception ex)
            {
                result.Visibility = RdpSessionVisibility.Unknown;
                result.FailureReason = ex.Message;
            }
            finally
            {
                if (ppSessionInfo != IntPtr.Zero)
                    Wtsapi32.WTSFreeMemoryEx(2, ppSessionInfo, count); // WTSTypeSessionInfoLevel1
                if (server != IntPtr.Zero)
                    CloseServer(server);
            }
            return result;
        }

        // https://stackoverflow.com/questions/31464835/how-to-programmatically-check-the-password-must-meet-complexity-requirements-g
        public static List<Dictionary<string, string>> GetPasswordPolicy()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                using (SamServer server = new SamServer(null, SERVER_ACCESS_MASK.SAM_SERVER_ENUMERATE_DOMAINS | SERVER_ACCESS_MASK.SAM_SERVER_LOOKUP_DOMAIN))
                {
                    foreach (string domain in server.EnumerateDomains())
                    {
                        var sid = server.GetDomainSid(domain);
                        var pi = server.GetDomainPasswordInformation(sid);

                        results.Add(new Dictionary<string, string>()
                        {
                            { "Domain", domain },
                            { "SID", string.Format("{0}", sid) },
                            { "MaxPasswordAge", string.Format("{0}", pi.MaxPasswordAge) },
                            { "MinPasswordAge", string.Format("{0}", pi.MinPasswordAge) },
                            { "MinPasswordLength", string.Format("{0}", pi.MinPasswordLength) },
                            { "PasswordHistoryLength", string.Format("{0}", pi.PasswordHistoryLength) },
                            { "PasswordProperties", string.Format("{0}", pi.PasswordProperties) },
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }

        internal static RegistryView GetAutoLogonRegistryView(bool is64BitOperatingSystem)
        {
            return is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
        }

        public static Dictionary<string, string> GetAutoLogon()
        {
            var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Winlogon lives below redirected HKLM\SOFTWARE; use the OS-native
            // view even when this process is 32-bit on a 64-bit host.
            RegistryView view = GetAutoLogonRegistryView(Environment.Is64BitOperatingSystem);
            using (RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
            using (RegistryKey winlogon = localMachine.OpenSubKey(WinlogonKeyPath))
            {
                if (winlogon != null)
                {
                    foreach (string valueName in AutoLogonValueNames)
                    {
                        object value = winlogon.GetValue(valueName);
                        bool isPassword = valueName == "DefaultPassword" || valueName == "AltDefaultPassword";
                        results[valueName] = isPassword
                            ? value as string ?? string.Empty
                            : value == null ? string.Empty : value.ToString();
                    }
                }
            }
            return results;
        }

        internal static AutoLogonFinding ClassifyAutoLogon(IDictionary<string, string> values)
        {
            if (values == null)
            {
                return AutoLogonFinding.None;
            }

            string password;
            string alternatePassword;
            if ((values.TryGetValue("DefaultPassword", out password) && !string.IsNullOrEmpty(password)) ||
                (values.TryGetValue("AltDefaultPassword", out alternatePassword) && !string.IsNullOrEmpty(alternatePassword)))
            {
                return AutoLogonFinding.PlaintextPassword;
            }

            string enabled;
            if (values.TryGetValue("AutoAdminLogon", out enabled) && enabled == "1")
            {
                return AutoLogonFinding.EnabledWithoutPlaintextPassword;
            }

            return AutoLogonFinding.None;
        }

        // From: https://stackoverflow.com/questions/35867427/read-text-from-clipboard
        public static string GetClipboardText()
        {
            string clipboardText = "";
            Exception clipboardException = null;

            Thread clipboardThread = new Thread(() =>
            {
                try
                {
                    if (Clipboard.ContainsText(TextDataFormat.Text))
                        clipboardText = Clipboard.GetText(TextDataFormat.Text);

                    else if (Clipboard.ContainsText(TextDataFormat.Html))
                        clipboardText = Clipboard.GetText(TextDataFormat.Html);

                    else if (Clipboard.ContainsAudio())
                        clipboardText = $"{Clipboard.GetAudioStream()}";

                    else if (Clipboard.ContainsFileDropList())
                        clipboardText = $"{Clipboard.GetFileDropList()}";

                    //else if (Clipboard.ContainsImage()) //No system.Drwing import
                    //clipboardText = string.Format("{0}", Clipboard.GetImage());
                }
                catch (Exception ex)
                {
                    clipboardException = ex;
                }
            });

            clipboardThread.SetApartmentState(ApartmentState.STA);
            clipboardThread.IsBackground = true;
            clipboardThread.Start();

            if (!clipboardThread.Join(ClipboardReadTimeoutMs))
            {
                Beaprint.GrayPrint($"  [X] Clipboard read timed out after {ClipboardReadTimeoutMs}ms");
                return "";
            }

            if (clipboardException != null)
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", clipboardException));

            return clipboardText;
        }
    }
}
