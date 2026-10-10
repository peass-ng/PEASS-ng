using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using winPEAS.Helpers;

namespace winPEAS.Info.WindowsCreds
{
    internal sealed class AccessibilityBinaryInfo
    {
        public string Path { get; set; }
        public string Sha256 { get; set; }
        public long? Size { get; set; }
        public DateTime? LastWriteTimeUtc { get; set; }
        public string Owner { get; set; }
        public string FileVersion { get; set; }
        public string OriginalFileName { get; set; }
        public string Signer { get; set; }
        public string SignatureStatus { get; set; }
        public FileAttributes? Attributes { get; set; }
        public uint? HardLinkCount { get; set; }
        public List<string> LowPrivilegeWriteAces { get; } = new List<string>();
        public List<string> Issues { get; } = new List<string>();

        public bool IsSuspicious => Issues.Count > 0;
    }

    internal static class AccessibilityBinaryIntegrity
    {
        private const FileSystemRights DangerousRights =
            FileSystemRights.WriteData |
            FileSystemRights.AppendData |
            FileSystemRights.WriteExtendedAttributes |
            FileSystemRights.WriteAttributes |
            FileSystemRights.Delete |
            FileSystemRights.ChangePermissions |
            FileSystemRights.TakeOwnership;

        private const uint GenericRead = 0x80000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint FileShareDelete = 0x00000004;
        private const uint OpenExisting = 3;
        private const uint FileFlagBackupSemantics = 0x02000000;

        private static readonly string[] BinaryNames = { "sethc.exe", "utilman.exe" };
        public static List<AccessibilityBinaryInfo> GetBinaryInfo()
        {
            var results = new List<AccessibilityBinaryInfo>();
            foreach (var location in GetSystemLocations())
            {
                foreach (string binaryName in BinaryNames)
                {
                    results.Add(InspectBinary(
                        Path.Combine(location.Item1, binaryName),
                        Path.Combine(location.Item2, binaryName),
                        binaryName));
                }
            }
            return results;
        }

        private static IEnumerable<Tuple<string, string>> GetSystemLocations()
        {
            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (string.IsNullOrEmpty(windowsDirectory))
            {
                windowsDirectory = Path.GetPathRoot(Environment.SystemDirectory) + "Windows";
            }

            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                // Sysnative bypasses WOW64 file-system redirection for a 32-bit winPEAS process.
                yield return Tuple.Create(
                    Path.Combine(windowsDirectory, "Sysnative"),
                    Path.Combine(windowsDirectory, "System32"));
                yield return Tuple.Create(
                    Path.Combine(windowsDirectory, "SysWOW64"),
                    Path.Combine(windowsDirectory, "SysWOW64"));
                yield break;
            }

            yield return Tuple.Create(
                Path.Combine(windowsDirectory, "System32"),
                Path.Combine(windowsDirectory, "System32"));

            if (Environment.Is64BitOperatingSystem)
            {
                yield return Tuple.Create(
                    Path.Combine(windowsDirectory, "SysWOW64"),
                    Path.Combine(windowsDirectory, "SysWOW64"));
            }
        }

