using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace winPEAS.Info.FilesInfo
{
    internal enum ServerConfigState { Absent, ReadableWithCaKey, ReadableWithoutCaKey, Denied, TooLarge, Unknown }

    internal sealed class ServerConfigFinding
    {
        internal string Path;
        internal ServerConfigState State;
    }

    internal sealed class ServerConfigReport
    {
        internal readonly List<ServerConfigFinding> Findings = new List<ServerConfigFinding>();
        internal bool Partial;
    }

    // Only direct, local server-config candidates. No tree walk or API operation.
    internal static class VelociraptorServerConfig
    {
        internal const int MaxConfigBytes = 65536;
        internal const int MaxCandidates = 6;
        internal const int MaxProbeMilliseconds = 500;

        internal static IEnumerable<string> CandidatePaths(string programFiles, string programFilesX86, string programData)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in new[] { programFiles, programFilesX86, programData })
            {
                if (!IsLocalFixedRoot(root)) continue;
                string fullRoot;
                try { fullRoot = Path.GetFullPath(root); }
                catch (Exception) { continue; }
                foreach (string directory in new[] { "VelociraptorServer", "Velociraptor" })
                {
                    string candidate = Path.Combine(fullRoot, directory, "server.config.yaml");
                    if (seen.Add(candidate)) yield return candidate;
                }
            }
        }

        private static bool IsLocalFixedRoot(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) ||
                    path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
                    return false;
                string root = Path.GetPathRoot(Path.GetFullPath(path));
                return !string.IsNullOrEmpty(root) && new DriveInfo(root).DriveType == DriveType.Fixed;
            }
            catch (Exception) { return false; }
        }

        internal static ServerConfigReport Collect()
        {
            string programFiles = Environment.GetEnvironmentVariable("ProgramW6432")
                ?? Environment.GetEnvironmentVariable("ProgramFiles")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string programData = Environment.GetEnvironmentVariable("ProgramData")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return CollectCore(CandidatePaths(programFiles, programFilesX86, programData),
                File.GetAttributes,
                path => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
        }

        // Injected file operations let tests cover access errors without a server installation.
        internal static ServerConfigReport CollectCore(IEnumerable<string> candidates,
            Func<string, FileAttributes> getAttributes, Func<string, Stream> openRead)
        {
            var report = new ServerConfigReport();
            var watch = Stopwatch.StartNew();
            int inspected = 0;
            foreach (string path in candidates)
            {
                if (inspected >= MaxCandidates || watch.ElapsedMilliseconds >= MaxProbeMilliseconds)
                {
                    report.Partial = true;
                    break;
                }
                inspected++;
                ServerConfigState state = ProbeCore(path, getAttributes, openRead, watch);
                if (state != ServerConfigState.Absent)
                    report.Findings.Add(new ServerConfigFinding { Path = path, State = state });
                if (state == ServerConfigState.Unknown || state == ServerConfigState.TooLarge ||
                    state == ServerConfigState.Denied)
                    report.Partial = true;
            }
            return report;
        }

        internal static ServerConfigState ProbeCore(string path,
            Func<string, FileAttributes> getAttributes, Func<string, Stream> openRead, Stopwatch watch)
        {
            if (!IsLocalFixedRoot(path) || !string.Equals(Path.GetFileName(path), "server.config.yaml",
                    StringComparison.OrdinalIgnoreCase)) return ServerConfigState.Unknown;
            if (watch.ElapsedMilliseconds >= MaxProbeMilliseconds) return ServerConfigState.Unknown;
            try
            {
                for (string current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                {
                    FileAttributes attributes = getAttributes(current);
                    if ((attributes & FileAttributes.ReparsePoint) != 0) return ServerConfigState.Unknown;
                    if (current == path && (attributes & FileAttributes.Directory) != 0)
                        return ServerConfigState.Unknown;
                }
                using (Stream stream = openRead(path))
                {
                    if (!stream.CanRead) return ServerConfigState.Denied;
                    long size = stream.Length;
                    if (size < 0 || size > MaxConfigBytes) return ServerConfigState.TooLarge;
                    byte[] bytes = new byte[(int)size];
                    try
                    {
                        int read = 0;
                        while (read < bytes.Length)
                        {
                            if (watch.ElapsedMilliseconds >= MaxProbeMilliseconds) return ServerConfigState.Unknown;
                            int count = stream.Read(bytes, read, bytes.Length - read);
                            if (count <= 0) return ServerConfigState.Unknown;
                            read += count;
                        }
                        if (watch.ElapsedMilliseconds >= MaxProbeMilliseconds) return ServerConfigState.Unknown;
                        return HasCaPrivateKeyMarker(Encoding.UTF8.GetString(bytes))
                            ? ServerConfigState.ReadableWithCaKey : ServerConfigState.ReadableWithoutCaKey;
                    }
                    finally { Array.Clear(bytes, 0, bytes.Length); }
                }
            }
            catch (FileNotFoundException) { return ServerConfigState.Absent; }
            catch (DirectoryNotFoundException) { return ServerConfigState.Absent; }
            catch (UnauthorizedAccessException) { return ServerConfigState.Denied; }
            catch (Exception) { return ServerConfigState.Unknown; }
        }

        internal static bool HasCaPrivateKeyMarker(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            bool inCa = false;
            bool inPrivateKey = false;
            bool sawBegin = false;
            int keyIndent = -1;
            foreach (string rawLine in content.Split('\n'))
            {
                string line = rawLine.TrimEnd('\r');
                int indent = 0;
                while (indent < line.Length && line[indent] == ' ') indent++;
                string trimmed = line.Substring(indent).TrimStart('\uFEFF');
                if (trimmed.Length == 0 || trimmed[0] == '#') continue;
                if (indent == 0)
                {
                    inCa = trimmed == "CA:" || trimmed.StartsWith("CA: #", StringComparison.Ordinal);
                    inPrivateKey = false;
                    sawBegin = false;
                    keyIndent = -1;
                    continue;
                }
                if (!inCa) continue;
                if (inPrivateKey && indent <= keyIndent)
                {
                    inPrivateKey = false;
                    sawBegin = false;
                }
                if (trimmed.StartsWith("private_key:", StringComparison.Ordinal))
                {
                    inPrivateKey = true;
                    sawBegin = false;
                    keyIndent = indent;
                }
                if (!inPrivateKey) continue;
                if (trimmed.IndexOf("-----BEGIN ", StringComparison.Ordinal) >= 0 &&
                    trimmed.IndexOf("PRIVATE KEY-----", StringComparison.Ordinal) >= 0) sawBegin = true;
                if (sawBegin && trimmed.IndexOf("-----END ", StringComparison.Ordinal) >= 0 &&
                    trimmed.IndexOf("PRIVATE KEY-----", StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }
    }
}
