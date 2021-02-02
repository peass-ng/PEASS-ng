using System;
using System.Collections.Generic;
using winPEAS.Helpers;
using winPEAS._3rdParty.Watson.Msrc;

namespace winPEAS._3rdParty.Watson
{

    //////////////////////////////
    ////// MAIN WATSON CLASS /////
    //////////////////////////////
    class Watson
    {
        public static void FindVulns()
        {
            Console.WriteLine(Beaprint.YELLOW + "  [?] " + Beaprint.LBLUE + "Windows vulns search powered by " + Beaprint.LRED + "Watson" + Beaprint.LBLUE + "(https://github.com/rasta-mouse/Watson)" + Beaprint.NOCOLOR);

            // Supported versions
            var supportedVersions = new Dictionary<int, string>()
            {
                { 10240, "1507" }, { 10586, "1511" }, { 14393, "1607" }, { 15063, "1703" }, { 16299, "1709" },
                { 17134, "1803" }, { 17763, "1809" }, { 18362, "1903" }, { 18363, "1909" }, { 19041, "2004" },
                { 19042, "20H2" }
            };

            // Get OS Build number
            var buildNumber = Wmi.GetBuildNumber();
            if (buildNumber != 0)
            {
                if (!supportedVersions.ContainsKey(buildNumber))
                {
                    Console.Error.WriteLine($" [!] Windows version not supported, build number: '{buildNumber}'");
                    return;
                }

                var version = supportedVersions[buildNumber];
                Console.WriteLine(" [*] OS Version: {0} ({1})", version, buildNumber);
            }
            else
            {
                Console.Error.WriteLine(" [!] Could not retrieve Windows BuildNumber");
                return;
            }
            
            // List of KBs installed
            Console.WriteLine(" [*] Enumerating installed KBs...");
            var installedKBs = Wmi.GetInstalledKBs();

#if DEBUG
            Console.WriteLine();

            foreach (var kb in installedKBs)
            {
                Console.WriteLine(" {0}", kb);
            }

            Console.WriteLine();
#endif

            // List of Vulnerabilities
            var vulnerabilities = new VulnerabilityCollection();

            // Check each one
            CVE_2019_0836.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_0841.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_1064.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_1130.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_1253.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_1315.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_1385.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_1388.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2019_1405.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2020_0668.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2020_0683.Check(vulnerabilities, buildNumber, installedKBs);
            CVE_2020_1013.Check(vulnerabilities, buildNumber, installedKBs);

            // Print the results
            vulnerabilities.ShowResults();
        }
    }
}
