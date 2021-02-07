using System;
using System.Runtime.InteropServices;
using System.Text;
using winPEAS.Native.Structs;

namespace winPEAS.Native
{
    internal class User32
    {
        // https://stackoverflow.com/questions/115868/how-do-i-get-the-title-of-the-current-active-window-using-c
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("User32.dll")]
        public static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
    }
}
