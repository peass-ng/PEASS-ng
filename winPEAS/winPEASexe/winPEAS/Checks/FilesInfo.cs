using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.Helpers.Search;
using winPEAS.Info.FilesInfo.Certificates;
using winPEAS.Info.FilesInfo.McAfee;
using winPEAS.Info.UserInfo;
using winPEAS.InterestingFiles;
using winPEAS.KnownFileCreds;
using winPEAS.KnownFileCreds.Slack;
using winPEAS.KnownFileCreds.SuperPutty;

namespace winPEAS.Checks
{
    internal class FilesInfo : ISystemCheck
    {
        static readonly string _patternsFileCredsColor = @"RDCMan.settings|.rdg|_history|httpd.conf|.htpasswd|.gitconfig|.git-credentials|Dockerfile|docker-compose.ymlaccess_tokens.db|accessTokens.json|azureProfile.json|appcmd.exe|scclient.exe|unattend.txt|access.log|error.log|credential|password|.gpg|.pgp|config.php|elasticsearch|kibana.|.p12|\.der|.csr|.crt|.cer|.pem|known_hosts|id_rsa|id_dsa|.ovpn|tomcat-users.xml|web.config|.kdbx|.key|KeePass.config|ntds.dir|Ntds.dit|sam|system|SAM|SYSTEM|security|software|SECURITY|SOFTWARE|FreeSSHDservice.ini|sysprep.inf|sysprep.xml|unattend.xml|unattended.xml|vnc|groups.xml|services.xml|scheduledtasks.xml|printers.xml|drives.xml|datasources.xml|php.ini|https.conf|https-xampp.conf|my.ini|my.cnf|access.log|error.log|server.xml|setupinfo|pagefile.sys|NetSetup.log|iis6.log|AppEvent.Evt|SecEvent.Evt|default.sav|security.sav|software.sav|system.sav|ntuser.dat|index.dat|bash.exe|wsl.exe";
    //    static readonly string _patternsFileCreds = @"RDCMan.settings;*.rdg;*_history*;httpd.conf;.htpasswd;.gitconfig;.git-credentials;Dockerfile;docker-compose.yml;access_tokens.db;accessTokens.json;azureProfile.json;appcmd.exe;scclient.exe;*.gpg$;*.pgp$;*config*.php;elasticsearch.y*ml;kibana.y*ml;*.p12$;*.cer$;known_hosts;*id_rsa*;*id_dsa*;*.ovpn;tomcat-users.xml;web.config;*.kdbx;KeePass.config;Ntds.dit;SAM;SYSTEM;security;software;FreeSSHDservice.ini;sysprep.inf;sysprep.xml;*vnc*.ini;*vnc*.c*nf*;*vnc*.txt;*vnc*.xml;php.ini;https.conf;https-xampp.conf;my.ini;my.cnf;access.log;error.log;server.xml;ConsoleHost_history.txt;pagefile.sys;NetSetup.log;iis6.log;AppEvent.Evt;SecEvent.Evt;default.sav;security.sav;software.sav;system.sav;ntuser.dat;index.dat;bash.exe;wsl.exe;unattend.txt;*.der$;*.csr$;unattend.xml;unattended.xml;groups.xml;services.xml;scheduledtasks.xml;printers.xml;drives.xml;datasources.xml;setupinfo;setupinfo.bak";

