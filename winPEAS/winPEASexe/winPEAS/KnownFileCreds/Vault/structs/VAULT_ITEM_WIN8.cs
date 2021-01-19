using System;
using System.Runtime.InteropServices;

namespace winPEAS.KnownFileCreds.Vault.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VAULT_ITEM_WIN8
    {
        public Guid SchemaId;
        public IntPtr pszCredentialFriendlyName;
        public IntPtr pResourceElement;
        public IntPtr pIdentityElement;
        public IntPtr pAuthenticatorElement;
        public IntPtr pPackageSid;
        public UInt64 LastModified;
        public UInt32 dwFlags;
        public UInt32 dwPropertiesCount;
        public IntPtr pPropertyElements;
    }
}
