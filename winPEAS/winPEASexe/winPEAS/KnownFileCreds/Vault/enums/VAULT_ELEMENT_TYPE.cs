using System;

namespace winPEAS.KnownFileCreds.Vault.Enums
{
    public enum VAULT_ELEMENT_TYPE : Int32
    {
        Undefined = -1,
        Boolean = 0,
        Short = 1,
        UnsignedShort = 2,
        Int = 3,
        UnsignedInt = 4,
        Double = 5,
        Guid = 6,
        String = 7,
        ByteArray = 8,
        TimeStamp = 9,
        ProtectedArray = 10,
        Attribute = 11,
        Sid = 12,
        Last = 13
    }
}
