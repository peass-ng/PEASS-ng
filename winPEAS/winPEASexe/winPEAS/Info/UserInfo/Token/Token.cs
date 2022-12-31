using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using winPEAS.Helpers;
using winPEAS.Native;
using winPEAS.Native.Enums;

namespace winPEAS.Info.UserInfo.Token
{
    internal static class Token
    {
        public static Dictionary<string, string> GetTokenGroupPrivs()
        {
            // Returns all privileges that the current process/user possesses
            // adapted from https://stackoverflow.com/questions/4349743/setting-size-of-token-privileges-luid-and-attributes-array-returned-by-gettokeni

            var results = new Dictionary<string, string>();
            try
            {
                int tokenInfLength = 0;
                IntPtr thisHandle = WindowsIdentity.GetCurrent().Token;
                Advapi32.GetTokenInformation(thisHandle, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, tokenInfLength, out tokenInfLength);
                IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);
                if (Advapi32.GetTokenInformation(WindowsIdentity.GetCurrent().Token, TOKEN_INFORMATION_CLASS.TokenPrivileges, tokenInformation, tokenInfLength, out tokenInfLength))
                {
                    TOKEN_PRIVILEGES thisPrivilegeSet = (TOKEN_PRIVILEGES)Marshal.PtrToStructure(tokenInformation, typeof(TOKEN_PRIVILEGES));
                    for (int index = 0; index < thisPrivilegeSet.PrivilegeCount; index++)
                    {
                        LUID_AND_ATTRIBUTES laa = thisPrivilegeSet.Privileges[index];
                        StringBuilder strBuilder = new StringBuilder();
                        int luidNameLen = 0;
                        IntPtr luidPointer = Marshal.AllocHGlobal(Marshal.SizeOf(laa.Luid));
                        Marshal.StructureToPtr(laa.Luid, luidPointer, true);
                        Advapi32.LookupPrivilegeName(null, luidPointer, null, ref luidNameLen);
                        strBuilder.EnsureCapacity(luidNameLen + 1);
                        if (Advapi32.LookupPrivilegeName(null, luidPointer, strBuilder, ref luidNameLen))
                            results[strBuilder.ToString()] = $"{(LuidAttributes)laa.Attributes}";
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
