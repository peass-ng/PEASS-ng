using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using winPEAS.Helpers;

namespace winPEAS.Info.UserInfo.SAM
{
    public sealed class SamServer : IDisposable
    {
        private IntPtr _handle;

        public SamServer(string name, SERVER_ACCESS_MASK access)
        {
            Name = name;
            Check(SamConnect(new UNICODE_STRING(name), out _handle, access, IntPtr.Zero));
        }

        public string Name { get; }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                SamCloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }

        public void SetDomainPasswordInformation(SecurityIdentifier domainSid, DOMAIN_PASSWORD_INFORMATION passwordInformation)
        {
            if (domainSid == null)
                throw new ArgumentNullException(nameof(domainSid));

            var sid = new byte[domainSid.BinaryLength];
            domainSid.GetBinaryForm(sid, 0);

            Check(SamOpenDomain(_handle, DOMAIN_ACCESS_MASK.DOMAIN_WRITE_PASSWORD_PARAMS, sid, out IntPtr domain));
            IntPtr info = Marshal.AllocHGlobal(Marshal.SizeOf(passwordInformation));
            Marshal.StructureToPtr(passwordInformation, info, false);
            try
            {
                Check(SamSetInformationDomain(domain, DOMAIN_INFORMATION_CLASS.DomainPasswordInformation, info));
            }
            finally
            {
                Marshal.FreeHGlobal(info);
                SamCloseHandle(domain);
            }
        }

        public DOMAIN_PASSWORD_INFORMATION GetDomainPasswordInformation(SecurityIdentifier domainSid)
        {
            if (domainSid == null)
                throw new ArgumentNullException(nameof(domainSid));

            var sid = new byte[domainSid.BinaryLength];
            domainSid.GetBinaryForm(sid, 0);

            Check(SamOpenDomain(_handle, DOMAIN_ACCESS_MASK.DOMAIN_READ_PASSWORD_PARAMETERS, sid, out IntPtr domain));
            var info = IntPtr.Zero;
            try
            {
                Check(SamQueryInformationDomain(domain, DOMAIN_INFORMATION_CLASS.DomainPasswordInformation, out info));
                return (DOMAIN_PASSWORD_INFORMATION)Marshal.PtrToStructure(info, typeof(DOMAIN_PASSWORD_INFORMATION));
            }
            finally
            {
                SamFreeMemory(info);
                SamCloseHandle(domain);
            }
        }

        public SecurityIdentifier GetDomainSid(string domain)
        {
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));

            Check(SamLookupDomainInSamServer(_handle, new UNICODE_STRING(domain), out IntPtr sid));
            return new SecurityIdentifier(sid);
        }

        public IEnumerable<string> EnumerateDomains()
        {
            int cookie = 0;
            while (true)
            {
                var status = SamEnumerateDomainsInSamServer(_handle, ref cookie, out IntPtr info, 1, out int count);
                if (status != NTSTATUS.STATUS_SUCCESS && status != NTSTATUS.STATUS_MORE_ENTRIES)
                    Check(status);

                if (count == 0)
                    break;

                var us = (UNICODE_STRING)Marshal.PtrToStructure(info + IntPtr.Size, typeof(UNICODE_STRING));
                SamFreeMemory(info);
                yield return us.ToString();
                us.Buffer = IntPtr.Zero; // we don't own this one
            }
        }               

        [StructLayout(LayoutKind.Sequential)]
        private class UNICODE_STRING : IDisposable
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;

            public UNICODE_STRING()
                : this(null)
            {
            }

            public UNICODE_STRING(string s)
            {
                if (s != null)
                {
                    Length = (ushort)(s.Length * 2);
                    MaximumLength = (ushort)(Length + 2);
                    Buffer = Marshal.StringToHGlobalUni(s);
                }
            }

            public override string ToString() => Buffer != IntPtr.Zero ? Marshal.PtrToStringUni(Buffer) : null;

            protected virtual void Dispose(bool disposing)
            {
                if (Buffer != IntPtr.Zero)
                {
                    try
                    {
                        Marshal.FreeHGlobal(Buffer);
                    }
                    catch (Exception ex)
                    {
                        Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
                    }
                    Buffer = IntPtr.Zero;
                }
            }

            ~UNICODE_STRING() => Dispose(false);

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private static void Check(NTSTATUS err)
        {
            if (err == NTSTATUS.STATUS_SUCCESS)
                return;

            //throw new System.ComponentModel.Win32Exception("Error " + err + " (0x" + ((int)err).ToString("X8") + ")");
        }

        

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamConnect(UNICODE_STRING ServerName, out IntPtr ServerHandle, SERVER_ACCESS_MASK DesiredAccess, IntPtr ObjectAttributes);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamCloseHandle(IntPtr ServerHandle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamFreeMemory(IntPtr Handle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamOpenDomain(IntPtr ServerHandle, DOMAIN_ACCESS_MASK DesiredAccess, byte[] DomainId, out IntPtr DomainHandle);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamLookupDomainInSamServer(IntPtr ServerHandle, UNICODE_STRING name, out IntPtr DomainId);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamQueryInformationDomain(IntPtr DomainHandle, DOMAIN_INFORMATION_CLASS DomainInformationClass, out IntPtr Buffer);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamSetInformationDomain(IntPtr DomainHandle, DOMAIN_INFORMATION_CLASS DomainInformationClass, IntPtr Buffer);

        [DllImport("samlib.dll", CharSet = CharSet.Unicode)]
        private static extern NTSTATUS SamEnumerateDomainsInSamServer(IntPtr ServerHandle, ref int EnumerationContext, out IntPtr EnumerationBuffer, int PreferedMaximumLength, out int CountReturned);
    }

}
