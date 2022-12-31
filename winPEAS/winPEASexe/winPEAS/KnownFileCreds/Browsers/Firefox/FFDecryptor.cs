using System;
using System.Runtime.InteropServices;
using System.Text;
using winPEAS.Native;

namespace winPEAS.KnownFileCreds.Browsers.Firefox
{
    /// <summary>
    /// Firefox helper class
    /// </summary>
    static class FFDecryptor
    {
        static IntPtr NSS3;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long DLLFunctionDelegate(string configdir);

        private const string ffFolderName = @"\Mozilla Firefox\";
        public static long NSS_Init(string configdir)
        {
            var mozillaPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + ffFolderName;
            if (!System.IO.Directory.Exists(mozillaPath))
                mozillaPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + ffFolderName;
            if (!System.IO.Directory.Exists(mozillaPath))
                throw new Exception("Firefox folder not found");

            Kernel32.LoadLibrary(mozillaPath + "mozglue.dll");
            NSS3 = Kernel32.LoadLibrary(mozillaPath + "nss3.dll");
            IntPtr pProc = Kernel32.GetProcAddress(NSS3, "NSS_Init");
            DLLFunctionDelegate dll = (DLLFunctionDelegate)Marshal.GetDelegateForFunctionPointer(pProc, typeof(DLLFunctionDelegate));
            return dll(configdir);
        }

        public static string Decrypt(string cypherText)
        {
            IntPtr ffDataUnmanagedPointer = IntPtr.Zero;
            StringBuilder sb = new StringBuilder(cypherText);

            try
            {
                byte[] ffData = Convert.FromBase64String(cypherText);

                ffDataUnmanagedPointer = Marshal.AllocHGlobal(ffData.Length);
                Marshal.Copy(ffData, 0, ffDataUnmanagedPointer, ffData.Length);

                TSECItem tSecDec = new TSECItem();
                TSECItem item = new TSECItem();
                item.SECItemType = 0;
                item.SECItemData = ffDataUnmanagedPointer;
                item.SECItemLen = ffData.Length;

                if (PK11SDR_Decrypt(ref item, ref tSecDec, 0) == 0)
                {
                    if (tSecDec.SECItemLen != 0)
                    {
                        byte[] bvRet = new byte[tSecDec.SECItemLen];
                        Marshal.Copy(tSecDec.SECItemData, bvRet, 0, tSecDec.SECItemLen);
                        return Encoding.ASCII.GetString(bvRet);
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (ffDataUnmanagedPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ffDataUnmanagedPointer);
                }
            }

            return null;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DLLFunctionDelegate4(IntPtr arenaOpt, IntPtr outItemOpt, StringBuilder inStr, int inLen);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DLLFunctionDelegate5(ref TSECItem data, ref TSECItem result, int cx);
        public static int PK11SDR_Decrypt(ref TSECItem data, ref TSECItem result, int cx)
        {
            IntPtr pProc = Kernel32.GetProcAddress(NSS3, "PK11SDR_Decrypt");
            DLLFunctionDelegate5 dll = (DLLFunctionDelegate5)Marshal.GetDelegateForFunctionPointer(pProc, typeof(DLLFunctionDelegate5));
            return dll(ref data, ref result, cx);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TSECItem
        {
            public int SECItemType;
            public IntPtr SECItemData;
            public int SECItemLen;
        }
    }
}
