using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using winPEAS.Utils;

namespace winPEAS.KnownFileCreds
{
    static class InternetExplorer
    {
        public static Dictionary<string, List<string>> GetIEHistFav()
        {
            int lastDays = 90;
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>()
            {
                { "history", new List<string>() },
                { "favorites", new List<string>() },
            };

            DateTime startTime = System.DateTime.Now.AddDays(-lastDays);

            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string[] SIDs = Registry.Users.GetSubKeyNames();
                    foreach (string SID in SIDs)
                    {
                        if (SID.StartsWith("S-1-5") && !SID.EndsWith("_Classes"))
                        {
                            Dictionary<string, object> settings = RegistryHelper.GetRegValues("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLs", SID));
                            if ((settings != null) && (settings.Count > 1))
                            {
                                foreach (KeyValuePair<string, object> kvp in settings)
                                {
                                    byte[] timeBytes = RegistryHelper.GetRegValueBytes("HKU", String.Format("{0}\\SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime", SID), kvp.Key.ToString().Trim());
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

                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userIEBookmarkPath = String.Format("{0}\\Favorites\\", dir);

                            if (Directory.Exists(userIEBookmarkPath))
                            {
                                string[] bookmarkPaths = Directory.GetFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories);
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
                            byte[] timeBytes = RegistryHelper.GetRegValueBytes("HKCU", "SOFTWARE\\Microsoft\\Internet Explorer\\TypedURLsTime", kvp.Key.ToString().Trim());
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

                    string userIEBookmarkPath = String.Format("{0}\\Favorites\\", System.Environment.GetEnvironmentVariable("USERPROFILE"));

                    string[] bookmarkPaths = Directory.GetFiles(userIEBookmarkPath, "*.url", SearchOption.AllDirectories);

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
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }


        public static List<string> GetCurrentIETabs()
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
                            results.Add(String.Format("{0}", locationURL));
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex));
            }
            return results;
        }
    }
}
