using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace winPEAS.Info.SystemInfo.Printers
{
    internal class Printers
    {
        [DllImport("advapi32.dll", EntryPoint = "GetNamedSecurityInfoW", CharSet = CharSet.Unicode)]
        public static extern int GetNamedSecurityInfo(
            string objectName,
            SE_OBJECT_TYPE objectType,
            SecurityInfos securityInfo,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(
           IntPtr SecurityDescriptor,
           uint StringSDRevision,
           SecurityInfos SecurityInformation,
           out IntPtr StringSecurityDescriptor,
           out int StringSecurityDescriptorSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_INFOS
        {
            public string Owner;
            public RawSecurityDescriptor SecurityDescriptor;
            public string SDDL;
        }

        public enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE = 0,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
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
                        string printerSDDL = null;
                        var printerName = $"{printer.GetPropertyValue("Name")}";
                        var status = $"{printer.GetPropertyValue("Status")}";

                        try
                        {
                            var info = GetSecurityInfos(printerName, SE_OBJECT_TYPE.SE_PRINTER);
                            printerSDDL = info.SDDL;
                        }
                        catch { }

                       result.Add(new PrinterInfo(
                            printerName,
                            status,
                            printerSDDL,
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
            var errorReturn = GetNamedSecurityInfo(ObjectName, ObjectType, info, out pSidOwner, out pSidGroup, out pDacl, out pSacl, out pSecurityDescriptor);
            if (errorReturn != 0)
            {
                return infos;
            }

            if (ConvertSecurityDescriptorToStringSecurityDescriptor(pSecurityDescriptor, 1, SecurityInfos.DiscretionaryAcl | SecurityInfos.Owner, out var pSddlString, out _))
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
