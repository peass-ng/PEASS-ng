using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using winPEAS.Checks;
using winPEAS.Helpers;

namespace winPEAS.KnownFileCreds.Browsers
{
    internal class Firefox : IBrowser
    {
        public void PrintInfo()
        {
            PrintDBsFirefox();
            PrintHistFirefox();
        }

        private static void PrintDBsFirefox()
        {
            try
            {
                Beaprint.MainPrint("Looking for Firefox DBs");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                List<string> firefoxDBs = Firefox.GetFirefoxDbs();
                if (firefoxDBs.Count > 0)
                {
                    foreach (string firefoxDB in firefoxDBs) //No Beaprints because line needs red
                    {
                        Beaprint.BadPrint("    Firefox credentials file exists at " + firefoxDB);
                    }

                    Beaprint.InfoPrint("Run SharpWeb (https://github.com/djhohnstein/SharpWeb)");
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

        private static void PrintHistFirefox()
        {
            try
            {
                Beaprint.MainPrint("Looking for GET credentials in Firefox history");
                Beaprint.LinkPrint("https://book.hacktricks.xyz/windows/windows-local-privilege-escalation#browsers-history");
                List<string> firefoxHist = Firefox.GetFirefoxHistory();
                if (firefoxHist.Count > 0)
                {
                    Dictionary<string, string> colorsB = new Dictionary<string, string>()
                    {
                        { Globals.PrintCredStrings, Beaprint.ansi_color_bad },
                    };

                    foreach (string url in firefoxHist)
                    {
                        if (MyUtils.ContainsAnyRegex(url.ToUpper(), Browser.CredStringsRegex))
                        {
                            Beaprint.AnsiPrint("    " + url, colorsB);
                        }
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

        private static List<string> GetFirefoxDbs()
        {
            List<string> results = new List<string>();
            // checks if Firefox has a history database
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];

                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userFirefoxBasePath = $"{dir}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\";
                            if (System.IO.Directory.Exists(userFirefoxBasePath))
                            {
                                string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                                foreach (string directory in directories)
                                {
                                    string firefoxCredentialFile3 = $"{directory}\\{"key3.db"}";
                                    if (File.Exists(firefoxCredentialFile3))
                                    {
                                        results.Add(firefoxCredentialFile3);
                                    }

                                    string firefoxCredentialFile4 = $"{directory}\\{"key4.db"}";
                                    if (File.Exists(firefoxCredentialFile4))
                                    {
                                        results.Add(firefoxCredentialFile3);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    string userFirefoxBasePath =
                        $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\";

                    if (Directory.Exists(userFirefoxBasePath))
                    {
                        string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                        foreach (string directory in directories)
                        {
                            string firefoxCredentialFile3 = $"{directory}\\{"key3.db"}";
                            if (File.Exists(firefoxCredentialFile3))
                            {
                                results.Add(firefoxCredentialFile3);
                            }

                            string firefoxCredentialFile4 = $"{directory}\\{"key4.db"}";
                            if (File.Exists(firefoxCredentialFile4))
                            {
                                results.Add(firefoxCredentialFile4);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return results;
        }

        private static List<string> GetFirefoxHistory()
        {
            List<string> results = new List<string>();
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = $"{Environment.GetEnvironmentVariable("SystemDrive")}\\Users\\";
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userFirefoxBasePath = $"{dir}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\";
                            results = ParseFirefoxHistory(userFirefoxBasePath);
                        }
                    }
                }
                else
                {
                    string userFirefoxBasePath =
                        $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\";
                    results = ParseFirefoxHistory(userFirefoxBasePath);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        private static List<string> ParseFirefoxHistory(string path)
        {
            List<string> results = new List<string>();
            // parses a Firefox history file via regex
            if (Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (string directory in directories)
                {
                    string firefoxHistoryFile = String.Format("{0}\\{1}", directory, "places.sqlite");
                    Regex historyRegex = new Regex(@"(http|ftp|https|file)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

                    try
                    {
                        using (StreamReader r = new StreamReader(firefoxHistoryFile))
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
                        Console.WriteLine("\r\n    [x] IO exception, places.sqlite file likely in use (i.e. Firefox is likely running).", exception.Message);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.PrintException(ex.Message);
                    }
                }
            }
            return results;
        }
    }
}
