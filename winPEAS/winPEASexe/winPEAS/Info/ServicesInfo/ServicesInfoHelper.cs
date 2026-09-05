using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.Native;

namespace winPEAS.Info.ServicesInfo
{
    internal sealed class WritableServiceDllInfo
    {
        public string ServiceName { get; set; }
        public string Account { get; set; }
        public string ServiceDllPath { get; set; }
        public bool FileExists { get; set; }
        public string AccessReason { get; set; }
    }

    internal sealed class WritableServiceDllReport
    {
        public List<WritableServiceDllInfo> Findings { get; } = new List<WritableServiceDllInfo>();
        public int ServicesInspected { get; set; }
        public bool ServiceLimitReached { get; set; }
        public bool FindingLimitReached { get; set; }
    }

    class ServicesInfoHelper
    {
        internal const int MaxServiceDllServices = 4096;
        internal const int MaxServiceDllFindings = 64;

        private const int ServiceWin32ShareProcess = 0x20;
        private const int ServiceWin32TypeMask = 0x30;
        private const int ServiceDisabled = 0x4;
        private const int FileWriteData = 0x00000002;
        private const int FileAddFile = 0x00000002;
        private const int FileDeleteChild = 0x00000040;
        private const int Delete = 0x00010000;
        private const int WriteDac = 0x00040000;
        private const int WriteOwner = 0x00080000;
        private const int FileAllAccess = 0x001F01FF;
        private const int FileGenericRead = 0x00120089;
        private const int FileGenericWrite = 0x00120116;
        private const int FileGenericExecute = 0x001200A0;
        private const int GenericAll = 0x10000000;
        private const int GenericExecute = 0x20000000;
        private const int GenericWrite = 0x40000000;
        private const int GenericRead = unchecked((int)0x80000000);

        ///////////////////////////////////////////////
        //// Non Standard Services (Non Microsoft) ////
        ///////////////////////////////////////////////
        public static List<Dictionary<string, string>> GetNonstandardServices()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

