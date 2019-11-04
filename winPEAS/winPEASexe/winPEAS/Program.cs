using Colorful; // http://colorfulconsole.com/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace winPEAS
{
    class Program
    {
        public static bool banner = true;
        public static string version = "vBETA VERSION";
        static bool is_fast = false;
        static bool exec_cmd = false;
        public static bool using_ansi = false;

        // Static blacklists
        static string strTrue = "True";
        static string strFalse = "False";
        static string badgroups = "docker|Remote";
        static string badpasswd = "NotChange|NotExpi";
        static string badPrivileges = "Enabled|ENABLED|SeImpersonatePrivilege|SeAssignPrimaryPrivilege|SeTcbPrivilege|SeBackupPrivilege|SeRestorePrivilege|SeCreateTokenPrivilege|SeLoadDriverPrivilege|SeTakeOwnershipPrivilege|SeDebugPrivilege";
        static string goodSoft = "Windows Phone Kits|Windows Kits|Windows Defender|Windows Mail|Windows Media Player|Windows Multimedia Platform|windows nt|Windows Photo Viewer|Windows Portable Devices|Windows Security|Windows Sidebar|WindowsApps|WindowsPowerShell|Microsoft|WOW6432Node|internet explorer|Internet Explorer|Common Files";
        static string badShares = "[a-zA-Z]+[$]";
        static string badIps = "127.0.0.1";
        static string badUAC = "No prompting|PromptForNonWindowsBinaries";
        static string goodUAC = "PromptPermitDenyOnSecureDesktop";
        static string badLAPS = "LAPS not installed";
        static string print_credStrings = "[pP][aA][sS][sS][wW][a-zA-Z0-9_-]*|[pP][wW][dD][a-zA-Z0-9_-]*|[uU][sS][eE][rR][a-zA-Z0-9_-]*|[nN][aA][mM][eE]|[lL][oO][gG][iI][nN]|[lL][oO][gG][iI][nN]|[cC][oO][nN][tT][rR][aA][sS][eE][a-zA-Z0-9_-]*|[cC][rR][eE][dD][eE][nN][tT][iI][aA][lL][a-zA-Z0-9_-]*|[aA][pP][iI]|[tT][oO][kK][eE][nN]";
        static List<string> credStringsRegex = new List<string> { "PASSW[a-zA-Z0-9_-]*=", "PWD[a-zA-Z0-9_-]*=", "USER[a-zA-Z0-9_-]*=", "NAME=", "&LOGIN", "=LOGIN", "CONTRASEÑA[a-zA-Z0-9_-]*=", "CREDENTIAL[a-zA-Z0-9_-]*=", "API_KEY", "TOKEN" };
        static string patterns_file_creds = @"RDCMan.settings;*.rdg;*_history;.sudo_as_admin_successful;.profile;*bashrc;httpd.conf;*.plan;.htpasswd;.git-credentials;*.rhosts;hosts.equiv;Dockerfile;docker-compose.yml;credentials;credentials.db;access_tokens.db;accessTokens.json;legacy_credentials;azureProfile.json;appcmd.exe;scclient.exe;unattend.txt;*.gpg$;*.pgp$;*config*.php;elasticsearch.y*ml;kibana.y*ml;*.p12$;*.der$;*.csr$;*.cer$;known_hosts;id_rsa;id_dsa;*.ovpn;anaconda-ks.cfg;hostapd.conf;rsyncd.conf;cesi.conf;supervisord.conf;tomcat-users.xml;web.config;*.kdbx;KeePass.config;Ntds.dit;SAM;SYSTEM;FreeSSHDservice.ini;sysprep.inf;sysprep.xml;unattend.xml;unattended.xml;*vnc*.ini;*vnc*.c*nf*;*vnc*.txt;*vnc*.xml;groups.xml;services.xml;scheduledtasks.xml;printers.xml;drives.xml;datasources.xml;php.ini;https.conf;https-xampp.conf;httpd.conf;my.ini;my.cnf;access.log;error.log;server.xml;SiteList.xml;ConsoleHost_history.txt;setupinfo;setupinfo.bak";
        static string patterns_file_creds_color = "RDCMan.settings|.rdg|_history|.sudo_as_admin_successful|.profile|bashrc|httpd.conf|.plan|.htpasswd|.git-credentials|.rhosts|hosts.equiv|Dockerfile|docker-compose.yml|credentials|credentials.db|access_tokens.db|accessTokens.json|legacy_credentials|azureProfile.json|appcmd.exe|scclient.exe|unattend.txt|access.log|error.log|credential|password|.gpg|.pgp|config.php|elasticsearch|kibana.|.p12|.der|.csr|.crt|.cer|.pem|known_hosts|id_rsa|id_dsa|.ovpn|anaconda-ks.cfg|hostapd.conf|rsyncd.conf|cesi.conf|supervisord.conf|tomcat-users.xml|web.config|.kdbx|.key|KeePass.config|ntds.dir|Ntds.dit|sam|system|SAM|SYSTEM|FreeSSHDservice.ini|sysprep.inf|sysprep.xml|unattend.xml|unattended.xml|vnc|groups.xml|services.xml|scheduledtasks.xml|printers.xml|drives.xml|datasources.xml|php.ini|https.conf|https-xampp.conf|httpd.conf|my.ini|my.cnf|access.log|error.log|server.xml|SiteList.xml|setupinfo";

        static Color color_default = Beaprint.color_default;
        static Color color_key = Beaprint.color_key;
        static Color color_good = Beaprint.color_good;
        static Color color_bad = Beaprint.color_bad;
        static string ansi_color_bad = Beaprint.ansi_color_bad;
        static string ansi_color_good = Beaprint.ansi_color_good;
        static string ansi_users_active = Beaprint.ansi_users_active;
        static string ansi_users_disabled = Beaprint.ansi_users_disabled;
        static string ansi_current_user = Beaprint.ansi_current_user;
        static StyleSheet onlyDefaultStyleSheet = new StyleSheet(color_default);
        static StyleSheet onlyKeyStyleSheet = new StyleSheet(color_key);

        // Create Dynamic blacklists
        static string currentUserName = Environment.UserName;
        static string currentDomainName = Environment.UserDomainName;
        static List<string> currentUserGroups = UserInfo.GetUserGroups(currentUserName);
        public static List<string> interestingUsersGroups = new List<string> { "Everyone", "Users", "Todos" , currentUserName }; //Authenticated Users (Authenticated left behin to avoid repetitions)
        static string paint_interestingUserGroups = String.Join("|", currentUserGroups);
        static string paint_activeUsers = String.Join("|", UserInfo.GetMachineUsers(true, false, false, false, false));
        static string paint_disabledUsers = String.Join("|", UserInfo.GetMachineUsers(false, true, false, false, false));
        static string paint_lockoutUsers = String.Join("|", UserInfo.GetMachineUsers(false, false, true, false, false));
        static string paint_adminUsers = String.Join("|", UserInfo.GetMachineUsers(false, false, false, true, false));




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
                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { strTrue, ansi_color_bad }
                        };
                        Beaprint.DictPrint(basicDictSystem, colorsSI, false);
                    }
                    else
                    {
                        StyleSheet styleSheetSI = new StyleSheet(color_default);
                        styleSheetSI.AddStyle(strTrue, color_bad);
                        Beaprint.DictPrint(basicDictSystem, styleSheetSI, false);
                    }
                    System.Console.WriteLine();
                    Watson.FindVulns();
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintLAPSInfo()
            {
                try
                {
                    Beaprint.MainPrint("LAPS Settings", "T1012");
                    Beaprint.LinkPrint("", "If installed, local administrator password change frequently in domain-joined boxes and is restricted by ACL");
                    Dictionary<string, string> lapsDict = SystemInfo.GetLapsSettings();
                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { badLAPS, ansi_color_bad }
                        };
                        Beaprint.DictPrint(lapsDict, colorsSI, false);
                    }
                    else
                    {
                        StyleSheet styleSheetLAPS = new StyleSheet(color_default);
                        styleSheetLAPS.AddStyle(badLAPS, color_bad);
                        Beaprint.DictPrint(lapsDict, styleSheetLAPS, false);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintUserEV()
            {
                try
                {
                    Beaprint.MainPrint("User Environment Variables", "");
                    Beaprint.LinkPrint("", "Check for some passwords or keys in the env variables");
                    Dictionary<string, string> userEnvDict = SystemInfo.GetUserEnvVariables();
                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { print_credStrings, ansi_color_bad }
                        };
                        Beaprint.DictPrint(userEnvDict, colorsSI, false);
                    }
                    else
                    {
                        StyleSheet styleSheetUEV = new StyleSheet(color_default);
                        styleSheetUEV.AddStyle(print_credStrings, color_bad);
                        Beaprint.DictPrint(userEnvDict, styleSheetUEV, false);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintSystemEV()
            {
                try
                {
                    Beaprint.MainPrint("System Environment Variables", "");
                    Beaprint.LinkPrint("", "Check for some passwords or keys in the env variables");
                    Dictionary<string, string> sysEnvDict = SystemInfo.GetSystemEnvVariables();
                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { print_credStrings, ansi_color_bad }
                        };
                        Beaprint.DictPrint(sysEnvDict, colorsSI, false);
                    }
                    else
                    {
                        StyleSheet styleSheetSEV = new StyleSheet(color_default);
                        styleSheetSEV.AddStyle(print_credStrings, color_bad);
                        Beaprint.DictPrint(sysEnvDict, styleSheetSEV, false);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintInetInfo()
            {
                try
                {
                    Beaprint.MainPrint("HKCU Internet Settings", "T1012");
                    Dictionary<string, string> HKCUDict = SystemInfo.GetInternetSettings("HKCU");
                    Beaprint.DictPrint(HKCUDict, true);

                    Beaprint.MainPrint("HKLM Internet Settings", "T1012");
                    Dictionary<string, string> HKMLDict = SystemInfo.GetInternetSettings("HKLM");
                    Beaprint.DictPrint(HKMLDict, true);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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

                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                            {
                                { "Permissions.*", ansi_color_bad}
                            };
                            Beaprint.AnsiPrint(String.Format(dToPrint, drive["Name"], drive["Type"], drive["Volume label"], drive["Filesystem"], (((Int64.Parse(drive["Available space"]) / 1024) / 1024) / 1024).ToString(), drive_permissions), colorsSI);
                        }

                        else
                        {
                            Formatter[] colorsString = new Formatter[]
                            {
                            new Formatter(drive["Name"], drive_permissions.Length > 0 ? color_bad : color_default),
                            new Formatter(drive["Type"], color_default),
                            new Formatter(drive["Volume label"], color_default),
                            new Formatter(drive["Filesystem"],color_default),
                            new Formatter((((Int64.Parse(drive["Available space"]) / 1024) / 1024) / 1024).ToString(), color_default),
                            new Formatter(drive_permissions, color_bad)
                            };
                            Colorful.Console.WriteLineFormatted(dToPrint, color_key, colorsString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintUACInfo()
            {
                try {
                    Beaprint.MainPrint("UAC Status", "T1012");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#basic-uac-bypass-full-file-system-access", "If you are in the Administrators group check how to bypass the UAC");
                    Dictionary<string, string> uacDict = SystemInfo.GetUACSystemPolicies();

                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsSI = new Dictionary<string, string>()
                        {
                            { badUAC, ansi_color_bad },
                            { goodUAC, ansi_color_good }
                        };
                        Beaprint.DictPrint(uacDict, colorsSI, false);
                    }
                    else
                    {
                        StyleSheet styleSheetUAC = new StyleSheet(color_default);
                        //styleSheet.AddStyle("True", color_bad);
                        styleSheetUAC.AddStyle(badUAC, color_bad);
                        styleSheetUAC.AddStyle(goodUAC, color_good);
                        Beaprint.DictPrint(uacDict, styleSheetUAC, false);
                    }

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
                    Colorful.Console.WriteLine(ex);
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
            
            StyleSheet CreateUsersSS()
            {
                StyleSheet styleSheetUsers = new StyleSheet(color_default);
                try
                {
                    styleSheetUsers.AddStyle(paint_activeUsers, Color.Cyan);
                    if (paint_disabledUsers.Length > 1) styleSheetUsers.AddStyle(paint_disabledUsers + "|Disabled", Color.MediumPurple);
                    if (paint_lockoutUsers.Length > 1) styleSheetUsers.AddStyle(paint_lockoutUsers + "|Lockout", Color.Blue);
                    styleSheetUsers.AddStyle(currentUserName, Color.Magenta);
                    styleSheetUsers.AddStyle(currentDomainName, Color.Magenta);
                    styleSheetUsers.AddStyle(paint_adminUsers, color_bad);
                    styleSheetUsers.AddStyle(badgroups, color_bad);
                    styleSheetUsers.AddStyle(badpasswd, color_bad);
                    styleSheetUsers.AddStyle(badPrivileges, color_bad);
                    styleSheetUsers.AddStyle("DefaultPassword.*", color_bad);
                    styleSheetUsers.AddStyle(@"\|->Groups:|\|->Password:|Current user:", color_key);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
                return styleSheetUsers;
            }

            Dictionary<string, string> colorsU()
            {
                return new Dictionary<string, string>()
                {
                    { paint_activeUsers, ansi_users_active },
                    { paint_disabledUsers, ansi_users_disabled },
                    { currentUserName, ansi_current_user },
                    { currentDomainName, ansi_current_user },
                    { paint_adminUsers, ansi_color_bad },
                    { badgroups, ansi_color_bad },
                    { badpasswd, ansi_color_bad },
                    { badPrivileges, ansi_color_bad },
                    { "DefaultPassword.*", ansi_color_bad },
                };
            }

            void PrintCU()
            {
                try
                {
                    Beaprint.MainPrint("Current users", "T1087&T1069&T1033");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#users-and-groups", "Check if you have some admin equivalent privileges");
                    List<string> users_grps = UserInfo.GetMachineUsers(false, false, false, false, true);
                    if (using_ansi)
                    {
                        Beaprint.AnsiPrint("  Current user: " + currentUserName, colorsU());
                        Beaprint.ListPrint(users_grps, colorsU());
                    }
                    else
                    {
                        Colorful.Console.WriteLineStyled("  Current user: " + currentUserName, CreateUsersSS());
                        Beaprint.ListPrint(users_grps, CreateUsersSS());
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintTokenP()
            {
                try
                {
                    Beaprint.MainPrint("Current Token privileges", "T1134");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#token-manipulation", "Check if you can escalate privilege using some enabled token");
                    Dictionary<string, string> token_privs = UserInfo.GetTokenGroupPrivs();
                    if (using_ansi)
                        Beaprint.DictPrint(token_privs, colorsU(), false);
                    else
                        Beaprint.DictPrint(token_privs, CreateUsersSS(), false);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintClipboardText()
            {
                try
                {
                    Beaprint.MainPrint("Clipboard text", "T1134");
                    string clipb = UserInfo.GetClipboardText();
                    if (String.IsNullOrEmpty(clipb))
                        Colorful.Console.WriteLine(clipb, color_bad);
                    else
                    {
                        if (exec_cmd)
                            Beaprint.BadPrint("    " + MyUtils.ExecCMD("powershell -command Get-Clipboard"));
                        else {
                            Beaprint.NotFoundPrint();
                            Beaprint.InfoPrint("    This C# implementation to capture the clipboard is not trustable in every Windows version");
                            Beaprint.InfoPrint("    If you want to see what is inside the clipboard execute 'powershell -command \"Get - Clipboard\"'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintLoggedUsers()
            {
                try
                {
                    Beaprint.MainPrint("Logged users", "T1087&T1033");
                    List<string> loggedusers = UserInfo.GetLoggedUsers();
                    if (using_ansi)
                        Beaprint.ListPrint(loggedusers, colorsU());
                    else
                        Beaprint.ListPrint(loggedusers, CreateUsersSS());
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                        if (using_ansi) {
                            System.Console.WriteLine(header);
                            foreach (Dictionary<string, string> rdp_ses in rdp_sessions)
                                Beaprint.AnsiPrint(String.Format(format, rdp_ses["SessionID"], rdp_ses["pSessionName"], rdp_ses["pUserName"], rdp_ses["pDomainName"], rdp_ses["State"], rdp_ses["SourceIP"]), colorsU());
                        }
                        else {
                            Colorful.Console.WriteLineStyled(header, onlyKeyStyleSheet);
                            foreach (Dictionary<string, string> rdp_ses in rdp_sessions)
                                Colorful.Console.WriteLineStyled(String.Format(format, rdp_ses["SessionID"], rdp_ses["pSessionName"], rdp_ses["pUserName"], rdp_ses["pDomainName"], rdp_ses["State"], rdp_ses["SourceIP"]), CreateUsersSS());
                        }
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintEverLoggedUsers()
            {
                try
                {
                    Beaprint.MainPrint("Ever logged users", "T1087&T1033");
                    List<string> everlogged = UserInfo.GetEverLoggedUsers();
                    if (using_ansi)
                        Beaprint.ListPrint(everlogged, colorsU());
                    else
                        Beaprint.ListPrint(everlogged, CreateUsersSS());
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintAutoLogin()
            {
                try
                {
                    Beaprint.MainPrint("Looking for AutoLogon credentials", "T1012");
                    bool ban = false;
                    Dictionary<string, string> autologon = UserInfo.GetAutoLogon();
                    if (autologon.Count > 0) {
                        foreach (KeyValuePair<string, string> entry in autologon)
                        {
                            if (entry.Value != null && entry.Value != "")
                            {
                                if (!ban)
                                {
                                    Beaprint.BadPrint("Some AutoLogon credentials were found!!");
                                    ban = true;
                                }
                                if (using_ansi)
                                    Beaprint.AnsiPrint(String.Format("    {0,-30}:  {1}", entry.Key, entry.Value), colorsU());
                                else
                                    Colorful.Console.WriteLineStyled(String.Format("    {0,-30}:  {1}", entry.Key, entry.Value), CreateUsersSS());
                            }
                            else
                                Beaprint.NotFoundPrint();
                        }
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintHomeFolders()
            {
                try
                {
                    Beaprint.MainPrint("Home folders found", "T1087&T1083&T1033");
                    List<string> user_folders = UserInfo.GetUsersFolders();
                    foreach(string ufold in user_folders)
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintPasswordPolicies()
            {
                try
                {
                    Beaprint.MainPrint("Password Policies", "T1201");
                    Beaprint.LinkPrint("", "This is insteresting for brute-force");
                    List<Dictionary<string, string>> PPy = UserInfo.GetPasswordPolicy();
                    if (using_ansi)
                        Beaprint.DictPrint(PPy, colorsU(), false);
                    else
                        Beaprint.DictPrint(PPy, CreateUsersSS(), false);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    var color_product = color_default;
                    foreach (Dictionary<string, string> proc_info in processes_info)
                    {
                        if (ProcessesInfo.defensiveProcesses.ContainsKey(proc_info["Name"]))
                        {
                            proc_info["Product"] = ProcessesInfo.defensiveProcesses[proc_info["Name"]].ToString();
                            color_product = color_good;
                        }
                        else if (ProcessesInfo.interestingProcesses.ContainsKey(proc_info["Name"]))
                        {
                            proc_info["Product"] = ProcessesInfo.interestingProcesses[proc_info["Name"]].ToString();
                            color_product = color_bad;
                        }
                        else if (ProcessesInfo.browserProcesses.ContainsKey(proc_info["Name"]))
                        {
                            color_product = Color.MediumPurple;
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
                            formString += "\n    Possible DLL Hijacking folder: {7}";
                        if (proc_info["CommandLine"].Length > 1)
                            formString += "\n    {8}";

                        if (using_ansi) 
                        {
                            Dictionary<string, string> colorsP = new Dictionary<string, string>()
                            {
                                { currentUserName, ansi_current_user },
                                { "Permissions:.*", ansi_color_bad },
                                { "Possible DLL Hijacking.*", ansi_color_bad },
                                { proc_info["ExecutablePath"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?"), (file_rights.Count > 0 || dir_rights.Count > 0) ? ansi_color_bad : ansi_color_good },
                            };
                            Beaprint.AnsiPrint(String.Format(formString, proc_info["Name"], proc_info["ProcessID"], proc_info["ExecutablePath"], proc_info["Product"], proc_info["Owner"], proc_info["isDotNet"], String.Join(", ", file_rights), dir_rights.Count > 0 ? Path.GetDirectoryName(proc_info["ExecutablePath"]) : "", String.Join(", ", dir_rights), proc_info["CommandLine"]), colorsP);
                        }
                        else
                        {
                            Formatter[] colorsString = new Formatter[]
                                {
                                new Formatter(proc_info["Name"], Color.DarkOrange),
                                new Formatter(proc_info["ProcessID"], Color.MediumPurple),
                                new Formatter(proc_info["ExecutablePath"], (file_rights.Count > 0 || dir_rights.Count > 0) ? color_bad : color_good),
                                new Formatter(proc_info["Product"], color_product),
                                new Formatter(proc_info["Owner"], (proc_info["Owner"].ToLower() == currentUserName.ToLower()) ? Color.Magenta : color_bad),
                                new Formatter(proc_info["isDotNet"], color_default),
                                new Formatter(String.Join(", ", file_rights), color_bad),
                                new Formatter(String.Join(", ", dir_rights), color_bad),
                                new Formatter(proc_info["CommandLine"], Color.Gray),
                                };
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                        }
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                            formString += "\n    Possible DLL Hijacking in binary folder: {9}";
                        if (service_info["Description"].Length > 1)
                            formString += "\n    {10}";

                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsS = new Dictionary<string, string>()
                            {
                                { "File Permissions:.*", ansi_color_bad },
                                { "Possible DLL Hijacking.*", ansi_color_bad },
                                { "No quotes and Space detected", ansi_color_bad },
                                { service_info["PathName"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?"), (file_rights.Count > 0 || dir_rights.Count > 0 || no_quotes_and_space) ? ansi_color_bad : ansi_color_good },
                                { service_info["StartMode"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?"), (service_info["StartMode"].ToLower() == "auto") ? ansi_color_bad : ansi_color_good },
                            };
                            Beaprint.AnsiPrint(String.Format(formString, service_info["Name"], service_info["CompanyName"], service_info["DisplayName"], service_info["PathName"], service_info["StartMode"], service_info["State"], service_info["isDotNet"], "No quotes and Space detected", String.Join(", ", file_rights), dir_rights.Count > 0 ? Path.GetDirectoryName(service_info["FilteredPath"]) : "", String.Join(", ", dir_rights), service_info["Description"]), colorsS);
                        }
                        else
                        {
                            Formatter[] colorsString = new Formatter[]
                            {
                                new Formatter(service_info["Name"], color_default),
                                new Formatter(service_info["CompanyName"], color_default),
                                new Formatter(service_info["DisplayName"], color_default),
                                new Formatter(service_info["PathName"], (file_rights.Count > 0 || dir_rights.Count > 0 || no_quotes_and_space) ? color_bad : color_good),
                                new Formatter(service_info["StartMode"], (service_info["StartMode"].ToLower() == "auto") ? color_bad : color_good),
                                new Formatter(service_info["State"], (service_info["State"].ToLower() == "running") ? color_good : color_default),
                                new Formatter(service_info["isDotNet"], color_default),
                                new Formatter("No quotes and Space detected", color_bad),
                                new Formatter(String.Join(", ", file_rights), color_bad),
                                new Formatter(String.Join(", ", dir_rights), color_bad),
                                new Formatter(service_info["Description"], Color.Gray),
                            };
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                        }
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintInstalledApps()
            {
                try
                {
                    Beaprint.MainPrint("Installed Applications --Via Program Files--", "T1083&T1012&T1010&T1518");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#software", "Check if you can modify installed software");
                    Dictionary<string, Dictionary<string, string>> InstalledAppsPerms = ApplicationInfo.GetInstalledAppsPerms();
                    string format = "    ==>  {0}({1})";
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
                    Colorful.Console.WriteLine();

                    Beaprint.MainPrint("Installed Applications --Via Registry--", "T1083&T1012&T1010");
                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsA = new Dictionary<string, string>()
                        {
                            { goodSoft, ansi_color_good }
                        };
                        Beaprint.ListPrint(ApplicationInfo.GetAppsRegistry(), colorsA);
                    }
                    else
                    {
                        StyleSheet styleSheetIA = new StyleSheet(color_default);
                        styleSheetIA.AddStyle(goodSoft, color_good);
                        Beaprint.ListPrint(ApplicationInfo.GetAppsRegistry(), styleSheetIA);
                    }
                }
                catch
                {
                    //Colorful.Console.WriteLine(ex);
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
                            { "FolderPerms:.*", ansi_color_bad },
                            { "FilePerms:.*", ansi_color_bad },
                            { "(Unquoted and Space detected)", ansi_color_bad },
                            { "(RegPath is writable)", ansi_color_bad },
                            { (app["Folder"].Length > 0) ? app["Folder"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?") : "ouigyevb2uivydi2u3id2ddf3", !String.IsNullOrEmpty(app["interestingFolderRights"]) ? ansi_color_bad : ansi_color_good },
                            { (app["File"].Length > 0) ? app["File"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?") : "adu8v298hfubibuidiy2422r", !String.IsNullOrEmpty(app["interestingFileRights"]) ? ansi_color_bad : ansi_color_good },
                            { (app["Reg"].Length > 0) ? app["Reg"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?") : "o8a7eduia37ibduaunbf7a4g7ukdhk4ua", (app["isWritableReg"].ToLower() == "true") ? ansi_color_bad : ansi_color_good },
                        };
                        StyleSheet styleSheetAA = new StyleSheet(color_default);
                        styleSheetAA.AddStyle("FolderPerms", color_bad);
                        string string1 = "", string2 = "";

                        if (!String.IsNullOrEmpty(app["Folder"]))
                            string1 += "    Folder: " + app["Folder"];
                        if (!String.IsNullOrEmpty(app["interestingFolderRights"]))
                        {
                            string2 += "    FolderPerms: " + app["interestingFolderRights"];
                            styleSheetAA.AddStyle(app["Folder"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"), color_bad);
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(app["Folder"]))
                                styleSheetAA.AddStyle(app["Folder"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"), color_good);
                        }
                        if (using_ansi)
                        {
                            Beaprint.AnsiPrint(string1, colorsA);
                            Beaprint.AnsiPrint(string2, colorsA);
                        }
                        else
                        {
                            Colorful.Console.WriteLineStyled(string1, styleSheetAA);
                            styleSheetAA.AddStyle(paint_interestingUserGroups, Color.Magenta);
                            Colorful.Console.WriteLineStyled(string2, styleSheetAA);
                        }

                        // Because of mixin color rules between folder and file path 2 stylesheets are neede
                        styleSheetAA = new StyleSheet(color_default);
                        styleSheetAA.AddStyle("FilePerms", color_bad);
                        string1 = ""; string2 = "";

                        string filepath_mod = app["File"].Replace("\"", "").Replace("'", "");
                        if (!String.IsNullOrEmpty(app["File"]))
                            string1 += "    File: " + filepath_mod;

                        if (app["isUnquotedSpaced"].ToLower() == "true")
                        {
                            string1 += " (Unquoted and Space detected)";
                            styleSheetAA.AddStyle("Unquoted and Space detected", color_bad);
                        }

                        if (!String.IsNullOrEmpty(app["interestingFileRights"]))
                        {
                            string2 += "    FilePerms: " + app["interestingFileRights"];
                            styleSheetAA.AddStyle(filepath_mod.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"), color_bad);
                        }
                        else if (app["isUnquotedSpaced"].ToLower() == "true")
                        {
                            styleSheetAA.AddStyle(filepath_mod.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"), color_bad);
                        }
                        else
                        {
                            if (app["File"] != null && app["File"] != "")
                                styleSheetAA.AddStyle(filepath_mod.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"), color_good);
                        }

                        if (!String.IsNullOrEmpty(app["Reg"]))
                            if (string2 != "")
                                string2 += "\n    RegPath: " + app["Reg"];
                            else
                                string2 += "    RegPath: " + app["Reg"];
                        if (app["isWritableReg"].ToLower() == "true")
                        {
                            string2 += " (RegPath is writable)";
                            styleSheetAA.AddStyle("RegPath is writable", color_bad);
                            styleSheetAA.AddStyle(app["Reg"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"), color_bad);
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(app["Reg"]))
                                styleSheetAA.AddStyle(app["Reg"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"), color_good);
                        }

                        if (using_ansi)
                        {
                            Beaprint.AnsiPrint(string1, colorsA);
                            Beaprint.AnsiPrint(string2, colorsA);
                        }
                        else
                        {
                            Colorful.Console.WriteLineStyled(string1, styleSheetAA);
                            styleSheetAA.AddStyle(paint_interestingUserGroups, Color.Magenta);
                            Colorful.Console.WriteLineStyled(string2, styleSheetAA);
                        }
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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

                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsS = new Dictionary<string, string>()
                            {
                                { "Permissions.*", ansi_color_bad },
                                { sapp["Action"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?"), (file_rights.Count > 0 || dir_rights.Count > 0) ? ansi_color_bad : ansi_color_good },
                            };
                            Beaprint.AnsiPrint(String.Format(formString, sapp["Author"], sapp["Name"], sapp["Action"], String.Join(", ", file_rights), String.Join(", ", dir_rights), sapp["Trigger"], sapp["Description"]), colorsS);
                        }
                        else
                        {
                            Formatter[] colorsString = new Formatter[]
                            {
                                new Formatter(sapp["Author"], Color.DarkOrange),
                                new Formatter(sapp["Name"], color_default),
                                new Formatter(sapp["Action"],  (file_rights.Count > 0 || dir_rights.Count > 0) ? color_bad : color_good),
                                new Formatter(String.Join(", ", file_rights),  color_bad),
                                new Formatter(String.Join(", ", dir_rights),  color_bad),
                                new Formatter(sapp["Trigger"],  Color.MediumPurple),
                                new Formatter(sapp["Description"],  Color.Gray),
                            };
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                        }
                        Beaprint.PrintLineSeparator();
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsN = new Dictionary<string, string>()
                        {
                            { badShares, ansi_color_bad },
                        };
                        Beaprint.ListPrint(NetworkInfo.GetNetworkShares("127.0.0.1"), colorsN);
                    }
                    else
                    {
                        StyleSheet styleSheetNS = new StyleSheet(color_default);
                        styleSheetNS.AddStyle(badShares, color_bad);
                        Beaprint.ListPrint(NetworkInfo.GetNetworkShares("127.0.0.1"), styleSheetNS);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                            Colorful.Console.WriteLine(line, color_default);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintNetworkIfaces()
            {
                try
                {
                    Beaprint.MainPrint("Network Ifaces and known hosts", "T1016");
                    foreach (Dictionary<string, string> card in NetworkInfo.GetNetCardInfo())
                    {
                        string formString = "    {0}[{1}]: {2} / {3}";
                        if (card["Gateways"].Length > 1)
                            formString += "\n\tGateways: {4}";
                        if (card["DNSs"].Length > 1)
                            formString += "\n\tDNSs: {5}";
                        if (card["arp"].Length > 1)
                            formString += "\n\tKnown hosts:\n{6}";

                        if (using_ansi)
                        {
                            System.Console.WriteLine(String.Format(formString, card["Name"], card["PysicalAddr"], card["IPs"], card["Netmasks"].Replace(", 0.0.0.0", ""), card["Gateways"], card["DNSs"], card["arp"]));
                        }
                        else
                        {
                            Formatter[] colorsString = new Formatter[]
                                {
                            new Formatter(card["Name"], Color.DarkOrange),
                            new Formatter(card["PysicalAddr"], color_default),
                            new Formatter(card["IPs"], Color.OrangeRed),
                            new Formatter(card["Netmasks"].Replace(", 0.0.0.0", ""), Color.Gray),
                            new Formatter(card["Gateways"], Color.MediumPurple),
                            new Formatter(card["DNSs"], Color.MediumPurple),
                            new Formatter(card["arp"], Color.Gray),
                                };
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintListeningPorts()
            {
                try
                {
                    Beaprint.MainPrint("Current Listening Ports", "T1049&T1049");
                    Beaprint.LinkPrint("", "Check for services restricted from the outside");
                    List<List<string>> conns = NetworkInfo.GetNetConnections();

                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsN = new Dictionary<string, string>()
                        {
                            { badIps, ansi_color_bad },
                        };
                        foreach (List<string> conn in conns)
                            Beaprint.AnsiPrint(String.Format("    {0,-10}{1,-23}{2,-23}{3}", conn[0], conn[1], conn[2], conn[3]), colorsN);
                    }
                    else
                    {
                        StyleSheet styleSheetLP = new StyleSheet(color_default);
                        styleSheetLP.AddStyle(badIps, color_bad);
                        styleSheetLP.AddStyle("Proto|Local Address|Foreing Address|State", color_key);
                        foreach (List<string> conn in conns)
                            Colorful.Console.WriteLineStyled(String.Format("    {0,-10}{1,-23}{2,-23}{3}", conn[0], conn[1], conn[2], conn[3]), styleSheetLP);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintFirewallRules()
            {
                try
                {
                    Beaprint.MainPrint("Firewall Rules", "T1016");
                    StyleSheet styleSheetFW = new StyleSheet(color_default);
                    styleSheetFW.AddStyle(strFalse, color_bad);
                    styleSheetFW.AddStyle(strTrue, color_good);
                    styleSheetFW.AddStyle(@"Current Profiles:|FirewallEnabled \(Domain\):|FirewallEnabled \(Private\):|FirewallEnabled \(Public\):", color_key);
                    Colorful.Console.WriteLineStyled("    Current Profiles: " + NetworkInfo.GetFirewallProfiles(), styleSheetFW);
                    foreach (KeyValuePair<string, string> entry in NetworkInfo.GetFirewallBooleans())
                        Colorful.Console.WriteLineStyled(String.Format("    {0,-23}:\t{1}", entry.Key, entry.Value), styleSheetFW);


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

                        if (using_ansi) {
                            Dictionary<string, string> colorsN = new Dictionary<string, string>()
                            {
                                { strFalse, ansi_color_bad },
                                { strTrue, ansi_color_good },
                                { "File Permissions.*|Folder Permissions.*", ansi_color_bad },
                                { rule["AppName"].Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?"), (file_perms.Length > 0 || folder_perms.Length > 0) ? ansi_color_bad : ansi_color_good },
                            };
                            Beaprint.AnsiPrint(String.Format(formString, rule["Profiles"], rule["Name"],  rule["AppName"], rule["Action"], rule["Protocol"], rule["Direction"], rule["Direction"] == "IN" ? rule["Local"] : rule["Remote"], rule["Direction"] == "IN" ? rule["Remote"] : rule["Local"], file_perms, folder_perms, rule["Description"]), colorsN);
                        }
                        else
                        {
                            Formatter[] colorsString = new Formatter[]
                            {
                                new Formatter(rule["Profiles"], Color.Gray),
                                new Formatter(rule["Name"], Color.DarkOrange),
                                new Formatter(rule["AppName"], (file_perms.Length > 0 || folder_perms.Length > 0) ? color_bad : color_good),
                                new Formatter(rule["Action"], Color.OrangeRed),
                                new Formatter(rule["Protocol"], Color.MediumPurple),
                                new Formatter(rule["Direction"], color_default),
                                new Formatter(rule["Direction"] == "IN" ? rule["Local"] : rule["Remote"], rule["Direction"] == "IN" ? color_default : Color.White),
                                new Formatter(rule["Direction"] == "IN" ? rule["Remote"] : rule["Local"], rule["Direction"] == "IN" ? Color.White : color_default),
                                new Formatter(file_perms, color_bad),
                                new Formatter(folder_perms, color_bad),
                                new Formatter(rule["Description"], Color.Gray),
                            };
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintDNSCache()
            {
                try
                {
                    Beaprint.MainPrint("DNS cached --limit 70--", "T1016");
                    Colorful.Console.WriteLineStyled(String.Format("    {0,-38}{1,-38}{2}", "Entry", "Name", "Data"), onlyKeyStyleSheet);
                    List<Dictionary<string, string>> DNScache = NetworkInfo.GetDNSCache();
                    foreach (Dictionary<string, string> entry in DNScache.GetRange(0, DNScache.Count <= 70 ? DNScache.Count : 70))
                        Colorful.Console.WriteLineStyled(String.Format("    {0,-38}{1,-38}{2}", entry["Entry"], entry["Name"], entry["Data"]), onlyDefaultStyleSheet);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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

                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsC = new Dictionary<string, string>()
                        {
                            { "Identity.*|Credential.*", ansi_color_bad },
                        };
                        Beaprint.DictPrint(vault_creds, colorsC, true);
                    }
                    else
                    {
                        StyleSheet styleSheetVC = new StyleSheet(color_default);
                        styleSheetVC.AddStyle("Identity.*", color_bad);
                        styleSheetVC.AddStyle("Credential.*", color_bad);
                        styleSheetVC.AddStyle("GUID.*", Color.DarkOrange);
                        Beaprint.DictPrint(vault_creds, styleSheetVC, true);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsC = new Dictionary<string, string>()
                            {
                                { "User:.*", ansi_color_bad },
                            };
                            Beaprint.AnsiPrint(MyUtils.ExecCMD("cmdkey /list"), colorsC);
                        }
                        else
                        {
                            StyleSheet styleSheetCM = new StyleSheet(color_default);
                            styleSheetCM.AddStyle("User:.*", color_bad);
                            styleSheetCM.AddStyle("Currently stored credentials:|Target:|Type:", color_key);
                            Colorful.Console.WriteLineStyled("    " + MyUtils.ExecCMD("cmdkey /list"), styleSheetCM);
                        }
                        Beaprint.InfoPrint("If any cred was found, you can use it with 'runas /savecred'");
                    }
                    else
                    {
                        Beaprint.GrayPrint("    This function is not still implemented.");
                        Beaprint.InfoPrint("If you want to list credentials inside Credential Manager use 'cmdkey /list'");
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }

            }

            void PrintSavedRDPInfo()
            {
                try
                {
                    Beaprint.MainPrint("Saved RDP connections", "");

                    List<Dictionary<string, string>> rdps_info = KnownFileCredsInfo.GetSavedRDPConnections();
                    if (rdps_info.Count > 0)
                        Colorful.Console.WriteLineStyled(String.Format("    {0,-20}{1,-55}{2}", "Host", "Username Hint", "User SID"), onlyKeyStyleSheet);
                    else
                        Beaprint.NotFoundPrint();

                    foreach (Dictionary<string, string> rdp_info in rdps_info)
                        Colorful.Console.WriteLineStyled(String.Format("    {0,-20}{1,-55}{2}", rdp_info["Host"], rdp_info["Username Hint"], rdp_info["SID"]), onlyDefaultStyleSheet);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                        string formString = "    {0}({1})";
                        foreach (Dictionary<string, string> rf in master_keys)
                        {
                            Formatter[] colorsString = new Formatter[]
                            {
                                new Formatter(rf["MasterKey"], color_default),
                                new Formatter(rf["Accessed"], Color.Gray),
                            };
                            Colorful.Console.WriteLineFormatted(formString, color_default, colorsString);

                        }
                        if (MyUtils.IsHighIntegrity())
                            Beaprint.InfoPrint("Follow the provided link for further instructions in how to decrypt the masterkey.");
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintWifi()
            {
                try
                {
                    Beaprint.MainPrint("Looking saved Wifis", "");
                    if (exec_cmd)
                    {
                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsC = new Dictionary<string, string>()
                            {
                                { ": .*", ansi_color_bad },
                            };
                            Beaprint.AnsiPrint("    " + MyUtils.ExecCMD("netsh wlan show profile"), colorsC);
                        }
                        else
                        {
                            StyleSheet styleSheetWf = new StyleSheet(color_key);
                            styleSheetWf.AddStyle(": .*", color_bad);
                            Colorful.Console.WriteLineStyled(MyUtils.ExecCMD("netsh wlan show profile"), styleSheetWf);
                        }
                    }
                    else
                    {
                        Beaprint.GrayPrint("    This function is not still implemented.");
                        Beaprint.InfoPrint("If you want to list saved Wifis connections you can list the using 'netsh wlan show profile'");
                    }
                    Beaprint.InfoPrint("If you want to get the clear-text password use 'netsh wlan show profile <SSID> key=clear'");
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                        Beaprint.BadPrint("    AlwaysInstallElevated isn't available");
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                        Beaprint.BadPrint("    WSUS is using http!");
                        Beaprint.InfoPrint("You can test https://github.com/pimps/wsuxploit to escalate privileges");
                        if (using_HKLM_WSUS == "1")
                            Beaprint.BadPrint("    And UseWUServer is equals to 1, so it is vulnerable!");
                        else if (using_HKLM_WSUS == "0")
                            Beaprint.GoodPrint("    But UseWUServer is equals to 0, so it is not vulnerable!");
                        else
                            Colorful.Console.WriteLine("    But UseWUServer is equals to " + using_HKLM_WSUS + ", so it may work or not", color_default);
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
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsB = new Dictionary<string, string>()
                            {
                                { print_credStrings, ansi_color_bad },
                            };
                            foreach (string url in firefoxHist)
                            {
                                if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                    Beaprint.AnsiPrint("    " + url, colorsB);
                            }
                        }
                        else
                        {
                            StyleSheet styleSheetHF = new StyleSheet(color_default);
                            styleSheetHF.AddStyle(print_credStrings, color_bad);
                            foreach (string url in firefoxHist)
                            {
                                if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                    Colorful.Console.WriteLineStyled("    " + url, styleSheetHF);
                            }
                        }
                    }
                    else
                    {
                        Beaprint.NotFoundPrint();
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
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
                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsB = new Dictionary<string, string>()
                            {
                                { print_credStrings, ansi_color_bad },
                            };
                            foreach (string url in history)
                            {
                                if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                    Beaprint.AnsiPrint("    " + url, colorsB);
                            }
                        }
                        else
                        {
                            StyleSheet styleSheetHF = new StyleSheet(color_default);
                            styleSheetHF.AddStyle(print_credStrings, color_bad);
                            foreach (string url in history)
                            {
                                if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                    Colorful.Console.WriteLineStyled("    " + url, styleSheetHF);
                            }
                        }
                        Colorful.Console.WriteLine();
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrinteCurrentIETabs()
            {
                try
                {
                    Beaprint.MainPrint("Current IE tabs", "T1503");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                    List<string> urls = KnownFileCredsInfo.GetCurrentIETabs();

                    if (using_ansi)
                    {
                        Dictionary<string, string> colorsB = new Dictionary<string, string>()
                        {
                            { print_credStrings, ansi_color_bad },
                        };
                        Beaprint.ListPrint(urls, colorsB);
                    }
                    else
                    {
                        StyleSheet styleSheetIET = new StyleSheet(color_default);
                        styleSheetIET.AddStyle(print_credStrings, color_bad);
                        Beaprint.ListPrint(urls, styleSheetIET);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                        if (using_ansi)
                        {
                            Dictionary<string, string> colorsB = new Dictionary<string, string>()
                            {
                                { print_credStrings, ansi_color_bad },
                            };
                            foreach (string url in history)
                            {
                                if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                    Beaprint.AnsiPrint("    " + url, colorsB);
                            }
                        }
                        else
                        {
                            StyleSheet styleSheetHF = new StyleSheet(color_default);
                            styleSheetHF.AddStyle(print_credStrings, color_bad);
                            foreach (string url in history)
                            {
                                if (MyUtils.ContainsAnyRegex(url.ToUpper(), credStringsRegex))
                                    Colorful.Console.WriteLineStyled("    " + url, styleSheetHF);
                            }
                        }
                        Colorful.Console.WriteLine();
                    }

                    Beaprint.MainPrint("IE favorites", "T1217");
                    Beaprint.ListPrint(favorites);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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

                    if (using_ansi)
                    {
                        Dictionary<string, string> colorF = new Dictionary<string, string>()
                        {
                            { "ProxyPassword.*", ansi_color_bad },
                        };
                        Beaprint.DictPrint(putty_sess, colorF, true);
                    }
                    else
                    {
                        StyleSheet styleSheetPS = new StyleSheet(color_default);
                        styleSheetPS.AddStyle("ProxyPassword.*", color_bad);
                        
                        Beaprint.DictPrint(putty_sess, styleSheetPS, true);
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintPuttySSH()
            {
                try
                {
                    Beaprint.MainPrint("Putty SSH Host keys", "");
                    List<Dictionary<string, string>> putty_sess = KnownFileCredsInfo.ListPuttySSHHostKeys();
                    Beaprint.DictPrint(putty_sess, false);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                            Formatter[] colorsString = new Formatter[]
                            {
                                new Formatter(cc["Description"], color_default),
                                new Formatter(cc["file"], color_bad),
                                new Formatter(cc["Accessed"], Color.Gray),
                                new Formatter(cc["Size"], Color.Gray),
                            };
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                            Colorful.Console.WriteLine("");
                        }
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintUserCredsFiles()
            {
                try
                {
                    string patterns = "*credential*;*password*";
                    string pattern_color = "[cC][rR][eE][dD][eE][nN][tT][iI][aA][lL]|[pP][aA][sS][sS][wW][oO][rR][dD]";
                    List<string> valid_extensions = new List<string>() { ".txt", ".conf", ".cnf", ".yml", ".yaml", ".doc", ".docx", ".xlsx", ".json", ".xml" };
                    StyleSheet styleSheetPCF = new StyleSheet(color_default);
                    styleSheetPCF.AddStyle(pattern_color, color_bad);
                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { pattern_color, ansi_color_bad },
                    };

                    Beaprint.MainPrint("Looking for possible password files in users homes", "T1083&T1081");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                    string searchPath = String.Format("{0}\\", Environment.GetEnvironmentVariable("SystemDrive") + "\\Users");
                    List<string> files_paths = MyUtils.FindFiles(searchPath, patterns);
                    foreach (string file_path in files_paths)
                    {
                        if (!Path.GetFileName(file_path).Contains("."))
                        {
                            if (using_ansi)
                                Beaprint.AnsiPrint("    " + file_path, colorF);
                            else
                                Colorful.Console.WriteLineStyled("    " + file_path, styleSheetPCF);
                        }
                        else
                        {
                            foreach (string ext in valid_extensions)
                            {
                                if (file_path.Contains(ext))
                                {
                                    if (using_ansi)
                                        Beaprint.AnsiPrint("    " + file_path, colorF);
                                    else
                                        Colorful.Console.WriteLineStyled("    " + file_path, styleSheetPCF);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintRecycleBin()
            {
                try
                {
                    StyleSheet styleSheetRB = new StyleSheet(color_default);
                    styleSheetRB.AddStyle(patterns_file_creds_color, color_bad);
                    string pattern_bin = patterns_file_creds + ";*password*;*credential*";
                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { patterns_file_creds + "|.*password.*|.*credential.*", ansi_color_bad },
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
                                if (using_ansi)
                                    Beaprint.DictPrint(rec_file, colorF, true);
                                else
                                    Beaprint.DictPrint(rec_file, styleSheetRB, true);
                                Colorful.Console.WriteLine();
                            }
                        }
                    }
                    if (recy_files.Count <= 0)
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintPossCredsFiles()
            {
                try
                {
                    StyleSheet styleSheetPCF = new StyleSheet(color_default);
                    styleSheetPCF.AddStyle(patterns_file_creds_color, color_bad);
                    Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { patterns_file_creds_color, ansi_color_bad },
                    };

                    Beaprint.MainPrint("Looking for possible known files that can contain creds", "T1083&T1081");
                    Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                    string searchPath = String.Format("{0}\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    MyUtils.FindFiles(searchPath, patterns_file_creds, styleSheetPCF, colorF);
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
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
                    Colorful.Console.WriteLine(ex);
                }
            }

            void PrintRecentFiles()
            {
                try
                {
                    Beaprint.MainPrint("Recent files --limit 70--", "T1083&T1081");
                    List<Dictionary<string, string>> rec_files = KnownFileCredsInfo.GetRecentFiles();
                    if (rec_files.Count != 0)
                    {
                        string formString = "    {0}({1})";
                        foreach (Dictionary<string, string> rf in rec_files.GetRange(0, rec_files.Count <= 70 ? rec_files.Count : 70))
                        {
                            Formatter[] colorsString = new Formatter[]
                            {
                                new Formatter(rf["Target"], color_default),
                                new Formatter(rf["Accessed"], Color.Gray),
                            };
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                        }
                    }
                    else
                        Beaprint.NotFoundPrint();
                }
                catch (Exception ex)
                {
                    Colorful.Console.WriteLine(ex);
                }
            }


            Beaprint.GreatPrint("Interesting files and registry");
            PrintPuttySess();
            PrintPuttySSH();
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
            interestingUsersGroups.AddRange(currentUserGroups);
            paint_interestingUserGroups = String.Join("|", interestingUsersGroups);

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

                if (string.Equals(arg, "ansi", StringComparison.CurrentCultureIgnoreCase))
                    using_ansi = true;

                if (string.Equals(arg, "quiet", StringComparison.CurrentCultureIgnoreCase))
                    banner = false;

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


