using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Helpers.Search;
using static winPEAS.Helpers.YamlConfig.YamlConfig.SearchParameters;

namespace winPEAS.Checks
{
    internal class FileAnalysis : ISystemCheck
    {
        private const int ListFileLimit = 70;

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("File Analysis");

            new List<Action>
            {
                PrintYAMLSearchFiles
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private static List<CustomFileInfo> InitializeFileSearch()
        {
            var files = new List<CustomFileInfo>();
            var systemDrive = $"{SearchHelper.SystemDrive}\\";

            List<string> directories = new List<string>()
            {
                @$"{systemDrive}inetpub",
                @$"{systemDrive}usr\etc\snmp",
                @$"{systemDrive}windows\temp",
                @$"{systemDrive}xampp",
            };

            List<string> wildcardDirectories = new List<string>()
                {
                    "apache*",
                    "tomcat*",
                };

            foreach (var wildcardDirectory in wildcardDirectories)
            {
                directories.AddRange(Directory.GetDirectories(systemDrive, wildcardDirectory, SearchOption.TopDirectoryOnly));
            }

            foreach (var directory in directories)
            {
                files.AddRange(SearchHelper.GetFilesFast(directory, "*", isFoldersIncluded: true));
            }

            files.AddRange(SearchHelper.RootDirUsers);
          //  files.AddRange(SearchHelper.RootDirCurrentUser); // not needed, it's contained within RootDirUsers
            files.AddRange(SearchHelper.DocumentsAndSettings);
            files.AddRange(SearchHelper.GroupPolicyHistory);    // TODO maybe not needed here
            files.AddRange(SearchHelper.ProgramFiles);
            files.AddRange(SearchHelper.ProgramFilesX86);

            return files;
        }

        private static bool Search(List<CustomFileInfo> files, string fileName, FileSettings fileSettings, ref int resultsCount)
        {
            bool isRegexSearch = fileName.Contains("*");
            string pattern = string.Empty;

            if (isRegexSearch)
            {
                pattern = GetRegexpFromString(fileName);
            }         

            foreach (var file in files)
            {
                bool isFileFound;
                if (isRegexSearch)
                {
                    isFileFound = Regex.IsMatch(file.Filename, pattern, RegexOptions.IgnoreCase);
                }
                else
                {
                    isFileFound = file.Filename.ToLower() == fileName;
                }

                if (isFileFound)
                {
                    // there are no inner sections
                    if (fileSettings.files == null)
                    {
                        var isProcessed = ProcessResult(file, fileSettings, ref resultsCount);
                        if (!isProcessed)
                        {
                            return true;
                        }
                    }
                    // there are inner sections
                    else 
                    {
                        foreach (var innerFileToSearch in fileSettings.files)
                        {
                            // search for inner files/folders by inner file/folder name
                            var innerFiles = SearchHelper.GetFilesFast(file.FullPath, innerFileToSearch.name, isFoldersIncluded: true);

                            foreach (var innerFile in innerFiles)
                            {
                                // process inner file/folder
                                var isProcessed = ProcessResult(innerFile, innerFileToSearch.value, ref resultsCount);
                                if (!isProcessed)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
       
        private static void PrintYAMLSearchFiles()
        {
            try
            {
                var files = InitializeFileSearch();
                var folders = files.Where(f => f.IsDirectory).ToList();
                var config = Checks.YamlConfig;
                var defaults = config.defaults;
                var searchItems = config.search.Where(i => i.value.config.auto_check &&
                                                           (i.value.disable == null || !i.value.disable.Contains("winpeas")));

                foreach (var searchItem in searchItems)
                {
                    var searchName = searchItem.name;
                    var value = searchItem.value;
                    var searchConfig = value.config;

                    CheckRunner.Run(() =>
                    {
                        Beaprint.MainPrint($"Analyzing {searchName} Files (limit {ListFileLimit})");

                        int resultsCount = 0;
                        bool isSearchFinished = false;

                        foreach (var file in value.files)
                        {
                            var fileName = file.name.ToLower();
                            var fileSettings = file.value;
                            var itemsToSearch = fileSettings.type == "f" ? files : folders;

                            isSearchFinished = Search(itemsToSearch, fileName, fileSettings, ref resultsCount);

                            if (isSearchFinished)
                            {
                                break;
                            }
                        }
                    }, Checks.IsDebug);
                }
            }
            catch (Exception e)
            {
            }
        }

        private static string GetRegexpFromString(string str)
        {
            // we need to update the regexp to work here
            // . -> \.
            // * -> .*
            // add $ at the end to avoid false positives
            
            var pattern = str.Replace(".", @"\.")
                             .Replace("*", @".*");

            pattern = $"{pattern}$";

            return pattern;
        }

        private static bool ProcessResult(
            CustomFileInfo fileInfo,
            Helpers.YamlConfig.YamlConfig.SearchParameters.FileSettings fileSettings,
            ref int resultsCount)
        {
            // print depending on the options here
            resultsCount++;

            if (resultsCount > ListFileLimit) return false;


            if (fileSettings.type == "f")
            {
                if ((bool)fileSettings.just_list_file)
                {
                    Beaprint.BadPrint($"    {fileInfo.FullPath}");
                }
                else
                {
                    GrepResult(fileInfo, fileSettings);
                }
            }
            else if (fileSettings.type == "d")
            {
                // just list the directory 
                if ((bool)fileSettings.just_list_file)
                {
                    string[] files = Directory.GetFiles(fileInfo.FullPath, "*", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        Beaprint.BadPrint($"    {file}");
                    }
                }
                else
                {
                   // should not happen
                }
            }

            return true;
        }

        private static void GrepResult(CustomFileInfo fileInfo, FileSettings fileSettings)
        {
            Beaprint.NoColorPrint($"    '{fileInfo.FullPath}' - content:");

            var fileContent = File.ReadLines(fileInfo.FullPath);
            var colors = new Dictionary<string, string>();

            if ((bool)fileSettings.only_bad_lines)
            {
                colors.Add(fileSettings.bad_regex, Beaprint.ansi_color_bad);
                fileContent = fileContent.Where(l => Regex.IsMatch(l, fileSettings.bad_regex, RegexOptions.IgnoreCase));
            }
            else
            {
                string lineGrep = null;

                if ((bool)fileSettings.remove_empty_lines)
                {
                    fileContent = fileContent.Where(l => !string.IsNullOrWhiteSpace(l));
                }

                if (!string.IsNullOrWhiteSpace(fileSettings.remove_regex))
                {
                    var pattern = GetRegexpFromString(fileSettings.remove_regex);
                    fileContent = fileContent.Where(l => !Regex.IsMatch(l, pattern, RegexOptions.IgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(fileSettings.good_regex))
                {
                    colors.Add(fileSettings.good_regex, Beaprint.ansi_color_good);
                }
                if (!string.IsNullOrWhiteSpace(fileSettings.bad_regex))
                {
                    colors.Add(fileSettings.bad_regex, Beaprint.ansi_color_bad);
                }
                if (!string.IsNullOrWhiteSpace(fileSettings.line_grep))
                {
                    lineGrep = SanitizeLineGrep(fileSettings.line_grep);
                }
                
                fileContent = fileContent.Where(line => (!string.IsNullOrWhiteSpace(fileSettings.good_regex) && Regex.IsMatch(line, fileSettings.good_regex, RegexOptions.IgnoreCase)) ||
                                                       (!string.IsNullOrWhiteSpace(fileSettings.bad_regex) && Regex.IsMatch(line, fileSettings.bad_regex, RegexOptions.IgnoreCase)) ||
                                                       (!string.IsNullOrWhiteSpace(lineGrep) && Regex.IsMatch(line, lineGrep, RegexOptions.IgnoreCase)));
            }    

            var content = string.Join(Environment.NewLine, fileContent);

            Beaprint.AnsiPrint(content, colors);

            Console.WriteLine();
        }

        private static string SanitizeLineGrep(string lineGrep)
        {
            // sanitize the string, e.g.
            // '-i -a -o "description.*" | sort | uniq'
            // - remove everything except from "description.*"

            Regex regex = new Regex("\"([^\"]+)\"");
            Match match = regex.Match(lineGrep);

            if (match.Success)
            {
                var group = match.Groups[1];
                return group.Value;
            }

            return null;
        }
    }
}
