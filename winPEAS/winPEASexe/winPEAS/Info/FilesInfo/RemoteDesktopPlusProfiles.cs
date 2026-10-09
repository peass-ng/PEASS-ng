using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace winPEAS.Info.FilesInfo
{
    internal sealed class RemoteDesktopPlusProfileReport
    {
        internal readonly List<string> Paths = new List<string>();
        internal bool Partial { get; set; }
        internal bool LimitReached { get; set; }
        internal int CandidatesInspected { get; set; }
    }

    internal static class RemoteDesktopPlusProfiles
    {
        internal const int MaxXmlBytes = 64 * 1024;
        internal const int MaxDirectories = 128;
        internal const int MaxCandidates = 32;
        internal const int MaxFindings = 20;
        private const int MaxScanMilliseconds = 2000;

        // The user-profile candidates come from winPEAS's existing file inventory.
        // Only the system drive's immediate children are probed additionally.
        internal static RemoteDesktopPlusProfileReport Scan(string systemDriveRoot, IEnumerable<string> userProfileCandidates)
        {
            var report = new RemoteDesktopPlusProfileReport();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var clock = Stopwatch.StartNew();

            if (!string.IsNullOrEmpty(systemDriveRoot))
            {
                if (systemDriveRoot.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    report.Partial = true;
                }
                else
                {
                    try
                    {
                        if (Directory.Exists(systemDriveRoot))
                        {
                            InspectCandidate(Path.Combine(systemDriveRoot, "profiles.xml"), seen, report, clock);
                            int directories = 0;
                            foreach (string directory in Directory.EnumerateDirectories(systemDriveRoot, "*", SearchOption.TopDirectoryOnly))
                            {
                                if (directories >= MaxDirectories || clock.ElapsedMilliseconds >= MaxScanMilliseconds ||
                                    report.CandidatesInspected >= MaxCandidates || report.Paths.Count >= MaxFindings)
                                {
                                    report.LimitReached = true;
                                    break;
                                }
                                directories++;
                                try
                                {
                                    if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                                    {
                                        continue;
                                    }
                                    InspectCandidate(Path.Combine(directory, "profiles.xml"), seen, report, clock);
                                }
                                catch (IOException) { report.Partial = true; }
                                catch (UnauthorizedAccessException) { report.Partial = true; }
                                catch (System.Security.SecurityException) { report.Partial = true; }
                            }
                        }
                        else report.Partial = true;
                    }
                    catch (IOException) { report.Partial = true; }
                    catch (UnauthorizedAccessException) { report.Partial = true; }
                    catch (ArgumentException) { report.Partial = true; }
                    catch (System.Security.SecurityException) { report.Partial = true; }
                }
            }

            if (userProfileCandidates != null)
            {
                foreach (string path in userProfileCandidates)
                {
                    if (report.CandidatesInspected >= MaxCandidates || report.Paths.Count >= MaxFindings ||
                        clock.ElapsedMilliseconds >= MaxScanMilliseconds)
                    {
                        report.LimitReached = true;
                        break;
                    }
                    InspectCandidate(path, seen, report, clock);
                }
            }
            return report;
        }

        private static void InspectCandidate(string path, HashSet<string> seen,
            RemoteDesktopPlusProfileReport report, Stopwatch clock)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                report.Partial = true;
                return;
            }
            try
            {
                if (!string.Equals(Path.GetFileName(path), "profiles.xml", StringComparison.OrdinalIgnoreCase) ||
                    !seen.Add(path)) return;
            }
            catch (ArgumentException)
            {
                report.Partial = true;
                return;
            }
            if (report.CandidatesInspected >= MaxCandidates || report.Paths.Count >= MaxFindings ||
                clock.ElapsedMilliseconds >= MaxScanMilliseconds)
            {
                report.LimitReached = true;
                return;
            }
            try
            {
                if (!File.Exists(path)) return;
                report.CandidatesInspected++;
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                {
                    report.Partial = true;
                    return;
                }
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (stream.Length > MaxXmlBytes)
                    {
                        report.Partial = true;
                        return;
                    }
                    if (TryInspectXml(stream)) report.Paths.Add(path);
                }
            }
            catch (IOException) { report.Partial = true; }
            catch (UnauthorizedAccessException) { report.Partial = true; }
            catch (ArgumentException) { report.Partial = true; }
            catch (System.Security.SecurityException) { report.Partial = true; }
        }

        // Inspects only the legacy exported profile shape. No credential value is returned.
        internal static bool TryInspectXml(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek) return false;
            try
            {
                long length = stream.Length;
                if (length == 0 || length > MaxXmlBytes) return false;
                stream.Position = 0;
                byte[] bytes = new byte[(int)length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read == 0) return false;
                    offset += read;
                }
                if (stream.ReadByte() != -1) return false;

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = MaxXmlBytes,
                    MaxCharactersFromEntities = 0
                };
                var document = new XmlDocument { XmlResolver = null };
                using (var bounded = new MemoryStream(bytes, false))
                using (XmlReader reader = XmlReader.Create(bounded, settings)) document.Load(reader);
                XmlElement root = document.DocumentElement;
                if (root == null || root.LocalName != "Data" || root.NamespaceURI.Length != 0) return false;
                foreach (XmlNode node in root.ChildNodes)
                {
                    XmlElement profile = node as XmlElement;
                    if (profile == null || profile.LocalName != "Profile" || profile.NamespaceURI.Length != 0) continue;
                    XmlNode name = profile.SelectSingleNode("ProfileName");
                    XmlNode password = profile.SelectSingleNode("Password");
                    XmlNode secure = profile.SelectSingleNode("Secure");
                    if (name != null && !string.IsNullOrWhiteSpace(name.InnerText) &&
                        password != null && !string.IsNullOrWhiteSpace(password.InnerText) &&
                        secure != null && (string.Equals(secure.InnerText.Trim(), "true", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(secure.InnerText.Trim(), "false", StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            catch (XmlException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return false;
        }
    }
}
