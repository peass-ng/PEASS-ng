using System;
using System.Collections.Generic;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.Info.UserInfo;
using winPEAS.Info.UserInfo.LogonSessions;
using winPEAS.Info.UserInfo.Token;

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
                PrintTokenP,
                PrintClipboardText,
                PrintLoggedUsers,
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
            Dictionary<string, string> usersColors = new Dictionary<string, string>()
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
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#users-and-groups", "Check if you have some admin equivalent privileges");

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
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#token-manipulation", "Check if you can escalate privilege using some enabled token");
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
                string clipboard = Info.UserInfo.UserInfoHelper.GetClipboardText();
                if (!string.IsNullOrEmpty(clipboard))
                {
                    Beaprint.BadPrint(clipboard);
                }
                else
                {
                    if (Checks.ExecCmd)
                    {
                        Beaprint.BadPrint("    " + MyUtils.ExecCMD("-command Get-Clipboard", "powershell.exe"));
                    }
                    else
                    {
                        Beaprint.NotFoundPrint();
                        Beaprint.InfoPrint("    This C# implementation to capture the clipboard is not trustable in every Windows version");
                        Beaprint.InfoPrint("    If you want to see what is inside the clipboard execute 'powershell -command \"Get - Clipboard\"'");
                    }
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
                List<Dictionary<string, string>> rdp_sessions = Info.UserInfo.UserInfoHelper.GetRDPSessions();
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
                List<Dictionary<string, string>> PPy = Info.UserInfo.UserInfoHelper.GetPasswordPolicy();
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
                    Beaprint.NoColorPrint  ($"    Method:                       {logonSession.Method}\n" + 
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
    }
}
