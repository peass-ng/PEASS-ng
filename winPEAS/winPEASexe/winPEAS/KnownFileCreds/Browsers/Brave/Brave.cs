using System.IO;

namespace winPEAS.KnownFileCreds.Browsers.Brave
{
    internal class Brave : ChromiumBase, IBrowser
    {
        public override string Name => "Brave Browser";

        public override string BaseAppDataPath => Path.Combine(AppDataPath, "..\\Local\\BraveSoftware\\Brave-Browser\\User Data\\Default\\");

        public override void PrintInfo()
        {
            PrintSavedCredentials();
        }
    }
}
