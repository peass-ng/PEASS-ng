using System;
using System.Collections.Generic;
using winPEAS.Helpers;
using winPEAS.Info.ApplicationInfo;

namespace winPEAS.Checks
{
    internal class SoapClientInfo : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint(".NET SOAP Client Proxies (SOAPwn)");

            CheckRunner.Run(PrintSoapClientFindings, isDebug);
        }

        private static void PrintSoapClientFindings()
        {
            try
            {
                Beaprint.MainPrint("Potential SOAPwn / HttpWebClientProtocol abuse surfaces");
                Beaprint.LinkPrint(
                    "https://labs.watchtowr.com/soapwn-pwning-net-framework-applications-through-http-client-proxies-and-wsdl/",
                    "Look for .NET services that let attackers control SoapHttpClientProtocol URLs or WSDL imports to coerce NTLM or drop files.");

                List<SoapClientProxyFinding> findings = SoapClientProxyAnalyzer.CollectFindings();
                if (findings.Count == 0)
                {
                    Beaprint.NotFoundPrint();
                    return;
                }

                foreach (SoapClientProxyFinding finding in findings)
                {
                    string severity = finding.BinaryIndicators.Contains("ServiceDescriptionImporter")
                        ? "Dynamic WSDL import"
                        : "SOAP proxy usage";

                    Beaprint.BadPrint($"    [{severity}] {finding.BinaryPath}");

                    foreach (SoapClientProxyInstance instance in finding.Instances)
                    {
                        string instanceInfo = $"        -> {instance.SourceType}: {instance.Name}";
                        if (!string.IsNullOrEmpty(instance.Account))
                        {
                            instanceInfo += $" ({instance.Account})";
                        }
                        if (!string.IsNullOrEmpty(instance.Extra))
                        {
                            instanceInfo += $" | {instance.Extra}";
                        }

                        Beaprint.GrayPrint(instanceInfo);
                    }

                    if (finding.BinaryIndicators.Count > 0)
                    {
                        Beaprint.BadPrint("        Binary indicators: " + string.Join(", ", finding.BinaryIndicators));
                    }

                    if (finding.ConfigIndicators.Count > 0)
                    {
                        string configLabel = string.IsNullOrEmpty(finding.ConfigPath)
                            ? "Config indicators"
                            : $"Config indicators ({finding.ConfigPath})";
                        Beaprint.BadPrint("        " + configLabel + ": " + string.Join(", ", finding.ConfigIndicators));
                    }

                    if (finding.BinaryScanFailed)
                    {
                        Beaprint.GrayPrint("        (Binary scan skipped due to access/size limits)");
                    }

                    if (finding.ConfigScanFailed)
                    {
                        Beaprint.GrayPrint("        (Unable to read config file)");
                    }

                    Beaprint.PrintLineSeparator();
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
        }
    }
}
