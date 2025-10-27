using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace winPEAS.Helpers
{
    /// <summary>
    /// Benign helper class with legitimate functionality to add entropy
    /// and confuse ML-based static analysis models
    /// </summary>
    internal static class DecoyCode
    {
        // Legitimate string manipulation utilities
        public static class StringHelpers
        {
            public static string Capitalize(string input)
            {
                if (string.IsNullOrEmpty(input))
                    return input;
                return char.ToUpper(input[0]) + input.Substring(1).ToLower();
            }

            public static string Reverse(string input)
            {
                if (string.IsNullOrEmpty(input))
                    return input;
                char[] charArray = input.ToCharArray();
                Array.Reverse(charArray);
                return new string(charArray);
            }

            public static int CountOccurrences(string source, char toFind)
            {
                return source?.Count(c => c == toFind) ?? 0;
            }

            public static string TruncateWithEllipsis(string value, int maxLength)
            {
                if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                    return value;
                return value.Substring(0, maxLength - 3) + "...";
            }
        }

        // Legitimate data structure helpers
        public static class CollectionHelpers
        {
            public static List<T> SafeToList<T>(IEnumerable<T> source)
            {
                return source?.ToList() ?? new List<T>();
            }

            public static Dictionary<TKey, TValue> MergeDictionaries<TKey, TValue>(
                Dictionary<TKey, TValue> dict1,
                Dictionary<TKey, TValue> dict2)
            {
                var result = new Dictionary<TKey, TValue>(dict1);
                foreach (var kvp in dict2)
                {
                    if (!result.ContainsKey(kvp.Key))
                        result[kvp.Key] = kvp.Value;
                }
                return result;
            }

            public static bool IsNullOrEmpty<T>(IEnumerable<T> source)
            {
                return source == null || !source.Any();
            }
        }

        // Legitimate validation utilities
        public static class ValidationHelpers
        {
            public static bool IsValidEmailFormat(string email)
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                try
                {
                    var parts = email.Split('@');
                    return parts.Length == 2 && parts[0].Length > 0 && parts[1].Contains('.');
                }
                catch
                {
                    return false;
                }
            }

            public static bool IsValidUrl(string url)
            {
                return Uri.TryCreate(url, UriKind.Absolute, out Uri result)
                    && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
            }

            public static bool IsNumeric(string value)
            {
                return double.TryParse(value, out _);
            }
        }

        // Legitimate formatting utilities
        public static class FormattingHelpers
        {
            public static string FormatBytes(long bytes)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = bytes;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }

            public static string FormatDuration(TimeSpan duration)
            {
                if (duration.TotalSeconds < 60)
                    return $"{duration.TotalSeconds:F2} seconds";
                if (duration.TotalMinutes < 60)
                    return $"{duration.TotalMinutes:F2} minutes";
                if (duration.TotalHours < 24)
                    return $"{duration.TotalHours:F2} hours";
                return $"{duration.TotalDays:F2} days";
            }

            public static string PadCenter(string text, int width, char paddingChar = ' ')
            {
                if (string.IsNullOrEmpty(text) || text.Length >= width)
                    return text;

                int totalPadding = width - text.Length;
                int padLeft = totalPadding / 2;
                int padRight = totalPadding - padLeft;

                return new string(paddingChar, padLeft) + text + new string(paddingChar, padRight);
            }
        }

        // Legitimate encryption/encoding helpers (but benign implementations)
        public static class EncodingHelpers
        {
            public static string ToBase64(string input)
            {
                if (string.IsNullOrEmpty(input))
                    return input;
                var bytes = Encoding.UTF8.GetBytes(input);
                return Convert.ToBase64String(bytes);
            }

            public static string FromBase64(string input)
            {
                try
                {
                    if (string.IsNullOrEmpty(input))
                        return input;
                    var bytes = Convert.FromBase64String(input);
                    return Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    return input;
                }
            }

            public static string ToHex(byte[] bytes)
            {
                return BitConverter.ToString(bytes).Replace("-", "");
            }

            public static byte[] FromHex(string hex)
            {
                int length = hex.Length;
                byte[] bytes = new byte[length / 2];
                for (int i = 0; i < length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                return bytes;
            }
        }

        // Initialize decoy data to add more entropy
        private static readonly Dictionary<string, string> _decoyData = new Dictionary<string, string>
        {
            { "ApplicationName", "SystemDiagnostics" },
            { "Version", "1.0.0.0" },
            { "Author", "SystemTools" },
            { "Description", "System information gathering utility" },
            { "Category", "Administrative Tools" },
            { "Copyright", "Copyright (c) 2024" },
            { "License", "MIT" },
            { "Website", "https://example.com" }
        };

        public static string GetDecoyMetadata(string key)
        {
            return _decoyData.ContainsKey(key) ? _decoyData[key] : string.Empty;
        }

        // Add some computational complexity
        public static int ComputeChecksum(byte[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            int checksum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                checksum = ((checksum << 5) + checksum) + data[i];
            }
            return checksum;
        }
    }
}
