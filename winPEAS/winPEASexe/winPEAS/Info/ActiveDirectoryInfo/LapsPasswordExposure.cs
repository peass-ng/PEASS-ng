using System.Text;

namespace winPEAS.Info.ActiveDirectoryInfo
{
    internal sealed class LapsPasswordExposureReport
    {
        public bool LegacyPasswordReadable { get; set; }
        public bool WindowsLapsPasswordReadable { get; set; }
        public bool IsExposed => LegacyPasswordReadable || WindowsLapsPasswordReadable;
    }

    internal static class LapsPasswordExposure
    {
        internal static LapsPasswordExposureReport Evaluate(bool legacyPasswordReturned, bool windowsLapsPasswordReturned)
        {
            return new LapsPasswordExposureReport
            {
                LegacyPasswordReadable = legacyPasswordReturned,
                WindowsLapsPasswordReadable = windowsLapsPasswordReturned
            };
        }

        internal static string EscapeLdapFilterValue(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var escaped = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\':
                        escaped.Append(@"\5c");
                        break;
                    case '*':
                        escaped.Append(@"\2a");
                        break;
                    case '(':
                        escaped.Append(@"\28");
                        break;
                    case ')':
                        escaped.Append(@"\29");
                        break;
                    case '\0':
                        escaped.Append(@"\00");
                        break;
                    default:
                        escaped.Append(character);
                        break;
                }
            }

            return escaped.ToString();
        }
    }
}
