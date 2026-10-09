using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using winPEAS.Info.ServicesInfo;

namespace winPEAS.Info.FilesInfo
{
    internal sealed class ApacheSystemWebRootReport
    {
        internal string Layout;
        internal string ServiceName;
        internal IisServedRoot Root;
        internal string Note;
    }

    // Checks only conventional local XAMPP and WAMP installations. No web request,
    // recursive search, content execution, or write probe is performed.
    internal static class ApacheSystemWebRoot
    {
        internal const int MaxConfigBytes = 128 * 1024;
        private static readonly string[] ServiceNames = { "Apache2.4", "Apache2.2", "Apache2" };
        private static readonly string[] WampServiceNames = { "wampapache64", "wampapache" };

        internal static ApacheSystemWebRootReport Scan()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return null;
            string drive = GetTrustedSystemDrive();
            if (drive == null) return null;
            ApacheSystemWebRootReport xampp = ScanXampp(drive);
            if (xampp != null && xampp.Root != null) return xampp;
            return ScanWamp(drive) ?? xampp;
        }

        private static ApacheSystemWebRootReport ScanXampp(string drive)
        {
            string expectedRoot = IisServedRootPermissions.NormalizeLocalPath(drive + @"\xampp\htdocs");
            string expectedImage = IisServedRootPermissions.NormalizeLocalPath(drive + @"\xampp\apache\bin\httpd.exe");
            if (expectedRoot == null || expectedImage == null ||
                !IsPhysicalFixedPath(expectedRoot) || !Directory.Exists(expectedRoot)) return null;

            var report = new ApacheSystemWebRootReport { Layout = "XAMPP", Note = "Apache service identity or document-root mapping unresolved." };
            foreach (string name in ServiceNames)
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name))
                    {
                        if (key == null) continue;
                        report.ServiceName = name;
                        int start = Convert.ToInt32(key.GetValue("Start", 4));
                        if (start == 4) { report.Note = "Apache service is disabled; runtime state unknown."; continue; }
                        if (!IsSystemApacheService(key.GetValue("ObjectName") as string,
                            key.GetValue("ImagePath") as string, expectedImage))
                        {
                            report.Note = "Apache service account or executable does not confirm SYSTEM XAMPP Apache.";
                            continue;
                        }
                        if (!IsPhysicalFixedPath(expectedImage))
                        {
                            report.Note = "Apache executable path unavailable or contains a reparse point.";
                            continue;
                        }
                        string configPath = drive + @"\xampp\apache\conf\httpd.conf";
                        if (!IsPhysicalFixedPath(configPath))
                        {
                            report.Note = "Apache configuration path unavailable or contains a reparse point.";
                            continue;
                        }
                        string config = ReadBoundedConfig(configPath);
                        if (config == null)
                        {
                            report.Note = "Apache configuration unavailable or over the read limit.";
                            continue;
                        }
                        string configuredRoot = ParseDefaultDocumentRoot(config);
                        if (!string.Equals(configuredRoot, expectedRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            report.Note = "Default Apache DocumentRoot is absent or differs from XAMPP htdocs.";
                            continue;
                        }
                        report.Root = new IisServedRoot { Path = expectedRoot, Configured = true };
                        IisServedRootPermissions.AssessRoot(report.Root);
                        report.Note = "Apache service configuration maps the default document root to this path; runtime, virtual-host overrides, and server-side handlers are unverified.";
                        return report;
                    }
                }
                catch (Exception)
                {
                    report.Note = "Apache service or configuration metadata inaccessible.";
                }
            }
            return report;
        }

        private static ApacheSystemWebRootReport ScanWamp(string drive)
        {
            ApacheSystemWebRootReport x64 = ScanWampLayout(drive, "wamp64");
            if (x64 != null && x64.Root != null) return x64;
            return ScanWampLayout(drive, "wamp") ?? x64;
        }

        private static ApacheSystemWebRootReport ScanWampLayout(string drive, string layout)
        {
            string installDir = drive + "\\" + layout;
            string expectedRoot = IisServedRootPermissions.NormalizeLocalPath(installDir + @"\www");
            if (expectedRoot == null || !IsPhysicalFixedPath(expectedRoot) || !Directory.Exists(expectedRoot))
                return null;

            var report = new ApacheSystemWebRootReport { Layout = "WAMP", Note = "Apache service identity or document-root mapping unresolved." };
            foreach (string name in WampServiceNames)
            {
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name))
                    {
                        if (key == null) continue;
                        report.ServiceName = name;
                        if (Convert.ToInt32(key.GetValue("Start", 4)) == 4)
                        { report.Note = "Apache service is disabled; runtime state unknown."; continue; }
                        string imagePath = key.GetValue("ImagePath") as string;
                        string expectedImage = ConventionalWampImage(imagePath, drive, layout);
                        if (expectedImage == null || !IsSystemApacheService(
                            key.GetValue("ObjectName") as string, imagePath, expectedImage))
                        {
                            report.Note = "Apache service account or executable does not confirm SYSTEM WAMP Apache.";
                            continue;
                        }
                        string apacheHome = Path.GetDirectoryName(Path.GetDirectoryName(expectedImage));
                        string configPath = Path.Combine(apacheHome, @"conf\httpd.conf");
                        if (!IsPhysicalFixedPath(expectedImage) || !IsPhysicalFixedPath(configPath))
                        {
                            report.Note = "Apache executable or configuration path unavailable or contains a reparse point.";
                            continue;
                        }
                        string config = ReadBoundedConfig(configPath);
                        if (config == null)
                        {
                            report.Note = "Apache configuration unavailable or over the read limit.";
                            continue;
                        }
                        if (!string.Equals(ParseWampDocumentRoot(config, installDir),
                            expectedRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            report.Note = "Default Apache DocumentRoot is absent or differs from WAMP www.";
                            continue;
                        }
                        report.Root = new IisServedRoot { Path = expectedRoot, Configured = true };
                        IisServedRootPermissions.AssessRoot(report.Root);
                        report.Note = "Apache service configuration maps the default document root to this path; runtime, virtual-host overrides, PHP handler, and server token are unverified.";
                        return report;
                    }
                }
                catch (Exception) { report.Note = "Apache service or configuration metadata inaccessible."; }
            }
            return report;
        }

        // Derive the selected version from the exact service image, not a directory walk.
        internal static string ConventionalWampImage(string imagePath, string drive)
        {
            return ConventionalWampImage(imagePath, drive, "wamp64");
        }

        internal static string ConventionalWampImage(string imagePath, string drive, string layout)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || string.IsNullOrEmpty(drive) ||
                (layout != "wamp64" && layout != "wamp")) return null;
            ServiceCommandLineAssessment command = ServicesInfoHelper.AssessServiceCommandLine(imagePath);
            if (command.ScanLimitReached || string.IsNullOrEmpty(command.ExecutablePath)) return null;
            string image = IisServedRootPermissions.NormalizeLocalPath(command.ExecutablePath);
            string prefix = drive + "\\" + layout + @"\bin\apache\";
            if (image == null || !image.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
            string[] suffix = image.Substring(prefix.Length).Split('\\');
            if (suffix.Length != 3 || !string.Equals(suffix[1], "bin", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(suffix[2], "httpd.exe", StringComparison.OrdinalIgnoreCase) ||
                !suffix[0].StartsWith("apache", StringComparison.OrdinalIgnoreCase) ||
                suffix[0].Length <= 6 || suffix[0].Length > 32) return null;
            string version = suffix[0].Substring(6);
            if (version[0] < '0' || version[0] > '9' || version[version.Length - 1] == '.' ||
                version.Contains("..")) return null;
            bool previousDot = false;
            foreach (char c in version)
            {
                if (c == '.')
                {
                    if (previousDot) return null;
                    previousDot = true;
                }
                else
                {
                    if (c < '0' || c > '9') return null;
                    previousDot = false;
                }
            }
            return image;
        }

        internal static bool IsSystemApacheService(string account, string imagePath, string expectedImage)
        {
            bool system = string.Equals(account, "LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(account, @"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase);
            if (!system || string.IsNullOrWhiteSpace(imagePath) || string.IsNullOrEmpty(expectedImage)) return false;
            ServiceCommandLineAssessment command = ServicesInfoHelper.AssessServiceCommandLine(imagePath);
            if (command.ScanLimitReached || string.IsNullOrEmpty(command.ExecutablePath)) return false;
            string image = IisServedRootPermissions.NormalizeLocalPath(command.ExecutablePath);
            if (!string.Equals(image, expectedImage, StringComparison.OrdinalIgnoreCase)) return false;
            int start = imagePath.IndexOf(command.ExecutablePath, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return false;
            int end = start + command.ExecutablePath.Length;
            string arguments = imagePath.Substring(end).TrimStart('"').Trim();
            // An alternate -f/-d/-C/-c configuration could remap DocumentRoot.
            // Only the conventional service invocation uses the inspected config.
            return arguments.Length == 0 ||
                string.Equals(arguments, "-k runservice", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTrustedSystemDrive()
        {
            try
            {
                string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string root = string.IsNullOrEmpty(windows) ? null : Path.GetPathRoot(windows);
                if (string.IsNullOrEmpty(root) || root.Length != 3 || !char.IsLetter(root[0]) ||
                    root[1] != ':' || new DriveInfo(root).DriveType != DriveType.Fixed) return null;
                return root.TrimEnd('\\');
            }
            catch (Exception) { return null; }
        }

        private static bool IsPhysicalFixedPath(string path)
        {
            try
            {
                string root = Path.GetPathRoot(path);
                if (new DriveInfo(root).DriveType != DriveType.Fixed) return false;
                string current = root;
                foreach (string part in path.Substring(root.Length).Split(
                    new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    current = Path.Combine(current, part);
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return false;
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        private static string ReadBoundedConfig(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    if (stream.Length > MaxConfigBytes) return null;
                    byte[] data = new byte[MaxConfigBytes + 1];
                    int length = 0;
                    int read;
                    while (length < data.Length &&
                        (read = stream.Read(data, length, data.Length - length)) > 0) length += read;
                    if (length > MaxConfigBytes) return null;
                    return Encoding.UTF8.GetString(data, 0, length);
                }
            }
            catch (Exception) { return null; }
        }

        internal static string ParseWampDocumentRoot(string config, string installDir)
        {
            if (string.IsNullOrEmpty(config) || config.Length > MaxConfigBytes) return null;
            string direct = ParseDefaultDocumentRoot(config);
            if (direct != null) return direct;
            if (config.IndexOf("${INSTALL_DIR}", StringComparison.Ordinal) < 0) return null;

            // Resolve only the conventional WAMP define and only when it names
            // the same fixed installation directory inferred from the service.
            const string define = "Define INSTALL_DIR";
            bool found = false;
            using (var reader = new StringReader(config))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("#", StringComparison.Ordinal) ||
                        !line.StartsWith(define, StringComparison.OrdinalIgnoreCase) ||
                        line.Length <= define.Length || !char.IsWhiteSpace(line[define.Length])) continue;
                    if (found) return null;
                    string value = line.Substring(define.Length).Trim();
                    if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                        value = value.Substring(1, value.Length - 2);
                    if (!string.Equals(IisServedRootPermissions.NormalizeLocalPath(value), installDir,
                        StringComparison.OrdinalIgnoreCase)) return null;
                    found = true;
                }
            }
            return found ? ParseDefaultDocumentRoot(config.Replace("${INSTALL_DIR}", installDir.Replace('\\', '/'))) : null;
        }

        internal static string ParseDefaultDocumentRoot(string config)
        {
            if (string.IsNullOrEmpty(config) || config.Length > MaxConfigBytes) return null;
            bool inVirtualHost = false;
            string configured = null;
            using (var reader = new StringReader(config))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("<VirtualHost", StringComparison.OrdinalIgnoreCase))
                    { inVirtualHost = true; continue; }
                    if (line.StartsWith("</VirtualHost", StringComparison.OrdinalIgnoreCase))
                    { inVirtualHost = false; continue; }
                    if (inVirtualHost || !line.StartsWith("DocumentRoot", StringComparison.OrdinalIgnoreCase) ||
                        line.Length == "DocumentRoot".Length || !char.IsWhiteSpace(line["DocumentRoot".Length])) continue;
                    string value = line.Substring("DocumentRoot".Length).Trim();
                    if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                        value = value.Substring(1, value.Length - 2);
                    if (configured != null) return null; // Multiple global roots need full config review.
                    configured = IisServedRootPermissions.NormalizeLocalPath(value);
                    if (configured == null) return null;
                }
            }
            return configured;
        }
    }
}
