using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using winPEAS.Helpers;

namespace winPEAS.Info.UserInfo.Token
{
    internal static class Token
    {
        // From Seatbelt

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeName(
            string lpSystemName,
            IntPtr lpLuid,
            System.Text.StringBuilder lpName,
            ref int cchName);

        public static Dictionary<string, string> GetTokenGroupPrivs()
        {
            // Returns all privileges that the current process/user possesses
            // adapted from https://stackoverflow.com/questions/4349743/setting-size-of-token-privileges-luid-and-attributes-array-returned-by-gettokeni

            Dictionary<string, string> results = new Dictionary<string, string> { };
            try
            {
                int tokenInfLength = 0;
                IntPtr thisHandle = WindowsIdentity.GetCurrent().Token;
                GetTokenInformation(thisHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, tokenInfLength, out tokenInfLength);
                IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);
                if (GetTokenInformation(WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenPrivileges, tokenInformation, tokenInfLength, out tokenInfLength))
                {
                    TOKEN_PRIVILEGES thisPrivilegeSet = (TOKEN_PRIVILEGES)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_PRIVILEGES));
                    for (int index = 0; index < thisPrivilegeSet.PrivilegeCount; index++)
                    {
                        LUID_AND_ATTRIBUTES laa = thisPrivilegeSet.Privileges[index];
                        StringBuilder strBuilder = new StringBuilder();
                        int luidNameLen = 0;
                        IntPtr luidPointer = Marshal.AllocHGlobal(Marshal.SizeOf(laa.Luid));
                        Marshal.StructureToPtr(laa.Luid, luidPointer, true);
                        LookupPrivilegeName(null, luidPointer, null, ref luidNameLen);
                        strBuilder.EnsureCapacity(luidNameLen + 1);
                        if (LookupPrivilegeName(null, luidPointer, strBuilder, ref luidNameLen))
                            results[strBuilder.ToString()] = $"{(LuidAttributes) laa.Attributes}";
                        Marshal.FreeHGlobal(luidPointer);
                    }
                }
            }
            catch (Exception ex)
            {
                Beaprint.PrintException(ex.Message);
            }
            return results;
        }
    }
}
