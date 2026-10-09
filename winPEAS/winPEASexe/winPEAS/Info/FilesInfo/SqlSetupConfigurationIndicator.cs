using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using winPEAS.Helpers;

namespace winPEAS.Info.FilesInfo
{
    internal sealed class SqlSetupCandidateInventory
    {
        internal readonly List<CustomFileInfo> Files = new List<CustomFileInfo>();
        internal bool Partial;
    }

    internal sealed class SqlSetupMarkerAssessment
    {
        internal readonly List<string> Markers = new List<string>();
        internal bool Partial;
        internal string Reason;
    }

    // This helper feeds the existing YAML file search; it never runs SQL or prints INI values.
    internal static class SqlSetupConfigurationIndicator
    {
        internal const int MaxRootDirectories = 12;
        internal const int MaxChildDirectories = 24;
        internal const int MaxConfigReads = 12;
        internal const int MaxReadBytes = 65536;
        internal const int MaxFileBytes = 262144;
        private const int MaxProbeMilliseconds = 600;
        private const int MaxTextLines = 1000;
        private const int MaxLineLength = 4096;
        private static readonly string[] ConfigNames = { "sql-Configuration.INI", "ConfigurationFile.ini" };
        private static readonly HashSet<string> CredentialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SAPWD", "SQLSVCPASSWORD", "AGTSVCPASSWORD", "ASSVCPASSWORD",
            "ISSVCPASSWORD", "RSSVCPASSWORD"
        };

        internal static bool IsSetupConfigName(string name)
        {
            return ConfigNames.Any(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
        }

        internal static bool IsFixedDriveRoot(string root)
        {
            return !string.IsNullOrEmpty(root) && root.Length == 3 &&
                ((root[0] >= 'A' && root[0] <= 'Z') || (root[0] >= 'a' && root[0] <= 'z')) &&
                root[1] == ':' && root[2] == '\\';
        }

        internal static SqlSetupCandidateInventory ScanDriveRoot(string root)
        {
            var inventory = new SqlSetupCandidateInventory();
            if (!IsFixedDriveRoot(root)) return inventory;
            try
            {
                if (new DriveInfo(root).DriveType != DriveType.Fixed) return inventory;
                var timer = Stopwatch.StartNew();
                int rootCount = 0, childCount = 0;
                foreach (string sqlDirectory in Directory.EnumerateDirectories(root, "SQL*", SearchOption.TopDirectoryOnly))
                {
                    if (timer.ElapsedMilliseconds >= MaxProbeMilliseconds || rootCount++ >= MaxRootDirectories)
                    {
                        inventory.Partial = true;
                        break;
                    }
                    if (!IsPlainDirectory(sqlDirectory)) continue;
                    AddConfigFiles(sqlDirectory, inventory);
                    try
                    {
                        foreach (string child in Directory.EnumerateDirectories(sqlDirectory, "*", SearchOption.TopDirectoryOnly))
                        {
                            if (timer.ElapsedMilliseconds >= MaxProbeMilliseconds || childCount++ >= MaxChildDirectories)
                            {
                                inventory.Partial = true;
                                break;
                            }
                            if (IsPlainDirectory(child)) AddConfigFiles(child, inventory);
                        }
                    }
                    catch (Exception) { inventory.Partial = true; }
                    if (childCount >= MaxChildDirectories)
                    {
                        inventory.Partial = true;
                        break;
                    }
                }
            }
            catch (Exception) { inventory.Partial = true; }
            return inventory;
        }

        private static bool IsPlainDirectory(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                return (attributes & FileAttributes.Directory) != 0 &&
                    (attributes & FileAttributes.ReparsePoint) == 0;
            }
            catch (Exception) { return false; }
        }

        private static void AddConfigFiles(string directory, SqlSetupCandidateInventory inventory)
        {
            foreach (string name in ConfigNames)
            {
                string path = Path.Combine(directory, name);
                try
                {
                    var file = new FileInfo(path);
                    if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    inventory.Files.Add(new CustomFileInfo(file.Name, file.Extension, file.FullName, file.Length, false));
                }
                catch (Exception) { inventory.Partial = true; }
            }
        }

        internal static SqlSetupMarkerAssessment Inspect(string path, long length)
        {
            var result = new SqlSetupMarkerAssessment();
            if (string.IsNullOrWhiteSpace(path) || length < 0 || length > MaxFileBytes)
            {
                result.Partial = true;
                result.Reason = "missing or oversized file";
                return result;
            }
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                {
                    result.Partial = true;
                    result.Reason = "reparse point";
                    return result;
                }
                byte[] buffer = new byte[MaxReadBytes];
                int count = 0;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan))
                {
                    while (count < buffer.Length)
                    {
                        int read = stream.Read(buffer, count, buffer.Length - count);
                        if (read == 0) break;
                        count += read;
                    }
                    result.Partial = stream.Length > count;
                }
                Encoding encoding = Encoding.UTF8;
                int start = 0;
                if (count >= 2 && buffer[0] == 0xff && buffer[1] == 0xfe)
                {
                    encoding = Encoding.Unicode;
                    start = 2;
                }
                else if (count >= 2 && buffer[0] == 0xfe && buffer[1] == 0xff)
                {
                    encoding = Encoding.BigEndianUnicode;
                    start = 2;
                }
                else if (count >= 3 && buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                    start = 3;
                string text = encoding.GetString(buffer, start, count - start);
                if (text.IndexOf('\0') >= 0)
                {
                    result.Partial = true;
                    result.Reason = "binary or unsupported encoding";
                    return result;
                }
                result.Markers.AddRange(FindPopulatedCredentialKeys(text));
                if (text.Count(c => c == '\n') >= MaxTextLines) result.Partial = true;
                if (result.Partial) result.Reason = "read or line limit reached";
            }
            catch (Exception)
            {
                result.Partial = true;
                result.Reason = "unreadable";
            }
            return result;
        }

        internal static List<string> FindPopulatedCredentialKeys(string text)
        {
            var markers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) return new List<string>();
            int lines = 0;
            foreach (string raw in text.Split('\n'))
            {
                if (++lines > MaxTextLines) break;
                if (raw.Length > MaxLineLength) continue;
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                int equal = line.IndexOf('=');
                if (equal < 1) continue;
                string key = line.Substring(0, equal).Trim();
                if (!CredentialKeys.Contains(key)) continue;
                string value = line.Substring(equal + 1).Trim();
                if (value.Length >= 2 && (value[0] == '"' || value[0] == '\''))
                {
                    int close = value.IndexOf(value[0], 1);
                    if (close > 0) value = value.Substring(1, close - 1).Trim();
                }
                if (value.Length == 0 || value.All(c => c == '*') ||
                    value.Equals("REDACTED", StringComparison.OrdinalIgnoreCase) ||
                    (value.StartsWith("<", StringComparison.Ordinal) && value.EndsWith(">", StringComparison.Ordinal)))
                    continue;
                markers.Add(key.ToUpperInvariant());
            }
            return markers.OrderBy(key => key, StringComparer.Ordinal).ToList();
        }
    }
}
