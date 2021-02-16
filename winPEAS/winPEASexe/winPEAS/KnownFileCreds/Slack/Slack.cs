using System;
using System.IO;
using winPEAS.Helpers;
using winPEAS.Info.UserInfo;

namespace winPEAS.KnownFileCreds.Slack
{
    internal static class Slack
    {
        const string SlackBasePath = @"AppData\Roaming\Slack\";

        internal static void PrintInfo()
        {
            Beaprint.MainPrint("Slack files & directories");

            Beaprint.ColorPrint("  note: check manually if something is found", Beaprint.YELLOW);

            var userDirs = User.GetUsersFolders();

            foreach (var userDir in userDirs)
            {
                try
                {
                    var userSlackDir = Path.Combine(userDir, SlackBasePath);

                    if (Directory.Exists(userSlackDir))
                    {
                        Beaprint.BadPrint($"   Directory:       {userSlackDir}");

                        var userSlackCookiesFile = Path.Combine(userSlackDir, "Cookies");
                        if (File.Exists(userSlackCookiesFile))
                        {
                            Beaprint.BadPrint($"   File:            {userSlackCookiesFile}");
                        }

                        var userSlackWorkspacesPath = Path.Combine(userSlackDir, @"storage\slack-workspaces");
                        if (File.Exists(userSlackWorkspacesPath))
                        {
                            Beaprint.BadPrint($"   File:            {userSlackWorkspacesPath}");
                        }

                        var userSlackDownloadsPath = Path.Combine(userSlackDir, @"storage\slack-downloads");
                        if (File.Exists(userSlackDownloadsPath))
                        {
                            Beaprint.BadPrint($"   File:            {userSlackDownloadsPath}");
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
