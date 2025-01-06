using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.Helpers.Extensions;
using winPEAS.Info.UserInfo;
using winPEAS.Info.UserInfo.LogonSessions;
using winPEAS.Info.UserInfo.Tenant;
using winPEAS.Info.UserInfo.Token;
using winPEAS.Native;
using winPEAS.Native.Enums;
using winPEAS.Native.Structs;

namespace winPEAS.Checks
{
    internal class UserInfo : ISystemCheck
    {
        /* Colors Code
        * RED:
        * ---- Privileges users and groups names
        * MAGENTA:
        * ---- Current user and domain
        * BLUE:
        * ---- Locked users
        * CYAN:
        * ---- Active users
        * MediumPurple:
        * ---- Disabled users
       */


        static string badgroups = "docker|Remote |DNSAdmins|AD Recycle Bin|Azure Admins|Admins|Server Operators";//The space in Remote is important to not mix with SeShutdownRemotePrivilege
        static readonly string _badPasswd = "NotChange|NotExpi";
        static readonly string _badPrivileges = "SeImpersonatePrivilege|SeAssignPrimaryPrivilege|SeTcbPrivilege|SeBackupPrivilege|SeRestorePrivilege|SeCreateTokenPrivilege|SeLoadDriverPrivilege|SeTakeOwnershipPrivilege|SeDebugPrivilege";

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Users Information");

