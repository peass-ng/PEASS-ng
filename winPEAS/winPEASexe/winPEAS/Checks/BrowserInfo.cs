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
        public string[] MitreAttackIds { get; } = new[] { "T1217", "T1539", "T1555.003" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Browsers Information", "T1217,T1539,T1555.003");

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
