using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace winPEAS
{
    class Beaprint
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
        static string MAGENTA = "\x1b[1:35m";
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


        /////////////////////////////////
        /////////  PRINT THINGS /////////
        /////////////////////////////////
        public static void PrintBanner()
        {
                System.Console.WriteLine(BLUE + String.Format(@"     
             {0}*((,.,/((((((((((((((((((((/,  */               
      {0},/*,..*((((((((((((((((((((((((((((((((((,           
    {0},*/((((((((((((((((((/,  .*//((//**, .*(((((((*       
    {0}(((((((((((((((({2}**********/{1}########## {0}.(* ,(((((((   
    {0}(((((((((((/{2}********************/{1}####### {0}.(. (((((((
    {0}((((((..{2}******************{3}/@@@@@/{2}***/{1}###### {0}./(((((((
    {0},,....{2}********************{3}@@@@@@@@@@{2}(***,{1}#### {0}.//((((((
    {0}, ,..{2}********************{3}/@@@@@%@@@@{2}/********{1}##{0}((/ /((((
    {0}..(({1}###########{2}*********{3}/%@@@@@@@@@{2}/************{0},,..((((
    {0}.({1}##################(/{2}******{3}/@@@@@{2}/***************{0}.. /((
    {0}.({1}#########################(/{2}**********************{0}..*((
    {0}.({1}##############################(/{2}*****************{0}.,(((
    {0}.({1}###################################(/{2}************{0}..(((
    {0}.({1}#######################################({2}*********{0}..(((
    {0}.({1}#######(,.***.,(###################(..***.{2}*******{0}..(((
    {0}.({1}#######*(#####((##################((######/({2}*****{0}..(((
    {0}.({1}###################(/***********(##############({0}...(((
    {0}.(({1}#####################/*******(################{0}.((((((
    {0}.((({1}############################################{0}(..((((
    {0}..((({1}##########################################{0}(..(((((
    {0}....(({1}########################################{0}( .(((((
    {0}......(({1}####################################{0}( .((((((
    {0}((((((((({1}#################################{0}(../((((((
        {0}(((((((((/{1}##########################{0}(/..((((((
              {0}(((((((((/,.  ,*//////*,. ./(((((((((((((((.
                 {0}(((((((((((((((((((((((((((((/", LGREEN, GREEN, BLUE, NOCOLOR) + NOCOLOR);

                System.Console.WriteLine();
                System.Console.WriteLine(LYELLOW + "ADVISORY: " + BLUE + Program.advisory);
                System.Console.WriteLine();
                Thread.Sleep(700);
        }

        private Beaprint(){}

        public static void PrintInit()
        {
            if (Program.banner)
                PrintBanner();

            System.Console.WriteLine(YELLOW + "  WinPEAS " + GREEN + Program.version + NOCOLOR + YELLOW + " by carlospolop" + NOCOLOR);
            System.Console.WriteLine();
            PrintLegend();
            System.Console.WriteLine();
            LinkPrint("https://book.hacktricks.xyz/windows/checklist-windows-privilege-escalation", "You can find a Windows local PE Checklist here:");

        }

        static void PrintLegend()
        {
            System.Console.WriteLine(YELLOW + "  [+] " + GREEN + "Legend:" + NOCOLOR);
            System.Console.WriteLine(RED + "         Red" + GRAY + "                Indicates a special privilege over an object or something is misconfigured" + NOCOLOR);
            System.Console.WriteLine(GREEN + "         Green" + GRAY + "              Indicates that some protection is enabled or something is well configured" + NOCOLOR);
            System.Console.WriteLine(CYAN + "         Cyan" + GRAY + "               Indicates active users" + NOCOLOR);
            System.Console.WriteLine(BLUE + "         Blue" + GRAY + "               Indicates disabled users" + NOCOLOR);
            System.Console.WriteLine(LYELLOW + "         LightYellow" + GRAY + "        Indicates links" + NOCOLOR);

        }

        public static void PrintUsage()
        {
            System.Console.WriteLine(YELLOW + "  [*] " + GREEN + "WinPEAS is a binary to enumerate possible paths to escalate privileges locally" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        quiet" + GRAY + "             Do not print banner" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        searchslow" + GRAY + "        Sleep while searching files to not consume a notable amount of resources" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        searchall" + GRAY + "         Search all known filenames whith possible credentials (could take some mins)" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        cmd" + GRAY + "               Obtain wifi, cred manager and clipboard information executing CMD commands" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        notcolor" + GRAY + "          Don't use ansi colors (all white)" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        systeminfo" + GRAY + "        Search system information" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        userinfo" + GRAY + "          Search user information" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        procesinfo" + GRAY + "        Search processes information" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        servicesinfo" + GRAY + "      Search services information" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        applicationsinfo" + GRAY + "  Search installed applications information" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        networkinfo" + GRAY + "       Search network information" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        windowscreds" + GRAY + "      Search windows credentials" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        browserinfo" + GRAY + "       Search browser information" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        filesinfo" + GRAY + "         Search files that can contains credentials" + NOCOLOR);
            System.Console.WriteLine(LBLUE + "        wait" + GRAY + "              Wait for user input between checks" + NOCOLOR);
            System.Console.WriteLine(YELLOW + "        [+] " + LYELLOW + "By default all checks (except CMD checks) are executed" + NOCOLOR);
        }


        /////////////////////////////////
        /// DIFFERENT PRINT FUNCTIONS ///
        /////////////////////////////////
        public static void GreatPrint(string toPrint)
        {

            System.Console.WriteLine();
            System.Console.WriteLine();
            int halfTotal = 60;
            System.Console.WriteLine(LCYAN + "  " + new String('=', halfTotal - toPrint.Length) + "(" + NOCOLOR + YELLOW + toPrint + LCYAN + ")" + new String('=', halfTotal - toPrint.Length) + NOCOLOR);
        }

        public static void MainPrint(string toPrint)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(YELLOW + "  [+] " + GREEN + toPrint + NOCOLOR);
        }

        public static void LinkPrint(string link, string comment = "")
        {
            System.Console.WriteLine(YELLOW + "   [?] " + LBLUE + comment + " " + LYELLOW + link + NOCOLOR);
        }

        public static void InfoPrint(string toPrint)
        {
            System.Console.WriteLine(YELLOW + "    [i] " + LBLUE + toPrint + NOCOLOR);
        }

        public static void NotFoundPrint()
        {
            GrayPrint("    Not Found");
        }

        public static void GoodPrint(string to_print)
        {
            System.Console.WriteLine(GREEN + to_print + NOCOLOR);
        }

        public static void BadPrint(string to_print)
        {
            System.Console.WriteLine(RED + to_print + NOCOLOR);
        }

        public static void GrayPrint(string to_print)
        {
            System.Console.WriteLine(DGRAY + to_print + NOCOLOR);
        }

        public static void PrintLineSeparator()
        {
            GrayPrint("   =================================================================================================");
            System.Console.WriteLine();
        }
        public static void AnsiPrint(string to_print, Dictionary<string, string> ansi_colors_regexp)
        {
            if (to_print.Trim().Length > 0)
            {
                foreach (string line in to_print.Split('\n'))
                {
                    string new_line = line;
                    foreach (KeyValuePair<string, string> color in ansi_colors_regexp)
                        new_line = Regexansi(new_line, color.Value, color.Key);

                    System.Console.WriteLine(new_line);
                }
            }
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
                if (delete_nulls && String.IsNullOrEmpty(entry.Value.Trim()))
                    continue;
                string value = entry.Value;
                string key = entry.Key;
                string line = "";
                if (!no_gray)
                    line = ansi_color_gray + "    " + key + ": " + NOCOLOR + value;
                else
                    line = "    " + key + ": " + value;

                foreach (KeyValuePair<string, string> color in ansi_colors_regexp)
                    line = Regexansi(line, color.Value, color.Key);

                System.Console.WriteLine(line);
            }

        }
        public static void DictPrint(Dictionary<string, string> dicprint, bool delete_nulls)
        {
            if (dicprint.Count > 0)
            {
                foreach (KeyValuePair<string, string> entry in dicprint)
                {
                    if (delete_nulls && String.IsNullOrEmpty(entry.Value))
                        continue;
                    System.Console.WriteLine(ansi_color_gray + "    " + entry.Key + ": " + NOCOLOR + entry.Value);
                }
            }
            else
                NotFoundPrint();
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
                NotFoundPrint();
        }

        public static void DictPrint(Dictionary<string, object> dicprint, bool delete_nulls)
        {

            if (dicprint != null)
            {
                Dictionary<string, string> results = new Dictionary<string, string>();
                foreach (KeyValuePair<string, object> entry in dicprint)
                    results[entry.Key] = String.Format("{0}", entry.Value);
                DictPrint(results, delete_nulls);
            }
            else
                NotFoundPrint();

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
                NotFoundPrint();
        }

        public static void ListPrint(List<string> list_to_print)
        {
            if (list_to_print.Count > 0)
            {
                foreach (string elem in list_to_print)
                    System.Console.WriteLine("    " + elem);
            }
            else
                NotFoundPrint();
        }

        public static void ListPrint(List<string> list_to_print, Dictionary<string, string> dic_colors)
        {
            if (list_to_print.Count > 0)
            {
                foreach (string elem in list_to_print)
                    AnsiPrint("    " + elem, dic_colors);
            }
            else
                NotFoundPrint();
        }


        //////////////////////////////////
        /// Delete Colors (nocolor) :( ///
        /// //////////////////////////////
        public static void deleteColors()
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
