using System;
using System.Collections.Generic;
using System.IO;
using winPEAS.Helpers;
using winPEAS.KnownFileCreds.Browsers;
using winPEAS.KnownFileCreds.Browsers.Brave;
using winPEAS.KnownFileCreds.Browsers.Chrome;
using winPEAS.KnownFileCreds.Browsers.Firefox;
using winPEAS.KnownFileCreds.Browsers.Opera;

namespace winPEAS.Checks
{
    internal class BrowserInfo : ISystemCheck
    {
        public string[] MitreAttackIds { get; } = new[] { "T1217", "T1539", "T1555.003" };

        public void PrintInfo(bool isDebug)
        {
            Beaprint.GreatPrint("Browsers Information", "T1217,T1539,T1555.003");

            new List<IBrowser>
            {
                new Firefox(),
                new Chrome(),
                new Opera(),
                new Brave(),
                new InternetExplorer(),
            }.ForEach(browser => CheckRunner.Run(browser.PrintInfo, isDebug));

            CheckRunner.Run(PrintEdgeCredentialStorePaths, isDebug);
        }

        private static void PrintEdgeCredentialStorePaths()
        {
            string[] files = GetEdgeCredentialStorePaths(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            if (files[0] == null) return;
            Beaprint.MainPrint("Edge saved-login store paths (current user, Default profile)");
            Beaprint.NoColorPrint("    Login Data: " + files[0]);
            if (files[1] != null) Beaprint.NoColorPrint("    Local State: " + files[1]);
            Beaprint.GrayPrint("    File presence does not establish saved logins or usable credentials. DPAPI context, file access, and account reuse require separate review; contents were not read.");
        }

        // Exact current-profile paths only; no profile enumeration or credential decryption.
        internal static string[] GetEdgeCredentialStorePaths(string localAppData)
        {
            var result = new string[2];
            if (string.IsNullOrEmpty(localAppData) || !Path.IsPathRooted(localAppData)) return result;
            try
            {
                string userData = Path.Combine(localAppData, "Microsoft", "Edge", "User Data");
                string profile = Path.Combine(userData, "Default");
                if (!IsPlainFixedDirectory(profile)) return result;
                string loginData = Path.Combine(profile, "Login Data");
                string localState = Path.Combine(userData, "Local State");
                if (IsPlainFile(loginData)) result[0] = loginData;
                if (IsPlainFile(localState)) result[1] = localState;
            }
            catch (Exception) { /* Missing or inaccessible profile is an unknown, not a failed check. */ }
            return result;
        }

        private static bool IsPlainFixedDirectory(string path)
        {
            string full = Path.GetFullPath(path);
            string root = Path.GetPathRoot(full);
            if (string.IsNullOrEmpty(root) || root.StartsWith(@"\\") ||
                new DriveInfo(root).DriveType != DriveType.Fixed) return false;
            string current = root;
            foreach (string part in full.Substring(root.Length).Split(
                new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, part);
                FileAttributes attributes = File.GetAttributes(current);
                if ((attributes & FileAttributes.Directory) == 0 ||
                    (attributes & FileAttributes.ReparsePoint) != 0) return false;
            }
            return true;
        }

        private static bool IsPlainFile(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                return (attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0;
            }
            catch (Exception) { return false; }
        }
    }
}
