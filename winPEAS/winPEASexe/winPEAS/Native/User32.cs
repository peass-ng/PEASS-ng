using System;
using System.Runtime.InteropServices;
using System.Text;

namespace winPEAS.Native
{
    internal class User32
    {
        // https://stackoverflow.com/questions/115868/how-do-i-get-the-title-of-the-current-active-window-using-c
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }
}
