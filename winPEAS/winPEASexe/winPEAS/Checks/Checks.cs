using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Principal;
using winPEAS.Helpers;
using winPEAS.Helpers.AppLocker;
using winPEAS.Helpers.Registry;
using winPEAS.Helpers.Search;
using winPEAS.Helpers.YamlConfig;
using winPEAS.Info.NetworkInfo.NetworkScanner;
using winPEAS.Info.UserInfo;

namespace winPEAS.Checks
{
    public static class Checks
    {
        public static bool IsDomainEnumeration = false;
        public static bool IsNoColor = false;
        public static bool Banner = true;
        public static bool IsDebug = false;
        public static bool IsLinpeas = false;
        public static bool IsLolbas = false;
        public static bool IsNetworkScan = false;
        public static bool SearchProgramFiles = false;

        private static IEnumerable<int> PortScannerPorts = null;
        private static string NetworkScanOptions = string.Empty;

        // Create Dynamic blacklists
        public static readonly string CurrentUserName = Environment.UserName;
        public static string CurrentUserDomainName = Environment.UserDomainName;
        public static string CurrentAdDomainName = "";
        public static bool IsPartOfDomain = false;
        public static bool IsCurrentUserLocal = true;
        public static ManagementObjectCollection Win32Users = null;
        public static Dictionary<string, string> CurrentUserSiDs = new Dictionary<string, string>();
        static string _paintActiveUsers = "";
        public static string PaintActiveUsersNoAdministrator = "";
        public static string PaintDisabledUsers = "";
        public static string PaintDisabledUsersNoAdministrator = "";
        public static bool IsLongPath = false;
        public static bool WarningIsLongPath = false;
        public static int MaxRegexFileSize = 1000000;
        //static string paint_lockoutUsers = "";
        public static string PaintAdminUsers = "";
        public static YamlConfig YamlConfig;
        public static YamlRegexConfig RegexesYamlConfig;

        private static List<SystemCheck> _systemChecks;
        private static readonly HashSet<string> _systemCheckSelectedKeysHashSet = new HashSet<string>();

        // github url for Linpeas.sh
        public static string LinpeasUrl = "https://github.com/carlospolop/PEASS-ng/releases/latest/download/linpeas.sh";

        public const string DefaultLogFile = "out.txt";


        class SystemCheck
        {
            public string Key { get; }
            public ISystemCheck Check { get; }

            public SystemCheck(string key, ISystemCheck check)
            {
                this.Key = key;
                this.Check = check;
            }
        }

        internal static void Run(string[] args)
        {
            //Check parameters
            bool isAllChecks = true;
            bool isFileSearchEnabled = false;
            bool wait = false;
            FileStream fileStream = null;
            StreamWriter fileWriter = null;
            TextWriter oldOut = Console.Out;

            _systemChecks = new List<SystemCheck>
            {
                new SystemCheck("systeminfo", new SystemInfo()),
                new SystemCheck("eventsinfo", new EventsInfo()),
                new SystemCheck("userinfo", new UserInfo()),
                new SystemCheck("processinfo", new ProcessInfo()),
                new SystemCheck("servicesinfo", new ServicesInfo()),
                new SystemCheck("applicationsinfo", new ApplicationsInfo()),
                new SystemCheck("networkinfo", new NetworkInfo()),
                new SystemCheck("cloudinfo", new CloudInfo()),
                new SystemCheck("windowscreds", new WindowsCreds()),
                new SystemCheck("browserinfo", new BrowserInfo()),
                new SystemCheck("filesinfo", new FilesInfo()),
                new SystemCheck("fileanalysis", new FileAnalysis()),
            };

            var systemCheckAllKeys = new HashSet<string>(_systemChecks.Select(i => i.Key));
            var print_fileanalysis_warn = true;

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--help", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(arg, "help", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(arg, "/h", StringComparison.CurrentCultureIgnoreCase) ||
                    string.Equals(arg, "-h", StringComparison.CurrentCultureIgnoreCase))
                {
                    Beaprint.PrintUsage();
                    return;
                }

                if (string.Equals(arg, "fileanalysis", StringComparison.CurrentCultureIgnoreCase))
                {
                    print_fileanalysis_warn = false;
                    isFileSearchEnabled = true;
                }

