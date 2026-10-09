using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace winPEAS.Info.FilesInfo
{
    internal enum ZipNtdsBackupStatus { NoMatch, Match, Unknown }

    internal sealed class ZipNtdsBackupFinding
    {
        internal string Path { get; set; }
        internal string NtdsMember { get; set; }
        internal string SystemMember { get; set; }
    }

    internal sealed class ZipNtdsBackupReport
    {
        internal readonly List<ZipNtdsBackupFinding> Findings = new List<ZipNtdsBackupFinding>();
        internal readonly List<string> UnknownPaths = new List<string>();
        internal bool LimitReached;
    }

    // Reads the end record and central directory only. No ZIP member is opened or extracted.
    internal static class ZipNtdsBackupIndicator
    {
        internal const int MaxArchives = 20;
        internal const int MaxMilliseconds = 2000;
        // Classic ZIP uses 32-bit central-directory offsets. The file may be large;
        // the bytes actually read remain bounded by the EOCD record and directory cap.
        internal const long MaxArchiveBytes = (long)uint.MaxValue + 4 * 1024 * 1024 + 22;
        internal const int MaxCentralBytes = 4 * 1024 * 1024;
        internal const int MaxMembers = 2048;
        private const int MaxNameBytes = 4096;

        internal static ZipNtdsBackupReport Scan()
        {
            var report = new ZipNtdsBackupReport();
            var timer = Stopwatch.StartNew();
            int inspected = 0;
            DriveInfo[] drives;
            try { drives = DriveInfo.GetDrives(); }
            catch (Exception) { report.UnknownPaths.Add("fixed drive enumeration"); return report; }

            foreach (DriveInfo drive in drives)
            {
                if (Expired(timer)) { report.LimitReached = true; break; }
                DriveType type;
                string root;
                try { type = drive.DriveType; root = drive.RootDirectory.FullName; }
                catch (Exception) { report.UnknownPaths.Add("fixed drive metadata"); continue; }
                if (type != DriveType.Fixed || root.StartsWith(@"\\", StringComparison.Ordinal)) continue;

                // Windows path lookup is case insensitive. Include both spellings for unusual volumes,
                // and deduplicate them before enumerating any directory.
                var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string directory in new[] { Path.Combine(root, "BACKUP"),
                    Path.Combine(root, "Backups"), root })
                {
                    if (!seenDirectories.Add(directory)) continue;
                    if (Expired(timer)) { report.LimitReached = true; return report; }
                    try
                    {
                        FileAttributes attributes;
                        try { attributes = File.GetAttributes(directory); }
                        catch (DirectoryNotFoundException) { continue; }
                        catch (FileNotFoundException) { continue; }
                        if ((attributes & FileAttributes.Directory) == 0) continue;
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            report.UnknownPaths.Add(directory);
                            continue;
                        }
                        foreach (string path in Directory.EnumerateFiles(directory, "*.zip", SearchOption.TopDirectoryOnly))
                        {
                            if (!string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
                                continue;
                            if (Expired(timer) || inspected >= MaxArchives)
                            {
                                report.LimitReached = true;
                                return report;
                            }
                            inspected++;
                            try
                            {
                                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                                {
                                    report.UnknownPaths.Add(path);
                                    continue;
                                }
                                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.RandomAccess))
                                {
                                    string ntdsMember, systemMember;
                                    ZipNtdsBackupStatus status = InspectMetadata(file, () => Expired(timer),
                                        out ntdsMember, out systemMember);
                                    if (status == ZipNtdsBackupStatus.Match)
                                        report.Findings.Add(new ZipNtdsBackupFinding { Path = path,
                                            NtdsMember = ntdsMember, SystemMember = systemMember });
                                    else if (status == ZipNtdsBackupStatus.Unknown)
                                        report.UnknownPaths.Add(path);
                                    if (Expired(timer)) { report.LimitReached = true; return report; }
                                }
                            }
                            catch (Exception) { report.UnknownPaths.Add(path); }
                        }
                    }
                    catch (Exception) { report.UnknownPaths.Add(directory); }
                }
            }
            if (Expired(timer)) report.LimitReached = true;
            return report;
        }

        private static bool Expired(Stopwatch timer) { return timer.ElapsedMilliseconds >= MaxMilliseconds; }

        // Limits are parameters so synthetic fixtures can exercise the same parser without large files.
        internal static ZipNtdsBackupStatus InspectMetadata(Stream stream, Func<bool> expired,
            out string ntdsMember, out string systemMember,
            long maxArchiveBytes = MaxArchiveBytes, int maxMembers = MaxMembers)
        {
            ntdsMember = null;
            systemMember = null;
            try
            {
                if (!stream.CanRead || !stream.CanSeek || expired()) return ZipNtdsBackupStatus.Unknown;
                long length = stream.Length;
                if (length < 22 || length > maxArchiveBytes) return ZipNtdsBackupStatus.Unknown;

                // Accept only a zero-comment EOCD at EOF, so even the locator read
                // stays on metadata for a well-formed archive.
                var endRecord = new byte[22];
                stream.Seek(length - endRecord.Length, SeekOrigin.Begin);
                if (!ReadExact(stream, endRecord, endRecord.Length) || U32(endRecord, 0) != 0x06054b50 ||
                    U16(endRecord, 20) != 0 || U16(endRecord, 4) != 0 || U16(endRecord, 6) != 0 ||
                    U16(endRecord, 8) != U16(endRecord, 10)) return ZipNtdsBackupStatus.Unknown;
                int count = U16(endRecord, 10);
                uint centralSize = U32(endRecord, 12);
                uint centralOffset = U32(endRecord, 16);
                if (count == ushort.MaxValue || centralSize == uint.MaxValue || centralOffset == uint.MaxValue ||
                    count > maxMembers || centralSize > MaxCentralBytes) return ZipNtdsBackupStatus.Unknown;
                long endOffset = length - endRecord.Length;
                if ((long)centralOffset + centralSize != endOffset) return ZipNtdsBackupStatus.Unknown;

                stream.Seek(centralOffset, SeekOrigin.Begin);
                long centralEnd = (long)centralOffset + centralSize;
                bool encrypted = false;
                for (int i = 0; i < count; i++)
                {
                    if (expired() || stream.Position + 46 > centralEnd) return ZipNtdsBackupStatus.Unknown;
                    var header = new byte[46];
                    if (!ReadExact(stream, header, header.Length) || U32(header, 0) != 0x02014b50)
                        return ZipNtdsBackupStatus.Unknown;
                    ushort flags = U16(header, 8);
                    int nameLength = U16(header, 28);
                    int extraLength = U16(header, 30);
                    int commentLength = U16(header, 32);
                    if (nameLength > MaxNameBytes || stream.Position + nameLength + extraLength + commentLength > centralEnd)
                        return ZipNtdsBackupStatus.Unknown;
                    var nameBytes = new byte[nameLength];
                    if (!ReadExact(stream, nameBytes, nameLength)) return ZipNtdsBackupStatus.Unknown;
                    stream.Seek(extraLength + commentLength, SeekOrigin.Current);
                    encrypted |= (flags & 1) != 0;
                    string name = ((flags & 0x800) != 0 ? Encoding.UTF8 : Encoding.GetEncoding(437)).GetString(nameBytes);
                    string basename = name.Substring(Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\')) + 1);
                    if (string.Equals(basename, "ntds.dit", StringComparison.OrdinalIgnoreCase)) ntdsMember = SafeName(name);
                    if (string.Equals(basename, "SYSTEM", StringComparison.OrdinalIgnoreCase)) systemMember = SafeName(name);
                }
                if (stream.Position != centralEnd || encrypted) return ZipNtdsBackupStatus.Unknown;
                return ntdsMember != null && systemMember != null
                    ? ZipNtdsBackupStatus.Match : ZipNtdsBackupStatus.NoMatch;
            }
            catch (Exception) { return ZipNtdsBackupStatus.Unknown; }
        }

        private static bool ReadExact(Stream stream, byte[] bytes, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(bytes, offset, count - offset);
                if (read == 0) return false;
                offset += read;
            }
            return true;
        }

        private static string SafeName(string name)
        {
            var safe = new StringBuilder();
            foreach (char c in name)
            {
                if (safe.Length == 512) { safe.Append("..."); break; }
                safe.Append(char.IsControl(c) ? '?' : c);
            }
            return safe.ToString();
        }

        private static ushort U16(byte[] data, int offset)
        {
            return (ushort)(data[offset] | data[offset + 1] << 8);
        }

        private static uint U32(byte[] data, int offset)
        {
            return (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
        }
    }
}
