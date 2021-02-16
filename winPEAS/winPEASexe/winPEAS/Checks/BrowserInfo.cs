using System.Collections.Generic;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Browsers;
using winPEAS.KnownFileCreds.Browsers.Brave;
using winPEAS.KnownFileCreds.Browsers.Chrome;
using winPEAS.KnownFileCreds.Browsers.Firefox;
using winPEAS.KnownFileCreds.Browsers.Opera;

namespace winPEAS.Checks
{
    internal class BrowserInfo : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Browsers Information");

            new List<IBrowser>
            {
                new Firefox(),
                new Chrome(),
                new Opera(),
                new Brave(),
                new InternetExplorer(),
            }.ForEach(browser => CheckRunner.Run(browser.PrintInfo, isDebug));
        }
    }
}