        private static AccessibilityBinaryInfo InspectBinary(
            string accessPath,
            string displayPath,
            string expectedFileName)
        {
            var result = new AccessibilityBinaryInfo
            {
                Path = displayPath,
                SignatureStatus = "Not checked"
            };

            if (!File.Exists(accessPath))
            {
                result.Issues.Add("file is missing or inaccessible");
                return result;
            }

            try
            {
                var file = new FileInfo(accessPath);
                result.Size = file.Length;
                result.LastWriteTimeUtc = file.LastWriteTimeUtc;
                result.Attributes = file.Attributes;
                result.HardLinkCount = GetHardLinkCount(accessPath);
                result.Sha256 = GetSha256(accessPath);
                result.Owner = GetOwner(accessPath);

                if ((file.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    result.Issues.Add("file is a reparse point");
                }

                FileVersionInfo version = FileVersionInfo.GetVersionInfo(accessPath);
                result.FileVersion = version.FileVersion;
                result.OriginalFileName = version.OriginalFilename;
                if (!string.Equals(version.OriginalFilename, expectedFileName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Issues.Add("PE original filename is not " + expectedFileName);
                }

                bool signatureValid = AuthenticodeHelper.Verify(accessPath, out string signatureStatus);
                result.SignatureStatus = signatureStatus;
                result.Signer = GetSigner(accessPath);
                if (!signatureValid)
                {
                    result.Issues.Add("Authenticode trust validation failed");
                }
                if (!string.IsNullOrEmpty(result.Signer) &&
                    result.Signer.IndexOf("Microsoft", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    result.Issues.Add("signer is not Microsoft");
                }

                if (!IsExpectedOwner(result.Owner))
                {
                    result.Issues.Add("unexpected owner");
                }

                result.LowPrivilegeWriteAces.AddRange(GetLowPrivilegeWriteAces(accessPath));
                if (result.LowPrivilegeWriteAces.Count > 0)
                {
                    result.Issues.Add("low-privilege principal has a write-equivalent allow ACE");
                }
            }
            catch (Exception ex)
            {
                result.Issues.Add("inspection failed: " + ex.Message);
            }

            return result;
        }

        private static string GetSha256(string path)
        {
            using (var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                8192,
                FileOptions.SequentialScan))
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static string GetOwner(string path)
        {
            FileSecurity security = File.GetAccessControl(path, AccessControlSections.Owner);
            IdentityReference owner = security.GetOwner(typeof(SecurityIdentifier));
            try
            {
                return owner.Translate(typeof(NTAccount)).Value + " [" + owner.Value + "]";
            }
            catch
            {
                return owner.Value;
            }
        }

        private static bool IsExpectedOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner))
            {
                return false;
            }

            string[] expectedOwnerSids =
            {
                "S-1-5-18",     // Local System
                "S-1-5-32-544", // Built-in Administrators
                "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464" // TrustedInstaller
            };
            return expectedOwnerSids.Any(sid =>
                string.Equals(owner, sid, StringComparison.OrdinalIgnoreCase) ||
                owner.EndsWith("[" + sid + "]", StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> GetLowPrivilegeWriteAces(string path)
        {
            var findings = new List<string>();
            HashSet<string> lowPrivilegeSids = PermissionsHelper.GetUnprivilegedTokenSids();
            FileSecurity security = File.GetAccessControl(path, AccessControlSections.Access);

            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                if (rule.AccessControlType != AccessControlType.Allow ||
                    (rule.FileSystemRights & DangerousRights) == 0 ||
                    !lowPrivilegeSids.Contains(rule.IdentityReference.Value))
                {
                    continue;
                }

                string identity = rule.IdentityReference.Value;
                try
                {
                    identity = rule.IdentityReference.Translate(typeof(NTAccount)).Value;
                }
                catch
                {
                    // Keep the SID when the account name cannot be resolved.
                }

                findings.Add(identity + ": " + (rule.FileSystemRights & DangerousRights));
            }

            return findings.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string GetSigner(string path)
        {
            try
            {
                using (var signedFileCertificate = X509Certificate.CreateFromSignedFile(path))
                using (var certificate = new X509Certificate2(signedFileCertificate))
                {
                    return certificate.Subject;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static uint? GetHardLinkCount(string path)
        {
            using (SafeFileHandle handle = CreateFile(
                path,
                GenericRead,
                FileShareRead | FileShareWrite | FileShareDelete,
                IntPtr.Zero,
                OpenExisting,
                FileFlagBackupSemantics,
                IntPtr.Zero))
            {
                if (handle.IsInvalid || !GetFileInformationByHandle(handle, out ByHandleFileInformation info))
                {
                    return null;
                }
                return info.NumberOfLinks;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FileTime
        {
            public uint Low;
            public uint High;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ByHandleFileInformation
        {
            public uint FileAttributes;
            public FileTime CreationTime;
            public FileTime LastAccessTime;
            public FileTime LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileInformationByHandle(
            SafeFileHandle fileHandle,
            out ByHandleFileInformation fileInformation);
    }
}