            try
            {
                using (ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_service"))
                {
                    using (ManagementObjectCollection data = wmiData.Get())
                    {
                        foreach (ManagementObject result in data)
                        {
                            if (result["PathName"] != null)
                            {
                                string binaryPath = MyUtils.GetExecutableFromPath(result["PathName"].ToString());
                                string companyName = "";
                                string isDotNet = "";
                                try
                                {
                                    FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(binaryPath);
                                    companyName = myFileVersionInfo.CompanyName;
                                    isDotNet = MyUtils.CheckIfDotNet(binaryPath) ? "isDotNet" : "";
                                }
                                catch (Exception)
                                {
                                    // Not enough privileges
                                }

                                if (string.IsNullOrEmpty(companyName) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                                {
                                    Dictionary<string, string> toadd = new Dictionary<string, string>
                                    {
                                        ["Name"] = GetStringOrEmpty(result["Name"]),
                                        ["DisplayName"] = GetStringOrEmpty(result["DisplayName"]),
                                        ["CompanyName"] = companyName,
                                        ["State"] = GetStringOrEmpty(result["State"]),
                                        ["StartMode"] = GetStringOrEmpty(result["StartMode"]),
                                        ["PathName"] = GetStringOrEmpty(result["PathName"]),
                                        ["FilteredPath"] = binaryPath,
                                        ["isDotNet"] = isDotNet,
                                        ["Description"] = GetStringOrEmpty(result["Description"])
                                    };

                                    results.Add(toadd);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return results;
        }

        private static string GetStringOrEmpty(object obj)
        {
            return obj == null ? string.Empty : obj.ToString();
        }

        public static List<Dictionary<string, string>> GetNonstandardServicesFromReg()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

            try
            {
                foreach (string key in RegistryHelper.GetRegSubkeys("HKLM", @"SYSTEM\CurrentControlSet\Services"))
                {
                    Dictionary<string, object> key_values = RegistryHelper.GetRegValues("HKLM", @"SYSTEM\CurrentControlSet\Services\" + key);

                    if (key_values.ContainsKey("DisplayName") && key_values.ContainsKey("ImagePath"))
                    {
                        string companyName = "";
                        string isDotNet = "";
                        string pathName = Environment.ExpandEnvironmentVariables(string.Format("{0}", key_values["ImagePath"]).Replace("\\SystemRoot\\", "%SystemRoot%\\"));
                        string binaryPath = MyUtils.ReconstructExecPath(pathName);
                        if (binaryPath != "")
                        {
                            try
                            {
                                FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(binaryPath);
                                companyName = myFileVersionInfo.CompanyName;
                                isDotNet = MyUtils.CheckIfDotNet(binaryPath) ? "isDotNet" : "";
                            }
                            catch (Exception)
                            {
                                // Not enough privileges
                            }
                        }

                        string displayName = string.Format("{0}", key_values["DisplayName"]);
                        string imagePath = string.Format("{0}", key_values["ImagePath"]);
                        string description = key_values.ContainsKey("Description") ? string.Format("{0}", key_values["Description"]) : "";
                        string startMode = "";
                        if (key_values.ContainsKey("Start"))
                        {
                            switch (key_values["Start"].ToString())
                            {
                                case "0":
                                    startMode = "Boot";
                                    break;
                                case "1":
                                    startMode = "System";
                                    break;
                                case "2":
                                    startMode = "Autoload";
                                    break;
                                case "3":
                                    startMode = "System";
                                    break;
                                case "4":
                                    startMode = "Manual";
                                    break;
                                case "5":
                                    startMode = "Disabled";
                                    break;
                            }
                        }
                        if (string.IsNullOrEmpty(companyName) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                        {
                            Dictionary<string, string> toadd = new Dictionary<string, string>
                            {
                                ["Name"] = displayName,
                                ["DisplayName"] = displayName,
                                ["CompanyName"] = companyName,
                                ["State"] = "",
                                ["StartMode"] = startMode,
                                ["PathName"] = pathName,
                                ["FilteredPath"] = binaryPath,
                                ["isDotNet"] = isDotNet,
                                ["Description"] = description
                            };
                            results.Add(toadd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }

        public static Dictionary<string, string> GetModifiableServices(Dictionary<string, string> SIDs)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            ServiceController[] scServices;
            scServices = ServiceController.GetServices();

            var GetServiceHandle = typeof(System.ServiceProcess.ServiceController).GetMethod("GetServiceHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            object[] readRights = { 0x00020000 };

            foreach (ServiceController sc in scServices)
            {
                try
                {
                    IntPtr handle = (IntPtr)GetServiceHandle.Invoke(sc, readRights);
                    ServiceControllerStatus status = sc.Status;
                    byte[] psd = new byte[0];
                    bool ok = Advapi32.QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, psd, 0, out uint bufSizeNeeded);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 122 || err == 0)
                        { // ERROR_INSUFFICIENT_BUFFER
                          // expected; now we know bufsize
                            psd = new byte[bufSizeNeeded];
                            ok = Advapi32.QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, psd, bufSizeNeeded, out bufSizeNeeded);
                        }
                        else
                        {
                            //throw new ApplicationException("error calling QueryServiceObjectSecurity() to get DACL for " + _name + ": error code=" + err);
                            continue;
                        }
                    }
                    if (!ok)
                    {
                        //throw new ApplicationException("error calling QueryServiceObjectSecurity(2) to get DACL for " + _name + ": error code=" + Marshal.GetLastWin32Error());
                        continue;
                    }

                    // get security descriptor via raw into DACL form so ACE ordering checks are done for us.
                    RawSecurityDescriptor rsd = new RawSecurityDescriptor(psd, 0);
                    RawAcl racl = rsd.DiscretionaryAcl;
                    DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, racl);

                    List<string> permissions = new List<string>();

                    foreach (System.Security.AccessControl.CommonAce ace in dacl)
                    {
                        if (SIDs.ContainsKey(ace.SecurityIdentifier.ToString()))
                        {
                            string aceType = ace.AceType.ToString();
                            if (!(aceType.Contains("Denied")))
                            { //https://docs.microsoft.com/en-us/dotnet/api/system.security.accesscontrol.commonace?view=net-6.0
                                int serviceRights = ace.AccessMask;
                                string current_perm_str = PermissionsHelper.PermInt2Str(serviceRights, PermissionType.WRITEABLE_OR_EQUIVALENT_SVC);

                                if (!string.IsNullOrEmpty(current_perm_str) && !permissions.Contains(current_perm_str))
                                    permissions.Add(current_perm_str);
                            }
                        }
                    }

                    if (permissions.Count > 0)
                    {
                        string perms = String.Join(", ", permissions);
                        if (perms.Replace("Start", "").Replace("Stop", "").Length > 3) //Check if any other permissions appart from Start and Stop
                            results.Add(sc.ServiceName, perms);
                    }

                }
                catch (Exception)
                {
                    //Beaprint.PrintException(ex.Message)
                }
            }
            return results;
        }

        //////////////////////////////////////////
        ///////  Find Write reg. Services ////////
        //////////////////////////////////////////
        /// Find Services which Reg you have write or equivalent access
        public static List<Dictionary<string, string>> GetWriteServiceRegs(Dictionary<string, string> NtAccountNames)
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            try
            {
                RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"system\currentcontrolset\services");
                foreach (string serviceRegName in regKey.GetSubKeyNames())
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"system\currentcontrolset\services\" + serviceRegName);
                    List<string> perms = PermissionsHelper.GetMyPermissionsR(key, NtAccountNames);
                    if (perms.Count > 0)
                    {
                        results.Add(new Dictionary<string, string> {
                        { "Path", @"HKLM\system\currentcontrolset\services\" + serviceRegName },
                        { "Permissions", string.Join(", ", perms) }
                    });
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }


        private static readonly DateTime LegacyDriverCutoff = new DateTime(2015, 7, 29);

        public static List<KernelDriverInfo> GetKernelDriverInfos()
        {
            List<KernelDriverInfo> drivers = new List<KernelDriverInfo>();

            try
            {
                using (ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT Name,DisplayName,PathName,StartMode,State,ServiceType FROM win32_service"))
                {
                    using (ManagementObjectCollection data = wmiData.Get())
                    {
                        foreach (ManagementObject result in data)
                        {
                            string serviceType = GetStringOrEmpty(result["ServiceType"]);
                            if (string.IsNullOrEmpty(serviceType) || !serviceType.ToLowerInvariant().Contains("kernel driver"))
                                continue;

                            string binaryPath = MyUtils.ReconstructExecPath(GetStringOrEmpty(result["PathName"]));

                            drivers.Add(new KernelDriverInfo
                            {
                                Name = GetStringOrEmpty(result["Name"]),
                                DisplayName = GetStringOrEmpty(result["DisplayName"]),
                                StartMode = GetStringOrEmpty(result["StartMode"]),
                                State = GetStringOrEmpty(result["State"]),
                                PathName = binaryPath,
                                Signature = GetDriverSignatureInfo(binaryPath)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }

            return drivers;
        }

        private static KernelDriverSignatureInfo GetDriverSignatureInfo(string binaryPath)
        {
            KernelDriverSignatureInfo info = new KernelDriverSignatureInfo
            {
                FilePath = binaryPath,
                IsSigned = false
            };

            if (string.IsNullOrEmpty(binaryPath) || !File.Exists(binaryPath))
            {
                info.Error = "Binary not found";
                return info;
            }

            try
            {
                using (var baseCertificate = X509Certificate.CreateFromSignedFile(binaryPath))
                using (var certificate = new X509Certificate2(baseCertificate))
                {
                    info.IsSigned = true;
                    info.Subject = certificate.Subject;
                    info.Issuer = certificate.Issuer;
                    info.NotBefore = certificate.NotBefore;
                    info.NotAfter = certificate.NotAfter;
                    info.IsLegacyExpired = certificate.NotAfter < LegacyDriverCutoff;
                }
            }
            catch (CryptographicException cryptoEx)
            {
                info.Error = cryptoEx.Message;
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }

            return info;
        }

        internal class KernelDriverInfo
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string PathName { get; set; }
            public string StartMode { get; set; }
            public string State { get; set; }
            public KernelDriverSignatureInfo Signature { get; set; }
        }

        internal class KernelDriverSignatureInfo
        {
            public string FilePath { get; set; }
            public bool IsSigned { get; set; }
            public string Subject { get; set; }
            public string Issuer { get; set; }
            public DateTime? NotBefore { get; set; }
            public DateTime? NotAfter { get; set; }
            public bool IsLegacyExpired { get; set; }
            public string Error { get; set; }
        }


        //////////////////////////////////////////////////////
        //////// Writable LocalSystem service DLLs ///////////
        //////////////////////////////////////////////////////
        public static WritableServiceDllReport GetWritableSystemServiceDlls(Dictionary<string, string> currentUserSids)
        {
            var report = new WritableServiceDllReport();
            if (currentUserSids == null || currentUserSids.Count == 0)
            {
                return report;
            }

            string windowsDirectory = Environment.GetEnvironmentVariable("SystemRoot");
            if (string.IsNullOrWhiteSpace(windowsDirectory))
            {
                windowsDirectory = Environment.GetEnvironmentVariable("windir");
            }
            if (string.IsNullOrWhiteSpace(windowsDirectory))
            {
                return report;
            }

            var tokenSids = new HashSet<string>(currentUserSids.Keys, StringComparer.OrdinalIgnoreCase);

            try
            {
                using (RegistryKey servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
                {
                    if (servicesKey == null)
                    {
                        return report;
                    }

                    string[] serviceNames = servicesKey.GetSubKeyNames();
                    foreach (string serviceName in serviceNames)
                    {
                        if (report.ServicesInspected >= MaxServiceDllServices)
                        {
                            report.ServiceLimitReached = true;
                            break;
                        }
                        if (report.Findings.Count >= MaxServiceDllFindings)
                        {
                            report.FindingLimitReached = true;
                            break;
                        }

                        report.ServicesInspected++;
                        try
                        {
                            using (RegistryKey serviceKey = servicesKey.OpenSubKey(serviceName))
                            {
                                if (serviceKey == null || !IsEligibleSystemService(
                                    GetRegistryInt(serviceKey, "Type"),
                                    GetRegistryInt(serviceKey, "Start"),
                                    GetRegistryInt(serviceKey, "LaunchProtected"),
                                    GetRegistryString(serviceKey, "ObjectName"),
                                    GetRegistryString(serviceKey, "ImagePath"),
                                    windowsDirectory))
                                {
                                    continue;
                                }

                                string rawServiceDll;
                                if (!TryGetServiceDll(serviceKey, out rawServiceDll))
                                {
                                    continue;
                                }

                                string serviceDllPath = NormalizeServiceDllPath(rawServiceDll, windowsDirectory);
                                if (string.IsNullOrWhiteSpace(serviceDllPath) || serviceDllPath.Length > 32767 ||
                                    serviceDllPath.IndexOf('%') >= 0 || !IsLocalDrivePath(serviceDllPath))
                                {
                                    continue;
                                }

                                string accessPath = GetFileSystemAccessPath(
                                    serviceDllPath,
                                    windowsDirectory,
                                    Environment.Is64BitOperatingSystem,
                                    Environment.Is64BitProcess);
                                bool fileExists = File.Exists(accessPath);
                                string directoryPath = Path.GetDirectoryName(accessPath);
                                if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                                {
                                    continue;
                                }

                                RawSecurityDescriptor fileSecurity = fileExists
                                    ? GetSecurityDescriptor(accessPath, false)
                                    : null;
                                RawSecurityDescriptor directorySecurity = GetSecurityDescriptor(directoryPath, true);
                                string accessReason = GetReplacementReason(
                                    fileSecurity,
                                    directorySecurity,
                                    fileExists,
                                    tokenSids);
                                if (string.IsNullOrEmpty(accessReason))
                                {
                                    continue;
                                }

                                report.Findings.Add(new WritableServiceDllInfo
                                {
                                    ServiceName = serviceName,
                                    Account = GetDisplayAccount(GetRegistryString(serviceKey, "ObjectName")),
                                    ServiceDllPath = serviceDllPath,
                                    FileExists = fileExists,
                                    AccessReason = accessReason,
                                });
                            }
                        }
                        catch
                        {
                            // A malformed or inaccessible service entry must not stop enumeration.
                        }
                    }
                }
            }
            catch
            {
                // Service registry enumeration can be restricted; keep this passive check quiet.
            }

            return report;
        }

        internal static bool IsEligibleSystemService(
            int? serviceType,
            int? startType,
            int? launchProtected,
            string account,
            string imagePath,
            string windowsDirectory)
        {
            return serviceType.HasValue && startType.HasValue &&
                   (serviceType.Value & ServiceWin32TypeMask) == ServiceWin32ShareProcess &&
                   startType.Value != ServiceDisabled &&
                   (!launchProtected.HasValue || launchProtected.Value == 0) &&
                   IsLocalSystemAccount(account) &&
                   IsSystemSvchostImage(imagePath, windowsDirectory);
        }

        internal static bool IsLocalSystemAccount(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                return true;
            }

            string normalized = account.Trim();
            return normalized.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals(@"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals(@"NT AUTHORITY\LocalSystem", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsSystemSvchostImage(string rawImagePath, string windowsDirectory)
        {
            if (string.IsNullOrWhiteSpace(rawImagePath) || string.IsNullOrWhiteSpace(windowsDirectory))
            {
                return false;
            }

            string imagePath = rawImagePath.Trim();
            string executable;
            if (imagePath.StartsWith("\"", StringComparison.Ordinal))
            {
                int closingQuote = imagePath.IndexOf('"', 1);
                if (closingQuote < 0)
                {
                    return false;
                }
                executable = imagePath.Substring(1, closingQuote - 1);
            }
            else
            {
                int extension = imagePath.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (extension < 0)
                {
                    return false;
                }
                executable = imagePath.Substring(0, extension + 4).Trim();
            }

            executable = StripWin32DevicePrefix(executable);
            executable = ExpandWindowsPath(executable, windowsDirectory);
            executable = StripWin32DevicePrefix(executable).Replace('/', '\\');
            string windowsRoot = windowsDirectory.TrimEnd('\\', '/');
            return executable.Equals(windowsRoot + @"\System32\svchost.exe", StringComparison.OrdinalIgnoreCase) ||
                   executable.Equals(windowsRoot + @"\SysWOW64\svchost.exe", StringComparison.OrdinalIgnoreCase);
        }

        internal static string NormalizeServiceDllPath(string rawPath, string windowsDirectory)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            string path = rawPath.Trim();
            if (path.Length >= 2 && path[0] == '"' && path[path.Length - 1] == '"')
            {
                path = path.Substring(1, path.Length - 2).Trim();
            }

            path = StripWin32DevicePrefix(path);
            path = ExpandWindowsPath(path, windowsDirectory);
            path = StripWin32DevicePrefix(path);
            return path.Trim().Replace('/', '\\');
        }

        internal static string GetFileSystemAccessPath(
            string path,
            string windowsDirectory,
            bool is64BitOperatingSystem,
            bool is64BitProcess)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(windowsDirectory) ||
                !is64BitOperatingSystem || is64BitProcess)
            {
                return path;
            }

            string windowsRoot = windowsDirectory.TrimEnd('\\', '/');
            string system32Prefix = windowsRoot + @"\System32\";
            if (!path.StartsWith(system32Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return windowsRoot + @"\Sysnative\" + path.Substring(system32Prefix.Length);
        }

        internal static string GetReplacementReason(
            RawSecurityDescriptor fileSecurity,
            RawSecurityDescriptor directorySecurity,
            bool fileExists,
            ISet<string> tokenSids)
        {
            if (directorySecurity == null || tokenSids == null || tokenSids.Count == 0)
            {
                return string.Empty;
            }

            bool canControlDirectory = HasEffectiveAccess(directorySecurity, tokenSids, WriteDac) ||
                                       HasEffectiveAccess(directorySecurity, tokenSids, WriteOwner);
            if (!fileExists)
            {
                if (HasEffectiveAccess(directorySecurity, tokenSids, FileAddFile))
                {
                    return "FILE_ADD_FILE on the parent directory can plant the missing service DLL";
                }
                if (canControlDirectory)
                {
                    return "WRITE_DAC or WRITE_OWNER on the parent directory can grant DLL creation rights";
                }
                return string.Empty;
            }

            if (fileSecurity != null && HasEffectiveAccess(fileSecurity, tokenSids, FileWriteData))
            {
                return "FILE_WRITE_DATA is granted on the configured service DLL";
            }
            if (fileSecurity != null &&
                (HasEffectiveAccess(fileSecurity, tokenSids, WriteDac) ||
                 HasEffectiveAccess(fileSecurity, tokenSids, WriteOwner)))
            {
                return "WRITE_DAC or WRITE_OWNER on the service DLL can grant overwrite rights";
            }
            if (canControlDirectory)
            {
                return "WRITE_DAC or WRITE_OWNER on the parent directory can grant replacement rights";
            }

            bool canCreate = HasEffectiveAccess(directorySecurity, tokenSids, FileAddFile);
            if (canCreate && HasEffectiveAccess(directorySecurity, tokenSids, FileDeleteChild))
            {
                return "FILE_ADD_FILE and FILE_DELETE_CHILD on the parent directory permit DLL replacement";
            }
            if (canCreate && fileSecurity != null && HasEffectiveAccess(fileSecurity, tokenSids, Delete))
            {
                return "DELETE on the DLL plus FILE_ADD_FILE on its parent permit DLL replacement";
            }

            return string.Empty;
        }

        internal static bool HasEffectiveAccess(
            RawSecurityDescriptor security,
            ISet<string> tokenSids,
            int desiredAccess)
        {
            if (security == null || tokenSids == null || tokenSids.Count == 0 || desiredAccess == 0)
            {
                return false;
            }

            RawAcl dacl = security.DiscretionaryAcl;
            if (dacl == null)
            {
                return true;
            }

            int remaining = desiredAccess;
            foreach (GenericAce genericAce in dacl)
            {
                CommonAce ace = genericAce as CommonAce;
                if (ace == null || ace.IsCallback ||
                    (ace.AceFlags & AceFlags.InheritOnly) == AceFlags.InheritOnly ||
                    !tokenSids.Contains(ace.SecurityIdentifier.Value))
                {
                    continue;
                }

                int accessMask = MapGenericFileRights(ace.AccessMask);
                if (ace.AceQualifier == AceQualifier.AccessDenied && (accessMask & remaining) != 0)
                {
                    return false;
                }
                if (ace.AceQualifier == AceQualifier.AccessAllowed)
                {
                    remaining &= ~accessMask;
                    if (remaining == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int MapGenericFileRights(int accessMask)
        {
            int mapped = accessMask;
            if ((mapped & GenericAll) != 0)
            {
                mapped |= FileAllAccess;
            }
            if ((mapped & GenericRead) != 0)
            {
                mapped |= FileGenericRead;
            }
            if ((mapped & GenericWrite) != 0)
            {
                mapped |= FileGenericWrite;
            }
            if ((mapped & GenericExecute) != 0)
            {
                mapped |= FileGenericExecute;
            }
            return mapped & ~(GenericAll | GenericRead | GenericWrite | GenericExecute);
        }

        private static bool TryGetServiceDll(RegistryKey serviceKey, out string rawServiceDll)
        {
            rawServiceDll = string.Empty;
            RegistryKey parametersKey = null;
            try
            {
                parametersKey = serviceKey.OpenSubKey("Parameters");
                RegistryKey sourceKey = parametersKey ?? serviceKey;

                object manifest = sourceKey.GetValue(
                    "ServiceManifest",
                    null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (manifest != null && !string.IsNullOrWhiteSpace(manifest.ToString()))
                {
                    // A valid ServiceManifest can redirect the DLL independently of ServiceDll.
                    return false;
                }

                RegistryValueKind kind = sourceKey.GetValueKind("ServiceDll");
                if (kind != RegistryValueKind.ExpandString)
                {
                    return false;
                }

                object value = sourceKey.GetValue(
                    "ServiceDll",
                    null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);
                rawServiceDll = value == null ? string.Empty : value.ToString();
                return !string.IsNullOrWhiteSpace(rawServiceDll);
            }
            catch
            {
                return false;
            }
            finally
            {
                parametersKey?.Dispose();
            }
        }

        private static int? GetRegistryInt(RegistryKey key, string valueName)
        {
            try
            {
                object value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                return value == null ? (int?)null : Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }

        private static string GetRegistryString(RegistryKey key, string valueName)
        {
            try
            {
                object value = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                return value == null ? string.Empty : value.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetDisplayAccount(string account)
        {
            return string.IsNullOrWhiteSpace(account) ? "LocalSystem (default)" : account;
        }

        private static string ExpandWindowsPath(string path, string windowsDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string expanded = path.Trim();
            if (!string.IsNullOrWhiteSpace(windowsDirectory))
            {
                expanded = ReplaceWindowsDirectoryPrefix(expanded, @"\SystemRoot", windowsDirectory);
                expanded = ReplaceWindowsDirectoryPrefix(expanded, "%SystemRoot%", windowsDirectory);
                expanded = ReplaceWindowsDirectoryPrefix(expanded, "%windir%", windowsDirectory);
            }

            try
            {
                expanded = Environment.ExpandEnvironmentVariables(expanded);
            }
            catch
            {
                return string.Empty;
            }
            return expanded;
        }

        private static string ReplaceWindowsDirectoryPrefix(string path, string prefix, string windowsDirectory)
        {
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                (path.Length > prefix.Length && path[prefix.Length] != '\\' && path[prefix.Length] != '/'))
            {
                return path;
            }
            return windowsDirectory.TrimEnd('\\', '/') + path.Substring(prefix.Length);
        }

        private static string StripWin32DevicePrefix(string path)
        {
            if (path.StartsWith(@"\??\", StringComparison.Ordinal))
            {
                return path.Substring(4);
            }
            if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                return path.Substring(4);
            }
            return path;
        }

        private static bool IsLocalDrivePath(string path)
        {
            // Avoid UNC or device paths so this passive check cannot intentionally contact a remote host.
            if (path.Length < 3 || !char.IsLetter(path[0]) || path[1] != ':' ||
                (path[2] != '\\' && path[2] != '/'))
            {
                return false;
            }

            try
            {
                DriveType driveType = new DriveInfo(path.Substring(0, 3)).DriveType;
                return driveType == DriveType.Fixed || driveType == DriveType.Removable || driveType == DriveType.Ram;
            }
            catch
            {
                return false;
            }
        }

        private static RawSecurityDescriptor GetSecurityDescriptor(string path, bool isDirectory)
        {
            try
            {
                byte[] descriptor = isDirectory
                    ? Directory.GetAccessControl(path, AccessControlSections.Access).GetSecurityDescriptorBinaryForm()
                    : File.GetAccessControl(path, AccessControlSections.Access).GetSecurityDescriptorBinaryForm();
                return new RawSecurityDescriptor(descriptor, 0);
            }
            catch
            {
                return null;
            }
        }


        //////////////////////////////////////
        ////////  PATH DLL Hijacking /////////
        //////////////////////////////////////
        /// Look for write or equivalent permissions on ay folder in PATH
        public static Dictionary<string, string> GetPathDLLHijacking()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            try
            {
                // grabbed from the registry instead of System.Environment.GetEnvironmentVariable to prevent false positives
                string path = RegistryHelper.GetRegValue("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "Path");
                if (string.IsNullOrEmpty(path))
                    path = Environment.GetEnvironmentVariable("PATH");

                List<string> folders = path.Split(';').ToList();

                foreach (string folder in folders)
                    results[folder] = String.Join(", ", PermissionsHelper.GetPermissionsFolder(folder, Checks.Checks.CurrentUserSiDs));

            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
