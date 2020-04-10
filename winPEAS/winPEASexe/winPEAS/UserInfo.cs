using System;
using System.Collections.Generic;
using System.Management;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;

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
                    try
                    {
                        Marshal.FreeHGlobal(Buffer);
                    }
                    catch(Exception ex)
                    {
                        Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
                    }
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

        public static string staticSID2GroupName(string SID)
        {
            Dictionary<string, string> known_SID_dic = new Dictionary<string, string>()
            {
                //From https://support.microsoft.com/en-us/help/243330/well-known-security-identifiers-in-windows-operating-systems
                { "S-1-0", "Null Authority" }, //An identifier authority.
                { "S-1-0-0", "Nobody" }, //No security principal.
                { "S-1-1", "World Authority" }, //An identifier authority.
                { "S-1-1-0", "Everyone" }, //A group that includes all users, even anonymous users and guests. Membership is controlled by the operating system.
                { "S-1-2", "Local Authority" }, //An identifier authority.
                { "S-1-2-0", "Local" }, //A group that includes all users who have logged on locally.
                { "S-1-3", "Creator Authority" }, //An identifier authority.
                { "S-1-3-0", "Creator Owner" }, //A placeholder in an inheritable access control entry (ACE). When the ACE is inherited, the system replaces this SID with the SID for the object's creator.
                { "S-1-3-1", "Creator Group" }, //A placeholder in an inheritable ACE. When the ACE is inherited, the system replaces this SID with the SID for the primary group of the object's creator. The primary group is used only by the POSIX subsystem.
                { "S-1-3-4", "Owner Rights" }, //A group that represents the current owner of the object. When an ACE that carries this SID is applied to an object, the system ignores the implicit READ_CONTROL and WRITE_DAC permissions for the object owner.
                { "S-1-4", "Non-unique Authority" }, //An identifier authority.
                { "S-1-5", "NT Authority" }, //An identifier authority.
                { "S-1-5-1", "Dialup" }, //A group that includes all users who have logged on through a dial-up connection. Membership is controlled by the operating system.
                { "S-1-5-2", "Network" }, //A group that includes all users that have logged on through a network connection. Membership is controlled by the operating system.
                { "S-1-5-3", "Batch" }, //A group that includes all users that have logged on through a batch queue facility. Membership is controlled by the operating system.
                { "S-1-5-4", "Interactive" }, //A group that includes all users that have logged on interactively. Membership is controlled by the operating system.
                { "S-1-5-5-.+-.+", "Logon Session" }, //A logon session. The X and Y values for these SIDs are different for each session.
                { "S-1-5-6", "Service" }, //A group that includes all security principals that have logged on as a service. Membership is controlled by the operating system.
                { "S-1-5-7", "Anonymous" }, //A group that includes all users that have logged on anonymously. Membership is controlled by the operating system.
                { "S-1-5-9", "Enterprise Domain Controllers" }, //A group that includes all domain controllers in a forest that uses an Active Directory directory service. Membership is controlled by the operating system.
                { "S-1-5-10", "Principal Self" }, //A placeholder in an inheritable ACE on an account object or group object in Active Directory. When the ACE is inherited, the system replaces this SID with the SID for the security principal who holds the account.
                { "S-1-5-11", "Authenticated Users" }, //A group that includes all users whose identities were authenticated when they logged on. Membership is controlled by the operating system.
                { "S-1-5-12", "Restricted Code" }, //This SID is reserved for future use.
                { "S-1-5-13", "Terminal Server Users" }, //Terminal Server Users
                { "S-1-5-14", "Remote Interactive Logon" }, //Remote Interactive Logon
                { "S-1-5-17", "This Organization" }, //An account that is used by the default Internet Information Services (IIS) user.
                { "S-1-5-18", "Local System" }, //A service account that is used by the operating system.
                { "S-1-5-19", "NT Authority\\Local Service" },
                { "S-1-5-20", "NT Authority\\Network Service" },
                { "S-1-5-21.+-500", "Administrator" }, //A user account for the system administrator. By default, it is the only user account that is given full control over the system.
                { "S-1-5-21.+-501", "Guest" }, //A user account for people who do not have individual accounts. This user account does not require a password. By default, the Guest account is disabled.
                { "S-1-5-21.+-502", "KRBTGT" }, //A service account that is used by the Key Distribution Center (KDC) service.
                { "S-1-5-21.+-512", "Domain Admins" }, //A global group whose members are authorized to administer the domain. By default, the Domain Admins group is a member of the Administrators group on all computers that have joined a domain, including the domain controllers. Domain Admins is the default owner of any object that is created by any member of the group.
                { "S-1-5-21.+-513", "Domain Users" }, //A global group that, by default, includes all user accounts in a domain. When you create a user account in a domain, it is added to this group by default.
                { "S-1-5-21.+-514", "Domain Guests" }, //A global group that, by default, has only one member, the domain's built-in Guest account.
                { "S-1-5-21.+-515", "Domain Computers" }, //A global group that includes all clients and servers that have joined the domain.
                { "S-1-5-21.+-516", "Domain Controllers" }, //A global group that includes all domain controllers in the domain. New domain controllers are added to this group by default.
                { "S-1-5-21.+-517", "Cert Publishers" }, //A global group that includes all computers that are running an enterprise certification authority. Cert Publishers are authorized to publish certificates for User objects in Active Directory.
                { "S-1-5-21.+-518", "Schema Admins" }, //A universal group in a native-mode domain; a global group in a mixed-mode domain. The group is authorized to make schema changes in Active Directory. By default, the only member of the group is the Administrator account for the forest root domain.
                { "S-1-5-21.+-519", "Enterprise Admins" }, //A universal group in a native-mode domain; a global group in a mixed-mode domain. The group is authorized to make forest-wide changes in Active Directory, such as adding child domains. By default, the only member of the group is the Administrator account for the forest root domain.
                { "S-1-5-21.+-520", "Group Policy Creator Owners" }, //A global group that is authorized to create new Group Policy objects in Active Directory. By default, the only member of the group is Administrator.
                { "S-1-5-21.+-525", "Protected Users" }, //https://book.hacktricks.xyz/windows/stealing-credentials/credentials-protections#protected-users
                { "S-1-5-21.+-526", "Key Admins" }, //A security group. The intention for this group is to have delegated write access on the msdsKeyCredentialLink attribute only. The group is intended for use in scenarios where trusted external authorities (for example, Active Directory Federated Services) are responsible for modifying this attribute. Only trusted administrators should be made a member of this group.
                { "S-1-5-21.+-527", "Enterprise Key Admins" }, //A security group. The intention for this group is to have delegated write access on the msdsKeyCredentialLink attribute only. The group is intended for use in scenarios where trusted external authorities (for example, Active Directory Federated Services) are responsible for modifying this attribute. Only trusted administrators should be made a member of this group.
                { "S-1-5-21.+-553", "RAS and IAS Servers" }, //A domain local group. By default, this group has no members. Servers in this group have Read Account Restrictions and Read Logon Information access to User objects in the Active Directory domain local group.
                { "S-1-5-32-544", "Administrators" }, //A built-in group. After the initial installation of the operating system, the only member of the group is the Administrator account. When a computer joins a domain, the Domain Admins group is added to the Administrators group. When a server becomes a domain controller, the Enterprise Admins group also is added to the Administrators group.
                { "S-1-5-32-545", "Users" }, //A built-in group. After the initial installation of the operating system, the only member is the Authenticated Users group. When a computer joins a domain, the Domain Users group is added to the Users group on the computer.
                { "S-1-5-32-546", "Guests" }, //A built-in group. By default, the only member is the Guest account. The Guests group allows occasional or one-time users to log on with limited privileges to a computer's built-in Guest account.
                { "S-1-5-32-547", "Power Users" }, //A built-in group. By default, the group has no members. Power users can create local users and groups; modify and delete accounts that they have created; and remove users from the Power Users, Users, and Guests groups. Power users also can install programs; create, manage, and delete local printers; and create and delete file shares.
                { "S-1-5-32-548", "Account Operators" }, //A built-in group that exists only on domain controllers. By default, the group has no members. By default, Account Operators have permission to create, modify, and delete accounts for users, groups, and computers in all containers and organizational units of Active Directory except the Builtin container and the Domain Controllers OU. Account Operators do not have permission to modify the Administrators and Domain Admins groups, nor do they have permission to modify the accounts for members of those groups.
                { "S-1-5-32-549", "Server Operators" }, //A built-in group that exists only on domain controllers. By default, the group has no members. Server Operators can log on to a server interactively; create and delete network shares; start and stop services; back up and restore files; format the hard disk of the computer; and shut down the computer.
                { "S-1-5-32-550", "Print Operators" }, //A built-in group that exists only on domain controllers. By default, the only member is the Domain Users group. Print Operators can manage printers and document queues.
                { "S-1-5-32-551", "Backup Operators" }, //A built-in group. By default, the group has no members. Backup Operators can back up and restore all files on a computer, regardless of the permissions that protect those files. Backup Operators also can log on to the computer and shut it down.
                { "S-1-5-32-552", "Replicators" }, //A built-in group that is used by the File Replication service on domain controllers. By default, the group has no members. Do not add users to this group.
                { "S-1-5-32-582", "Storage Replica Administrators" }, //A built-in group that grants complete and unrestricted access to all features of Storage Replica.
                { "S-1-5-64-10", "NTLM Authentication" }, //An SID that is used when the NTLM authentication package authenticated the client.
                { "S-1-5-64-14", "SChannel Authentication" }, //An SID that is used when the SChannel authentication package authenticated the client.
                { "S-1-5-64-21", "Digest Authentication" }, //An SID that is used when the Digest authentication package authenticated the client.
                { "S-1-5-80", "NT Service" }, //An NT Service account prefix.
                { "S-1-3-2", "Creator Owner Server" }, //This SID is not used in Windows 2000.
                { "S-1-3-3", "Creator Group Server" }, //This SID is not used in Windows 2000.
                { "S-1-5-8", "Proxy" }, //This SID is not used in Windows 2000.
                { "S-1-5-15", "This Organization" }, //A group that includes all users from the same organization. Only included with AD accounts and only added by a Windows Server 2003 or later domain controller.
                { "S-1-5-32-554", "Builtin\\Pre-Windows 2000 Compatible Access" }, //An alias added by Windows 2000. A backward compatibility group which allows read access on all users and groups in the domain.
                { "S-1-5-32-555", "Builtin\\Remote Desktop Users" }, //An alias. Members in this group are granted the right to log on remotely.
                { "S-1-5-32-556", "Builtin\\Network Configuration Operators" }, //An alias. Members in this group can have some administrative privileges to manage configuration of networking features.
                { "S-1-5-32-557", "Builtin\\Incoming Forest Trust Builders" }, //An alias. Members of this group can create incoming, one-way trusts to this forest.
                { "S-1-5-32-558", "Builtin\\Performance Monitor Users" }, //An alias. Members of this group have remote access to monitor this computer.
                { "S-1-5-32-559", "Builtin\\Performance Log Users" }, //An alias. Members of this group have remote access to schedule logging of performance counters on this computer.
                { "S-1-5-32-560", "Builtin\\Windows Authorization Access Group" }, //An alias. Members of this group have access to the computed tokenGroupsGlobalAndUniversal attribute on User objects.
                { "S-1-5-32-561", "Builtin\\Terminal Server License Servers" }, //An alias. A group for Terminal Server License Servers. When Windows Server 2003 Service Pack 1 is installed, a new local group is created.
                { "S-1-5-32-562", "Builtin\\Distributed COM Users" }, //An alias. A group for COM to provide computerwide access controls that govern access to all call, activation, or launch requests on the computer.
                { "S-1-2-1", "Console Logon" }, //A group that includes users who are logged on to the physical console.
                { "S-1-5-21.+-498", "Enterprise Read-only Domain Controllers" }, //A universal group. Members of this group are read-only domain controllers in the enterprise.
                { "S-1-5-21.+-521", "Read-only Domain Controllers" }, //A global group. Members of this group are read-only domain controllers in the domain.
                { "S-1-5-21.+-571", "Allowed RODC Password Replication Group" }, //A domain local group. Members in this group can have their passwords replicated to all read-only domain controllers in the domain.
                { "S-1-5-21.+-572", "Denied RODC Password Replication Group" }, //A domain local group. Members in this group cannot have their passwords replicated to any read-only domain controllers in the domain.
                { "S-1-5-32-569", "	Builtin\\Cryptographic Operators" }, //A built-in local group. Members are authorized to perform cryptographic operations.
                { "S-1-5-32-573", "Builtin\\Event Log Readers" }, //A built-in local group. Members of this group can read event logs from local computer.
                { "S-1-5-32-574", "Builtin\\Certificate Service DCOM Access" }, //A built-in local group. Members of this group are allowed to connect to Certification Authorities in the enterprise.
                { "S-1-5-80-0", "NT Services\\All Services" }, //A group that includes all service processes that are configured on the system. Membership is controlled by the operating system.
                { "S-1-5-83-0", "NT Virtual Machine\\Virtual Machines" }, //A built-in group. The group is created when the Hyper-V role is installed. Membership in the group is maintained by the Hyper-V Management Service (VMMS). This group requires the Create Symbolic Links right (SeCreateSymbolicLinkPrivilege), and also the Log on as a Service right (SeServiceLogonRight).
                { "S-1-5-90-0", "Windows Manager\\Windows Manager Group" }, //A built-in group that is used by the Desktop Window Manager (DWM). DWM is a Windows service that manages information display for Windows applications.
                { "S-1-16-0", "Untrusted Mandatory Level" }, //An untrusted integrity level.
                { "S-1-16-4096", "Low Mandatory Level" }, //A low integrity level.
                { "S-1-16-8192", "Medium Mandatory Level" }, //A medium integrity level.
                { "S-1-16-8448", "Medium Plus Mandatory Level" }, //A medium plus integrity level.
                { "S-1-16-12288", "High Mandatory Level" }, //A high integrity level.
                { "S-1-16-16384", "System Mandatory Level" }, //A system integrity level.
                { "S-1-16-20480", "Protected Process Mandatory Level" }, //A protected-process integrity level.
                { "S-1-16-28672", "Secure Process Mandatory Level" }, //A secure process integrity level.
                { "S-1-5-21-.+-522", "Cloneable Domain Controllers" }, //A global group. Members of this group that are domain controllers may be cloned.
                { "S-1-5-32-575", "Builtin\\RDS Remote Access Servers" }, //A built-in local group. Servers in this group enable users of RemoteApp programs and personal virtual desktops access to these resources. In Internet-facing deployments, these servers are typically deployed in an edge network. This group needs to be populated on servers running RD Connection Broker. RD Gateway servers and RD Web Access servers used in the deployment need to be in this group.
                { "S-1-5-32-576", "Builtin\\RDS Endpoint Servers" }, //A built-in local group. Servers in this group run virtual machines and host sessions where users RemoteApp programs and personal virtual desktops run. This group needs to be populated on servers running RD Connection Broker. RD Session Host servers and RD Virtualization Host servers used in the deployment need to be in this group.
                { "S-1-5-32-577", "Builtin\\RDS Management Servers" }, //A builtin local group. Servers in this group can perform routine administrative actions on servers running Remote Desktop Services. This group needs to be populated on all servers in a Remote Desktop Services deployment. The servers running the RDS Central Management service must be included in this group.
                { "S-1-5-32-578", "Builtin\\Hyper-V Administrators" }, //A built-in local group. Members of this group have complete and unrestricted access to all features of Hyper-V.
                { "S-1-5-32-579", "Builtin\\Access Control Assistance Operators" }, //A built-in local group. Members of this group can remotely query authorization attributes and permissions for resources on this computer.
                { "S-1-5-32-580", "Builtin\\Remote Management Users" }, //A built-in local group. Members of this group can access WMI resources over management protocols (such as WS-Management via the Windows Remote Management service). This applies only to WMI namespaces that grant access to the user.
                { "S-1-5-113" , "Local account" },
                { "S-1-5-114" , "Local account and member of Administrators group" },
                { "S-1-5-64-36" , "Cloud Account Authentication" },
            };

            try
            {
                foreach (KeyValuePair<string, string> kSID_entry in known_SID_dic)
                {
                    Match mtch = Regex.Match(SID, "^"+kSID_entry.Key+"$", RegexOptions.IgnoreCase);
                    if (!String.IsNullOrEmpty(mtch.Value))
                        return known_SID_dic[kSID_entry.Key];
                }

                return "";
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error in PermInt2Str: " + ex);
            }
            return "";
        }

        public static string SID2GroupName(string SID)
        {
            //Frist, look in well-known SIDs
            string groupName = staticSID2GroupName(SID);
            if (!String.IsNullOrEmpty(groupName))
                return groupName;

            //If not well known, search in local or domain (depending on the nature of the user)
            ContextType ct = ContextType.Domain;
            if (Program.currentUserIsLocal)
                ct = ContextType.Machine;

            try
            {
                groupName = GetSIDGroupName(SID, ct);
            }
            catch (Exception ex)
            {
                //If error, check inside the other one
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}\n    Checking using the other Principal Context", ex.Message));
                try
                {
                    if (ct == ContextType.Machine)
                        groupName = GetSIDGroupName(SID, ContextType.Domain);
                    else
                        groupName = GetSIDGroupName(SID, ContextType.Machine);
                    return groupName;
                }
                catch
                {
                    Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
                }
            }

            //If nothing, check inside the other one
            if (String.IsNullOrEmpty(groupName))
            {
                try
                {
                    if (ct == ContextType.Machine)
                        groupName = GetSIDGroupName(SID, ContextType.Domain);
                    else
                        groupName = GetSIDGroupName(SID, ContextType.Machine);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return groupName;
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

                //else if (Clipboard.ContainsImage()) //No system.Drwing import
                    //c = String.Format("{0}", Clipboard.GetImage());
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return c;
        }
    }
}
