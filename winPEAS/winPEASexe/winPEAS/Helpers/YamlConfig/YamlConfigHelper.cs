using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Yaml.Serialization;
using static winPEAS.Helpers.YamlConfig.YamlConfig;


namespace winPEAS.Helpers.YamlConfig
{
    internal class YamlConfigHelper
    {
        const string REGEXES_FILES = "regexes.yaml";
        const string SENSITIVE_FILES = "sensitive_files.yaml";

        public static YamlRegexConfig GetRegexesSearchConfig()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Where(i => i.EndsWith(REGEXES_FILES)).FirstOrDefault();

            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string configFileContent = reader.ReadToEnd();

                    YamlSerializer yamlSerializer = new YamlSerializer();
                    var yamlConfigObject = yamlSerializer.Deserialize(configFileContent, typeof(YamlRegexConfig));

                    if (yamlConfigObject == null || yamlConfigObject.Length == 0)
                    {
                        throw new Exception($"Config '{resourceName}' is empty, check the config for more information");
                    }

                    YamlRegexConfig yamlConfig = (YamlRegexConfig)yamlConfigObject[0];
                    // check
                    if (yamlConfig.regular_expresions == null || yamlConfig.regular_expresions.Length == 0)
                    {
                        throw new System.Exception("No configuration was read");
                    }

                    return yamlConfig;

                }
            }
            catch (System.Exception e)
            {
                Beaprint.PrintException($"An exception occurred while parsing regexes.yaml configuration file: {e.Message}");

                throw;
            }
        }

        public static YamlConfig GetWindowsSearchConfig()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Where(i => i.EndsWith(SENSITIVE_FILES)).FirstOrDefault();

            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string configFileContent = reader.ReadToEnd();

                    YamlSerializer yamlSerializer = new YamlSerializer();
                    YamlConfig yamlConfig = (YamlConfig)yamlSerializer.Deserialize(configFileContent, typeof(YamlConfig))[0];

                    // update variables
                    foreach (var variable in yamlConfig.variables)
                    {
                        configFileContent = configFileContent.Replace($"${variable.name}", variable.value);
                    }

                    // deserialize again
                    yamlConfig = (YamlConfig)yamlSerializer.Deserialize(configFileContent, typeof(YamlConfig))[0];

                    // check
                    if (yamlConfig.defaults == null || yamlConfig.search == null || yamlConfig.search.Length == 0)
                    {
                        throw new System.Exception("No configuration was read");
                    }

                    // apply the defaults e.g. for filesearch
                    foreach (var searchItem in yamlConfig.search)
                    {
                        SetDefaultOptions(searchItem, yamlConfig.defaults);
                    }

                    return yamlConfig;
                }
            }
            catch (System.Exception e)
            {
                Beaprint.PrintException($"An exception occured while parsing sensitive_files.yaml configuration file: {e.Message}");

                throw;
            }
        }

        private static void SetDefaultOptions(SearchParams searchItem, Defaults defaults)
        {
            searchItem.value.config.auto_check = GetValueOrDefault(searchItem.value.config.auto_check, defaults.auto_check);

            SetFileOptions(searchItem.value.files, defaults);
        }

        private static void SetFileOptions(FileParam[] fileParams, Defaults defaults)
        {
            foreach (var fileParam in fileParams)
            {
                var value = fileParam.value;

                value.bad_regex = GetValueOrDefault(value.bad_regex, defaults.bad_regex);
                value.good_regex = GetValueOrDefault(value.good_regex, defaults.good_regex);
                value.just_list_file = GetValueOrDefault(value.just_list_file, defaults.just_list_file);
                value.line_grep = GetValueOrDefault(value.line_grep, defaults.line_grep);
                value.only_bad_lines = GetValueOrDefault(value.only_bad_lines, defaults.only_bad_lines);
                value.remove_empty_lines = GetValueOrDefault(value.remove_empty_lines, defaults.remove_empty_lines);
                value.remove_regex = GetValueOrDefault(value.remove_regex, defaults.remove_regex);
                value.remove_path = GetValueOrDefault(value.remove_path, defaults.remove_path);
                value.type = GetValueOrDefault(value.type, defaults.type).ToLower();

                if (value.files != null)
                {
                    SetFileOptions(value.files, defaults);
                }
            }
        }

        //private static WildcardOptions GetWildCardOptions(string str)
        //{
        //    if (!str.Contains("*")) return WildcardOptions.None;
        //    if (str.StartsWith("*")) return WildcardOptions.StartsWith;
        //    if (str.EndsWith("*")) return WildcardOptions.EndsWith;

        //    return WildcardOptions.Middle;
        //}

        private static T GetValueOrDefault<T>(T val, T defaultValue)
        {
            return val == null ? defaultValue : val;
        }

        private static T GetValueOrDefault<T>(Dictionary<object, object> dict, string key, T defaultValue)
        {
            return dict.ContainsKey(key) ? (T)dict[key] : defaultValue;
        }
    }
}

