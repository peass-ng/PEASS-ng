using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace winPEAS.Helpers
{
    internal class HandlesHelper
    {
        private const int CNST_SYSTEM_EXTENDED_HANDLE_INFORMATION = 64;
        public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        public const int DUPLICATE_SAME_ACCESS = 0x2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FILE_NAME_INFO
        {
            public int FileNameLength;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1000)]
            public string FileName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct THREAD_BASIC_INFORMATION
        {
            public uint ExitStatus;
            public IntPtr TebBaseAdress;
            public CLIENT_ID ClientId;
            public uint AffinityMask;
            public uint Priority;
            public uint BasePriority;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CLIENT_ID
        {
            public int UniqueProcess;
            public int UniqueThread;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            public int ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public int BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
        {
            public IntPtr Object;
            public UIntPtr UniqueProcessId;
            public IntPtr HandleValue;
            public uint GrantedAccess;
            public ushort CreatorBackTraceIndex;
            public ushort ObjectTypeIndex;
            public uint HandleAttributes;
            public uint Reserved;
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x1000,
            Synchronize = 0x00100000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_BASIC_INFORMATION
        { // Information Class 0
            public int Attributes;
            public int GrantedAccess;
            public int HandleCount;
            public int PointerCount;
            public int PagedPoolUsage;
            public int NonPagedPoolUsage;
            public int Reserved1;
            public int Reserved2;
            public int Reserved3;
            public int NameInformationLength;
            public int TypeInformationLength;
            public int SecurityDescriptorLength;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreateTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_NAME_INFORMATION
        { // Information Class 1
            public UNICODE_STRING Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_TYPE_INFORMATION
        { // Information Class 1
            public UNICODE_STRING Name;
            public ulong TotalNumberOfObjects;
            public ulong TotalNumberOfHandles;
        }

        public enum ObjectInformationClass : int
        {
            ObjectBasicInformation = 0,
            ObjectNameInformation = 1,
            ObjectTypeInformation = 2,
            ObjectAllTypesInformation = 3,
            ObjectHandleInformation = 4
        }

        public struct VULNERABLE_HANDLER_INFO
        {
            public string handlerType;
            public bool isVuln;
            public string reason;
        }

        public struct PT_RELEVANT_INFO
        {
            public int pid;
            public string name;
            public string imagePath;
            public string userName;
            public string userSid;
        }

        public struct KEY_RELEVANT_INFO
        {
            public string hive;
            public string path;
        }










        // Check if the given handler is exploitable
        public static VULNERABLE_HANDLER_INFO checkExploitaible(SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX h, string typeName)
        {
            VULNERABLE_HANDLER_INFO vulnHandler = new VULNERABLE_HANDLER_INFO();
            vulnHandler.handlerType = typeName;

            if (typeName == "process")
            {
                // Hex perms from https://docs.microsoft.com/en-us/windows/win32/procthread/process-security-and-access-rights and https://github.com/buffer/maltracer/blob/master/defines.py

                //PROCESS_ALL_ACCESS
                if ((h.GrantedAccess & 0x001F0FFF) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "PROCESS_ALL_ACCESS";
                }

                //PROCESS_CREATE_PROCESS
                else if ((h.GrantedAccess & 0x0080) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "PROCESS_CREATE_PROCESS";
                }

                //PROCESS_CREATE_THREAD
                else if ((h.GrantedAccess & 0x0002) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "PROCESS_CREATE_THREAD";
                }

                //PROCESS_DUP_HANDLE
                else if ((h.GrantedAccess & 0x0040) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "PROCESS_DUP_HANDLE";
                }

                //PROCESS_VM_WRITE
                else if ((h.GrantedAccess & 0x0020) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "PROCESS_VM_WRITE";

                    if ((h.GrantedAccess & 0x0010) == h.GrantedAccess)
                        vulnHandler.reason += "& PROCESS_VM_READ";

                    if ((h.GrantedAccess & 0x0008) == h.GrantedAccess)
                        vulnHandler.reason += "& PROCESS_VM_OPERATION";
                }
            }

            else if (typeName == "thread")
            {
                // Codes from https://docs.microsoft.com/en-us/windows/win32/procthread/thread-security-and-access-rights and https://github.com/x0r19x91/code-injection/blob/master/inject.asm

                //THREAD_ALL_ACCESS
                if ((h.GrantedAccess & 0x1f03ff) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "THREAD_ALL_ACCESS";
                }

                //THREAD_DIRECT_IMPERSONATION
                else if ((h.GrantedAccess & 0x0200) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "THREAD_DIRECT_IMPERSONATION";
                }

                //THREAD_GET_CONTEXT & THREAD_SET_CONTEXT 
                else if (((h.GrantedAccess & 0x0008) == h.GrantedAccess) && ((h.GrantedAccess & 0x0010) == h.GrantedAccess))
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "THREAD_GET_CONTEXT & THREAD_SET_CONTEXT";
                }
            }

            else if (typeName == "file")
            {

                string perm = PermissionsHelper.PermInt2Str((int)h.GrantedAccess, PermissionType.WRITEABLE_OR_EQUIVALENT);
                if (perm != null && perm.Length > 0)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = perm;
                }
            }

            else if (typeName == "key")
            {
                string perm = PermissionsHelper.PermInt2Str((int)h.GrantedAccess, PermissionType.WRITEABLE_OR_EQUIVALENT_REG);
                if (perm != null && perm.Length > 0)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = perm;
                }
            }

            else if (typeName == "section")
            {
                // Perms from 
                // https://docs.microsoft.com/en-us/windows/win32/secauthz/standard-access-rights
                // https://docs.microsoft.com/en-us/windows/win32/secauthz/access-mask-format
                // https://github.com/lab52io/LeakedHandlesFinder/blob/master/LeakedHandlesFinder/LeakedHandlesFinder.cpp


                //MAP_WRITE
                if ((h.GrantedAccess & 0x2) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "MAP_WRITE (Research Needed)";
                }
                //DELETE, READ_CONTROL, WRITE_DAC, and WRITE_OWNER = STANDARD_RIGHTS_ALL
                else if ((h.GrantedAccess & 0xf0000) == h.GrantedAccess)
                {
                    vulnHandler.isVuln = true;
                    vulnHandler.reason = "STANDARD_RIGHTS_ALL (Research Needed)";
                }
            }

            return vulnHandler;
        }

        // Given a found handler get what type is it.
        public static string GetObjectType(IntPtr handle)
        {
            OBJECT_TYPE_INFORMATION basicType = new OBJECT_TYPE_INFORMATION();

            try
            {
                IntPtr _basic = IntPtr.Zero;
                string name;
                int nameLength = 0;

                try
                {
                    _basic = Marshal.AllocHGlobal(0x1000);

                    Native.Ntdll.NtQueryObject(handle, (int)ObjectInformationClass.ObjectTypeInformation, _basic, 0x1000, ref nameLength);
                    basicType = (OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(_basic, basicType.GetType());
                    name = Marshal.PtrToStringUni(basicType.Name.Buffer, basicType.Name.Length >> 1);
                    return name;
                }
                finally
                {
                    if (_basic != IntPtr.Zero)
                        Marshal.FreeHGlobal(_basic);
                }
            }
            catch { }

            return null;
        }

        // Get the name of the handler (if any)
        public static string GetObjectName(IntPtr handle)
        {
            OBJECT_BASIC_INFORMATION basicInfo = new OBJECT_BASIC_INFORMATION();
            try
            {

                IntPtr _basic = IntPtr.Zero;
                int nameLength = 0;

                try
                {
                    _basic = Marshal.AllocHGlobal(Marshal.SizeOf(basicInfo));

                    Native.Ntdll.NtQueryObject(handle, (int)ObjectInformationClass.ObjectBasicInformation, _basic, Marshal.SizeOf(basicInfo), ref nameLength);
                    basicInfo = (OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(_basic, basicInfo.GetType());
                    nameLength = basicInfo.NameInformationLength;
                }
                finally
                {
                    if (_basic != IntPtr.Zero)
                        Marshal.FreeHGlobal(_basic);
                }

                if (nameLength == 0)
                {
                    return null;
                }

                OBJECT_NAME_INFORMATION nameInfo = new OBJECT_NAME_INFORMATION();
                IntPtr _objectName = Marshal.AllocHGlobal(nameLength);

                try
                {
                    while ((uint)(Native.Ntdll.NtQueryObject(handle, (int)ObjectInformationClass.ObjectNameInformation, _objectName, nameLength, ref nameLength)) == STATUS_INFO_LENGTH_MISMATCH)
                    {
                        Marshal.FreeHGlobal(_objectName);
                        _objectName = Marshal.AllocHGlobal(nameLength);
                    }
                    nameInfo = (OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(_objectName, nameInfo.GetType());
                }
                finally
                {
                    Marshal.FreeHGlobal(_objectName);
                }

                try
                {
                    if (nameInfo.Name.Length > 0)
                        return Marshal.PtrToStringUni(nameInfo.Name.Buffer, nameInfo.Name.Length >> 1);
                }
                catch
                {

                }

                return null;
            }
            catch { return null; }
        }

        // Get all handlers inside the system
        public static List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> GetAllHandlers()
        {
            bool is_64 = Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
            int infoLength = 0x10000;
            int length = 0;
            IntPtr _info = Marshal.AllocHGlobal(infoLength);
            IntPtr _handle = IntPtr.Zero;
            long handleCount = 0;


            // Try to find the size
            while ((Native.Ntdll.NtQuerySystemInformation(CNST_SYSTEM_EXTENDED_HANDLE_INFORMATION, _info, infoLength, ref length)) == STATUS_INFO_LENGTH_MISMATCH)
            {
                infoLength = length;
                Marshal.FreeHGlobal(_info);
                _info = Marshal.AllocHGlobal(infoLength);
            }


            if (is_64)
            {
                handleCount = Marshal.ReadInt64(_info);
                _handle = new IntPtr(_info.ToInt64() + 16);
            }
            else
            {
                handleCount = Marshal.ReadInt32(_info);
                _handle = new IntPtr(_info.ToInt32() + 8);
            }

            SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX handleInfo = new SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX();
            List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX> handles = new List<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();

            int infoSize = Marshal.SizeOf(handleInfo);
            Type infoType = handleInfo.GetType();


            for (long i = 0; i < handleCount; i++)
            {
                if (is_64)
                {
                    handleInfo = (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)Marshal.PtrToStructure(_handle, infoType);
                    _handle = new IntPtr(_handle.ToInt64() + infoSize);
                }
                else
                {
                    handleInfo = (SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX)Marshal.PtrToStructure(_handle, infoType);
                    _handle = new IntPtr(_handle.ToInt32() + infoSize);
                }

                handles.Add(handleInfo);
            }

            return handles;
        }

        // Get the owner of a process given the PID
        public static Dictionary<string, string> GetProcU(Process p)
        {
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                ["name"] = "",
                ["sid"] = ""
            };
            IntPtr pHandle = IntPtr.Zero;
            try
            {
                Native.Advapi32.OpenProcessToken(p.Handle, 8, out pHandle);
                WindowsIdentity WI = new WindowsIdentity(pHandle);
                string uSEr = WI.Name;
                string sid = WI.User.Value;
                data["name"] = uSEr.Contains(@"\") ? uSEr.Substring(uSEr.IndexOf(@"\") + 1) : uSEr;
                data["sid"] = sid;
                return data;
            }
            catch
            {
                return data;
            }
            finally
            {
                if (pHandle != IntPtr.Zero)
                {
                    Native.Kernel32.CloseHandle(pHandle);
                }
            }
        }

        // Get info of the process given the PID
        public static PT_RELEVANT_INFO getProcInfoById(int pid)
        {
            PT_RELEVANT_INFO pri = new PT_RELEVANT_INFO();

            Process proc = Process.GetProcessById(pid);
            Dictionary<string, string> user = GetProcU(proc);

            StringBuilder fileName = new StringBuilder(2000);
            Native.Psapi.GetProcessImageFileName(proc.Handle, fileName, 2000);

            pri.pid = pid;
            pri.name = proc.ProcessName;
            pri.userName = user["name"];
            pri.userSid = user["sid"];
            pri.imagePath = fileName.ToString();

            return pri;
        }

        // Get information of a handler of type process
        public static PT_RELEVANT_INFO getProcessHandlerInfo(IntPtr handle)
        {
            PT_RELEVANT_INFO pri = new PT_RELEVANT_INFO();
            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
            IntPtr[] pbi_arr = new IntPtr[6];
            int pid;


            int retLength = 0;

            // Try to find the size
            uint status = (uint)Native.Ntdll.NtQueryInformationProcess(handle, 0, pbi_arr, 48, ref retLength);
            if (status == 0)
            {

                //pbi.ExitStatus = (int)pbi_arr[0];
                //pbi.PebBaseAddress = pbi_arr[1];
                //pbi.AffinityMask = pbi_arr[2];
                //pbi.BasePriority = (int)pbi_arr[3];
                pbi.UniqueProcessId = pbi_arr[4];
                //pbi.InheritedFromUniqueProcessId = pbi_arr[5];
                pid = (int)pbi.UniqueProcessId;
            }
            else
            {
                pid = (int)Native.Kernel32.GetProcessId(handle);
            }

            if (pid == 0)
                return pri;

            return getProcInfoById(pid);
        }

        // Get information of a handler of type thread
        public static PT_RELEVANT_INFO getThreadHandlerInfo(IntPtr handle)
        {
            PT_RELEVANT_INFO pri = new PT_RELEVANT_INFO();
            THREAD_BASIC_INFORMATION tbi = new THREAD_BASIC_INFORMATION();
            IntPtr[] tbi_arr = new IntPtr[6];
            int pid;


            /* You could also get the PID using this method
            int retLength = 0;
            uint status = (uint)NtQueryInformationThread(handle, 0, tbi_arr, 48, ref retLength);
            if (status != 0)
            {
                return pri;
            }

            pid = (int)GetProcessIdOfThread(handle);

            CLIENT_ID ci = new CLIENT_ID();

            tbi.ExitStatus = (uint)tbi_arr[0];
            tbi.TebBaseAdress = tbi_arr[1];
            tbi.ClientId = tbi_arr[2];
            tbi.AffinityMask = (uint)tbi_arr[3];
            tbi.Priority = (uint)tbi_arr[4];
            tbi.BasePriority = (uint)tbi_arr[5];*/

            pid = (int)Native.Kernel32.GetProcessIdOfThread(handle);
            if (pid == 0)
                return pri;

            return getProcInfoById(pid);
        }

        // Get information of a handler of type key
        public static KEY_RELEVANT_INFO getKeyHandlerInfo(IntPtr handle)
        {
            KEY_RELEVANT_INFO kri = new KEY_RELEVANT_INFO();
            int retLength = 0;

            // Get KeyNameInformation (3)
            uint status = (uint)Native.Ntdll.NtQueryKey(handle, 3, null, 0, ref retLength);
            var keyInformation = new byte[retLength];
            status = (uint)Native.Ntdll.NtQueryKey(handle, 3, keyInformation, retLength, ref retLength);

            string path = Encoding.Unicode.GetString(keyInformation, 4, keyInformation.Length - 4).ToLower();
            string hive = "";

            // https://groups.google.com/g/comp.os.ms-windows.programmer.win32/c/nCs-9zFRm6I
            if (path.StartsWith(@"\registry\machine"))
            {
                path = path.Replace(@"\registry\machine", "");
                hive = "HKLM";
            }

            else if (path.StartsWith(@"\registry\user"))
            {
                path = path.Replace(@"\registry\user", "");
                hive = "HKU";
            }

            else
            { // This shouldn't be needed
                if (path.StartsWith("\\"))
                    path = path.Substring(1);
                hive = Registry.RegistryHelper.CheckIfExists(path);
            }

            if (path.StartsWith("\\"))
                path = path.Substring(1);

            kri.hive = hive;
            kri.path = path;

            return kri;
        }
    }
}
