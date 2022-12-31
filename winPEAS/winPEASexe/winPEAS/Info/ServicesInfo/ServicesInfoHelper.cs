using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Helpers.Registry;
using winPEAS.Native;

namespace winPEAS.Info.ServicesInfo
{
    class ServicesInfoHelper
    {
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
