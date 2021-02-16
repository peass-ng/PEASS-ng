using System.Collections.Generic;

namespace winPEAS.Info.ProcessInfo
{
    static class InterestingProcesses
    {
        public static Dictionary<string, string> Definitions = new Dictionary<string, string>()
        {
            {"CmRcService.exe"             , "Configuration Manager Remote Control Service"},
            {"ftp.exe"                     , "Misc. FTP client"},
            {"LMIGuardian.exe"             , "LogMeIn Reporter"},
            {"LogMeInSystray.exe"          , "LogMeIn System Tray"},
            {"RaMaint.exe"                 , "LogMeIn maintenance sevice"},
            {"mmc.exe"                     , "Microsoft Management Console"},
            {"putty.exe"                   , "Putty SSH client"},
            {"pscp.exe"                    , "Putty SCP client"},
            {"psftp.exe"                   , "Putty SFTP client"},
            {"puttytel.exe"                , "Putty Telnet client"},
            {"plink.exe"                   , "Putty CLI client"},
            {"pageant.exe"                 , "Putty SSH auth agent"},
            {"kitty.exe"                   , "Kitty SSH client"},
            {"telnet.exe"                  , "Misc. Telnet client"},
            {"SecureCRT.exe"               , "SecureCRT SSH/Telnet client"},
            {"TeamViewer.exe"              , "TeamViewer"},
            {"tv_x64.exe"                  , "TeamViewer x64 remote control"},
            {"tv_w32.exe"                  , "TeamViewer x86 remote control"},
            {"keepass.exe"                 , "KeePass password vault"},
            {"mstsc.exe"                   , "Microsoft RDP client"},
            {"vnc.exe"                     , "Possible VNC client"},
            {"powershell.exe"              , "PowerShell host process"},
            {"cmd.exe"                     , "Command Prompt"},
            {"WinSCP.exe"                  , "WINScp client"},
            {"Code.exe"                    , "Visual Studio Code"},
            {"filezilla.exe"               , "FileZilla Client"},
        };
    }
}
