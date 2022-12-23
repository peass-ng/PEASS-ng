using System.Collections.Generic;
using System.Linq;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Browsers.Models;

namespace winPEAS.KnownFileCreds.Browsers
{
    internal abstract class BrowserBase : IBrowser
    {
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
    }
}
