using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.ServiceProcess;
using System.Reflection;
using System.Security.AccessControl;
using System.Runtime.InteropServices;

namespace winPEAS
{
    class ServicesInfo
    {
        ///////////////////////////////////////////////
        //// Non Standard Services (Non Microsoft) ////
        ///////////////////////////////////////////////
        public static List<Dictionary<string, string>> GetNonstandardServices()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

            try
            {
                ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\cimv2", "SELECT * FROM win32_service");
                ManagementObjectCollection data = wmiData.Get();

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
                        catch (Exception ex)
                        {
                            // Not enough privileges
                        }

                        if (String.IsNullOrEmpty(companyName) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                        {
                            Dictionary<string, string> toadd = new Dictionary<string, string>();
                            toadd["Name"] = String.Format("{0}", result["Name"]);
                            toadd["DisplayName"] = String.Format("{0}", result["DisplayName"]);
                            toadd["CompanyName"] = companyName;
                            toadd["State"] = String.Format("{0}", result["State"]);
                            toadd["StartMode"] = String.Format("{0}", result["StartMode"]);
                            toadd["PathName"] = String.Format("{0}", result["PathName"]);
                            toadd["FilteredPath"] = binaryPath;
                            toadd["isDotNet"] = isDotNet;
                            toadd["Description"] = String.Format("{0}", result["Description"]);
                            results.Add(toadd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }

        public static List<Dictionary<string, string>> GetNonstandardServicesFromReg()
        {
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();

            try
            {
                foreach (string key in MyUtils.GetRegSubkeys("HKLM", @"SYSTEM\CurrentControlSet\Services"))
                {
                    Dictionary<string, object> key_values = MyUtils.GetRegValues("HKLM", @"SYSTEM\CurrentControlSet\Services\" + key);

                    if (key_values.ContainsKey("DisplayName") && key_values.ContainsKey("ImagePath"))
                    {
                        string companyName = "";
                        string isDotNet = "";
                        string pathName = Environment.ExpandEnvironmentVariables(String.Format("{0}", key_values["ImagePath"]).Replace("\\SystemRoot\\", "%SystemRoot%\\"));
                        string binaryPath = MyUtils.ReconstructExecPath(pathName);
                        if (binaryPath != "")
                        {
                            try
                            {
                                FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(binaryPath);
                                companyName = myFileVersionInfo.CompanyName;
                                isDotNet = MyUtils.CheckIfDotNet(binaryPath) ? "isDotNet" : "";
                            }
                            catch (Exception ex)
                            {
                                // Not enough privileges
                            }
                        }

                        string displayName = String.Format("{0}", key_values["DisplayName"]);
                        string imagePath = String.Format("{0}", key_values["ImagePath"]);
                        string description = key_values.ContainsKey("Description") ? String.Format("{0}", key_values["Description"]) : "";
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
                        if (String.IsNullOrEmpty(companyName) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                        {
                            Dictionary<string, string> toadd = new Dictionary<string, string>();
                            toadd["Name"] = String.Format("{0}", displayName);
                            toadd["DisplayName"] = String.Format("{0}", displayName);
                            toadd["CompanyName"] = companyName;
                            toadd["State"] = "";
                            toadd["StartMode"] = startMode;
                            toadd["PathName"] = pathName;
                            toadd["FilteredPath"] = binaryPath;
                            toadd["isDotNet"] = isDotNet;
                            toadd["Description"] = description;
                            results.Add(toadd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }


        //////////////////////////////////////////
        ///////  Find Modifiable Services ////////
        //////////////////////////////////////////
        /// Find services that you can modify using PS os sc for example
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool QueryServiceObjectSecurity(
            IntPtr serviceHandle,
            System.Security.AccessControl.SecurityInfos secInfo,
            byte[] lpSecDesrBuf,
            uint bufSize,
            out uint bufSizeNeeded);
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
                    uint bufSizeNeeded;
                    bool ok = QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, psd, 0, out bufSizeNeeded);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 122 || err == 0)
                        { // ERROR_INSUFFICIENT_BUFFER
                          // expected; now we know bufsize
                            psd = new byte[bufSizeNeeded];
                            ok = QueryServiceObjectSecurity(handle, SecurityInfos.DiscretionaryAcl, psd, bufSizeNeeded, out bufSizeNeeded);
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
                            int serviceRights = ace.AccessMask;

                            string current_perm_str = MyUtils.PermInt2Str(serviceRights, true, true);
                            if (!String.IsNullOrEmpty(current_perm_str) && !permissions.Contains(current_perm_str))
                                permissions.Add(current_perm_str);
                        }
                    }

                    if (permissions.Count > 0)
                    {
                        string perms = String.Join(", ", permissions);
                        if (perms.Replace("Start", "").Replace("Stop","").Length > 3) //Check if any other permissions appart from Start and Stop
                            results.Add(sc.ServiceName, perms);
                    }

                }
                catch (Exception ex)
                {
                    //Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
                }
            }
            return results;
        }

        //////////////////////////////////////////
        ///////  Find Write reg. Services ////////
        //////////////////////////////////////////
        /// Find Services which Reg you have write or equivalent access
        public static List<Dictionary<string, string>> GetWriteServiceRegs(Dictionary<string,string> NtAccountNames)
        {
            List<Dictionary<string,string>> results = new List<Dictionary<string, string>>();
            try
            {
                RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"system\currentcontrolset\services");
                foreach (string serviceRegName in regKey.GetSubKeyNames())
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"system\currentcontrolset\services\" + serviceRegName);
                    List<string> perms = MyUtils.GetMyPermissionsR(key, NtAccountNames);
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
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
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
                string path = MyUtils.GetRegValue("HKLM", "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "Path");
                if (String.IsNullOrEmpty(path))
                    path = Environment.GetEnvironmentVariable("PATH");

                List<string> folders = path.Split(';').ToList();

                foreach (string folder in folders)
                    results[folder] = String.Join(", ", MyUtils.GetPermissionsFolder(folder, Program.currentUserSIDs));
                
            }
            catch (Exception ex)
            {
                Beaprint.GrayPrint(String.Format("  [X] Exception: {0}", ex.Message));
            }
            return results;
        }
    }
}
