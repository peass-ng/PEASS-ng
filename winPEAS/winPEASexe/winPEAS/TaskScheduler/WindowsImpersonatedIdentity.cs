using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using winPEAS.Native;
using winPEAS.Native.Classes;

namespace winPEAS.TaskScheduler
{
    /// <summary>
    /// Impersonation of a user. Allows to execute code under another
    /// user context.
    /// Please note that the account that instantiates the Impersonator class
    /// needs to have the 'Act as part of operating system' privilege set.
    /// </summary>
    internal class WindowsImpersonatedIdentity : IDisposable, IIdentity
    {
        private const int LOGON_TYPE_NEW_CREDENTIALS = 9;
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_PROVIDER_DEFAULT = 0;
        private const int LOGON32_PROVIDER_WINNT50 = 3;

#if NETSTANDARD || NETCOREAPP
#else
        private WindowsImpersonationContext impersonationContext = null;
#endif
        SafeTokenHandle token;
        private WindowsIdentity identity = null;

        /// <summary>
        /// Constructor. Starts the impersonation with the given credentials.
        /// Please note that the account that instantiates the Impersonator class
        /// needs to have the 'Act as part of operating system' privilege set.
        /// </summary>
        /// <param name="userName">The name of the user to act as.</param>
        /// <param name="domainName">The domain name of the user to act as.</param>
        /// <param name="password">The password of the user to act as.</param>
        public WindowsImpersonatedIdentity(string userName, string domainName, string password)
        {
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(domainName) && string.IsNullOrEmpty(password))
            {
                identity = WindowsIdentity.GetCurrent();
            }
            else
            {
                if (Advapi32.LogonUser(userName, domainName, password, domainName == null ? LOGON_TYPE_NEW_CREDENTIALS : LOGON32_LOGON_INTERACTIVE, domainName == null ? LOGON32_PROVIDER_WINNT50 : LOGON32_PROVIDER_DEFAULT, out token) != 0)
                {
#if NETSTANDARD || NETCOREAPP
					if (!NativeMethods.ImpersonateLoggedOnUser(token.DangerousGetHandle()))
						throw new Win32Exception();
#else
                    identity = new WindowsIdentity(token.DangerousGetHandle());
                    impersonationContext = identity.Impersonate();
#endif
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        public string AuthenticationType => identity?.AuthenticationType;

        public bool IsAuthenticated => identity != null && identity.IsAuthenticated;

        public string Name => identity?.Name;

        public void Dispose()
        {
#if NETSTANDARD || NETCOREAPP
			NativeMethods.RevertToSelf();
#else
            impersonationContext?.Undo();
#endif
            token?.Dispose();
            identity?.Dispose();
        }
    }
}
