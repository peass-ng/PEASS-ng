using System;
using System.Collections.Generic;
using System.Management;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//Configuring Fody: https://tech.trailmax.info/2014/01/bundling-all-your-assemblies-into-one-or-alternative-to-ilmerge/
//I have also created the folder Costura32 and Costura64 with the respective Dlls of Colorful.Console

namespace winPEAS
{
    public sealed class SamServer : IDisposable
    {
        private IntPtr _handle;

        public SamServer(string name, SERVER_ACCESS_MASK access)
        {
            Name = name;
            Check(SamConnect(new UNICODE_STRING(name), out _handle, access, IntPtr.Zero));
        }

        public string Name { get; }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                SamCloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public void SetDomainPasswordInformation(SecurityIdentifier domainSid, DOMAIN_PASSWORD_INFORMATION passwordInformation)
        {
            if (domainSid == null)
                throw new ArgumentNullException(nameof(domainSid));

            var sid = new byte[domainSid.BinaryLength];
            domainSid.GetBinaryForm(sid, 0);

            Check(SamOpenDomain(_handle, DOMAIN_ACCESS_MASK.DOMAIN_WRITE_PASSWORD_PARAMS, sid, out IntPtr domain));
            IntPtr info = Marshal.AllocHGlobal(Marshal.SizeOf(passwordInformation));
            Marshal.StructureToPtr(passwordInformation, info, false);
            try
            {
                Check(SamSetInformationDomain(domain, DOMAIN_INFORMATION_CLASS.DomainPasswordInformation, info));
            }
            finally
            {
                Marshal.FreeHGlobal(info);
                SamCloseHandle(domain);
            }
        }

        public DOMAIN_PASSWORD_INFORMATION GetDomainPasswordInformation(SecurityIdentifier domainSid)
        {
            if (domainSid == null)
                throw new ArgumentNullException(nameof(domainSid));

            var sid = new byte[domainSid.BinaryLength];
            domainSid.GetBinaryForm(sid, 0);

            Check(SamOpenDomain(_handle, DOMAIN_ACCESS_MASK.DOMAIN_READ_PASSWORD_PARAMETERS, sid, out IntPtr domain));
            var info = IntPtr.Zero;
            try
            {
                Check(SamQueryInformationDomain(domain, DOMAIN_INFORMATION_CLASS.DomainPasswordInformation, out info));
                return (DOMAIN_PASSWORD_INFORMATION)Marshal.PtrToStructure(info, typeof(DOMAIN_PASSWORD_INFORMATION));
            }
            finally
            {
                SamFreeMemory(info);
                SamCloseHandle(domain);
            }
        }

        public SecurityIdentifier GetDomainSid(string domain)
        {
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));

