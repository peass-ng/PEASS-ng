using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Native;
using winPEAS.Native.Enums;
using winPEAS.Helpers;

namespace winPEAS.Info.SystemInfo.Printers
{
    internal sealed class PrinterDriverAclFinding
    {
        public string Path { get; set; }
        public string Rights { get; set; }
    }

    internal sealed class PrinterDriverAclReport
    {
        public List<PrinterDriverAclFinding> Findings { get; } = new List<PrinterDriverAclFinding>();
        public bool RootPresent { get; set; }
        public bool Partial { get; set; }
        public int DriversInspected { get; set; }
        public int DllsInspected { get; set; }
    }

    internal class Printers
    {
        internal const int MaxDriverFolders = 16;
        internal const int MaxDllsPerDriver = 16;
        internal const int MaxAclFindings = 16;
        internal const int MaxAclMilliseconds = 1500;

        public static PrinterDriverAclReport GetDriverAclReview()
        {
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var watch = Stopwatch.StartNew();
            return ReviewDriverAclPaths(
                programData,
                path => Directory.EnumerateDirectories(path),
                path => Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly),
                Directory.Exists,
                path => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0,
                path => AllowRights(PermissionsHelper.GetPermissionsFolder(
                    path, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT)),
                path => GetExactFileAclCandidate(path, File.GetAccessControl,
                    security => PermissionsHelper.GetMyPermissionsF(
                        security, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT)),
                () => watch.ElapsedMilliseconds);
        }

        // The shared file helper extracts a pathname from command text and can stop at a
        // dot in a directory name. These paths already come from bounded file enumeration.
        internal static string GetExactFileAclCandidate(string path,
            Func<string, FileSecurity> getSecurity,
            Func<FileSecurity, IEnumerable<string>> describePermissions)
        {
            try
            {
                return string.IsNullOrEmpty(path) ? null :
                    AllowRights(describePermissions(getSecurity(path)));
            }
            catch
            {
                return null;
            }
        }

        private static string AllowRights(IEnumerable<string> permissions)
        {
            if (permissions == null) return null;
            foreach (string permission in permissions)
            {
                if (permission != null && permission.Contains("[Allow:")) return permission;
            }
            return null;
        }