            new List<Action>
            {
                PrintCU,
                PrintCurrentUserIdleTime,
                PrintCurrentTenantInfo,
                PrintTokenP,
                PrintClipboardText,
                PrintLoggedUsers,
                PrintLocalUsers,
                PrintRdpSessions,
                PrintEverLoggedUsers,
                PrintHomeFolders,
                PrintAutoLogin,
                PrintPasswordPolicies,
                PrintLogonSessions
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        Dictionary<string, string> ColorsU()
        {
            var usersColors = new Dictionary<string, string>()
                {
                    { Checks.PaintActiveUsersNoAdministrator, Beaprint.ansi_users_active },
                    { Checks.CurrentUserName + "|"+ Checks.CurrentUserDomainName, Beaprint.ansi_current_user },
                    { Checks.PaintAdminUsers+"|"+ badgroups + "|" + _badPasswd + "|" + _badPrivileges + "|" + "DefaultPassword.*", Beaprint.ansi_color_bad },
                    { @"Disabled", Beaprint.ansi_users_disabled },
                };

            if (Checks.PaintDisabledUsers.Length > 1)
            {
                usersColors[Checks.PaintDisabledUsersNoAdministrator] = Beaprint.ansi_users_disabled;
            }
            return usersColors;
        }

        void PrintCU()
        {
            try
            {
                Beaprint.MainPrint("Users");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#users--groups", "Check if you have some admin equivalent privileges");

                List<string> usersGrps = User.GetMachineUsers(false, false, false, false, true);

                Beaprint.AnsiPrint("  Current user: " + Checks.CurrentUserName, ColorsU());

                List<string> currentGroupsNames = new List<string>();
                foreach (KeyValuePair<string, string> g in Checks.CurrentUserSiDs)
                {
                    if (g.Key == WindowsIdentity.GetCurrent().User.ToString())
                    {
                        continue;
                    }
                    currentGroupsNames.Add(string.IsNullOrEmpty(g.Value) ? g.Key : g.Value);
                }

                Beaprint.AnsiPrint("  Current groups: " + string.Join(", ", currentGroupsNames), ColorsU());
                Beaprint.PrintLineSeparator();
                Beaprint.ListPrint(usersGrps, ColorsU());
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintTokenP()
        {
            try
            {
                Beaprint.MainPrint("Current Token privileges");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#token-manipulation", "Check if you can escalate privilege using some enabled token");
                Dictionary<string, string> tokenPrivs = Token.GetTokenGroupPrivs();
                Beaprint.DictPrint(tokenPrivs, ColorsU(), false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintClipboardText()
        {
            try
            {
                Beaprint.MainPrint("Clipboard text");
                string clipboard = UserInfoHelper.GetClipboardText();
                if (!string.IsNullOrEmpty(clipboard))
                {
                    Beaprint.BadPrint(clipboard);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintLoggedUsers()
        {
            try
            {
                Beaprint.MainPrint("Logged users");
                List<string> loggedUsers = User.GetLoggedUsers();

                Beaprint.ListPrint(loggedUsers, ColorsU());
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintRdpSessions()
        {
            try
            {
                Beaprint.MainPrint("RDP Sessions");
                List<Dictionary<string, string>> rdp_sessions = UserInfoHelper.GetRDPSessions();
                if (rdp_sessions.Count > 0)
                {
                    string format = "    {0,-10}{1,-15}{2,-15}{3,-25}{4,-10}{5}";
                    string header = string.Format(format, "SessID", "pSessionName", "pUserName", "pDomainName", "State", "SourceIP");
                    Beaprint.GrayPrint(header);
                    foreach (Dictionary<string, string> rdpSes in rdp_sessions)
                    {
                        Beaprint.AnsiPrint(string.Format(format, rdpSes["SessionID"], rdpSes["pSessionName"], rdpSes["pUserName"], rdpSes["pDomainName"], rdpSes["State"], rdpSes["SourceIP"]), ColorsU());
                    }
                }
                else
                {
                    Beaprint.NotFoundPrint();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintEverLoggedUsers()
        {
            try
            {
                Beaprint.MainPrint("Ever logged users");
                List<string> everLogged = User.GetEverLoggedUsers();
                Beaprint.ListPrint(everLogged, ColorsU());
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintHomeFolders()
        {
            try
            {
                Beaprint.MainPrint("Home folders found");
                List<string> user_folders = User.GetUsersFolders();
                foreach (string ufold in user_folders)
                {
                    string perms = string.Join(", ", PermissionsHelper.GetPermissionsFolder(ufold, Checks.CurrentUserSiDs));
                    if (perms.Length > 0)
                    {
                        Beaprint.BadPrint("    " + ufold + " : " + perms);
                    }
                    else
                    {
                        Beaprint.GoodPrint("    " + ufold);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintAutoLogin()
        {
            try
            {
                Beaprint.MainPrint("Looking for AutoLogon credentials");
                bool ban = false;
                Dictionary<string, string> autologon = UserInfoHelper.GetAutoLogon();
                if (autologon.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in autologon)
                    {
                        if (!string.IsNullOrEmpty(entry.Value))
                        {
                            if (!ban)
                            {
                                Beaprint.BadPrint("    Some AutoLogon credentials were found");
                                ban = true;
                            }
                            Beaprint.AnsiPrint(string.Format("    {0,-30}:  {1}", entry.Key, entry.Value), ColorsU());
                        }
                    }

                    if (!ban)
                    {
                        Beaprint.NotFoundPrint();
                    }
                }
                else
                {
                    Beaprint.NotFoundPrint();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintPasswordPolicies()
        {
            try
            {
                Beaprint.MainPrint("Password Policies");
                Beaprint.LinkPrint("", "Check for a possible brute-force");
                List<Dictionary<string, string>> PPy = UserInfoHelper.GetPasswordPolicy();
                Beaprint.DictPrint(PPy, ColorsU(), false);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private void PrintLogonSessions()
        {
            try
            {
                Beaprint.MainPrint("Print Logon Sessions");

                var logonSessions = LogonSessions.GetLogonSessions();

                foreach (var logonSession in logonSessions)
                {
                    Beaprint.NoColorPrint($"    Method:                       {logonSession.Method}\n" +
                                            $"    Logon Server:                 {logonSession.LogonServer}\n" +
                                            $"    Logon Server Dns Domain:      {logonSession.LogonServerDnsDomain}\n" +
                                            $"    Logon Id:                     {logonSession.LogonId}\n" +
                                            $"    Logon Time:                   {logonSession.LogonTime}\n" +
                                            $"    Logon Type:                   {logonSession.LogonType}\n" +
                                            $"    Start Time:                   {logonSession.StartTime}\n" +
                                            $"    Domain:                       {logonSession.Domain}\n" +
                                            $"    Authentication Package:       {logonSession.AuthenticationPackage}\n" +
                                            $"    Start Time:                   {logonSession.StartTime}\n" +
                                            $"    User Name:                    {logonSession.UserName}\n" +
                                            $"    User Principal Name:          {logonSession.UserPrincipalName}\n" +
                                            $"    User SID:                     {logonSession.UserSID}\n"
                                      );

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception)
            {
            }
        }

        private static void PrintCurrentUserIdleTime()
        {
            try
            {
                Beaprint.MainPrint("Current User Idle Time");

                var lastInputInfo = new LastInputInfo();
                lastInputInfo.Size = (uint)Marshal.SizeOf(lastInputInfo);

                if (User32.GetLastInputInfo(ref lastInputInfo))
                {
                    var currentUser = WindowsIdentity.GetCurrent().Name;
                    var idleTimeMiliSeconds = (uint)Environment.TickCount - lastInputInfo.Time;
                    var timeSpan = TimeSpan.FromMilliseconds(idleTimeMiliSeconds);
                    var idleTimeString = $"{timeSpan.Hours:D2}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s:{timeSpan.Milliseconds:D3}ms";

                    Beaprint.NoColorPrint($"   Current User   :     {currentUser}\n" +
                                                $"   Idle Time      :     {idleTimeString}");
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void PrintLocalUsers()
        {
            try
            {
                Beaprint.MainPrint("Display information about local users");

                var computerName = Environment.GetEnvironmentVariable("COMPUTERNAME");

                var localUsers = User.GetLocalUsers(computerName);

                var colors = new Dictionary<string, string>
                {
                    { "Administrator", Beaprint.ansi_color_bad },
                    { "Guest", Beaprint.YELLOW },
                    { "False", Beaprint.ansi_color_good },
                    { "True", Beaprint.ansi_color_bad },
                };

                foreach (var localUser in localUsers)
                {
                    var enabled = ((localUser.flags >> 1) & 1) == 0;
                    var pwdLastSet = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    var lastLogon = new DateTime(1970, 1, 1, 0, 0, 0);

                    if (localUser.passwordAge != 0)
                    {
                        pwdLastSet = DateTime.Now.AddSeconds(-localUser.passwordAge);
                    }

                    if (localUser.last_logon != 0)
                    {
                        lastLogon = lastLogon.AddSeconds(localUser.last_logon).ToLocalTime();
                    }

                    Beaprint.AnsiPrint($"   Computer Name           :   {computerName}\n" +
                                        $"   User Name               :   {localUser.name}\n" +
                                        $"   User Id                 :   {localUser.user_id}\n" +
                                        $"   Is Enabled              :   {enabled}\n" +
                                        $"   User Type               :   {(UserPrivType)localUser.priv}\n" +
                                        $"   Comment                 :   {localUser.comment}\n" +
                                        $"   Last Logon              :   {lastLogon}\n" +
                                        $"   Logons Count            :   {localUser.num_logons}\n" +
                                        $"   Password Last Set       :   {pwdLastSet}\n",
                                            colors);

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void PrintCurrentTenantInfo()
        {
            try
            {
                Beaprint.MainPrint("Display Tenant information (DsRegCmd.exe /status)");

                var info = Tenant.GetTenantInfo();

                if (info != null)
                {

                    Beaprint.NoColorPrint($"    Tenant Display Name        :        {info.TenantDisplayName}\n" +
                                                $"    Tenant Id                  :        {info.TenantId}\n" +
                                                $"    Idp Domain                 :        {info.IdpDomain}\n" +
                                                $"    Mdm Enrollment Url         :        {info.MdmEnrollmentUrl}\n" +
                                                $"    Mdm TermsOfUse Url         :        {info.MdmTermsOfUseUrl}\n" +
                                                $"    Mdm Compliance Url         :        {info.MdmComplianceUrl}\n" +
                                                $"    User Setting Sync Url      :        {info.UserSettingSyncUrl}\n" +
                                                $"    Device Id                  :        {info.DeviceId}\n" +
                                                $"    Join Type                  :        {info.JType.GetDescription()}\n" +
                                                $"    Join User Email            :        {info.JoinUserEmail}\n" +
                                                $"    User Key Id                :        {info.UserKeyId}\n" +
                                                $"    User Email                 :        {info.UserEmail}\n" +
                                                $"    User Keyname               :        {info.UserKeyname}\n");

                    foreach (var cert in info.CertInfo)
                    {
                        Beaprint.NoColorPrint($"    Thumbprint      :     {cert.Thumbprint}\n" +
                                                    $"    Subject         :     {cert.Subject}\n" +
                                                    $"    Issuer          :     {cert.Issuer}\n" +
                                                    $"    Expiration      :     {cert.GetExpirationDateString()}");
                    }
                }
                else
                {
                    Beaprint.NoColorPrint("   Tenant is NOT Azure AD Joined.");
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
