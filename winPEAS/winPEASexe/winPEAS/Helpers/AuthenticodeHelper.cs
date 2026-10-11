using System;
using System.Runtime.InteropServices;

namespace winPEAS.Helpers
{
    internal static class AuthenticodeHelper
    {
        private static readonly Guid WinTrustActionGenericVerifyV2 =
            new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

        internal static bool Verify(string path, out string status)
        {
            IntPtr fileInfoPointer = IntPtr.Zero;
            try
            {
                var fileInfo = new WinTrustFileInfo(path);
                fileInfoPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);

                var trustData = new WinTrustData(fileInfoPointer);
                int result = WinVerifyTrust(new IntPtr(-1), WinTrustActionGenericVerifyV2, trustData);
                status = result == 0 ? "Valid" : "Invalid (0x" + result.ToString("X8") + ")";
                return result == 0;
            }
            catch (Exception ex)
            {
                status = "Error: " + ex.Message;
                return false;
            }
            finally
            {
                if (fileInfoPointer != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(fileInfoPointer, typeof(WinTrustFileInfo));
                    Marshal.FreeCoTaskMem(fileInfoPointer);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WinTrustFileInfo
        {
            public uint StructSize;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string FilePath;
            public IntPtr FileHandle;
            public IntPtr KnownSubject;

            public WinTrustFileInfo(string filePath)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));
                FilePath = filePath;
                FileHandle = IntPtr.Zero;
                KnownSubject = IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustData
        {
            public uint StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData));
            public IntPtr PolicyCallbackData = IntPtr.Zero;
            public IntPtr SipClientData = IntPtr.Zero;
            public uint UiChoice = 2; // WTD_UI_NONE
            public uint RevocationChecks = 0; // WTD_REVOKE_NONE
            public uint UnionChoice = 1; // WTD_CHOICE_FILE
            public IntPtr FileInfoPointer;
            public uint StateAction = 0; // WTD_STATEACTION_IGNORE
            public IntPtr StateData = IntPtr.Zero;
            public IntPtr UrlReference = IntPtr.Zero;
            public uint ProviderFlags = 0x00001000; // WTD_CACHE_ONLY_URL_RETRIEVAL
            public uint UiContext = 0;
            public IntPtr SignatureSettings = IntPtr.Zero;

            public WinTrustData(IntPtr fileInfoPointer)
            {
                FileInfoPointer = fileInfoPointer;
            }
        }

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
            [In] WinTrustData trustData);
    }
}
