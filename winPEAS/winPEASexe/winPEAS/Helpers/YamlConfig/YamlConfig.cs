using static winPEAS.Helpers.YamlConfig.YamlConfig.SearchParameters;

namespace winPEAS.Helpers.YamlConfig
{
    public class YamlRegexConfig
    {
        public class RegularExpressions
        {
            public string name { get; set; }
            public RegularExpression[] regexes { get; set; }
            public class RegularExpression
            {
                public string name { get; set; }
                public string regex { get; set; }

                public bool caseinsensitive { get; set; }

                public string disable { get; set; }
            }
        }

        public RegularExpressions[] regular_expresions { get; set; }
    }
    public class YamlConfig
    {

        public class FileParam
        {
            public string name { get; set; }
            public FileSettings value { get; set; }
        }

        public class SearchParameters
        {
            public class FileSettings
            {
                public string bad_regex { get; set; }
                // public string check_extra_path { get; set;  }    // not used in Winpeas
                public string good_regex { get; set; }
                public bool? just_list_file { get; set; }
                public string line_grep { get; set; }
                public bool? only_bad_lines { get; set; }
                public bool? remove_empty_lines { get; set; }
                // public string remove_path { get; set;  }     // not used in Winpeas
                public string remove_regex { get; set; }
                public string remove_path { get; set; }
                // public string[] search_in { get; set;  }   // not used in Winpeas
                public string type { get; set; }
                public FileParam[] files { get; set; }
            }

            public class FileParameters
            {
                public string file { get; set; }
                public FileSettings options { get; set; }
            }

            public class Config
            {
                public bool auto_check { get; set; }
            }

            public Config config { get; set; }
            public string[] disable { get; set; }       // disabled scripts - linpeas/winpeas
            public FileParam[] files { get; set; }
        }

        public class SearchParams
        {
            public string name { get; set; }
            public SearchParameters value { get; set; }
        }

        public class Defaults
        {
            public bool auto_check { get; set; }
            public string bad_regex { get; set; }
            //public string check_extra_path { get; set;  }  not used in winpeas
            public string good_regex { get; set; }
            public bool just_list_file { get; set; }
            public string line_grep { get; set; }
            public bool only_bad_lines { get; set; }
            public bool remove_empty_lines { get; set; }
            public string remove_path { get; set; }
            public string remove_regex { get; set; }
            public string[] search_in { get; set; }
            public string type { get; set; }
        }

        public class Variable
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public SearchParams[] search { get; set; }

        public Defaults defaults { get; set; }

        public Variable[] variables { get; set; }
    }
}
