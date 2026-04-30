using System;
using System.Collections.Generic;
using System.Linq;
using winPEAS.Checks;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Browsers.Models;

namespace winPEAS.KnownFileCreds.Browsers
{
    internal abstract class BrowserBase : IBrowser
    {
        protected const string BrowserHistoryLink = "https://book.hacktricks.wiki/en/windows-hardening/windows-local-privilege-escalation/index.html#browsers-history";

        public abstract string Name { get; }
        public abstract IEnumerable<CredentialModel> GetSavedCredentials();
        public abstract void PrintInfo();


        public virtual void PrintSavedCredentials()
        {
            Beaprint.MainPrint($"Showing saved credentials for {Name}");

            var credentials = (GetSavedCredentials() ?? new List<CredentialModel>()).ToList();

            if (credentials.Count == 0)
            {
                Beaprint.ColorPrint("    Info: if no credentials were listed, you might need to close the browser and try again.", Beaprint.ansi_color_yellow);
            }

            foreach (var credential in credentials)
            {
                if (!string.IsNullOrEmpty(credential.Username))
                {
                    Beaprint.BadPrint($"     Url:           {credential.Url}\n" +
                                      $"     Username:      {credential.Username}\n" +
                                      $"     Password:      {credential.Password}\n ");

                    Beaprint.PrintLineSeparator();
                }
            }
        }

        protected static void PrintBrowserHistoryLink()
        {
            Beaprint.LinkPrint(BrowserHistoryLink);
        }

        protected static void PrintCredentialHistory(List<string> history, string browserName)
        {
            if (history.Count > 0)
            {
                Dictionary<string, string> colors = new Dictionary<string, string>
                {
                    { Globals.PrintCredStrings, Beaprint.ansi_color_bad },
                };

                foreach (string url in history)
                {
                    if (MyUtils.ContainsAnyRegex(url.ToUpper(), Browser.CredStringsRegex))
                    {
                        Beaprint.AnsiPrint("    " + url, colors);
                    }
                }

                Console.WriteLine();

                int limit = 50;
                Beaprint.MainPrint($"{browserName} history -- limit {limit}\n");
                Beaprint.ListPrint(history.Take(limit).ToList());
            }
            else
            {
                Beaprint.NotFoundPrint();
            }
        }
    }
}
