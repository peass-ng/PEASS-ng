using Colorful; // http://colorfulconsole.com/
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace winPEAS
{
    class Beaprint
    {
        public static string GRAY = "\x1b[1;30m";
        static string RED = "\x1b[31m";
        public static string LRED = "\x1b[1;31m";
        static string GREEN = "\x1b[32m";
        static string LGREEN = "\x1b[1;32m";
        public static string YELLOW = "\x1b[33m";
        static string LYELLOW = "\x1b[1;33m";
        static string BLUE = "\x1b[34m";
        public static string LBLUE = "\x1b[1;34m";
        static string MAGENTA = "\x1b[35m";
        static string LMAGENTA = "\x1b[1;35m";
        static string CYAN = "\x1b[36m";
        static string LCYAN = "\x1b[1;36m";
        static string REDYELLOW = "\x1b[31;103m";
        public static string NOCOLOR = "\x1b[0m";
        public static string ansii_color_bad = RED;
        public static string ansii_color_good = GREEN;
        public static string ansii_users_active = CYAN;
        public static string ansii_users_disabled = BLUE;
        public static string ansii_current_user = MAGENTA;

        public static Color color_key = Color.DarkSeaGreen;
        public static Color color_not_found = Color.Gray;
        public static Color color_default = Color.LightSkyBlue;
        public static Color color_good = Color.Green;
        public static Color color_bad = Color.Red;
        static List<Color> color_line = new List<Color>() {
            Color.SkyBlue,
            Color.LightPink,
            Color.MediumVioletRed,
            Color.Linen,
            Color.MediumTurquoise,
            Color.MediumPurple,
            Color.Tomato,
            Color.GreenYellow,
            Color.HotPink,
            Color.Peru
        };

        public static void PrintInit()
        {
            Colorful.Console.WriteLine();
            Colorful.Console.WriteLine();

            if (Program.using_ansii)
            {
                System.Console.WriteLine(YELLOW + "[+] " + NOCOLOR + "WinPEAS" + GREEN + Program.version + NOCOLOR );
            }
            else
            {
                Formatter[] colorsString = new Formatter[]
                        {
                        new Formatter( "  [+] ", Color.Yellow),
                        new Formatter( "WinPEASv", color_key),
                        new Formatter( Program.version, color_default),
                };

                Colorful.Console.WriteLineFormatted("{0}{1}{2}", color_key, colorsString);
            }
            LinkPrint("https://book.hacktricks.xyz/windows/checklist-windows-privilege-escalation", "You can find a Windows local PE Checklist here:");
            PrintLeyend();
        }

        static void PrintLeyend()
        {
            if (Program.using_ansii)
            {
                System.Console.WriteLine(YELLOW + "  [+] " + GREEN + "Leyend:" + NOCOLOR);
                System.Console.WriteLine(RED + "         Red" + GRAY + "                Indicates a special privilege over an object or something is misconfigured" + NOCOLOR);
                System.Console.WriteLine(GREEN + "         Green" + GRAY + "              Indicates that some protection is enabled or something is well configured" + NOCOLOR);
                System.Console.WriteLine(CYAN + "         Cyan" + GRAY + "               Indicates active users" + NOCOLOR);
                System.Console.WriteLine(BLUE + "         Blue" + GRAY + "               Indicates disabled users" + NOCOLOR);
                System.Console.WriteLine(LYELLOW + "         LightYellow" + GRAY + "        Indicates links" + NOCOLOR);
            }
            else
            {
                Colorful.Console.Write("  [+] ", Color.Yellow); Colorful.Console.WriteLine(" Leyend", color_good);
                Colorful.Console.Write("         Red", color_bad); Colorful.Console.WriteLine("        Indicates a special privilege over an object or something is misconfigured", Color.Gray);
                Colorful.Console.Write("         Green", color_good); Colorful.Console.WriteLine("      Indicates that some protection is enabled or something is well configured", Color.Gray);
                Colorful.Console.Write("         Pistachio", color_key); Colorful.Console.WriteLine("  Indicates static information (no host dependant), in general");
                Colorful.Console.Write("         Lightblue", color_default); Colorful.Console.WriteLine("  Indicates information extracted from the host (host dependant)");
                Colorful.Console.Write("         Magenta", Color.Magenta); Colorful.Console.WriteLine("    Indicates current user and domain");
                Colorful.Console.Write("         Cyan", Color.Cyan); Colorful.Console.WriteLine("       Indicates active users");
                Colorful.Console.Write("         Blue", Color.Blue); Colorful.Console.WriteLine("       Indicates locked users");
                Colorful.Console.Write("         Purple", Color.MediumPurple); Colorful.Console.WriteLine("     Indicates disablde users and links (and other info for coloring purposes)");
            }
        }

        public static void PrintUsage()
        {
            if (Program.using_ansii)
            {
                System.Console.WriteLine(YELLOW + "  [*] " + GREEN + "WinPEAS is a binary to enumerate possible paths to escalate privileges locally" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tansii" + GRAY + "             Use ANSII colors (see color from linux terminal)" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tfast" + GRAY + "              This will avoid very time consuming checks" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tcmd" + GRAY + "               Obtain wifi, cred manager and clipboard information executing CMD commands" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tsysteminfo" + GRAY + "        Search system information" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tuserinfo" + GRAY + "          Search user information" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tprocesinfo" + GRAY + "        Search processes information" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tservicesinfo" + GRAY + "      Search services information" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tapplicationsinfo" + GRAY + "  Search installed applications information" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tnetworkinfo" + GRAY + "       Search network information" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\twindowscreds" + GRAY + "      Search windows credentials" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tbrowserinfo" + GRAY + "       Search browser information" + NOCOLOR);
                System.Console.WriteLine(LBLUE + "\tfilesinfo" + GRAY + "         Search files that can contains credentials" + NOCOLOR);
                System.Console.WriteLine(YELLOW + "\t[+] " + LYELLOW + "By default all checks are executed" + NOCOLOR);
            }
            else
            {
                Colorful.Console.Write("  [*] ", Color.Yellow); Colorful.Console.WriteLine("WinPEAS is a binary to enumerate possible paths to escalate privileges locally", color_key);
                Colorful.Console.Write("\tansii", color_default); Colorful.Console.WriteLine("             Use ANSII colors (see color from linux terminal)", Color.Gray);
                Colorful.Console.Write("\tfast", color_default); Colorful.Console.WriteLine("              This will avoid very time consuming checks", Color.Gray);
                Colorful.Console.Write("\tcmd", color_default); Colorful.Console.WriteLine("               Obtain wifi, cred manager and clipboard information executing CMD commands", Color.Gray);
                Colorful.Console.Write("\tsysteminfo", color_default); Colorful.Console.WriteLine("        Search system information", Color.Gray);
                Colorful.Console.Write("\tuserinfo", color_default); Colorful.Console.WriteLine("          Search user information", Color.Gray);
                Colorful.Console.Write("\tprocesinfo", color_default); Colorful.Console.WriteLine("        Search processes information", Color.Gray);
                Colorful.Console.Write("\tservicesinfo", color_default); Colorful.Console.WriteLine("      Search services information", Color.Gray);
                Colorful.Console.Write("\tapplicationsinfo", color_default); Colorful.Console.WriteLine("  Search installed applications information", Color.Gray);
                Colorful.Console.Write("\tnetworkinfo", color_default); Colorful.Console.WriteLine("       Search network information", Color.Gray);
                Colorful.Console.Write("\twindowscreds", color_default); Colorful.Console.WriteLine("      Search windows credentials", Color.Gray);
                Colorful.Console.Write("\tbrowserinfo", color_default); Colorful.Console.WriteLine("       Search browser information", Color.Gray);
                Colorful.Console.Write("\tfilesinfo", color_default); Colorful.Console.WriteLine("         Search files that can contains credentials", Color.Gray);
                Colorful.Console.Write("\t[+] ", Color.Yellow); Colorful.Console.WriteLine(" By default all checks are executed", color_good);
            }
        }

        public static void GreatPrint(string toPrint)
        {
            try
            {
                System.Console.WriteLine();
                System.Console.WriteLine();
                int halfTotal = 60;
                if (Program.using_ansii)
                    System.Console.WriteLine(LCYAN + "  " + new String('=', halfTotal - toPrint.Length) + "(" +NOCOLOR +  YELLOW + toPrint + LCYAN + ")" + new String('=', halfTotal - toPrint.Length) + NOCOLOR);
                else
                {
                    StyleSheet styleSheet = new StyleSheet(Color.White);
                    styleSheet.AddStyle("[a-zA-Z]", Color.Yellow);
                    styleSheet.AddStyle("[=()]", Color.LightSkyBlue);
                    Colorful.Console.WriteLineStyled(new String('=', halfTotal - toPrint.Length) + "( " + toPrint + " )" + new String('=', halfTotal - toPrint.Length), styleSheet);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void MainPrint(string toPrint, string attackid)
        {
            try
            {
                Colorful.Console.WriteLine();
                if (Program.using_ansii)
                    System.Console.WriteLine(YELLOW + "  [+] " + LRED + toPrint + YELLOW + "(" + GRAY + attackid + YELLOW + ")" + NOCOLOR);
                else
                {
                    string iniPrint = "[+] {0} ({1})";
                    Formatter[] colors = new Formatter[]
                    {
                        new Formatter(toPrint, Color.Salmon),
                        new Formatter(attackid, Color.Gray),
                    };
                    Colorful.Console.WriteLineFormatted(iniPrint, Color.Yellow, colors);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void LinkPrint(string link, string comment = "")
        {
            try
            {
                if (Program.using_ansii)
                    System.Console.WriteLine(YELLOW + "   [?] " + LBLUE + comment + " " + LYELLOW + link + NOCOLOR);
                else
                {
                    Formatter[] colors = new Formatter[]
                    {
                         new Formatter("   [?] ", Color.Yellow),
                         new Formatter(link, Color.MediumPurple),
                    };
                    Colorful.Console.WriteLineFormatted("{0}" + comment + " {1}", color_default, colors);
                }
                Colorful.Console.WriteLine();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void InfoPrint(string toPrint)
        {
            try
            {
                if (Program.using_ansii)
                    System.Console.WriteLine(YELLOW + "    [i] " + LBLUE + toPrint + NOCOLOR);
                else
                {
                    string iniPrint = "    [i] {0}";
                    Formatter[] colorsString = new Formatter[]
                    {
                        new Formatter(toPrint, color_key),
                    };
                    Colorful.Console.WriteLineFormatted(iniPrint, Color.Yellow, colorsString);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void NotFoundPrint()
        {
            GrayPrint("    Not Found");
        }

        public static void GoodPrint(string to_print)
        {
            if (Program.using_ansii)
                System.Console.WriteLine(GREEN + to_print + NOCOLOR);
            else
                Colorful.Console.WriteLine(to_print, color_good);
        }

        public static void BadPrint(string to_print)
        {
            if (Program.using_ansii)
                System.Console.WriteLine(RED + to_print + NOCOLOR);
            else
                Colorful.Console.WriteLine(to_print, color_bad);
        }

        public static void GrayPrint(string to_print)
        {
            if (Program.using_ansii)
                System.Console.WriteLine(GRAY + to_print + NOCOLOR);
            else
                Colorful.Console.WriteLine(to_print, color_not_found);

        }

        public static void PrintLineSeparator()
        {
            GrayPrint("   =================================================================================================");
        }
        public static void AnsiiPrint(string to_print, Dictionary<string, string> ansii_colors_regexp)
        {
            if (to_print.Trim().Length > 0)
            {
                foreach (string line in to_print.Split('\n'))
                {
                    string new_line = line;
                    foreach (KeyValuePair<string, string> color in ansii_colors_regexp)
                        new_line = RegexAnsii(new_line, color.Value, color.Key);

                    System.Console.WriteLine(new_line);
                }
            }
        }

        static string RegexAnsii(string to_match, string color, string rgxp)
        {
            Regex regex = new Regex(rgxp);
            Match match = regex.Match(to_match);
            if (match.Success)
                return to_match.Replace(match.Value, color + match.Value + NOCOLOR);
            return to_match;
        }
        public static void DictPrint(Dictionary<string, string> dicprint, Dictionary<string, string> ansii_colors_regexp, bool delete_nulls)
        {
            try
            {
                foreach (KeyValuePair<string, string> entry in dicprint)
                {
                    if (delete_nulls && String.IsNullOrEmpty(entry.Value.Trim()))
                        continue;
                    string value = entry.Value;
                    string key = entry.Key;
                    foreach (KeyValuePair<string, string> color in ansii_colors_regexp)
                    {
                        key = RegexAnsii(key, color.Value, color.Key);
                        value = RegexAnsii(value, color.Value, color.Key);
                    }
                    System.Console.WriteLine("    " + key + ": " + value);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }
        public static void DictPrint(Dictionary<string, string> dicprint, bool delete_nulls)
        {
            try
            {
                if (dicprint.Count > 0)
                {
                    foreach (KeyValuePair<string, string> entry in dicprint)
                    {
                        if (delete_nulls && String.IsNullOrEmpty(entry.Value))
                            continue;
                        if (Program.using_ansii)
                            System.Console.WriteLine("    " + entry.Key + ": " + entry.Value);

                        else
                        {
                            Formatter[] colorsString = new Formatter[]
                            {
                                new Formatter(entry.Key, color_key),
                                new Formatter(entry.Value, color_default),
                            };
                            string formString = "    {0}" + new String(' ', (entry.Key.Length <= 30 ? 30 - entry.Key.Length : 0)) + ":  {1}";
                            Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
                        }
                    }
                }
                else
                    NotFoundPrint();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void DictPrint(Dictionary<string, string> dicprint, StyleSheet ssheet, bool delete_nulls)
        {
            try
            {
                if (dicprint.Count > 0)
                {
                    string formString = "    {0,-30}:  {1}";

                    foreach (KeyValuePair<string, string> entry in dicprint)
                    {
                        if (delete_nulls && String.IsNullOrEmpty(entry.Value))
                            continue;
                        //Check if a string is already painted (the stylesheet will contains red color first and dont want of overwrite that)
                        bool repeated = false;
                        foreach (StyleClass<TextPattern> sstext in ssheet.Styles)
                        {
                            string target = sstext.Target.Value;
                            if (Regex.Match(entry.Key, target).Success || Regex.Match(" " + entry.Key, target).Success)
                            {
                                repeated = true;
                                break;
                            }
                        }
                        if (!repeated)
                            ssheet.AddStyle(" " + entry.Key.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("]", "\\]").Replace("[", "\\[").Replace("?", "\\?"), color_key);

                        Colorful.Console.WriteLineStyled(String.Format(formString, entry.Key, entry.Value), ssheet);
                    }
                }
                else
                    NotFoundPrint();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void DictPrint(List<Dictionary<string, string>> listdicprint, bool delete_nulls)
        {
            try
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
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void DictPrint(Dictionary<string, object> dicprint, bool delete_nulls)
        {
            try
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
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void DictPrint(List<Dictionary<string, string>> listdicprint, StyleSheet ssheet, bool delete_nulls)
        {
            try
            {
                if (listdicprint.Count > 0)
                {
                    foreach (Dictionary<string, string> dicprint in listdicprint)
                    {
                        DictPrint(dicprint, ssheet, delete_nulls);
                        PrintLineSeparator();
                    }
                }
                else
                    NotFoundPrint();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void DictPrint(List<Dictionary<string, string>> listdicprint, Dictionary<string, string> colors, bool delete_nulls)
        {
            try
            {
                if (listdicprint.Count > 0)
                {
                    foreach (Dictionary<string, string> dicprint in listdicprint)
                    {
                        DictPrint(dicprint, colors, delete_nulls);
                        PrintLineSeparator();
                    }
                }
                else
                    NotFoundPrint();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void LinePrint(string formString, List<string> to_complete)
        {
            try
            {
                Formatter[] colorsString = new Formatter[to_complete.Count];
                for (int i = 0; i < to_complete.Count; i++)
                {
                    colorsString[i] = new Formatter(to_complete[i], color_line[i % color_line.Count]);
                }
                Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void ColumnPrint(string formString, List<string> to_complete)
        {
            try
            {
                string[] params_format = new string[to_complete.Count];
                for (int i = 0; i < to_complete.Count; i++)
                {
                    params_format[i] = "{" + i + "}";
                }
                formString = String.Format(formString, params_format);

                Formatter[] colorsString = new Formatter[to_complete.Count];
                for (int i = 0; i < to_complete.Count; i++)
                {
                    colorsString[i] = new Formatter(to_complete[i], color_line[i % color_line.Count]);
                }
                Colorful.Console.WriteLineFormatted(formString, color_key, colorsString);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void ListPrint(List<string> list_to_print)
        {
            try
            {
                if (list_to_print.Count > 0)
                {
                    if (Program.using_ansii)
                    {
                        foreach (string elem in list_to_print)
                            System.Console.WriteLine("    " + elem);
                    }
                    else
                    {
                        foreach (string elem in list_to_print)
                            Colorful.Console.WriteLine("    " + elem, color_default);
                    }
                }
                else
                    NotFoundPrint();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        public static void ListPrint(List<string> list_to_print, Dictionary<string, string> dic_colors)
        {
            if (list_to_print.Count > 0)
            {
                foreach (string elem in list_to_print)
                    AnsiiPrint("    " + elem, dic_colors);
            }
            else
                NotFoundPrint();
        }

        public static void ListPrint(List<string> list_to_print, StyleSheet ss)
        {
            try
            {
                if (list_to_print.Count > 0)
                {
                    foreach (string elem in list_to_print)
                        Colorful.Console.WriteLineStyled("    " + elem, ss);
                }
                else
                    NotFoundPrint();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }


    }
}
