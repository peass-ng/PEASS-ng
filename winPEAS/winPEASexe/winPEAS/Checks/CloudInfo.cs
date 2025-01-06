using System.Collections.Generic;
using winPEAS.Helpers;
using winPEAS.Info.CloudInfo;

namespace winPEAS.Checks
{
    internal class CloudInfo : ISystemCheck
    {
        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Cloud Information");

            Dictionary<string, string> colorsTraining = new Dictionary<string, string>()
                {
                    { "training.hacktricks.wiki", Beaprint.ansi_color_good },
                    { "Learn & practice cloud hacking in", Beaprint.ansi_color_yellow },
                };
            Beaprint.AnsiPrint("Learn and practice cloud hacking in training.hacktricks.wiki", colorsTraining);

            var cloudInfoList = new List<CloudInfoBase>
            {
               new AWSInfo(),
               new AzureInfo(),
               new GCPInfo(),
               new GCPJoinedInfo(),
               new GCDSInfo(),
               new GPSInfo(),
            };

            foreach (var cloudInfo in cloudInfoList)
            {
                string isCloud = cloudInfo.IsCloud ? "Yes" : "No";
                string line = string.Format($"{cloudInfo.Name + "?",-40}{isCloud,-5}");

                Dictionary<string, string> colorsMS = new Dictionary<string, string>()
                {
                    { "Yes", Beaprint.ansi_color_bad },
                };
                Beaprint.AnsiPrint(line, colorsMS);
            }

            foreach (var cloudInfo in cloudInfoList)
            {                
                if (cloudInfo.IsCloud)
                {
                    Beaprint.MainPrint(cloudInfo.Name + " Enumeration");

                    if (cloudInfo.IsAvailable)
                    {
                        foreach (var kvp in cloudInfo.EndpointDataList())
                        {
                            // key = "section", e.g. User, Network, ...
                            string section = kvp.Key;
                            var endpointDataList = kvp.Value;

                            Beaprint.ColorPrint(section, Beaprint.ansi_color_good);

                            foreach (var endpointData in endpointDataList)
                            {
                                var colors = new Dictionary<string, string>
                                {
                                    { endpointData.EndpointName, Beaprint.GRAY }
                                };

                                string message;
                                if (!string.IsNullOrEmpty(endpointData.Data))
                                {
                                    message = endpointData.Data;
                                    // if it is a JSON data, add additional newline so it's displayed on a separate line
                                    if (message.StartsWith("{"))
                                    {
                                        message = $"\n{message}\n";
                                    }

                                    if (endpointData.IsAttackVector)
                                    {
                                        colors.Add(message, Beaprint.ansi_color_bad);
                                    }
                                    else
                                    {
                                        colors.Add(message, Beaprint.ansi_color_gray);
                                    }
                                }
                                else
                                {
                                    message = "No data received from the metadata endpoint";
                                }

                                Beaprint.ColorPrint($"{endpointData.EndpointName,-30}{message}", Beaprint.ansi_color_gray);
                            }

                            Beaprint.GrayPrint("");
                        }
                    }
                    else
                    {
                        Beaprint.NoColorPrint("Could not connect to the metadata endpoint");
                    }
                }
            }
        }
    }
}
