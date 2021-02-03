using System;

namespace winPEAS.Native.Enums
{
    [Flags]
    public enum AccessTypes : uint
    {
        TokenAssignPrimary = 0x0001,
        TokenDuplicate = 0x0002,
        TokenImpersonate = 0x0004,
        TokenQuery = 0x0008,
        TokenQuerySource = 0x0010,
        TokenAdjustPrivileges = 0x0020,
        TokenAdjustGroups = 0x0040,
        TokenAdjustDefault = 0x0080,
        TokenAdjustSessionID = 0x0100,
        TokenAllAccessP = 0x000F00FF,
        TokenAllAccess = 0x000F01FF,
        TokenRead = 0x00020008,
        TokenWrite = 0x000200E0,
        TokenExecute = 0x00020000,

        Delete = 0x00010000,
        ReadControl = 0x00020000,
        WriteDac = 0x00040000,
        WriteOwner = 0x00080000,
        Synchronize = 0x00100000,
        StandardRightsRequired = 0x000F0000,
        StandardRightsRead = 0x00020000,
        StandardRightsWrite = 0x00020000,
        StandardRightsExecute = 0x00020000,
        StandardRightsAll = 0x001F0000,
        SpecificRightsAll = 0x0000FFFF,
        AccessSystemSecurity = 0x01000000,
        MaximumAllowed = 0x02000000,
        GenericRead = 0x80000000,
        GenericWrite = 0x40000000,
        GenericExecute = 0x20000000,
        GenericAll = 0x10000000,
    }
}
