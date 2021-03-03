using System;
using System.Runtime.InteropServices;
using System.Security;

namespace winPEAS.Helpers.CredentialManager
{
    [SuppressUnmanagedCodeSecurity]
    internal static class SecureStringHelper
    {
        internal static unsafe SecureString CreateSecureString(string plainString)
        {
            if (string.IsNullOrEmpty(plainString))
                return new SecureString();

            SecureString str;
            fixed (char* str2 = plainString)
            {
                var chPtr = str2;
                str = new SecureString(chPtr, plainString.Length);
                str.MakeReadOnly();
            }

            return str;
        }

        internal static string CreateString(SecureString secureString)
        {
            if ((secureString == null) || (secureString.Length == 0))
                return string.Empty;

            string str;
            var zero = IntPtr.Zero;

            try
            {
                zero = Marshal.SecureStringToBSTR(secureString);
                str = Marshal.PtrToStringBSTR(zero);
            }
            finally
            {
                if (zero != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(zero);
            }

            return str;
        }
    }
}