                if (string.Equals(arg, "filesinfo", StringComparison.CurrentCultureIgnoreCase))
                {
                    isFileSearchEnabled = true;
                }

                if (string.Equals(arg, "all", StringComparison.CurrentCultureIgnoreCase))
                {
                    print_fileanalysis_warn = false;
                }

                if (arg.StartsWith("log", StringComparison.CurrentCultureIgnoreCase))
                {
                    // get logfile argument if present
                    string logFile = DefaultLogFile;
                    var parts = arg.Split('=');
                    if (parts.Length == 2)
                    {
                        logFile = parts[1];

                        if (string.IsNullOrWhiteSpace(logFile))
                        {
                            Beaprint.PrintException("Please specify a valid log file.");
                            return;
                        }
                    }

                    try
                    {
                        fileStream = new FileStream(logFile, FileMode.OpenOrCreate, FileAccess.Write);
                        fileWriter = new StreamWriter(fileStream);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.PrintException($"Cannot open \"{logFile}\" for writing:\n {ex.Message}");
                        return;
                    }

                    Beaprint.ColorPrint($"\"log\" argument present, redirecting output to file \"{logFile}\"", Beaprint.ansi_color_good);
                    Console.SetOut(fileWriter);
                }

                if (string.Equals(arg, "notcolor", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsNoColor = true;
                }

                if (string.Equals(arg, "quiet", StringComparison.CurrentCultureIgnoreCase))
                {
                    Banner = false;
                }

                if (string.Equals(arg, "wait", StringComparison.CurrentCultureIgnoreCase))
                {
                    wait = true;
                }

                if (string.Equals(arg, "debug", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsDebug = true;
                }

                if (string.Equals(arg, "domain", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsDomainEnumeration = true;
                }

                if (string.Equals(arg, "searchpf", StringComparison.CurrentCultureIgnoreCase))
                {
                    SearchProgramFiles = true;
                }

                if (string.Equals(arg, "max-regex-file-size", StringComparison.CurrentCultureIgnoreCase))
                {
                    var parts = arg.Split('=');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                    {
                        MaxRegexFileSize = Int32.Parse(parts[1]);
                    }

                }

                if (string.Equals(arg, "-lolbas", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsLolbas = true;
                }

                if (arg.StartsWith("-linpeas", StringComparison.CurrentCultureIgnoreCase))
                {
                    IsLinpeas = true;

                    var parts = arg.Split('=');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                    {
                        LinpeasUrl = parts[1];

                        var isReachable = MyUtils.IsUrlReachable(LinpeasUrl);

                        if (!isReachable)
                        {
                            Beaprint.ColorPrint($" [!] the provided linpeas.sh url: '{LinpeasUrl}' is invalid / unreachable / returned empty response.", Beaprint.YELLOW);

                            return;
                        }
                    }
                }

                if (arg.StartsWith("-network", StringComparison.CurrentCultureIgnoreCase))
                {
                    /*
                       -network="auto"                          -    find interfaces/hosts automatically
                       -network="10.10.10.10,10.10.10.20"       -    scan only selected ip address(es)
                       -network="10.10.10.10/24"                -    scan host based on ip address/netmask
                     */
                    if (!IsNetworkTypeValid(arg))
                    {
                        Beaprint.ColorPrint($" [!] the \"-network\" argument is invalid. For help, run winpeass.exe --help", Beaprint.YELLOW);

                        return;
                    }

                    var parts = arg.Split('=');
                    string networkType = parts[1];

                    IsNetworkScan = true;
                    NetworkScanOptions = networkType;
                }

                if (arg.StartsWith("-ports", StringComparison.CurrentCultureIgnoreCase))
                {
                    // e.g. -ports="80,443,8080"
                    var parts = arg.Split('=');
                    if (!IsNetworkScan || parts.Length != 2 || string.IsNullOrEmpty(parts[1]))
                    {
                        Beaprint.ColorPrint($" [!] the \"-network\" argument is not present or valid, add it if you want to define network scan ports. For help, run winpeass.exe --help", Beaprint.YELLOW);

                        return;
                    }

                    var portString = parts[1];
                    IEnumerable<int> ports = new List<int>();
                    try
                    {
                        PortScannerPorts = portString.Trim('"').Trim('\'').Split(',').ToList().ConvertAll<int>(int.Parse);
                    }
                    catch (Exception)
                    {
                        Beaprint.ColorPrint($" [!] the \"-ports\" argument is not present or valid, add it if you want to define network scan ports. For help, run winpeass.exe --help", Beaprint.YELLOW);

                        return;
                    }
                }

                string argToLower = arg.ToLower();
                if (systemCheckAllKeys.Contains(argToLower))
                {
                    _systemCheckSelectedKeysHashSet.Add(argToLower);
                    isAllChecks = false;
                }
            }

            if (print_fileanalysis_warn){
                _systemChecks.RemoveAt(_systemChecks.Count - 1);
                Beaprint.ColorPrint(" [!] If you want to run the file analysis checks (search sensitive information in files), you need to specify the 'fileanalysis' or 'all' argument. Note that this search might take several minutes. For help, run winpeass.exe --help", Beaprint.YELLOW);
            }

            if (isAllChecks)
            {
                isFileSearchEnabled = true;
            }

            try
            {
                CheckRunner.Run(() =>
                {
                    //Start execution
                    if (IsNoColor)
                    {
                        Beaprint.DeleteColors();
                    }
                    else
                    {
                        CheckRegANSI();
                    }

                    CheckLongPath();

                    Beaprint.PrintInit();

                    CheckRunner.Run(() => CreateDynamicLists(isFileSearchEnabled), IsDebug);

                    RunChecks(isAllChecks, wait, IsNetworkScan);

                    SearchHelper.CleanLists();

                    Beaprint.PrintMarketingBanner();
                }, IsDebug, "Total time");

                if (IsDebug)
                {
                    MemoryHelper.DisplayMemoryStats();
                }
            }
            finally
            {
                Console.SetOut(oldOut);

                fileWriter?.Close();
                fileStream?.Close();
            }
        }

        private static bool IsNetworkTypeValid(string arg)
        {
            var parts = arg.Split('=');
            string networkType = string.Empty;  

            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[1]))
            {
                networkType = parts[1];

                // auto
                if (string.Equals(networkType, "auto", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }

                // netmask  e.g. 10.10.10.10/24
                else if (networkType.Contains("/"))
                {
                    var rangeParts = networkType.Split('/');

                    if (rangeParts.Length == 2 && int.TryParse(rangeParts[1], out int res) && res <= 32 && res >= 0)
                    {
                        return true;
                    }
                }
                // list of ip addresses
                else if (networkType.Contains(","))
                {
                    var ips = networkType.Split(',');

                    try
                    {
                        var validIpsCount = ips.ToList().ConvertAll<IPAddress>(IPAddress.Parse).Count();
                    }
                    catch (Exception)
                    {
                        return false;
                    }

                    return true;
                }
                // single ip
                else if (IPAddress.TryParse(networkType, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RunChecks(bool isAllChecks, bool wait, bool isNetworkScan)
        {
            for (int i = 0; i < _systemChecks.Count; i++)
            {
                var systemCheck = _systemChecks[i];

                if (_systemCheckSelectedKeysHashSet.Contains(systemCheck.Key) || isAllChecks)
                {
                    systemCheck.Check.PrintInfo(IsDebug);

                    if ((i < _systemCheckSelectedKeysHashSet.Count - 1) && wait)
                    {
                        WaitInput();
                    }
                }
            }

            if (isNetworkScan)
            {
                NetworkScanner scanner = new NetworkScanner(NetworkScanOptions, PortScannerPorts);
                scanner.Scan();
            }
        }

        private static void CreateDynamicLists(bool isFileSearchEnabled)
        {
            Beaprint.GrayPrint("   Creating Dynamic lists, this could take a while, please wait...");

            try
            {
                Beaprint.GrayPrint("   - Loading sensitive_files yaml definitions file...");
                YamlConfig = YamlConfigHelper.GetWindowsSearchConfig();
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting sensitive_files yaml info: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Loading regexes yaml definitions file...");
                RegexesYamlConfig = YamlConfigHelper.GetRegexesSearchConfig();
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting regexes yaml info: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Checking if domain...");
                CurrentAdDomainName = DomainHelper.IsDomainJoined();
                IsPartOfDomain = !string.IsNullOrEmpty(CurrentAdDomainName);
                IsCurrentUserLocal = CurrentAdDomainName != CurrentUserDomainName;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting AD info: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Getting Win32_UserAccount info...");

                // by default only enumerate local users
                SelectQuery query = new SelectQuery("Win32_UserAccount", "LocalAccount=true");
                if (IsDomainEnumeration)
                {
                    // include also domain users
                    query = new SelectQuery("Win32_UserAccount");
                }

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    Win32Users = searcher.Get();
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while getting Win32_UserAccount info: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Creating current user groups list...");
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                CurrentUserSiDs[identity.User.ToString()] = Environment.UserName;
                IdentityReferenceCollection currentSIDs = identity.Groups;
                foreach (IdentityReference group in identity.Groups)
                {
                    string gName = "";
                    try
                    {
                        gName = UserInfoHelper.SID2GroupName(group.ToString());
                    }
                    catch (Exception ex)
                    {
                        Beaprint.GrayPrint("Error obtaining current SIDs: " + ex);
                    }
                    CurrentUserSiDs[group.ToString()] = gName;
                }

            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating current user groups list: " + ex);
            }

            try
            {
                var domainString = IsDomainEnumeration ? "(local + domain)" : "(local only)";
                Beaprint.GrayPrint($"   - Creating active users list {domainString}...");
                _paintActiveUsers = string.Join("|", User.GetMachineUsers(true, false, false, false, false));
                PaintActiveUsersNoAdministrator = _paintActiveUsers.Replace("|Administrator", "").Replace("Administrator|", "").Replace("Administrator", "");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating active users list: " + ex);
            }

            try
            {
                Beaprint.GrayPrint("   - Creating disabled users list...");
                PaintDisabledUsers = string.Join("|", User.GetMachineUsers(false, true, false, false, false));
                PaintDisabledUsersNoAdministrator = PaintDisabledUsers.Replace("|Administrator", "").Replace("Administrator|", "").Replace("Administrator", "");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating disabled users list: " + ex);
            }

            //paint_lockoutUsers = string.Join("|", UserInfo.GetMachineUsers(false, false, true, false, false));

            try
            {
                Beaprint.GrayPrint("   - Admin users list...");
                PaintAdminUsers = string.Join("|", User.GetMachineUsers(false, false, false, true, false));
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating admin users groups list: " + ex);
            }

            //create AppLocker lists
            try
            {
                Beaprint.GrayPrint("   - Creating AppLocker bypass list...");
                AppLockerHelper.CreateLists();
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while creating AppLocker bypass list: " + ex);
            }

            //create the file lists
            // only if we are running all checks or systeminfo / fileanalysis
            Beaprint.GrayPrint("   - Creating files/directories list for search...");
            if (isFileSearchEnabled)
            {
                try
                {
                    SearchHelper.CreateSearchDirectoriesList();
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint("Error while creating directory list: " + ex);
                }
            }
            else
            {
                Beaprint.GrayPrint("        [skipped, file search is disabled]");
            }
        }

        private static void CheckRegANSI()
        {
            try
            {
                if (RegistryHelper.GetRegValue("HKCU", "CONSOLE", "VirtualTerminalLevel") == "" && RegistryHelper.GetRegValue("HKCU", "CONSOLE", "VirtualTerminalLevel") == "")
                    Console.WriteLine(@"ANSI color bit for Windows is not set. If you are executing this from a Windows terminal inside the host you should run 'REG ADD HKCU\Console /v VirtualTerminalLevel /t REG_DWORD /d 1' and then start a new CMD");
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while checking ansi color registry: " + ex);
            }
        }

        private static void CheckLongPath()
        {
            try
            {
                if (RegistryHelper.GetRegValue("HKLM", @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled") != "1")
                {
                    Console.WriteLine(@"Long paths are disabled, so the maximum length of a path supported is 260 chars (this may cause false negatives when looking for files). If you are admin, you can enable it with 'REG ADD HKLM\SYSTEM\CurrentControlSet\Control\FileSystem /v VirtualTerminalLevel /t REG_DWORD /d 1' and then start a new CMD");
                    IsLongPath = false;
                }
                else
                    IsLongPath = true;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint("Error while checking LongPathsEnabled registry: " + ex);
            }
        }

        private static void WaitInput()
        {
            Console.Write("\n -- Press a key to continue... ");
            Console.ReadLine();
        }
    }
}
