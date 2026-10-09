using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace winPEAS.Info.ApplicationInfo
{
    internal enum ConfigFileState { Absent, Readable, Denied, TooLarge, Malformed, Unknown }

    internal sealed class LansweeperConfigResult
    {
        internal ConfigFileState ConfigState;
        internal ConfigFileState KeyState;
        internal bool ProtectedConnectionStrings;
        internal bool Partial;

        internal bool Candidate => ConfigState == ConfigFileState.Readable &&
            KeyState == ConfigFileState.Readable && ProtectedConnectionStrings;
    }

    internal static class LansweeperConfigMetadata
    {
        internal const int MaxConfigBytes = 65536;
        internal const int MaxProbeMilliseconds = 300;
        private const string InstallName = "Lansweeper";

        internal static IEnumerable<string> CandidateInstallPaths(string programFiles, string programFilesX86)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in new[] { programFiles, programFilesX86 })
            {
                if (string.IsNullOrWhiteSpace(root) || !Path.IsPathRooted(root) ||
                    root.StartsWith(@"\\", StringComparison.Ordinal) || root.StartsWith("//", StringComparison.Ordinal))
                    continue;
                string fullRoot;
                try { fullRoot = Path.GetFullPath(root); }
                catch (Exception) { continue; }
                try
                {
                    if (new DriveInfo(Path.GetPathRoot(fullRoot)).DriveType != DriveType.Fixed) continue;
                }
                catch (Exception) { continue; }
                if (!IsOrdinaryRoot(fullRoot)) continue;
                string install = Path.Combine(fullRoot, InstallName);
                if (seen.Add(install)) yield return install;
            }
        }

        private static bool IsOrdinaryRoot(string root)
        {
            try
            {
                for (string current = root; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                    string parent = Path.GetDirectoryName(current);
                    if (parent == current) break;
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        internal static IEnumerable<LansweeperConfigResult> Collect()
        {
            string programFiles = Environment.GetEnvironmentVariable("ProgramW6432")
                ?? Environment.GetEnvironmentVariable("ProgramFiles")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            foreach (string install in CandidateInstallPaths(programFiles, programFilesX86))
            {
                LansweeperConfigResult result = ProbeInstallCore(install, File.GetAttributes,
                    path => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
                if (result != null) yield return result;
            }
        }

        // File operations are injected so the probe can be tested without an installed application.
        internal static LansweeperConfigResult ProbeInstallCore(string install,
            Func<string, FileAttributes> getAttributes, Func<string, Stream> openRead)
        {
            var watch = Stopwatch.StartNew();
            if (GetPathState(install, true, getAttributes) != ConfigFileState.Readable) return null;
            var result = new LansweeperConfigResult();
            string website = Path.Combine(install, "Website");
            string key = Path.Combine(install, "Key");
            string configPath = Path.Combine(website, "web.config");
            string keyPath = Path.Combine(key, "Encryption.txt");
            result.ConfigState = ProbeConfig(website, configPath, getAttributes, openRead, watch,
                out result.ProtectedConnectionStrings);
            result.KeyState = ProbeKey(key, keyPath, getAttributes, openRead, watch);
            result.Partial = watch.ElapsedMilliseconds > MaxProbeMilliseconds ||
                result.ConfigState == ConfigFileState.Unknown || result.KeyState == ConfigFileState.Unknown;
            return result;
        }

        private static ConfigFileState GetPathState(string path, bool directory,
            Func<string, FileAttributes> getAttributes)
        {
            try
            {
                FileAttributes attributes = getAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0 ||
                    ((attributes & FileAttributes.Directory) != 0) != directory)
                    return ConfigFileState.Unknown;
                return ConfigFileState.Readable;
            }
            catch (FileNotFoundException) { return ConfigFileState.Absent; }
            catch (DirectoryNotFoundException) { return ConfigFileState.Absent; }
            catch (UnauthorizedAccessException) { return ConfigFileState.Denied; }
            catch (Exception) { return ConfigFileState.Unknown; }
        }

        private static ConfigFileState ProbeConfig(string folder, string path,
            Func<string, FileAttributes> getAttributes, Func<string, Stream> openRead,
            Stopwatch watch, out bool protectedSection)
        {
            protectedSection = false;
            if (watch.ElapsedMilliseconds > MaxProbeMilliseconds) return ConfigFileState.Unknown;
            ConfigFileState folderState = GetPathState(folder, true, getAttributes);
            if (folderState != ConfigFileState.Readable) return folderState;
            ConfigFileState fileState = GetPathState(path, false, getAttributes);
            if (fileState != ConfigFileState.Readable) return fileState;
            try
            {
                using (Stream stream = openRead(path))
                {
                    if (!stream.CanRead) return ConfigFileState.Denied;
                    if (stream.Length > MaxConfigBytes || stream.Length < 0) return ConfigFileState.TooLarge;
                    byte[] bytes = new byte[(int)stream.Length];
                    int count = 0;
                    while (count < bytes.Length)
                    {
                        if (watch.ElapsedMilliseconds > MaxProbeMilliseconds) return ConfigFileState.Unknown;
                        int read = stream.Read(bytes, count, bytes.Length - count);
                        if (read == 0) return ConfigFileState.Unknown;
                        count += read;
                    }
                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                        MaxCharactersInDocument = MaxConfigBytes,
                        MaxCharactersFromEntities = 0
                    };
                    bool found = false;
                    using (XmlReader reader = XmlReader.Create(new MemoryStream(bytes, false), settings))
                    {
                        while (reader.Read())
                        {
                            if (watch.ElapsedMilliseconds > MaxProbeMilliseconds) return ConfigFileState.Unknown;
                            if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1 &&
                                reader.LocalName == "connectionStrings" && reader.NamespaceURI.Length == 0)
                                found = !string.IsNullOrEmpty(reader.GetAttribute("configProtectionProvider"));
                        }
                    }
                    if (watch.ElapsedMilliseconds > MaxProbeMilliseconds) return ConfigFileState.Unknown;
                    protectedSection = found;
                    return ConfigFileState.Readable;
                }
            }
            catch (XmlException) { return ConfigFileState.Malformed; }
            catch (UnauthorizedAccessException) { return ConfigFileState.Denied; }
            catch (Exception) { return ConfigFileState.Unknown; }
        }

        private static ConfigFileState ProbeKey(string folder, string path,
            Func<string, FileAttributes> getAttributes, Func<string, Stream> openRead, Stopwatch watch)
        {
            if (watch.ElapsedMilliseconds > MaxProbeMilliseconds) return ConfigFileState.Unknown;
            ConfigFileState folderState = GetPathState(folder, true, getAttributes);
            if (folderState != ConfigFileState.Readable) return folderState;
            ConfigFileState fileState = GetPathState(path, false, getAttributes);
            if (fileState != ConfigFileState.Readable) return fileState;
            try
            {
                using (Stream stream = openRead(path))
                {
                    if (!stream.CanRead) return ConfigFileState.Denied;
                    return watch.ElapsedMilliseconds <= MaxProbeMilliseconds
                        ? ConfigFileState.Readable : ConfigFileState.Unknown;
                }
            }
            catch (UnauthorizedAccessException) { return ConfigFileState.Denied; }
            catch (Exception) { return ConfigFileState.Unknown; }
        }
    }
}
