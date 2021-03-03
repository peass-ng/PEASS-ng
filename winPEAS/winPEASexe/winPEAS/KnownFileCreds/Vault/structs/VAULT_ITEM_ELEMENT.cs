using System.Runtime.InteropServices;
using winPEAS.KnownFileCreds.Vault.Enums;

namespace winPEAS.KnownFileCreds.Vault.Structs
{
    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
    public struct VAULT_ITEM_ELEMENT
    {
        [FieldOffset(0)]
        public VAULT_SCHEMA_ELEMENT_ID SchemaElementId;
        [FieldOffset(8)]
        public VAULT_ELEMENT_TYPE Type;
    }
}
