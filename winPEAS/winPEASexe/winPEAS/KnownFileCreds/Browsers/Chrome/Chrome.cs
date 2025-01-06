using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using winPEAS.Checks;
using winPEAS.Helpers;

namespace winPEAS.KnownFileCreds.Browsers.Chrome
{
    internal class Chrome : ChromiumBase, IBrowser
    {
        public override string Name => "Chrome";

        public override string BaseAppDataPath => Path.Combine(AppDataPath, "..\\Local\\Google\\Chrome\\User Data\\Default\\");

        public override void PrintInfo()
        {
            PrintSavedCredentials();
            PrintDBsChrome();
            PrintHistBookChrome();
        }

        private static void PrintDBsChrome()
        {
            try
            {
                Beaprint.MainPrint("Looking for Chrome DBs");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#browsers-history");
                Dictionary<string, string> chromeDBs = GetChromeDbs();

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

                if ((!chromeDBs.ContainsKey("userChromeLoginDataPath")) &&
                    (!chromeDBs.ContainsKey("userChromeCookiesPath")))
                {
                    Beaprint.NotFoundPrint();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintHistBookChrome()
        {
            try
            {
                Beaprint.MainPrint("Looking for GET credentials in Chrome history");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#browsers-history");
                Dictionary<string, List<string>> chromeHistBook = GetChromeHistBook();
                List<string> history = chromeHistBook["history"];
                List<string> bookmarks = chromeHistBook["bookmarks"];

                if (history.Count > 0)
                {
                    Dictionary<string, string> colorsB = new Dictionary<string, string>()
                    {
                        { Globals.PrintCredStrings, Beaprint.ansi_color_bad },
                    };

                    foreach (string url in history)
                    {
                        if (MyUtils.ContainsAnyRegex(url.ToUpper(), Browser.CredStringsRegex))
                        {
                            Beaprint.AnsiPrint("    " + url, colorsB);
                        }
                    }
                    Console.WriteLine();

                    int limit = 50;
                    Beaprint.MainPrint($"Chrome history -- limit {limit}\n");
                    Beaprint.ListPrint(history.Take(limit).ToList());
                }
                else
                {
                    Beaprint.NotFoundPrint();
                }

                Beaprint.MainPrint("Chrome bookmarks");
                Beaprint.ListPrint(bookmarks);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static Dictionary<string, string> GetChromeDbs()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            // checks if Chrome has a history database
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
                    var dirs = Directory.EnumerateDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];

                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userChromeCookiesPath =
                                $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies";
                            if (File.Exists(userChromeCookiesPath))
                            {
                                results["userChromeCookiesPath"] = userChromeCookiesPath;
                            }

                            string userChromeLoginDataPath =
                                $"{dir}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data";
                            if (File.Exists(userChromeLoginDataPath))
                            {
                                results["userChromeLoginDataPath"] = userChromeLoginDataPath;
                            }
                        }
                    }
                }
                else
                {
                    string userChromeCookiesPath =
                        $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies";
                    if (File.Exists(userChromeCookiesPath))
                    {
                        results["userChromeCookiesPath"] = userChromeCookiesPath;
                    }

                    string userChromeLoginDataPath =
                        $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data";
                    if (File.Exists(userChromeLoginDataPath))
                    {
                        results["userChromeLoginDataPath"] = userChromeLoginDataPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        private static List<string> ParseChromeHistory(string path)
        {
            List<string> results = new List<string>();

            // parses a Chrome history file via regex
            if (File.Exists(path))
            {
                Regex historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

                try
                {
                    using (StreamReader r = new StreamReader(path))
                    {
                        string line;
                        while ((line = r.ReadLine()) != null)
                        {
                            Match m = historyRegex.Match(line);
                            if (m.Success)
                            {
                                results.Add(m.Groups[0].ToString().Trim());
                            }
                        }
                    }
                }
                catch (IOException exception)
                {
                    Console.WriteLine("\r\n    [x] IO exception, history file likely in use (i.e. Browser is likely running): ", exception.Message);
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
            }
            return results;
        }

        private static Dictionary<string, List<string>> GetChromeHistBook()
        {
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>()
            {
                { "history", new List<string>() },
                { "bookarks", new List<string>() },
            };
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    Console.WriteLine("\r\n\r\n=== Chrome (All Users) ===");

                    string userFolder = string.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    var dirs = Directory.EnumerateDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userChromeHistoryPath = string.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", dir);
                            results["history"] = ParseChromeHistory(userChromeHistoryPath);

                            string userChromeBookmarkPath = string.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", dir);
                            results["bookmarks"] = ParseChromeBookmarks(userChromeBookmarkPath);
                        }
                    }
                }
                else
                {
                    string userChromeHistoryPath = string.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", Environment.GetEnvironmentVariable("USERPROFILE"));
                    results["history"] = ParseChromeHistory(userChromeHistoryPath);

                    string userChromeBookmarkPath = string.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", Environment.GetEnvironmentVariable("USERPROFILE"));

                    results["bookmarks"] = ParseChromeBookmarks(userChromeBookmarkPath);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return results;
        }

        private static List<string> ParseChromeBookmarks(string path)
        {
            List<string> results = new List<string>();
            // parses a Chrome bookmarks
            if (File.Exists(path))
            {
                try
                {
                    string contents = File.ReadAllText(path);

                    // reference: http://www.tomasvera.com/programming/using-javascriptserializer-to-parse-json-objects/
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    Dictionary<string, object> deserialized = json.Deserialize<Dictionary<string, object>>(contents);
                    Dictionary<string, object> roots = (Dictionary<string, object>)deserialized["roots"];
                    Dictionary<string, object> bookmark_bar = (Dictionary<string, object>)roots["bookmark_bar"];
                    System.Collections.ArrayList children = (System.Collections.ArrayList)bookmark_bar["children"];

                    foreach (Dictionary<string, object> entry in children)
                    {
                        //Console.WriteLine("      Name: {0}", entry["name"].ToString().Trim());
                        if (entry.ContainsKey("url"))
                        {
                            results.Add(entry["url"].ToString().Trim());
                        }
                    }
                }
                catch (IOException exception)
                {
                    Console.WriteLine("\r\n    [x] IO exception, Bookmarks file likely in use (i.e. Chrome is likely running).", exception.Message);
                }
                catch (Exception ex)
                {
                    Beaprint.PrintException(ex.Message);
                }
            }
            return results;
        }
    }
}
