using System.IO;

namespace winPEAS.KnownFileCreds.Browsers.Opera
{
    internal class Opera : ChromiumBase, IBrowser
    {
        public override string Name => "Opera";

        public override void PrintInfo()
        {
            PrintSavedCredentials();
        }

        public override string BaseAppDataPath => Path.Combine(AppDataPath, "..\\Roaming\\Opera Software\\Opera Stable");
    }
}
