using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text.RegularExpressions;
using winPEAS.Helpers;
using winPEAS.Native;

namespace winPEAS.Info.ProcessInfo
{
    internal class ProcessesInfo
    {
        private static string GetProcU(Process p)
        {
            IntPtr pHandle = IntPtr.Zero;
            try
            {
                Advapi32.OpenProcessToken(p.Handle, 8, out pHandle);
                WindowsIdentity WI = new WindowsIdentity(pHandle);
                String uSEr = WI.Name;
                return uSEr.Contains(@"\") ? uSEr.Substring(uSEr.IndexOf(@"\") + 1) : uSEr;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (pHandle != IntPtr.Zero)
                {
                    Kernel32.CloseHandle(pHandle);
                }
            }
        }

        // TODO: check out https://github.com/harleyQu1nn/AggressorScripts/blob/master/ProcessColor.cna#L10
        public static List<Dictionary<string, string>> GetProcInfo()
        {
            List<Dictionary<string, string>> f_results = new List<Dictionary<string, string>>();
            try
            {
                var wmiQueRyStr = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
                using (var srcher = new ManagementObjectSearcher(wmiQueRyStr))
                using (var reslts = srcher.Get())
                {
                    var queRy = from p in Process.GetProcesses()
                                join mo in reslts.Cast<ManagementObject>()
                                on p.Id equals (int)(uint)mo["ProcessId"]
                                select new
                                {
                                    Proc = p,
                                    Pth = (string)mo["ExecutablePath"],
                                    CommLine = (string)mo["CommandLine"],
                                    Owner = GetProcU(p), //Needed inside the next foreach
                                };

                    foreach (var itm in queRy)
                    {
                        if (itm.Pth != null)
                        {
                            string companyName = "";
                            string isDotNet = "";
                            try
                            {
                                FileVersionInfo myFileVerInfo = FileVersionInfo.GetVersionInfo(itm.Pth);
                                //compName = myFileVerInfo.CompanyName;
                                isDotNet = MyUtils.CheckIfDotNet(itm.Pth) ? "isDotNet" : "";
                            }
                            catch
                            {
                                // Not enough privileges
                            }
                            if ((string.IsNullOrEmpty(companyName)) || (!Regex.IsMatch(companyName, @"^Microsoft.*", RegexOptions.IgnoreCase)))
                            {
                                Dictionary<string, string> to_add = new Dictionary<string, string>();
                                to_add["Name"] = itm.Proc.ProcessName;
                                to_add["ProcessID"] = itm.Proc.Id.ToString();
                                to_add["ExecutablePath"] = itm.Pth;
                                to_add["Product"] = companyName;
                                to_add["Owner"] = itm.Owner == null ? "" : itm.Owner;
                                to_add["isDotNet"] = isDotNet;
                                to_add["CommandLine"] = itm.CommLine;
                                f_results.Add(to_add);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return f_results;
        }
    }
}
