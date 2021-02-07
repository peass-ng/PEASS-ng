using System.Text.RegularExpressions;

namespace winPEAS.Info.EventsInfo
{
    internal static class Common
    {
        public static Regex[] GetInterestingProcessArgsRegex()
        {
            // helper that returns the set of "sensitive" cmdline regular expressions
            // adapted from @djhohnstein's EventLogParser project - https://github.com/djhohnstein/EventLogParser/blob/master/EventLogParser/EventLogHelpers.cs           
            var globalOptions = RegexOptions.IgnoreCase & RegexOptions.Multiline;

            Regex[] processCmdLineRegex =
            {
                //new Regex(@"(New-Object.*System.Management.Automation.PSCredential.*)", globalOptions),
                new Regex(@"(bitsadmin(.exe)?.*(/RemoveCredentials|/SetCredentials) .*)", globalOptions),
                new Regex(@"(bootcfg(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(certreq(.exe)?.*-p .*)", globalOptions),
                new Regex(@"(certutil(.exe)?.*-p .*)", globalOptions),
                new Regex(@"(cmdkey(.exe)?.*/pass:.*)", globalOptions),
                new Regex(@"(cscript.*-w .*)", globalOptions),
                new Regex(@"(driverquery(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(eventcreate(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(getmac(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(gpfixup(.exe)?.*/pwd:.*)", globalOptions),
                new Regex(@"(gpresult(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(kitty(.exe)?.*(-pw|-pass) .*)", globalOptions),
                new Regex(@"(mapadmin(.exe)?.*-p .*)", globalOptions),
                new Regex(@"(mount(.exe)?.*-p:.*)", globalOptions),
                new Regex(@"(net(.exe)?.*use .*)", globalOptions),
                new Regex(@"(net(.exe)?.*user .*)", globalOptions),
                new Regex(@"(nfsadmin(.exe)?.*-p .*)", globalOptions),
                new Regex(@"(openfiles(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(pscp(.exe)?.*-pw .*)", globalOptions),
                new Regex(@"(psexec(.exe)?.*-p .*)", globalOptions),
                new Regex(@"(psexec64(.exe)?.*-p .*)", globalOptions),
                new Regex(@"(putty(.exe)?.*-pw .*)", globalOptions),
                new Regex(@"(schtasks(.exe)?.*(/p|/rp) .*)", globalOptions),
                new Regex(@"(setx(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(ssh(.exe)?.*-i .*)", globalOptions),
                new Regex(@"(systeminfo(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(takeown(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(taskkill(.exe)?.*/p .*)", globalOptions),
                new Regex(@"(tscon(.exe)?.*/password:.*)", globalOptions),
                new Regex(@"(wecutil(.exe)?.*(/up|/cup|/p):.*)", globalOptions),
                new Regex(@"(winrm(.vbs)?.*-p .*)", globalOptions),
                new Regex(@"(winrs(.exe)?.*/p(assword)? .*)", globalOptions),
                new Regex(@"(wmic(.exe)?.*/password:.*)", globalOptions),
                new Regex(@"(ConvertTo-SecureString.*AsPlainText.*)", globalOptions),
            };

            return processCmdLineRegex;
        }
    }
}
