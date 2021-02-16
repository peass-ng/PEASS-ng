using System;
using System.Runtime.InteropServices;
using winPEAS.KnownFileCreds.Kerberos;
using winPEAS.KnownFileCreds.SecurityPackages;

namespace winPEAS.Native
{
    internal class Secur32
    {
        [DllImport("secur32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint AcquireCredentialsHandle(
            IntPtr pszPrincipal,
            string pszPackage,
            int fCredentialUse,
            IntPtr PAuthenticationID,
            IntPtr pAuthData,
            int pGetKeyFn,
            IntPtr pvGetKeyArgument,
            ref SECURITY_HANDLE phCredential,
            ref SecurityPackages.SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint InitializeSecurityContext(
            ref SECURITY_HANDLE phCredential,
            IntPtr phContext,
            IntPtr pszTargetName,
            int fContextReq,
            int Reserved1,
            int TargetDataRep,
            IntPtr pInput,
            int Reserved2,
            out SECURITY_HANDLE phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SecurityPackages.SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint InitializeSecurityContext(
            ref SECURITY_HANDLE phCredential,
            ref SECURITY_HANDLE phContext,
            IntPtr pszTargetName,
            int fContextReq,
            int Reserved1,
            int TargetDataRep,
            ref SecBufferDesc pInput,
            int Reserved2,
            out SECURITY_HANDLE phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SecurityPackages.SECURITY_INTEGER ptsExpiry);

        [DllImport("secur32.dll", SetLastError = true)]
        internal static extern uint AcceptSecurityContext(
            ref SECURITY_HANDLE phCredential,
            IntPtr phContext,
            ref SecBufferDesc pInput,
            uint fContextReq,
            uint TargetDataRep,
            out SECURITY_HANDLE phNewContext,
            out SecBufferDesc pOutput,
            out uint pfContextAttr,
            out SecurityPackages.SECURITY_INTEGER ptsTimeStamp);

        [DllImport("secur32.dll", SetLastError = true)]
        internal static extern uint DeleteSecurityContext(ref SECURITY_HANDLE phCredential);

        [DllImport("secur32.dll", SetLastError = true)]
        internal static extern uint FreeCredentialsHandle(ref SECURITY_HANDLE phCredential);

        [DllImport("secur32.dll", SetLastError = true)]
        internal static extern int
            LsaRegisterLogonProcess(LSA_STRING_IN LogonProcessName, out IntPtr LsaHandle, out ulong SecurityMode);

        [DllImport("Secur32.dll", SetLastError = false)]
        internal static extern uint LsaEnumerateLogonSessions(out UInt64 LogonSessionCount, out IntPtr LogonSessionList);

        [DllImport("Secur32.dll", SetLastError = false)]
        internal static extern uint LsaGetLogonSessionData(IntPtr luid, out IntPtr ppLogonSessionData);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern int LsaLookupAuthenticationPackage([In] IntPtr LsaHandle, [In] ref LSA_STRING_IN PackageName, [Out] out int AuthenticationPackage);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern int LsaCallAuthenticationPackage(IntPtr LsaHandle, int AuthenticationPackage, ref KERB_QUERY_TKT_CACHE_REQUEST ProtocolSubmitBuffer, int SubmitBufferLength, out IntPtr ProtocolReturnBuffer, out int ReturnBufferLength, out int ProtocolStatus);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern uint LsaFreeReturnBuffer(IntPtr buffer);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern int LsaConnectUntrusted([Out] out IntPtr LsaHandle);

        [DllImport("secur32.dll", SetLastError = false)]
        internal static extern int LsaDeregisterLogonProcess([In] IntPtr LsaHandle);

        [DllImport("secur32.dll", EntryPoint = "LsaCallAuthenticationPackage", SetLastError = false)]
        internal static extern int LsaCallAuthenticationPackage_KERB_RETRIEVE_TKT(IntPtr LsaHandle, int AuthenticationPackage, ref KERB_RETRIEVE_TKT_REQUEST ProtocolSubmitBuffer, int SubmitBufferLength, out IntPtr ProtocolReturnBuffer, out int ReturnBufferLength, out int ProtocolStatus);
    }
}
