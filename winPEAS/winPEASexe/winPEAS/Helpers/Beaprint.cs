using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace winPEAS.Helpers
{
    internal static class Beaprint
    {
        public static string GRAY = "\x1b[1;37m";
        public static string DGRAY = "\x1b[1;90m";
        static string RED = "\x1b[1;31m";
        public static string LRED = "\x1b[1;31m";
        static string GREEN = "\x1b[1;32m";
        static string LGREEN = "\x1b[1;32m";
        public static string YELLOW = "\x1b[33m";
        static string LYELLOW = "\x1b[1;33m";
        static string BLUE = "\x1b[34m";
        public static string LBLUE = "\x1b[1;34m";
        static string MAGENTA = "\x1b[1;35m";
        //static string LMAGENTA = "\x1b[1;35m";
        static string CYAN = "\x1b[36m";
        static string LCYAN = "\x1b[1;36m";
        //static string REDYELLOW = "\x1b[31;103m";
        public static string NOCOLOR = "\x1b[0m";
        public static string ansi_color_bad = RED;
        public static string ansi_color_good = GREEN;
        public static string ansi_color_gray = GRAY;
        public static string ansi_color_yellow = YELLOW;
        public static string ansi_users_active = CYAN;
        public static string ansi_users_disabled = BLUE;
        public static string ansi_current_user = MAGENTA;

        private static string Advisory =
            "winpeas should be used for authorized penetration testing and/or educational purposes only." +
            "Any misuse of this software will not be the responsibility of the author or of any other collaborator. " +
            "Use it at your own devices and/or with the device owner's permission.";

        /////////////////////////////////
        /////////  PRINT THINGS /////////
        /////////////////////////////////
        public static void PrintBanner()
        {
            Console.WriteLine(BLUE + string.Format(@"     
               {0}((((((((((((((((((((((((((((((((
        {0}(((((((((((((((((((((((((((((((((((((((((((
      {0}(((((((((((((({2}**********/{1}##########{0}(((((((((((((   
    {0}(((((((((((({2}********************/{1}#######{0}(((((((((((
    {0}(((((((({2}******************{3}/@@@@@/{0}{2}****{1}######{0}((((((((((
    {0}(((((({2}********************{3}@@@@@@@@@@/{0}{2}***,{1}####{0}((((((((((
    {0}((((({2}********************{3}/@@@@@%@@@@/{0}{2}********{1}##{0}(((((((((
    {0}((({1}############{2}*********{3}/%@@@@@@@@@/{0}{2}************{0}((((((((
    {0}(({1}##################(/{2}******{3}/@@@@@/{0}{2}***************{0}((((((
    {0}(({1}#########################(/{2}**********************{0}(((((
    {0}(({1}##############################(/{2}*****************{0}(((((
    {0}(({1}###################################(/{2}************{0}(((((
    {0}(({1}#######################################({2}*********{0}(((((
    {0}(({1}#######(,.***.,(###################(..***.{2}*******{0}(((((
    {0}(({1}#######*(#####((##################((######/({2}*****{0}(((((
    {0}(({1}###################(/***********(##############({0})(((((
    {0}((({1}#####################/*******(################{0})((((((
    {0}(((({1}############################################{0})((((((
    {0}((((({1}##########################################{0})(((((((
    {0}(((((({1}########################################{0})(((((((
    {0}(((((((({1}####################################{0})((((((((
    {0}((((((((({1}#################################{0})(((((((((
        {0}(((((((((({1}##########################{0})(((((((((
              {0}((((((((((((((((((((((((((((((((((((((
                 {0}((((((((((((((((((((((((((((((", LGREEN, GREEN, BLUE, NOCOLOR) + NOCOLOR);

            Console.WriteLine();
            Console.WriteLine(LYELLOW + "ADVISORY: " + BLUE + Advisory);
            Console.WriteLine();
        }

        public static void PrintMarketingBanner()
        {
            // Twitter

            // Patreon link
            Console.WriteLine(GREEN + string.Format(@"
       /---------------------------------------------------------------------------------\
       |                             {1}Do you like PEASS?{0}                                  |
       |---------------------------------------------------------------------------------| 
       |         {3}Learn Cloud Hacking{0}       :     {2}training.hacktricks.wiki{0}                 |
       |         {3}Follow on Twitter{0}         :     {2}@hacktricks_live{0}                        |
       |         {3}Respect on HTB{0}            :     {2}SirBroccoli            {0}                 |
       |---------------------------------------------------------------------------------|
       |                                 {1}Thank you!{0}                                      |
       \---------------------------------------------------------------------------------/
", GREEN, BLUE, RED, YELLOW) + NOCOLOR);

        }

        public static void PrintInit()
        {
            if (Checks.Checks.Banner)
            {
                PrintBanner();
            }

            Console.WriteLine(YELLOW + "  WinPEAS-ng" + NOCOLOR + YELLOW + " by @hacktricks_live" + NOCOLOR);

            PrintMarketingBanner();

            PrintLegend();
            Console.WriteLine();
            Console.WriteLine(BLUE + " You can find a Windows local PE Checklist here: " + YELLOW + "https://book.hacktricks.wiki/en/windows-hardening/checklist-windows-privilege-escalation.html");
        }

        static void PrintLegend()
        {
            Console.WriteLine(YELLOW + "  [+] " + GREEN + "Legend:" + NOCOLOR);
            Console.WriteLine(RED + "         Red" + GRAY + "                Indicates a special privilege over an object or something is misconfigured" + NOCOLOR);
            Console.WriteLine(GREEN + "         Green" + GRAY + "              Indicates that some protection is enabled or something is well configured" + NOCOLOR);
            Console.WriteLine(CYAN + "         Cyan" + GRAY + "               Indicates active users" + NOCOLOR);
            Console.WriteLine(BLUE + "         Blue" + GRAY + "               Indicates disabled users" + NOCOLOR);
            Console.WriteLine(LYELLOW + "         LightYellow" + GRAY + "        Indicates links" + NOCOLOR);

        }

        public static void PrintUsage()
        {
            Console.WriteLine(YELLOW + "  [*] " + GREEN + "WinPEAS is a binary to enumerate possible paths to escalate privileges locally. By default it'll run all the following checks unless otherwise specified, but you could also indicate as arguments the names of the checks to run if you only want to run a few of them." + NOCOLOR);
            Console.WriteLine(LCYAN + "        domain" + GRAY + "               Enumerate domain information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        systeminfo" + GRAY + "           Search system information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        eventsinfo" + GRAY + "           Display interesting events information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        userinfo" + GRAY + "             Search user information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        processinfo" + GRAY + "          Search processes information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        servicesinfo" + GRAY + "         Search services information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        applicationsinfo" + GRAY + "     Search installed applications information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        networkinfo" + GRAY + "          Search network information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        cloudinfo" + GRAY + "            Enumerate cloud information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        windowscreds" + GRAY + "         Search windows credentials" + NOCOLOR);
            Console.WriteLine(LCYAN + "        browserinfo" + GRAY + "          Search browser information" + NOCOLOR);
            Console.WriteLine(LCYAN + "        filesinfo" + GRAY + "            Search generic files that can contains credentials" + NOCOLOR);
            Console.WriteLine(LCYAN + "        fileanalysis" + GRAY + "         [NOT RUN BY DEFAULT] Search specific files that can contains credentials and for regexes inside files. Might take several minutes." + NOCOLOR);
            Console.WriteLine(LCYAN + "        all" + GRAY + "                  Run all checks the previous check including fileanalysis." + NOCOLOR);
            
            Console.WriteLine();
            Console.WriteLine(LCYAN + "        quiet" + GRAY + "                Do not print banner" + NOCOLOR);
            Console.WriteLine(LCYAN + "        notcolor" + GRAY + "             Don't use ansi colors (all white)" + NOCOLOR);
            Console.WriteLine(LCYAN + "        searchpf" + GRAY + "             Search credentials via regex also in Program Files folders" + NOCOLOR);
            Console.WriteLine(LCYAN + "        wait" + GRAY + "                 Wait for user input between checks" + NOCOLOR);
            Console.WriteLine(LCYAN + "        debug" + GRAY + "                Display debugging information - memory usage, method execution time" + NOCOLOR);
            Console.WriteLine(LCYAN + "        log[=logfile]" + GRAY + $"        Log all output to file defined as logfile, or to \"{Checks.Checks.DefaultLogFile}\" if not specified" + NOCOLOR);
            Console.WriteLine(LCYAN + "        max-regex-file-size=1000000" + GRAY + $"        Max file size (in Bytes) to search regex in. Default: {Checks.Checks.MaxRegexFileSize}B" + NOCOLOR);

            Console.WriteLine();
            Console.WriteLine(GREEN + "        Additional checks (slower):");
            Console.WriteLine(LCYAN + "        -lolbas" + GRAY + $"              Run additional LOLBAS check" + NOCOLOR);
            Console.WriteLine(LCYAN + "        -linpeas=[url]" + GRAY + $"       Run additional linpeas.sh check for default WSL distribution, optionally provide custom linpeas.sh URL\n" +
                                     $"                             (default: {Checks.Checks.LinpeasUrl})" + NOCOLOR);
            Console.WriteLine(LCYAN + "        -network|-ports" + GRAY + $"      Run additional network scanning - find network interfaces, hosts and scan nmap top 1000 TCP ports for each host found\n" +
                                     $"                             -network=\"auto\"                          -    find interfaces/hosts automatically"  + NOCOLOR + "\n" +
                                     $"                             -network=\"10.10.10.10,10.10.10.20\"       -    scan only selected ip address(es)" + NOCOLOR + "\n" +
                                     $"                             -network=\"10.10.10.10/24\"                -    scan host based on ip address/netmask" + NOCOLOR + "\n" +
                                     $"                             -ports=\"80,443,8080\"                     -    If a list of ports is provided, use this list instead of the nmap top 1000 TCP" + NOCOLOR);

        }


        /////////////////////////////////
        /// DIFFERENT PRINT FUNCTIONS ///
        /////////////////////////////////
        public static void GreatPrint(string toPrint)
        {
            // print_title

            Console.WriteLine();
            Console.WriteLine();
            int halfTotal = 60;
            //Console.WriteLine(LCYAN + "  " + new String('=', halfTotal - toPrint.Length) + "(" + NOCOLOR + YELLOW + toPrint + LCYAN + ")" + new String('=', halfTotal - toPrint.Length) + NOCOLOR);

            Console.WriteLine($"{LCYAN}════════════════════════════════════╣ {GREEN}{toPrint}{LCYAN} ╠════════════════════════════════════{NOCOLOR}");
        }

        public static void MainPrint(string toPrint)
        {
            // print_2title

            Console.WriteLine();
            //Console.WriteLine(YELLOW + "  [+] " + GREEN + toPrint + NOCOLOR);
            Console.WriteLine($"{LCYAN}╔══════════╣ {GREEN}{toPrint}{NOCOLOR}");
        }

        public static void LinkPrint(string link, string comment = "")
        {
            // print_info
            //Console.WriteLine(YELLOW + "   [?] " + LBLUE + comment + " " + LYELLOW + link + NOCOLOR);            
            Console.WriteLine($"{LCYAN}╚ {LBLUE}{comment} {LYELLOW}{link}{NOCOLOR}");
        }

        public static void InfoPrint(string toPrint)
        {
            // print_info
            //Console.WriteLine(YELLOW + "    [i] " + LBLUE + toPrint + NOCOLOR);
            Console.WriteLine($"{LCYAN}╚ {LBLUE}{toPrint}{NOCOLOR}");
        }

        public static void NotFoundPrint()
        {
            GrayPrint("    Not Found");
        }

        public static void GoodPrint(string to_print)
        {
            Console.WriteLine(GREEN + to_print + NOCOLOR);
        }

        public static void BadPrint(string to_print)
        {
            Console.WriteLine(RED + to_print + NOCOLOR);
        }

        public static void ColorPrint(string to_print, string color)
        {
            Console.WriteLine(color + to_print + NOCOLOR);
        }

        public static void GrayPrint(string to_print)
        {
            Console.WriteLine(DGRAY + to_print + NOCOLOR);
        }

        public static void LongPathWarning(string path)
        {
            if (!Checks.Checks.WarningIsLongPath)
            {
                GrayPrint($"The path {path} is too large, try to enable LongPaths in the registry (no more warning about this will be shown)");
                Checks.Checks.WarningIsLongPath = true;
            }
        }

        internal static void PrintDebugLine(string log)
        {
            Console.WriteLine(DGRAY + "  [Debug]  " + log + NOCOLOR);
            Console.WriteLine();
        }

        public static void PrintLineSeparator()
        {
            GrayPrint("   =================================================================================================");
            Console.WriteLine();
        }

        public static void PrintException(string message)
        {
            GrayPrint($"  [X] Exception: {message}");
        }

        public static void PrintNoNL(string message)
        {
            Console.Write(message);
        }

        public static void AnsiPrint(string to_print, Dictionary<string, string> ansi_colors_regexp)
        {
            if (to_print.Trim().Length > 0)
            {
                foreach (string line in to_print.Split('\n'))
                {
                    string new_line = line;
                    foreach (KeyValuePair<string, string> color in ansi_colors_regexp)
                    {
                        new_line = Regexansi(new_line, color.Value, color.Key);
                    }

                    Console.WriteLine(new_line);
                }
            }
        }

        internal static void NoColorPrint(string message)
        {
            AnsiPrint(message, new Dictionary<string, string>());
        }

        static string Regexansi(string to_match, string color, string rgxp)
        {
            if (to_match.Length == 0 || color.Length == 0 || rgxp.Length == 0)
                return to_match;

            Regex regex = new Regex(rgxp);
            foreach (Match match in regex.Matches(to_match))
            {
                if (match.Value.Length > 0)
                    to_match = to_match.Replace(match.Value, NOCOLOR + color + match.Value + NOCOLOR);
            }
            return to_match;
        }
        public static void DictPrint(Dictionary<string, string> dicprint, Dictionary<string, string> ansi_colors_regexp, bool delete_nulls, bool no_gray = false)
        {
            foreach (KeyValuePair<string, string> entry in dicprint)
            {
                if (delete_nulls && (entry.Value == null || string.IsNullOrEmpty(entry.Value.Trim())))
                {
                    continue;
                }

                string value = entry.Value;
                string key = entry.Key;
                string line;
                if (!no_gray)
                {
                    line = ansi_color_gray + "    " + key + ": " + NOCOLOR + value;
                }
                else
                {
                    line = "    " + key + ": " + value;
                }

                foreach (KeyValuePair<string, string> color in ansi_colors_regexp)
                {
                    line = Regexansi(line, color.Value, color.Key);
                }

                Console.WriteLine(line);
            }
        }

        public static void DictPrint(Dictionary<string, string> dicprint, bool delete_nulls)
        {
            if (dicprint.Count > 0)
            {
                foreach (KeyValuePair<string, string> entry in dicprint)
                {
                    if (delete_nulls && string.IsNullOrEmpty(entry.Value))
                    {
                        continue;
                    }
                    Console.WriteLine(ansi_color_gray + "    " + entry.Key + ": " + NOCOLOR + entry.Value);
                }
            }
            else
            {
                NotFoundPrint();
            }
        }

        public static void DictPrint(List<Dictionary<string, string>> listdicprint, bool delete_nulls)
        {
            if (listdicprint.Count > 0)
            {
                foreach (Dictionary<string, string> dicprint in listdicprint)
                {
                    DictPrint(dicprint, delete_nulls);
                    PrintLineSeparator();
                }
            }
            else
            {
                NotFoundPrint();
            }
        }

        public static void DictPrint(Dictionary<string, object> dicprint, bool delete_nulls)
        {

            if (dicprint != null)
            {
                Dictionary<string, string> results = new Dictionary<string, string>();
                foreach (KeyValuePair<string, object> entry in dicprint)
                {
                    results[entry.Key] = string.Format("{0}", entry.Value);
                }

                DictPrint(results, delete_nulls);
            }
            else
            {
                NotFoundPrint();
            }
        }

        public static void DictPrint(List<Dictionary<string, string>> listdicprint, Dictionary<string, string> colors, bool delete_nulls, bool no_gray = false)
        {
            if (listdicprint.Count > 0)
            {
                foreach (Dictionary<string, string> dicprint in listdicprint)
                {
                    DictPrint(dicprint, colors, delete_nulls, no_gray);
                    PrintLineSeparator();
                }
            }
            else
            {
                NotFoundPrint();
            }
        }

        public static void ListPrint(List<string> list_to_print)
        {
            if (list_to_print.Count > 0)
            {
                foreach (string elem in list_to_print)
                {
                    Console.WriteLine("    " + elem);
                    // printf ${BLUE}"═╣ "$GREEN"$1"$NC #There is 1 "═"
                }
            }
            else
            {
                NotFoundPrint();
            }
        }

        public static void ListPrint(List<string> list_to_print, Dictionary<string, string> dic_colors)
        {
            if (list_to_print.Count > 0)
            {
                foreach (string elem in list_to_print)
                {
                    AnsiPrint("    " + elem, dic_colors);
                }
            }
            else
            {
                NotFoundPrint();
            }
        }


        //////////////////////////////////
        /// Delete Colors (nocolor) :( ///
        /// //////////////////////////////
        public static void DeleteColors()
        {
            GRAY = "";
            RED = "";
            LRED = "";
            GREEN = "";
            LGREEN = "";
            YELLOW = "";
            LYELLOW = "";
            BLUE = "";
            LBLUE = "";
            MAGENTA = "";
            //LMAGENTA = "";
            CYAN = "";
            LCYAN = "";
            //REDYELLOW = "";
            NOCOLOR = "";
            ansi_color_bad = "";
            ansi_color_good = "";
            ansi_color_gray = "";
            ansi_color_yellow = "";
            ansi_users_active = "";
            ansi_users_disabled = "";
            ansi_current_user = "";
        }
    }
}
