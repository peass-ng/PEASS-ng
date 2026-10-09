using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace winPEAS.Info.FilesInfo
{
    internal sealed class MRemoteNgConnectionFinding
    {
        internal string Path { get; set; }
        // Full-file encryption hides the connection nodes, so their count is unavailable.
        internal int? EncryptedNodeCount { get; set; }
    }

    internal sealed class MRemoteNgConnectionReport
    {
        internal readonly List<MRemoteNgConnectionFinding> Findings = new List<MRemoteNgConnectionFinding>();
        internal bool LimitReached { get; set; }
        internal bool Partial { get; set; }
        internal int CandidatesInspected { get; set; }
    }

    internal static class MRemoteNgConnectionFiles
    {
        internal const int MaxXmlBytes = 1024 * 1024;
        internal const int MaxDirectories = 48;
        internal const int MaxCandidates = 160;
        private const int MaxScanMilliseconds = 3000;
        private const string ConnectionNamespace = "http://mremoteng.org";

        internal static MRemoteNgConnectionReport ScanCurrentUser()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string profile = string.IsNullOrEmpty(appData) ? null : Path.Combine(appData, "mRemoteNG");
            return Scan(documents, profile);
        }

        // The caller supplies roots so the traversal can be checked with synthetic files.
        internal static MRemoteNgConnectionReport Scan(string documents, string profile)
        {
            var report = new MRemoteNgConnectionReport();
            var pending = new Queue<Tuple<string, int>>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if ((!string.IsNullOrEmpty(documents) && documents.StartsWith(@"\\", StringComparison.Ordinal)) ||
                (!string.IsNullOrEmpty(profile) && profile.StartsWith(@"\\", StringComparison.Ordinal)))
                report.Partial = true;
            AddRoot(documents, 2, pending, seen);
            AddRoot(profile, 1, pending, seen);
            var clock = Stopwatch.StartNew();
            int directories = 0;

            while (pending.Count > 0)
            {
                if (directories >= MaxDirectories || report.CandidatesInspected >= MaxCandidates ||
                    clock.ElapsedMilliseconds >= MaxScanMilliseconds)
                {
                    report.LimitReached = true;
                    break;
                }

                Tuple<string, int> current = pending.Dequeue();
                try
                {
                    if ((File.GetAttributes(current.Item1) & FileAttributes.ReparsePoint) != 0) continue;
                    directories++;
                    foreach (string file in Directory.EnumerateFiles(current.Item1, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (clock.ElapsedMilliseconds >= MaxScanMilliseconds)
                        {
                            report.LimitReached = true;
                            break;
                        }
                        if (!IsCandidateName(file)) continue;
                        if (report.CandidatesInspected >= MaxCandidates || clock.ElapsedMilliseconds >= MaxScanMilliseconds)
                        {
                            report.LimitReached = true;
                            break;
                        }
                        report.CandidatesInspected++;
                        MRemoteNgConnectionFinding finding;
                        bool unknown;
                        if (TryInspectFile(file, out finding, out unknown)) report.Findings.Add(finding);
                        if (unknown) report.Partial = true;
                    }
                    if (report.LimitReached) break;
                    if (current.Item2 == 0) continue;
                    foreach (string child in Directory.EnumerateDirectories(current.Item1, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (clock.ElapsedMilliseconds >= MaxScanMilliseconds)
                        {
                            report.LimitReached = true;
                            break;
                        }
                        if (seen.Add(child)) pending.Enqueue(Tuple.Create(child, current.Item2 - 1));
                        if (pending.Count + directories >= MaxDirectories)
                        {
                            report.LimitReached = true;
                            break;
                        }
                    }
                    if (report.LimitReached) break;
                }
                catch (IOException) { report.Partial = true; }
                catch (UnauthorizedAccessException) { report.Partial = true; }
                catch (ArgumentException) { report.Partial = true; }
                catch (System.Security.SecurityException) { report.Partial = true; }
            }
            return report;
        }

        private static void AddRoot(string path, int depth, Queue<Tuple<string, int>> pending, HashSet<string> seen)
        {
            if (!string.IsNullOrEmpty(path) && !path.StartsWith(@"\\", StringComparison.Ordinal) &&
                Directory.Exists(path) && seen.Add(path))
                pending.Enqueue(Tuple.Create(path, depth));
        }

        private static bool IsCandidateName(string path)
        {
            string name = Path.GetFileName(path);
            return name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".xml.bak", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".xml.backup", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("confCons.xml.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryInspectFile(string path, out MRemoteNgConnectionFinding finding, out bool unknown)
        {
            finding = null;
            unknown = false;
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                {
                    unknown = true;
                    return false;
                }
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (stream.Length > MaxXmlBytes) unknown = true;
                    return TryInspectXml(stream, path, out finding);
                }
            }
            catch (IOException) { unknown = true; return false; }
            catch (UnauthorizedAccessException) { unknown = true; return false; }
            catch (ArgumentException) { unknown = true; return false; }
            catch (System.Security.SecurityException) { unknown = true; return false; }
        }

        internal static bool TryInspectXml(Stream stream, string path, out MRemoteNgConnectionFinding finding)
        {
            finding = null;
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                return false;

            try
            {
                long length = stream.Length;
                if (length == 0 || length > MaxXmlBytes) return false;
                // Recheck the byte cap while reading in case the file grows after opening.
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
                if (root == null || root.LocalName != "Connections") return false;

                bool namedNamespace = root.NamespaceURI == ConnectionNamespace;
                bool metadata = root.HasAttribute("ConfVersion") && root.HasAttribute("EncryptionEngine");
                if (!namedNamespace && !metadata) return false;

                int encryptedNodes = 0;
                bool connectionShape = false;
                foreach (XmlNode node in root.GetElementsByTagName("*"))
                {
                    XmlElement element = node as XmlElement;
                    if (element == null || element.LocalName != "Node") continue;
                    bool isConnection = element.HasAttribute("Hostname") || element.HasAttribute("Protocol") || element.HasAttribute("Username");
                    if (isConnection) connectionShape = true;
                    if (isConnection && !string.IsNullOrWhiteSpace(element.GetAttribute("Password"))) encryptedNodes++;
                }
                bool fullFileEncryption = string.Equals(root.GetAttribute("FullFileEncryption"), "true", StringComparison.OrdinalIgnoreCase);
                if (!namedNamespace && !connectionShape && !fullFileEncryption) return false;
                if (encryptedNodes == 0 && !fullFileEncryption) return false;

                finding = new MRemoteNgConnectionFinding
                {
                    Path = path,
                    EncryptedNodeCount = fullFileEncryption && encryptedNodes == 0 ? (int?)null : encryptedNodes
                };
                return true;
            }
            catch (XmlException) { return false; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
