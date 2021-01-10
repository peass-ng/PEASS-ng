using System.Collections.Generic;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Browsers;

namespace winPEAS.Checks
{
    internal class BrowserInfo : ISystemCheck
    {
        public void PrintInfo()
        {
            Beaprint.GreatPrint("Browsers Information");

            new List<IBrowser>
            {
                new Firefox(),
                new Chrome(),
                new InternetExplorer(),
            }.ForEach(browser => browser.PrintInfo());
        }
    }
}