        private static readonly IList<string> patternsFileCreds = new List<string>()
        {
            "*.cer$",
            "*.csr$",
            "*.der$",
            "*.ftpconfig",
            "*.gpg$",
            "*.kdbx",
            "*.ovpn",
            "*.p12$",
            "*.pgp$",
            "*.rdg",
            "*_history*",
            "*config*.php",
            "*id_dsa*",
            "*id_rsa*",
            "*vnc*.c*nf*",
            "*vnc*.ini",
            "*vnc*.txt",
            "*vnc*.xml",
            ".git-credentials",
            ".gitconfig",
            ".htpasswd",
            "AppEvent.Evt",
            "ConsoleHost_history.txt",
            "Dockerfile",
            "FreeSSHDservice.ini",
            "KeePass.config",
            "NetSetup.log",
            "Ntds.dit",
            "RDCMan.settings",
            "SAM",
            "SYSTEM",
            "SecEvent.Evt",
            "access.log",
            "accessTokens.json",
            "access_tokens.db",
            "appcmd.exe",
            "azureProfile.json",
            "bash.exe",
            "datasources.xml",
            "default.sav",
            "docker-compose.yml",
            "drives.xml",
            "elasticsearch.y*ml",
            "error.log",
            "ffftp.ini",
            "filezilla.xml",
            "groups.xml",
            "httpd.conf",
            "https-xampp.conf",
            "https.conf",
            "iis6.log",
            "index.dat",
            "kibana.y*ml",
            "known_hosts",
            "my.cnf",
            "my.ini",
            "ntuser.dat",
            "pagefile.sys",
            "php.ini",
            "printers.xml",
            "recentservers.xml",
            "scclient.exe",
            "scheduledtasks.xml",
            "security",
            "security.sav",
            "server.xml",
            "services.xml",
            "setupinfo",
            "setupinfo.bak",
            "sitemanager.xml",
            "sites.ini",
            "software",
            "software.sav",
            "sysprep.inf",
            "sysprep.xml",
            "system.sav",
            "tomcat-users.xml",
            "unattend.txt",
            "unattend.xml",
            "unattended.xml",
            "wcx_ftp.ini",
            "web.*.config",
            "winscp.ini",
            "ws_ftp.ini",
            "wsl.exe",
        };


        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Interesting files and registry");

            new List<Action>
            {
                Putty.PrintInfo,
                SuperPutty.PrintInfo,
                PrintCloudCreds,
                PrintUnattendFiles,
                PrintSAMBackups,
                PrintMcAffeSitelistFiles,
                PrintLinuxShells,
                PrintCachedGPPPassword,
                PrintPossCredsRegs,
                PrintUserCredsFiles,
                PrintOracleSQLDeveloperConfigFiles,
                Slack.PrintInfo,
                PrintOutlookDownloads,
                PrintMachineAndUserCertificateFiles,
                PrintUsersInterestingFiles,
                PrintUsersDocsKeys,
                PrintRecentFiles,
                PrintRecycleBin,
                PrintHiddenFilesAndFolders,
                PrintOtherUsersInterestingFiles,
                PrintExecutablesInNonDefaultFoldersWithWritePermissions,
            }.ForEach(action => CheckRunner.Run(action, isDebug));

            SearchHelper.CleanLists();
        }