        // Fixed-depth metadata review. The folder is an installed-driver lead,
        // not proof that a DLL is loaded or that any vendor patch is absent.
        internal static PrinterDriverAclReport ReviewDriverAclPaths(
            string programData,
            Func<string, IEnumerable<string>> directories,
            Func<string, IEnumerable<string>> dlls,
            Func<string, bool> exists,
            Func<string, bool> reparse,
            Func<string, string> directoryRights,
            Func<string, string> fileRights,
            Func<long> elapsedMilliseconds)
        {
            var report = new PrinterDriverAclReport();
            if (string.IsNullOrWhiteSpace(programData) || programData.StartsWith(@"\\", StringComparison.Ordinal)) return report;

            string root = Path.Combine(programData, "RICOH_DRV");
            try
            {
                if (reparse(programData) || !exists(root) || reparse(root)) return report;
                report.RootPresent = true;
                int drivers = 0;
                foreach (string driver in directories(root))
                {
                    if (++drivers > MaxDriverFolders || elapsedMilliseconds() >= MaxAclMilliseconds)
                    {
                        report.Partial = true;
                        break;
                    }
                    if (!string.Equals(Path.GetDirectoryName(driver), root, StringComparison.OrdinalIgnoreCase) || reparse(driver)) continue;
                    report.DriversInspected++;

                    string common = Path.Combine(driver, "_common");
                    string dlz = Path.Combine(common, "dlz");
                    if (!exists(common) || reparse(common) || !exists(dlz) || reparse(dlz)) continue;

                    foreach (string directory in new[] { root, driver, common, dlz })
                    {
                        if (elapsedMilliseconds() >= MaxAclMilliseconds)
                        {
                            report.Partial = true;
                            return report;
                        }
                        AddAclFinding(report, directory, directoryRights(directory));
                        if (report.Findings.Count >= MaxAclFindings)
                        {
                            report.Partial = true;
                            return report;
                        }
                    }

                    int files = 0;
                    foreach (string dll in dlls(dlz))
                    {
                        if (++files > MaxDllsPerDriver || elapsedMilliseconds() >= MaxAclMilliseconds)
                        {
                            report.Partial = true;
                            break;
                        }
                        if (!string.Equals(Path.GetDirectoryName(dll), dlz, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(Path.GetExtension(dll), ".dll", StringComparison.OrdinalIgnoreCase) ||
                            reparse(dll)) continue;
                        report.DllsInspected++;
                        AddAclFinding(report, dll, fileRights(dll));
                        if (report.Findings.Count >= MaxAclFindings)
                        {
                            report.Partial = true;
                            return report;
                        }
                    }
                }
            }
            catch
            {
                report.Partial = true;
            }
            return report;
        }

        private static void AddAclFinding(PrinterDriverAclReport report, string path, string rights)
        {
            if (!string.IsNullOrEmpty(rights))
            {
                foreach (PrinterDriverAclFinding finding in report.Findings)
                {
                    if (string.Equals(finding.Path, path, StringComparison.OrdinalIgnoreCase)) return;
                }
                report.Findings.Add(new PrinterDriverAclFinding { Path = path, Rights = rights });
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_INFOS
        {
            public string Owner;
            public RawSecurityDescriptor SecurityDescriptor;
            public string SDDL;
        }

        public static IEnumerable<PrinterInfo> GetPrinterWMIInfos()
        {
            var result = new List<PrinterInfo>();

            using (var printerQuery = new ManagementObjectSearcher("SELECT * from Win32_Printer"))
            {
                try
                {
                    foreach (var printer in printerQuery.Get())
                    {
                        var isDefault = (bool)printer.GetPropertyValue("Default");
                        var isNetworkPrinter = (bool)printer.GetPropertyValue("Network");
                        string printerSddl = null;
                        var printerName = $"{printer.GetPropertyValue("Name")}";
                        var status = $"{printer.GetPropertyValue("Status")}";

                        try
                        {
                            var info = GetSecurityInfos(printerName, SE_OBJECT_TYPE.SE_PRINTER);
                            printerSddl = info.SDDL;
                        }
                        catch { }

                        result.Add(new PrinterInfo(
                            printerName,
                            status,
                            printerSddl,
                            isDefault,
                            isNetworkPrinter
                        ));
                    }
                }
                catch (Exception)
                {
                }
            }

            return result;
        }

        private static SECURITY_INFOS GetSecurityInfos(string ObjectName, SE_OBJECT_TYPE ObjectType)
        {
            var pSidOwner = IntPtr.Zero;
            var pSidGroup = IntPtr.Zero;
            var pDacl = IntPtr.Zero;
            var pSacl = IntPtr.Zero;
            var pSecurityDescriptor = IntPtr.Zero;
            var info = SecurityInfos.DiscretionaryAcl | SecurityInfos.Owner;

            var infos = new SECURITY_INFOS();

            // get the security infos
            var errorReturn = Advapi32.GetNamedSecurityInfo(ObjectName, ObjectType, info, out pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);
            if (errorReturn != 0)
            {
                return infos;
            }

            if (Advapi32.ConvertSecurityDescriptorToStringSecurityDescriptor(pSecurityDescriptor, 1, SecurityInfos.DiscretionaryAcl | SecurityInfos.Owner, out var pSddlString, out _))
            {
                infos.SDDL = Marshal.PtrToStringUni(pSddlString) ?? string.Empty;
            }
            var ownerSid = new SecurityIdentifier(pSidOwner);
            infos.Owner = ownerSid.Value;

            if (pSddlString != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pSddlString);
            }

            if (pSecurityDescriptor != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pSecurityDescriptor);
            }

            return infos;
        }
    }
}
