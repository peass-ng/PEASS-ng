using System;
using System.Runtime.InteropServices;
using winPEAS.Native.Enums;

namespace winPEAS.Native.Classes
{
    public partial class SafeTokenHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        private const Int32 ERROR_NO_TOKEN = 0x000003F0;
        private const Int32 ERROR_INSUFFICIENT_BUFFER = 122;
        private static SafeTokenHandle currentProcessToken = null;

        private SafeTokenHandle() : base(true) { }

        internal SafeTokenHandle(IntPtr handle, bool own = true) : base(own)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle() => Kernel32.CloseHandle(handle);

        public T GetInfo<T>(TOKEN_INFORMATION_CLASS type)
        {
            int cbSize = Marshal.SizeOf(typeof(T));
            IntPtr pType = Marshal.AllocHGlobal(cbSize);

            try
            {
                // Retrieve token information. 
                if (!Advapi32.GetTokenInformation(this, type, pType, cbSize, out cbSize))
                    throw new System.ComponentModel.Win32Exception();

                // Marshal from native to .NET.
                switch (type)
                {
                    case TOKEN_INFORMATION_CLASS.TokenType:
                    case TOKEN_INFORMATION_CLASS.TokenImpersonationLevel:
                    case TOKEN_INFORMATION_CLASS.TokenSessionId:
                    case TOKEN_INFORMATION_CLASS.TokenSandBoxInert:
                    case TOKEN_INFORMATION_CLASS.TokenOrigin:
                    case TOKEN_INFORMATION_CLASS.TokenElevationType:
                    case TOKEN_INFORMATION_CLASS.TokenHasRestrictions:
                    case TOKEN_INFORMATION_CLASS.TokenUIAccess:
                    case TOKEN_INFORMATION_CLASS.TokenVirtualizationAllowed:
                    case TOKEN_INFORMATION_CLASS.TokenVirtualizationEnabled:
                        return (T)Convert.ChangeType(Marshal.ReadInt32(pType), typeof(T));

                    case TOKEN_INFORMATION_CLASS.TokenLinkedToken:
                        return (T)Convert.ChangeType(Marshal.ReadIntPtr(pType), typeof(T));

                    case TOKEN_INFORMATION_CLASS.TokenUser:
                    case TOKEN_INFORMATION_CLASS.TokenGroups:
                    case TOKEN_INFORMATION_CLASS.TokenPrivileges:
                    case TOKEN_INFORMATION_CLASS.TokenOwner:
                    case TOKEN_INFORMATION_CLASS.TokenPrimaryGroup:
                    case TOKEN_INFORMATION_CLASS.TokenDefaultDacl:
                    case TOKEN_INFORMATION_CLASS.TokenSource:
                    case TOKEN_INFORMATION_CLASS.TokenStatistics:
                    case TOKEN_INFORMATION_CLASS.TokenRestrictedSids:
                    case TOKEN_INFORMATION_CLASS.TokenGroupsAndPrivileges:
                    case TOKEN_INFORMATION_CLASS.TokenElevation:
                    case TOKEN_INFORMATION_CLASS.TokenAccessInformation:
                    case TOKEN_INFORMATION_CLASS.TokenIntegrityLevel:
                    case TOKEN_INFORMATION_CLASS.TokenMandatoryPolicy:
                    case TOKEN_INFORMATION_CLASS.TokenLogonSid:
                        return (T)Marshal.PtrToStructure(pType, typeof(T));

                    case TOKEN_INFORMATION_CLASS.TokenSessionReference:
                    case TOKEN_INFORMATION_CLASS.TokenAuditPolicy:
                    default:
                        return default(T);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pType);
            }
        }

        public static SafeTokenHandle FromCurrentProcess(AccessTypes desiredAccess = AccessTypes.TokenDuplicate)
        {
            lock (currentProcessToken)
            {
                if (currentProcessToken == null)
                    currentProcessToken = FromProcess(Kernel32.GetCurrentProcess(), desiredAccess);
                return currentProcessToken;
            }
        }

        public static SafeTokenHandle FromCurrentThread(AccessTypes desiredAccess = AccessTypes.TokenDuplicate, bool openAsSelf = true)
            => FromThread(Kernel32.GetCurrentThread(), desiredAccess, openAsSelf);

        public static SafeTokenHandle FromProcess(IntPtr hProcess, AccessTypes desiredAccess = AccessTypes.TokenDuplicate)
        {
            SafeTokenHandle val;
            if (!Advapi32.OpenProcessToken(hProcess, desiredAccess, out val))
                throw new System.ComponentModel.Win32Exception();
            return val;
        }

        public static SafeTokenHandle FromThread(IntPtr hThread, AccessTypes desiredAccess = AccessTypes.TokenDuplicate, bool openAsSelf = true)
        {
            SafeTokenHandle val;
            if (!Advapi32.OpenThreadToken(hThread, desiredAccess, openAsSelf, out val))
            {
                if (Marshal.GetLastWin32Error() == ERROR_NO_TOKEN)
                {
                    SafeTokenHandle pval = FromCurrentProcess();
                    if (!Advapi32.DuplicateTokenEx(pval, AccessTypes.TokenImpersonate | desiredAccess, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.Impersonation, TokenType.TokenImpersonation, ref val))
                        throw new System.ComponentModel.Win32Exception();
                    if (!Advapi32.SetThreadToken(IntPtr.Zero, val))
                        throw new System.ComponentModel.Win32Exception();
                }
                else
                    throw new System.ComponentModel.Win32Exception();
            }
            return val;
        }
    }
}
