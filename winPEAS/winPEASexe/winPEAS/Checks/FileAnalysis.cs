using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
                PrintYAMLSearchFiles,
                PrintYAMLRegexesSearchFiles
            }.ForEach(action => CheckRunner.Run(action, isDebug));
        }

        private static List<CustomFileInfo> InitializeFileSearch(bool useProgramFiles = true)
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
            // files.AddRange(SearchHelper.RootDirCurrentUser); // not needed, it's contained within RootDirUsers
            files.AddRange(SearchHelper.DocumentsAndSettings);
            files.AddRange(SearchHelper.GroupPolicyHistory);    // TODO maybe not needed here
            if (useProgramFiles)
            {
                files.AddRange(SearchHelper.ProgramFiles);
                files.AddRange(SearchHelper.ProgramFilesX86);
            }

            return files;
        }

        private static bool[] Search(List<CustomFileInfo> files, string fileName, FileSettings fileSettings, ref int resultsCount, string searchName, bool somethingFound)
        {
            if (Checks.IsDebug)
                Beaprint.PrintDebugLine($"Searching for {fileName}");

            bool isRegexSearch = fileName.Contains("*");
            bool isFolder = fileSettings.files != null;
            string pattern = string.Empty;


            if (isRegexSearch)
            {
                pattern = GetRegexpFromString(fileName);
            }

            foreach (var file in files)
            {
                bool isFileFound = false;

                if (isFolder)
                {
                    if (pattern == string.Empty)
                    {
                        isFileFound = file.FullPath.ToLower().Contains($"\\{fileName}\\");
                    }
                    else
                    {
                        foreach (var fold in file.FullPath.Split('\\').Skip(1))
                        {   
                            try
                            {
                                isFileFound = Regex.IsMatch(fold, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(20));
                                if (isFileFound) break;
                            }
                            catch (RegexMatchTimeoutException e)
                            {
                                if (Checks.IsDebug)
                                {
                                    Beaprint.GrayPrint($"The file in folder regex {pattern} had a timeout in {fold} (ReDoS avoided but regex unchecked in a file)");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (pattern == String.Empty)
                    {
                        isFileFound = file.Filename.ToLower() == fileName.ToLower();
                    }
                    else
                    {
                        try
                        {
                            isFileFound = Regex.IsMatch(file.Filename, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(20));
                        }
                        catch (RegexMatchTimeoutException e)
                        {
                            if (Checks.IsDebug)
                            {
                                Beaprint.GrayPrint($"The file regex {pattern} had a timeout in {file.Filename} (ReDoS avoided but regex unchecked in a file)");
                            }
                        }
                    }
                }


                if (isFileFound)
                {
                    if (!somethingFound)
                    {
                        Beaprint.MainPrint($"Found {searchName} Files");
                        somethingFound = true;
                    }

                    if (!isFolder)
                    {
                        var isProcessed = ProcessResult(file, fileSettings, ref resultsCount);
                        if (!isProcessed)
                        {
                            return new bool[] { true, somethingFound };
                        }
                    }
                    // there are inner sections
                    else
                    {
                        foreach (var innerFileToSearch in fileSettings.files)
                        {
                            List<CustomFileInfo> one_file_list = new List<CustomFileInfo>() { file };
                            Search(one_file_list, innerFileToSearch.name, innerFileToSearch.value, ref resultsCount, searchName, somethingFound);
                        }
                    }
                }
            }


            return new bool[] { false, somethingFound };
        }

        public static List<string> SearchContent(string text, string regex_str, bool caseinsensitive)
        {
            List<string> foundMatches = new List<string>();

            try
            {
                Regex rgx;
                bool is_re_match = false;
                try
                {
                    // Escape backslashes in the regex string - I don't think this is needed anymore
                    //string escapedRegex = regex_str.Trim().Replace(@"\", @"\\");
                    string escapedRegex = regex_str.Trim();

                    // Use "IsMatch" because it supports timeout, if exception is thrown exit the func to avoid ReDoS in "rgx.Matches"
                    if (caseinsensitive)
                    {
                        is_re_match = Regex.IsMatch(text, escapedRegex, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(120));
                        rgx = new Regex(escapedRegex, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        is_re_match = Regex.IsMatch(text, escapedRegex, RegexOptions.None, TimeSpan.FromSeconds(120));
                        rgx = new Regex(escapedRegex);
                    }
                }
                catch (RegexMatchTimeoutException e)
                {
                    if (Checks.IsDebug)
                    {
                        Beaprint.GrayPrint($"The regex {regex_str} had a timeout (ReDoS avoided but regex unchecked in a file)");
                    }
                    return foundMatches;
                }

                if (!is_re_match)
                {
                    return foundMatches;
                }

                int cont = 0;
                foreach (Match match in rgx.Matches(text))
                {
                    if (cont > 10) break;

                    if (match.Value.Length < 400 && match.Value.Trim().Length > 2)
                        foundMatches.Add(match.Value);

                    cont++;
                }
            }
            catch (Exception e)
            {
                Beaprint.GrayPrint($"Error looking for regex {regex_str} inside files: {e}");
            }

            return foundMatches;
        }

        private static void PrintYAMLSearchFiles()
        {
            try
            {
                var files = InitializeFileSearch();
                //var folders = files.Where(f => f.IsDirectory).ToList();
                var config = Checks.YamlConfig;
                var defaults = config.defaults;
                var searchItems = config.search.Where(i => !(i.value.disable != null && i.value.disable.Contains("winpeas")));

                foreach (var searchItem in searchItems)
                {
                    var searchName = searchItem.name;
                    var value = searchItem.value;
                    var searchConfig = value.config;
                    bool somethingFound = false;

                    CheckRunner.Run(() =>
                    {
                        int resultsCount = 0;
                        bool[] results;
                        bool isSearchFinished = false;

                        foreach (var file in value.files)
                        {
                            var fileName = file.name.ToLower();
                            var fileSettings = file.value;

                            results = Search(files, fileName, fileSettings, ref resultsCount, searchName, somethingFound);

                            isSearchFinished = results[0];
                            somethingFound = results[1];

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

        private static void PrintYAMLRegexesSearchFiles()
        {
            try
            {
                //List<string> extra_no_extensions = new List<string>() { ".msi", ".exe", ".dll", ".pyc", ".pyi", ".lnk", ".css", ".hyb", ".etl", ".mo", ".xrm-ms", ".idl", ".vsix", ".mui", ".qml", ".tt" };

                List<string> valid_extensions = new List<string>() {
                    // text
                    ".txt", ".text", ".md", ".markdown", ".toml", ".rtf",

                    // config
                    ".cnf", ".conf", ".config", ".json", ".yml", ".yaml", ".xml", ".xaml", 

                    // dev
                    ".py", ".js", ".html", ".c", ".cpp", ".pl", ".rb", ".smali", ".java", ".php", ".bat", ".ps1",

                    // hidden
                    ".id_rsa", ".id_dsa", ".bash_history", ".rsa",
                };

                List<string> invalid_names = new List<string>()
                {
                    "eula.rtf", "changelog.md"
                };

                if (Checks.IsDebug)
                    Beaprint.PrintDebugLine("Looking for secrets inside files via regexes");

                // No dirs, less than 1MB, only interesting extensions and not false positives files.
                var files = InitializeFileSearch(Checks.SearchProgramFiles).Where(f => !f.IsDirectory && valid_extensions.Contains(f.Extension.ToLower()) && !invalid_names.Contains(f.Filename.ToLower()) && f.Size > 0 && f.Size < Checks.MaxRegexFileSize).ToList();
                var config = Checks.RegexesYamlConfig; // Get yaml info
                Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> foundRegexes = new Dictionary<string, Dictionary<string, Dictionary<string, List<string>>>> { };

                if (Checks.IsDebug)
                {
                    Beaprint.PrintDebugLine($"Searching regexes in {files.Count} files");
                    valid_extensions.ForEach(ext =>
                    {
                        int cont = 0;
                        files.ForEach(f =>
                        {
                            if (f.Extension.ToLower() == ext.ToLower())
                                cont++;
                        });
                        Beaprint.PrintDebugLine($"Found {cont} files with ext {ext}");
                    });

                }

                /*
                 * Useful for debbugging purposes to see the common file extensions found
                Dictionary <string, int> dict_str = new Dictionary<string, int>();
                foreach (var f in files)
                {
                    if (dict_str.ContainsKey(f.Extension))
                        dict_str[f.Extension] += 1;
                    else
                        dict_str[f.Extension] = 1;
                }

                var sortedDict = from entry in dict_str orderby entry.Value descending select entry;

                foreach (KeyValuePair<string, int> kvp in sortedDict)
                {
                    Console.WriteLine(string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value));
                }*/

                //double pb = 0;
                //using (var progress = new ProgressBar())
                //{
                //    CheckRunner.Run(() =>
                //    {
                //        int num_threads = 8;
                //        try
                //        {
                //            num_threads = Environment.ProcessorCount;
                //        }
                //        catch (Exception ex) { }

                //        Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = num_threads }, f =>
                //        {

                //            foreach (var regex_obj in config.regular_expresions)
                //            {
                //                foreach (var regex in regex_obj.regexes)
                //                {
                //                    if (regex.disable != null && regex.disable.ToLower().Contains("winpeas"))
                //                    {
                //                        continue;
                //                    }

                //                    List<string> results = new List<string> { };

                //                    var timer = new Stopwatch();
                //                    if (Checks.IsDebug)
                //                    {
                //                        timer.Start();
                //                    }


                //                    try
                //                    {
                //                        string text = File.ReadAllText(f.FullPath);

                //                        results = SearchContent(text, regex.regex, (bool)regex.caseinsensitive);
                //                        if (results.Count > 0)
                //                        {
                //                            if (!foundRegexes.ContainsKey(regex_obj.name)) foundRegexes[regex_obj.name] = new Dictionary<string, Dictionary<string, List<string>>> { };
                //                            if (!foundRegexes[regex_obj.name].ContainsKey(regex.name)) foundRegexes[regex_obj.name][regex.name] = new Dictionary<string, List<string>> { };

                //                            foundRegexes[regex_obj.name][regex.name][f.FullPath] = results;
                //                        }
                //                    }
                //                    catch (System.IO.IOException)
                //                    {
                //                        // Cannot read the file
                //                    }

                //                    if (Checks.IsDebug)
                //                    {
                //                        timer.Stop();

                //                        TimeSpan timeTaken = timer.Elapsed;
                //                        if (timeTaken.TotalMilliseconds > 20000)
                //                            Beaprint.PrintDebugLine($"\nThe regex {regex.regex} took {timeTaken.TotalMilliseconds}s in {f.FullPath}");
                //                    }
                //                }
                //            }
                //            pb += (double)100 / files.Count;
                //            progress.Report(pb / 100); //Value must be in [0..1] range
                //        });
                //    }, Checks.IsDebug);
                //}


                double pb = 0;
                using (var progress = new ProgressBar())
                {
                    CheckRunner.Run(() =>
                    {
                        int num_threads = 8;
                        try
                        {
                            num_threads = Environment.ProcessorCount;
                        }
                        catch (Exception ex) { }

                        Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = num_threads }, f =>
                        {
                            foreach (var regex_obj in config.regular_expresions)
                            {
                                foreach (var regex in regex_obj.regexes)
                                {
                                    if (regex.disable != null && regex.disable.ToLower().Contains("winpeas"))
                                    {
                                        continue;
                                    }

                                    Dictionary<string, List<string>> fileResults = new Dictionary<string, List<string>>();

                                    var timer = new Stopwatch();
                                    if (Checks.IsDebug)
                                    {
                                        timer.Start();
                                    }

                                    try
                                    {
                                        using (StreamReader sr = new StreamReader(f.FullPath))
                                        {
                                            string line;
                                            while ((line = sr.ReadLine()) != null)
                                            {
                                                List<string> results = SearchContent(line, regex.regex, (bool)regex.caseinsensitive);
                                                if (results.Count > 0)
                                                {
                                                    if (!fileResults.ContainsKey(f.FullPath))
                                                    {
                                                        fileResults[f.FullPath] = new List<string>();
                                                    }
                                                    fileResults[f.FullPath].AddRange(results);
                                                }
                                            }
                                        }

                                        if (fileResults.Count > 0)
                                        {
                                            if (!foundRegexes.ContainsKey(regex_obj.name)) foundRegexes[regex_obj.name] = new Dictionary<string, Dictionary<string, List<string>>> { };
                                            if (!foundRegexes[regex_obj.name].ContainsKey(regex.name)) foundRegexes[regex_obj.name][regex.name] = new Dictionary<string, List<string>> { };

                                            foundRegexes[regex_obj.name][regex.name] = fileResults;
                                        }
                                    }
                                    catch (System.IO.IOException)
                                    {
                                        // Cannot read the file
                                    }

                                    if (Checks.IsDebug)
                                    {
                                        timer.Stop();

                                        TimeSpan timeTaken = timer.Elapsed;
                                        if (timeTaken.TotalMilliseconds > 10000)
                                            Beaprint.PrintDebugLine($"\nThe regex {regex.regex} took {timeTaken.TotalMilliseconds}ms in {f.FullPath}");
                                    }
                                }
                            }
                            pb += (double)100 / files.Count;
                            progress.Report(pb / 100); //Value must be in [0..1] range
                        });
                    }, Checks.IsDebug);
                }


                // Print results
                foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, List<string>>>> item in foundRegexes)
                {
                    foreach (KeyValuePair<string, Dictionary<string, List<string>>> item2 in item.Value)
                    {
                        string masterCategory = item.Key;
                        string regexCategory = item2.Key;
                        int limit = 70;

                        string msg = $"Found {masterCategory}-{regexCategory} Regexes";
                        if (item2.Value.Count > limit)
                            msg += $" (limited to {limit})";

                        Beaprint.MainPrint(msg);

                        int cont = 0;
                        foreach (KeyValuePair<string, List<string>> item3 in item2.Value)
                        {
                            if (cont > limit)
                                break;

                            foreach (string regexMatch in item3.Value)
                            {
                                string filePath = item3.Key;
                                Beaprint.PrintNoNL($"{filePath}: ");
                                Beaprint.BadPrint(regexMatch);
                            }

                            cont++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Beaprint.GrayPrint($"Error looking for regexes inside files: {e}");
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

            // If contains undesireable string, stop processing
            if (fileSettings.remove_path != null && fileSettings.remove_path.Length > 0)
            {
                foreach (var rem_path in fileSettings.remove_path.Split('|'))
                {
                    if (fileInfo.FullPath.ToLower().Contains(rem_path.ToLower()))
                        return false;
                }
            }

            if (fileSettings.type == "f")
            {
                var colors = new Dictionary<string, string>
                {
                    { fileInfo.Filename, Beaprint.ansi_color_bad }
                };
                Beaprint.AnsiPrint($"File: {fileInfo.FullPath}", colors);

                if (!(bool)fileSettings.just_list_file)
                {
                    GrepResult(fileInfo, fileSettings);
                }
            }
            else if (fileSettings.type == "d")
            {
                var colors = new Dictionary<string, string>
                {
                    { fileInfo.Filename, Beaprint.ansi_color_bad }
                };
                Beaprint.AnsiPrint($"Folder: {fileInfo.FullPath}", colors);

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

            if (content.Length > 0)
                Console.WriteLine();
        }

        private static string SanitizeLineGrep(string lineGrep)
        {
            // sanitize the string, e.g.
            // '-i -a -o "description.*" | sort | uniq'
            // - remove everything except from "description.*"

            Regex regex;
            if (lineGrep.Contains("-i"))
            {
                regex = new Regex("\"([^\"]+)\"", RegexOptions.IgnoreCase);
            }
            else
            {
                regex = new Regex("\"([^\"]+)\"");
            }

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
