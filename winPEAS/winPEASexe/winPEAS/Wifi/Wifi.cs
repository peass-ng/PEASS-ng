using System.Collections.Generic;
using System.Text.RegularExpressions;
using winPEAS.Helpers;

namespace winPEAS.Wifi
{
    internal class Wifi
    {
        public static Dictionary<string, string> Retrieve()
        {
            Dictionary<string, string> connections = new Dictionary<string, string>();
            foreach (string ssid in GetSSIDs())
            {
                string password = GetPassword(ssid);
                connections.Add(ssid, password);
            }

            return connections;
        }

        private static IEnumerable<string> GetSSIDs()
        {
            string args = "wlan show profiles";
            string result = MyUtils.ExecCMD(args, "netsh");
            Regex regex = new Regex(@"\s+:\s+([^\r\n]+)", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(result);
            List<string> ssids = new List<string>();

            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Groups.Count > 0 && !string.IsNullOrWhiteSpace(matches[i].Groups[1].Value))
                {
                    ssids.Add(matches[i].Groups[1].Value);
                }
            }

            return ssids;
        }

        private static string GetPassword(string ssid)
        {
            string args = $@" wlan show profile name=""{ssid}"" key=""clear""";
            string result = MyUtils.ExecCMD(args, "netsh");
            Regex regex = new Regex(@"Key Content\s+:\s+([^\r\n]+)", RegexOptions.Multiline);
            MatchCollection matches = regex.Matches(result);
            string password = string.Empty;

            if (matches.Count > 0 && matches[0].Groups.Count > 1)
            {
                password = matches[0].Groups[1].Value;
            }

            return password;
        }
    }
}
