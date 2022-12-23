using System.Collections.Generic;

namespace winPEAS.Helpers.Search
{
    static class Patterns
    {
        public static readonly HashSet<string> WhitelistExtensions = new HashSet<string>()
        {
            ".cer",
            ".csr",
            ".der",
            ".p12",
        };

        public static readonly HashSet<string> WhiteListExactfilenamesWithExtensions = new HashSet<string>()
        {
            "docker-compose.yml",
            "dockerfile",
        };

        public static readonly IList<string> WhiteListRegexp = new List<string>()
        {
            "config.*\\.php$",
        };
    }
}