            Check(SamLookupDomainInSamServer(_handle, new UNICODE_STRING(domain), out IntPtr sid));
            return new SecurityIdentifier(sid);
        }

        public IEnumerable<string> EnumerateDomains()
        {
            int cookie = 0;
            while (true)
            {
                var status = SamEnumerateDomainsInSamServer(_handle, ref cookie, out IntPtr info, 1, out int count);
                if (status != NTSTATUS.STATUS_SUCCESS && status != NTSTATUS.STATUS_MORE_ENTRIES)
                    Check(status);

                if (count == 0)
                    break;

                var us = (UNICODE_STRING)Marshal.PtrToStructure(info + IntPtr.Size, typeof(UNICODE_STRING));
                SamFreeMemory(info);
                yield return us.ToString();
                us.Buffer = IntPtr.Zero; // we don't own this one
            }
        }

        private enum DOMAIN_INFORMATION_CLASS
        {
            DomainPasswordInformation = 1,
        }

        [Flags]
        public enum PASSWORD_PROPERTIES
        {
            DOMAIN_PASSWORD_COMPLEX = 0x00000001,
            DOMAIN_PASSWORD_NO_ANON_CHANGE = 0x00000002,
            DOMAIN_PASSWORD_NO_CLEAR_CHANGE = 0x00000004,
            DOMAIN_LOCKOUT_ADMINS = 0x00000008,
            DOMAIN_PASSWORD_STORE_CLEARTEXT = 0x00000010,
            DOMAIN_REFUSE_PASSWORD_CHANGE = 0x00000020,
        }

        [Flags]
        private enum DOMAIN_ACCESS_MASK
        {
            DOMAIN_READ_PASSWORD_PARAMETERS = 0x00000001,
            DOMAIN_WRITE_PASSWORD_PARAMS = 0x00000002,
            DOMAIN_READ_OTHER_PARAMETERS = 0x00000004,
            DOMAIN_WRITE_OTHER_PARAMETERS = 0x00000008,
            DOMAIN_CREATE_USER = 0x00000010,
            DOMAIN_CREATE_GROUP = 0x00000020,
            DOMAIN_CREATE_ALIAS = 0x00000040,
            DOMAIN_GET_ALIAS_MEMBERSHIP = 0x00000080,
            DOMAIN_LIST_ACCOUNTS = 0x00000100,
            DOMAIN_LOOKUP = 0x00000200,
            DOMAIN_ADMINISTER_SERVER = 0x00000400,
            DOMAIN_ALL_ACCESS = 0x000F07FF,
            DOMAIN_READ = 0x00020084,
            DOMAIN_WRITE = 0x0002047A,
            DOMAIN_EXECUTE = 0x00020301
        }

        [Flags]
        public enum SERVER_ACCESS_MASK
        {
            SAM_SERVER_CONNECT = 0x00000001,
            SAM_SERVER_SHUTDOWN = 0x00000002,
            SAM_SERVER_INITIALIZE = 0x00000004,
            SAM_SERVER_CREATE_DOMAIN = 0x00000008,
            SAM_SERVER_ENUMERATE_DOMAINS = 0x00000010,
            SAM_SERVER_LOOKUP_DOMAIN = 0x00000020,
            SAM_SERVER_ALL_ACCESS = 0x000F003F,
            SAM_SERVER_READ = 0x00020010,
            SAM_SERVER_WRITE = 0x0002000E,
            SAM_SERVER_EXECUTE = 0x00020021
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DOMAIN_PASSWORD_INFORMATION
        {
            public short MinPasswordLength;
            public short PasswordHistoryLength;
            public PASSWORD_PROPERTIES PasswordProperties;
            private long _maxPasswordAge;
            private long _minPasswordAge;

            public TimeSpan MaxPasswordAge
            {
                get
                {
                    return -new TimeSpan(_maxPasswordAge);
                }
                set
                {
                    _maxPasswordAge = value.Ticks;
                }
            }

            public TimeSpan MinPasswordAge
            {
                get
                {
                    return -new TimeSpan(_minPasswordAge);
                }
                set
                {
                    _minPasswordAge = value.Ticks;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private class UNICODE_STRING : IDisposable
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;

            public UNICODE_STRING()
                : this(null)
            {
            }

            public UNICODE_STRING(string s)
            {
                if (s != null)
                {
                    Length = (ushort)(s.Length * 2);
                    MaximumLength = (ushort)(Length + 2);
                    Buffer = Marshal.StringToHGlobalUni(s);
                }
            }

            public override string ToString() => Buffer != IntPtr.Zero ? Marshal.PtrToStringUni(Buffer) : null;

            protected virtual void Dispose(bool disposing)
            {
                if (Buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(Buffer);
                    Buffer = IntPtr.Zero;
                }
            }

            ~UNICODE_STRING() => Dispose(false);

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private static void Check(NTSTATUS err)
        {
            if (err == NTSTATUS.STATUS_SUCCESS)
                return;

            //throw new System.ComponentModel.Win32Exception("Error " + err + " (0x" + ((int)err).ToString("X8") + ")");
        }

        private enum NTSTATUS
        {
            STATUS_SUCCESS = 0x0,
            STATUS_MORE_ENTRIES = 0x105,
            STATUS_INVALID_HANDLE = unchecked((int)0xC0000008),
            STATUS_INVALID_PARAMETER = unchecked((int)0xC000000D),
            STATUS_ACCESS_DENIED = unchecked((int)0xC0000022),
            STATUS_OBJECT_TYPE_MISMATCH = unchecked((int)0xC0000024),
            STATUS_NO_SUCH_DOMAIN = unchecked((int)0xC00000DF),
        }

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamConnect(UNICODE_STRING ServerName, out IntPtr ServerHandle, SERVER_ACCESS_MASK DesiredAccess, IntPtr ObjectAttributes);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamCloseHandle(IntPtr ServerHandle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamFreeMemory(IntPtr Handle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamOpenDomain(IntPtr ServerHandle, DOMAIN_ACCESS_MASK DesiredAccess, byte[] DomainId, out IntPtr DomainHandle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamLookupDomainInSamServer(IntPtr ServerHandle, UNICODE_STRING name, out IntPtr DomainId);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamQueryInformationDomain(IntPtr DomainHandle, DOMAIN_INFORMATION_CLASS DomainInformationClass, out IntPtr Buffer);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamSetInformationDomain(IntPtr DomainHandle, DOMAIN_INFORMATION_CLASS DomainInformationClass, IntPtr Buffer);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamEnumerateDomainsInSamServer(IntPtr ServerHandle, ref int EnumerationContext, out IntPtr EnumerationBuffer, int PreferedMaximumLength, out int CountReturned);
    }

    class UserInfo
    {
        // https://stackoverflow.com/questions/5247798/get-list-of-local-computer-usernames-in-windows
        public static List<string> GetMachineUsers(Boolean onlyActive, Boolean onlyDisabled, Boolean onlyLockout, Boolean onlyAdmins, Boolean fullInfo)
        {
            List<string> retList = new List<string>();
            try
            {
                foreach (ManagementObject user in Program.win32_users)
                {
                    if (onlyActive && !(bool)user["Disabled"] && !(bool)user["Lockout"]) retList.Add((string)user["Name"]);
                    else if (onlyDisabled && (bool)user["Disabled"] && !(bool)user["Lockout"]) retList.Add((string)user["Name"]);
                    else if (onlyLockout && (bool)user["Lockout"]) retList.Add((string)user["Name"]);
                    else if (onlyAdmins)
                    {
                        string domain = (string)user["Domain"];
                        if (string.Join(",", GetUserGroups((string)user["Name"], domain)).Contains("Admin")) retList.Add((string)user["Name"]);
                    }
                    else if (fullInfo)
                    {
                        string domain = (string)user["Domain"];
                        string userLine = user["Caption"] + ((bool)user["Disabled"] ? "(Disabled)" : "") + ((bool)user["Lockout"] ? "(Lockout)" : "") + ((string)user["Fullname"] != "false" ? "" : "(" + user["Fullname"] + ")") + (((string)user["Description"]).Length > 1 ? ": " + user["Description"] : "");
                        List<string> user_groups = GetUserGroups((string)user["Name"], domain);
                        string groupsLine = "";
                        if (user_groups.Count > 0)
                        {
                            groupsLine = "\n        |->Groups: " + string.Join(",", user_groups);
                        }
                        string passLine = "\n        |->Password: " + ((bool)user["PasswordChangeable"] ? "CanChange" : "NotChange") + "-" + ((bool)user["PasswordExpires"] ? "Expi" : "NotExpi") + "-" + ((bool)user["PasswordRequired"] ? "Req" : "NotReq") + "\n";
                        retList.Add(userLine + groupsLine + passLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return retList;
        }


        public static bool IsLocaluser(string UserName, string domain)
        {
            return Program.currentADDomainName != Program.currentUserDomainName && domain != Program.currentUserDomainName;
        }

        // https://stackoverflow.com/questions/3679579/check-for-groups-a-local-user-is-a-member-of/3681442#3681442
        public static List<string> GetUserGroups(string sUserName, string domain)
        {
            List<string> myItems = new List<string>();
            try
            {
                if (Program.currentUserIsLocal && domain != Program.currentUserDomainName)
                    return myItems; //If local user and other domain, do not look

                UserPrincipal oUserPrincipal = GetUser(sUserName, domain);
                if (oUserPrincipal != null)
                {
                    PrincipalSearchResult<Principal> oPrincipalSearchResult = oUserPrincipal.GetGroups();
                    foreach (Principal oResult in oPrincipalSearchResult)
                        myItems.Add(oResult.Name);
                }
                else
                {
                    Beaprint.GrayPrint("  [-] Controlled exception, info about " + domain + "\\" + sUserName + " not found");
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return myItems;
        }

        public static UserPrincipal GetUser(string sUserName, string domain)
        {
            UserPrincipal user = null;
            try
            {
                if (Program.partofdomain && !Program.currentUserIsLocal) //Check if part of domain and notlocal users
                {
                    user = GetUserDomain(sUserName, domain);
                    if (user == null) //If part of domain but null, then user is local
                        user = GetUserLocal(sUserName);
                }
                else //If not part of a domain, then check local
                    user = GetUserLocal(sUserName);
            }
            catch
            { //If error, then some error ocurred trying to find a user inside an unexistant domain, check if local user
                user = GetUserLocal(sUserName);
            }
            return user;
        }
        public static UserPrincipal GetUserLocal(string sUserName)
        {
            // Extract local user information
            //https://stackoverflow.com/questions/14594545/query-local-administrator-group
            var context = new PrincipalContext(ContextType.Machine);
            var user = new UserPrincipal(context);
            user.SamAccountName = sUserName;
            var searcher = new PrincipalSearcher(user);
            user = searcher.FindOne() as UserPrincipal;
            return user;
        }
        public static UserPrincipal GetUserDomain(string sUserName, string domain)
        {
            //if not local, try to extract domain user information
            //https://stackoverflow.com/questions/12710355/check-if-user-is-a-domain-user-or-local-user/12710452
            //var domainContext = new PrincipalContext(ContextType.Domain, Environment.UserDomainName);
            var domainContext = new PrincipalContext(ContextType.Domain, domain);
            UserPrincipal domainuser = UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, sUserName);
            return domainuser;
        }

        public static PrincipalContext GetPrincipalContext()
        {
            PrincipalContext oPrincipalContext = new PrincipalContext(ContextType.Machine);
            return oPrincipalContext;
        }

        public static List<string> GetLoggedUsers()
        {
            List<string> retList = new List<string>();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_UserProfile WHERE Loaded = True");
                foreach (ManagementObject user in searcher.Get())
                {
                    string username = new SecurityIdentifier(user["SID"].ToString()).Translate(typeof(NTAccount)).ToString();
                    if (!username.Contains("NT AUTHORITY")) retList.Add(username);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return retList;
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
        public enum WTS_INFO_CLASS
        {
            WTSInitialProgram = 0,
            WTSApplicationName = 1,
            WTSWorkingDirectory = 2,
            WTSOEMId = 3,
            WTSSessionId = 4,
            WTSUserName = 5,
            WTSWinStationName = 6,
            WTSDomainName = 7,
            WTSConnectState = 8,
            WTSClientBuildNumber = 9,
            WTSClientName = 10,
            WTSClientDirectory = 11,
            WTSClientProductId = 12,
            WTSClientHardwareId = 13,
            WTSClientAddress = 14,
            WTSClientDisplay = 15,
            WTSClientProtocolType = 16,
            WTSIdleTime = 17,
            WTSLogonTime = 18,
            WTSIncomingBytes = 19,
            WTSOutgoingBytes = 20,
            WTSIncomingFrames = 21,
            WTSOutgoingFrames = 22,
            WTSClientInfo = 23,
            WTSSessionInfo = 24,
            WTSSessionInfoEx = 25,
            WTSConfigInfo = 26,
            WTSValidationInfo = 27,
            WTSSessionAddressV4 = 28,
            WTSIsRemoteSession = 29
        }
        [DllImport("wtsapi32.dll")]
        static extern void WTSCloseServer(IntPtr hServer);
        public static void CloseServer(IntPtr ServerHandle)
        {
            WTSCloseServer(ServerHandle);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_ADDRESS
        {
            public uint AddressFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;
        }
        [DllImport("Wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            uint sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out uint pBytesReturned
        );
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
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);
        public static IntPtr OpenServer(String Name)
        {
            IntPtr server = WTSOpenServer(Name);
            return server;
        }
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern Int32 WTSEnumerateSessionsEx(
            IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pLevel,
            [MarshalAs(UnmanagedType.U4)] Int32 Filter,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);
        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);
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
                Int32 retval = WTSEnumerateSessionsEx(server, ref level, 0, ref ppSessionInfo, ref count);
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

                        rdp_session["SessionID"] = String.Format("{0}", si.SessionID);
                        rdp_session["pSessionName"] = String.Format("{0}", si.pSessionName);
                        rdp_session["pUserName"] = String.Format("{0}", si.pUserName);
                        rdp_session["pDomainName"] = String.Format("{0}", si.pDomainName);
                        rdp_session["State"] = String.Format("{0}", si.State);
                        rdp_session["SourceIP"] = "";

                        // Now use WTSQuerySessionInformation to get the remote IP (if any) for the connection
                        IntPtr addressPtr = IntPtr.Zero;
                        uint bytes = 0;

                        WTSQuerySessionInformation(server, (uint)si.SessionID, WTS_INFO_CLASS.WTSClientAddress, out addressPtr, out bytes);
                        WTS_CLIENT_ADDRESS address = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure((System.IntPtr)addressPtr, typeof(WTS_CLIENT_ADDRESS));

                        if (address.Address[2] != 0)
                        {
                            string sourceIP = String.Format("{0}.{1}.{2}.{3}", address.Address[2], address.Address[3], address.Address[4], address.Address[5]);
                            rdp_session["SourceIP"] = String.Format("{0}", sourceIP);
                        }
                        results.Add(rdp_session);
                    }
                    WTSFreeMemory(ppSessionInfo);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            finally
            {
                CloseServer(server);
            }
            return results;
        }

        public static List<string> GetEverLoggedUsers()
        {
            List<string> retList = new List<string>();
            try
            {
                SelectQuery query = new SelectQuery("Win32_UserProfile");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject user in searcher.Get())
                {
                    string username = new SecurityIdentifier(user["SID"].ToString()).Translate(typeof(NTAccount)).ToString();
                    if (!username.Contains("NT AUTHORITY")) retList.Add(username);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return retList;
        }

        public static List<string> GetUsersFolders()
        {
            return MyUtils.ListFolder("Users");
        }

        // https://stackoverflow.com/questions/31464835/how-to-programmatically-check-the-password-must-meet-complexity-requirements-g
        public static List<Dictionary<string, string>> GetPasswordPolicy()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                using (SamServer server = new SamServer(null, SamServer.SERVER_ACCESS_MASK.SAM_SERVER_ENUMERATE_DOMAINS | SamServer.SERVER_ACCESS_MASK.SAM_SERVER_LOOKUP_DOMAIN))
                {
                    foreach (string domain in server.EnumerateDomains())
                    {
                        var sid = server.GetDomainSid(domain);
                        var pi = server.GetDomainPasswordInformation(sid);

                        results.Add(new Dictionary<string, string>()
                        {
                            { "Domain", domain },
                            { "SID", String.Format("{0}", sid) },
                            { "MaxPasswordAge", String.Format("{0}", pi.MaxPasswordAge) },
                            { "MinPasswordAge", String.Format("{0}", pi.MinPasswordAge) },
                            { "MinPasswordLength", String.Format("{0}", pi.MinPasswordLength) },
                            { "PasswordHistoryLength", String.Format("{0}", pi.PasswordHistoryLength) },
                            { "PasswordProperties", String.Format("{0}", pi.PasswordProperties) },
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }


        // From Seatbelt
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);


        protected struct TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 35)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public UInt32 Attributes;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool LookupPrivilegeName(
            string lpSystemName,
            IntPtr lpLuid,
            System.Text.StringBuilder lpName,
            ref int cchName);

        [Flags]
        public enum LuidAttributes : uint
        {
            DISABLED = 0x00000000,
            SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001,
            SE_PRIVILEGE_ENABLED = 0x00000002,
            SE_PRIVILEGE_REMOVED = 0x00000004,
            SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000
        }

        enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin
        }

        public static Dictionary<string, string> GetTokenGroupPrivs()
        {
            // Returns all privileges that the current process/user possesses
            // adapted from https://stackoverflow.com/questions/4349743/setting-size-of-token-privileges-luid-and-attributes-array-returned-by-gettokeni

            Dictionary<string, string> results = new Dictionary<string, string> { };
            try
            {
                int TokenInfLength = 0;
                IntPtr ThisHandle = WindowsIdentity.GetCurrent().Token;
                GetTokenInformation(ThisHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, TokenInfLength, out TokenInfLength);
                IntPtr TokenInformation = Marshal.AllocHGlobal(TokenInfLength);
                if (GetTokenInformation(WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenPrivileges, TokenInformation, TokenInfLength, out TokenInfLength))
                {
                    TOKEN_PRIVILEGES ThisPrivilegeSet = (TOKEN_PRIVILEGES)Marshal.PtrToStructure(TokenInformation, typeof(TOKEN_PRIVILEGES));
                    for (int index = 0; index < ThisPrivilegeSet.PrivilegeCount; index++)
                    {
                        LUID_AND_ATTRIBUTES laa = ThisPrivilegeSet.Privileges[index];
                        System.Text.StringBuilder StrBuilder = new System.Text.StringBuilder();
                        int LuidNameLen = 0;
                        IntPtr LuidPointer = Marshal.AllocHGlobal(Marshal.SizeOf(laa.Luid));
                        Marshal.StructureToPtr(laa.Luid, LuidPointer, true);
                        LookupPrivilegeName(null, LuidPointer, null, ref LuidNameLen);
                        StrBuilder.EnsureCapacity(LuidNameLen + 1);
                        if (LookupPrivilegeName(null, LuidPointer, StrBuilder, ref LuidNameLen))
                            results[StrBuilder.ToString()] = String.Format("{0}", (LuidAttributes)laa.Attributes);
                        Marshal.FreeHGlobal(LuidPointer);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }

        public static Dictionary<string, string> GetAutoLogon()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            results["DefaultDomainName"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultDomainName");
            results["DefaultUserName"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultUserName");
            results["DefaultPassword"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "DefaultPassword");
            results["AltDefaultDomainName"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultDomainName");
            results["AltDefaultUserName"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultUserName");
            results["AltDefaultPassword"] = MyUtils.GetRegValue("HKLM", "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", "AltDefaultPassword");
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
                    c = String.Format("{0}", Clipboard.GetAudioStream());

                else if (Clipboard.ContainsFileDropList())
                    c = String.Format("{0}", Clipboard.GetFileDropList());

                else if (Clipboard.ContainsImage())
                    c = String.Format("{0}", Clipboard.GetImage());
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return c;
        }
    }
}
