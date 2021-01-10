using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.InterestingFiles;
using winPEAS.KnownFileCreds;

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
                PrintCloudCreds,
                PrintUnattendFiles,
                PrintSAMBackups,
                PrintMcAffeSitelistFiles,
                PrintLinuxShells,
                PrintCachedGPPPassword,
                PrintPossCredsRegs,
                PrintUserCredsFiles,
                PrintUsersInterestingFiles,
                PrintUsersDocsKeys,
                PrintRecentFiles,
                PrintRecycleBin
            }.ForEach(action => CheckRunner.Run(action, isDebug));
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

        void PrintMcAffeSitelistFiles()
        {
            try
            {
                Beaprint.MainPrint("Looking for McAfee Sitelist.xml Files");
                List<string> sam_files = InterestingFiles.InterestingFiles.GetMcAfeeSitelistFiles();
                foreach (string path in sam_files)
                {
                    Beaprint.BadPrint("    " + path);
                }

            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        void PrintLinuxShells()
        {
            try
            {
                Beaprint.MainPrint("Looking for Linux shells - wsl.exe, bash.exe");
                List<string> linuxShells = InterestingFiles.InterestingFiles.GetLinuxShells();

                foreach (string path in linuxShells)
                {
                    Beaprint.BadPrint("    " + path);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
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
                string[] pass_reg_hkcu = new string[] { @"Software\ORL\WinVNC3\Password", @"Software\TightVNC\Server", @"Software\SimonTatham\PuTTY\Sessions" };
                string[] pass_reg_hklm = new string[] { @"SYSTEM\CurrentControlSet\Services\SNMP" };

                Beaprint.MainPrint("Looking for possible regs with creds");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#inside-the-registry");

                string winVNC4 = RegistryHelper.GetRegValue("HKLM", @"SOFTWARE\RealVNC\WinVNC4", "passwword");
                if (!string.IsNullOrEmpty(winVNC4.Trim()))
                    Beaprint.BadPrint(winVNC4);

                foreach (string reg_hkcu in pass_reg_hkcu)
                    Beaprint.DictPrint(RegistryHelper.GetRegValues("HKLM", reg_hkcu), false);
                foreach (string reg_hklm in pass_reg_hklm)
                    Beaprint.DictPrint(RegistryHelper.GetRegValues("HKLM", reg_hklm), false);
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
                string patterns = "*credential*;*password*";
                string pattern_color = "[cC][rR][eE][dD][eE][nN][tT][iI][aA][lL]|[pP][aA][sS][sS][wW][oO][rR][dD]";
                List<string> valid_extensions = new List<string>() { ".txt", ".conf", ".cnf", ".yml", ".yaml", ".doc", ".docx", ".xlsx", ".json", ".xml" };
                Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { pattern_color, Beaprint.ansi_color_bad },
                    };

                Beaprint.MainPrint("Looking for possible password files in users homes");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                string searchPath = string.Format("{0}\\", Environment.GetEnvironmentVariable("SystemDrive") + "\\Users");
                List<string> files_paths = SearchHelper.FindFiles(searchPath, patterns);
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
                Dictionary<string, string> colorF = new Dictionary<string, string>()
                    {
                        { _patternsFileCredsColor, Beaprint.ansi_color_bad },
                    };

                Beaprint.MainPrint("Searching known files that can contain creds in home");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#credentials-inside-files");
                string searchPath = Environment.GetEnvironmentVariable("USERPROFILE");

                //SearchHelper.FindFiles(searchPath, _patternsFileCreds, colorF);
                string patterns = string.Join(";", patternsFileCreds);
                SearchHelper.FindFiles(searchPath, patterns, colorF);
                
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
                List<string> doc_files = InterestingFiles.InterestingFiles.ListUsersDocs();
                Beaprint.ListPrint(doc_files.GetRange(0, doc_files.Count <= 100 ? doc_files.Count : 100));
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
    }
}
