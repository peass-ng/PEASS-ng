//using Colorful; // http://colorfulconsole.com/
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;

namespace winPEAS
{
    class Program
    {
        public static string version = "vBETA VERSION, Please if you find any issue let me know in https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/issues";
        public static string advisory = "winpeas should be used for authorized penetration testing and/or educational purposes only.Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own networks and/or with the network owner's permission.";
        public static bool banner = true;
        public static bool search_fast = false;
        public static int search_time = 150;
        static bool is_fast = false;
        static bool exec_cmd = false;
        public static bool notcolor = false;

        // Static blacklists
        static string strTrue = "True";
        static string strFalse = "False";
        static string badgroups = "docker|Remote |Admins|DNSAdmin|Azure Admins";//The space in Remote is important to not mix with SeShutdownRemotePrivilege
        static string badpasswd = "NotChange|NotExpi";
        static string badPrivileges = "SeImpersonatePrivilege|SeAssignPrimaryPrivilege|SeTcbPrivilege|SeBackupPrivilege|SeRestorePrivilege|SeCreateTokenPrivilege|SeLoadDriverPrivilege|SeTakeOwnershipPrivilege|SeDebugPrivilege";
        static string goodSoft = "Windows Phone Kits|Windows Kits|Windows Defender|Windows Mail|Windows Media Player|Windows Multimedia Platform|windows nt|Windows Photo Viewer|Windows Portable Devices|Windows Security|Windows Sidebar|WindowsApps|WindowsPowerShell| Windows$|Microsoft|WOW6432Node|internet explorer|Internet Explorer|Common Files";
        static string commonShares = "[a-zA-Z]+[$]";
        static string badIps = "127.0.0.1";
        static string badUAC = "No prompting|PromptForNonWindowsBinaries";
        static string goodUAC = "PromptPermitDenyOnSecureDesktop";
        static string badLAPS = "LAPS not installed";
        static string print_credStrings_limited = "[pP][aA][sS][sS][wW][a-zA-Z0-9_-]*|[pP][wW][dD][a-zA-Z0-9_-]*|[nN][aA][mM][eE]|[lL][oO][gG][iI][nN]|[cC][oO][nN][tT][rR][aA][sS][eE][a-zA-Z0-9_-]*|[cC][rR][eE][dD][eE][nN][tT][iI][aA][lL][a-zA-Z0-9_-]*|[aA][pP][iI]|[tT][oO][kK][eE][nN]|[sS][eE][sS][sS][a-zA-Z0-9_-]*";
        static string print_credStrings = print_credStrings_limited + "|[uU][sS][eE][rR][a-zA-Z0-9_-]*";
        static List<string> credStringsRegex = new List<string> { "PASSW[a-zA-Z0-9_-]*=", "PWD[a-zA-Z0-9_-]*=", "USER[a-zA-Z0-9_-]*=", "NAME=", "&LOGIN", "=LOGIN", "CONTRASEÑA[a-zA-Z0-9_-]*=", "CREDENTIAL[a-zA-Z0-9_-]*=", "API_KEY", "TOKEN" };
        static string patterns_file_creds = @"RDCMan.settings;*.rdg;*_history;.sudo_as_admin_successful;.profile;*bashrc;httpd.conf;*.plan;.htpasswd;.git-credentials;*.rhosts;hosts.equiv;Dockerfile;docker-compose.yml;credentials;credentials.db;access_tokens.db;accessTokens.json;legacy_credentials;azureProfile.json;appcmd.exe;scclient.exe;unattend.txt;*.gpg$;*.pgp$;*config*.php;elasticsearch.y*ml;kibana.y*ml;*.p12$;*.der$;*.csr$;*.cer$;known_hosts;id_rsa;id_dsa;*.ovpn;anaconda-ks.cfg;hostapd.conf;rsyncd.conf;cesi.conf;supervisord.conf;tomcat-users.xml;web.config;*.kdbx;KeePass.config;Ntds.dit;SAM;SYSTEM;FreeSSHDservice.ini;sysprep.inf;sysprep.xml;unattend.xml;unattended.xml;*vnc*.ini;*vnc*.c*nf*;*vnc*.txt;*vnc*.xml;groups.xml;services.xml;scheduledtasks.xml;printers.xml;drives.xml;datasources.xml;php.ini;https.conf;https-xampp.conf;httpd.conf;my.ini;my.cnf;access.log;error.log;server.xml;SiteList.xml;ConsoleHost_history.txt;setupinfo;setupinfo.bak";
        static string patterns_file_creds_color = "RDCMan.settings|.rdg|_history|.sudo_as_admin_successful|.profile|bashrc|httpd.conf|.plan|.htpasswd|.git-credentials|.rhosts|hosts.equiv|Dockerfile|docker-compose.yml|credentials|credentials.db|access_tokens.db|accessTokens.json|legacy_credentials|azureProfile.json|appcmd.exe|scclient.exe|unattend.txt|access.log|error.log|credential|password|.gpg|.pgp|config.php|elasticsearch|kibana.|.p12|.der|.csr|.crt|.cer|.pem|known_hosts|id_rsa|id_dsa|.ovpn|anaconda-ks.cfg|hostapd.conf|rsyncd.conf|cesi.conf|supervisord.conf|tomcat-users.xml|web.config|.kdbx|.key|KeePass.config|ntds.dir|Ntds.dit|sam|system|SAM|SYSTEM|FreeSSHDservice.ini|sysprep.inf|sysprep.xml|unattend.xml|unattended.xml|vnc|groups.xml|services.xml|scheduledtasks.xml|printers.xml|drives.xml|datasources.xml|php.ini|https.conf|https-xampp.conf|httpd.conf|my.ini|my.cnf|access.log|error.log|server.xml|SiteList.xml|setupinfo";

        // Create Dynamic blacklists
        static string currentUserName = Environment.UserName;
        public static List<string> interestingUsersGroups = new List<string> { "Everyone", "Users", "Todos", currentUserName }; //Authenticated Users (Authenticated left behin to avoid repetitions)
        public static string currentUserDomainName = Environment.UserDomainName;
        public static string currentADDomainName = "";
        public static bool partofdomain = false;
        public static bool currentUserIsLocal = true;
        static SelectQuery query = null;
        static ManagementObjectSearcher searcher = null;
        public static ManagementObjectCollection win32_users = null;
        static List<string> currentUserGroups = new List<string>();
        static string paint_activeUsers = "";
        static string paint_activeUsers_no_Administrator = "";
        static string paint_disabledUsers = "";
        static string paint_disabledUsers_no_Administrator = "";
        //static string paint_lockoutUsers = "";
        static string paint_adminUsers = "";

        public static void CreateDynamicLists()
        {
            try
            {
                Beaprint.GrayPrint("   Creating Dynamic lists, this could take a while, please wait...");
                Beaprint.GrayPrint("   - Getting AD info...");
                currentADDomainName = MyUtils.IsDomainJoined();
                partofdomain = currentADDomainName == "" ? false : true;
                currentUserIsLocal = currentADDomainName != currentUserDomainName;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting AD info: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Getting Win32_UserAccount info...");
                query = new SelectQuery("Win32_UserAccount");
                searcher = new ManagementObjectSearcher(query);
                win32_users = searcher.Get();
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting Win32_UserAccount info: " + ex);
            }

            try { 
                Beaprint.GrayPrint("   - Creating current user groups list...");
                currentUserGroups = UserInfo.GetUserGroups(currentUserName, currentUserDomainName);
                interestingUsersGroups.AddRange(currentUserGroups);
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating current user groups list: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Creating cctive users list...");
                paint_activeUsers = String.Join("|", UserInfo.GetMachineUsers(true, false, false, false, false));
                paint_activeUsers_no_Administrator = paint_activeUsers.Replace("|Administrator", "").Replace("Administrator|", "").Replace("Administrator", "");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating active users list: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Creating disabled users list...");
                paint_disabledUsers = String.Join("|", UserInfo.GetMachineUsers(false, true, false, false, false));
                paint_disabledUsers_no_Administrator = paint_disabledUsers.Replace("|Administrator", "").Replace("Administrator|", "").Replace("Administrator", "");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating disabled users list: " + ex);
            }

            //paint_lockoutUsers = String.Join("|", UserInfo.GetMachineUsers(false, false, true, false, false));

            try
            {
                Beaprint.GrayPrint("   - Admin users list...");
                paint_adminUsers = String.Join("|", UserInfo.GetMachineUsers(false, false, false, true, false));
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating admin users groups list: " + ex);
            }
        }

