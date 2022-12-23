using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.Info.UserInfo.SAM;
using winPEAS.Native;
using winPEAS.Native.Enums;

//Configuring Fody: https://tech.trailmax.info/2014/01/bundling-all-your-assemblies-into-one-or-alternative-to-ilmerge/
//I have also created the folder Costura32 and Costura64 with the respective Dlls of Colorful.Console

namespace winPEAS.Info.UserInfo
{
    class UserInfoHelper
    {
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

        public static List<Dictionary<string, string>> GetRDPSessions()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            // adapted from http://www.pinvoke.net/default.aspx/wtsapi32.wtsenumeratesessions
            IntPtr server = IntPtr.Zero;
            List<String> ret = new List<string>();
            server = OpenServer("localhost");

            try
            {
                IntPtr ppSessionInfo = IntPtr.Zero;

                Int32 count = 0;
                Int32 level = 1;
                Int32 retval = Wtsapi32.WTSEnumerateSessionsEx(server, ref level, 0, ref ppSessionInfo, ref count);
                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO_1));
                Int64 current = (Int64)ppSessionInfo;

                if (retval != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Dictionary<string, string> rdp_session = new Dictionary<string, string>();
                        WTS_SESSION_INFO_1 si = (WTS_SESSION_INFO_1)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO_1));
                        current += dataSize;
                        if (si.pUserName == null || si.pUserName == "")
                            continue;

                        rdp_session["SessionID"] = string.Format("{0}", si.SessionID);
                        rdp_session["pSessionName"] = string.Format("{0}", si.pSessionName);
                        rdp_session["pUserName"] = string.Format("{0}", si.pUserName);
                        rdp_session["pDomainName"] = string.Format("{0}", si.pDomainName);
                        rdp_session["State"] = string.Format("{0}", si.State);
                        rdp_session["SourceIP"] = "";

                        // Now use WTSQuerySessionInformation to get the remote IP (if any) for the connection
                        IntPtr addressPtr = IntPtr.Zero;
                        uint bytes = 0;

                        Wtsapi32.WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientAddress, out addressPtr, out bytes);
                        WTS_CLIENT_ADDRESS address = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure((System.IntPtr)addressPtr, typeof(WTS_CLIENT_ADDRESS));

                        if (address.Address[2] != 0)
                        {
                            string sourceIP = string.Format("{0}.{1}.{2}.{3}", address.Address[2], address.Address[3], address.Address[4], address.Address[5]);
                            rdp_session["SourceIP"] = string.Format("{0}", sourceIP);
                        }
                        results.Add(rdp_session);
                    }

                    Wtsapi32.WTSFreeMemory(ppSessionInfo);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
            }
            finally
            {
                CloseServer(server);
            }
            return results;
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

        public static Dictionary<string, string> GetAutoLogon()
        {
            Dictionary<string, string> results = new Dictionary<string, string>
            {
                ["DefaultDomainName"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultDomainName"),
                ["DefaultUserName"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultUserName"),
                ["DefaultPassword"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultPassword"),
                ["AltDefaultDomainName"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultDomainName"),
                ["AltDefaultUserName"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultUserName"),
                ["AltDefaultPassword"] = RegistryHelper.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultPassword")
            };
            return results;
        }

        // From: https://stackoverflow.com/questions/35867427/read-text-from-clipboard
        public static string GetClipboardText()
        {
            string c = "";
            try
            {
                if (Clipboard.ContainsText(TextDataFormat.Text))
                    c = Clipboard.GetText(TextDataFormat.Text);

                else if (Clipboard.ContainsText(TextDataFormat.Html))
                    c = Clipboard.GetText(TextDataFormat.Html);

                else if (Clipboard.ContainsAudio())
                    c = $"{Clipboard.GetAudioStream()}";

                else if (Clipboard.ContainsFileDropList())
                    c = $"{Clipboard.GetFileDropList()}";

                //else if (Clipboard.ContainsImage()) //No system.Drwing import
                //c = string.Format("{0}", Clipboard.GetImage());
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
            }
            return c;
        }
    }
}
