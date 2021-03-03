using System;
using System.ComponentModel;

namespace winPEAS.Info.SystemInfo.SysMon
{
    // hashing algorithm reference from @mattifestation's SysmonRuleParser.ps1
    //  ref - https://github.com/mattifestation/PSSysmonTools/blob/master/PSSysmonTools/Code/SysmonRuleParser.ps1#L589-L595
    [Flags]
    public enum SysmonHashAlgorithm
    {
        [Description(SysMon.NotDefined)]
        NotDefined = 0,

        SHA1 = 1,
        MD5 = 2,
        SHA256 = 4,
    }
}