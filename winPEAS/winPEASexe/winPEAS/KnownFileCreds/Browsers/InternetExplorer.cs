using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using winPEAS.Checks;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.KnownFileCreds.Browsers.Models;

namespace winPEAS.KnownFileCreds.Browsers
{
    internal class InternetExplorer : BrowserBase, IBrowser
    {
        public override string Name => "Internet Explorer (unsupported)";

        public override void PrintInfo()
        {
            PrintSavedCredentials();
            PrintCurrentIETabs();
            PrintHistFavIE();
        }

        private static void PrintCurrentIETabs()
        {
            try
            {
                Beaprint.MainPrint("Current IE tabs");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#browsers-history");
                List<string> urls = GetCurrentIETabs();

                Dictionary<string, string> colorsB = new Dictionary<string, string>()
                {
                    { Globals.PrintCredStrings, Beaprint.ansi_color_bad },
                };

                Beaprint.ListPrint(urls, colorsB);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static void PrintHistFavIE()
        {
            try
            {
                Beaprint.MainPrint("Looking for GET credentials in IE history");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#browsers-history");
                Dictionary<string, List<string>> ieHistoryBook = GetIEHistFav();
                List<string> history = ieHistoryBook["history"];
                List<string> favorites = ieHistoryBook["favorites"];

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
                    Beaprint.MainPrint($"IE history -- limit {limit}\n");
                    Beaprint.ListPrint(history.Take(limit).ToList());
                }
                else
                {
                    Beaprint.NotFoundPrint();
                }

                Beaprint.MainPrint("IE favorites");
                Beaprint.ListPrint(favorites);
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }

        private static Dictionary<string, List<string>> GetIEHistFav()
        {
            int lastDays = 90;
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>()
            {
                { "history", new List<string>() },
                { "favorites", new List<string>() },
            };

            DateTime startTime = DateTime.Now.AddDays(-lastDays);

            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string[] SIDs = Registry.Users.GetSubKeyNames();
                    foreach (string SID in SIDs)
                    {
                        if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                        {
                            Dictionary<string, object> settings = RegistryHelper.GetRegValues("HKU", string.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs", SID));
                            if ((settings != null) && (settings.Count > 1))
                            {
                                foreach (KeyValuePair<string, object> kvp in settings)
                                {
                                    byte[] timeBytes = RegistryHelper.GetRegValueBytes("HKU", string.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime", SID), kvp.Key.ToString().Trim());
                                    if (timeBytes != null)
                                    {
                                        long timeLong = (long)(BitConverter.ToInt64(timeBytes, 0));
                                        DateTime urlTime = DateTime.FromFileTime(timeLong);
                                        if (urlTime > startTime)
                                        {
                                            results["history"].Add(kvp.Value.ToString().Trim());
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string userFolder = string.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    var dirs = Directory.EnumerateDirectories(userFolder);
                    foreach (var dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userIEBookmarkPath = string.Format("{0}\\Favorites\\", dir);

                            if (Directory.Exists(userIEBookmarkPath))
                            {
                                string[] bookmarkPaths = Directory.EnumerateFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories).ToArray();
                                if (bookmarkPaths.Length != 0)
                                {
                                    foreach (string bookmarkPath in bookmarkPaths)
                                    {
                                        using (StreamReader rdr = new StreamReader(bookmarkPath))
                                        {
                                            string line;
                                            string url = "";
                                            while ((line = rdr.ReadLine()) != null)
                                            {
                                                if (line.StartsWith("URL=", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    if (line.Length > 4)
                                                        url = line.Substring(4);
                                                    break;
                                                }
                                            }
                                            results["history"].Add(url.ToString().Trim());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Dictionary<string, object> settings = RegistryHelper.GetRegValues("HKCU", "SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs");
                    if ((settings != null) && (settings.Count != 0))
                    {
                        foreach (KeyValuePair<string, object> kvp in settings)
                        {
                            results["history"].Add(kvp.Value.ToString().Trim());
                        }
                    }

                    string userIEBookmarkPath = string.Format("{0}\\Favorites\\", Environment.GetEnvironmentVariable("USERPROFILE"));
                    if (Directory.Exists(userIEBookmarkPath))
                    {
                        string[] bookmarkPaths = Directory.EnumerateFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories).ToArray();
                        foreach (string bookmarkPath in bookmarkPaths)
                        {
                            using (StreamReader rdr = new StreamReader(bookmarkPath))
                            {
                                string line;
                                string url = "";
                                while ((line = rdr.ReadLine()) != null)
                                {
                                    if (line.StartsWith("URL=", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (line.Length > 4)
                                            url = line.Substring(4);
                                        break;
                                    }
                                }
                                results["favorites"].Add(url.ToString().Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }
        private static List<string> GetCurrentIETabs()
        {
            List<string> results = new List<string>();
            // Lists currently open Internet Explorer tabs, via COM
            // Notes:
            //  https://searchcode.com/codesearch/view/9859954/
            //  https://gist.github.com/yizhang82/a1268d3ea7295a8a1496e01d60ada816

            try
            {
                // Shell.Application COM GUID
                Type shell = Type.GetTypeFromCLSID(new Guid("13709620-C279-11CE-A49E-444553540000"));

                // actually instantiate the Shell.Application COM object
                Object shellObj = Activator.CreateInstance(shell);

                // grab all the current windows
                Object windows = shellObj.GetType().InvokeMember("Windows", BindingFlags.InvokeMethod, null, shellObj, null);

                // grab the open tab count
                Object openTabs = windows.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, windows, null);
                int openTabsCount = Int32.Parse(openTabs.ToString());

                for (int i = 0; i < openTabsCount; i++)
                {
                    // grab the acutal tab
                    Object item = windows.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, windows, new object[] { i });
                    try
                    {
                        // extract the tab properties
                        Object locationName = item.GetType().InvokeMember("LocationName", BindingFlags.GetProperty, null, item, null);
                        Object locationURL = item.GetType().InvokeMember("LocationUrl", BindingFlags.GetProperty, null, item, null);

                        // ensure we have a site address
                        if (Regex.IsMatch(locationURL.ToString(), @"(^https?://.+)|(^ftp://)"))
                        {
                            results.Add(string.Format("{0}", locationURL));
                        }
                        Marshal.ReleaseComObject(item);
                        item = null;
                    }
                    catch
                    {
                        //
                    }
                }
                Marshal.ReleaseComObject(windows);
                windows = null;
                Marshal.ReleaseComObject(shellObj);
                shellObj = null;
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }

        public override IEnumerable<CredentialModel> GetSavedCredentials()
        {
            // unsupported
            var result = new List<CredentialModel>();
            return result;
        }
    }
}