        void PrintCloudCreds()
        {
            try
            {
                Beaprint.MainPrint("Cloud Credentials");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                List<Dictionary<string, string>> could_creds = KnownFileCredsInfo.ListCloudCreds();
                if (could_creds.Count != 0)
                {
                    foreach (Dictionary<string, string> cc in could_creds)
                    {
                        string formString = "    {0} ({1})\n    Accessed:{2} -- Size:{3}";
                        Beaprint.BadPrint(string.Format(formString, cc["file"], cc["Description"], cc["Accessed"], cc["Size"]));
                        System.Console.WriteLine("");
                    }
                }
                else
                    Beaprint.NotFoundPrint();
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintUnattendFiles()
        {
            try
            {
                Beaprint.MainPrint("Unattend Files");
                //Beaprint.LinkPrint("");
                List<string> unattended_files = Unattended.GetUnattendedInstallFiles();
                foreach (string path in unattended_files)
                {
                    List<string> pwds = Unattended.ExtractUnattendedPwd(path);
                    Beaprint.BadPrint("    " + path);
                    System.Console.WriteLine(string.Join("\n", pwds));
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintSAMBackups()
        {
            try
            {
                Beaprint.MainPrint("Looking for common SAM & SYSTEM backups");
                List<string> sam_files = InterestingFiles.InterestingFiles.GetSAMBackups();
                foreach (string path in sam_files)
                    Beaprint.BadPrint("    " + path);

            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintMcAffeSitelistFiles()
        {
            try
            {
                Beaprint.MainPrint("Looking for McAfee Sitelist.xml Files");
                var sitelistFilesInfos = McAfee.GetMcAfeeSitelistInfos();

                foreach (var sitelistFilesInfo in sitelistFilesInfos)
                {
                    Beaprint.NoColorPrint($"   Path:                    {sitelistFilesInfo.Path}");

                    if (!string.IsNullOrEmpty(sitelistFilesInfo.ParseException))
                    {
                        Beaprint.NoColorPrint($"   Parse Exception:           {sitelistFilesInfo.ParseException}");
                    }

                    foreach (var site in sitelistFilesInfo.Sites)
                    {
                        Beaprint.BadPrint($"    ShareName       : {site.ShareName}\n" +
                                          $"    UserName        : {site.UserName}\n" +
                                          $"    Server          : {site.Server}\n" +
                                          $"    EncPassword     : {site.EncPassword}\n" +
                                          $"    DecPassword     : {site.DecPassword}\n" +
                                          $"    DomainName      : {site.DomainName}\n" +
                                          $"    Name            : {site.Name}\n" +
                                          $"    Type            : {site.Type}\n" +
                                          $"    RelativePath    : {site.RelativePath}\n");
                    }

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintLinuxShells()
        {
            Beaprint.MainPrint("Looking for Linux shells/distributions - wsl.exe, bash.exe");
            List<string> linuxShells = InterestingFiles.InterestingFiles.GetLinuxShells();
            string hive = "HKCU";
            string basePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss";

            if (linuxShells.Any())
            {
                foreach (string path in linuxShells)
                {
                    Beaprint.BadPrint("    " + path);
                }

                Beaprint.BadPrint("");

                try
                {
                    var wslKeys = RegistryHelper.GetRegSubkeys(hive, basePath);

                    if (wslKeys.Any())
                    {
                        const string linpeas = "linpeas.sh";
                        const string distribution = "Distribution";
                        const string rootDirectory = "Root directory";
                        const string runWith = "Run command";


                        Dictionary<string, string> colors = new Dictionary<string, string>();                        
                        new List<string>
                        {
                            linpeas,
                            distribution,
                            rootDirectory,
                            runWith
                        }.ForEach(str => colors.Add(str, Beaprint.ansi_color_bad));

                        Beaprint.BadPrint("    Found installed WSL distribution(s) - listed below");
                        Beaprint.AnsiPrint($"    Run {linpeas} in your WSL distribution(s) home folder(s).\n", colors);

                        foreach (var wslKey in wslKeys)
                        {
                            try
                            {
                                string distributionSubKey = $"{basePath}\\{wslKey}";
                                string distributionRootDirectory = $"{RegistryHelper.GetRegValue(hive, distributionSubKey, "BasePath")}\\rootfs";
                                string distributionName = RegistryHelper.GetRegValue(hive, distributionSubKey, "DistributionName");

                                Beaprint.AnsiPrint($"    {distribution}:      \"{distributionName}\"\n" +
                                                   $"    {rootDirectory}:    \"{distributionRootDirectory}\"\n" +
                                                   $"    {runWith}:       wsl.exe --distribution \"{distributionName}\"",
                                                    colors);
                                Beaprint.PrintLineSeparator();
                            }
                            catch (Exception) { }
                        }
                    }
                    else
                    {
                        Beaprint.GoodPrint("    WSL - no installed Linux distributions found.");
                    }
                }
                catch (Exception) { }
            }
        }

        void PrintCachedGPPPassword()
        {
            try
            {
                Beaprint.MainPrint("Cached GPP Passwords");
                Dictionary<string, Dictionary<string, string>> gpp_passwords = GPP.GetCachedGPPPassword();

                Dictionary<string, string> gppColors = new Dictionary<string, string>()
                    {
                        { "cpassword.*", Beaprint.ansi_color_bad },
                    };

                foreach (KeyValuePair<string, Dictionary<string, string>> entry in gpp_passwords)
                {
                    Beaprint.BadPrint("    Found " + entry.Key);
                    Beaprint.DictPrint(entry.Value, gppColors, true);
                }

            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintPossCredsRegs()
        {
            try
            {
                string[] passRegHkcu = new string[] { @"Software\ORL\WinVNC3\Password", @"Software\TightVNC\Server", @"Software\SimonTatham\PuTTY\Sessions" };
                string[] passRegHklm = new string[] { @"SYSTEM\CurrentControlSet\Services\SNMP" };

                Beaprint.MainPrint("Looking for possible regs with creds");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#inside-the-registry");

                string winVnc4 = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\RealVNC\WinVNC4", "password");
                if (!string.IsNullOrEmpty(winVnc4.Trim()))
                {
                    Beaprint.BadPrint(winVnc4);
                }

                foreach (string regHkcu in passRegHkcu)
                {
                    Beaprint.DictPrint(RegistryHelper.GetRegValues("HKLM", regHkcu), false);
                }

                foreach (string regHklm in passRegHklm)
                {
                    Beaprint.DictPrint(RegistryHelper.GetRegValues("HKLM", regHklm), false);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintUserCredsFiles()
        {
            try
            {
                string pattern_color = "[cC][rR][eE][dD][eE][nN][tT][iI][aA][lL]|[pP][aA][sS][sS][wW][oO][rR][dD]";                                
                var validExtensions = new HashSet<string>
                {
                    ".cnf",
                    ".conf",
                    ".doc",
                    ".docx",
                    ".json",
                    ".xlsx",
                    ".xml",
                    ".yaml",
                    ".yml",
                    ".txt",
                };

                var colorF = new Dictionary<string, string>()
                {
                    { pattern_color, Beaprint.ansi_color_bad },
                };

                Beaprint.MainPrint("Looking for possible password files in users homes");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");               
                var fileInfos = SearchHelper.SearchUserCredsFiles();

                foreach (var fileInfo in fileInfos)
                {
                    if (!fileInfo.Filename.Contains("."))
                    {
                        Beaprint.AnsiPrint("    " + fileInfo.FullPath, colorF);
                    }
                    else
                    {
                        string extLower = fileInfo.Extension.ToLower();

                        if (validExtensions.Contains(extLower))
                        {
                            Beaprint.AnsiPrint("    " + fileInfo.FullPath, colorF);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintRecycleBin()
        {
            try
            {
                //string pattern_bin = _patternsFileCreds + ";*password*;*credential*";
                string pattern_bin = string.Join(";", patternsFileCreds) + ";*password*;*credential*";
                
                Dictionary<string, string> colorF = new Dictionary<string, string>()
                {
                    { _patternsFileCredsColor + "|.*password.*|.*credential.*", Beaprint.ansi_color_bad },
                };

                Beaprint.MainPrint("Looking inside the Recycle Bin for creds files");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                List<Dictionary<string, string>> recy_files = InterestingFiles.InterestingFiles.GetRecycleBin();
                
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
                {
                    Beaprint.NotFoundPrint();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintUsersInterestingFiles()
        {
            try
            {
                var colorF = new Dictionary<string, string>
                {
                    { _patternsFileCredsColor, Beaprint.ansi_color_bad },
                };

                Beaprint.MainPrint("Searching known files that can contain creds in home");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
               
                var files = SearchHelper.SearchUsersInterestingFiles();

                Beaprint.AnsiPrint("    " + string.Join("\n    ", files), colorF);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintUsersDocsKeys()
        {
            try
            {
                Beaprint.MainPrint("Looking for documents --limit 100--");
                List<string> docFiles = InterestingFiles.InterestingFiles.ListUsersDocs();
                Beaprint.ListPrint(docFiles.GetRange(0, docFiles.Count <= 100 ? docFiles.Count : 100));
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintRecentFiles()
        {
            try
            {
                Beaprint.MainPrint("Recent files --limit 70--");
                List<Dictionary<string, string>> recFiles = KnownFileCredsInfo.GetRecentFiles();

                Dictionary<string, string> colorF = new Dictionary<string, string>()
                {
                    { _patternsFileCredsColor, Beaprint.ansi_color_bad },
                };

                if (recFiles.Count != 0)
                {
                    foreach (Dictionary<string, string> recF in recFiles.GetRange(0, recFiles.Count <= 70 ? recFiles.Count : 70))
                    {
                        Beaprint.AnsiPrint("    " + recF["Target"] + "(" + recF["Accessed"] + ")", colorF);
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

        void PrintOtherUsersInterestingFiles()
        {
            try
            {
                Beaprint.MainPrint("Searching interesting files in other users home directories (can be slow)\n");
                
                // check if admin already, if yes, print a message, if not, try to enumerate all files
                if (MyUtils.IsHighIntegrity())
                {
                    Beaprint.BadPrint("     You are already Administrator, check users home folders manually.");
                }
                else
                // get all files and check them
                {
                    var users = User.GetOtherUsersFolders();

                    foreach (var user in users)
                    {
                        Beaprint.GoodPrint($"     Checking folder: {user}\n");

                        var files = SearchHelper.GetFilesFast(user, isFoldersIncluded: true);

                        foreach (var file in files)
                        {
                            try
                            {
                                FileAttributes attr = File.GetAttributes(file.FullPath);
                                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                                {
                                    List<string> dirRights = PermissionsHelper.GetPermissionsFolder(file.FullPath, Checks.CurrentUserSiDs, isOnlyWriteOrEquivalentCheck: true);

                                    if (dirRights.Count > 0)
                                    {
                                        Beaprint.BadPrint($"     Folder Permissions \"{file.FullPath}\": " + string.Join(",", dirRights));
                                    }
                                }
                                else
                                {
                                    List<string> fileRights = PermissionsHelper.GetPermissionsFile(file.FullPath, Checks.CurrentUserSiDs, isOnlyWriteOrEquivalentCheck: true);

                                    if (fileRights.Count > 0)
                                    {
                                        Beaprint.BadPrint($"     File Permissions \"{file.FullPath}\": " + string.Join(",", fileRights));
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }

                        Beaprint.PrintLineSeparator();
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintHiddenFilesAndFolders()
        {
            HashSet<string> excludedFilenames = new HashSet<string>()
                {
                    "cache.bin",
                    "container.dat",
                    "desktop.ini",
                    "iconcache.db",
                    "ntuser.ini",
                    "ntuser.dat",
                    "ntuser.dat.log1",
                    "ntuser.dat.log2",
                    "pof.dat.log1",
                    "pof.dat.log2",
                    "privateregistry.bin.log1",
                    "privateregistry.bin.log2",
                    "settings.dat.log1",
                    "settings.dat.log2",
                    "thumbs.db",
                    "user.dat.log1",
                    "user.dat.log2",
                    "userclasses.dat",
                    "userclasses.dat.log1",
                    "userclasses.dat.log2",
                    "usrclass.dat",
                    "usrclass.dat.log1",
                    "usrclass.dat.log2",
                };

            HashSet<string> excludedExtensions = new HashSet<string>()
                {
                    ".blf",
                    ".igpi",
                    ".regtrans-ms",
                    ".search-ms",
                    ".suo",
                };

            HashSet<string> excludedKnownFolders = new HashSet<string>()
                {
                    "accountpictures",
                    "appdata",
                    "application data",
                    "cookies",
                    "desktop",
                    "documents",
                    "intelgraphicsprofiles",
                    "libraries",
                    "local settings",
                    "my documents",
                    "nethood",
                    "printhood",
                    "recent",
                    "recent",
                    "sendto",
                    "start menu",
                    "templates",
                };

            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");

            Beaprint.MainPrint($"Searching hidden files or folders in {systemDrive}\\Users home (can be slow)\n");

            foreach (var file in SearchHelper.RootDirUsers)
            {
                try
                {
                    if (File.GetAttributes(file.FullPath).HasFlag(FileAttributes.Hidden))
                    {
                        if (file.Extension != null && excludedExtensions.Contains(file.Extension.ToLower()))
                        {
                            continue;
                        }

                        if (file.Filename != null && excludedFilenames.Contains(file.Filename.ToLower()))
                        {
                            continue;
                        }

                        // skip well known folders
                        if (excludedKnownFolders.Contains(Path.GetFileName(file.FullPath).ToLower()))
                        {
                            continue;
                        }

                        if (file.FullPath.ToLower().Contains("microsoft"))
                        {
                            continue;
                        }

                        Beaprint.BadPrint($"     {file.FullPath}");
                    }
                }
                catch (PathTooLongException) { }
                catch (Exception)
                {
                    // & other exceptions
                }
            }
        }

        private void PrintExecutablesInNonDefaultFoldersWithWritePermissions()
        {
            Beaprint.MainPrint($"Searching executable files in non-default folders with write (equivalent) permissions (can be slow)");

            var systemDrive = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\";

            var excludedDirs = new HashSet<string>()
            {
                @"c:\esupport",
                @"c:\perflogs",
                @"c:\programdata",
                @"c:\program files (x86)",
                @"c:\program files",
                @"c:\windows",
                @"c:\windows.old",
            };

            var currentUserDir = @$"{systemDrive}users\{Environment.GetEnvironmentVariable("USERNAME")}".ToLower();

            var allowedExtensions = new HashSet<string>()
            {
                ".bat",
                ".exe",
                ".ps1"
            };

            var files = SearchHelper.GetFilesFast(systemDrive, "*", excludedDirs);            

            foreach (var file in files)
            {
                try
                {
                    if (file.Extension != null && allowedExtensions.Contains(file.Extension.ToLower()))
                    {
                        // check the file permissions
                        List<string> fileRights = PermissionsHelper.GetPermissionsFile(file.FullPath, Checks.CurrentUserSiDs, isOnlyWriteOrEquivalentCheck: true);

                        if (fileRights.Count > 0)
                        {
                            string log = $"     File Permissions \"{file.FullPath}\": " + string.Join(",", fileRights);

                            if (file.FullPath.ToLower().StartsWith(currentUserDir))
                            {
                                Beaprint.NoColorPrint(log);
                            }
                            else
                            {
                                Beaprint.BadPrint(log);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static void PrintOracleSQLDeveloperConfigFiles()
        {
            Beaprint.MainPrint($"Searching for Oracle SQL Developer config files\n");

            var userFolders = User.GetUsersFolders();

            foreach (var userFolder in userFolders)
            {
                try
                {
                    var path = $"{userFolder}\\AppData\\Roaming\\SQL Developer\\";
                    var pattern = "connections*.xml";

                    if (Directory.Exists(path))
                    {
                        var files = Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly);

                        foreach (var file in files)
                        {
                            if (File.Exists(file))
                            {
                                Beaprint.BadPrint($"     {file}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        private static void PrintMachineAndUserCertificateFiles()
        {
            Beaprint.MainPrint($"Enumerating machine and user certificate files\n");

            try
            {
                var certificateInfos = Certificates.GetCertificateInfos();

                foreach (var certificateInfo in certificateInfos)
                {
                    
                    Beaprint.NoColorPrint($"  Issuer             : {certificateInfo.Issuer}\n" +
                                                $"  Subject            : {certificateInfo.Subject}\n" +
                                                $"  ValidDate          : {certificateInfo.ValidDate}\n"  +
                                                $"  ExpiryDate         : {certificateInfo.ExpiryDate}\n" +
                                                $"  HasPrivateKey      : {certificateInfo.HasPrivateKey}\n"  +
                                                $"  StoreLocation      : {certificateInfo.StoreLocation}\n"  +
                                                $"  KeyExportable      : {certificateInfo.KeyExportable}\n"  +
                                                $"  Thumbprint         : {certificateInfo.Thumbprint}\n");

                    if (!string.IsNullOrEmpty(certificateInfo.Template))
                    {
                        Beaprint.NoColorPrint($"  Template           : {certificateInfo.Template}");
                    }

                    if (certificateInfo.EnhancedKeyUsages?.Count > 0)
                    {
                        Beaprint.ColorPrint("  Enhanced Key Usages", Beaprint.LBLUE);

                        foreach (var keyUsages in certificateInfo.EnhancedKeyUsages)
                        {
                            var info = keyUsages == "Client Authentication" ? "     [*] Certificate is used for client authentication!" : string.Empty;

                            Beaprint.NoColorPrint($"       {keyUsages}{info}");
                        }
                    }
                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static void PrintOutlookDownloads()
        {
            Beaprint.MainPrint("Enumerating Outlook download files\n");

            try
            {
                var userDirs = User.GetUsersFolders();

                foreach (var userDir in userDirs)
                {
                    try
                    {
                        var userOutlookBasePath = $"{userDir}\\AppData\\Local\\Microsoft\\Windows\\INetCache\\Content.Outlook\\";

                        if (Directory.Exists(userOutlookBasePath))
                        {
                            var files = SearchHelper.GetFilesFast(userOutlookBasePath, "*");

                            foreach (var file in files)
                            {
                                Beaprint.BadPrint($"   {file.FullPath}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }   
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
