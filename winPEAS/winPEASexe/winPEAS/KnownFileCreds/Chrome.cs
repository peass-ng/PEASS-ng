using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace winPEAS.KnownFileCreds
{
    static class Chrome
    {
        public static Dictionary<string, string> GetChromeDbs()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            // checks if Chrome has a history database
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", dir);
                            if (System.IO.File.Exists(userChromeCookiesPath))
                                results["userChromeCookiesPath"] = userChromeCookiesPath;

                            string userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", dir);
                            if (System.IO.File.Exists(userChromeLoginDataPath))
                                results["userChromeLoginDataPath"] = userChromeLoginDataPath;
                        }
                    }
                }
                else
                {
                    string userChromeCookiesPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Cookies", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(userChromeCookiesPath))
                        results["userChromeCookiesPath"] = userChromeCookiesPath;

                    string userChromeLoginDataPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (System.IO.File.Exists(userChromeLoginDataPath))
                        results["userChromeLoginDataPath"] = userChromeLoginDataPath;
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<string> ParseChromeHistory(string path)
        {
            List<string> results = new List<string>();

            // parses a Chrome history file via regex
            if (System.IO.File.Exists(path))
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
                catch (System.IO.IOException exception)
                {
                    Console.WriteLine("\r\n    [x] IO exception, history file likely in use (i.e. Browser is likely running): ", exception.Message);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
                }
            }
            return results;
        }

        public static Dictionary<string, List<string>> GetChromeHistBook()
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

                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", dir);
                            results["history"] = ParseChromeHistory(userChromeHistoryPath);

                            string userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", dir);
                            results["bookmarks"] = ParseChromeBookmarks(userChromeBookmarkPath);
                        }
                    }
                }
                else
                {
                    string userChromeHistoryPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History", System.Environment.GetEnvironmentVariable("USERPROFILE"));
                    results["history"] = ParseChromeHistory(userChromeHistoryPath);

                    string userChromeBookmarkPath = String.Format("{0}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks", System.Environment.GetEnvironmentVariable("USERPROFILE"));

                    results["bookmarks"] = ParseChromeBookmarks(userChromeBookmarkPath);
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        private static List<string> ParseChromeBookmarks(string path)
        {
            List<string> results = new List<string>();
            // parses a Chrome bookmarks
            if (System.IO.File.Exists(path))
            {
                try
                {
                    string contents = System.IO.File.ReadAllText(path);

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
                            results.Add(entry["url"].ToString().Trim());
                    }
                }
                catch (System.IO.IOException exception)
                {
                    Console.WriteLine("\r\n    [x] IO exception, Bookmarks file likely in use (i.e. Chrome is likely running).", exception.Message);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
                }
            }
            return results;
        }
    }
}
