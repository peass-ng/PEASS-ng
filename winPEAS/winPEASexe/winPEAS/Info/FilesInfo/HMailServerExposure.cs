using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using winPEAS.Helpers;

namespace winPEAS.Info.FilesInfo
{
    internal enum HMailReadState { Unknown, Accessible }

    internal sealed class HMailDatabaseSettings
    {
        internal string Type;
        internal string Internal;
        internal string PasswordEncryption;
        internal string DatabaseFolder;
        internal bool PasswordPresent;
    }

    internal sealed class HMailExposureResult
    {
        internal string IniPath;
        internal string DatabasePath;
        internal HMailReadState IniState;
        internal HMailReadState DatabaseState;
        internal bool IsSqlCe;
        internal bool EncryptedPasswordPresent;
    }

    internal static class HMailServerExposure
    {
        private const int MaxIniBytes = 16384;
        private const int MaxProbeMilliseconds = 300;
        private const string DatabaseName = "hMailServer.sdf";

        internal static IEnumerable<string> CandidateInstallPaths(string programFiles, string programFilesX86)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in new[] { programFiles, programFilesX86 })
            {
                if (string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root)) continue;
                string install;
                try { install = Path.Combine(Path.GetFullPath(root), "hMailServer"); }
                catch (Exception) { continue; }
                if (seen.Add(install)) yield return install;
            }
        }

        internal static HMailDatabaseSettings ParseDatabaseSection(string text)
        {
            var settings = new HMailDatabaseSettings();
            bool inDatabase = false;
            bool inDirectories = false;
            using (var reader = new StringReader(text ?? ""))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("[", StringComparison.Ordinal))
                    {
                        inDatabase = line.Equals("[Database]", StringComparison.OrdinalIgnoreCase);
                        inDirectories = line.Equals("[Directories]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }
                    if ((!inDatabase && !inDirectories) || line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                    int equals = line.IndexOf('=');
                    if (equals <= 0) continue;
                    string key = line.Substring(0, equals).Trim();
                    string value = line.Substring(equals + 1).Trim();
                    if (inDirectories)
                    {
                        if (key.Equals("DatabaseFolder", StringComparison.OrdinalIgnoreCase)) settings.DatabaseFolder = value;
                        continue;
                    }
                    if (key.Equals("Type", StringComparison.OrdinalIgnoreCase)) settings.Type = value;
                    else if (key.Equals("Internal", StringComparison.OrdinalIgnoreCase)) settings.Internal = value;
                    else if (key.Equals("PasswordEncryption", StringComparison.OrdinalIgnoreCase)) settings.PasswordEncryption = value;
                    else if (key.Equals("Password", StringComparison.OrdinalIgnoreCase)) settings.PasswordPresent = value.Length != 0;
                }
            }
            return settings;
        }

        internal static string ResolveDatabaseFolder(string installPath, string configuredFolder)
        {
            try
            {
                string install = Path.GetFullPath(installPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string folder = string.IsNullOrWhiteSpace(configuredFolder) ? "Database" : configuredFolder.Trim();
                if (folder.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || folder.IndexOf('"') >= 0) return null;
                string resolved = Path.GetFullPath(Path.IsPathRooted(folder) ? folder : Path.Combine(install, folder));
                string prefix = install + Path.DirectorySeparatorChar;
                return resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? resolved : null;
            }
            catch (Exception) { return null; }
        }

        private static bool IsOrdinaryPath(string install, string path)
        {
            try
            {
                string ancestor = Path.GetFullPath(install);
                while (ancestor != null)
                {
                    if ((File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0) return false;
                    string parent = Path.GetDirectoryName(ancestor);
                    if (parent == ancestor) break;
                    ancestor = parent;
                }
                string relative = path.Substring(install.TrimEnd(Path.DirectorySeparatorChar).Length)
                    .TrimStart(Path.DirectorySeparatorChar);
                string current = install;
                foreach (string component in relative.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
                {
                    current = Path.Combine(current, component);
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        internal static HMailExposureResult ProbeInstall(string installPath)
        {
            if (!Directory.Exists(installPath)) return null;
            var result = new HMailExposureResult { IniPath = Path.Combine(installPath, "Bin", "hMailServer.ini") };
            var watch = Stopwatch.StartNew();
            try
            {
                if (!IsOrdinaryPath(installPath, result.IniPath)) return result;
                byte[] bytes;
                using (var stream = new FileStream(result.IniPath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    if (stream.Length > MaxIniBytes || stream.Length < 0 || watch.ElapsedMilliseconds > MaxProbeMilliseconds)
                        return result;
                    bytes = new byte[(int)stream.Length];
                    int count = 0;
                    while (count < bytes.Length)
                    {
                        if (watch.ElapsedMilliseconds > MaxProbeMilliseconds) return result;
                        int read = stream.Read(bytes, count, bytes.Length - count);
                        if (read == 0) return result;
                        count += read;
                    }
                }
                if (watch.ElapsedMilliseconds > MaxProbeMilliseconds) return result;
                HMailDatabaseSettings settings = ParseDatabaseSection(Encoding.Default.GetString(bytes));
                result.IniState = HMailReadState.Accessible;
                result.IsSqlCe = string.Equals(settings.Type, "MSSQLCE", StringComparison.OrdinalIgnoreCase);
                result.EncryptedPasswordPresent = settings.PasswordPresent && settings.PasswordEncryption == "1";
                if (!result.IsSqlCe) return result;
                string folder = ResolveDatabaseFolder(installPath, settings.DatabaseFolder);
                if (folder == null) return result;
                result.DatabasePath = Path.Combine(folder, DatabaseName);
                if (!IsOrdinaryPath(installPath, result.DatabasePath) || watch.ElapsedMilliseconds > MaxProbeMilliseconds)
                    return result;
                using (var stream = new FileStream(result.DatabasePath, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    if (stream.Length > 0 && stream.ReadByte() >= 0 && watch.ElapsedMilliseconds <= MaxProbeMilliseconds)
                        result.DatabaseState = HMailReadState.Accessible;
                }
            }
            catch (Exception) { /* Access and path errors are unknown, never evidence of readability. */ }
            return result;
        }

        internal static void PrintInfo()
        {
            string programFiles = Environment.GetEnvironmentVariable("ProgramW6432")
                ?? Environment.GetEnvironmentVariable("ProgramFiles")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            foreach (string install in CandidateInstallPaths(programFiles, programFilesX86))
            {
                HMailExposureResult result = ProbeInstall(install);
                if (result == null) continue;
                Beaprint.MainPrint("hMailServer configuration and SQL CE database access", "T1552.001");
                Beaprint.NoColorPrint("    INI: " + result.IniPath + " — " + result.IniState);
                Beaprint.NoColorPrint("    SQL CE DB: " + (result.DatabasePath ?? "path unknown") + " — " + result.DatabaseState);
                if (result.IniState == HMailReadState.Accessible && result.DatabaseState == HMailReadState.Accessible)
                    Beaprint.BadPrint("    Readable hMailServer database config and SQL CE DB; encrypted DB password "
                        + (result.EncryptedPasswordPresent ? "present" : "not confirmed") + "; inspect manually.");
            }
        }
    }
}
