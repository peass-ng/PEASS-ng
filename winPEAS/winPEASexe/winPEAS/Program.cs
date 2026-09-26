using System;


namespace winPEAS
{
    public static class Program
    {
        // Static blacklists        
        //static string goodSoft = "Windows Phone Kits|Windows Kits|Windows Defender|Windows Mail|Windows Media Player|Windows Multimedia Platform|windows nt|Windows Photo Viewer|Windows Portable Devices|Windows Security|Windows Sidebar|WindowsApps|WindowsPowerShell| Windows$|Microsoft|WOW6432Node|internet explorer|Internet Explorer|Common Files";                       

        [STAThread]
        public static void Main(string[] args)
        {
            Checks.Checks.Run(args);
        }
    }
}
