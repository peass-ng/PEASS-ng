using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace winPEAS.KnownFileCreds
{
    static class Firefox
    {
        public static List<string> GetFirefoxDbs()
        {
            List<string> results = new List<string>();
            // checks if Firefox has a history database
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
                            string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", dir);
                            if (System.IO.Directory.Exists(userFirefoxBasePath))
                            {
                                string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                                foreach (string directory in directories)
                                {
                                    string firefoxCredentialFile3 = String.Format("{0}\\{1}", directory, "key3.db");
                                    if (System.IO.File.Exists(firefoxCredentialFile3))
                                        results.Add(firefoxCredentialFile3);

                                    string firefoxCredentialFile4 = String.Format("{0}\\{1}", directory, "key4.db");
                                    if (System.IO.File.Exists(firefoxCredentialFile4))
                                        results.Add(firefoxCredentialFile3);
                                }
                            }
                        }
                    }
                }
                else
                {
                    string userName = Environment.GetEnvironmentVariable("USERNAME");
                    string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", System.Environment.GetEnvironmentVariable("USERPROFILE"));

                    if (System.IO.Directory.Exists(userFirefoxBasePath))
                    {
                        string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                        foreach (string directory in directories)
                        {
                            string firefoxCredentialFile3 = String.Format("{0}\\{1}", directory, "key3.db");
                            if (System.IO.File.Exists(firefoxCredentialFile3))
                                results.Add(firefoxCredentialFile3);

                            string firefoxCredentialFile4 = String.Format("{0}\\{1}", directory, "key4.db");
                            if (System.IO.File.Exists(firefoxCredentialFile4))
                                results.Add(firefoxCredentialFile4);
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

        public static List<string> GetFirefoxHistory()
        {
            List<string> results = new List<string>();
            try
            {
                if (MyUtils.IsHighIntegrity())
                {
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", dir);
                            results = ParseFirefoxHistory(userFirefoxBasePath);
                        }
                    }
                }
                else
                {
                    string userFirefoxBasePath = String.Format("{0}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\", System.Environment.GetEnvironmentVariable("USERPROFILE"));
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
