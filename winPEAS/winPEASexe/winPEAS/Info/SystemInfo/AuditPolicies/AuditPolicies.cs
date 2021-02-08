using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using winPEAS.Helpers.Search;
using winPEAS.Native;

namespace winPEAS.Info.SystemInfo.AuditPolicies
{
    internal class AuditPolicies
    {
        private static readonly string SystemRoot = Environment.GetEnvironmentVariable("SystemRoot");

        // https://code.msdn.microsoft.com/windowsdesktop/Reading-and-Writing-Values-85084b6a
        private static int Capacity = 512;

        public static IEnumerable<AuditPolicyGPOInfo> GetAuditPoliciesInfos()
        {
            var searchPath = $"{SystemRoot}\\System32\\GroupPolicy\\DataStore\\0\\sysvol\\";
            var files = SearchHelper.GetFilesFast(searchPath, "audit.csv");
            var classicFiles = SearchHelper.GetFilesFast(searchPath, "GptTmpl.inf");

            foreach (var classicFilePath in classicFiles)
            {
                var fullFilePath = classicFilePath.FullPath;
                var result = ParseGPOPath(fullFilePath);
                var domain = result[0];
                var gpo = result[1];

                //ParseClassicPolicy
                var sections = ReadSections(fullFilePath);

                if (!sections.Contains("Event Audit"))
                    continue;

                var settings = ParseClassicPolicy(fullFilePath);

                yield return new AuditPolicyGPOInfo(
                    classicFilePath.FullPath,
                    domain,
                    gpo,
                    "classic",
                    settings
                );
            }

            foreach (var filePath in files)
            {
                var result = ParseGPOPath(filePath.FullPath);
                var domain = result[0];
                var gpo = result[1];

                var settings = ParseAdvancedPolicy(filePath.FullPath);

                yield return new AuditPolicyGPOInfo(
                    filePath.FullPath,
                    domain,
                    gpo,
                    "advanced",
                    settings
                );
            }
        }

        private static string[] ParseGPOPath(string path)
        {
            // returns an array of the domain and GPO GUID from an audit.csv (or GptTmpl.inf) path

            var searchPath = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\GroupPolicy\\DataStore\\0\\sysvol\\";
            var sysnativeSearchPath = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\Sysnative\\GroupPolicy\\DataStore\\0\\sysvol\\";
            var actualSearchPath = Regex.IsMatch(path, "System32") ? searchPath : sysnativeSearchPath;

            var rest = path.Substring(actualSearchPath.Length, path.Length - actualSearchPath.Length);
            var parts = rest.Split('\\');
            string[] result = { parts[0], parts[2] };
            return result;
        }

        private static string[] ReadSections(string filePath)
        {
            // first line will not recognize if ini file is saved in UTF-8 with BOM
            while (true)
            {
                var chars = new char[Capacity];
                var size = Kernel32.GetPrivateProfileString(null, null, "", chars, Capacity, filePath);

                if (size == 0)
                    return new string[] { };

                if (size < Capacity - 2)
                {
                    var result = new string(chars, 0, size);
                    var sections = result.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    return sections;
                }

                Capacity *= 2;
            }
        }

        private static List<AuditEntryInfo> ParseAdvancedPolicy(string path)
        {
            // parses a "advanced" auditing policy (audit.csv), returning a list of AuditEntries

            var results = new List<AuditEntryInfo>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (values[0].Equals("Machine Name")) // skip the header
                    {
                        continue;
                    }

                    // CSV  lines:
                    // Machine Name,Policy Target,Subcategory,Subcategory GUID,Inclusion Setting,Exclusion Setting,Setting Value

                    var target = values[1];
                    var subcategory = values[2];
                    var subcategoryGuid = values[3];
                    var auditType = (AuditType)int.Parse(values[6]);

                    results.Add(new AuditEntryInfo(
                        target,
                        subcategory,
                        subcategoryGuid,
                        auditType
                    ));
                }
            }

            return results;
        }

        private static List<AuditEntryInfo> ParseClassicPolicy(string path)
        {
            // parses a "classic" auditing policy (GptTmpl.inf), returning a list of AuditEntries

            var results = new List<AuditEntryInfo>();

            var settings = ReadKeyValuePairs("Event Audit", path);
            foreach (var setting in settings)
            {
                var parts = setting.Split('=');

                var result = new AuditEntryInfo(
                    string.Empty,
                    parts[0],
                    string.Empty,
                    (AuditType)Int32.Parse(parts[1])
                );

                results.Add(result);
            }

            return results;
        }

        private static string[] ReadKeyValuePairs(string section, string filePath)
        {
            while (true)
            {
                var returnedString = Marshal.AllocCoTaskMem(Capacity * sizeof(char));
                var size = Kernel32.GetPrivateProfileSection(section, returnedString, Capacity, filePath);

                if (size == 0)
                {
                    Marshal.FreeCoTaskMem(returnedString);
                    return new string[] { };
                }

                if (size < Capacity - 2)
                {
                    var result = Marshal.PtrToStringAuto(returnedString, size - 1);
                    Marshal.FreeCoTaskMem(returnedString);
                    var keyValuePairs = result.Split('\0');
                    return keyValuePairs;
                }

                Marshal.FreeCoTaskMem(returnedString);
                Capacity *= 2;
            }
        }
    }
}
