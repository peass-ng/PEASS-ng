using System;

namespace winPEAS.Native.Enums
{
    [Flags]
    public enum ServerTypes : uint
    {
        Workstation = 0x00000001,
        Server = 0x00000002,
        SqlServer = 0x00000004,
        DomainCtrl = 0x00000008,
        BackupDomainCtrl = 0x00000010,
        TimeSource = 0x00000020,
        AppleFilingProtocol = 0x00000040,
        Novell = 0x00000080,
        DomainMember = 0x00000100,
        PrintQueueServer = 0x00000200,
        DialinServer = 0x00000400,
        XenixServer = 0x00000800,
        UnixServer = 0x00000800,
        NT = 0x00001000,
        WindowsForWorkgroups = 0x00002000,
        MicrosoftFileAndPrintServer = 0x00004000,
        NTServer = 0x00008000,
        BrowserService = 0x00010000,
        BackupBrowserService = 0x00020000,
        MasterBrowserService = 0x00040000,
        DomainMaster = 0x00080000,
        OSF1Server = 0x00100000,
        VMSServer = 0x00200000,
        Windows = 0x00400000,
        DFS = 0x00800000,
        NTCluster = 0x01000000,
        TerminalServer = 0x02000000,
        VirtualNTCluster = 0x04000000,
        DCE = 0x10000000,
        AlternateTransport = 0x20000000,
        LocalListOnly = 0x40000000,
        PrimaryDomain = 0x80000000,
        All = 0xFFFFFFFF
    };
}
