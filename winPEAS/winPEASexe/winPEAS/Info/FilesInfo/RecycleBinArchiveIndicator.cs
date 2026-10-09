using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace winPEAS.Info.FilesInfo
{
    internal sealed class RecycleBinArchiveCandidate
    {
        internal string OriginalName;
        internal DateTime DeletedUtc;
        internal string RecycledPath;
        internal long? Size;
        internal string Accessibility;
        internal bool LikelyBackup;
    }

    internal sealed class RecycleBinArchiveResult
    {
        internal readonly List<RecycleBinArchiveCandidate> Candidates = new List<RecycleBinArchiveCandidate>();
        internal int RecordsInspected;
        internal int MalformedRecords;
        internal bool Partial;
    }

    // Reads Recycle Bin metadata only. The paired $R file is opened solely to test access;
    // no bytes from it (or from an archive) are read.
    internal static class RecycleBinArchiveIndicator
    {
        internal const int MaxVolumes = 16;
        internal const int MaxSidDirectories = 128;
        internal const int MaxDirectoryEntries = 4096;
        internal const int MaxRecords = 1024;
        internal const int MaxCandidates = 20;
        internal const int MaxMetadataBytes = 4096;
        internal const int MaxElapsedMilliseconds = 3000;

        internal static RecycleBinArchiveResult Scan()
        {
            var result = new RecycleBinArchiveResult();
            var timer = Stopwatch.StartNew();
            int volumeCount = 0, directoryCount = 0, entryCount = 0;
            string[] drives;
            try { drives = Environment.GetLogicalDrives(); }
            catch { result.Partial = true; return result; }

            foreach (string drive in drives)
            {
                if (Expired(timer) || volumeCount++ >= MaxVolumes) { result.Partial = true; break; }
                try
                {
                    DriveType type = new DriveInfo(drive).DriveType;
                    if (type != DriveType.Fixed && type != DriveType.Removable) continue;
                }
                catch { result.Partial = true; continue; }
                string recycleRoot = Path.Combine(drive, "$Recycle.Bin");
                if (!Directory.Exists(recycleRoot)) continue;
                try
                {
                    if (IsReparsePoint(recycleRoot)) { result.Partial = true; continue; }
                    foreach (string sidDirectory in Directory.EnumerateDirectories(recycleRoot, "S-1-*", SearchOption.TopDirectoryOnly))
                    {
                        if (Expired(timer) || directoryCount++ >= MaxSidDirectories) { result.Partial = true; return result; }
                        try
                        {
                            if (IsReparsePoint(sidDirectory)) { result.Partial = true; continue; }
                            foreach (string path in Directory.EnumerateFiles(sidDirectory, "*", SearchOption.TopDirectoryOnly))
                            {
                                if (Expired(timer) || entryCount++ >= MaxDirectoryEntries || result.RecordsInspected >= MaxRecords)
                                { result.Partial = true; return result; }
                                string filename = Path.GetFileName(path);
                                if (filename.Length < 3 || !filename.StartsWith("$I", StringComparison.OrdinalIgnoreCase)) continue;
                                result.RecordsInspected++;
                                RecycleBinArchiveCandidate candidate;
                                try
                                {
                                    if (IsReparsePoint(path)) { result.Partial = true; continue; }
                                    byte[] metadata = ReadMetadata(path);
                                    if (!TryParse(metadata, Path.Combine(sidDirectory, "$R" + filename.Substring(2)), out candidate))
                                    { result.MalformedRecords++; continue; }
                                }
                                catch { result.Partial = true; continue; }
                                if (candidate == null) continue;
                                if (!TryAddCandidate(result, candidate)) return result;
                                if (CheckPairedFile(candidate)) result.Partial = true;
                            }
                        }
                        catch { result.Partial = true; }
                    }
                }
                catch { result.Partial = true; }
            }
            return result;
        }

        private static bool Expired(Stopwatch timer) { return timer.ElapsedMilliseconds >= MaxElapsedMilliseconds; }

        private static bool IsReparsePoint(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

        internal static bool TryAddCandidate(RecycleBinArchiveResult result, RecycleBinArchiveCandidate candidate)
        {
            if (result.Candidates.Count >= MaxCandidates) { result.Partial = true; return false; }
            result.Candidates.Add(candidate);
            return true;
        }

        private static byte[] ReadMetadata(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (stream.Length > MaxMetadataBytes || stream.Length < 26) return new byte[0];
                byte[] bytes = new byte[(int)stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int count = stream.Read(bytes, offset, bytes.Length - offset);
                    if (count == 0) break;
                    offset += count;
                }
                if (offset != bytes.Length) return new byte[0];
                return bytes;
            }
        }

        // Windows Vista/7 uses version 1 and a fixed 260-character path at offset 24.
        // Windows 8+ uses version 2 and a character count at offset 24.
        internal static bool TryParse(byte[] data, string pairedPath, out RecycleBinArchiveCandidate candidate)
        {
            candidate = null;
            if (data == null || data.Length < 28 || data.Length > MaxMetadataBytes) return false;
            long version = BitConverter.ToInt64(data, 0);
            int pathOffset, characterCount;
            if (version == 1)
            {
                if (data.Length < 544) return false;
                pathOffset = 24;
                characterCount = 260;
            }
            else if (version == 2)
            {
                uint length = BitConverter.ToUInt32(data, 24);
                if (length == 0 || length > 2048 || length > (data.Length - 28) / 2) return false;
                pathOffset = 28;
                characterCount = (int)length;
            }
            else return false;

            long fileTime = BitConverter.ToInt64(data, 16);
            DateTime deletedUtc;
            try { deletedUtc = DateTime.FromFileTimeUtc(fileTime); }
            catch { return false; }
            string originalPath = Encoding.Unicode.GetString(data, pathOffset, characterCount * 2);
            int terminator = originalPath.IndexOf('\0');
            if (terminator >= 0) originalPath = originalPath.Substring(0, terminator);
            if (originalPath.Length == 0 || HasUnsafeCharacters(originalPath)) return false;
            int slash = Math.Max(originalPath.LastIndexOf('\\'), originalPath.LastIndexOf('/'));
            string name = originalPath.Substring(slash + 1);
            if (name.Length == 0) return false;
            string extension = Path.GetExtension(name);
            if (!extension.Equals(".7z", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".rar", StringComparison.OrdinalIgnoreCase)) return true;
            candidate = new RecycleBinArchiveCandidate
            {
                OriginalName = name,
                DeletedUtc = deletedUtc,
                RecycledPath = pairedPath,
                Size = BitConverter.ToInt64(data, 8),
                Accessibility = "unknown",
                LikelyBackup = name.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("config", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("snapshot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("dump", StringComparison.OrdinalIgnoreCase) >= 0
            };
            if (candidate.Size < 0) { candidate = null; return false; }
            return true;
        }

        private static bool HasUnsafeCharacters(string value)
        {
            foreach (char character in value)
                if (char.IsControl(character) || char.IsSurrogate(character)) return true;
            return false;
        }

        // Returns true when access or file type leaves the paired file unresolved.
        internal static bool CheckPairedFile(RecycleBinArchiveCandidate candidate)
        {
            try
            {
                if (IsReparsePoint(candidate.RecycledPath))
                {
                    candidate.Accessibility = "unknown (reparse point skipped)";
                    return true;
                }
                using (var stream = new FileStream(candidate.RecycledPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    candidate.Size = stream.Length;
                    candidate.Accessibility = "readable";
                }
                return false;
            }
            catch (FileNotFoundException) { candidate.Accessibility = "missing"; return false; }
            catch (DirectoryNotFoundException) { candidate.Accessibility = "missing"; return false; }
            catch (UnauthorizedAccessException) { candidate.Accessibility = "inaccessible"; return true; }
            catch { candidate.Accessibility = "unknown"; return true; }
        }
    }
}
