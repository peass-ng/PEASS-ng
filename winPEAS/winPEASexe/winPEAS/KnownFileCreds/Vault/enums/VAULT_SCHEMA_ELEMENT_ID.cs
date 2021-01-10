using System;

namespace winPEAS.KnownFileCreds.Vault.Enums
{
    public enum VAULT_SCHEMA_ELEMENT_ID : Int32
    {
        Illegal = 0,
        Resource = 1,
        Identity = 2,
        Authenticator = 3,
        Tag = 4,
        PackageSid = 5,
        AppStart = 100,
        AppEnd = 10000
    }
}
