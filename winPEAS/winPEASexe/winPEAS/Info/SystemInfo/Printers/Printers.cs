using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using winPEAS.Native;
using winPEAS.Native.Enums;

namespace winPEAS.Info.SystemInfo.Printers
{
    internal class Printers
    {
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
