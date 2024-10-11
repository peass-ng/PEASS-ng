using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using winPEAS.Helpers.CredentialManager;
using winPEAS.Info.NetworkInfo;
using winPEAS.Native.Classes;
using winPEAS.Native.Enums;
using winPEAS.Native.Structs;

namespace winPEAS.Native
{
    internal class Advapi32
    {
        /// <summary>
        ///     The CredRead function reads a credential from the user's credential set.
        ///     The credential set used is the one associated with the logon session of the current token.
        ///     The token must not have the user's SID disabled.
        /// </summary>
        /// <remarks>
        ///     If the value of the Type member of the CREDENTIAL structure specified by the Credential parameter is
        ///     CRED_TYPE_DOMAIN_EXTENDED, a namespace must be specified in the target name. This function can return only one
        ///     credential of the specified type.
        /// </remarks>
        /// <param name="target">Pointer to a null-terminated string that contains the name of the credential to read.</param>
        /// <param name="type">Type of the credential to read. Type must be one of the CRED_TYPE_* defined types.</param>
        /// <param name="reservedFlag">Currently reserved and must be zero.</param>
        /// <param name="credentialPtr">
        ///     Pointer to a single allocated block buffer to return the credential.
        ///     Any pointers contained within the buffer are pointers to locations within this single allocated block.
        ///     The single returned buffer must be freed by calling CredFree.
        /// </param>
        /// <returns>The function returns TRUE on success and FALSE on failure.</returns>
        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPtr);

        /// <summary>
        ///     The CredWrite function creates a new credential or modifies an existing credential in the user's credential set.
        ///     The new credential is associated with the logon session of the current token.
        ///     The token must not have the user's security identifier (SID) disabled.
        /// </summary>
        /// <remarks>
        ///     This function creates a credential if a credential with the specified TargetName and Type does not exist. If a
        ///     credential with the specified TargetName and Type exists, the new specified credential replaces the existing one.
        ///     When this function writes a CRED_TYPE_CERTIFICATE credential, the Credential->CredentialBlob member specifies the
        ///     PIN protecting the private key of the certificate specified by the Credential->UserName member. The credential
        ///     manager does not maintain the PIN. Rather, the PIN is passed to the cryptographic service provider (CSP) indicated
        ///     on the certificate for later use by the CSP and the authentication packages. The CSP defines the lifetime of the
        ///     PIN. Most CSPs flush the PIN when the smart card removal from the smart card reader.
        ///     If the value of the Type member of the CREDENTIAL structure specified by the Credential parameter is
        ///     CRED_TYPE_DOMAIN_EXTENDED, a namespace must be specified in the target name. This function does not support writing
        ///     to target names that contain wildcards.
        /// </remarks>
        /// <param name="userCredential">A pointer to the CREDENTIAL structure to be written.</param>
        /// <param name="flags">Flags that control the function's operation. The following flag is defined.</param>
        /// <returns>If the function succeeds, the function returns TRUE, if the function fails, it returns FALSE. </returns>
        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredWrite([In] ref NativeMethods.CREDENTIAL userCredential, [In] UInt32 flags);

        /// <summary>
        ///     The CredFree function frees a buffer returned by any of the credentials management functions.
        /// </summary>
        /// <param name="cred">Pointer to the buffer to be freed.</param>
        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        internal static extern void CredFree([In] IntPtr cred);