        public static void CheckRegANSI()
        {
            try
            {
                if (MyUtils.GetRegValue("HKCU", "CONSOLE", "VirtualTerminalLevel") == "" && MyUtils.GetRegValue("HKCU", "CONSOLE", "VirtualTerminalLevel") == "")
                    System.Console.WriteLine(@"ANSI color bit for Windows is not set. If you are execcuting this from a Windows terminal inside the host you should run 'REG ADD HKCU\Console /v VirtualTerminalLevel /t REG_DWORD /d 1'");
            }
            catch(Exception ex)
            {
                Beaprint.GrayPrint("Error while checking ansi color registry: " + ex);
            }
        }



        /////////////////////////////////////////////////
        /////////////// SYSTEM INFORMATION //////////////
        /////////////////////////////////////////////////
        private static void PrintSystemInfo()
        {
            void PrintBasicSystemInfo()
            {
                try
                {
                    Beaprint.MainPrint("Basic System Information", "T1082&T1124&T1012&T1497&T1212");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#kernel-exploits", "Check if the Windows versions is vulnerable to some known exploit");
                    Dictionary<string, string> basicDictSystem = SystemInfo.GetBasicOSInfo();
                    basicDictSystem["Hotfixes"] = Beaprint.ansi_color_good + basicDictSystem["Hotfixes"] + Beaprint.NOCOLOR;
                    Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { strTrue, Beaprint.ansi_color_bad },
                        };
                    Beaprint.DictPrint(basicDictSystem, colorsSI, false);
                    System.Console.WriteLine();
                    Watson.FindVulns();
                     //To update Watson, update the CVEs and add the new ones and update the main function so it uses new CVEs (becausfull with the Beaprints inside the FindVulns function)
                     //Usually you won't need to do anything with the classes Wmi, Vulnerability and VulnerabilityCollection
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintPSInfo()
            {
                try
                {
                    Beaprint.MainPrint("PowerShell Settings", "");
                    Dictionary<string, string> PSs = SystemInfo.GetPowerShellSettings();
                    Beaprint.DictPrint(PSs, true);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintAuditInfo()
            {
                try
                {
                    Beaprint.MainPrint("Audit Settings", "T1012");
                    Beaprint.LinkPrint("", "Check what is being logged");
                    Dictionary<string, string> auditDict = SystemInfo.GetAuditSettings();
                    Beaprint.DictPrint(auditDict, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintWEFInfo()
            {
                try
                {
                    Beaprint.MainPrint("WEF Settings", "T1012");
                    Beaprint.LinkPrint("", "Windows Event Forwarding, is interesting to know were are sent the logs");
                    Dictionary<string, string> weftDict = SystemInfo.GetWEFSettings();
                    Beaprint.DictPrint(weftDict, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintLAPSInfo()
            {
                try
                {
                    Beaprint.MainPrint("LAPS Settings", "T1012");
                    Beaprint.LinkPrint("", "If installed, local administrator password change frequently in domain-joined boxes and is restricted by ACL");
                    Dictionary<string, string> lapsDict = SystemInfo.GetLapsSettings();
                    Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { badLAPS, Beaprint.ansi_color_bad }
                        };
                    Beaprint.DictPrint(lapsDict, colorsSI, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintUserEV()
            {
                try
                {
                    Beaprint.MainPrint("User Environment Variables", "");
                    Beaprint.LinkPrint("", "Check for some passwords or keys in the env variables");
                    Dictionary<string, string> userEnvDict = SystemInfo.GetUserEnvVariables();
                    Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { print_credStrings_limited, Beaprint.ansi_color_bad }
                        };
                    Beaprint.DictPrint(userEnvDict, colorsSI, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintSystemEV()
            {
                try
                {
                    Beaprint.MainPrint("System Environment Variables", "");
                    Beaprint.LinkPrint("", "Check for some passwords or keys in the env variables");
                    Dictionary<string, string> sysEnvDict = SystemInfo.GetSystemEnvVariables();
                    Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { print_credStrings_limited, Beaprint.ansi_color_bad }
                        };
                    Beaprint.DictPrint(sysEnvDict, colorsSI, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintInetInfo()
            {
                try
                {
                    Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { "ProxyServer.*", Beaprint.ansi_color_bad }
                        };

                    Beaprint.MainPrint("HKCU Internet Settings", "T1012");
                    Dictionary<string, string> HKCUDict = SystemInfo.GetInternetSettings("HKCU");
                    Beaprint.DictPrint(HKCUDict, colorsSI, true);

                    Beaprint.MainPrint("HKLM Internet Settings", "T1012");
                    Dictionary<string, string> HKMLDict = SystemInfo.GetInternetSettings("HKLM");
                    Beaprint.DictPrint(HKMLDict, colorsSI, true);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintDrivesInfo()
            {
                try
                {
                    Beaprint.MainPrint("Drives Information", "T1120");
                    Beaprint.LinkPrint("", "Remember that you should search more info inside the other drives");
                    foreach (Dictionary<string, string> drive in SystemInfo.GetDrivesInfo())
                    {
                        string drive_permissions = String.Join(", ", MyUtils.GetPermissionsFolder(drive["Name"], interestingUsersGroups));
                        string dToPrint = "    {0} (Type: {1})";
                        if (drive["Volume label"] != "")
                            dToPrint += "(Volume label: {2})";

                        if (drive["Filesystem"] != "")
                            dToPrint += "(Filesystem: {3})";

                        if (drive["Available space"] != "")
                            dToPrint += "(Available space: {4} GB)";

                        if (drive_permissions.Length > 0)
                            dToPrint += "(Permissions: {5})";

                        Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                            {
                                { "Permissions.*", Beaprint.ansi_color_bad}
                            };
                        Beaprint.AnsiPrint(String.Format(dToPrint, drive["Name"], drive["Type"], drive["Volume label"], drive["Filesystem"], (((Int64.Parse(drive["Available space"]) / 1024) / 1024) / 1024).ToString(), drive_permissions), colorsSI);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintAVInfo()
            {
                try
                {
                    Beaprint.MainPrint("AV Information", "T1063");
                    Dictionary<string, string> AVInfo = SystemInfo.GetAVInfo();
                    if (AVInfo.ContainsKey("Name") && AVInfo["Name"].Length > 0)
                        Beaprint.GoodPrint("    Some AV was detected, search for bypasses");
                    else
                        Beaprint.BadPrint("    No AV was detected!!");

                    Beaprint.DictPrint(AVInfo, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintUACInfo()
            {
                try
                {
                    Beaprint.MainPrint("UAC Status", "T1012");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#basic-uac-bypass-full-file-system-access", "If you are in the Administrators group check how to bypass the UAC");
                    Dictionary<string, string> uacDict = SystemInfo.GetUACSystemPolicies();

                    Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { badUAC, Beaprint.ansi_color_bad },
                            { goodUAC, Beaprint.ansi_color_good }
                        };
                    Beaprint.DictPrint(uacDict, colorsSI, false);

                    if ((uacDict["EnableLUA"] == "") || (uacDict["EnableLUA"] == "0"))
                        Beaprint.BadPrint("      [*] EnableLUA != 1, UAC policies disabled.\r\n      [+] Any local account can be used for lateral movement.");

                    if ((uacDict["EnableLUA"] == "1") && (uacDict["LocalAccountTokenFilterPolicy"] == "1"))
                        Beaprint.BadPrint("      [*] LocalAccountTokenFilterPolicy set to 1.\r\n      [+] Any local account can be used for lateral movement.");

                    if ((uacDict["EnableLUA"] == "1") && (uacDict["LocalAccountTokenFilterPolicy"] != "1") && (uacDict["FilterAdministratorToken"] != "1"))
                        Beaprint.GoodPrint("      [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken != 1.\r\n      [-] Only the RID-500 local admin account can be used for lateral movement.");

                    if ((uacDict["EnableLUA"] == "1") && (uacDict["LocalAccountTokenFilterPolicy"] != "1") && (uacDict["FilterAdministratorToken"] == "1"))
                        Beaprint.GoodPrint("      [*] LocalAccountTokenFilterPolicy set to 0 and FilterAdministratorToken == 1.\r\n      [-] No local accounts can be used for lateral movement.");
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            Beaprint.GreatPrint("System Information");
            PrintBasicSystemInfo();
            PrintPSInfo();
            PrintAuditInfo();
            PrintWEFInfo();
            PrintLAPSInfo();
            PrintUserEV();
            PrintSystemEV();
            PrintInetInfo();
            PrintDrivesInfo();
            PrintAVInfo();
            PrintUACInfo();
        }



        /////////////////////////////////////////////////
        /////////////// USERS INFORMATION ///////////////
        /////////////////////////////////////////////////
        private static void PrintInfoUsers()
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

            Dictionary<string, string> colorsU()
            {
                Dictionary<string, string> usersColors = new Dictionary<string, string>()
                {
                    { paint_activeUsers_no_Administrator, Beaprint.ansi_users_active },
                    { currentUserName + "|"+ currentUserDomainName, Beaprint.ansi_current_user },
                    { paint_adminUsers+"|"+ badgroups + "|" + badgroups + "|" + badpasswd + "|" + badPrivileges + "|" + "DefaultPassword.*", Beaprint.ansi_color_bad },
                    { @"Disabled", Beaprint.ansi_users_disabled },
                };

                if (paint_disabledUsers.Length > 1)
                    usersColors[paint_disabledUsers_no_Administrator] = Beaprint.ansi_users_disabled;
                return usersColors;
            }

            void PrintCU()
            {
                try
                {
                    Beaprint.MainPrint("Current users", "T1087&T1069&T1033");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#users-and-groups", "Check if you have some admin equivalent privileges");
                    List<string> users_grps = UserInfo.GetMachineUsers(false, false, false, false, true);

                    Beaprint.AnsiPrint("  Current user: " + currentUserName, colorsU());
                    Beaprint.ListPrint(users_grps, colorsU());
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintTokenP()
            {
                try
                {
                    Beaprint.MainPrint("Current Token privileges", "T1134");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#token-manipulation", "Check if you can escalate privilege using some enabled token");
                    Dictionary<string, string> token_privs = UserInfo.GetTokenGroupPrivs();
                    Beaprint.DictPrint(token_privs, colorsU(), false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintClipboardText()
            {
                try
                {
                    Beaprint.MainPrint("Clipboard text", "T1134");
                    string clipb = UserInfo.GetClipboardText();
                    if (String.IsNullOrEmpty(clipb))
                        Beaprint.BadPrint(clipb);
                    else
                    {
                        if (exec_cmd)
                            Beaprint.BadPrint("    " + MyUtils.ExecCMD("-command Get-Clipboard", "powershell.exe"));
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
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintLoggedUsers()
            {
                try
                {
                    Beaprint.MainPrint("Logged users", "T1087&T1033");
                    List<string> loggedusers = UserInfo.GetLoggedUsers();

                    Beaprint.ListPrint(loggedusers, colorsU());
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintRdpSessions()
            {
                try
                {
                    Beaprint.MainPrint("RDP Sessions", "T1087&T1033");
                    List<Dictionary<string, string>> rdp_sessions = UserInfo.GetRDPSessions();
                    if (rdp_sessions.Count > 0)
                    {
                        string format = "    {0,-10}{1,-15}{2,-15}{3,-25}{4,-10}{5}";
                        string header = String.Format(format, "SessID", "pSessionName", "pUserName", "pDomainName", "State", "SourceIP");
                        Beaprint.GrayPrint(header);
                        foreach (Dictionary<string, string> rdp_ses in rdp_sessions)
                            Beaprint.AnsiPrint(String.Format(format, rdp_ses["SessionID"], rdp_ses["pSessionName"], rdp_ses["pUserName"], rdp_ses["pDomainName"], rdp_ses["State"], rdp_ses["SourceIP"]), colorsU());
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintEverLoggedUsers()
            {
                try
                {
                    Beaprint.MainPrint("Ever logged users", "T1087&T1033");
                    List<string> everlogged = UserInfo.GetEverLoggedUsers();
                    Beaprint.ListPrint(everlogged, colorsU());
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintAutoLogin()
            {
                try
                {
                    Beaprint.MainPrint("Looking for AutoLogon credentials", "T1012");
                    bool ban = false;
                    Dictionary<string, string> autologon = UserInfo.GetAutoLogon();
                    if (autologon.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> entry in autologon)
                        {
                            if (!String.IsNullOrEmpty(entry.Value))
                            {
                                if (!ban)
                                {
                                    Beaprint.BadPrint("Some AutoLogon credentials were found!!");
                                    ban = true;
                                }
                                Beaprint.AnsiPrint(String.Format("    {0,-30}:  {1}", entry.Key, entry.Value), colorsU());
                            }
                        }
                        if (!ban)
                            Beaprint.NotFoundPrint();
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintHomeFolders()
            {
                try
                {
                    Beaprint.MainPrint("Home folders found", "T1087&T1083&T1033");
                    List<string> user_folders = UserInfo.GetUsersFolders();
                    foreach (string ufold in user_folders)
                    {
                        string perms = String.Join(", ", MyUtils.GetPermissionsFolder(ufold, interestingUsersGroups));
                        if (perms.Length > 0)
                            Beaprint.BadPrint("    " + ufold + " : " + perms);
                        else
                            Beaprint.GoodPrint("    " + ufold);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintPasswordPolicies()
            {
                try
                {
                    Beaprint.MainPrint("Password Policies", "T1201");
                    Beaprint.LinkPrint("", "Check for a possible brute-force");
                    List<Dictionary<string, string>> PPy = UserInfo.GetPasswordPolicy();
                    Beaprint.DictPrint(PPy, colorsU(), false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }


            Beaprint.GreatPrint("Users Information");
            PrintCU();
            PrintTokenP();
            PrintClipboardText();
            PrintLoggedUsers();
            PrintRdpSessions();
            PrintEverLoggedUsers();
            PrintAutoLogin();
            PrintHomeFolders();
            PrintPasswordPolicies();
        }



        /////////////////////////////////////////////////
        ///////////// PROCESSES INFORMATION /////////////
        /////////////////////////////////////////////////
        private static void PrintInfoProcesses()
        {
            void PrintInterestingProcesses()
            {
                /* Colors Code
                 * RED:
                 * ---- Write privileges in path
                 * ---- Different Owner than myself
                 * GREEN:
                 * ---- No Write privileges in path
                 * MAGENTA:
                 * ---- Current username
                */
                try
                {
                    Beaprint.MainPrint("Interesting Processes -non Microsoft-", "T1010&T1057&T1007");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#running-processes", "Check if any interesting proccesses for memmory dump or if you could overwrite some binary running");
                    List<Dictionary<string, string>> processes_info = ProcessesInfo.GetProcessInfo();
                    foreach (Dictionary<string, string> proc_info in processes_info)
                    {
                        if (ProcessesInfo.defensiveProcesses.ContainsKey(proc_info["Name"]))
                        {
                            proc_info["Product"] = ProcessesInfo.defensiveProcesses[proc_info["Name"]].ToString();
                        }
                        else if (ProcessesInfo.interestingProcesses.ContainsKey(proc_info["Name"]))
                        {
                            proc_info["Product"] = ProcessesInfo.interestingProcesses[proc_info["Name"]].ToString();
                        }
                        else if (ProcessesInfo.browserProcesses.ContainsKey(proc_info["Name"]))
                        {
                            proc_info["Product"] = ProcessesInfo.browserProcesses[proc_info["Name"]].ToString();
                        }

                        List<string> file_rights = MyUtils.GetPermissionsFile(proc_info["ExecutablePath"], interestingUsersGroups);
                        List<string> dir_rights = new List<string>();
                        if (proc_info["ExecutablePath"] != null && proc_info["ExecutablePath"] != "")
                            dir_rights = MyUtils.GetPermissionsFolder(Path.GetDirectoryName(proc_info["ExecutablePath"]), interestingUsersGroups);

                        string formString = "    {0}({1})[{2}]";
                        if (proc_info["Product"] != null && proc_info["Product"].Length > 1)
                            formString += ": {3}";
                        if (proc_info["Owner"].Length > 1)
                            formString += " -- POwn: {4}";
                        if (proc_info["isDotNet"].Length > 1)
                            formString += " -- {5}";
                        if (file_rights.Count > 0)
                            formString += "\n    Permissions: {6}";
                        if (dir_rights.Count > 0)
                            formString += "\n    Possible DLL Hijacking folder: {7} ({8})";
                        if (proc_info["CommandLine"].Length > 1)
                            formString += "\n    "+ Beaprint.ansi_color_gray + "Command Line: {9}";

                        Dictionary<string, string> colorsP = new Dictionary<string, string>()
                        {
                            { " "+currentUserName, Beaprint.ansi_current_user },
                            { "Permissions:.*", Beaprint.ansi_color_bad },
                            { "Possible DLL Hijacking.*", Beaprint.ansi_color_bad },
                            { proc_info["ExecutablePath"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+")+"[^\"^']", (file_rights.Count > 0 || dir_rights.Count > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                        };
                        Beaprint.AnsiPrint(String.Format(formString, proc_info["Name"], proc_info["ProcessID"], proc_info["ExecutablePath"], proc_info["Product"], proc_info["Owner"], proc_info["isDotNet"], String.Join(", ", file_rights), dir_rights.Count > 0 ? Path.GetDirectoryName(proc_info["ExecutablePath"]) : "", String.Join(", ", dir_rights), proc_info["CommandLine"]), colorsP);
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            Beaprint.GreatPrint("Processes Information");
            PrintInterestingProcesses();
        }



        /////////////////////////////////////////////////
        ////////////// SERVICES INFORMATION /////////////
        /////////////////////////////////////////////////
        private static void PrintInfoServices()
        {
            void PrintInterestingServices()
            {
                /* Colors Code
                 * RED:
                 * ---- Write privilege in path or path without quotes and some space
                 * ---- Startmode = Auto
                 * GREEN:
                 * ---- No write privileges
                 * ---- Startmode = Manual
                */
                try
                {
                    Beaprint.MainPrint("Interesting Services -non Microsoft-", "T1007");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#services", "Check if you can overwrite some service binary or perform a DLL hijacking, also cehck for unquoted paths");
                    List<Dictionary<string, string>> services_info = ServicesInfo.GetNonstandardServices();
                    if (services_info.Count < 1)
                        services_info = ServicesInfo.GetNonstandardServicesFromReg();
                    foreach (Dictionary<string, string> service_info in services_info)
                    {
                        List<string> file_rights = MyUtils.GetPermissionsFile(service_info["FilteredPath"], interestingUsersGroups);
                        List<string> dir_rights = new List<string>();
                        if (service_info["FilteredPath"] != null && service_info["FilteredPath"] != "")
                            dir_rights = MyUtils.GetPermissionsFolder(Path.GetDirectoryName(service_info["FilteredPath"]), interestingUsersGroups);

                        bool no_quotes_and_space = MyUtils.CheckQuoteAndSpace(service_info["PathName"]);

                        string formString = "    {0}(";
                        if (service_info["CompanyName"] != null && service_info["CompanyName"].Length > 1)
                            formString += "{1} - ";
                        if (service_info["DisplayName"].Length > 1)
                            formString += "{2}";
                        formString += ")";
                        if (service_info["PathName"].Length > 1)
                            formString += "[{3}]";
                        if (service_info["StartMode"].Length > 1)
                            formString += " - {4}";
                        if (service_info["State"].Length > 1)
                            formString += " - {5}";
                        if (service_info["isDotNet"].Length > 1)
                            formString += " - {6}";
                        if (no_quotes_and_space)
                            formString += " - {7}";
                        if (file_rights.Count > 0)
                            formString += "\n    File Permissions: {8}";
                        if (dir_rights.Count > 0)
                            formString += "\n    Possible DLL Hijacking in binary folder: {9} ({10})";
                        if (service_info["Description"].Length > 1)
                            formString += "\n    "+Beaprint.ansi_color_gray+"{11}";

                        {
                            Dictionary<string, string> colorsS = new Dictionary<string, string>()
                            {
                                { "File Permissions:.*", Beaprint.ansi_color_bad },
                                { "Possible DLL Hijacking.*", Beaprint.ansi_color_bad },
                                { "No quotes and Space detected", Beaprint.ansi_color_bad },
                                { service_info["PathName"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (file_rights.Count > 0 || dir_rights.Count > 0 || no_quotes_and_space) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            };
                            Beaprint.AnsiPrint(String.Format(formString, service_info["Name"], service_info["CompanyName"], service_info["DisplayName"], service_info["PathName"], service_info["StartMode"], service_info["State"], service_info["isDotNet"], "No quotes and Space detected", String.Join(", ", file_rights), dir_rights.Count > 0 ? Path.GetDirectoryName(service_info["FilteredPath"]) : "", String.Join(", ", dir_rights), service_info["Description"]), colorsS);
                        }
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintWritableRegServices()
            {
                try
                {
                    Beaprint.MainPrint("Looking if you can modify any service registry", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#services-registry-permissions", "Check if you can modify the registry of a service");
                    List<string> overWriteServs = ServicesInfo.GetWriteServiceRegs();
                    if (overWriteServs.Count <= 0)
                        Beaprint.GoodPrint("    [-] Looks like you cannot change the registry of any service...");
                    else
                    {
                        foreach (string writeServReg in overWriteServs)
                            Beaprint.BadPrint("    [!] Looks like you can change the binpath the service in " + writeServReg + " !!");
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintPathDLLHijacking()
            {
                try
                {
                    Beaprint.MainPrint("Checking write permissions in PATH folders (DLL Hijacking)", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#dll-hijacking", "Check for DLL Hijacking in PATH folders");
                    Dictionary<string, string> path_dllhijacking = ServicesInfo.GetPathDLLHijacking();
                    foreach (KeyValuePair<string, string> entry in path_dllhijacking)
                    {
                        if (String.IsNullOrEmpty(entry.Value))
                            Beaprint.GoodPrint("    " + entry.Key);
                        else
                            Beaprint.BadPrint("    (DLL Hijacking) " + entry.Key + ": " + entry.Value);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }


            Beaprint.GreatPrint("Services Information");
            PrintInterestingServices();
            PrintWritableRegServices();
            PrintPathDLLHijacking();
        }



        /////////////////////////////////////////////////
        //////////// APPLICATION INFORMATION ////////////
        /////////////////////////////////////////////////
        private static void PrintInfoApplications()
        {
            void PrintActiveWindow()
            {
                try
                {
                    Beaprint.MainPrint("Current Active Window Application", "T1010&T1518");
                    string title = ApplicationInfo.GetActiveWindowTitle();
                    List<string> permsFile = MyUtils.GetPermissionsFile(title, interestingUsersGroups);
                    List<string> permsFolder = MyUtils.GetPermissionsFolder(title, interestingUsersGroups);
                    if (permsFile.Count > 0)
                    {
                        Beaprint.BadPrint("    " + title);
                        Beaprint.BadPrint("    FilePermissions: " + String.Join(",", permsFile));
                    }
                    else
                        Beaprint.GoodPrint("    " + title);

                    if (permsFolder.Count > 0)
                    {
                        Beaprint.BadPrint("    Possible DLL Hijacking, folder is writable: " + MyUtils.GetFolderFromString(title));
                        Beaprint.BadPrint("    FolderPermissions: " + String.Join(",", permsFile));
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintInstalledApps()
            {
                try
                {
                    Beaprint.MainPrint("Installed Applications --Via Program Files--", "T1083&T1012&T1010&T1518");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#software", "Check if you can modify installed software");
                    Dictionary<string, Dictionary<string, string>> InstalledAppsPerms = ApplicationInfo.GetInstalledAppsPerms();
                    string format = "    ==>  {0} ({1})";
                    foreach (KeyValuePair<string, Dictionary<string, string>> app in InstalledAppsPerms)
                    {
                        if (String.IsNullOrEmpty(app.Value.ToString())) //If empty, nothing found, is good
                            Beaprint.GoodPrint(app.Key);

                        else //Then, we need to look deeper
                        {
                            //Checkeamos si la carpeta (que va a existir como subvalor dentro de si misma) debe ser good
                            if (String.IsNullOrEmpty(app.Value[app.Key]))
                                Beaprint.GoodPrint("    " + app.Key);

                            else
                            {
                                Beaprint.BadPrint(String.Format("    {0}({1})", app.Key, app.Value[app.Key]));
                                app.Value[app.Key] = ""; //So no reprinted later
                            }

                            //Check the rest of the values to see if we have something to print in red (permissions)
                            foreach (KeyValuePair<string, string> subfolder in app.Value)
                            {
                                if (!String.IsNullOrEmpty(subfolder.Value))
                                    Beaprint.BadPrint(String.Format(format, subfolder.Key, subfolder.Value));
                            }
                        }
                    }
                    System.Console.WriteLine();

                    Beaprint.MainPrint("Installed Applications --Via Registry--", "T1083&T1012&T1010");

                    Dictionary<string, string> colorsA = new Dictionary<string, string>()
                    {
                        { goodSoft, Beaprint.ansi_color_good }
                    };
                    Beaprint.ListPrint(ApplicationInfo.GetAppsRegistry(), colorsA);
                }
                catch
                {
                    //Beaprint.GrayPrint(String.Format("{0}",ex));
                }
            }

            void PrintAutoRuns()
            {
                try
                {
                    Beaprint.MainPrint("Autorun Applications", "T1010");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#run-at-startup", "Check if you can modify other users AutoRuns binaries");
                    List<Dictionary<string, string>> apps = ApplicationInfo.GetAutoRuns();

                    foreach (Dictionary<string, string> app in apps)
                    {
                        Dictionary<string, string> colorsA = new Dictionary<string, string>()
                        {
                            { "FolderPerms:.*", Beaprint.ansi_color_bad },
                            { "FilePerms:.*", Beaprint.ansi_color_bad },
                            { "(Unquoted and Space detected)", Beaprint.ansi_color_bad },
                            { ".*(RegPath is writable)", Beaprint.ansi_color_bad },
                            { (app["Folder"].Length > 0) ? app["Folder"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "ouigyevb2uivydi2u3id2ddf3", !String.IsNullOrEmpty(app["interestingFolderRights"]) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { (app["File"].Length > 0) ? app["File"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "adu8v298hfubibuidiy2422r", !String.IsNullOrEmpty(app["interestingFileRights"]) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            { (app["Reg"].Length > 0) ? app["Reg"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+") : "o8a7eduia37ibduaunbf7a4g7ukdhk4ua", (app["isWritableReg"].ToLower() == "true") ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                        };
                        string line = "";

                        if (!String.IsNullOrEmpty(app["Folder"]))
                            line += "    Folder: " + app["Folder"];

                        if (!String.IsNullOrEmpty(app["interestingFolderRights"]))
                        {
                            line += "\n    FolderPerms: " + app["interestingFolderRights"];
                        }

                        string filepath_mod = app["File"].Replace("\"", "").Replace("'", "");
                        if (!String.IsNullOrEmpty(app["File"]))
                            line += "\n    File: " + filepath_mod;

                        if (app["isUnquotedSpaced"].ToLower() == "true")
                            line += " (Unquoted and Space detected)";                    

                        if (!String.IsNullOrEmpty(app["interestingFileRights"]))
                            line += "\n    FilePerms: " + app["interestingFileRights"];

                        if (!String.IsNullOrEmpty(app["Reg"]))
                            line += "\n    RegPath: " + app["Reg"];

                        if (app["isWritableReg"].ToLower() == "true")
                            line += " (RegPath is writable)";
                        
                        Beaprint.AnsiPrint(line, colorsA);
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintScheduled()
            {
                try
                {
                    Beaprint.MainPrint("Scheduled Applications --Non Microsoft--", "T1010");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#run-at-startup", "Check if you can modify other users scheduled binaries");
                    List<Dictionary<string, string>> scheduled_apps = ApplicationInfo.GetScheduledAppsNoMicrosoft();

                    foreach (Dictionary<string, string> sapp in scheduled_apps)
                    {
                        List<string> file_rights = MyUtils.GetPermissionsFile(sapp["Action"], interestingUsersGroups);
                        List<string> dir_rights = MyUtils.GetPermissionsFolder(sapp["Action"], interestingUsersGroups);
                        string formString = "    ({0}) {1}: {2}";
                        if (file_rights.Count > 0)
                            formString += "\n    Permissions file: {3}";
                        if (dir_rights.Count > 0)
                            formString += "\n    Permissions folder(DLL Hijacking): {4}";
                        if (!String.IsNullOrEmpty(sapp["Trigger"]))
                            formString += "\n    Trigger: {5}";
                        if (String.IsNullOrEmpty(sapp["Description"]))
                            formString += "\n    {6}";

                        Dictionary<string, string> colorsS = new Dictionary<string, string>()
                        {
                            { "Permissions.*", Beaprint.ansi_color_bad },
                            { sapp["Action"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (file_rights.Count > 0 || dir_rights.Count > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                        };
                        Beaprint.AnsiPrint(String.Format(formString, sapp["Author"], sapp["Name"], sapp["Action"], String.Join(", ", file_rights), String.Join(", ", dir_rights), sapp["Trigger"], sapp["Description"]), colorsS);
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }


            Beaprint.GreatPrint("Applications Information");
            PrintActiveWindow();
            PrintInstalledApps();
            PrintAutoRuns();
            PrintScheduled();
        }



        /////////////////////////////////////////////////
        ////////////// NETWORK INFORMATION //////////////
        /////////////////////////////////////////////////
        private static void PrintInfoNetwork()
        {
            void PrintNetShares()
            {
                try
                {
                    Beaprint.MainPrint("Network Shares", "T1135");
                    Dictionary<string, string> colorsN = new Dictionary<string, string>()
                    {
                        { commonShares, Beaprint.ansi_color_good },
                        { "Permissions.*", Beaprint.ansi_color_bad }
                    };
                    List<Dictionary<string, string>> shares = NetworkInfo.GetNetworkShares("127.0.0.1");
                    foreach(Dictionary<string, string> share in shares)
                    {
                        string line = String.Format("    {0} (" + Beaprint.ansi_color_gray + "Path: {1}" + Beaprint.NOCOLOR + ")", share["Name"], share["Path"]);
                        if (share["Permissions"].Length > 0)
                            line += " -- Permissions: " + share["Permissions"];
                        Beaprint.AnsiPrint(line, colorsN);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintHostsFile()
            {
                try
                {
                    Beaprint.MainPrint("Host File", "T1016");
                    string[] lines = File.ReadAllLines(@Path.GetPathRoot(Environment.SystemDirectory) + @"\windows\system32\drivers\etc\hosts");
                    foreach (string line in lines)
                    {
                        if (line.Length > 0 && line[0] != '#')
                            System.Console.WriteLine("    " + line.Replace("\t","    "));
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintNetworkIfaces()
            {
                try
                {
                    Beaprint.MainPrint("Network Ifaces and known hosts", "T1016");
                    Beaprint.LinkPrint("", "The masks are only for the IPv4 addresses");
                    foreach (Dictionary<string, string> card in NetworkInfo.GetNetCardInfo())
                    {
                        string formString = "    {0}[{1}]: {2} / {3}";
                        if (card["Gateways"].Length > 1)
                            formString += "\n        "+Beaprint.ansi_color_gray+"Gateways: "+Beaprint.NOCOLOR+"{4}";
                        if (card["DNSs"].Length > 1)
                            formString += "\n        " + Beaprint.ansi_color_gray + "DNSs: " + Beaprint.NOCOLOR + "{5}";
                        if (card["arp"].Length > 1)
                            formString += "\n        " + Beaprint.ansi_color_gray + "Known hosts:" + Beaprint.NOCOLOR + "\n{6}";

                        System.Console.WriteLine(String.Format(formString, card["Name"], card["PysicalAddr"], card["IPs"], card["Netmasks"].Replace(", 0.0.0.0", ""), card["Gateways"], card["DNSs"], card["arp"]));
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintListeningPorts()
            {
                try
                {
                    Beaprint.MainPrint("Current Listening Ports", "T1049&T1049");
                    Beaprint.LinkPrint("", "Check for services restricted from the outside");
                    List<List<string>> conns = NetworkInfo.GetNetConnections();

                    Dictionary<string, string> colorsN = new Dictionary<string, string>()
                    {
                        { badIps, Beaprint.ansi_color_bad },
                    };

                    foreach (List<string> conn in conns)
                    {
                        if (conn[0].Contains("UDP") && conn[1].Contains("0.0.0.0:") && (conn[1].Split(':')[1].Length > 4))
                            continue;
                        Beaprint.AnsiPrint(String.Format("    {0,-10}{1,-23}{2,-23}{3}", conn[0], conn[1], conn[2], conn[3]), colorsN);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintFirewallRules()
            {
                try
                {
                    Beaprint.MainPrint("Firewall Rules", "T1016");
                    Beaprint.LinkPrint("", "Showing only DENY rules (too many ALLOW rules always)");
                    Dictionary<string, string> colorsN = new Dictionary<string, string>()
                        {
                            { strFalse, Beaprint.ansi_color_bad },
                            { strTrue, Beaprint.ansi_color_good },
                        };
                    
                    Beaprint.AnsiPrint("    Current Profiles: " + NetworkInfo.GetFirewallProfiles(), colorsN);
                    foreach (KeyValuePair<string, string> entry in NetworkInfo.GetFirewallBooleans())
                        Beaprint.AnsiPrint(String.Format("    {0,-23}:    {1}", entry.Key, entry.Value), colorsN);

                    Beaprint.GrayPrint("    DENY rules:");
                    foreach (Dictionary<string, string> rule in NetworkInfo.GetFirewallRules())
                    {
                        string file_perms = String.Join(", ", MyUtils.GetPermissionsFile(rule["AppName"], interestingUsersGroups));
                        string folder_perms = String.Join(", ", MyUtils.GetPermissionsFolder(rule["AppName"], interestingUsersGroups));
                        string formString = "    ({0}){1}[{2}]: {3} {4} {5} from {6} --> {7}";
                        if (file_perms.Length > 0)
                            formString += "\n    File Permissions: {8}";
                        if (folder_perms.Length > 0)
                            formString += "\n    Folder Permissions: {9}";
                        formString += "\n    {10}";

                        colorsN = new Dictionary<string, string>()
                            {
                                { strFalse, Beaprint.ansi_color_bad },
                                { strTrue, Beaprint.ansi_color_good },
                                { "File Permissions.*|Folder Permissions.*", Beaprint.ansi_color_bad },
                                { rule["AppName"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?").Replace("+","\\+"), (file_perms.Length > 0 || folder_perms.Length > 0) ? Beaprint.ansi_color_bad : Beaprint.ansi_color_good },
                            };
                        Beaprint.AnsiPrint(String.Format(formString, rule["Profiles"], rule["Name"], rule["AppName"], rule["Action"], rule["Protocol"], rule["Direction"], rule["Direction"] == "IN" ? rule["Local"] : rule["Remote"], rule["Direction"] == "IN" ? rule["Remote"] : rule["Local"], file_perms, folder_perms, rule["Description"]), colorsN);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintDNSCache()
            {
                try
                {
                    Beaprint.MainPrint("DNS cached --limit 70--", "T1016");
                    Beaprint.GrayPrint(String.Format("    {0,-38}{1,-38}{2}", "Entry", "Name", "Data"));
                    List<Dictionary<string, string>> DNScache = NetworkInfo.GetDNSCache();
                    foreach (Dictionary<string, string> entry in DNScache.GetRange(0, DNScache.Count <= 70 ? DNScache.Count : 70))
                        System.Console.WriteLine(String.Format("    {0,-38}{1,-38}{2}", entry["Entry"], entry["Name"], entry["Data"]));
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }


            Beaprint.GreatPrint("Network Information");
            PrintNetShares();
            PrintHostsFile();
            PrintNetworkIfaces();
            PrintListeningPorts();
            PrintFirewallRules();
            PrintDNSCache();
        }



        /////////////////////////////////////////////////
        ////////////// WINDOWS CREDENTIALS //////////////
        /////////////////////////////////////////////////
        private static void PrintWindowsCreds()
        {
            void PrintvaultCreds()
            {
                try
                {
                    Beaprint.MainPrint("Checking Windows Vault", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-manager-windows-vault");
                    List<Dictionary<string, string>> vault_creds = KnownFileCredsInfo.DumpVault();

                    Dictionary<string, string> colorsC = new Dictionary<string, string>()
                    {
                        { "Identity.*|Credential.*|Resource.*", Beaprint.ansi_color_bad },
                    };
                    Beaprint.DictPrint(vault_creds, colorsC, true, true);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintCredManag()
            {
                try
                {
                    Beaprint.MainPrint("Checking Credential manager", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-manager-windows-vault");
                    if (exec_cmd)
                    {
                        Dictionary<string, string> colorsC = new Dictionary<string, string>()
                        {
                            { "User:.*", Beaprint.ansi_color_bad },
                        };
                        Beaprint.AnsiPrint(MyUtils.ExecCMD("/list", "cmdkey.exe"), colorsC);
                        Beaprint.InfoPrint("If any cred was found, you can use it with 'runas /savecred'");
                    }
                    else
                    {
                        Beaprint.GrayPrint("    This function is not yet implemented.");
                        Beaprint.InfoPrint("If you want to list credentials inside Credential Manager use 'cmdkey /list'");
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }

            }

            void PrintSavedRDPInfo()
            {
                try
                {
                    Beaprint.MainPrint("Saved RDP connections", "");

                    List<Dictionary<string, string>> rdps_info = KnownFileCredsInfo.GetSavedRDPConnections();
                    if (rdps_info.Count > 0)
                        System.Console.WriteLine(String.Format("    {0,-20}{1,-55}{2}", "Host", "Username Hint", "User SID"));
                    else
                        Beaprint.NotFoundPrint();

                    foreach (Dictionary<string, string> rdp_info in rdps_info)
                        System.Console.WriteLine(String.Format("    {0,-20}{1,-55}{2}", rdp_info["Host"], rdp_info["Username Hint"], rdp_info["SID"]));
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintRecentRunCommands()
            {
                try
                {
                    Beaprint.MainPrint("Recently run commands", "");
                    Dictionary<string, object> recentCommands = KnownFileCredsInfo.GetRecentRunCommands();
                    Beaprint.DictPrint(recentCommands, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintDPAPIMasterKeys()
            {
                try
                {
                    Beaprint.MainPrint("Checking for DPAPI Master Keys", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#dpapi");
                    List<Dictionary<string, string>> master_keys = KnownFileCredsInfo.ListMasterKeys();
                    if (master_keys.Count != 0)
                    {
                        Beaprint.DictPrint(master_keys, true);

                        if (MyUtils.IsHighIntegrity())
                            Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the masterkey.");
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintDpapiCredFiles()
            {
                try
                {
                    Beaprint.MainPrint("Checking for Credential Files", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#dpapi");
                    List<Dictionary<string, string>> cred_files = KnownFileCredsInfo.GetCredFiles();
                    Beaprint.DictPrint(cred_files, false);
                    if (cred_files.Count != 0)
                        Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the creds file");
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintRCManFiles()
            {
                try
                {
                    Beaprint.MainPrint("Checking for RDCMan Settings Files", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#remote-desktop-credential-manager", "Dump credentials from Remote Desktop Connection Manager");
                    List<Dictionary<string, string>> rdc_files = KnownFileCredsInfo.GetRDCManFiles();
                    Beaprint.DictPrint(rdc_files, false);
                    if (rdc_files.Count != 0)
                        Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the .rdg file");
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintKerberosTickets()
            {
                try
                {
                    Beaprint.MainPrint("Looking for kerberos tickets", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/pentesting/pentesting-kerberos-88");
                    List<Dictionary<string, string>> kerberos_tckts = KnownFileCredsInfo.ListKerberosTickets();
                    Beaprint.DictPrint(kerberos_tckts, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintKerberosTGTTickets()
            {
                try
                {
                    Beaprint.MainPrint("Looking for kerberos TGT tickets", "");
                    List<Dictionary<string, string>> kerberos_tgts = KnownFileCredsInfo.GetKerberosTGTData();
                    Beaprint.DictPrint(kerberos_tgts, false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintWifi()
            {
                try
                {
                    Beaprint.MainPrint("Looking saved Wifis", "");
                    if (exec_cmd)
                    {
                        Dictionary<string, string> colorsC = new Dictionary<string, string>()
                        {
                            { ": .*", Beaprint.ansi_color_bad },
                        };
                        Beaprint.AnsiPrint("    " + MyUtils.ExecCMD("wlan show profile", "netsh.exe"), colorsC);
                    }
                    else
                    {
                        Beaprint.GrayPrint("    This function is not yet implemented.");
                        Beaprint.InfoPrint("If you want to list saved Wifis connections you can list the using 'netsh wlan show profile'");
                    }
                    Beaprint.InfoPrint("If you want to get the clear-text password use 'netsh wlan show profile <SSID> key=clear'");
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintAppCmd()
            {
                try
                {
                    Beaprint.MainPrint("Looking AppCmd.exe", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#appcmd-exe");
                    if (File.Exists(Environment.ExpandEnvironmentVariables(@"%systemroot%\system32\inetsrv\appcmd.exe")))
                        Beaprint.BadPrint("    AppCmd.exe was found in " + Environment.ExpandEnvironmentVariables(@"%systemroot%\system32\inetsrv\appcmd.exe You should try to search for credentials"));
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintSCClient()
            {
                try
                {
                    Beaprint.MainPrint("Looking SSClient.exe", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#scclient-sccm");
                    if (File.Exists(Environment.ExpandEnvironmentVariables(@"%systemroot%\Windows\CCM\SCClient.exe")))
                        Beaprint.BadPrint("    SCClient.exe was found in " + Environment.ExpandEnvironmentVariables(@"%systemroot%\Windows\CCM\SCClient.exe DLL Side loading?"));
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintAlwaysInstallElevated()
            {
                try
                {
                    Beaprint.MainPrint("Checking AlwaysInstallElevated", "T1012");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#alwaysinstallelevated");
                    string path = "Software\\Policies\\Microsoft\\Windows\\Installer";
                    string HKLM_AIE = MyUtils.GetRegValue("HKLM", path, "AlwaysInstallElevated");
                    string HKCU_AIE = MyUtils.GetRegValue("HKCU", path, "AlwaysInstallElevated");
                    if (HKLM_AIE == "1")
                        Beaprint.BadPrint("    AlwaysInstallElevated set to 1 in HKLM!");
                    if (HKCU_AIE == "1")
                        Beaprint.BadPrint("    AlwaysInstallElevated set to 1 in HKCU!");
                    if (HKLM_AIE != "1" && HKCU_AIE != "1")
                        Beaprint.GoodPrint("    AlwaysInstallElevated isn't available");
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintWSUS()
            {
                try
                {
                    Beaprint.MainPrint("Checking WSUS", "T1012");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#wsus");
                    string path = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate";
                    string path2 = "Software\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU";
                    string HKLM_WSUS = MyUtils.GetRegValue("HKLM", path, "WUServer");
                    string using_HKLM_WSUS = MyUtils.GetRegValue("HKLM", path, "UseWUServer");
                    if (HKLM_WSUS.Contains("http://"))
                    {
                        Beaprint.BadPrint("    WSUS is using http: " + HKLM_WSUS);
                        Beaprint.InfoPrint("You can test https://github.com/pimps/wsuxploit to escalate privileges");
                        if (using_HKLM_WSUS == "1")
                            Beaprint.BadPrint("    And UseWUServer is equals to 1, so it is vulnerable!");
                        else if (using_HKLM_WSUS == "0")
                            Beaprint.GoodPrint("    But UseWUServer is equals to 0, so it is not vulnerable!");
                        else
                            System.Console.WriteLine("    But UseWUServer is equals to " + using_HKLM_WSUS + ", so it may work or not");
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(HKLM_WSUS))
                            Beaprint.NotFoundPrint();
                        else
                            Beaprint.GoodPrint("    WSUS value: " + HKLM_WSUS);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            Beaprint.GreatPrint("Windows Credentials");
            PrintvaultCreds();
            PrintCredManag();
            PrintSavedRDPInfo();
            PrintRecentRunCommands();
            PrintDPAPIMasterKeys();
            PrintDpapiCredFiles();
            PrintRCManFiles();
            PrintKerberosTickets();
            //PrintKerberosTGTTickets(); #Not working
            PrintWifi();
            PrintAppCmd();
            PrintSCClient();
            PrintAlwaysInstallElevated();
            PrintWSUS();
        }



        /////////////////////////////////////////////////
        ////////////// BROWSERS INFORMATION /////////////
        /////////////////////////////////////////////////
        private static void PrintBrowserInfo()
        {
            void PrintDBsFirefox()
            {
                try
                {
                    Beaprint.MainPrint("Looking for Firefox DBs", "T1503");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                    List<string> firefoxDBs = KnownFileCredsInfo.GetFirefoxDbs();
                    if (firefoxDBs.Count > 0)
                    {
                        foreach (string firefoxDB in firefoxDBs) //No Beaprints because line needs red
                            Beaprint.BadPrint("    Firefox credentials file exists at " + firefoxDB);

                        Beaprint.InfoPrint("Run SharpWeb (https://github.com/djhohnstein/SharpWeb)");
                    }
                    else
                    {
                        Beaprint.NotFoundPrint();
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintHistFirefox()
            {
                try
                {
                    Beaprint.MainPrint("Looking for GET credentials in Firefox history", "T1503");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                    List<string> firefoxHist = KnownFileCredsInfo.GetFirefoxHistory();
                    if (firefoxHist.Count > 0)
                    {
                        Dictionary<string, string> colorsB = new Dictionary<string, string>()
                            {
                                { print_credStrings, Beaprint.ansi_color_bad },
                            };
                        foreach (string url in firefoxHist)
                        {
                            if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                Beaprint.AnsiPrint("    " + url, colorsB);
                        }
                    }
                    else
                    {
                        Beaprint.NotFoundPrint();
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintDBsChrome()
            {
                try
                {
                    Beaprint.MainPrint("Looking for Chrome DBs", "T1503");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                    Dictionary<string, string> chromeDBs = KnownFileCredsInfo.GetChromeDbs();
                    if (chromeDBs.ContainsKey("userChromeCookiesPath"))
                    {
                        Beaprint.BadPrint("    Chrome cookies database exists at " + chromeDBs["userChromeCookiesPath"]);
                        Beaprint.InfoPrint("Follow the provided link for further instructions.");
                    }

                    if (chromeDBs.ContainsKey("userChromeLoginDataPath"))
                    {
                        Beaprint.BadPrint("    Chrome saved login database exists at " + chromeDBs["userChromeCookiesPath"]);
                        Beaprint.InfoPrint("Follow the provided link for further instructions.");
                    }

                    if ((!chromeDBs.ContainsKey("userChromeLoginDataPath")) && (!chromeDBs.ContainsKey("userChromeCookiesPath")))
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintHistBookChrome()
            {
                try
                {
                    Beaprint.MainPrint("Looking for GET credentials in Chrome history", "T1503");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                    Dictionary<string, List<string>> chromeHistBook = KnownFileCredsInfo.GetChromeHistBook();
                    List<string> history = chromeHistBook["history"];
                    List<string> bookmarks = chromeHistBook["bookmarks"];

                    if (history.Count > 0)
                    {
                        Dictionary<string, string> colorsB = new Dictionary<string, string>()
                            {
                                { print_credStrings, Beaprint.ansi_color_bad },
                            };
                        foreach (string url in history)
                        {
                            if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                Beaprint.AnsiPrint("    " + url, colorsB);
                        }
                        System.Console.WriteLine();
                    }
                    else
                    {
                        Beaprint.NotFoundPrint();
                    }

                    Beaprint.MainPrint("Chrome bookmarks", "T1217");
                    Beaprint.ListPrint(bookmarks);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrinteCurrentIETabs()
            {
                try
                {
                    Beaprint.MainPrint("Current IE tabs", "T1503");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                    List<string> urls = KnownFileCredsInfo.GetCurrentIETabs();

                    Dictionary<string, string> colorsB = new Dictionary<string, string>()
                        {
                            { print_credStrings, Beaprint.ansi_color_bad },
                        };
                    Beaprint.ListPrint(urls, colorsB);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintHistFavIE()
            {
                try
                {
                    Beaprint.MainPrint("Looking for GET credentials in IE history", "T1503");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                    Dictionary<string, List<string>> chromeHistBook = KnownFileCredsInfo.GetIEHistFav();
                    List<string> history = chromeHistBook["history"];
                    List<string> favorites = chromeHistBook["favorites"];

                    if (history.Count > 0)
                    {
                        Dictionary<string, string> colorsB = new Dictionary<string, string>()
                            {
                                { print_credStrings, Beaprint.ansi_color_bad },
                            };
                        foreach (string url in history)
                        {
                            if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                Beaprint.AnsiPrint("    " + url, colorsB);
                        }
                        System.Console.WriteLine();
                    }

                    Beaprint.MainPrint("IE favorites", "T1217");
                    Beaprint.ListPrint(favorites);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }


            Beaprint.GreatPrint("Browsers Information");
            PrintDBsFirefox();
            PrintHistFirefox();
            PrintDBsChrome();
            PrintHistBookChrome();
            PrinteCurrentIETabs();
            PrintHistFavIE();
        }


        /////////////////////////////////////////////////
        /////////////// INTERESTING FILES ///////////////
        /////////////////////////////////////////////////
        private static void PrintInterestingFiles(bool is_fast)
        {
            void PrintPuttySess()
            {
                try
                {
                    Beaprint.MainPrint("Putty Sessions", "");
                    List<Dictionary<string, string>> putty_sess = KnownFileCredsInfo.GetPuttySessions();

                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                        {
                            { "ProxyPassword.*|PublicKeyFile.*|HostName.*|PortForwardings.*", Beaprint.ansi_color_bad },
                        };
                    Beaprint.DictPrint(putty_sess, colorF, true, true);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintPuttySSH()
            {
                try
                {
                    Beaprint.MainPrint("Putty SSH Host keys", "");
                    List<Dictionary<string, string>> putty_sess = KnownFileCredsInfo.ListPuttySSHHostKeys();
                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                        {
                            { ".*", Beaprint.ansi_color_bad },
                        };
                    Beaprint.DictPrint(putty_sess, colorF, false, true);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintSSHKeysReg()
            {
                try
                {
                    Beaprint.MainPrint("SSH keys in registry", "");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#ssh-keys-in-registry", "If you find anything here, follow the link to learn how to decrypt the SSH keys");

                    string[] ssh_reg = MyUtils.GetRegSubkeys("HKCU", @"OpenSSH\Agent\Keys");
                    if (ssh_reg.Length == 0)
                        Beaprint.NotFoundPrint();
                    else
                    {
                        foreach (string ssh_key_entry in ssh_reg)
                            Beaprint.BadPrint(ssh_key_entry);
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintCloudCreds()
            {
                try
                {
                    Beaprint.MainPrint("Cloud Credentials", "T1538&T1083&T1081");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                    List<Dictionary<string, string>> could_creds = KnownFileCredsInfo.ListCloudCreds();
                    if (could_creds.Count != 0)
                    {
                        foreach (Dictionary<string, string> cc in could_creds)
                        {
                            string formString = "    {0}[{1}]\n    Accessed:{2} -- Size:{3}";
                            System.Console.WriteLine(String.Format(formString, cc));
                            System.Console.WriteLine("");
                        }
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintPossCredsRegs()
            {
                try
                {
                    string[] pass_reg_hkcu = new string[] { @"Software\ORL\WinVNC3\Password", @"Software\TightVNC\Server", @"Software\SimonTatham\PuTTY\Sessions" };
                    string[] pass_reg_hklm = new string[] { @"SYSTEM\Microsoft\Windows NT\Currentversion\WinLogon", @"SYSTEM\CurrentControlSet\Services\SNMP" };

                    Beaprint.MainPrint("Looking for possible regs with creds", "T1012&T1214");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#inside-the-registry");

                    string winVNC4 = MyUtils.GetRegValue("HKLM", @"SOFTWARE\RealVNC\WinVNC4", "passwword");
                    if (!String.IsNullOrEmpty(winVNC4.Trim()))
                        Beaprint.BadPrint(winVNC4);

                    foreach (string reg_hkcu in pass_reg_hkcu)
                        Beaprint.DictPrint(MyUtils.GetRegValues("HKLM", reg_hkcu), false);
                    foreach (string reg_hklm in pass_reg_hklm)
                        Beaprint.DictPrint(MyUtils.GetRegValues("HKLM", reg_hklm), false);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintUserCredsFiles()
            {
                try
                {
                    string patterns = "*credential*;*password*";
                    string pattern_color = "[cC][rR][eE][dD][eE][nN][tT][iI][aA][lL]|[pP][aA][sS][sS][wW][oO][rR][dD]";
                    List<string> valid_extensions = new List<string>() { ".txt", ".conf", ".cnf", ".yml", ".yaml", ".doc", ".docx", ".xlsx", ".json", ".xml" };
                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { pattern_color, Beaprint.ansi_color_bad },
                    };

                    Beaprint.MainPrint("Looking for possible password files in users homes", "T1083&T1081");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                    string searchPath = String.Format("{0}\\", Environment.GetEnvironmentVariable("SystemDrive") + "\\Users");
                    List<string> files_paths = MyUtils.FindFiles(searchPath, patterns);
                    foreach (string file_path in files_paths)
                    {
                        if (!Path.GetFileName(file_path).Contains("."))
                        {
                            Beaprint.AnsiPrint("    " + file_path, colorF);
                        }
                        else
                        {
                            foreach (string ext in valid_extensions)
                            {
                                if (file_path.Contains(ext))
                                {
                                    Beaprint.AnsiPrint("    " + file_path, colorF);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintRecycleBin()
            {
                try
                {
                    string pattern_bin = patterns_file_creds + ";*password*;*credential*";
                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { patterns_file_creds_color + "|.*password.*|.*credential.*", Beaprint.ansi_color_bad },
                    };

                    Beaprint.MainPrint("Looking inside the Recycle Bin for creds files", "T1083&T1081&T1145");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                    List<Dictionary<string, string>> recy_files = InterestingFiles.GetRecycleBin();
                    foreach (Dictionary<string, string> rec_file in recy_files)
                    {
                        foreach (string pattern in pattern_bin.Split(';'))
                        {
                            if (Regex.Match(rec_file["Name"], pattern.Replace("*", ".*"), RegexOptions.IgnoreCase).Success)
                            {
                                Beaprint.DictPrint(rec_file, colorF, true);
                                System.Console.WriteLine();
                            }
                        }
                    }
                    if (recy_files.Count <= 0)
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintPossCredsFiles()
            {
                try
                {
                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { patterns_file_creds_color, Beaprint.ansi_color_bad },
                    };

                    Beaprint.MainPrint("Looking for possible known files that can contain creds", "T1083&T1081");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                    string searchPath = String.Format("{0}\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    MyUtils.FindFiles(searchPath, patterns_file_creds, colorF);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintUsersDocsKeys()
            {
                try
                {
                    Beaprint.MainPrint("Looking for documents --limit 100--", "T1083");
                    List<string> doc_files = InterestingFiles.ListUsersDocs();
                    Beaprint.ListPrint(doc_files.GetRange(0, doc_files.Count <= 100 ? doc_files.Count : 100));
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }

            void PrintRecentFiles()
            {
                try
                {
                    Beaprint.MainPrint("Recent files --limit 70--", "T1083&T1081");
                    List<Dictionary<string, string>> rec_files = KnownFileCredsInfo.GetRecentFiles();
                    if (rec_files.Count != 0)
                        Beaprint.DictPrint(rec_files.GetRange(0, rec_files.Count <= 70 ? rec_files.Count : 70), false);
                    
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("{0}", ex));
                }
            }


            Beaprint.GreatPrint("Interesting files and registry");
            PrintPuttySess();
            PrintPuttySSH();
            PrintSSHKeysReg();
            PrintCloudCreds();
            PrintPossCredsRegs();
            PrintUserCredsFiles();
            PrintRecycleBin();
            if (!is_fast)
            {
                PrintPossCredsFiles();
                PrintUsersDocsKeys();
            }
            PrintRecentFiles();
        }


        [STAThread]
        static void Main(string[] args)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += (sender, arg) => { if (arg.Name.StartsWith("Colorful.Console")) return Assembly.Load(Properties.Resources.String1); return null; };
       
            //Check parameters
            bool check_all = true;
            bool check_si = false;
            bool check_iu = false;
            bool check_ip = false;
            bool check_is = false;
            bool check_ia = false;
            bool check_in = false;
            bool check_wc = false;
            bool check_bi = false;
            bool check_if = false;
            foreach (string arg in args)
            {
                if (string.Equals(arg, "fast", StringComparison.CurrentCultureIgnoreCase))
                    is_fast = true;

                if (string.Equals(arg, "cmd", StringComparison.CurrentCultureIgnoreCase))
                    exec_cmd = true;

                if (string.Equals(arg, "notcolor", StringComparison.CurrentCultureIgnoreCase))
                    notcolor = true;

                if (string.Equals(arg, "quiet", StringComparison.CurrentCultureIgnoreCase))
                    banner = false;

                if (string.Equals(arg, "searchfast", StringComparison.CurrentCultureIgnoreCase))
                    search_fast = false;

                if (string.Equals(arg, "help", StringComparison.CurrentCultureIgnoreCase))
                {
                    Beaprint.PrintUsage();
                    return;
                }

                if (string.Equals(arg, "-h", StringComparison.CurrentCultureIgnoreCase))
                {
                    Beaprint.PrintUsage();
                    return;
                }

                if (string.Equals(arg, "--help", StringComparison.CurrentCultureIgnoreCase))
                {
                    Beaprint.PrintUsage();
                    return;
                }

                if (string.Equals(arg, "/h", StringComparison.CurrentCultureIgnoreCase))
                {
                    Beaprint.PrintUsage();
                    return;
                }

                else if (string.Equals(arg, "systeminfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_si = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "userinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_iu = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "procesinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_ip = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "servicesinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_is = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "applicationsinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_ia = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "networkinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_in = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "windowscreds", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_wc = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "browserinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_bi = true;
                    check_all = false;
                }

                else if (string.Equals(arg, "filesinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    check_if = true;
                    check_all = false;
                }
            }

            //Start execution
            if (notcolor)
                Beaprint.deleteColors();
            else
                CheckRegANSI();
            
            CreateDynamicLists();

            Beaprint.PrintInit();
            if (check_si || check_all)
                PrintSystemInfo();
            if (check_iu || check_all)
                PrintInfoUsers();
            if (check_ip || check_all)
                PrintInfoProcesses();
            if (check_is || check_all)
                PrintInfoServices();
            if (check_ia || check_all)
                PrintInfoApplications();
            if (check_in || check_all)
                PrintInfoNetwork();
            if (check_wc || check_all)
                PrintWindowsCreds();
            if (check_bi || check_all)
                PrintBrowserInfo();
            if (check_if || check_all)
                PrintInterestingFiles(is_fast);

            /*
             * Wifi (passwords?)
             * Keylogger?
             * Check if you can modify a task
             * Input prompt ==> Better in PS
             * List Drivers ==> but how do I know if a driver is malicious?
             */

            //System.Console.ReadLine(); //For debugging
        }
    }
}


