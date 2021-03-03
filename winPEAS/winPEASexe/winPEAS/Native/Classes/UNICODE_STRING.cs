using System;
using System.Runtime.InteropServices;
using winPEAS.Helpers;

namespace winPEAS.Native.Classes
{
    [StructLayout(LayoutKind.Sequential)]
    public class UNICODE_STRING : IDisposable
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;

        public UNICODE_STRING()
            : this(null)
        {
        }

        public UNICODE_STRING(string s)
        {
            if (s != null)
            {
                Length = (ushort)(s.Length * 2);
                MaximumLength = (ushort)(Length + 2);
                Buffer = Marshal.StringToHGlobalUni(s);
            }
        }

        public override string ToString() => Buffer != IntPtr.Zero ? Marshal.PtrToStringUni(Buffer) : null;

        protected virtual void Dispose(bool disposing)
        {
            if (Buffer != IntPtr.Zero)
            {
                try
                {
                    Marshal.FreeHGlobal(Buffer);
                }
                catch (Exception ex)
                {
                    Beaprint.GrayPrint(string.Format("  [X] Exception: {0}", ex));
                }
                Buffer = IntPtr.Zero;
            }
        }

        ~UNICODE_STRING() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