        /// <summary>
        ///     The CredDelete function deletes a credential from the user's credential set.
        ///     The credential set used is the one associated with the logon session of the current token.
        ///     The token must not have the user's SID disabled.
        /// </summary>
        /// <param name="target">Pointer to a null-terminated string that contains the name of the credential to delete.</param>
        /// <param name="type">
        ///     Type of the credential to delete. Must be one of the CRED_TYPE_* defined types.
        ///     For a list of the defined types, see the Type member of the CREDENTIAL structure.
        ///     If the value of this parameter is CRED_TYPE_DOMAIN_EXTENDED,
        ///     this function can delete a credential that specifies a user name when there are multiple credentials for the same
        ///     target. The value of the TargetName parameter must specify the user name as Target|UserName.
        /// </param>
        /// <param name="flags">Reserved and must be zero.</param>
        /// <returns>The function returns TRUE on success and FALSE on failure.</returns>
        [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode)]
        internal static extern bool CredDelete(StringBuilder target, CredentialType type, int flags);

        /// <summary>
        /// Enumerate credentials in the credential store
        /// signature: BOOL CredEnumerate (
        ///  _In_ LPCTSTR     Filter,
        ///  _In_ DWORD       Flags,
        ///  _Out_ DWORD       *Count,
        ///  _Out_ PCREDENTIAL **Credentials
        ///);
        /// <param name="filter">[in] 
        /// Pointer to a null-terminated string that contains the filter for the returned credentials.Only credentials with a TargetName matching the filter will be returned.The filter specifies a name prefix followed by an asterisk.For instance, the filter "FRED*" will return all credentials with a TargetName beginning with the string "FRED".
        /// If NULL is specified, all credentials will be returned.</param>
        /// <param name="flag">[in]        
        /// The value of this parameter can be zero or more of the following values combined with a bitwise-OR operation.
        ///  Value Meaning
        ///  CRED_ENUMERATE_ALL_CREDENTIALS 0x1
        ///  This function enumerates all of the credentials in the user's credential set. The target name of each credential is returned in the "namespace:attribute=target" format. If this flag is set and the Filter parameter is not NULL, the function fails and returns ERROR_INVALID_FLAGS.
        ///  Windows Server 2003 and Windows XP:  This flag is not supported.
        ///</param>
        /// <param name="count">[out] Count of the credentials returned in the Credentials array.</param>
        /// <param name="pCredentials"> [out]      
        ///  Pointer to an array of pointers to credentials.The returned credential is a single allocated block. Any pointers contained within the buffer are pointers to locations within this single allocated block.The single returned buffer must be freed by calling CredFree.
        ///  Return value
        /// </param>
        /// <returns>
        /// The function returns TRUE on success and FALSE on failure. The GetLastError function can be called to get a more specific status code.The following status codes can be returned.
        ///  Return code/value Description
        ///  ERROR_NOT_FOUND
        ///  1168 (0x490)
        ///  No credential exists matching the specified Filter.
        ///  ERROR_NO_SUCH_LOGON_SESSION
        ///  1312 (0x520)
        ///  The logon session does not exist or there is no credential set associated with this logon session. Network logon sessions do not have an associated credential set.
        ///  ERROR_INVALID_FLAGS
        ///  1004 (0x3EC)
        ///  A flag that is not valid was specified for the Flags parameter, or CRED_ENUMERATE_ALL_CREDENTIALS is specified for the Flags parameter and the Filter parameter is not NULL.
        /// </returns>
        /// </summary>
        [DllImport("Advapi32.dll", EntryPoint = "CredEnumerate", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool CredEnumerate(string filter, int flag, out int count, out IntPtr pCredentials);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        //////////////////////////////////////////
        ///////  Find Modifiable Services ////////
        //////////////////////////////////////////

        /// Find services that you can modify using PS os sc for example
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool QueryServiceObjectSecurity(
            IntPtr serviceHandle,
            SecurityInfos secInfo,
            byte[] lpSecDesrBuf,
            uint bufSize,
            out uint bufSizeNeeded);

        [DllImport("advapi32.dll", EntryPoint = "GetNamedSecurityInfoW", CharSet = CharSet.Unicode)]
        internal static extern int GetNamedSecurityInfo(
            string objectName,
            SE_OBJECT_TYPE objectType,
            SecurityInfos securityInfo,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(
            IntPtr SecurityDescriptor,
            uint StringSDRevision,
            SecurityInfos SecurityInformation,
            out IntPtr StringSecurityDescriptor,
            out int StringSecurityDescriptorSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeName(
            string lpSystemName,
            IntPtr lpLuid,
            StringBuilder lpName,
            ref int cchName);

        [DllImport("advapi32.dll")]
        internal static extern bool
            DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool RevertToSelf();

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail), DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateTokenEx([In] SafeTokenHandle ExistingTokenHandle, [In] AccessTypes DesiredAccess, [In] IntPtr TokenAttributes, [In] SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, [In] TokenType TokenType, [In, Out] ref SafeTokenHandle DuplicateTokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetTokenInformation(SafeTokenHandle hToken, TOKEN_INFORMATION_CLASS tokenInfoClass, IntPtr pTokenInfo, Int32 tokenInfoLength, out Int32 returnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern int LogonUser(string lpszUserName, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(string systemName, string name, out LUID luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, AccessTypes DesiredAccess, out SafeTokenHandle TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenThreadToken(IntPtr ThreadHandle, AccessTypes DesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool OpenAsSelf, out SafeTokenHandle TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetThreadToken(IntPtr ThreadHandle, SafeTokenHandle TokenHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool LookupAccountSid(
            string? lpSystemName,
            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
            StringBuilder lpName,
            ref uint cchName,
            StringBuilder ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        // P/Invoke declaration for RegQueryValueExW
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int RegQueryValueExW(
            SafeRegistryHandle hKey,
            string lpValueName,
            IntPtr lpReserved,
            out uint lpType,
            byte[] lpData,
            ref uint lpcbData);

        public byte[] ReadRegistryValue(string keyPath, string valueName)
        {
            using (RegistryKey baseKey = Registry.LocalMachine) // Access HKLM
            using (RegistryKey subKey = baseKey.OpenSubKey(keyPath, writable: false))
            {
                if (subKey == null)
                    throw new InvalidOperationException("Registry key not found.");

                SafeRegistryHandle hKey = subKey.Handle;
                uint lpType;
                uint dataSize = 0;

                // First call to determine the size of the data
                int ret = RegQueryValueExW(
                    hKey,
                    valueName,
                    IntPtr.Zero,
                    out lpType,
                    null,
                    ref dataSize);

                if (ret != 0)
                    throw new System.ComponentModel.Win32Exception(ret);

                byte[] data = new byte[dataSize];

                // Second call to get the actual data
                ret = RegQueryValueExW(
                    hKey,
                    valueName,
                    IntPtr.Zero,
                    out lpType,
                    data,
                    ref dataSize);

                if (ret != 0)
                    throw new System.ComponentModel.Win32Exception(ret);

                return data;
            }
        }

        public static string TranslateSid(string sid)
        {
            // adapted from http://www.pinvoke.net/default.aspx/advapi32.LookupAccountSid
            var accountSid = new SecurityIdentifier(sid);
            var accountSidByes = new byte[accountSid.BinaryLength];
            accountSid.GetBinaryForm(accountSidByes, 0);

            var name = new StringBuilder();
            var cchName = (uint)name.Capacity;
            var referencedDomainName = new StringBuilder();
            var cchReferencedDomainName = (uint)referencedDomainName.Capacity;

            var err = 0;
            if (!LookupAccountSid(null, accountSidByes, name, ref cchName, referencedDomainName, ref cchReferencedDomainName, out var sidUse))
            {
                err = Marshal.GetLastWin32Error();

                if (err == Win32Error.InsufficientBuffer)
                {
                    name.EnsureCapacity((int)cchName);
                    referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
                    err = 0;
                    if (!LookupAccountSid(null, accountSidByes, name, ref cchName, referencedDomainName,
                        ref cchReferencedDomainName, out sidUse))
                    {
                        err = Marshal.GetLastWin32Error();
                    }
                }
            }

            return err == 0 ? $"{referencedDomainName}\\{name}" : "";
        }
    }
}
