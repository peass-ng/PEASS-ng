using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using winPEAS.Helpers;

namespace winPEAS.Info.ProcessInfo
{
    internal class ProcessesInfo
    {
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
                                    Owner = HandlesHelper.GetProcU(p)["name"], //Needed inside the next foreach
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
                                Dictionary<string, string> to_add = new Dictionary<string, string>
                                {
                                    ["Name"] = itm.Proc.ProcessName,
                                    ["ProcessID"] = itm.Proc.Id.ToString(),
                                    ["ExecutablePath"] = itm.Pth,
                                    ["Product"] = companyName,
                                    ["Owner"] = itm.Owner == null ? "" : itm.Owner,
                                    ["isDotNet"] = isDotNet,
                                    ["CommandLine"] = itm.CommLine
                                };
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

        public static List<Dictionary<string, string>> GetVulnHandlers(ProgressBar progress)
        {
            List<Dictionary<string, string>> vulnHandlers = new List<Dictionary<string, string>>();
            List<HandlesHelper.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> handlers = HandlesHelper.GetAllHandlers();
            List<string> interestingHandlerTypes = new List<string>() { "file", "key", "process", "thread" }; //section

            int processedHandlersCount = 0;
            int UPDATE_PROGRESSBAR_COUNT = 500;
            double pb = 0;
            int totalCount = handlers.Count;

            foreach (HandlesHelper.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX h in handlers)
            {
                processedHandlersCount++;

                if (processedHandlersCount % UPDATE_PROGRESSBAR_COUNT == 0)
                {
                    pb = (double)processedHandlersCount / totalCount;
                    progress.Report(pb); //Value must be in [0..1] range
                }

                // skip some objects to avoid getting stuck
                // see: https://github.com/adamdriscoll/PoshInternals/issues/7
                if (h.GrantedAccess == 0x0012019f
                    || h.GrantedAccess == 0x00120189
                    || h.GrantedAccess == 0x120089
                    || h.GrantedAccess == 0x1A019F)
                    continue;

                IntPtr dupHandle;
                IntPtr _processHandle = Native.Kernel32.OpenProcess(HandlesHelper.ProcessAccessFlags.DupHandle | HandlesHelper.ProcessAccessFlags.QueryInformation, false, h.UniqueProcessId);

                if (_processHandle == (IntPtr)0)
                    continue;

                uint status = (uint)Native.Ntdll.NtDuplicateObject(
                    _processHandle,
                    h.HandleValue,
                    Native.Kernel32.GetCurrentProcess(),
                    out dupHandle,
                    0,
                    false,
                    HandlesHelper.DUPLICATE_SAME_ACCESS);

                Native.Kernel32.CloseHandle(_processHandle);

                if (status != 0)
                    continue;

                string typeName = HandlesHelper.GetObjectType(dupHandle).ToLower();
                if (interestingHandlerTypes.Contains(typeName))
                {
                    HandlesHelper.VULNERABLE_HANDLER_INFO handlerExp = HandlesHelper.checkExploitaible(h, typeName);
                    if (handlerExp.isVuln == true)
                    {
                        HandlesHelper.PT_RELEVANT_INFO origProcInfo = HandlesHelper.getProcInfoById((int)h.UniqueProcessId);
                        if (!Checks.Checks.CurrentUserSiDs.ContainsKey(origProcInfo.userSid))
                            continue;

                        string hName = HandlesHelper.GetObjectName(dupHandle);

                        Dictionary<string, string> to_add = new Dictionary<string, string>
                        {
                            ["Handle Name"] = hName,
                            ["Handle"] = h.HandleValue.ToString() + "(" + typeName + ")",
                            ["Handle Owner"] = "Pid is " + h.UniqueProcessId.ToString() + "(" + origProcInfo.name + ") with owner: " + origProcInfo.userName,
                            ["Reason"] = handlerExp.reason
                        };

                        if (typeName == "process" || typeName == "thread")
                        {
                            HandlesHelper.PT_RELEVANT_INFO hInfo;
                            if (typeName == "process")
                            {
                                hInfo = HandlesHelper.getProcessHandlerInfo(dupHandle);
                            }

                            else //Thread
                            {
                                hInfo = HandlesHelper.getThreadHandlerInfo(dupHandle);
                            }

                            // If the privileged access is from a proc to itself, or to a process of the same user, not a privesc
                            if (hInfo.pid == 0 ||
                                (int)h.UniqueProcessId == hInfo.pid ||
                                origProcInfo.userSid == hInfo.userSid)
                                continue;

                            to_add["Handle PID"] = hInfo.pid.ToString() + "(" + hInfo.userName + ")";
                        }

                        else if (typeName == "file")
                        {
                            //StringBuilder filePath = new StringBuilder(2000);
                            //HandlersHelper.GetFinalPathNameByHandle(dupHandle, filePath, 2000, 0);

                            HandlesHelper.FILE_NAME_INFO fni = new HandlesHelper.FILE_NAME_INFO();

                            // Sometimes both GetFileInformationByHandle and GetFileInformationByHandleEx hangs
                            // So a timeput of 1s is put to the function to prevent that
                            var task = Task.Run(() =>
                            {
                                // FILE_NAME_INFO (2)
                                return Native.Kernel32.GetFileInformationByHandleEx(dupHandle, 2, out fni, (uint)Marshal.SizeOf(fni));
                            });

                            bool isCompletedSuccessfully = task.Wait(TimeSpan.FromMilliseconds(1000));

                            if (!isCompletedSuccessfully)
                            {
                                //throw new TimeoutException("The function has taken longer than the maximum time allowed.");
                                continue;
                            }

                            string sFilePath = fni.FileName;
                            if (sFilePath.Length == 0)
                                continue;

                            List<string> permsFile = PermissionsHelper.GetPermissionsFile(sFilePath, Checks.Checks.CurrentUserSiDs, PermissionType.WRITEABLE_OR_EQUIVALENT);
                            try
                            {
                                System.Security.AccessControl.FileSecurity fs = System.IO.File.GetAccessControl(sFilePath);
                                IdentityReference sid = fs.GetOwner(typeof(SecurityIdentifier));
                                string ownerName = sid.Translate(typeof(NTAccount)).ToString();

                                // If current user already have permissions over that file or the proc belongs to the owner of the file,
                                // handler not interesting to elevate privs
                                if (permsFile.Count > 0 || origProcInfo.userSid == sid.Value)
                                    continue;

                                to_add["File Path"] = sFilePath;
                                to_add["File Owner"] = ownerName;
                            }
                            catch (System.IO.FileNotFoundException)
                            {
                                // File wasn't found
                                continue;
                            }
                            catch (System.InvalidOperationException)
                            {
                                continue;
                            }

                        }

                        else if (typeName == "key")
                        {
                            HandlesHelper.KEY_RELEVANT_INFO kri = HandlesHelper.getKeyHandlerInfo(dupHandle);
                            if (kri.path.Length == 0 && kri.hive != null && kri.hive.Length > 0)
                                continue;

                            RegistryKey regKey = Helpers.Registry.RegistryHelper.GetReg(kri.hive, kri.path);
                            if (regKey == null)
                                continue;

                            List<string> permsReg = PermissionsHelper.GetMyPermissionsR(regKey, Checks.Checks.CurrentUserSiDs);

                            // If current user already have permissions over that reg, handle not interesting to elevate privs
                            if (permsReg.Count > 0)
                                continue;

                            to_add["Registry"] = kri.hive + "\\" + kri.path;
                        }


                        vulnHandlers.Add(to_add);
                    }
                }
            }

            return vulnHandlers;
        }
    }
}
