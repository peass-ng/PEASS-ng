using System;

namespace winPEAS.KnownFileCreds.Kerberos
{
    public enum KERB_ENCRYPTION_TYPE : UInt32
    {
        reserved0 = 0,
        des_cbc_crc = 1,
        des_cbc_md4 = 2,
        des_cbc_md5 = 3,
        reserved1 = 4,
        des3_cbc_md5 = 5,
        reserved2 = 6,
        des3_cbc_sha1 = 7,
        dsaWithSHA1_CmsOID = 9,
        md5WithRSAEncryption_CmsOID = 10,
        sha1WithRSAEncryption_CmsOID = 11,
        rc2CBC_EnvOID = 12,
        rsaEncryption_EnvOID = 13,
        rsaES_OAEP_ENV_OID = 14,
        des_ede3_cbc_Env_OID = 15,
        des3_cbc_sha1_kd = 16,
        aes128_cts_hmac_sha1_96 = 17,
        aes256_cts_hmac_sha1_96 = 18,
        aes128_cts_hmac_sha256_128 = 19,
        aes256_cts_hmac_sha384_192 = 20,
        rc4_hmac = 23,
        rc4_hmac_exp = 24,
        camellia128_cts_cmac = 25,
        camellia256_cts_cmac = 26,
        subkey_keymaterial = 65
    }

    [Flags]
    public enum KERB_TICKET_FLAGS : UInt32
    {
        reserved = 2147483648,
        forwardable = 0x40000000,
        forwarded = 0x20000000,
        proxiable = 0x10000000,
        proxy = 0x08000000,
        may_postdate = 0x04000000,
        postdated = 0x02000000,
        invalid = 0x01000000,
        renewable = 0x00800000,
        initial = 0x00400000,
        pre_authent = 0x00200000,
        hw_authent = 0x00100000,
        ok_as_delegate = 0x00040000,
        name_canonicalize = 0x00010000,
        //cname_in_pa_data = 0x00040000,
        enc_pa_rep = 0x00010000,
        reserved1 = 0x00000001
    }

    [Flags]
    public enum KERB_CACHE_OPTIONS : UInt64
    {
        KERB_RETRIEVE_TICKET_DEFAULT = 0x0,
        KERB_RETRIEVE_TICKET_DONT_USE_CACHE = 0x1,
        KERB_RETRIEVE_TICKET_USE_CACHE_ONLY = 0x2,
        KERB_RETRIEVE_TICKET_USE_CREDHANDLE = 0x4,
        KERB_RETRIEVE_TICKET_AS_KERB_CRED = 0x8,
        KERB_RETRIEVE_TICKET_WITH_SEC_CRED = 0x10,
        KERB_RETRIEVE_TICKET_CACHE_TICKET = 0x20,
        KERB_RETRIEVE_TICKET_MAX_LIFETIME = 0x40,
    }

    public enum KERB_PROTOCOL_MESSAGE_TYPE : UInt32
    {
        KerbDebugRequestMessage = 0,
        KerbQueryTicketCacheMessage = 1,
        KerbChangeMachinePasswordMessage = 2,
        KerbVerifyPacMessage = 3,
        KerbRetrieveTicketMessage = 4,
        KerbUpdateAddressesMessage = 5,
        KerbPurgeTicketCacheMessage = 6,
        KerbChangePasswordMessage = 7,
        KerbRetrieveEncodedTicketMessage = 8,
        KerbDecryptDataMessage = 9,
        KerbAddBindingCacheEntryMessage = 10,
        KerbSetPasswordMessage = 11,
        KerbSetPasswordExMessage = 12,
        KerbVerifyCredentialsMessage = 13,
        KerbQueryTicketCacheExMessage = 14,
        KerbPurgeTicketCacheExMessage = 15,
        KerbRefreshSmartcardCredentialsMessage = 16,
        KerbAddExtraCredentialsMessage = 17,
        KerbQuerySupplementalCredentialsMessage = 18,
        KerbTransferCredentialsMessage = 19,
        KerbQueryTicketCacheEx2Message = 20,
        KerbSubmitTicketMessage = 21,
        KerbAddExtraCredentialsExMessage = 22,
        KerbQueryKdcProxyCacheMessage = 23,
        KerbPurgeKdcProxyCacheMessage = 24,
        KerbQueryTicketCacheEx3Message = 25,
        KerbCleanupMachinePkinitCredsMessage = 26,
        KerbAddBindingCacheEntryExMessage = 27,
        KerbQueryBindingCacheMessage = 28,
        KerbPurgeBindingCacheMessage = 29,
        KerbQueryDomainExtendedPoliciesMessage = 30,
        KerbQueryS4U2ProxyCacheMessage = 31
    }
}
