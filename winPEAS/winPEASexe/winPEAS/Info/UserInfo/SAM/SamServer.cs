using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using winPEAS.Native;
using winPEAS.Native.Classes;

namespace winPEAS.Info.UserInfo.SAM
{
    public sealed class SamServer : IDisposable
    {
        private IntPtr _handle;

        public SamServer(string name, SERVER_ACCESS_MASK access)
        {
            Name = name;
            Check(Samlib.SamConnect(new UNICODE_STRING(name), out _handle, access, IntPtr.Zero));
        }

        public string Name { get; }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                Samlib.SamCloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public void SetDomainPasswordInformation(SecurityIdentifier domainSid, DOMAIN_PASSWORD_INFORMATION passwordInformation)
        {
            if (domainSid == null)
                throw new ArgumentNullException(nameof(domainSid));

            var sid = new byte[domainSid.BinaryLength];
            domainSid.GetBinaryForm(sid, 0);

            Check(Samlib.SamOpenDomain(_handle, DOMAIN_ACCESS_MASK.DOMAIN_WRITE_PASSWORD_PARAMS, sid, out IntPtr domain));
            IntPtr info = Marshal.AllocHGlobal(Marshal.SizeOf(passwordInformation));
            Marshal.StructureToPtr(passwordInformation, info, false);
            try
            {
                Check(Samlib.SamSetInformationDomain(domain, DOMAIN_INFORMATION_CLASS.DomainPasswordInformation, info));
            }
            finally
            {
                Marshal.FreeHGlobal(info);
                Samlib.SamCloseHandle(domain);
            }
        }

        public DOMAIN_PASSWORD_INFORMATION GetDomainPasswordInformation(SecurityIdentifier domainSid)
        {
            if (domainSid == null)
                throw new ArgumentNullException(nameof(domainSid));

            var sid = new byte[domainSid.BinaryLength];
            domainSid.GetBinaryForm(sid, 0);

            Check(Samlib.SamOpenDomain(_handle, DOMAIN_ACCESS_MASK.DOMAIN_READ_PASSWORD_PARAMETERS, sid, out IntPtr domain));
            var info = IntPtr.Zero;
            try
            {
                Check(Samlib.SamQueryInformationDomain(domain, DOMAIN_INFORMATION_CLASS.DomainPasswordInformation, out info));
                return (DOMAIN_PASSWORD_INFORMATION)Marshal.PtrToStructure(info, typeof(DOMAIN_PASSWORD_INFORMATION));
            }
            finally
            {
                Samlib.SamFreeMemory(info);
                Samlib.SamCloseHandle(domain);
            }
        }

        public SecurityIdentifier GetDomainSid(string domain)
        {
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));

            Check(Samlib.SamLookupDomainInSamServer(_handle, new UNICODE_STRING(domain), out IntPtr sid));
            return new SecurityIdentifier(sid);
        }

        public IEnumerable<string> EnumerateDomains()
        {
            int cookie = 0;
            while (true)
            {
                var status = Samlib.SamEnumerateDomainsInSamServer(_handle, ref cookie, out IntPtr info, 1, out int count);
                if (status != NTSTATUS.STATUS_SUCCESS && status != NTSTATUS.STATUS_MORE_ENTRIES)
                    Check(status);

                if (count == 0)
                    break;

                var us = (UNICODE_STRING)Marshal.PtrToStructure(info + IntPtr.Size, typeof(UNICODE_STRING));
                Samlib.SamFreeMemory(info);
                yield return us.ToString();
                us.Buffer = IntPtr.Zero; // we don't own this one
            }
        }



        private static void Check(NTSTATUS err)
        {
            if (err == NTSTATUS.STATUS_SUCCESS)
                return;

            //throw new System.ComponentModel.Win32Exception("Error " + err + " (0x" + ((int)err).ToString("X8") + ")");
        }
    }
}
