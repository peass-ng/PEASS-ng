using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using winPEAS.Helpers;

namespace winPEAS.InterestingFiles
{
    internal static class Unattended
    {
        public static List<string> ExtractUnattendedPwd(string path)
        {
            List<string> results = new List<string>();

            try
            {
                string text = File.ReadAllText(path);
                text = text.Replace("\n", "");
                text = text.Replace("\r", "");
                Regex regex = new Regex(@"<Password>.*</Password>");

                foreach (Match match in regex.Matches(text))
                {
                    results.Add(match.Value);
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return results;
        }

        public static List<string> GetUnattendedInstallFiles()
        {
            //From SharpUP
            var results = new List<string>();

            try
            {
                var winDir = Environment.GetEnvironmentVariable("windir");
                string[] searchLocations =
                {
                    $"{winDir}\\sysprep\\sysprep.xml",
                    $"{winDir}\\sysprep\\sysprep.inf",
                    $"{winDir}\\sysprep.inf",
                    $"{winDir}\\Panther\\Unattended.xml",
                    $"{winDir}\\Panther\\Unattend.xml",
                    $"{winDir}\\Panther\\Unattend\\Unattend.xml",
                    $"{winDir}\\Panther\\Unattend\\Unattended.xml",
                    $"{winDir}\\System32\\Sysprep\\unattend.xml",
                    $"{winDir}\\System32\\Sysprep\\Panther\\unattend.xml",
                    $"{winDir}\\..\\unattend.xml",
                    $"{winDir}\\..\\unattend.inf",
                };

                results.AddRange(searchLocations.Where(File.Exists));
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
